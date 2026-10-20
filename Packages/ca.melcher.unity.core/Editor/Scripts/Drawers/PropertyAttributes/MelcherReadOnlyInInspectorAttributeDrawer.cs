namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// Render a disabled property.
    /// </summary>
    [CustomPropertyDrawer(typeof(MelcherReadOnlyInInspectorAttribute))]
    public class MelcherReadOnlyInInspectorAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, includeChildren: true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            {
                label = EditorGUI.BeginProperty(position, label, property);
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
                EditorGUI.EndProperty();
            }
            GUI.enabled = true;
        }

    }   // class MelcherReadOnlyInInspectorAttributeDrawer

}   // namespace Melcher
