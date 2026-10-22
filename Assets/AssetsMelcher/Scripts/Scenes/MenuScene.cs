namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using System.Collections.Generic;
    using UnityEngine;

    public class MenuScene : MelcherStateMachineState
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public override void OnStateAdded(IMelcherStateMachineState fromState)
        {
            // Reset the last selected user and quote to avoid dangling
            // references to users who may not exist or be our own
            ProFitGlobals.activeClientId = null;
            ProFitGlobals.activeQuoteId = null;
            ProFitGlobals.activeClientWantsToBeContacted = null;

            if (!string.IsNullOrEmpty(ProFitGlobals.user.apiToken))
            {
                if (ProFitGlobals.userProfile.IsTrialActive())
                {
                    // Trial users see a countdown
                    EventBridge.Menu.showTrialCountdownUi.Invoke();
                }

                // If the user is returning to the main menu from elsewhere
                // in the game, jump back to the home screen
                ShowHome();
                return;
            }

            // Check if the player has a saved session ID. This happens when
            // "Remember Me" is checked on the log in screen and the user logs
            // in
            string apiToken = PlayerPrefs.GetString(Const.PlayerPrefs_Token, null);
            if (string.IsNullOrEmpty(apiToken))
            {
                // No saved data, prompt for sign up or log in
                ShowWelcome(MelcherUITransitionStyle.Progress);
                return;
            }

            EventBridge.Menu.showFullScreenSpinner?.Invoke(true);
            ProFitGlobals.ExecuteWhenReady(TryLogInWithSavedToken);

            void TryLogInWithSavedToken()
            {
                // Try to log in with old token. If this fails, fallback to sign up/
                // log in screen
                MelcherLog.LogEditorOnly("Trying to log in with session in PlayerPrefs");
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting User Profile");
                ProFitGlobals.server.Accounts.GetProfile(
                    apiToken,
                    isOffline: false,
                    onSuccess: userProfile =>
                    {
                        User user = new User()
                        {
                            apiToken    = apiToken,
                            id          = userProfile.userId,
                            userType    = userProfile.userType
                        };

                        if (!userProfile.IsSubscriptionOrTrialActive())
                        {
                            ProFitGlobals.ForgetRememberMeData();

                            OnError(builder: null, new System.Exception("User is pending or isn't valid."), jsonValue: default);
                            return;
                        }

                        ProFitGlobals.user = user;
                        ProFitGlobals.userProfile = userProfile;

                        m_LogInUi.FinishLogIn(rememberMe: true);
                    },
                    onError: OnError
                );
            }

            void OnError(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.showFullScreenSpinner?.Invoke(false);
                ShowWelcome(MelcherUITransitionStyle.Progress);
            }
        }

        protected void Start()
        {
            m_UseLandscapeOrientation = true;

            if (Application.isMobilePlatform)
            {
                m_UseLandscapeOrientation =
                    PlayerPrefs.GetInt(PlayerPref.AlwaysLandscapeOnMobile) != 0;
            }

            RefreshScreenOrientation();

            ProFitGlobals.ExecuteWhenReady(OnGameDataReady);
        }

        protected void OnGameDataReady()
        {
#if UNITY_EDITOR
            MelcherLog.LogWarningEditorOnly("Forcing white label mode to be off this session because the user is in the menu scene.");
            ProFitGlobals.appOptions.simulateWhiteLabelMode = false;
#endif  // if UNITY_EDITOR
        }

        protected void OnEnable()
        {
            EventBridge.Menu.showMainUi             += ShowMenu;
            EventBridge.Menu.hideMainUi             += HideMenu;
            EventBridge.Menu.showFullScreenSpinner  += ShowFullScreenSpinner;
            EventBridge.Menu.showBackgroundSpinner  += ShowBackgroundSpinner;
            EventBridge.Menu.setSpinnerStatusMessage+= SetSpinnerStatusMessage;
            EventBridge.Menu.showWelcomeUi          += ShowWelcome;
            EventBridge.Menu.pushSignUpUi           += PushSignUpUI;
            EventBridge.Menu.pushLogInUi            += PushLogInUI;
            EventBridge.Menu.showLogInUiAndSignIn   += ShowLogInAndSignIn;
            EventBridge.Menu.showDeleteAccountUi    += ShowDeleteAccountUI;
            EventBridge.Menu.pushResetPasswordUi    += PushResetPasswordUI;
            EventBridge.Menu.showPayNowPrompt       += ShowPayNowPrompt;
            EventBridge.Menu.showUnauthorizedDomainPrompt += ShowUnauthorizedDomainPrompt;
            EventBridge.Menu.showUserKickedPrompt   += ShowUserKickedPrompt;
            EventBridge.Menu.showTrialCountdownUi   += ShowTrialUI;
            EventBridge.Menu.hideTrialCountdownUi   += HideTrialUI;
            EventBridge.Menu.showHomeUi             += ShowHome;
            EventBridge.Menu.pushCreateClientUi     += PushCreateClientUI;
            EventBridge.Menu.showClientListUi       += ShowClientListUI;
            EventBridge.Menu.pushClientInfoUi       += PushClientProfileUI;
            EventBridge.Menu.showDeleteClientUi     += ShowDeleteClientUi;
            EventBridge.Menu.showQuoteInfoUi        += ShowQuoteInfoUI;
            EventBridge.Menu.showDeleteQuoteUi      += ShowDeleteQuoteUI;
            EventBridge.Menu.pushClientQuoteListUi  += PushClientQuoteListUI;
            EventBridge.Menu.refreshClientQuoteListUi += RefreshClientQuoteListUI;
            EventBridge.Menu.showUserProfileUi      += ShowUserProfileUI;
            EventBridge.Menu.pushManufacturerWhitelistUi += PushManufacturerWhitelistUI;
            EventBridge.Menu.showUserLogOutPrompt   += ShowUserLogOutPrompt;
            EventBridge.Menu.setMenuHasPendingChanges += SetMenuHasPendingChanges;

            EventBridge.Menu.navigateBackward += PopMenu;
            EventBridge.Menu.toggleScreenOrientation += ToggleScreenOrientation;
            EventBridge.Menu.showQuitPrompt += ShowQuitPrompt;
        }

        protected void OnDisable()
        {
            EventBridge.Menu.showMainUi             -= ShowMenu;
            EventBridge.Menu.hideMainUi             -= HideMenu;
            EventBridge.Menu.showFullScreenSpinner  -= ShowFullScreenSpinner;
            EventBridge.Menu.showBackgroundSpinner  -= ShowBackgroundSpinner;
            EventBridge.Menu.setSpinnerStatusMessage-= SetSpinnerStatusMessage;
            EventBridge.Menu.showWelcomeUi          -= ShowWelcome;
            EventBridge.Menu.pushSignUpUi           -= PushSignUpUI;
            EventBridge.Menu.pushLogInUi            -= PushLogInUI;
            EventBridge.Menu.showLogInUiAndSignIn   -= ShowLogInAndSignIn;
            EventBridge.Menu.showDeleteAccountUi    -= ShowDeleteAccountUI;
            EventBridge.Menu.pushResetPasswordUi    -= PushResetPasswordUI;
            EventBridge.Menu.showPayNowPrompt       -= ShowPayNowPrompt;
            EventBridge.Menu.showUnauthorizedDomainPrompt -= ShowUnauthorizedDomainPrompt;
            EventBridge.Menu.showUserKickedPrompt   -= ShowUserKickedPrompt;
            EventBridge.Menu.showTrialCountdownUi   -= ShowTrialUI;
            EventBridge.Menu.hideTrialCountdownUi   -= HideTrialUI;
            EventBridge.Menu.showHomeUi             -= ShowHome;
            EventBridge.Menu.pushCreateClientUi     -= PushCreateClientUI;
            EventBridge.Menu.showClientListUi       -= ShowClientListUI;
            EventBridge.Menu.pushClientInfoUi       -= PushClientProfileUI;
            EventBridge.Menu.showDeleteClientUi     -= ShowDeleteClientUi;
            EventBridge.Menu.showQuoteInfoUi        -= ShowQuoteInfoUI;
            EventBridge.Menu.showDeleteQuoteUi      -= ShowDeleteQuoteUI;
            EventBridge.Menu.pushClientQuoteListUi  -= PushClientQuoteListUI;
            EventBridge.Menu.refreshClientQuoteListUi -= RefreshClientQuoteListUI;
            EventBridge.Menu.showUserProfileUi      -= ShowUserProfileUI;
            EventBridge.Menu.pushManufacturerWhitelistUi -= PushManufacturerWhitelistUI;
            EventBridge.Menu.showUserLogOutPrompt   -= ShowUserLogOutPrompt;
            EventBridge.Menu.setMenuHasPendingChanges -= SetMenuHasPendingChanges;

            EventBridge.Menu.navigateBackward -= PopMenu;
            EventBridge.Menu.toggleScreenOrientation -= ToggleScreenOrientation;
            EventBridge.Menu.showQuitPrompt -= ShowQuitPrompt;
        }

        protected void ShowMenu()
        {
            m_MainUi.Show();
        }

        protected void HideMenu()
        {
            m_MainUi.Hide();
        }

        protected void ShowFullScreenSpinner(bool show)
        {
            if (show)
            {
                m_FullScreenLoadingUi.Show();
            }
            else
            {
                m_FullScreenLoadingUi.Hide();
            }
        }

        protected void ShowBackgroundSpinner(bool show)
        {
            if (show)
            {
                m_BackgroundLoadingUi.Show();
            }
            else
            {
                m_BackgroundLoadingUi.Hide();
            }
        }

        protected void SetSpinnerStatusMessage(string statusMessage)
        {
            m_FullScreenLoadingUi.SetStatusMessage(statusMessage);
            m_BackgroundLoadingUi.SetStatusMessage(statusMessage);
        }

        protected void ShowWelcome(MelcherUITransitionStyle transitionStyle)
        {
            // Hide the footer
            m_MainUi.ShowFooter(false);

            MelcherUITransition transition = MelcherUITransition.Default.WithStyle(transitionStyle);
            ResetHistory(m_WelcomeUi, transition);
            m_WelcomeUi.Show(transition);
        }

        protected void PushSignUpUI(UserType userType)
        {
            m_SignUpUi.SetUserType(userType);
            PushMenu(m_SignUpUi, transition: null);
        }

        protected void PushLogInUI()
        {
            PushMenu(m_LogInUi, transition: null);
        }

        /// <summary>
        /// Open the log in page and try to sign the user in.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        protected void ShowLogInAndSignIn(string email, string password)
        {
            m_LogInUi.SetFields(email, password);

            if (m_MenuHistory.Peek() != m_LogInUi)
            {
                // Prep the menu history as if we went straight to the log in page
                ResetHistory(m_WelcomeUi);
                m_MenuHistory.Push(m_LogInUi);
                m_MainUi.ShowBackButton(true);
                m_LogInUi.Show();
            }

            m_LogInUi.LogIn();
        }

        protected void ShowDeleteAccountUI()
        {
            m_DeleteAccountUi.Show();
        }

        protected void PushResetPasswordUI(string email)
        {
            m_ResetPasswordUi.SetFields(email);
            PushMenu(m_ResetPasswordUi, transition: null);
        }

        protected void ShowPayNowPrompt(string email, string password)
        {
            m_PayNowUi.SetFields(email, password);
            m_PayNowUi.Show();
        }

        protected void HidePayNowPrompt()
        {
            m_PayNowUi.Hide();
        }

        protected void ShowUnauthorizedDomainPrompt(string authorizedDomain)
        {
            m_UnauthorizedDomainUi.SetFields(authorizedDomain);
            m_UnauthorizedDomainUi.Show();
        }

        /// <summary>
        /// Show a prompt to inform the user of why they were locked out. This
        /// listener is triggered when the user's trial expires.
        /// </summary>
        /// <param name="kickReason">
        /// A message explaining why the user was locked out.
        /// </param>
        protected void ShowUserKickedPrompt(string kickReason)
        {
            m_KickedUserPromptUi.SetReason(kickReason);
            m_KickedUserPromptUi.Show();
        }

        protected void ShowTrialUI()
        {
            m_TrialCountdownUi.Show();
        }

        protected void HideTrialUI()
        {
            m_TrialCountdownUi.Hide();
        }

        protected void ShowHome()
        {
            MelcherUI currentUi = null;
            if (m_MenuHistory.Count > 0)
            {
                currentUi = m_MenuHistory.Peek();
            }

            if (currentUi != m_HomeUi)
            {
                // If we're coming from any page but the log in page, use a regress animation
                MelcherUITransition transition = MelcherUITransition.Default;
                if (currentUi != m_LogInUi)
                {
                    transition = transition.WithStyle(MelcherUITransitionStyle.Regress);
                }

                ResetHistory(m_HomeUi, transition);
                m_HomeUi.Show(transition);
            }

            m_MainUi.ShowFooter(true);
            m_MainUi.SelectHomeTab();
        }

        protected void PushCreateClientUI()
        {
            // For client's privacy, clear the form whenever it is opened
            // TODO: Use a timer?
            m_CreateClientUi.ClearForm();
            PushMenu(m_CreateClientUi, transition: null);
        }

        protected void ShowClientListUI()
        {
            EventBridge.Menu.verifyKioskPermission?.Invoke(
                onSuccess: () =>
                {
                    MelcherUI currentUi = null;
                    if (m_MenuHistory.Count > 0)
                    {
                        currentUi = m_MenuHistory.Peek();
                    }

                    if (currentUi != m_ClientListUi)
                    {
                        MelcherUITransition transition = MelcherUITransition.Default;
                        if (currentUi != m_HomeUi)
                        {
                            transition = transition.WithStyle(MelcherUITransitionStyle.Regress);
                        }

                        ResetHistory(m_ClientListUi, transition);
                        m_ClientListUi.Show(transition);
                    }

                    m_ClientListUi.Refresh();

                    m_MainUi.ShowFooter(true);
                    m_MainUi.SelectClientsTab();
                }
            );

        }

        protected void PushClientProfileUI(int clientId)
        {
            m_ClientProfileUi.SetFields(clientId);
            PushMenu(m_ClientProfileUi, transition: null);
        }

        protected void ShowDeleteClientUi(int clientId)
        {
            m_DeleteClientUi.SetFields(clientId);
            m_DeleteClientUi.Show();
        }

        protected void ShowQuoteInfoUI(int clientId, int quoteId)
        {
            m_QuoteInfoUi.SetFields(clientId, quoteId);
            m_QuoteInfoUi.Show();
        }

        protected void ShowDeleteQuoteUI(int clientId, int quoteId)
        {
            m_DeleteQuoteUi.SetFields(clientId, quoteId);
            m_DeleteQuoteUi.Show();
        }

        protected void PushClientQuoteListUI()
        {
            m_ClientQuoteListUi.Refresh();
            PushMenu(m_ClientQuoteListUi, transition: null);
        }

        protected void RefreshClientQuoteListUI()
        {
            m_ClientQuoteListUi.Refresh();
        }

        protected void ShowUserProfileUI()
        {
            EventBridge.Menu.verifyKioskPermission?.Invoke(
                onSuccess: () =>
                {
                    MelcherUI currentUi = null;
                    if (m_MenuHistory.Count > 0)
                    {
                        currentUi = m_MenuHistory.Peek();
                    }

                    if (currentUi != m_UserProfileUi)
                    {
                        // If we're coming from any page but the home or design page, use a regress animation
                        MelcherUITransition transition = MelcherUITransition.Default;
                        if (currentUi != m_HomeUi && currentUi != m_ClientListUi)
                        {
                            transition = transition.WithStyle(MelcherUITransitionStyle.Regress);
                        }

                        ResetHistory(m_UserProfileUi, transition);
                        m_UserProfileUi.Show(transition);
                    }

                    m_UserProfileUi.Refresh();

                    m_MainUi.ShowFooter(true);
                    m_MainUi.SelectUserProfileTab();
                }
            );
        }

        protected void ShowUserLogOutPrompt()
        {
            m_UserLogOutPromptUi.Show();
        }

        protected void PushManufacturerWhitelistUI()
        {
            m_ManufacturerWhitelistUi.SetSelectionFromProfile();
            PushMenu(m_ManufacturerWhitelistUi, transition: null);
        }

        protected void PushMenu(MelcherUI nextUi, MelcherUITransition? transition)
        {
            if (m_MenuHistory.Count > 0)
            {
                m_MenuHistory.Peek().Hide(transition);
            }

            m_MenuHistory.Push(nextUi);
            nextUi.Show(transition);

            m_MainUi.ShowBackButton(m_MenuHistory.Count > 1);
        }

        protected void SetMenuHasPendingChanges(MelcherUI menu, bool hasPendingChanges)
        {
            if (hasPendingChanges)
            {
                m_HasPendingChanges.Add(menu);
            }
            else
            {
                m_HasPendingChanges.Remove(menu);
            }
        }

        protected void PopMenu()
        {
            if (m_MenuHistory.Count > 1)
            {
                MelcherUI currentMenu = m_MenuHistory.Peek();

                if (m_HasPendingChanges.Contains(currentMenu))
                {
                    // Prompt the user if the changes should be discarded.
                    m_DiscardPendingChangesUi.SetCallback(
                        discard =>
                        {
                            if (discard)
                            {
                                m_HasPendingChanges.Remove(currentMenu);
                                PopMenu();
                            }
                        }
                    );

                    m_DiscardPendingChangesUi.Show();
                }
                else
                {
                    MelcherUITransition transition = MelcherUITransition.Default.WithStyle(MelcherUITransitionStyle.Regress);
                    m_MenuHistory.Pop().Hide(transition);
                    m_MenuHistory.Peek().Show(transition);

                    m_MainUi.ShowBackButton(m_MenuHistory.Count > 1);
                }
            }
        }

        /// <summary>
        ///     Invoked when the user clicks the toggle screen orientation
        ///     button on the menu scene's header. Intended to update the app's
        ///     local orientation setting (i.e., it's not tied to a specific
        ///     account) and then refresh the screen's orientation to match.
        /// </summary>
        protected void ToggleScreenOrientation()
        {
            m_UseLandscapeOrientation = !m_UseLandscapeOrientation;

            PlayerPrefs.SetInt(PlayerPref.AlwaysLandscapeOnMobile, m_UseLandscapeOrientation ? 1 : 0);
            PlayerPrefs.Save();

            RefreshScreenOrientation();
        }
        protected void RefreshScreenOrientation()
        {
            // Apply screen orientation
            Screen.autorotateToPortrait = !m_UseLandscapeOrientation;
            Screen.autorotateToLandscapeRight = m_UseLandscapeOrientation;
            Screen.autorotateToLandscapeLeft = m_UseLandscapeOrientation;
            Screen.autorotateToPortraitUpsideDown = false;

            if (m_UseLandscapeOrientation)
            {
                if (Screen.orientation != ScreenOrientation.LandscapeLeft && Screen.orientation != ScreenOrientation.LandscapeRight)
                {
                    Screen.orientation = ScreenOrientation.LandscapeLeft;
                }
            }
            else
            {
                Screen.orientation = ScreenOrientation.Portrait;
            }
        }


        protected void ShowQuitPrompt()
        {
            m_QuitAppUi.Show();
        }

        /// <summary>
        /// Hide the topmost UI, remove everything from the history stack and
        /// then push the new root UI. Importantly, this does NOT show the new
        /// root UI.
        /// </summary>
        protected void ResetHistory(MelcherUI rootUi, MelcherUITransition? transition = null)
        {
            m_MainUi.ShowBackButton(false);

            if (m_MenuHistory.Count > 0)
            {
                m_MenuHistory.Peek().Hide(transition);
                while (m_MenuHistory.Count > 0)
                {
                    m_MenuHistory.Pop();
                }
            }

            m_HasPendingChanges.Clear();

            m_MenuHistory.Push(rootUi);
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        private LoadingUI m_FullScreenLoadingUi = null;

        [SerializeField]
        private LoadingUI m_BackgroundLoadingUi = null;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private MainUI m_MainUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private WelcomeUI m_WelcomeUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private SignUpUI m_SignUpUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private LogInUI m_LogInUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private ResetPasswordUI m_ResetPasswordUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private DeleteAccountUI m_DeleteAccountUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private QuitAppUI m_QuitAppUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private PayNowUI m_PayNowUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private UnauthorizedDomainUI m_UnauthorizedDomainUi = null;

        /// <summary>
        /// A UI prompt to show when a user is forcibly logged out of their account.
        /// </summary>
        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private KickedUserPromptUI m_KickedUserPromptUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private TrialCountdownUI m_TrialCountdownUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private HomeUI m_HomeUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private CreateClientUI m_CreateClientUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private ClientListUI m_ClientListUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private ClientProfileUI m_ClientProfileUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private DeleteClientUI m_DeleteClientUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private ClientQuoteListUI m_ClientQuoteListUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private QuoteInfoUI m_QuoteInfoUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private DeleteQuoteUI m_DeleteQuoteUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private UserProfileUI m_UserProfileUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private ManufacturerWhitelistUI m_ManufacturerWhitelistUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private DiscardPendingChangesUI m_DiscardPendingChangesUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private UserLogOutPromptUI m_UserLogOutPromptUi = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        private Stack<MelcherUI> m_MenuHistory = new Stack<MelcherUI>();

        /// <summary>
        ///     These UI menus have changes that haven't been saved yet. The
        ///     user will see a prompt if they try to navigate backwards from
        ///     one of these menus.
        /// </summary>
        private HashSet<MelcherUI> m_HasPendingChanges = new ();

        /// <summary>
        ///     Whether the app will use landscape orientation in the menu.
        ///     This is only used in mobile builds because other platforms are
        ///     permanently in landscape orientation.
        /// </summary>
        protected bool m_UseLandscapeOrientation;

        // ====================================================================
        // Nested types
        // ====================================================================

        public static class PlayerPref
        {
            public const string AlwaysLandscapeOnMobile = nameof(MainUI) + "." + nameof(AlwaysLandscapeOnMobile);
        }   // class PlayerPref


    }   // class MenuScene

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
