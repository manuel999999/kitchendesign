namespace KitchenDesign
{
    using Melcher;
    using System;
    using UnityEngine;

    /// <summary>
    /// A prompt for the designer scene. It is used to ensure a user fully
    /// intends to perform an action (e.g., quit or restart).
    /// </summary>
    public class DesignerConfirmUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ShowPrompt(string title, string message, string confirmLabel, Action confirmAction)
        {
            m_TitleLabel.phrase = title;
            m_MessageLabel.phrase = message;
            m_ConfirmButton.GetComponentInChildren<MelcherLocalizedText>().phrase = confirmLabel;
            m_ConfirmAction = confirmAction;

            Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_CancelButton.onClick += OnClickCancel;
            m_ConfirmButton.onClick += OnClickConfirm;

            EventBridge.Designer.showConfirmPrompt += ShowPrompt;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_CancelButton.onClick -= OnClickCancel;
            m_ConfirmButton.onClick -= OnClickConfirm;

            EventBridge.Designer.showConfirmPrompt -= ShowPrompt;
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            Hide();
        }

        protected void OnClickConfirm(MelcherButton caller)
        {
            Hide();
            m_ConfirmAction?.Invoke();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DesignerConfirmUI))]

        [SerializeField] protected MelcherLocalizedText m_TitleLabel = null;
        [SerializeField] protected MelcherLocalizedText m_MessageLabel = null;
        [SerializeField] protected MelcherButton m_CancelButton = null;
        [SerializeField] protected MelcherButton m_ConfirmButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected Action m_ConfirmAction;

    }   // class DesignerConfirmUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
