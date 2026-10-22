var sb = new System.Text.StringBuilder();
float inch = 0.0254f;
string[] names = { "AppOptions_ProFit", "AppOptions_Stoll", "AppOptions_Gemini" };
int bad = 0, checks = 0;
foreach (var n in names)
{
    var o = UnityEditor.AssetDatabase.LoadAssetAtPath<KitchenDesign.AppOptions>("Assets/AssetsMelcher/AppOptions/" + n + ".asset");
    var f = o.userFilter;
    bool stoll = f == KitchenDesign.UserFilter.Stoll, gemini = f == KitchenDesign.UserFilter.Gemini, profit = f == KitchenDesign.UserFilter.ProFit;
    var r = KitchenDesign.AppRules.AppRulesFactory.Create(f);
    var exp = new System.Collections.Generic.List<(string, object, object)>
    {
        ("pad", stoll ? 0f : 5f * inch, r.OccupantMinimumHorizontalPaddingInMeters),
        ("col", stoll ? 6f * inch : 3f * inch, r.CabinetColumnWidthInMeters),
        ("riser", stoll ? (float?)(3f * inch) : null, r.HighBarTopRiserDepthInMeters),
        ("thick", stoll ? (float?)(2f * inch) : null, r.HighBarTopCountertopThicknessInMeters),
        ("depth", stoll ? (float?)(12f * inch) : null, r.HighBarTopCountertopDepthInMeters),
        ("reapply", stoll, r.ReappliesCountertopThicknessAfterLoad),
        ("overrides", stoll, r.AppliesRuntimeProductOverrides),
        ("fridge", stoll ? (float?)0f : null, r.FridgeHorizontalPaddingInMeters),
        ("inset", stoll ? (float?)(4f * inch) : null, r.BottomAnchoredProductInsetInMeters),
        ("smoker", stoll, r.AdjustsSmokerCutOutHeight),
        ("price", stoll ? KitchenDesign.Shared.Enums.PriceSheetExtras.StollCustomKitchen : gemini ? KitchenDesign.Shared.Enums.PriceSheetExtras.GeminiCatalog : KitchenDesign.Shared.Enums.PriceSheetExtras.None, r.PriceSheetExtras),
        ("disclaimer", stoll ? "Countertop and finishes not included on this final quote." : null, r.NonCatalogQuoteDisclaimer),
        ("measFrac", gemini, r.MeasurementFractionsFollowBrandCatalog),
        ("ticker", gemini, r.ShowsTickerFractions),
        ("catalog", gemini, r.RequiresBrandCatalog),
        ("storage", !gemini, r.ShowsStorageInProductSearch),
        ("plunge", stoll, r.ShowsPlungeCategory),
        ("stollMfr", stoll, r.IncludesStollManufacturer),
        ("id137", profit, r.IsManufacturerIdAvailable(137)),
        ("id30", true, r.IsManufacturerIdAvailable(30)),
    };
    sb.Append(n + "(" + f + "): ");
    foreach (var (k, a, b) in exp)
    {
        checks++;
        bool eq = a is float fa && b is float fb ? UnityEngine.Mathf.Approximately(fa, fb) : object.Equals(a, b);
        if (!eq) { bad++; sb.Append(k + " old=" + a + " new=" + b + "; "); }
    }
    sb.Append("| ");
}
return "checks=" + checks + " mismatches=" + bad + " " + sb;
