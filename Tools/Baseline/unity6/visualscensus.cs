System.Type Find(params string[] names) { foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { if (!a.GetName().Name.StartsWith("KitchenDesign")) continue; foreach (var t in a.GetTypes()) foreach (var n in names) if (t.Name == n) return t; } return null; }
string Text(object v) { if (v == null) return "null"; if (v is UnityEngine.Object o) return o ? o.name : "missing"; if (v is string s) return s; if (v is System.Collections.IEnumerable e) { var l = new System.Collections.Generic.List<string>(); foreach (var x in e) l.Add(Text(x)); return "[" + string.Join(",", l) + "]"; } var p = v.GetType().GetProperty("value"); if (p != null && v.GetType().Name.StartsWith("MelcherStableEnum")) return Text(p.GetValue(v)); return System.Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture); }
var dbType = Find("ProductVisualDatabase", "ProductVisualDatabaseSO");
var guids = UnityEditor.AssetDatabase.FindAssets("t:" + dbType.Name);
var db = UnityEditor.AssetDatabase.LoadAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]), dbType);
var entries = (System.Collections.IEnumerable)dbType.GetProperty("sourceEntries").GetValue(db);
var sb = new System.Text.StringBuilder();
var tally = new System.Collections.Generic.SortedDictionary<string, int>();
int count = 0;
foreach (var v in entries)
{
    count++;
    var vt = v.GetType();
    var info = vt.GetProperty("info").GetValue(v);
    sb.Append(((UnityEngine.Object)v).name).Append(" address=").Append(Text(vt.GetProperty("contentAddress").GetValue(v)));
    foreach (var p in info.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
    {
        if (p.GetIndexParameters().Length > 0) continue;
        string val = Text(p.GetValue(info));
        sb.Append(' ').Append(p.Name).Append('=').Append(val);
        if (p.PropertyType.IsEnum || p.PropertyType.IsArray) { string key = p.Name + "=" + val; tally[key] = tally.TryGetValue(key, out int c) ? c + 1 : 1; }
    }
    sb.Append('\n');
}
string all = sb.ToString().Normalize();
string outPath = System.Environment.GetEnvironmentVariable("HOME") + "/Documents/Unity/GeminiBaseline/visualscensus-latest.txt";
System.IO.File.WriteAllText(outPath, all);
var md5 = System.Security.Cryptography.MD5.Create();
string hash = System.BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(all))).Replace("-", "").Substring(0, 16);
int enumKeys = 0; foreach (var k in tally.Keys) if (!k.Contains("[")) enumKeys++;
return "db=" + dbType.FullName + " entries=" + count + " hash=" + hash + " distinctEnumValues=" + enumKeys + " written=" + outPath;
