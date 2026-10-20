namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// Custom editor to simplify the appearance of the script within the
    /// inspector.
    /// </summary>
    [CustomEditor(typeof(MelcherUnityNote))]
    [CanEditMultipleObjects]
    public class MelcherUnityNoteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LazyInit();

            serializedObject.Update();

            m_CommentProperty.stringValue = EditorGUILayout.TextArea(
                m_CommentProperty.stringValue,
                s_TextAreaStyleWithWordWrapping,
                s_CommentPropertyLayoutOptions
            );

            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            m_CommentProperty = serializedObject.FindProperty(nameof(MelcherUnityNote.comment));
        }

        private void LazyInit()
        {
            if (s_CommentPropertyLayoutOptions == null)
            {
                // Setup layout options to make a TextArea dynamically resize
                // to vertically fit its content
                s_CommentPropertyLayoutOptions = new[] {
                    GUILayout.ExpandHeight(true),
                };
            }

            if (s_TextAreaStyleWithWordWrapping == null)
            {
                // Copy the default text area style and enable word wrappping
                s_TextAreaStyleWithWordWrapping = new GUIStyle(EditorStyles.textArea);
                s_TextAreaStyleWithWordWrapping.wordWrap = true;
            }
        }

        /// <summary>
        /// Custom GUI style that supports word wrap. Used to draw the text
        /// area for the note.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIStyle s_TextAreaStyleWithWordWrapping;

        /// <summary>
        /// GUI layout options for the text area. This allows the text area
        /// to expand vertically based on its content.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUILayoutOption[] s_CommentPropertyLayoutOptions;

        /// <summary>
        /// Note's text property.
        /// </summary>
        private SerializedProperty m_CommentProperty;

    }   // class MelcherUnityNoteEditor

}   // namespace Melcher
