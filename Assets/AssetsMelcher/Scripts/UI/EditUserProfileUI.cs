namespace Melcher
{
    using System;
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// The prompt that shows up when a user edits a field in their profile.
    /// </summary>
    public class EditUserProfileUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void SetContent(
            string      title,
            string      header1,
            string      value1,
            string      header2 = null,
            string      value2 = null,
            TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard,
            TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default,
            Action<string, string> onAccept = null)
        {
            // Clear the old messages
            m_MessageBox.Clear();

            m_TitleLabel.phrase = title;
            m_OnAccept = onAccept;

            m_Row1.SetFields(header1, value1, contentType, keyboardType, m_PhoneNumberValidator);

            bool useRow2 = header2 != null;
            m_Row2.gameObject.SetActive(useRow2);
            if (useRow2)
            {
                m_Row2.SetFields(header2, value2, contentType, keyboardType, m_PhoneNumberValidator);
            }

            Show();
        }

        public void SetError(string error)
        {
            m_CanvasGroup.interactable = true;
            m_MessageBox.Error(error);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_CancelButton.onClick += OnClickCancel;
            m_ApplyButton.onClick += OnClickApply;

            EventBridge.Menu.showEditUserProfileUi += SetContent;
            EventBridge.Menu.setEditUserProfileError += SetError;
            EventBridge.Menu.hideEditUserProfileUi += Finished;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_CancelButton.onClick -= OnClickCancel;
            m_ApplyButton.onClick -= OnClickApply;

            EventBridge.Menu.showEditUserProfileUi -= SetContent;
            EventBridge.Menu.setEditUserProfileError -= SetError;
            EventBridge.Menu.hideEditUserProfileUi -= Finished;
        }

        protected void Finished()
        {
            Hide();
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            Hide();
        }

        protected void OnClickApply(MelcherButton caller)
        {
            m_MessageBox.Clear();

            if (m_OnAccept != null)
            {
                // Lock-out the UI until the web request returns
                m_CanvasGroup.interactable = false;

                m_OnAccept.Invoke(m_Row1.value, m_Row2.gameObject.activeSelf ? m_Row2.value : null);
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(EditUserProfileUI))]

        /// <summary>
        /// A validator that prevents users from entering invalid phone number
        /// characters and formats the phone number to be easier to read.
        /// </summary>
        [SerializeField, MelcherAssetField]
        protected TMP_InputValidator m_PhoneNumberValidator = null;

        [Space]

        [SerializeField] protected MelcherLocalizedText m_TitleLabel = null;

        [SerializeField] protected EditProfilePromptRow m_Row1 = null;
        [SerializeField] protected EditProfilePromptRow m_Row2 = null;

        [SerializeField] protected InlineMessageBox m_MessageBox = null;

        [SerializeField] protected MelcherButton m_CancelButton = null;
        [SerializeField] protected MelcherButton m_ApplyButton = null;


        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected Action<string, string> m_OnAccept;

    }   // class EditUserProfileUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
