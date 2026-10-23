int checks = 0, mismatches = 0;
var sb = new System.Text.StringBuilder();
var methods = new KitchenDesign.Shared.Enums.KitchenBuildingMethod[] { (KitchenDesign.Shared.Enums.KitchenBuildingMethod)0, KitchenDesign.Shared.Enums.KitchenBuildingMethod.Standard, KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog };
void Check(string name, object expected, object actual, KitchenDesign.Shared.Enums.KitchenBuildingMethod m) { checks++; if (!expected.Equals(actual)) { mismatches++; sb.Append(m + "." + name + " expected " + expected + " got " + actual + "; "); } }
foreach (var m in methods)
{
    KitchenDesign.Shared.Interfaces.IBuildingMethodRules r = new KitchenDesign.BuildingRules.BuildingMethodRules(m);
    Check("UsesBrandCatalog", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog, r.UsesBrandCatalog, m);
    Check("CanToggleToeKicks", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog, r.CanToggleToeKicks, m);
    Check("FridgeCabinetsResizableOnlyWhenEmpty", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog, r.FridgeCabinetsResizableOnlyWhenEmpty, m);
    Check("AppliesMidRunEndPanelPadding", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog, r.AppliesMidRunEndPanelPadding, m);
    Check("HasBlindCorners", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog, r.HasBlindCorners, m);
    Check("ClonedSectionsCopyTheirCabinet", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog, r.ClonedSectionsCopyTheirCabinet, m);
    Check("HasCabinetColumns", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.HasCabinetColumns, m);
    Check("LimitsCabinetSizeToPreventBowing", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.LimitsCabinetSizeToPreventBowing, m);
    Check("LabelsCabinetsByProductName", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.LabelsCabinetsByProductName, m);
    Check("BackingRiserUsesCabinetMaterial", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.BackingRiserUsesCabinetMaterial, m);
    Check("BacksplashesHaveCountertop", m != KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.BacksplashesHaveCountertop, m);
    Check("UsesStollBuilderDistances", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.UsesStollBuilderDistances, m);
    Check("DefaultCountertopOverhangInMeters", (m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections ? (1.0f * 0.0254f) * 0.5f : 1.0f * 0.0254f), r.DefaultCountertopOverhangInMeters, m);
    Check("ShelvesFitWidthToProduct", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.ShelvesFitWidthToProduct, m);
    Check("BackingSpansTakeOnlyStandardHeightCabinets", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.BackingSpansTakeOnlyStandardHeightCabinets, m);
    Check("ReportsCabinetFinishLinearLength", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.ReportsCabinetFinishLinearLength, m);
    Check("PricesCabinetFinishByArea", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Standard || m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.PricesCabinetFinishByArea, m);
    Check("PadsCornersForAdjacentSpans", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Standard, r.PadsCornersForAdjacentSpans, m);
    Check("SizesStraightCabinetsToOutsideEdge", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Standard, r.SizesStraightCabinetsToOutsideEdge, m);
    Check("ReclickingProductSelectsItsVolume", m != KitchenDesign.Shared.Enums.KitchenBuildingMethod.Standard, r.ReclickingProductSelectsItsVolume, m);
    Check("CabinetsHaveTheirOwnDepth", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog || m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.CabinetsHaveTheirOwnDepth, m);
    Check("CabinetsAreProducts", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog || m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.CabinetsAreProducts, m);
    Check("CabinetProductIsOptional", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections, r.CabinetProductIsOptional, m);
    Check("ProductsCenteredInCabinets", m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Sections || m == KitchenDesign.Shared.Enums.KitchenBuildingMethod.Catalog, r.ProductsCenteredInCabinets, m);
}
return "checks=" + checks + " mismatches=" + mismatches + " " + sb;
