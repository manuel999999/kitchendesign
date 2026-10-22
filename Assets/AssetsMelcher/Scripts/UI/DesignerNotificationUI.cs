namespace KitchenDesign
{
    using Melcher;
    using System;
    using UnityEngine;

    /// <summary>
    /// A simple pop-up for communicating a message to the user. The pop-up
    /// only has a title, a message and an OK button.
    /// </summary>
    public class DesignerNotificationUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ShowPrompt(string title, string message, Action onDismiss)
        {
            m_TitleLabel.phrase = title;
            m_MessageLabel.phrase = message;
            m_OnDismiss = onDismiss;

            Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_OkButton.onClick += OnClickOK;

            EventBridge.Designer.showNotification += ShowPrompt;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_OkButton.onClick -= OnClickOK;

            EventBridge.Designer.showNotification -= ShowPrompt;
        }

        protected void OnClickOK(MelcherButton caller)
        {
            Action onDismissCopy = m_OnDismiss;
            m_OnDismiss = null;
            onDismissCopy?.Invoke();

            Hide();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DesignerConfirmUI))]

        [SerializeField] protected MelcherLocalizedText m_TitleLabel = null;
        [SerializeField] protected MelcherLocalizedText m_MessageLabel = null;
        [SerializeField] protected MelcherButton m_OkButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        /// <summary>
        ///     The callback to fire when the user selects an option on the
        ///     notification window.
        /// </summary>
        protected Action m_OnDismiss;

    }   // class DesignerNotificationUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-04
/// ===========================================================================
