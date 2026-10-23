using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign.BuildingRules
{
    public sealed class BuildingMethodRules : IBuildingMethodRules
    {
        private readonly KitchenBuildingMethod _method;

        public BuildingMethodRules(KitchenBuildingMethod method)
        {
            this._method = method;
        }

        public KitchenBuildingMethod Method => this._method;

        public bool UsesBrandCatalog => this._method == KitchenBuildingMethod.Catalog;

        public bool CanToggleToeKicks => this._method == KitchenBuildingMethod.Catalog;

        public bool FridgeCabinetsResizableOnlyWhenEmpty => this._method == KitchenBuildingMethod.Catalog;

        public bool AppliesMidRunEndPanelPadding => this._method == KitchenBuildingMethod.Catalog;

        public bool HasBlindCorners => this._method == KitchenBuildingMethod.Catalog;

        public bool ClonedSectionsCopyTheirCabinet => this._method == KitchenBuildingMethod.Catalog;

        public bool HasCabinetColumns => this._method == KitchenBuildingMethod.Sections;

        public bool LimitsCabinetSizeToPreventBowing => this._method == KitchenBuildingMethod.Sections;

        public bool LabelsCabinetsByProductName => this._method == KitchenBuildingMethod.Sections;

        public bool BackingRiserUsesCabinetMaterial => this._method == KitchenBuildingMethod.Sections;

        public bool BacksplashesHaveCountertop => this._method != KitchenBuildingMethod.Sections;

        public bool UsesStollBuilderDistances => this._method == KitchenBuildingMethod.Sections;

        public float DefaultCountertopOverhangInMeters => 1.0f * Units.Inches2Meters * (this._method == KitchenBuildingMethod.Sections ? 0.5f : 1.0f);

        public bool ShelvesFitWidthToProduct => this._method == KitchenBuildingMethod.Sections;

        public bool BackingSpansTakeOnlyStandardHeightCabinets => this._method == KitchenBuildingMethod.Sections;

        public bool ReportsCabinetFinishLinearLength => this._method == KitchenBuildingMethod.Sections;

        public bool PricesCabinetFinishByArea => this._method == KitchenBuildingMethod.Standard || this._method == KitchenBuildingMethod.Sections;

        public bool PadsCornersForAdjacentSpans => this._method == KitchenBuildingMethod.Standard;

        public bool SizesStraightCabinetsToOutsideEdge => this._method == KitchenBuildingMethod.Standard;

        public bool ReclickingProductSelectsItsVolume => this._method != KitchenBuildingMethod.Standard;

        public bool CabinetsHaveTheirOwnDepth => this._method == KitchenBuildingMethod.Catalog || this._method == KitchenBuildingMethod.Sections;

        public bool CabinetsAreProducts => this._method == KitchenBuildingMethod.Catalog || this._method == KitchenBuildingMethod.Sections;

        public bool CabinetProductIsOptional => this._method == KitchenBuildingMethod.Sections;

        public bool ProductsCenteredInCabinets => this._method == KitchenBuildingMethod.Sections || this._method == KitchenBuildingMethod.Catalog;
    }
}
