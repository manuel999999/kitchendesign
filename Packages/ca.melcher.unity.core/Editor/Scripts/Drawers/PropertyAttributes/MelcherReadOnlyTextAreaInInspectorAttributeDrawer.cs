namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// Render a disabled text area.
    /// </summary>
    [CustomPropertyDrawer(typeof(MelcherReadOnlyTextAreaInInspectorAttribute))]
    public class MelcherReadOnlyTextAreaInInspectorDrawer : PropertyDrawer
    {
        const int Padding = 2;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetLabelHeight(label) + Padding + GetTextAreaHeight(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            {
                var labelPosition = position;
                labelPosition.height = GetLabelHeight(label);
                EditorGUI.LabelField(labelPosition, label);

                position.y += labelPosition.height + Padding;
                position.height -= labelPosition.height + Padding;
                property.stringValue = EditorGUI.TextArea(position, property.stringValue, EditorStyles.textArea);
            }
            GUI.enabled = true;
        }

        private float GetLabelHeight(GUIContent label)
        {
            return EditorStyles.label.CalcHeight(label, Screen.width);
        }

        private float GetTextAreaHeight(SerializedProperty property)
        {
            var content = new GUIContent(property.stringValue);
            return EditorStyles.textArea.CalcHeight(content, Screen.width);
        }

    }   // class MelcherReadOnlyTextAreaInInspectorDrawer

}   // namespace Melcher