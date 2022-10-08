using System;
using System.Collections.Generic;
using System.Linq;

using CWSG.Framework;
using CWSG.HarmonyPatches;

using HarmonyLib;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;

namespace CWSG;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    // we defer loading to as late as possible
    // there is no point doing all the loading if, say, it's not currently the Winter Star.
    private static bool Loaded = false;

    /// <summary>
    /// Gets the data loaded in from content packs, organized by NPCName, then priority, then the list of gifts.
    /// </summary>
    internal static Dictionary<string, SortedList<int, Dictionary<ModeEnum, List<ItemEntry>>>> Data { get; } = new();

    /// <summary>
    /// Gets the lowest relevant priority.
    /// Since lower priorities actually override higher ones.
    /// We can skip to the LAST Override/AddToVanilla per NPC.
    /// </summary>
    internal static Dictionary<string, int> LowestUsefulPriority { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal static IMonitor ModMonitor { get; private set; } = null!;
    internal static IContentPackHelper ContentPackHelper { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        ContentPackHelper = this.Helper.ContentPacks;

        // apply harmony patches.
        var harmony = new Harmony(this.ModManifest.UniqueID);
        harmony.Patch(
           original: AccessTools.Method(typeof(Utility), nameof(Utility.getGiftFromNPC)),
           prefix: new HarmonyMethod(typeof(UtilityPatches), nameof(UtilityPatches.getGiftFromNPC_Prefix))
        );

        helper.ConsoleCommands.Add(
            "cwsg.force_reload",
            "Forces CWSG to reload the packs",
            static (_, _) =>
            {
                ModMonitor.Log("Forcibly reloading packs", LogLevel.Info);
                Reset();
                LoadPacks();
            });

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
    }

    internal static void LoadPacks()
    {
        if (Loaded)
            return;

        foreach (IContentPack contentPack in ContentPackHelper.GetOwned())
        {
            ModMonitor.Log($"Reading content pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} from {contentPack.DirectoryPath}", LogLevel.Info);

            CustomGiftData? customGiftData = null;

            try
            {
                customGiftData = contentPack.ReadJsonFile<CustomGiftData>("content.json");
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Failed reading content pack:\n\n{ex}", LogLevel.Error);
            }

            if (customGiftData is null)
            {
                ModMonitor.Log($"{contentPack.Manifest.Name} {contentPack.Manifest.Version} is missing a \"content.json\" file. Writing example file.", LogLevel.Error);

                // make an example and write it?
                CustomGiftData? example = new()
                {
                    NPCGifts = new NPCGifts[]
                    {
                        new NPCGifts()
                        {
                            NameOfNPC = "Robin",
                            ItemNames = new[]{ new ItemEntry() { Name = "Parsnip", Quantity = 1, Type = ItemType.Vanilla} },
                        },
                    }
                };
                contentPack.WriteJsonFile("content.json", example);
                continue;
            }

            foreach (var npcGiftEntry in customGiftData.NPCGifts)
            {
                if (npcGiftEntry?.NameOfNPC is null)
                {
                    ModMonitor.Log("Skipping entry with no NPC name", LogLevel.Warn);
                    continue;
                }

                if (LowestUsefulPriority.TryGetValue(npcGiftEntry.NameOfNPC, out var priority))
                {
                    if (npcGiftEntry.Priority > priority)
                    {
                        ModMonitor.Log($"Skipping pack for {npcGiftEntry.NameOfNPC} with priority {npcGiftEntry.Priority} as it has a higher priority than will be relevant.");
                        continue;
                    }
                }

                // update relevant lowest useful priority.
                if (npcGiftEntry.Mode != ModeEnum.AddToExisting)
                    LowestUsefulPriority[npcGiftEntry.NameOfNPC] = npcGiftEntry.Priority;

                // data structures are fun.
                if (!Data.TryGetValue(npcGiftEntry.NameOfNPC, out var giftEntries))
                    giftEntries = new();
                if (!giftEntries.TryGetValue(npcGiftEntry.Priority, out var values))
                    values = new(3);
                if (!values.TryGetValue(npcGiftEntry.Mode, out var itemEntries))
                    itemEntries = new(npcGiftEntry.ItemNames.Length);

                itemEntries.AddRange(npcGiftEntry.ItemNames.Where((itemEntry) => itemEntry?.Name is not null));

                values[npcGiftEntry.Mode] = itemEntries;
                giftEntries[npcGiftEntry.Priority] = values;
                Data[npcGiftEntry.NameOfNPC] = giftEntries;

            }
        }

        Loaded = true;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ItemEntryExtensions.Initialize(this.Helper.ModRegistry);
        AssetManager.Initialize(this.Helper.GameContent);

        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.OnAssetInvalidated(e.NamesWithoutLocale);
        this.Helper.Events.GameLoop.DayEnding += static (_, _) =>
        {
            AssetManager.Reset();
            Reset();
        };
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Loaded)
            return;

        // copied from content patcher - checks for whether or not it's currently the feast of the Winter Star.
        IDictionary<string, string> festivalDates = Game1.content.Load<Dictionary<string, string>>("Data\\Festivals\\FestivalDates", LocalizedContentManager.LanguageCode.en);
        if (festivalDates.TryGetValue($"{Game1.currentSeason}{Game1.dayOfMonth}", out string? festivalName) && festivalName == "Feast of the Winter Star")
            LoadPacks();
    }

    /// <summary>
    /// Clears the data loaded at the end of the day.
    /// </summary>
    private static void Reset()
    {
        Data.Clear();
        LowestUsefulPriority.Clear();
        Loaded = false;
    }
}