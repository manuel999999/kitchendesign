namespace KitchenDesign.KitchenModel
{
    using KitchenDesign.Shared.Common;

    public static class StollSizes
    {
        public const float MinimumOccupantWidthInMetersCausingBowing = 15.0f * Units.Inches2Meters;
        public const float MaximumUnoccupiedPaddingInMeters = 8.0f * Units.Inches2Meters;
        public const float DefaultCabinetDepth = 29.0f;
        public const float FixedSmokerCabinetCutOutMetersToKeepBelowCountertop = 2.0f * Units.Inches2Meters;
    }
}
