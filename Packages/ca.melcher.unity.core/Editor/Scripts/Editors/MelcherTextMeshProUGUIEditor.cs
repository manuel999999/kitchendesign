namespace Melcher
{
    using UnityEngine;
    using UnityEditor;
    using TMPro;
    using TMPro.EditorUtilities;

    /// <summary>
    ///     This overrides the default inspector drawer for <see
    ///     cref="TextMeshProUGUI"/>. It makes the text input box read-only when
    ///     a <see cref="MelcherLocalizedText"/> component is attached. This
    ///     prevents developers from mistakenly editing the text mesh
    ///     component's text which will ultimately be stomped over by the
    ///     localized text component.
    /// </summary>
    [UnityEditor.CustomEditor(typeof(TextMeshProUGUI), editorForChildClasses: true)]
    public class MelcherTextMeshProUGUIEditor : TMP_EditorPanelUI
    {
        public override void OnInspectorGUI()
        {
            Component targetAsComponent = (Component)target;

            if (targetAsComponent.TryGetComponent(out MelcherLocalizedText localizedText))
            {
                OnInspectorGUIWithMelcherLocalizedText();
            }
            else
            {
                base.OnInspectorGUI();
            }
        }

        /// <summary>
        ///     Copy of TMP_BaseEditorPanel.OnInspectorGUI() that disables the
        ///     text input.
        /// </summary>
        protected void OnInspectorGUIWithMelcherLocalizedText()
        {
            // Make sure Multi selection only includes TMP Text objects.
            if (IsMixSelectionTypes()) return;

            serializedObject.Update();

            // If only DrawTextInput were virtual! This could be a 4 line
            // function override rather than a full copy of the function that
            // calls DrawTextInput.
            using (var disabledScope = new EditorGUI.DisabledScope(disabled: true))
            {
                DrawTextInput();

                EditorGUILayout.HelpBox($"Edit the text via the attached {nameof(MelcherLocalizedText)} component.", MessageType.Info);
                EditorGUILayout.Space();
            }

            DrawMainSettings();

            DrawExtraSettings();

            EditorGUILayout.Space();

            if (serializedObject.ApplyModifiedProperties() || m_HavePropertiesChanged)
            {
                m_TextComponent.havePropertiesChanged = true;
                m_HavePropertiesChanged = false;
                EditorUtility.SetDirty(target);
            }
        }

    }   // class MelcherTextMeshProUGUIEditor

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-05-04
/// ===========================================================================
