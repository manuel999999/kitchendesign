namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(MelcherSafeAreaConformerDock))]
    public class MelcherSafeAreaConformerDockEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                serializedObject.UpdateIfRequiredOrScript();

                var dockedSafeArea = serializedObject.targetObject as MelcherSafeAreaConformerDock;
                bool isCenterDocked = dockedSafeArea.isCenterDocked;

                // Loop through properties and create one field (including children) for each top level property.
                var property = serializedObject.GetIterator();
                bool expanded = true;
                while (property.NextVisible(expanded))
                {
                    expanded = false;

                    var propertyPath = property.propertyPath;

                    bool disabled = propertyPath == "m_Script";

                    if (!isCenterDocked)
                    {
                        // Hide docked safe area fields when not center docked
                        if (propertyPath == MelcherSafeAreaConformerDock.CenterDockPaddingMemberName ||
                            propertyPath == MelcherSafeAreaConformerDock.LeftDockMemberName ||
                            propertyPath == MelcherSafeAreaConformerDock.RightDockMemberName ||
                            propertyPath == MelcherSafeAreaConformerDock.TopDockMemberName ||
                            propertyPath == MelcherSafeAreaConformerDock.BottomDockMemberName)
                        {
                            continue;
                        }
                    }
                    else if (propertyPath == MelcherSafeAreaConformerDock.AnchoredSizeMemberName)
                    {
                        // Hide anchored size field when center docked
                        continue;
                    }

                    // Show the value of GetLayoutOrder() which considers /
                    // inherits the docked safe areas' order
                    if (propertyPath == MelcherSafeAreaConformerDock.LayoutOrderMemberName)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.IntField(
                                label: property.displayName,
                                value: dockedSafeArea.GetLayoutOrder(),
                                options: System.Array.Empty<GUILayoutOption>()
                            );
                        }
                        continue;
                    }
                    else if (propertyPath == MelcherSafeAreaConformerDock.CenterDockPaddingMemberName)
                    {
                        // TODO: Show a Unity-like padding thing
                    }

                    using (new EditorGUI.DisabledScope(disabled))
                    {
                        EditorGUILayout.PropertyField(property, true);
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }
            if (EditorGUI.EndChangeCheck())
            {
                //EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }

    }   // class MelcherSafeAreaConformerDockEditor

}   // namespace Melcher
