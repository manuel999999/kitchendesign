var names = new System.Collections.Generic.HashSet<string> {
    "BrandCatalog", "BrandCatalogSO", "KitchenTemplate", "KitchenTemplateSO",
    "CabinetSectionDoorStyle", "CabinetSectionDoorStyleSO", "CabinetSectionDoorHandleStyle", "CabinetSectionDoorHandleStyleSO",
    "CabinetSectionLegStyle", "CabinetSectionLegStyleSO", "CabinetSectionDoorEditorMapping", "CabinetSectionDoorEditorMappingSO" };
string Value(UnityEditor.SerializedProperty p)
{
    switch (p.propertyType)
    {
        case UnityEditor.SerializedPropertyType.Integer: return p.longValue.ToString();
        case UnityEditor.SerializedPropertyType.Boolean: return p.boolValue ? "1" : "0";
        case UnityEditor.SerializedPropertyType.Float: return p.doubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        case UnityEditor.SerializedPropertyType.String: return "\"" + p.stringValue + "\"";
        case UnityEditor.SerializedPropertyType.Enum: return "enum " + p.intValue;
        case UnityEditor.SerializedPropertyType.ObjectReference:
        {
            var o = p.objectReferenceValue;
            if (o == null) return "null";
            string guid; long local;
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out guid, out local);
            return guid + ":" + local;
        }
        case UnityEditor.SerializedPropertyType.Vector2: return p.vector2Value.ToString("R");
        case UnityEditor.SerializedPropertyType.Vector3: return p.vector3Value.ToString("R");
        case UnityEditor.SerializedPropertyType.Color: return p.colorValue.ToString("R");
        case UnityEditor.SerializedPropertyType.ArraySize: return "size " + p.intValue;
        default: return null;
    }
}
var lines = new System.Collections.Generic.List<string>();
int assets = 0;
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject"))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (!path.EndsWith(".asset")) continue;
    var main = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
    if (main == null || !names.Contains(main.GetType().Name)) continue;
    assets++;
    var so = new UnityEditor.SerializedObject(main);
    var it = so.GetIterator();
    bool enter = true;
    while (it.Next(enter))
    {
        enter = true;
        if (it.propertyPath == "m_Script") { enter = false; continue; }
        string v = Value(it);
        if (v != null) lines.Add(path + " " + it.propertyPath + " = " + v);
    }
}
lines.Sort(System.StringComparer.Ordinal);
string text = string.Join("\n", lines).Normalize(System.Text.NormalizationForm.FormC) + "\n";
System.IO.File.WriteAllText("/Users/emmanuel/Documents/Unity/GeminiBaseline/kitchendatacensus-latest.txt", text);
ulong h = 14695981039346656037UL; foreach (char ch in text) { h = (h ^ ch) * 1099511628211UL; }
return "assets=" + assets + " lines=" + lines.Count + " hash=" + h.ToString("X16");
