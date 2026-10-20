namespace Melcher
{
    using System.Collections.Generic;
    using UnityEngine;
    using MEC;

    /// <summary>
    /// The prompt that shows up to confirm the user wants to delete a client.
    /// </summary>
    public class DeleteClientUI : MelcherUI
    {
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

            if (ProFitGlobals.TryGetClientIndex(m_ClientId, out int clientIndex))
            {
                UserClient client = ProFitGlobals.userClients[clientIndex];

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
            EventBridge.Menu.showFullScreenSpinner.Invoke(true);
            m_MessageBox.Clear();

            ProFitWeb.DeleteClient(
                ProFitGlobals.user,
                m_ClientId,
                onSuccess: () =>
                {
                    EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                    m_CanvasGroup.interactable = true;
                    ProFitGlobals.activeClientId = null; // TODO: Necessary anymore? Maybe let the ClientListUI clear the activeClientId

                    Hide();

                    if (ProFitGlobals.TryGetClientIndex(m_ClientId, out int clientIndex))
                    {
                        ProFitGlobals.userClients.RemoveAt(clientIndex);
                    }

                    EventBridge.Menu.showClientListUi.Invoke();
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

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
