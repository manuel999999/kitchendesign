using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign.AppRules
{
    public sealed class StollAppRules : IAppRules
    {
        public float OccupantMinimumHorizontalPaddingInMeters => 0.0f;
        public float CabinetColumnWidthInMeters => 6.0f * Units.Inches2Meters;

        public float? HighBarTopRiserDepthInMeters => 3.0f * Units.Inches2Meters;
        public float? HighBarTopCountertopThicknessInMeters => 2.0f * Units.Inches2Meters;
        public float? HighBarTopCountertopDepthInMeters => 12.0f * Units.Inches2Meters;
        public bool ReappliesCountertopThicknessAfterLoad => true;

        public bool AppliesRuntimeProductOverrides => true;
        public float? FridgeHorizontalPaddingInMeters => 0.0f;
        public float? BottomAnchoredProductInsetInMeters => 4.0f * Units.Inches2Meters;
        public bool AdjustsSmokerCutOutHeight => true;

        public PriceSheetExtras PriceSheetExtras => PriceSheetExtras.StollCustomKitchen;
        public string NonCatalogQuoteDisclaimer => "Countertop and finishes not included on this final quote.";

        public bool MeasurementFractionsFollowBrandCatalog => false;
        public bool ShowsTickerFractions => false;
        public bool RequiresBrandCatalog => false;
        public bool ShowsStorageInProductSearch => true;
        public bool ShowsPlungeCategory => true;

        public bool IncludesStollManufacturer => true;

        public bool IsManufacturerIdAvailable(int manufacturerId)
        {
            return manufacturerId != ProFitAppRules.ProFitOnlyManufacturerId;
        }
    }
}
