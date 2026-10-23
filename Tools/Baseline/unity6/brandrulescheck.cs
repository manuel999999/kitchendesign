int checks = 0, mismatches = 0;
var sb = new System.Text.StringBuilder();
var ids = new System.Collections.Generic.SortedSet<int> { 0, 92, 95, 100, 109, 112, 114, 134, 141 };
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:BrandCatalogSO"))
{
    var c = UnityEditor.AssetDatabase.LoadAssetAtPath<KitchenDesign.Scriptables.BrandCatalogSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (c != null) ids.Add(c.cabinetManufacturerId);
}
void Check(string name, object expected, object actual, int id) { checks++; if (!object.Equals(expected, actual)) { mismatches++; sb.Append(id + "." + name + " expected " + expected + " got " + actual + "; "); } }
float? OldPadding(int id, bool isSmoker)
{
    if (id == 109) { return isSmoker ? 0f : 2.25f * 0.0254f; }
    else if (id == 114) { return 3f * 0.0254f; }
    else if (id == 100) { return 3f * 0.0254f; }
    else if (id == 92) { return 3f * 0.0254f; }
    else if (id == 141) { return 3f * 0.0254f; }
    else if (id == 134) { return 3f * 0.0254f; }
    return null;
}
foreach (int id in ids)
{
    var r = new KitchenDesign.Brands.BrandRules(id);
    Check("PricesToeKicks", id != 109, r.PricesToeKicks, id);
    Check("PricesFridgeBackPanels", System.Linq.Enumerable.Contains(new int[] { 112, 100 }, id) == false, r.PricesFridgeBackPanels, id);
    Check("PricesFridgeEndPanels", !(id == 109 || id == 114), r.PricesFridgeEndPanels, id);
    Check("PricesCountertopsFromCatalog", id == 109, r.PricesCountertopsFromCatalog, id);
    Check("PricesCornersAsOwnProduct", id == 109, r.PricesCornersAsOwnProduct, id);
    Check("ForcesKamadoGrillEndPanels", id == 109, r.ForcesKamadoGrillEndPanels, id);
    Check("FridgePanelSkuIncludesCabinetSize", id == 109, r.FridgePanelSkuIncludesCabinetSize, id);
    Check("PricesAmericanOutdoorBaseEnds", id == 112, r.PricesAmericanOutdoorBaseEnds, id);
    Check("PricesBaseEnds", !(id == 100 || id == 109), r.PricesBaseEnds, id);
    Check("UsesEnduraToeKick", id == 100, r.UsesEnduraToeKick, id);
    Check("Padding(smoker)", OldPadding(id, true), r.HeatedOrBurnerProductSidePaddingInMeters(true), id);
    Check("Padding(other)", OldPadding(id, false), r.HeatedOrBurnerProductSidePaddingInMeters(false), id);
}
Check("ChallengerPadding", 4.0f * 0.0254f, KitchenDesign.Brands.BrandRules.ChallengerOccupantMinimumHorizontalPaddingInMeters, 0);
return "ids=" + string.Join(",", ids) + " checks=" + checks + " mismatches=" + mismatches + " " + sb;
