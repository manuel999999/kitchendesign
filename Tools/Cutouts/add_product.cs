string r = "";
foreach (var sku in new[] { __SKUS__ })
{
    var info = KitchenDesign.ProFitGlobals.FindRuntimeProductBySku(sku, false);
    if (info == null) { r += sku + ": not in this app's products\n"; continue; }
    UnityEngine.Object.FindFirstObjectByType<KitchenDesign.KitchenModel.Kitchen>().TryAddProduct(info, null);
    r += sku + ": requested (" + info.name + ", visual " + info.visualId + ")\n";
}
return r;
