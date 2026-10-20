var k = Melcher.Kitchen.instance;
Melcher.Occupant sink = null;
foreach (var entry in k.layout.EnumerateCabinets()) foreach (var occ in entry.element.occupants) if (occ.name.Contains("__NAME__")) sink = occ;
var c = sink.productVisualContentInstance.countertopCutOut.stencilRoot;
var b = new UnityEngine.Bounds(c.position, UnityEngine.Vector3.zero);
foreach (var r in c.GetComponentsInChildren<UnityEngine.Renderer>(true)) b.Encapsulate(r.bounds);
var p = b.center;
string s = "probe " + p.ToString("F3") + " cutout bounds " + b.size.ToString("F3") + "\n";
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
    var rb = r.bounds;
    if (p.x < rb.min.x || p.x > rb.max.x || p.z < rb.min.z || p.z > rb.max.z) continue;
    if (rb.max.y < p.y - 0.05f) continue;
    string path = r.name; var t = r.transform.parent; for (int i = 0; i < 3 && t != null; i++, t = t.parent) path = t.name + "/" + path;
    s += path + " layer=" + UnityEngine.LayerMask.LayerToName(r.gameObject.layer) + " user=" + r.GetShaderUserValue() + " shader=" + (r.sharedMaterial ? r.sharedMaterial.shader.name : "null") + " ymax=" + rb.max.y.ToString("F3") + "\n";
}
return s;
