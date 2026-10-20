namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(MelcherMinMax))]
    [CanEditMultipleObjects]
    public class MelcherMinMaxDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                // Draw property name
                Rect remainingPosition = EditorGUI.PrefixLabel(position, label);

                const int Padding = 2;
                Rect floatPosition = remainingPosition;

                float defaultLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 30.0f;
                {
                    // Draw float field for min value
                    SerializedProperty minProperty = property.FindPropertyRelative(nameof(MelcherMinMax.min));
                    floatPosition.width = floatPosition.width * 0.5f - Padding;
                    EditorGUI.PropertyField(floatPosition, minProperty);

                    // Draw float field for max value
                    SerializedProperty maxProperty = property.FindPropertyRelative(nameof(MelcherMinMax.max));
                    floatPosition.x += floatPosition.width + (Padding * 2);
                    EditorGUI.PropertyField(floatPosition, maxProperty);
                }
                EditorGUIUtility.labelWidth = defaultLabelWidth;
            }
            EditorGUI.EndProperty();
        }
    }   // class MelcherMinMaxDrawer

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-02-20
/// ===========================================================================
