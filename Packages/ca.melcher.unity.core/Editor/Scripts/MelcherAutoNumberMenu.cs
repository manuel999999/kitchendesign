namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    using System.Text.RegularExpressions;

    public class MelcherAutoNumberMenu
    {
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Auto name selected objects")]
        public static void AutoNameAndNumberSelection()
        {
            AutoNameAndNumber(Selection.gameObjects);
        }

        public static void AutoNameAndNumber(GameObject[] objects)
        {
            // Sort objects based on sibling index
            System.Array.Sort(objects, (x, y) => x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex()));

            if (objects != null && objects.Length > 0)
            {
                Match match = Regex.Match(objects[0].name, @"\d+");
                if (match != null)
                {
                    string firstName = objects[0].name;

                    string prefix = firstName.Substring(0, match.Index);

                    int startingNumber = match.Success ? int.Parse(firstName.Substring(match.Index, match.Length), System.Globalization.NumberStyles.Any) : 0;

                    // Purge bracket numbers after first elements name
                    Undo.RecordObject(objects[0], "AutoNamed game objects");
                    objects[0].name = firstName.Substring(0, match.Index + match.Length);
                    RecordGameObjectChange(objects[0]);

                    string fmt = "{0:D" + match.Length + "}";

                    int selectedCount = objects.Length;
                    for (int i = 1; i < selectedCount; ++i)
                    {
                        Undo.RecordObject(objects[1], "AutoNamed game objects");
                        objects[i].name = prefix + string.Format(fmt, startingNumber + i);
                        RecordGameObjectChange(objects[i]);
                    }

                    Debug.Log("Renamed " + selectedCount + " objects");
                }
            }
        }

        public static void RecordGameObjectChange(GameObject go)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                EditorUtility.SetDirty(go);
                PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            }
        }
    }   // class MelcherAutoNumberMenu

}   // namespace Melcher

