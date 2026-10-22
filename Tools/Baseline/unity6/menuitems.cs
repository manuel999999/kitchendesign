var lines = new System.Collections.Generic.List<string>();
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
{
    if (!a.GetName().Name.StartsWith("KitchenDesign")) continue;
    foreach (var t in a.GetTypes())
        foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
            foreach (UnityEditor.MenuItem mi in m.GetCustomAttributes(typeof(UnityEditor.MenuItem), false))
            {
                if (mi.validate) continue;
                bool exists = mi.menuItem.StartsWith("CONTEXT/") || mi.menuItem.StartsWith("Assets/") || mi.menuItem.StartsWith("GameObject/") || UnityEditor.Menu.MenuItemExists(mi.menuItem);
                lines.Add(mi.menuItem + " | " + t.Name + "." + m.Name + " | exists=" + exists);
            }
}
lines.Sort(System.StringComparer.Ordinal);
var path = System.Environment.GetEnvironmentVariable("HOME") + "/Documents/Unity/GeminiBaseline/menuitems-latest.txt";
System.IO.File.WriteAllLines(path, lines);
int missing = 0; foreach (var l in lines) if (l.EndsWith("exists=False")) missing++;
return "menu items=" + lines.Count + " missing=" + missing + " written=" + path;
