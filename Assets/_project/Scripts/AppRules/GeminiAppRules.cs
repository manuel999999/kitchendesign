using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign.AppRules
{
    public sealed class GeminiAppRules : IAppRules
    {
        public float OccupantMinimumHorizontalPaddingInMeters => Const.OccupantMinimumHorizontalPaddingInMeters;
        public float CabinetColumnWidthInMeters => Const.CabinetColumnWidthInMeters;

        public float? HighBarTopRiserDepthInMeters => null;
        public float? HighBarTopCountertopThicknessInMeters => null;
        public float? HighBarTopCountertopDepthInMeters => null;
        public bool ReappliesCountertopThicknessAfterLoad => false;

        public bool AppliesRuntimeProductOverrides => false;
        public float? FridgeHorizontalPaddingInMeters => null;
        public float? BottomAnchoredProductInsetInMeters => null;
        public bool AdjustsSmokerCutOutHeight => false;

        public PriceSheetExtras PriceSheetExtras => PriceSheetExtras.GeminiCatalog;
        public string NonCatalogQuoteDisclaimer => null;

        public bool MeasurementFractionsFollowBrandCatalog => true;
        public bool ShowsTickerFractions => true;
        public bool RequiresBrandCatalog => true;
        public bool ShowsStorageInProductSearch => false;
        public bool ShowsPlungeCategory => false;

        public bool IncludesStollManufacturer => false;

        public bool IsManufacturerIdAvailable(int manufacturerId)
        {
            return manufacturerId != ProFitAppRules.ProFitOnlyManufacturerId;
        }
    }
}
