namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(MelcherVariableBase), useForChildren: true)]
    public class MelcherVariableDrawer : PropertyDrawer
    {
        private void Init()
        {
            if (s_DropdownIcon == null)
            {
                s_DropdownIcon = EditorGUIUtility.IconContent("icon dropdown");
            }

            if (s_AlignIcon == null)
            {
                s_AlignIcon = EditorGUIUtility.IconContent("d_align_vertically_center");
            }

            if (s_PopupStyle == null)
            {
                s_PopupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
                s_PopupStyle.imagePosition = ImagePosition.ImageOnly;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init();

            using (var propertyScope = new EditorGUI.PropertyScope(position, label, property))
            {
                position = EditorGUI.PrefixLabel(position, propertyScope.content);

                EditorGUI.BeginChangeCheck();
                {
                    // Draw the dropdown that chooses whether to use the app
                    // value or the inline value

                    Rect dropDownPosition = new Rect(position);
                    dropDownPosition.yMin += s_PopupStyle.margin.top;
                    dropDownPosition.width = s_PopupStyle.fixedWidth + s_PopupStyle.margin.right;
                    position.xMin = dropDownPosition.xMax;

                    int indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    using SerializedProperty useAppValueProperty =
                        property.FindPropertyRelative(MelcherVariableBase.DrawAppValue);

                    int dropdownIndex = useAppValueProperty.boolValue ? 1 : 0;

                    dropdownIndex =
                        EditorGUI.Popup(
                            dropDownPosition,
                            dropdownIndex,
                            s_UseAppValueOptions,
                            s_PopupStyle
                        );

                    useAppValueProperty.boolValue = dropdownIndex != 0;

                    position.xMin += dropDownPosition.width;

                    object targetObject = MelcherEditorUtility.GetActualObjectForSerializedProperty<object>(fieldInfo, property, out int arrayIndex);
                    MelcherVariableBase genericVariable = targetObject as MelcherVariableBase;

                    bool isDragging = DragAndDrop.objectReferences.Length > 0;
                    bool validDrag = isDragging && genericVariable.appValueType.IsAssignableFrom(DragAndDrop.objectReferences[0].GetType());

                    using SerializedProperty appValueProperty =
                        property.FindPropertyRelative(
                            MelcherVariable<bool, MelcherBoolAppValue>.AppValueMemberName
                        );

                    // Show the asset field if it is enabled or the user is
                    // dragging an asset of the correct type
                    if (useAppValueProperty.boolValue || validDrag)
                    {
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.PropertyField(position, appValueProperty, GUIContent.none);
                        }
                        if (EditorGUI.EndChangeCheck() && validDrag)
                        {
                            // Force select the "Use Asset" option after a
                            // successful drag and drop
                            useAppValueProperty.boolValue = true;
                        }
                    }
                    else
                    {
                        if (appValueProperty.objectReferenceValue != null)
                        {
                            // Clear the previous app value reference. This is
                            // necessary because the app value field encodes
                            // whether it is valid by being non-null.
                            //
                            // This also prevents accidentally holding a
                            // reference to an asset that doesn't need to be
                            // loaded.

                            appValueProperty.objectReferenceValue = null;
                        }

                        using SerializedProperty uniqueProperty =
                            property.FindPropertyRelative(
                                MelcherVariable<bool, MelcherBoolAppValue>.InlineMemberName
                            );

                        GUIContent uniqueLabel = GUIContent.none;

                        if (uniqueProperty.hasChildren && !uniqueProperty.isExpanded)
                        {
                            m_UniqueLabel.text =
                                fieldInfo.GetValue(property.serializedObject.targetObject).ToString();
                            uniqueLabel = m_UniqueLabel;
                        }

                        EditorGUI.PropertyField(
                            position,
                            uniqueProperty,
                            uniqueLabel,
                            includeChildren: true
                        );
                    }

                    EditorGUI.indentLevel = indent;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            using SerializedProperty useAssetProperty =
                property.FindPropertyRelative(MelcherVariable<bool, MelcherBoolAppValue>.DrawAppValue);

            if (useAssetProperty.boolValue)
            {
                return EditorGUI.GetPropertyHeight(useAssetProperty, includeChildren: false);
            }

            GUIStyle textFieldStyle = EditorStyles.textField;

            using SerializedProperty uniqueProperty =
                property.FindPropertyRelative(MelcherVariable<bool, MelcherBoolAppValue>.InlineMemberName);

            return EditorGUI.GetPropertyHeight(uniqueProperty, uniqueProperty.isExpanded);
        }

        // ====================================================================
        // Static fields
        // ====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_DropdownIcon;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_AlignIcon;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIStyle s_PopupStyle;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly GUIContent[] s_UseAppValueOptions =
            new[]
            {
                new GUIContent("Inline Value"),
                new GUIContent("App Value")
            };

        // ====================================================================
        // Internal fields
        // ====================================================================

        private GUIContent m_UniqueLabel = new GUIContent();

        protected bool m_ShowAssetField;

    }   // class MelcherVariableDrawer

}   // namespace Melcher