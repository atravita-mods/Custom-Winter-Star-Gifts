using System;
using System.Collections.Generic;

using SObject = StardewValley.Object;

using StardewValley;
using StardewModdingAPI;

namespace CWSG.Framework;
internal static class AssetManager
{
    private static IAssetName DataObjectInfo = null!;

    private static Lazy<Dictionary<string, int>> itemMap = new(GenerateItemMap);

    internal static void Initialize(IGameContentHelper helper)
    {
        DataObjectInfo = helper.ParseAssetName("Data/ObjectInformation");
    }

    internal static int GetID(string name)
    {
        if (int.TryParse(name, out int id) && Game1.objectInformation.ContainsKey(id))
            return id;

        return itemMap.Value.TryGetValue(name, out int val) ? val : -1;
    }

    internal static void OnAssetInvalidated(IReadOnlySet<IAssetName> assets)
    {
        if (assets.Contains(DataObjectInfo))
            Reset();
    }

    internal static void Reset()
    {
        if (itemMap.IsValueCreated)
            itemMap = new(GenerateItemMap);
    }

    private static Dictionary<string, int> GenerateItemMap()
    {
        ModEntry.ModMonitor.Log("Building map to resolve normal objects.", LogLevel.Info);

        Dictionary<string, int> mapping = new(Game1.objectInformation.Count)
        {
            // Special cases
            ["Egg"] = 176,
            ["Brown Egg"] = 180,
            ["Large Egg"] = 174,
            ["Large Brown Egg"] = 182,
            ["Strange Doll"] = 126,
            ["Strange Doll 2"] = 127,
        };

        // Processing from the data.
        foreach ((int id, string data) in Game1.objectInformation)
        {
            // category asdf should never end up in the player inventory.
            var cat = data.GetNthChunk('/', SObject.objectInfoTypeIndex);
            if (cat.Equals("asdf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = data.GetNthChunk('/', SObject.objectInfoNameIndex);
            if (name.Equals("Stone", StringComparison.OrdinalIgnoreCase) && id != 390)
            {
                continue;
            }
            if (name.Equals("Weeds", StringComparison.OrdinalIgnoreCase)
                || name.Equals("SupplyCrate", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Twig", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Rotten Plant", StringComparison.OrdinalIgnoreCase)
                || name.Equals("???", StringComparison.OrdinalIgnoreCase)
                || name.Equals("DGA Dummy Object", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Egg", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Large Egg", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Strange Doll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!mapping.TryAdd(name.ToString(), id))
            {
                ModEntry.ModMonitor.Log($"{name.ToString()} with {id} seems to be a duplicate SObject and may not be resolved correctly.", LogLevel.Warn);
            }
        }
        return mapping;
    }
}