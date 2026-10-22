var k = KitchenDesign.Kitchen.instance;
KitchenDesign.CabinetShelf shelf = null;
foreach (var entry in k.layout.EnumerateCabinets()) foreach (var occ in entry.element.occupants) if (shelf == null && occ.TryGetComponent(out KitchenDesign.CabinetShelf s)) shelf = s;
if (shelf == null) return "no shelf in the kitchen";
var info = KitchenDesign.ProFitGlobals.FindRuntimeProductBySku("__SKU__", false);
if (info == null) return "__SKU__ not in this app's products";
var dbField = typeof(KitchenDesign.Kitchen).GetField("m_ProductVisualDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var db = (KitchenDesign.Scriptables.ProductVisualDatabaseSO)dbField.GetValue(k);
var visual = db.FindRuntimeVisualByID(info.visualId);
if (visual == null) return "no visual " + info.visualId;
async Cysharp.Threading.Tasks.UniTaskVoid Run()
{
    var prefab = await KitchenDesign.ProFitGlobals.productContent.LoadContent(visual);
    var result = await shelf.TryInstantiateOccupantAnywhere(info, visual, prefab, k.InstantiateOccupant, null);
    UnityEngine.Debug.Log("[CutoutProto] into shelf: " + (result.newOccupant ? result.newOccupant.name : "failed " + result.result.status));
}
Run().Forget();
return "requested " + info.name + " into " + shelf.name;
