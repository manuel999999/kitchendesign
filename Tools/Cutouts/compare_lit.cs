var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>("Assets/_projectAssets/Shaders/KitchenLitGraph.shadergraph");
var root = new UnityEngine.GameObject("LitCompare");
root.transform.position = new UnityEngine.Vector3(500f, 500f, 500f);
var lightGo = new UnityEngine.GameObject("L");
lightGo.transform.SetParent(root.transform, false);
lightGo.transform.rotation = UnityEngine.Quaternion.Euler(40f, -30f, 0f);
var light = lightGo.AddComponent<UnityEngine.Light>();
light.type = UnityEngine.LightType.Directional;
light.intensity = 1.2f;
var sphere = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Sphere);
sphere.transform.SetParent(root.transform, false);
sphere.transform.localPosition = new UnityEngine.Vector3(-0.6f, 0f, 0f);
var cube = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
cube.transform.SetParent(root.transform, false);
cube.transform.localPosition = new UnityEngine.Vector3(0.6f, 0f, 0f);
cube.transform.localRotation = UnityEngine.Quaternion.Euler(20f, 35f, 0f);
var camGo = new UnityEngine.GameObject("C");
camGo.transform.SetParent(root.transform, false);
camGo.transform.localPosition = new UnityEngine.Vector3(0f, 0.3f, -3f);
camGo.transform.LookAt(root.transform.position);
var cam = camGo.AddComponent<UnityEngine.Camera>();
cam.enabled = false;
cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
cam.backgroundColor = UnityEngine.Color.black;
cam.fieldOfView = 35f;
UnityEngine.Color32[] Render(UnityEngine.Material m)
{
    sphere.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = m;
    cube.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = m;
    var rt = new UnityEngine.RenderTexture(400, 300, 24, UnityEngine.RenderTextureFormat.ARGB32);
    cam.targetTexture = rt;
    cam.Render();
    cam.targetTexture = null;
    var prev = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(400, 300, UnityEngine.TextureFormat.RGBA32, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 400, 300), 0, 0);
    tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    var px = tex.GetPixels32();
    UnityEngine.Object.DestroyImmediate(tex);
    rt.Release();
    UnityEngine.Object.DestroyImmediate(rt);
    return px;
}
string r = "";
foreach (var path in new[] { __PATHS__ })
{
    var orig = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    var twin = new UnityEngine.Material(shader);
    foreach (var t in new[] { "_BaseMap", "_BumpMap" }) if (orig.HasProperty(t)) { twin.SetTexture(t, orig.GetTexture(t)); twin.SetTextureScale(t, orig.GetTextureScale(t)); twin.SetTextureOffset(t, orig.GetTextureOffset(t)); }
    foreach (var f in new[] { "_Metallic", "_Smoothness", "_BumpScale", "_OcclusionStrength" }) if (orig.HasProperty(f)) twin.SetFloat(f, orig.GetFloat(f));
    twin.SetColor("_BaseColor", orig.GetColor("_BaseColor"));
    var a = Render(orig);
    var b = Render(twin);
    double sum = 0; int max = 0; int n = 0;
    for (int i = 0; i < a.Length; i++)
    {
        if (a[i].r == 0 && a[i].g == 0 && a[i].b == 0 && b[i].r == 0 && b[i].g == 0 && b[i].b == 0) continue;
        int d = System.Math.Max(System.Math.Abs(a[i].r - b[i].r), System.Math.Max(System.Math.Abs(a[i].g - b[i].g), System.Math.Abs(a[i].b - b[i].b)));
        sum += d; if (d > max) max = d; n++;
    }
    r += System.IO.Path.GetFileNameWithoutExtension(path) + " (" + orig.shader.name + "): pixels=" + n + " mean diff=" + (sum / System.Math.Max(n, 1)).ToString("F2") + "/255 max=" + max + "\n";
    UnityEngine.Object.DestroyImmediate(twin);
}
UnityEngine.Object.DestroyImmediate(root);
return r;
