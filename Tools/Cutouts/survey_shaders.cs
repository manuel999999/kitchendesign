var byShader = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>>();
void Add(UnityEngine.Material m, string source)
{
    if (m == null) return;
    string key = m.shader == null ? "null" : m.shader.name;
    if (!byShader.TryGetValue(key, out var set)) byShader[key] = set = new System.Collections.Generic.HashSet<string>();
    set.Add(source + ":" + m.name);
}
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:AppSkin"))
{
    var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Melcher.AppSkin>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
    Add(s.primarySelectionMaterial, "skin " + s.name + " primary");
    Add(s.secondarySelectionMaterial, "skin " + s.name + " secondary");
}
Add(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/AssetsMelcher/Materials/Kitchen_Default.mat"), "default");
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:BrandCatalog"))
{
    var c = UnityEditor.AssetDatabase.LoadAssetAtPath<Melcher.BrandCatalog>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
    if (c != null) Add(c.defaultCabinetFinishOverride, "catalog override");
}
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { "Assets/AssetsMelcher/Finishes" }))
{
    var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var n = System.IO.Path.GetFileName(p);
    if (n.StartsWith("Cabinet_") || n.StartsWith("Countertop_")) Add(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(p), "finish");
}
int sections = 0;
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/AssetsMelcher/ProductVisuals" }))
{
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
    var pvc = go == null ? null : go.GetComponent<Melcher.ProductVisualContent>();
    if (pvc == null) continue;
    Add(pvc.sectionMaterialOverride, "sectionOverride");
    if (pvc.sectionModelPrefab != null)
    {
        sections++;
        foreach (var r in pvc.sectionModelPrefab.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
            foreach (var m in r.sharedMaterials) Add(m, "authored");
    }
}
string res = "authored section prefabs=" + sections + "\n";
foreach (var kv in byShader)
{
    var list = new System.Collections.Generic.List<string>(kv.Value);
    var counts = new System.Collections.Generic.Dictionary<string, int>();
    foreach (var e in list) { var src = e.Substring(0, e.IndexOf(':')); counts[src] = counts.TryGetValue(src, out int c) ? c + 1 : 1; }
    res += kv.Key + " : " + string.Join(", ", System.Linq.Enumerable.Select(counts, x => x.Key + "=" + x.Value));
    if (list.Count <= 6) res += "  [" + string.Join("; ", list) + "]";
    res += "\n";
}
return res;
