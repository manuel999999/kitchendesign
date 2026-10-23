var go = UnityEngine.GameObject.Find("CutoutTestCam");
if (go == null)
{
    go = new UnityEngine.GameObject("CutoutTestCam");
    var c = go.AddComponent<UnityEngine.Camera>();
    c.CopyFrom(UnityEngine.Camera.main);
    c.enabled = false;
}
foreach (var entry in KitchenDesign.KitchenModel.Kitchen.instance.layout.EnumerateCabinets())
    foreach (var occ in entry.element.occupants)
    {
        occ.productVisualContentInstance.SetProductVisibility(__SHOW__);
        if (occ.TryGetComponent(out KitchenDesign.Cabinets.CabinetShelf shelf)) foreach (var nested in shelf.occupants) nested.productVisualContentInstance.SetProductVisibility(__SHOW__);
    }
return "camera ready, products shown=" + __SHOW__;
