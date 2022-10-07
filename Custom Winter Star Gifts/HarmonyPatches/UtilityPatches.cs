using System;
using System.Collections.Generic;
using System.Linq;

using CWSG.Framework;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Objects;

using SObject = StardewValley.Object;

namespace CWSG.HarmonyPatches;
internal static class UtilityPatches
{
    internal static bool getGiftFromNPC_Prefix(NPC who, ref Item __result)
    {
        try
        {
            Random r = new((int)Game1.uniqueIDForThisGame / 2 + Game1.year + Game1.dayOfMonth + Utility.getSeasonNumber(Game1.currentSeason) + who.getTileX());
            List<Item> possibleObjects = new();
            List<ItemEntry> possibleItemEntries = new();

            // need to ensure packs are loaded. Players like screwing with the clock.
            ModEntry.LoadPacks();
            
            SortedList<int, Dictionary<ModeEnum, List<ItemEntry>>>? allData = ModEntry.Data.TryGetValue("All", out var allEntries) ? allEntries : null;
            SortedList<int, Dictionary<ModeEnum, List<ItemEntry>>>? specificData = ModEntry.Data.TryGetValue(who.Name, out var specificEntries) ? specificEntries : null;
            

            List<int>? relevantKeys = Enumerable.Empty<int>().Concat(allData?.Keys ?? Enumerable.Empty<int>())
                .Concat(specificData?.Keys ?? Enumerable.Empty<int>()).ToHashSet().OrderByDescending(a => a).ToList();
            ModEntry.ModMonitor.Log(string.Join(',', relevantKeys));

            if (relevantKeys.Count == 0)
                return true; // no data for you.

            int allPriority = ModEntry.LowestUsefulPriority.TryGetValue("All", out int val) ? val : int.MaxValue;
            int specificPriority = ModEntry.LowestUsefulPriority.TryGetValue(who.Name, out int val2) ? val2 : int.MaxValue;
            int minPriority = Math.Min(allPriority, specificPriority);

            foreach (var key in relevantKeys)
            {
                // handle minimum entry shortcutting.
                if (key > allPriority)
                    allData?.Remove(key);
                if (key > specificPriority)
                    specificData?.Remove(key);
                if (key > minPriority)
                    continue;

                ModEntry.ModMonitor.Log($"Processing {key}");

                // this is the only block I care about AddToVanilla/Override Vanilla.
                if (key == minPriority)
                {
                    // check the specific list first.
                    if (specificData?.TryGetValue(key, out Dictionary<ModeEnum, List<ItemEntry>>? specific) == true)
                    {
                        if (!specific.TryGetValue(ModeEnum.Overwrite, out var itemEntries) || itemEntries.Count <= 0)
                        {
                            if (specific.TryGetValue(ModeEnum.AddToVanilla, out itemEntries))
                            {
                                possibleObjects = GetVanillaItems(who, r);
                            }
                        }

                        if (itemEntries?.Count > 0)
                        {
                            foreach (var itemEntry in itemEntries)
                            {
                                var item = itemEntry.ResolveItemEntry();
                                if (item is not null)
                                    possibleObjects.Add(item);
                                else
                                    ModEntry.ModMonitor.Log($"{itemEntry.Type} {itemEntry.Name} could not be resolved, skipping.", LogLevel.Info);
                            }
                        }
                    }

                    // If nothing was added by the specific, try the all items list.
                    if (possibleObjects.Count == 0 && allData?.TryGetValue(key, out var all) == true)
                    {
                        if (!all.TryGetValue(ModeEnum.Overwrite, out var itemEntries) || itemEntries.Count <= 0)
                        {
                            if (all.TryGetValue(ModeEnum.AddToVanilla, out itemEntries))
                                possibleObjects = GetVanillaItems(who, r);
                        }
                        if (itemEntries?.Count > 0)
                        {
                            possibleItemEntries.AddRange(itemEntries);
                        }
                    }
                }

                // process all data appends.
                {
                    if (allData?.TryGetValue(key, out var items)  == true && items.TryGetValue(ModeEnum.AddToExisting, out var itemEntries))
                    {
                        possibleItemEntries.AddRange(itemEntries);
                    }
                    if (specificData?.TryGetValue(key, out var specificItems) == true && specificItems.TryGetValue(ModeEnum.AddToExisting, out var itemEntries2))
                    {
                        possibleItemEntries.AddRange(itemEntries2);
                    }
                }
            }

            foreach (var itemEntry in possibleItemEntries)
            {
                var item = itemEntry.ResolveItemEntry();
                if (item is not null)
                    possibleObjects.Add(item);
                else
                    ModEntry.ModMonitor.Log($"{itemEntry.Type} {itemEntry.Name} could not be resolved, skipping.", LogLevel.Info);
            }

            ModEntry.ModMonitor.Log($"Got {possibleObjects.Count} objects");
            if (possibleObjects.Count > 0)
            {
                __result = Utility.GetRandom(possibleObjects, r);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in {nameof(getGiftFromNPC_Prefix)}:\n{ex}", LogLevel.Error);
        }
        return true; // run original logic
    }

    internal static List<Item> GetVanillaItems(NPC npc, Random r)
    {
        // copied from the vanilla method.
        List<Item> possibleObjects = new();
        switch (npc.Name)
        {
            case "Clint":
                possibleObjects.Add(new SObject(337, 1));
                possibleObjects.Add(new SObject(336, 5));
                possibleObjects.Add(new SObject(r.Next(535, 538), 5));
                break;
            case "Marnie":
                possibleObjects.Add(new SObject(176, 12));
                break;
            case "Robin":
                possibleObjects.Add(new SObject(388, 99));
                possibleObjects.Add(new SObject(390, 50));
                possibleObjects.Add(new SObject(709, 25));
                break;
            case "Willy":
                possibleObjects.Add(new SObject(690, 25));
                possibleObjects.Add(new SObject(687, 1));
                possibleObjects.Add(new SObject(703, 1));
                break;
            case "Evelyn":
                possibleObjects.Add(new SObject(223, 1));
                break;
            default:
                if (npc.Age == 2)
                {
                    possibleObjects.Add(new SObject(330, 1));
                    possibleObjects.Add(new SObject(103, 1));
                    possibleObjects.Add(new SObject(394, 1));
                    possibleObjects.Add(new SObject(r.Next(535, 538), 1));
                    break;
                }
                possibleObjects.Add(new SObject(608, 1));
                possibleObjects.Add(new SObject(651, 1));
                possibleObjects.Add(new SObject(611, 1));
                possibleObjects.Add(new Ring(517));
                possibleObjects.Add(new SObject(466, 10));
                possibleObjects.Add(new SObject(422, 1));
                possibleObjects.Add(new SObject(392, 1));
                possibleObjects.Add(new SObject(348, 1));
                possibleObjects.Add(new SObject(346, 1));
                possibleObjects.Add(new SObject(341, 1));
                possibleObjects.Add(new SObject(221, 1));
                possibleObjects.Add(new SObject(64, 1));
                possibleObjects.Add(new SObject(60, 1));
                possibleObjects.Add(new SObject(70, 1));
                break;
        }
        return possibleObjects;
    }
}