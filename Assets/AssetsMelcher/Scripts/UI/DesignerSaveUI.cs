namespace Melcher
{
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// A prompt that appears when the user chooses to save a design.
    /// </summary>
    public class DesignerSaveUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ShowPrompt(string saveName, string saveNotes, EventBridge.Designer.SaveConfirmHandler onSave, string error)
        {
            m_SaveNameField.text = saveName;

            m_SaveNotesField.text = saveNotes;

            if (string.IsNullOrEmpty(error))
            {
                m_MessageBox.Clear();
            }
            else
            {
                m_MessageBox.Error(error);
            }

            m_SaveAction = onSave;

            Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EventBridge.Designer.showSavePrompt += ShowPrompt;

            m_CancelButton.onClick += OnClickCancel;
            m_SaveButton.onClick += OnClickSave;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EventBridge.Designer.showSavePrompt -= ShowPrompt;

            m_CancelButton.onClick -= OnClickCancel;
            m_SaveButton.onClick -= OnClickSave;
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            Hide();
        }

        protected void OnClickSave(MelcherButton caller)
        {
            string saveName = m_SaveNameField.text.Trim();

            if (string.IsNullOrWhiteSpace(saveName))
            {
                m_MessageBox.Error("Name cannot be blank.");
                return;
            }

            Hide();

            m_SaveAction?.Invoke(m_SaveNameField.text, m_SaveNotesField.text);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DesignerSaveUI))]

        [SerializeField] protected TMP_InputField m_SaveNameField = null;
        [SerializeField] protected TMP_InputField m_SaveNotesField = null;
        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected MelcherButton m_CancelButton = null;
        [SerializeField] protected MelcherButton m_SaveButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected EventBridge.Designer.SaveConfirmHandler m_SaveAction;

    }   // class DesignerSaveUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
