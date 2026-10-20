namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(MelcherLayerFieldAttribute))]
    public class MelcherLayerFieldAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                property.intValue = EditorGUI.LayerField(position, label, property.intValue);
            }
            EditorGUI.EndProperty();
        }

    }   // class MelcherLayerFieldAttributeDrawer

}   // namespace Melcher
