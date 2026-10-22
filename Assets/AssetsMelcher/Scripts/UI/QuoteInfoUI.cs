namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// The prompt that shows up when a user tries to rename a quote.
    /// </summary>
    public class QuoteInfoUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void SetFields(int clientId, int quoteId)
        {
            m_ClientId = clientId;
            m_QuoteId = quoteId;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_DeleteButton.onClick += OnClickDelete;
            m_CancelButton.onClick += OnClickCancel;
            m_RenameButton.onClick += OnClickRename;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_DeleteButton.onClick -= OnClickDelete;
            m_CancelButton.onClick -= OnClickCancel;
            m_RenameButton.onClick -= OnClickRename;
        }

        protected override void DoShow()
        {
            // Clear the old messages
            m_MessageBox.Clear();

            if (!ProFitGlobals.TryGetClientAndQuoteIndex(m_ClientId, m_QuoteId, out int clientIndex, out int quoteIndex))
            {
                return;
            }

            UserClientQuote quote = ProFitGlobals.userClients[clientIndex].quotes[quoteIndex];

            m_QuoteNameField.text = quote.saveName;

            base.DoShow();
        }

        protected void OnClickDelete(MelcherButton caller)
        {
            Hide();
            EventBridge.Menu.showDeleteQuoteUi?.Invoke(m_ClientId, m_QuoteId);
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            Hide();

            // TODO: it would be better if this class and DeleteQuoteUI didn't
            // access the globals
            ProFitGlobals.activeQuoteId = null;
        }

        protected void OnClickRename(MelcherButton caller)
        {
            m_MessageBox.Clear();


            if (!ProFitGlobals.TryGetClientAndQuoteIndex(m_ClientId, m_QuoteId, out int clientIndex, out int quoteIndex))
            {
                return;
            }

            UserClientQuote quote = ProFitGlobals.userClients[clientIndex].quotes[quoteIndex];

            EventBridge.Menu.showFullScreenSpinner?.Invoke(true);
            ProFitGlobals.server.Quotes.UpdateQuote(
                ProFitGlobals.user,
                m_ClientId,
                m_QuoteId,
                m_QuoteNameField.text,
                quote.saveNotes,
                quote.saveData,
                coverPhoto: null,
                topDownPhoto: null,
                frontPhotos: null,
                topDownPlanPhoto: null,
                frontPlanPhotos: null,
                onSuccess: (newQuote) =>
                {
                    EventBridge.Menu.showFullScreenSpinner?.Invoke(false);
                    m_MessageBox.Clear();

                    if (!ProFitGlobals.TryGetClientAndQuoteIndex(m_ClientId, m_QuoteId, out int clientIndex, out int quoteIndex))
                    {
                        return;
                    }

                    ProFitGlobals.userClients[clientIndex].quotes[quoteIndex] = newQuote;

                    Hide();
                },
                onError: (builder, error, jsonValue) =>
                {
                    EventBridge.Menu.showFullScreenSpinner?.Invoke(false);
                    m_MessageBox.Error($"Did not find quote with ID {m_QuoteId}");
                }
            );
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(EditUserProfileUI))]

        [SerializeField] protected MelcherButton m_DeleteButton = null;

        [SerializeField] protected TMP_InputField m_QuoteNameField = null;

        [SerializeField] protected InlineMessageBox m_MessageBox = null;

        [SerializeField] protected MelcherButton m_CancelButton = null;
        [SerializeField] protected MelcherButton m_RenameButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected int m_ClientId;
        protected int m_QuoteId;

    }   // class QuoteInfoUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
