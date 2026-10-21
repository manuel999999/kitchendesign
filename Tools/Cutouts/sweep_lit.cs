var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>("Assets/_projectAssets/Shaders/KitchenLitGraph.shadergraph");
var old = UnityEngine.GameObject.Find("LitCompare"); while (old != null) { UnityEngine.Object.DestroyImmediate(old); old = UnityEngine.GameObject.Find("LitCompare"); }
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
double Diff(UnityEngine.Color32[] a, UnityEngine.Color32[] b, out int max, out int n)
{
    double sum = 0; max = 0; n = 0;
    for (int i = 0; i < a.Length; i++)
    {
        if (a[i].r == 0 && a[i].g == 0 && a[i].b == 0 && b[i].r == 0 && b[i].g == 0 && b[i].b == 0) continue;
        int d = System.Math.Max(System.Math.Abs(a[i].r - b[i].r), System.Math.Max(System.Math.Abs(a[i].g - b[i].g), System.Math.Abs(a[i].b - b[i].b)));
        sum += d; if (d > max) max = d; n++;
    }
    return sum / System.Math.Max(n, 1);
}
foreach (var path in new[] { __PATHS__ })
{
    var orig = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    var twin = new UnityEngine.Material(shader);
    foreach (var t in new[] { "_BaseMap", "_BumpMap" }) if (orig.HasProperty(t) && orig.GetTexture(t) != null) { twin.SetTexture(t, orig.GetTexture(t)); twin.SetTextureScale(t, orig.GetTextureScale(t)); twin.SetTextureOffset(t, orig.GetTextureOffset(t)); }
    foreach (var f in new[] { "_Metallic", "_BumpScale", "_OcclusionStrength" }) if (orig.HasProperty(f)) twin.SetFloat(f, orig.GetFloat(f));
    twin.SetColor("_BaseColor", orig.GetColor("_BaseColor"));
    var a = Render(orig);
    float s0 = orig.GetFloat("_Smoothness");
    int mx, n;
    twin.SetFloat("_Smoothness", s0);
    double d0 = Diff(a, Render(twin), out mx, out n);
    r += System.IO.Path.GetFileNameWithoutExtension(path) + " (" + orig.shader.name + ") same smoothness " + s0.ToString("F3") + ": mean " + d0.ToString("F2") + " max " + mx + " px " + n + "\n  sweep:";
    double best = 1e9; float bestS = s0; int bestMax = 0;
    for (int k = 0; k <= 10; k++)
    {
        float s = k * 0.1f; twin.SetFloat("_Smoothness", s);
        double d = Diff(a, Render(twin), out mx, out n);
        r += " " + s.ToString("F2") + "=" + d.ToString("F2") + "/" + mx;
        if (d < best) { best = d; bestS = s; bestMax = mx; }
    }
    r += "\n  best " + bestS.ToString("F2") + ": mean " + best.ToString("F2") + " max " + bestMax + "\n";
    UnityEngine.Object.DestroyImmediate(twin);
}
UnityEngine.Object.DestroyImmediate(root);
return r;
