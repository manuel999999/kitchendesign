string outPath = "/Users/emmanuel/Documents/Unity/GeminiBaseline/listenerorder-latest.txt";
var lines = new System.Collections.Generic.List<string>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
string Name(System.Delegate d)
{
    var m = d.Method;
    string type = m.DeclaringType == null ? "?" : m.DeclaringType.FullName;
    type = System.Text.RegularExpressions.Regex.Replace(type, @"\+<>c(__DisplayClass\d+_\d+)?", "");
    string method = System.Text.RegularExpressions.Regex.Replace(m.Name, @"^<(\w+)>b__\d+_\d+$", "<lambda in $1>");
    method = System.Text.RegularExpressions.Regex.Replace(method, @"^<(\w+)>g__(\w+)\|\d+_\d+$", "$1.$2");
    return type.Replace("KitchenDesign.", "") + "." + method;
}
System.Collections.Generic.IEnumerable<System.Delegate> Subscribers(object value)
{
    if (value == null) yield break;
    if (value is System.Delegate d)
    {
        foreach (var x in d.GetInvocationList()) yield return x;
        yield break;
    }
    var f = value.GetType().GetField("m_Subscribers", bf);
    if (f != null)
    {
        foreach (var x in (System.Collections.IEnumerable)f.GetValue(value)) yield return (System.Delegate)x;
        yield break;
    }
    foreach (var name in new[] { "Event", "_event" })
    {
        var ef = value.GetType().GetField(name, bf);
        if (ef != null)
        {
            var ev = ef.GetValue(value) as System.Delegate;
            if (ev != null) foreach (var x in ev.GetInvocationList()) yield return x;
            yield break;
        }
    }
}
void Dump(string group, System.Type holder, object instance)
{
    if (holder == null) return;
    var members = new System.Collections.Generic.List<(string name, object value)>();
    foreach (var f in holder.GetFields(bf))
    {
        if (f.IsStatic != (instance == null)) continue;
        if (f.Name.StartsWith("<")) continue;
        members.Add((f.Name, f.GetValue(instance)));
    }
    foreach (var p in holder.GetProperties(bf))
    {
        if (p.GetIndexParameters().Length > 0) continue;
        var g = p.GetGetMethod(true);
        if (g == null || g.IsStatic != (instance == null)) continue;
        members.Add((p.Name, p.GetValue(instance)));
    }
    members.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
    foreach (var (name, value) in members)
    {
        var subs = new System.Collections.Generic.List<string>();
        foreach (var d in Subscribers(value)) subs.Add(Name(d));
        if (subs.Count > 0) lines.Add(group + "." + name + " (" + subs.Count + "): " + string.Join(" | ", subs));
    }
}
System.Type T(string n)
{
    foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
    {
        var t = a.GetType(n);
        if (t != null) return t;
    }
    return null;
}
lines.Add("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
Dump("EventBridge.Menu", T("KitchenDesign.EventBridge+Menu"), null);
Dump("EventBridge.Designer", T("KitchenDesign.EventBridge+Designer"), null);
foreach (var n in new[] { "EventApp", "EventSession", "EventMenu", "EventDesigner" }) Dump(n, T("KitchenDesign.Events." + n), null);
var kitchenType = T("KitchenDesign.KitchenModel.Kitchen");
var kitchen = kitchenType == null ? null : UnityEngine.Object.FindFirstObjectByType(kitchenType);
if (kitchen != null)
{
    var ep = kitchenType.GetProperty("Events", bf);
    if (ep != null) { var ke = ep.GetValue(kitchen); if (ke != null) Dump("KitchenEvents", ke.GetType(), ke); }
}
var contentType = T("KitchenDesign.ProFitGlobals");
if (contentType != null)
{
    var pc = contentType.GetProperty("productContent", bf);
    if (pc != null) { var c = pc.GetValue(null); if (c != null) Dump("ProductVisualContentStatus", c.GetType(), c); }
}
System.IO.File.WriteAllLines(outPath, lines);
return lines.Count + " lines -> " + outPath;
