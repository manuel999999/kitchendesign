namespace Melcher
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using System;
    using Object = UnityEngine.Object;
    public static class MelcherEasyButtonController
    {
        private static Dictionary<int, List<MethodInfo>> RowDict = new();
        private const int RightEdgeSpacer = 30;
        public static void DoButtonCreation(Object target, Object[] targets)
        {
            GUILayout.Space(12);

            IEnumerable<MethodInfo> methods = target.GetType()
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                    )
                .Where(m => m.GetParameters().Length == 0);

            foreach (MethodInfo method in methods)
            {
                if (Attribute.GetCustomAttribute(method, typeof(MelcherEasyButtonAttribute)) as MelcherEasyButtonAttribute != null)
                {
                    GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.boldLabel);

                    style.padding = new RectOffset(0, 0, 0, 5);
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 13;
                    GUILayout.Label("Melcher Easy Buttons", style);
                    break;
                }
            }

            RowDict.Clear();

            foreach (MethodInfo method in methods)
            {
                MelcherEasyButtonAttribute buttonAttribute = Attribute.GetCustomAttribute(
                    method, typeof(MelcherEasyButtonAttribute)) as MelcherEasyButtonAttribute;

                if (buttonAttribute != null)
                {
                    if (buttonAttribute.rowId >= 0)
                    {
                        RowDict.TryAdd(buttonAttribute.rowId, new());

                        if (RowDict.TryGetValue(buttonAttribute.rowId, out List<MethodInfo> value))
                        {
                            value.Add(method);
                        }

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(buttonAttribute.label))
                    {
                        string methodName = UnityEditor.ObjectNames.NicifyVariableName(method.Name);
                        buttonAttribute.SetButtonLabel(methodName);
                    }

                    Align alignment = buttonAttribute.alignment;

                    if (alignment == Align.Center || alignment == Align.Right)
                    {
                        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                        GUILayout.FlexibleSpace();
                        GUILayout.ExpandWidth(true);
                    }

                    if (GUILayout.Button(buttonAttribute.label, buttonAttribute.style) == true)
                    {
                        foreach (UnityEngine.Object methodTarget in targets)
                            method.Invoke(methodTarget, null);
                    }
                    if (alignment == Align.Center)
                    {
                        GUILayout.FlexibleSpace();
                    }
                    if (alignment == Align.Center || alignment == Align.Right)
                    {
                        GUILayout.EndHorizontal();
                    }
                }
            }

            // Create buttons that are intended to be in a row
            //
            // Some of the custom parameters aren't allowed at this time,
            // like width. We are just going to auto space the widths no matter
            // what is passed in, until such a time as we can spend more time
            // looking into making this more flexible (it probably doesn't need to be)
            foreach (KeyValuePair<int, List<MethodInfo>> keyValuePair in RowDict)
            {
                GUILayout.BeginHorizontal();

                float perButtonWidthInRow =
                    (Screen.width - RightEdgeSpacer) / keyValuePair.Value.Count;

                for (int i = 0; i < keyValuePair.Value.Count; i++)
                {
                    MethodInfo method = keyValuePair.Value[i];
                    MelcherEasyButtonAttribute buttonAttribute = Attribute.GetCustomAttribute(
                        method, typeof(MelcherEasyButtonAttribute)) as MelcherEasyButtonAttribute;

                    if (string.IsNullOrWhiteSpace(buttonAttribute.label))
                    {
                        string methodName = UnityEditor.ObjectNames.NicifyVariableName(method.Name);
                        buttonAttribute.SetButtonLabel(methodName);
                    }

                    buttonAttribute.style.fixedWidth = perButtonWidthInRow - 2;

                    if (GUILayout.Button(buttonAttribute.label, buttonAttribute.style) == true)
                    {
                        foreach (UnityEngine.Object methodTarget in targets)
                            method.Invoke(methodTarget, null);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }
    }   // class MelcherEasyButtonController

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Justin Bender
/// Initial creation date: 2022-08-25
/// ===========================================================================
