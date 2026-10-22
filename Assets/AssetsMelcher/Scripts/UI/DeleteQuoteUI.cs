namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;
    using MEC;

    /// <summary>
    /// The prompt that shows up to confirm the user wants to delete a quote.
    /// </summary>
    public class DeleteQuoteUI : MelcherUI
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
            m_KeepButton.onClick += OnClickKeep;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_DeleteButton.onClick -= OnClickDelete;
            m_KeepButton.onClick -= OnClickKeep;
        }

        protected override void DoShow()
        {
            base.DoShow();

            if (!ProFitGlobals.TryGetClientAndQuoteIndex(m_ClientId, m_QuoteId, out int clientIndex, out int quoteIndex))
            {
                return;
            }

            string quoteName = ProFitGlobals.userClients[clientIndex].quotes[quoteIndex].saveName;
            m_QuoteNameLabel.phrase = quoteName;
            m_MessageBox.Clear();

            m_CountdownRoutine = Timing.RunCoroutine(CountdownDeleteButton());
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

            ProFitGlobals.server.Quotes.DeleteQuote(
                ProFitGlobals.user,
                m_QuoteId,
                onSuccess: () =>
                {
                    EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                    m_CanvasGroup.interactable = true;

                    Hide();

                    if (!ProFitGlobals.TryGetClientAndQuoteIndex(m_ClientId, m_QuoteId, out int clientIndex, out int quoteIndex))
                    {
                        return;
                    }

                    // Remove the quote from the client's quote list
                    ProFitGlobals.userClients[clientIndex].quotes.RemoveAt(quoteIndex);
                    // Refresh the client list
                    MelcherLog.LogEditorOnly($"Removed quote ID {m_QuoteId} from client #{m_ClientId}");
                    EventBridge.Menu.refreshClientQuoteListUi?.Invoke();
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

        protected IEnumerator<float> CountdownDeleteButton()
        {
            m_DeleteButton.interactable = false;
            int count = 3;

            MelcherLocalizedText buttonLabel = m_DeleteButton.GetComponentInChildren<MelcherLocalizedText>();

            const string DeleteText = "Delete";

            // Wait for some seconds before enabling the button
            for (int i = count; i > 0; --i)
            {
                buttonLabel.phrase = DeleteText + $" ({i})";
                yield return Timing.WaitForSeconds(1.0f);
            }

            m_DeleteButton.interactable = true;
            buttonLabel.phrase = DeleteText;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DeleteClientUI))]

        [SerializeField] protected MelcherLocalizedText m_QuoteNameLabel = null;
        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected MelcherButton m_DeleteButton = null;
        [SerializeField] protected MelcherButton m_KeepButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected CoroutineHandle m_CountdownRoutine;
        protected int m_ClientId;
        protected int m_QuoteId;

    }   // class DeleteQuoteUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
