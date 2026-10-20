namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    ///     A <see cref="PropertyDrawer"/> for <see
    ///     cref="MelcherReadWriteList{ElementType}"/>.
    ///     <br /><br />
    ///
    ///     This simplifies the inspector by eliminating an unnecessary layer
    ///     of nesting present in the default drawer. I.e., the default drawer
    ///     draws the <see cref="MelcherReadWriteList{ElementType}"/> property
    ///     and then the serialized list nested within that.
    /// </summary>
    [CustomPropertyDrawer(typeof(MelcherReadWriteList<>))]
    public class MelcherReadWriteListDrawer : PropertyDrawer
    {
        /// <summary>
        ///     Skip the main property and draw the nested, serialized list.
        ///     Use the main property's label for the nested list.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.NextVisible(enterChildren: true))
            {
                using var listPropertyScope =
                    new EditorGUI.PropertyScope(position, label, property);

                EditorGUI.PropertyField(position, property, label, includeChildren: true);
            }
        }

        /// <summary>
        ///     Get the height of the serialized list.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            property.NextVisible(enterChildren: true);

            return EditorGUI.GetPropertyHeight(property, label);
        }

    }   // class MelcherReadWriteListDrawer

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-04-02
/// ===========================================================================
