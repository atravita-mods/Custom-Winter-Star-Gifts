using System;

namespace CWSG.Framework;

public class NPCGifts
{
    public string? NameOfNPC { get; set; }
    public ItemEntry[] ItemNames { get; set; } = Array.Empty<ItemEntry>();
    public ModeEnum Mode { get; set; } = ModeEnum.Overwrite;
    public int Priority { get; set; } = 100;
}

public enum ModeEnum
{
    Overwrite,
    AddToVanilla,
    AddToExisting,
}