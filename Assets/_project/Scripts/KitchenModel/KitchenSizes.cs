namespace KitchenDesign.KitchenModel
{
    using KitchenDesign.Shared.Common;

    public static class KitchenSizes
    {
        public const float OccupantMinimumHorizontalPaddingInMeters = 5.0f * Units.Inches2Meters;
        public const float OccupantMinimumVerticalPaddingInMeters = 1.5f * Units.Inches2Meters;
        public const float CabinetCornerOccupiablePaddingInMeters = (6.0f * Units.Inches2Meters) - (OccupantMinimumHorizontalPaddingInMeters * 0.5f);
        public const float CabinetColumnWidthInMeters = 3.0f * Units.Inches2Meters;
        public const float SectionBuildingMethodMinCabinetLengthInMeters = 18.0f * Units.Inches2Meters;
        public const float SectionBuildingMethodMaxCabinetLengthInMeters = 60.0f * Units.Inches2Meters;
        public const float MinMetersFromEdgeAfterNudge = 2.0f * Units.Inches2Meters;
        public const int MaxStraightCabinetsPerSpan = 10;
        public const float ToeKickInsetInMeters = 1.5f * Units.Inches2Meters;
    }
}
