string B = "/Users/emmanuel/Documents/Unity/GeminiBaseline/";
string r = "design photo: before-cutouts / with-cutouts (mean per-channel difference to the old app, /255)\n";
double Diff(string a, string b)
{
    if (!System.IO.File.Exists(a) || !System.IO.File.Exists(b)) return -1;
    var ta = new UnityEngine.Texture2D(2, 2); ta.LoadImage(System.IO.File.ReadAllBytes(a));
    var tb = new UnityEngine.Texture2D(2, 2); tb.LoadImage(System.IO.File.ReadAllBytes(b));
    if (ta.width != tb.width || ta.height != tb.height) { UnityEngine.Object.DestroyImmediate(ta); UnityEngine.Object.DestroyImmediate(tb); return -2; }
    var pa = ta.GetPixels32(); var pb = tb.GetPixels32();
    double s = 0;
    for (int i = 0; i < pa.Length; i++) s += (System.Math.Abs(pa[i].r - pb[i].r) + System.Math.Abs(pa[i].g - pb[i].g) + System.Math.Abs(pa[i].b - pb[i].b)) / 3.0;
    UnityEngine.Object.DestroyImmediate(ta); UnityEngine.Object.DestroyImmediate(tb);
    return s / pa.Length;
}
foreach (var q in new[] { "27755", "32569", "28316", "27763", "27764", "27740", "44392", "36449" })
{
    foreach (var p in new[] { "photo_normalFront0", "photo_normalFront1", "photo_normalTopDown", "photo_planTopDown", "photo_cover" })
    {
        string old = B + "designs/" + q + "/photos/" + p + ".jpg";
        double before = Diff(old, B + "unity6-before-cutouts-2026-10-20/" + q + "/photos/" + p + ".jpg");
        double now = Diff(old, B + "unity6/" + q + "/photos/" + p + ".jpg");
        if (before < 0 && now < 0) continue;
        r += q + " " + p.Replace("photo_", "") + ": " + before.ToString("F2") + " / " + now.ToString("F2") + (now < before - 0.05 ? "  better" : now > before + 0.05 ? "  WORSE" : "") + "\n";
    }
}
return r;
