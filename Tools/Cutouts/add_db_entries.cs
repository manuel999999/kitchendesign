var db = UnityEditor.AssetDatabase.LoadAssetAtPath<KitchenDesign.ProductVisualDatabase>("Assets/AssetsMelcher/ProductVisualDatabase.asset");
string r = "before=" + db.sourceEntries.Count + "\n";
foreach (var n in new[] { __NAMES__ })
{
    var v = UnityEditor.AssetDatabase.LoadAssetAtPath<KitchenDesign.ProductVisual>("Assets/AssetsMelcher/ProductVisuals/" + n + "^.asset");
    if (v == null) { r += n + ": not found\n"; continue; }
    if (db.mutableSourceEntries.Contains(v)) { r += n + ": already listed\n"; continue; }
    db.mutableSourceEntries.Add(v);
    r += n + ": added\n";
}
UnityEditor.EditorUtility.SetDirty(db);
UnityEditor.AssetDatabase.SaveAssetIfDirty(db);
var t = typeof(KitchenDesign.ProFitAddressableGroupMenuCommand);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
var collect = t.GetMethod("CollectProductVisualDatabaseAssetGuids", flags);
object guids = collect != null ? collect.Invoke(null, null) : null;
var update = t.GetMethod("UpdateAssetGroups", flags);
var ps = update.GetParameters();
r += "UpdateAssetGroups(" + string.Join(", ", System.Array.ConvertAll(ps, p => p.ParameterType.Name + " " + p.Name)) + ")\n";
var args = new object[ps.Length];
args[0] = "t:prefab";
args[1] = KitchenDesign.ProFitGlobals.appProfile.productVisualFolderPath;
args[2] = "ProductVisuals";
for (int i = 3; i < ps.Length; i++) args[i] = guids != null && ps[i].ParameterType.IsAssignableFrom(guids.GetType()) ? guids : (ps[i].HasDefaultValue ? ps[i].DefaultValue : null);
update.Invoke(null, args);
UnityEditor.AssetDatabase.SaveAssets();
var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
foreach (var n in new[] { __NAMES__ })
{
    var guid = UnityEditor.AssetDatabase.AssetPathToGUID("Assets/AssetsMelcher/ProductVisuals/" + n + ".prefab");
    var e = settings.FindAssetEntry(guid);
    r += n + ".prefab addressable=" + (e != null ? e.parentGroup.Name + " address=" + e.address : "NO") + "\n";
}
r += "after=" + db.sourceEntries.Count;
return r;
