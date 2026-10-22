using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign.AppRules
{
    public sealed class ProFitAppRules : IAppRules
    {
        public const int ProFitOnlyManufacturerId = 137;

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

        public PriceSheetExtras PriceSheetExtras => PriceSheetExtras.None;
        public string NonCatalogQuoteDisclaimer => null;

        public bool MeasurementFractionsFollowBrandCatalog => false;
        public bool ShowsTickerFractions => false;
        public bool RequiresBrandCatalog => false;
        public bool ShowsStorageInProductSearch => true;
        public bool ShowsPlungeCategory => false;

        public bool IncludesStollManufacturer => false;

        public bool IsManufacturerIdAvailable(int manufacturerId)
        {
            return true;
        }
    }
}
