var k = KitchenDesign.Kitchen.instance;
string r = "";
var shaders = new System.Collections.Generic.Dictionary<string, int>();
int ci = 0;
foreach (var entry in k.layout.EnumerateCabinets())
{
    ci++;
    var cab = entry.element;
    foreach (var mr in cab.meshRenderers)
    {
        foreach (var m in mr.sharedMaterials)
        {
            string key = m == null ? "null" : m.shader.name + " (" + m.name + ")";
            shaders[key] = shaders.TryGetValue(key, out int n) ? n + 1 : 1;
        }
    }
    foreach (var occ in cab.occupants)
    {
        var pvc = occ.productVisualContentInstance;
        foreach (var mr in pvc.cabinetCutOut.interiorRoot.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
        {
            r += "interior " + mr.name + " layer=" + UnityEngine.LayerMask.LayerToName(mr.gameObject.layer) + " active=" + mr.gameObject.activeInHierarchy + " shader=" + (mr.sharedMaterial ? mr.sharedMaterial.shader.name : "null") + " user=" + mr.GetShaderUserValue() + "\n";
        }
        r += "cab " + ci + " occupant " + occ.name + " cutoutActive=" + pvc.cabinetCutOut.gameObject.activeInHierarchy + " pos=" + cab.GetTransform().position.ToString("F2") + " fwd=" + cab.GetTransform().forward.ToString("F2") + "\n";
    }
}
foreach (var kv in shaders) r += kv.Value + " x " + kv.Key + "\n";
return r;
