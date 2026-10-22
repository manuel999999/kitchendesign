namespace KitchenDesign.Shared.Enums
{
    [System.Flags]
    public enum MeasureDisplayFlag
    {
        None = 0,
        Label = 1 << 0,
        Line = 1 << 1,
        Tick = 1 << 2,
        All = ~0
    }
}
