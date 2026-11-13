namespace KitchenDesign.UI.MenuScreens
{
    using KitchenDesign.Utility;
    using KitchenDesign.UI.Views;
    using Melcher;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Enums;
    using MEC;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEngine;
    using KitchenDesign.Events;
    using KitchenDesign.Session;
    using KitchenDesign.Scriptables;

    using JsonDict = System.Collections.Generic.IDictionary<string, object>;

    public class LogInUI : Menu
    {
        private SessionStatus _session;
        private ServerClient _server;
        private AppOptionsSO _options;

        public void Initialize(SessionStatus session, ServerClient server, AppOptionsSO options)
        {
            this._session = session;
            this._server = server;
            this._options = options;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Log the user in. This will send the email and password to the
        /// server to verify the credentials. There is a hidden guest account
        /// that can be used by passing guest/guest.
        /// </summary>
        public void LogIn()
        {
            m_MessageBox.Clear();

            if (this._session.LogInState == LogInState.Running)
            {
                // The previous log in attempt still has a background task
                // running.
                m_MessageBox.Info("Please try again in a moment.");
                return;
            }

            EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(true));

            bool offlineMode =
                m_EmailField.text.Equals("guest", StringComparison.OrdinalIgnoreCase) &&
                m_PasswordField.text.Equals("guest", StringComparison.OrdinalIgnoreCase);

            this._server.Accounts.LogIn(
                m_EmailField.text,
                m_PasswordField.text,
                offlineMode,
                onSuccess: OnLogInSuccess,
                onError: ErrorHandler
            );

            void OnLogInSuccess(User user)
            {
                this._session.User = user;

                EventSession.OnSpinnerMessageRequested.Raise(new SpinnerMessageInfo("Requesting User Profile"));

                // Get the user's profile
                this._server.Accounts.GetProfile(
                    user.apiToken,
                    offlineMode,
                    OnProfileSuccess,
                    onError: ErrorHandler
                );
            }

            void OnProfileSuccess(UserProfile userProfile)
            {
                if (userProfile.userType != UserType.Offline &&
                    userProfile.isWhiteLabeled)
                {
                    ErrorHandler(
                        builder: null,
                        error: new Exception("User does not exist or does not have permission."),
                        jsonValue: default
                    );

                    return;
                }

                if (userProfile.userFilter != this._options.userFilter)
                {
                    ErrorHandler(
                        builder: null,
                        error: new Exception("User does not exist or does not have permission."),
                        jsonValue: default
                    );

                    return;
                }

                // Copy the User Type back into the user object. This is
                // convenient because then we can just pass our user object
                // to web API calls and are able to handle guests.
                User user = this._session.User;
                user.userType = userProfile.userType;
                this._session.User = user;

                this._session.UserProfile = userProfile;

                // Show the Pay Now prompt if the user isn't valid
                if (!userProfile.IsSubscriptionOrTrialActive())
                {
                    EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(false));
                    EventMenu.OnPayNowRequested.Raise(new CredentialsInfo(m_EmailField.text, m_PasswordField.text));
                    return;
                }

                FinishLogIn(m_RememberMeCheckbox.isOn);
            }
        }

        public void FinishLogIn(bool rememberMe)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Verify the user has permission to use the app from the current
            // host
            string authorizedDomain =
                Utility.UrlUtility.GetBaseDomain(this._session.UserProfile.authorizedDomain);

            //MelcherLog.Log("user's authorized domain is: " + authorizedDomain, logLevel: MelcherLog.LogLevel.Always);

            string embeddedAppUrl = MelcherWebGL.GetTopLevelUrl();

            //MelcherLog.Log("app url is: " + embeddedAppUrl, logLevel: MelcherLog.LogLevel.Always);

            if (embeddedAppUrl != null)
            {
                string host = new Uri(embeddedAppUrl).Host;

                //MelcherLog.Log("host is: " + host);

                bool isWhitelisted =
                    this._server.Connection.WhitelistedEmbedDomains.Any(whitelistedDomain => host.EndsWith(whitelistedDomain, StringComparison.OrdinalIgnoreCase));
                //MelcherLog.Log("is whitelisted: " + isWhitelisted, logLevel: MelcherLog.LogLevel.Always);

                bool hasAuthorizedDomain = !string.IsNullOrEmpty(authorizedDomain);
                //MelcherLog.Log("has authorized domain: " + hasAuthorizedDomain, logLevel: MelcherLog.LogLevel.Always);

                bool isAuthorized = hasAuthorizedDomain && host.EndsWith(authorizedDomain, StringComparison.OrdinalIgnoreCase);
                //MelcherLog.Log("is authorized: " + isAuthorized, logLevel: MelcherLog.LogLevel.Always);

                // The hosting site is allowed if it is a whitelisted domain
                // (either a hardcoded domain or one set specifically for the user)
                if (!isWhitelisted && !isAuthorized)
                {
                    EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(false));
                    EventMenu.OnUnauthorizedDomainRequested.Raise(new UnauthorizedDomainInfo(authorizedDomain));
                    return;
                }
            }
#endif  // if UNITY_WEBGL && !UNITY_EDITOR

            Timing.RunCoroutine(this._session.FinishLogInCoroutine(rememberMe, fetchClients: true, ErrorHandler));
        }

        public void ClearForm()
        {
            m_EmailField.text = string.Empty;
            m_PasswordField.text = string.Empty;

            m_RevealPasswordButton.SetSecure(true);

            m_MessageBox.Clear();
        }

        public void SetFields(string email, string password)
        {
            m_EmailField.text = email;
            m_PasswordField.text = password;
        }

        protected override void Awake()
        {
            base.Awake();

            m_QuickLogInRoot.gameObject.SetActive(Debug.isDebugBuild);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_LogInButton.onClick += TryLogIn;
            m_ForgotPasswordButton.onClick += OnForgotPassword;
            QuickLogInWidget.onQuickLogIn += QuickLogIn;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_LogInButton.onClick -= TryLogIn;
            m_ForgotPasswordButton.onClick -= OnForgotPassword;
            QuickLogInWidget.onQuickLogIn -= QuickLogIn;
        }

        protected override void DoShow()
        {
            // Hide the password field when returning to not expose the user's details
            m_PasswordField.contentType = TMP_InputField.ContentType.Password;
            m_PasswordField.ForceLabelUpdate();

            base.DoShow();
        }

        protected void TryLogIn(MelcherButton caller)
        {
            LogIn();
        }

        protected void OnForgotPassword(MelcherButton caller)
        {
            EventMenu.OnResetPasswordRequested.Raise(new ResetPasswordInfo(m_EmailField.text));
        }


        protected void QuickLogIn(string email, string password)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EventMenu.OnSignInRequested.Raise(new CredentialsInfo(email, password));
#endif  // if UNITY_EDITOR || DEVELOPMENT_BUILD
        }

        private void ErrorHandler(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
        {
            EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(false));
            m_MessageBox.ErrorFromWebRequest(builder, error, jsonValue);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(LogInUI))]

        [SerializeField] protected TMP_InputField m_EmailField = null;
        [SerializeField] protected TMP_InputField m_PasswordField = null;
        [SerializeField] protected RevealPasswordButton m_RevealPasswordButton = null;
        [SerializeField] protected MelcherToggle m_RememberMeCheckbox = null;
        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected MelcherButton m_LogInButton = null;
        [SerializeField] protected MelcherButton m_ForgotPasswordButton = null;

        [Space]

        [SerializeField] protected RectTransform m_QuickLogInRoot = null;

    }   // class LogInUI

}   // namespace KitchenDesign.UI.MenuScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
