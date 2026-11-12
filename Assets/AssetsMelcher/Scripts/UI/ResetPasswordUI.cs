namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.Networking;
    using TMPro;
    using KitchenDesign.Server;

    public class ResetPasswordUI : Menu
    {
        private ServerClient _server;

        public void Initialize(ServerClient server)
        {
            this._server = server;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetFields(string email)
        {
            m_EmailField.text = email;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_ResetPasswordButton.onClick += TryResetPassword;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_ResetPasswordButton.onClick -= TryResetPassword;
        }

        protected void TryResetPassword(MelcherButton caller)
        {
            this._server.Accounts.ResetPassword(
                m_EmailField.text,
                OnSuccess,
                OnError
            );

            void OnSuccess()
            {
            }

            void OnError(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherLog.LogError(error);
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ResetPasswordUI))]

        [SerializeField] protected TMP_InputField m_EmailField = null;
        [SerializeField] protected MelcherButton m_ResetPasswordButton = null;

    }   // class ResetPasswordUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
