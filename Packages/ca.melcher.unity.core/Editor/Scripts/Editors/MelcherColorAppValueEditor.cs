namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    ///     Show a color widget next to the reference field. This makes using
    ///     <see cref="MelcherColorAppValue"/>s more frictionless as users can
    ///     tell at a glance what color is referenced from the inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(MelcherColorAppValue))]
    public class MelcherColorAppValueEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect propertyPosition = position;

            bool isObjectReferenceAssigned =
                property.objectReferenceValue != null;

            if (isObjectReferenceAssigned)
            {
                // Make space to draw the color field.
                propertyPosition.width -= 70.0f;
            }

            EditorGUI.PropertyField(propertyPosition, property, label);

            if (!isObjectReferenceAssigned)
            {
                // There's no AppValue assigned so there's no reason to draw
                // the color field. Return early.
                return;
            }

            // Draw a color field so it is very clear in the inspector which
            // color is being referenced. This color field can also be modified
            // to modify the referenced AppValue.

            Rect colorPosition = position;
            colorPosition.xMin = propertyPosition.xMax + 8.0f;

            using SerializedObject serializedObject =
                new SerializedObject(property.objectReferenceValue);

            using SerializedProperty colorProperty =
                serializedObject.FindProperty(MelcherColorAppValue.PropertyPath.Value);

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.PropertyField(colorPosition, colorProperty, label: GUIContent.none);

                if (changeCheckScope.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

    }   // class MelcherColorAppValueEditor

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-07-31
/// ===========================================================================
