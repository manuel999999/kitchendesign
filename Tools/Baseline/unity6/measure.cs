double R(float v) { return System.Math.Round((double)v, 4); }
System.Collections.Generic.List<object> V(UnityEngine.Vector3 v) { return new System.Collections.Generic.List<object> { R(v.x), R(v.y), R(v.z) }; }
System.Collections.Generic.Dictionary<string, object> C(KitchenDesign.Cuboid c)
{
    UnityEngine.Vector3 s = c.max - c.min;
    return new System.Collections.Generic.Dictionary<string, object> {
        { "min", V(c.min) }, { "max", V(c.max) }, { "size", V(s) },
        { "size_in", new System.Collections.Generic.List<object> { R(s.x * 39.37008f), R(s.y * 39.37008f), R(s.z * 39.37008f) } } };
}
System.Collections.Generic.Dictionary<string, object> B(UnityEngine.Bounds b)
{
    return new System.Collections.Generic.Dictionary<string, object> { { "min", V(b.min) }, { "max", V(b.max) }, { "size", V(b.size) } };
}
System.Collections.Generic.Dictionary<string, object> T(UnityEngine.Transform t)
{
    return new System.Collections.Generic.Dictionary<string, object> { { "pos", V(t.position) }, { "euler", V(t.eulerAngles) }, { "scale", V(t.lossyScale) } };
}
object RendererBounds(UnityEngine.Renderer[] rs)
{
    bool any = false; UnityEngine.Bounds b = default;
    foreach (var r in rs) { if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
    return any ? (object)B(b) : null;
}
object DrawnBoundsIn(UnityEngine.Transform space, UnityEngine.Renderer[] rs)
{
    bool any = false; UnityEngine.Bounds b = default;
    foreach (var r in rs)
    {
        if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
        var mf = r.GetComponent<UnityEngine.MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
        UnityEngine.Bounds mb = mf.sharedMesh.bounds;
        for (int ci = 0; ci < 8; ci++)
        {
            UnityEngine.Vector3 v = new UnityEngine.Vector3((ci & 1) == 0 ? mb.min.x : mb.max.x, (ci & 2) == 0 ? mb.min.y : mb.max.y, (ci & 4) == 0 ? mb.min.z : mb.max.z);
            UnityEngine.Vector3 p = space.InverseTransformPoint(r.transform.TransformPoint(v));
            if (!any) { b = new UnityEngine.Bounds(p, UnityEngine.Vector3.zero); any = true; } else b.Encapsulate(p);
        }
    }
    return any ? (object)B(b) : null;
}

string outDir = "/Users/emmanuel/Documents/Unity/GeminiBaseline/unity6/36449";
var k = KitchenDesign.Kitchen.instance;
var layout = k.layout;
var root = new System.Collections.Generic.Dictionary<string, object>();
root["quote_id"] = 36449;
root["layout_object"] = layout.name;
root["ground"] = T(k.ground.transform);

var spans = new System.Collections.Generic.List<object>();
foreach (var span in layout.spans)
{
    var s = new System.Collections.Generic.Dictionary<string, object>();
    s["index"] = span.index;
    UnityEngine.Transform frame = span.orientationAlignedCabinet != null ? span.orientationAlignedCabinet.GetTransform() : null;
    s["frame"] = frame != null ? (object)T(frame) : null;
    s["outside"] = C(span.GetOutsideCuboidInSpanSpace(includeCountertop: false));
    s["outside_with_countertop"] = C(span.GetOutsideCuboidInSpanSpace(includeCountertop: true));
    var ci2 = new System.Collections.Generic.List<object>(); foreach (var e in span.cabinets) ci2.Add(e.index); s["cabinet_indices"] = ci2;
    var bk = span.backing;
    if (bk != null)
    {
        var rs = bk.GetComponentsInChildren<UnityEngine.Renderer>(false);
        var bd = new System.Collections.Generic.Dictionary<string, object>();
        bd["object"] = bk.name;
        try { bd["sku"] = bk.info != null ? bk.info.sku : null; } catch (System.Exception ex) { bd["sku_error"] = ex.Message; }
        try { bd["riser"] = C(bk.riserCuboid); } catch (System.Exception ex) { bd["riser_error"] = ex.Message; }
        try { bd["countertop"] = C(bk.GetCountertopCuboidInSpanSpace(default)); } catch (System.Exception ex) { bd["countertop_error"] = ex.Message; }
        try { bd["drawn_in_span_space"] = frame != null ? DrawnBoundsIn(frame, rs) : null; } catch (System.Exception ex) { bd["drawn_error"] = ex.Message; }
        bd["render_bounds"] = RendererBounds(rs);
        s["backing"] = bd;
    }
    spans.Add(s);
}
root["spans"] = spans;

var cabs = new System.Collections.Generic.List<object>();
foreach (var e in layout.EnumerateCabinets())
{
    var c = e.element; if (c == null) continue;
    var t = c.GetTransform();
    cabs.Add(new System.Collections.Generic.Dictionary<string, object> {
        { "index", e.index },
        { "object", c.name },
        { "type", c.cabinetType.ToString() },
        { "visual", c.cabinetSectionVisual != null ? c.cabinetSectionVisual.name : null },
        { "sku", c.cabinetSectionProductInfo != null ? c.cabinetSectionProductInfo.sku : null },
        { "transform", T(t) },
        { "size", C(c.resizerCuboid) },
        { "layout_outside", C(layout.GetCabinetOutsideCuboid(c)) },
        { "countertop", C(c.countertopCuboid) },
        { "has_selected_end_panel", c.hasSelectedEndPanel },
        { "end_panel_on_ground", c.isEndPanelOnGround },
        { "active", t.gameObject.activeInHierarchy },
        { "render_bounds", RendererBounds(t.GetComponentsInChildren<UnityEngine.Renderer>(false)) } });
}
root["cabinets"] = cabs;

var occList = new System.Collections.Generic.List<KitchenDesign.Occupant>(UnityEngine.Object.FindObjectsByType<KitchenDesign.Occupant>(UnityEngine.FindObjectsSortMode.None));
occList.Sort((a, b) => { int cx = a.transform.position.x.CompareTo(b.transform.position.x); return cx != 0 ? cx : a.transform.position.z.CompareTo(b.transform.position.z); });
var occs = new System.Collections.Generic.List<object>();
foreach (var o in occList)
{
    occs.Add(new System.Collections.Generic.Dictionary<string, object> {
        { "sku", o.productInfo != null ? o.productInfo.sku : null },
        { "visual", o.productVisual != null ? o.productVisual.name : null },
        { "transform", T(o.transform) },
        { "render_bounds", RendererBounds(o.GetComponentsInChildren<UnityEngine.Renderer>(false)) } });
}
root["occupants"] = occs;

var cams = new System.Collections.Generic.List<object>();
var camList = new System.Collections.Generic.List<UnityEngine.Camera>(UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None));
camList.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
foreach (var cam in camList)
{
    if (cam.name.StartsWith("EditorThumbnail")) continue;
    var d = T(cam.transform); d["name"] = cam.name; d["fov"] = R(cam.fieldOfView); d["ortho"] = cam.orthographic; d["ortho_size"] = R(cam.orthographicSize); d["enabled"] = cam.isActiveAndEnabled;
    cams.Add(d);
}
var vcList = new System.Collections.Generic.List<Unity.Cinemachine.CinemachineVirtualCameraBase>(UnityEngine.Object.FindObjectsByType<Unity.Cinemachine.CinemachineVirtualCameraBase>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None));
vcList.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
foreach (var vc in vcList)
{
    var d = T(vc.transform); d["name"] = "vcam:" + vc.name; d["priority"] = vc.Priority.Value; d["active"] = vc.isActiveAndEnabled;
    cams.Add(d);
}
root["cameras"] = cams;
root["renderer_count"] = k.GetComponentsInChildren<UnityEngine.Renderer>(false).Length;

System.IO.Directory.CreateDirectory(outDir);
System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "measured.json"), MiniJSON.Json.Serialize(root));
System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "resave.json"), MiniJSON.Json.Serialize(k.SaveToJson()));
return "spans=" + spans.Count + " cabinets=" + cabs.Count + " occupants=" + occs.Count + " cameras=" + cams.Count;
