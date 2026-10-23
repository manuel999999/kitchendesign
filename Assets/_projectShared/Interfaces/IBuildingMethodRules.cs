namespace KitchenDesign.Shared.Interfaces
{
    public interface IBuildingMethodRules
    {
        bool UsesBrandCatalog { get; }
        bool CanToggleToeKicks { get; }
        bool FridgeCabinetsResizableOnlyWhenEmpty { get; }
        bool AppliesMidRunEndPanelPadding { get; }
        bool HasBlindCorners { get; }
        bool ClonedSectionsCopyTheirCabinet { get; }
        bool HasCabinetColumns { get; }
        bool LimitsCabinetSizeToPreventBowing { get; }
        bool LabelsCabinetsByProductName { get; }
        bool BackingRiserUsesCabinetMaterial { get; }
        bool BacksplashesHaveCountertop { get; }
        bool UsesStollBuilderDistances { get; }
        float DefaultCountertopOverhangInMeters { get; }
        bool ShelvesFitWidthToProduct { get; }
        bool BackingSpansTakeOnlyStandardHeightCabinets { get; }
        bool ReportsCabinetFinishLinearLength { get; }
        bool PricesCabinetFinishByArea { get; }
        bool PadsCornersForAdjacentSpans { get; }
        bool SizesStraightCabinetsToOutsideEdge { get; }
        bool ReclickingProductSelectsItsVolume { get; }
        bool CabinetsHaveTheirOwnDepth { get; }
        bool CabinetsAreProducts { get; }
        bool CabinetProductIsOptional { get; }
        bool ProductsCenteredInCabinets { get; }
    }
}
