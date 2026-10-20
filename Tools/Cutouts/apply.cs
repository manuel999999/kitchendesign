var k = Melcher.Kitchen.instance;
if (k == null || k.layout == null) return "no kitchen";
var set = new KitchenDesign.Cutouts.CutoutClipSet();
var extraField = typeof(Melcher.ProductVisualContent).GetField("m_AdditionalCabinetRenderMaskMeshes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
int slot = 0;
int renderers = 0;
string log = "";

void SetSlot(UnityEngine.Renderer r, int s)
{
    var mr = r as UnityEngine.MeshRenderer;
    if (mr == null) return;
    KitchenDesign.Cutouts.CutoutClipSet.SetRendererSlot(mr, s);
    renderers++;
}

void AddCutOut(Melcher.CabinetCutOut c, int s, string label)
{
    if (c == null) return;
    foreach (var r in c.interiorRoot.GetComponentsInChildren<UnityEngine.MeshRenderer>(true)) SetSlot(r, s);
    if (!c.gameObject.activeInHierarchy) return;
    var root = c.stencilRoot;
    bool has = false;
    var b = new UnityEngine.Bounds();
    foreach (var mf in root.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
    {
        if (mf.sharedMesh == null) continue;
        var m = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
        var mb = mf.sharedMesh.bounds;
        for (int i = 0; i < 8; i++)
        {
            var corner = new UnityEngine.Vector3((i & 1) == 0 ? mb.min.x : mb.max.x, (i & 2) == 0 ? mb.min.y : mb.max.y, (i & 4) == 0 ? mb.min.z : mb.max.z);
            var p = m.MultiplyPoint3x4(corner);
            if (!has) { b = new UnityEngine.Bounds(p, UnityEngine.Vector3.zero); has = true; } else b.Encapsulate(p);
        }
    }
    if (!has) return;
    var open = KitchenDesign.Shared.Enums.CutoutFaces.None;
    for (int f = 0; f < 6; f++)
    {
        var face = (Melcher.CabinetCutOut.CubeFace)f;
        bool window = c.stencilRoot.GetChild(f).gameObject.activeSelf;
        bool shared = c.isCountertop ? face == Melcher.CabinetCutOut.CubeFace.YMin : (face == Melcher.CabinetCutOut.CubeFace.YMax && !c.GetInteriorFace(face).activeSelf);
        if (window || shared) open |= (KitchenDesign.Shared.Enums.CutoutFaces)(1 << f);
    }
    bool added = set.AddBox(s, root, b, open);
    var size = UnityEngine.Vector3.Scale(b.size, root.lossyScale);
    log += "\n  slot " + s + " " + label + (c.isCountertop ? " countertop" : " cabinet") + " size=" + size.ToString("F3") + " open=" + open + (added ? "" : " DROPPED");
}

void AddOccupant(Melcher.Occupant occ, int s, string prefix)
{
    var pvc = occ.productVisualContentInstance;
    if (pvc == null) return;
    var extra = extraField.GetValue(pvc) as System.Collections.IEnumerable;
    if (extra != null) foreach (var r in extra) SetSlot(r as UnityEngine.Renderer, s);
    AddCutOut(pvc.cabinetCutOut, s, prefix + occ.name);
    AddCutOut(pvc.countertopCutOut, s, prefix + occ.name);
    if (occ.TryGetComponent(out Melcher.CabinetShelf shelf))
    {
        foreach (var nested in shelf.occupants) AddOccupant(nested, s, prefix + occ.name + "/");
    }
}

foreach (var entry in k.layout.EnumerateCabinets())
{
    var cab = entry.element;
    slot++;
    foreach (var r in cab.meshRenderers) SetSlot(r, slot);
    foreach (var occ in cab.occupants) AddOccupant(occ, slot, "");
}
set.Apply();
return "cabinets=" + slot + " renderers=" + renderers + " boxes=" + set.BoxCount + " dropped=" + set.DroppedBoxCount + log;
