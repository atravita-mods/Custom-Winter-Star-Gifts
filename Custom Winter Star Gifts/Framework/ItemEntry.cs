
using CWSG.Integrations;

using StardewModdingAPI;

using StardewValley;

using SObject = StardewValley.Object;

namespace CWSG.Framework;
public class ItemEntry
{
    public string? Name { get; set; }
    public int Quantity { get; set; } = 1;
    public ItemType Type { get; set; } = ItemType.Vanilla;
}

public enum ItemType
{
    Vanilla,
    JA,
    DGA
}

internal static class ItemEntryExtensions
{
    private static IApi? JAAPI;

    private static IDynamicGameAssetsApi? DGAAPI;

    internal static void Initialize(IModRegistry registry)
    {
        if (registry.IsLoaded("spacechase0.JsonAssets"))
        {
            JAAPI = registry.GetApi<IApi>("spacechase0.JsonAssets");
        }
        if (registry.IsLoaded("spacechase0.DynamicGameAssets"))
        {
            DGAAPI = registry.GetApi<IDynamicGameAssetsApi>("spacechase0.DynamicGameAssets");
        }
    }

    internal static Item? ResolveItemEntry(this ItemEntry entry)
    {
        if (entry.Name is null)
            return null;

        switch (entry.Type)
        {
            case ItemType.Vanilla:
            {
                var id = AssetManager.GetID(entry.Name);
                return id != -1 ? new SObject(id, entry.Quantity) : null;
            }
            case ItemType.JA:
            {
                var id = JAAPI?.GetObjectId(entry.Name) ?? -1;
                if (id == -1)
                    goto case ItemType.Vanilla;
                return new SObject(id, entry.Quantity);
            }
            case ItemType.DGA:
            {
                Item? item = DGAAPI?.SpawnDGAItem(entry.Name) as Item;
                if (item is not null)
                    item.Stack = entry.Quantity;
                return item;
            }
            default:
                return null;
        }
    }
}