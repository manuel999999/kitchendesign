namespace KitchenDesign.UI.MenuScreens
{
    using KitchenDesign.UI.Views;
    using Melcher;
    using KitchenDesign.Server;
    using System.Collections.Generic;
    using UnityEngine;
    using MEC;
    using KitchenDesign.Events;
    using KitchenDesign.Session;

    /// <summary>
    /// The prompt that shows up to confirm the user wants to delete a client.
    /// </summary>
    public class DeleteClientUI : MelcherUI
    {
        private SessionStatus _session;
        private ClientListStatus _clientList;
        private ServerClient _server;

        public void Initialize(SessionStatus session, ClientListStatus clientList, ServerClient server)
        {
            this._session = session;
            this._clientList = clientList;
            this._server = server;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetFields(int clientId)
        {
            m_ClientId = clientId;
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

            string clientName = "Unknown";

            if (this._clientList.TryGetClientIndex(m_ClientId, out int clientIndex))
            {
                UserClient client = this._clientList.UserClients[clientIndex];

                clientName = string.Join(" ", client.firstName, client.lastName);
            }

            m_TitleLabel.phrase = $"Delete \"{clientName}\"?";
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
            EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(true));
            m_MessageBox.Clear();

            this._server.Clients.DeleteClient(
                this._session.User,
                m_ClientId,
                onSuccess: () =>
                {
                    EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(false));
                    m_CanvasGroup.interactable = true;
                    this._clientList.ActiveClientId = null; // TODO: Necessary anymore? Maybe let the ClientListUI clear the activeClientId

                    Hide();

                    if (this._clientList.TryGetClientIndex(m_ClientId, out int clientIndex))
                    {
                        this._clientList.UserClients.RemoveAt(clientIndex);
                    }

                    EventMenu.OnClientListRequested.Raise();
                },
                onError: (builder, error, jsonValue) =>
                {
                    EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(false));

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

        [SerializeField] protected MelcherLocalizedText m_TitleLabel = null;
        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected MelcherButton m_DeleteButton = null;
        [SerializeField] protected MelcherButton m_KeepButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected CoroutineHandle m_CountdownRoutine;
        protected int m_ClientId;

    }   // class DeleteClientUI

}   // namespace KitchenDesign.UI.MenuScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
