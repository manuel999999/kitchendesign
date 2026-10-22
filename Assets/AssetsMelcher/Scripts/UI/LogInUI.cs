namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using MEC;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEngine;

    using JsonDict = System.Collections.Generic.IDictionary<string, object>;

    public class LogInUI : Menu
    {
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

            if (ProFitGlobals.logInCoroutineState == LogInState.Running)
            {
                // The previous log in attempt still has a background task
                // running.
                m_MessageBox.Info("Please try again in a moment.");
                return;
            }

            EventBridge.Menu.showFullScreenSpinner.Invoke(true);

            bool offlineMode =
                m_EmailField.text.Equals("guest", StringComparison.OrdinalIgnoreCase) &&
                m_PasswordField.text.Equals("guest", StringComparison.OrdinalIgnoreCase);

            ProFitWeb.LogIn(
                m_EmailField.text,
                m_PasswordField.text,
                offlineMode,
                onSuccess: OnLogInSuccess,
                onError: ErrorHandler
            );

            void OnLogInSuccess(User user)
            {
                ProFitGlobals.user = user;

                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting User Profile");

                // Get the user's profile
                ProFitWeb.GetProfile(
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

                if (userProfile.userFilter != ProFitGlobals.appOptions.userFilter)
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
                User user = ProFitGlobals.user;
                user.userType = userProfile.userType;
                ProFitGlobals.user = user;

                ProFitGlobals.userProfile = userProfile;

                // Show the Pay Now prompt if the user isn't valid
                if (!userProfile.IsSubscriptionOrTrialActive())
                {
                    EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                    EventBridge.Menu.showPayNowPrompt.Invoke(m_EmailField.text, m_PasswordField.text);
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
                Utility.UrlUtility.GetBaseDomain(ProFitGlobals.userProfile.authorizedDomain);

            //MelcherLog.Log("user's authorized domain is: " + authorizedDomain, logLevel: MelcherLog.LogLevel.Always);

            string embeddedAppUrl = MelcherWebGL.GetTopLevelUrl();

            //MelcherLog.Log("app url is: " + embeddedAppUrl, logLevel: MelcherLog.LogLevel.Always);

            if (embeddedAppUrl != null)
            {
                string host = new Uri(embeddedAppUrl).Host;

                //MelcherLog.Log("host is: " + host);

                bool isWhitelisted =
                    ProFitWeb.WhitelistedEmbedDomains.Any(whitelistedDomain => host.EndsWith(whitelistedDomain, StringComparison.OrdinalIgnoreCase));
                //MelcherLog.Log("is whitelisted: " + isWhitelisted, logLevel: MelcherLog.LogLevel.Always);

                bool hasAuthorizedDomain = !string.IsNullOrEmpty(authorizedDomain);
                //MelcherLog.Log("has authorized domain: " + hasAuthorizedDomain, logLevel: MelcherLog.LogLevel.Always);

                bool isAuthorized = hasAuthorizedDomain && host.EndsWith(authorizedDomain, StringComparison.OrdinalIgnoreCase);
                //MelcherLog.Log("is authorized: " + isAuthorized, logLevel: MelcherLog.LogLevel.Always);

                // The hosting site is allowed if it is a whitelisted domain
                // (either a hardcoded domain or one set specifically for the user)
                if (!isWhitelisted && !isAuthorized)
                {
                    EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                    EventBridge.Menu.showUnauthorizedDomainPrompt.Invoke(authorizedDomain);
                    return;
                }
            }
#endif  // if UNITY_WEBGL && !UNITY_EDITOR

            Timing.RunCoroutine(ProFitGlobals.session.FinishLogInCoroutine(rememberMe, fetchClients: true, ErrorHandler));
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
            EventBridge.Menu.pushResetPasswordUi?.Invoke(m_EmailField.text);
        }


        protected void QuickLogIn(string email, string password)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EventBridge.Menu.showLogInUiAndSignIn.Invoke(email, password);
#endif  // if UNITY_EDITOR || DEVELOPMENT_BUILD
        }

        private void ErrorHandler(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
        {
            EventBridge.Menu.showFullScreenSpinner.Invoke(false);
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

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
