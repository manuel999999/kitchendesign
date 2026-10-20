namespace Melcher
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.UI;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(MelcherInvisibleRaycastTarget), editorForChildClasses: false)]
    public class MelcherInvisibleRaycastTargetEditor : GraphicEditor
    {
        /// <summary>
        /// The same as the base class but does not draw the appearance
        /// controls.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Script, System.Array.Empty<GUILayoutOption>());
            // Skip AppearanceControlsGUI
            RaycastControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }

    }   // class MelcherInvisibleRaycastTargetEditor

}   // namespace Melcher
