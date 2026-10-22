namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEditor;


    [CustomPropertyDrawer(typeof(Cuboid))]
    public class CuboidDrawer : PropertyDrawer
    {
        public const int RowCount = 4;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_Property = property;

            using var propertyScope = new EditorGUI.PropertyScope(position, label, property);

            Rect row = position;
            row.height /= RowCount;

            EditorGUI.LabelField(row, label);
            row.y += row.height;

            ++EditorGUI.indentLevel;

            try
            {
                SerializedProperty min = property.FindPropertyRelative(nameof(Cuboid.min));
                SerializedProperty max = property.FindPropertyRelative(nameof(Cuboid.max));
                Vector3 size = max.vector3Value - min.vector3Value;

                EditorGUI.PropertyField(row, min, new GUIContent(min.displayName));
                row.y += row.height;

                EditorGUI.BeginChangeCheck();
                {
                    size = EditorGUI.Vector3Field(row, "Size", size);
                    row.y += row.height;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    max.vector3Value = min.vector3Value + size;
                }
                EditorGUI.PropertyField(row, max, new GUIContent(max.displayName));
            }
            finally
            {
                --EditorGUI.indentLevel;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * RowCount;
        }

        protected SerializedProperty m_Property;
    }   // class CuboidDrawer

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-18
/// ===========================================================================
