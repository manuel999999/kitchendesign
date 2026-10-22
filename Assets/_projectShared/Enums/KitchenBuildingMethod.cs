namespace KitchenDesign.Shared.Enums
{
    [System.Flags]
    public enum KitchenBuildingMethod
    {
        Standard = 1 << 0,
        Sections = 1 << 1,
        Catalog = 1 << 2
    }
}
