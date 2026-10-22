namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;
    using MEC;

    /// <summary>
    /// The prompt that shows up to confirm the user wants to delete their
    /// account.
    /// </summary>
    public class DeleteAccountUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        protected override void OnEnable()
        {
            base.OnEnable();

            m_DeleteButton.onClick += OnClickDelete;
            m_KeepButton.onClick += OnClickKeep;

            m_PasswordInput.onValueChanged.AddListener(OnPasswordValueChanged);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_DeleteButton.onClick -= OnClickDelete;
            m_KeepButton.onClick -= OnClickKeep;

            m_PasswordInput.onValueChanged.RemoveListener(OnPasswordValueChanged);
        }

        protected override void DoShow()
        {
            base.DoShow();

            m_RevealPasswordButton.SetSecure(true);
            m_PasswordInput.text = "";

            // Disable the delete button until the user enters some kind of password
            m_DeleteButton.interactable = false;

            m_AccountEmailLabel.phrase = ProFitGlobals.userProfile.signInEmail;
            m_MessageBox.Clear();
        }

        protected override void DoHide()
        {
            Timing.KillCoroutines(m_CountdownRoutine);

            base.DoHide();
        }

        protected void OnClickDelete(MelcherButton caller)
        {
            // Lock-out the UI until the web request returns
            m_CanvasGroup.interactable = false;
            m_MessageBox.Clear();
            EventBridge.Menu.showFullScreenSpinner.Invoke(true);

            ProFitGlobals.server.Accounts.DeleteAccount(
                ProFitGlobals.user,
                m_PasswordInput.text,
                onSuccess: () =>
                {
                    EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                    m_CanvasGroup.interactable = true;

                    Hide();

                    ProFitGlobals.LogOut();
                    EventBridge.Menu.showWelcomeUi?.Invoke(MelcherUITransitionStyle.Regress);
                },
                onError: (builder, error, jsonValue) =>
                {
                    EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                    m_CanvasGroup.interactable = true;

                    m_MessageBox.Error(error.Message);
                }
            );
        }

        protected void OnClickKeep(MelcherButton caller)
        {
            Hide();
        }

        protected void OnPasswordValueChanged(string password)
        {
            m_DeleteButton.interactable = password.Length > 0;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DeleteClientUI))]

        [SerializeField] protected MelcherLocalizedText m_AccountEmailLabel = null;
        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected RevealPasswordButton m_RevealPasswordButton = null;
        [SerializeField] protected TMP_InputField m_PasswordInput = null;
        [SerializeField] protected MelcherButton m_DeleteButton = null;
        [SerializeField] protected MelcherButton m_KeepButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected CoroutineHandle m_CountdownRoutine;
        protected int m_ClientId;
        protected int m_QuoteId;

    }   // class DeleteAccountUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-12-14
/// ===========================================================================

