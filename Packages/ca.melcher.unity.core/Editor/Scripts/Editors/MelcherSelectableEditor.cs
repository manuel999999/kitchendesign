namespace Melcher
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.UI;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(MelcherSelectable), editorForChildClasses: true)]
    public class MelcherSelectableEditor : SelectableEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SerializedProperty property = serializedObject.FindProperty("m_RectTransform");

            do
            {
                EditorGUILayout.PropertyField(property);
            } while (property.Next(enterChildren: false));

            serializedObject.ApplyModifiedProperties();
        }
    }   // class MelcherSelectableEditor

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-21
/// ===========================================================================
