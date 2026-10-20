namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(MelcherAssetFieldAttribute))]
    public class MelcherAssetFieldAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            {
                Object originalValue = property.objectReferenceValue;

                EditorGUI.BeginChangeCheck();
                {
                    EditorGUI.PropertyField(position, property, label);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Object newValue = property.objectReferenceValue;

                    if (newValue != null &&
                        string.IsNullOrEmpty(AssetDatabase.GetAssetPath(newValue)))
                    {
                        MelcherLog.LogError(
                            $"Cannot assign {newValue} to {property.propertyPath}. " +
                            $"New value is not an asset.",
                            context: property.objectReferenceValue
                        );
                        property.objectReferenceValue = originalValue;
                    }
                }
            }
            EditorGUI.EndProperty();
        }

    }   // class MelcherAssetFieldAttributeDrawer

}   // namespace Melcher
