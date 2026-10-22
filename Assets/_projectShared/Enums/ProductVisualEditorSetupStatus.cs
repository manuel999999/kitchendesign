namespace KitchenDesign.Shared.Enums
{
    [System.Flags]
    public enum ProductVisualEditorSetupStatus
    {
        Good = 1 << 0,
        HashHasChanged = 1 << 1,
        MissingFields = 1 << 2
    }
}
