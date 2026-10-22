using KitchenDesign.Shared.Enums;

namespace KitchenDesign.Shared.Interfaces
{
    public interface IAppRules
    {
        float OccupantMinimumHorizontalPaddingInMeters { get; }
        float CabinetColumnWidthInMeters { get; }

        float? HighBarTopRiserDepthInMeters { get; }
        float? HighBarTopCountertopThicknessInMeters { get; }
        float? HighBarTopCountertopDepthInMeters { get; }
        bool ReappliesCountertopThicknessAfterLoad { get; }

        bool AppliesRuntimeProductOverrides { get; }
        float? FridgeHorizontalPaddingInMeters { get; }
        float? BottomAnchoredProductInsetInMeters { get; }
        bool AdjustsSmokerCutOutHeight { get; }

        PriceSheetExtras PriceSheetExtras { get; }
        string NonCatalogQuoteDisclaimer { get; }

        bool MeasurementFractionsFollowBrandCatalog { get; }
        bool ShowsTickerFractions { get; }
        bool RequiresBrandCatalog { get; }
        bool ShowsStorageInProductSearch { get; }
        bool ShowsPlungeCategory { get; }

        bool IncludesStollManufacturer { get; }
        bool IsManufacturerIdAvailable(int manufacturerId);
    }
}
