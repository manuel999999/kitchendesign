namespace KitchenDesign.Shared.Enums
{
    [System.Flags]
    public enum RepositionSnapMode : byte
    {
        None = 0,
        SnapHorizontal = 1 << 0,
        SnapVertical = 1 << 1
    }
}
