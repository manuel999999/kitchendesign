namespace Melcher
{
    using UnityEngine;
    using UnityEngine.Networking;
    using TMPro;

    public class ResetPasswordUI : Menu
    {
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
            ProFitWeb.ResetPassword(
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

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
