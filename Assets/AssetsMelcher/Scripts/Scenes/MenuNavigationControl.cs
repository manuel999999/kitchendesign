namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using System.Collections.Generic;
    using UnityEngine;
    using KitchenDesign.Events;

    public class MenuNavigationControl : MelcherMonoBehaviour
    {
        private AppSystems _systems;

        [SerializeField] private SortModeUI sortModePrompt;
        [SerializeField] private KioskPinUI kioskPinPrompt;
        [SerializeField] private EditUserProfileUI editProfilePrompt;
        [SerializeField] private EditUserProfileMultipleChoiceUI multipleChoicePrompt;

        // =====================================================================
        // Methods
        // =====================================================================

        public void Initialize(AppSystems systems)
        {
            this._systems = systems;

            m_SignUpUi.Initialize(systems.Server, systems.Options);
            m_LogInUi.Initialize(systems.Session, systems.Server, systems.Options);
            m_ResetPasswordUi.Initialize(systems.Server);
            m_DeleteAccountUi.Initialize(systems.Session, systems.Server);
            m_PayNowUi.Initialize(systems.Session, systems.Server, systems.Options);
            m_CreateClientUi.Initialize(systems.Session, systems.ClientList, systems.Server);
            m_ClientListUi.Initialize(systems.Session, systems.ClientList, systems.Server, this.sortModePrompt);
            m_ClientProfileUi.Initialize(systems.Session, systems.ClientList, systems.Server, this.editProfilePrompt);
            m_DeleteClientUi.Initialize(systems.Session, systems.ClientList, systems.Server);
            m_ClientQuoteListUi.Initialize(systems.Session, systems.ClientList, systems.Server, this.sortModePrompt);
            m_QuoteInfoUi.Initialize(systems.Session, systems.ClientList, systems.Server);
            m_DeleteQuoteUi.Initialize(systems.Session, systems.ClientList, systems.Server);
            m_UserProfileUi.Initialize(systems.Session, systems.Server, this.editProfilePrompt, this.multipleChoicePrompt, this.kioskPinPrompt);
            m_ManufacturerWhitelistUi.Initialize(systems.Session, systems.CatalogData, systems.Server, systems.Options, systems.Rules, hasPendingChanges => this.SetMenuHasPendingChanges(m_ManufacturerWhitelistUi, hasPendingChanges));
            m_UserLogOutPromptUi.Initialize(systems.Session);
            this.kioskPinPrompt.Initialize(systems.Session);

            // Reset the last selected user and quote to avoid dangling
            // references to users who may not exist or be our own
            this._systems.ClientList.ActiveClientId = null;
            this._systems.ClientList.ActiveQuoteId = null;
            this._systems.ClientList.ActiveClientWantsToBeContacted = null;

            if (!string.IsNullOrEmpty(this._systems.Session.User.apiToken))
            {
                if (this._systems.Session.UserProfile.IsTrialActive())
                {
                    // Trial users see a countdown
                    this.ShowTrialUI();
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

            this.ShowFullScreenSpinner(true);
            this._systems.CatalogData.ExecuteWhenLoaded(TryLogInWithSavedToken);

            void TryLogInWithSavedToken()
            {
                // Try to log in with old token. If this fails, fallback to sign up/
                // log in screen
                MelcherLog.LogEditorOnly("Trying to log in with session in PlayerPrefs");
                this.SetSpinnerStatusMessage("Requesting User Profile");
                this._systems.Server.Accounts.GetProfile(
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
                            this._systems.Session.ForgetRememberMeData();

                            OnError(builder: null, new System.Exception("User is pending or isn't valid."), jsonValue: default);
                            return;
                        }

                        this._systems.Session.User = user;
                        this._systems.Session.UserProfile = userProfile;

                        m_LogInUi.FinishLogIn(rememberMe: true);
                    },
                    onError: OnError
                );
            }

            void OnError(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                this.ShowFullScreenSpinner(false);
                ShowWelcome(MelcherUITransitionStyle.Progress);
            }
        }

        protected void Start()
        {
            m_UseLandscapeOrientation = true;

            if (Application.isMobilePlatform)
            {
                m_UseLandscapeOrientation =
                    PlayerPrefs.GetInt(MenuPreferenceKeys.AlwaysLandscapeOnMobile) != 0;
            }

            RefreshScreenOrientation();

            this._systems.CatalogData.ExecuteWhenLoaded(OnGameDataReady);
        }

        protected void OnGameDataReady()
        {
#if UNITY_EDITOR
            MelcherLog.LogWarningEditorOnly("Forcing white label mode to be off this session because the user is in the menu scene.");
            this._systems.Options.simulateWhiteLabelMode = false;
#endif  // if UNITY_EDITOR
        }

        protected void OnEnable()
        {


            EventSession.OnSpinnerMessageRequested.AddListener(this.OnSpinnerMessageRequested);
            EventSession.OnFullScreenSpinnerRequested.AddListener(this.OnFullScreenSpinnerRequested);
            EventSession.OnBackgroundSpinnerRequested.AddListener(this.OnBackgroundSpinnerRequested);
            EventSession.OnHomeRequested.AddListener(this.ShowHome);
            EventSession.OnWelcomeRequested.AddListener(this.OnWelcomeRequested);
            EventSession.OnLogInRequested.AddListener(this.PushLogInUI);
            EventSession.OnTrialCountdownRequested.AddListener(this.ShowTrialUI);
            EventSession.OnLoggedOut.AddListener(this.HideTrialUI);
            EventSession.OnSubscriptionActivated.AddListener(this.HideTrialUI);
            EventSession.OnUserKickedPromptRequested.AddListener(this.OnUserKickedPromptRequested);

            EventMenu.OnSignUpRequested.AddListener(this.OnSignUpRequested);
            EventMenu.OnSignInRequested.AddListener(this.OnSignInRequested);
            EventMenu.OnDeleteAccountRequested.AddListener(this.ShowDeleteAccountUI);
            EventMenu.OnResetPasswordRequested.AddListener(this.OnResetPasswordRequested);
            EventMenu.OnPayNowRequested.AddListener(this.OnPayNowRequested);
            EventMenu.OnUnauthorizedDomainRequested.AddListener(this.OnUnauthorizedDomainRequested);
            EventMenu.OnCreateClientRequested.AddListener(this.PushCreateClientUI);
            EventMenu.OnClientListRequested.AddListener(this.ShowClientListUI);
            EventMenu.OnClientInfoRequested.AddListener(this.OnClientInfoRequested);
            EventMenu.OnDeleteClientRequested.AddListener(this.OnDeleteClientRequested);
            EventMenu.OnClientQuoteListRequested.AddListener(this.PushClientQuoteListUI);
            EventMenu.OnClientQuoteListRefreshRequested.AddListener(this.RefreshClientQuoteListUI);
            EventMenu.OnQuoteInfoRequested.AddListener(this.OnQuoteInfoRequested);
            EventMenu.OnDeleteQuoteRequested.AddListener(this.OnDeleteQuoteRequested);
            EventMenu.OnUserProfileRequested.AddListener(this.ShowUserProfileUI);
            EventMenu.OnManufacturerWhitelistRequested.AddListener(this.PushManufacturerWhitelistUI);
            EventMenu.OnLogOutPromptRequested.AddListener(this.ShowUserLogOutPrompt);
            EventMenu.OnBackRequested.AddListener(this.PopMenu);
            EventMenu.OnScreenOrientationToggleRequested.AddListener(this.ToggleScreenOrientation);
            EventMenu.OnQuitPromptRequested.AddListener(this.ShowQuitPrompt);
        }

        protected void OnDisable()
        {


            EventSession.OnSpinnerMessageRequested.RemoveListener(this.OnSpinnerMessageRequested);
            EventSession.OnFullScreenSpinnerRequested.RemoveListener(this.OnFullScreenSpinnerRequested);
            EventSession.OnBackgroundSpinnerRequested.RemoveListener(this.OnBackgroundSpinnerRequested);
            EventSession.OnHomeRequested.RemoveListener(this.ShowHome);
            EventSession.OnWelcomeRequested.RemoveListener(this.OnWelcomeRequested);
            EventSession.OnLogInRequested.RemoveListener(this.PushLogInUI);
            EventSession.OnTrialCountdownRequested.RemoveListener(this.ShowTrialUI);
            EventSession.OnLoggedOut.RemoveListener(this.HideTrialUI);
            EventSession.OnSubscriptionActivated.RemoveListener(this.HideTrialUI);
            EventSession.OnUserKickedPromptRequested.RemoveListener(this.OnUserKickedPromptRequested);

            EventMenu.OnSignUpRequested.RemoveListener(this.OnSignUpRequested);
            EventMenu.OnSignInRequested.RemoveListener(this.OnSignInRequested);
            EventMenu.OnDeleteAccountRequested.RemoveListener(this.ShowDeleteAccountUI);
            EventMenu.OnResetPasswordRequested.RemoveListener(this.OnResetPasswordRequested);
            EventMenu.OnPayNowRequested.RemoveListener(this.OnPayNowRequested);
            EventMenu.OnUnauthorizedDomainRequested.RemoveListener(this.OnUnauthorizedDomainRequested);
            EventMenu.OnCreateClientRequested.RemoveListener(this.PushCreateClientUI);
            EventMenu.OnClientListRequested.RemoveListener(this.ShowClientListUI);
            EventMenu.OnClientInfoRequested.RemoveListener(this.OnClientInfoRequested);
            EventMenu.OnDeleteClientRequested.RemoveListener(this.OnDeleteClientRequested);
            EventMenu.OnClientQuoteListRequested.RemoveListener(this.PushClientQuoteListUI);
            EventMenu.OnClientQuoteListRefreshRequested.RemoveListener(this.RefreshClientQuoteListUI);
            EventMenu.OnQuoteInfoRequested.RemoveListener(this.OnQuoteInfoRequested);
            EventMenu.OnDeleteQuoteRequested.RemoveListener(this.OnDeleteQuoteRequested);
            EventMenu.OnUserProfileRequested.RemoveListener(this.ShowUserProfileUI);
            EventMenu.OnManufacturerWhitelistRequested.RemoveListener(this.PushManufacturerWhitelistUI);
            EventMenu.OnLogOutPromptRequested.RemoveListener(this.ShowUserLogOutPrompt);
            EventMenu.OnBackRequested.RemoveListener(this.PopMenu);
            EventMenu.OnScreenOrientationToggleRequested.RemoveListener(this.ToggleScreenOrientation);
            EventMenu.OnQuitPromptRequested.RemoveListener(this.ShowQuitPrompt);
        }

        private void OnSignUpRequested(SignUpInfo info)
        {
            this.PushSignUpUI(info.UserType);
        }

        private void OnSignInRequested(CredentialsInfo info)
        {
            this.ShowLogInAndSignIn(info.Email, info.Password);
        }

        private void OnResetPasswordRequested(ResetPasswordInfo info)
        {
            this.PushResetPasswordUI(info.Email);
        }

        private void OnPayNowRequested(CredentialsInfo info)
        {
            this.ShowPayNowPrompt(info.Email, info.Password);
        }

        private void OnUnauthorizedDomainRequested(UnauthorizedDomainInfo info)
        {
            this.ShowUnauthorizedDomainPrompt(info.AuthorizedDomain);
        }

        private void OnClientInfoRequested(ClientInfo info)
        {
            this.PushClientProfileUI(info.ClientId);
        }

        private void OnDeleteClientRequested(ClientInfo info)
        {
            this.ShowDeleteClientUi(info.ClientId);
        }

        private void OnQuoteInfoRequested(ClientQuoteInfo info)
        {
            this.ShowQuoteInfoUI(info.ClientId, info.QuoteId);
        }

        private void OnDeleteQuoteRequested(ClientQuoteInfo info)
        {
            this.ShowDeleteQuoteUI(info.ClientId, info.QuoteId);
        }

        private void OnSpinnerMessageRequested(SpinnerMessageInfo info)
        {
            this.SetSpinnerStatusMessage(info.Message);
        }

        private void OnFullScreenSpinnerRequested(SpinnerInfo info)
        {
            this.ShowFullScreenSpinner(info.IsShown);
        }

        private void OnBackgroundSpinnerRequested(SpinnerInfo info)
        {
            this.ShowBackgroundSpinner(info.IsShown);
        }

        private void OnWelcomeRequested()
        {
            this.ShowWelcome(MelcherUITransitionStyle.Regress);
        }

        private void OnUserKickedPromptRequested(UserKickedInfo info)
        {
            this.ShowUserKickedPrompt(info.Reason);
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
            this.kioskPinPrompt.VerifyKioskPermission(
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
            this.kioskPinPrompt.VerifyKioskPermission(
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

            PlayerPrefs.SetInt(MenuPreferenceKeys.AlwaysLandscapeOnMobile, m_UseLandscapeOrientation ? 1 : 0);
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


    }   // class MenuNavigationControl

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
