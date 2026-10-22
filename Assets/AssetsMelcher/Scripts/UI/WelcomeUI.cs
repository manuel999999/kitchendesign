namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    /// This is the log-in / sign-up welcome screen. It is the main landing
    /// page when starting the app and is only skipped if the player has
    /// enabled the "Remember Me" option and has a valid sign-in token cached.
    /// </summary>
    public class WelcomeUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        protected override void Awake()
        {
            base.Awake();

            // For iOS, we cannot allow users to register within the app. This
            // is to comply with "Guideline 3.1.1 - Business - Payments - In-App Purchase".
            SetUserRegistrationAllowed(MelcherUnityUtility.targetPlatform != RuntimePlatform.IPhonePlayer);
#if UNITY_WEBGL || UNITY_EDITOR
            // For some builds (e.g., Stoll), the 1-day trial sign-in option
            // is not allowed.
            SetOneDayTrialAllowed(ProFitGlobals.appOptions.allowOneDayTrial);
#else
            SetOneDayTrialAllowed(false);
#endif
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_SignUpTrialButton.onClick += ShowTrialSignUp;
            m_SignUpDealerButton.onClick += ShowDealerSignUp;
            m_LogInButton.onClick += ShowLogInScreen;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_SignUpTrialButton.onClick -= ShowTrialSignUp;
            m_SignUpDealerButton.onClick -= ShowDealerSignUp;
            m_LogInButton.onClick -= ShowLogInScreen;
        }

        protected void ShowTrialSignUp(MelcherButton caller)
        {
            EventBridge.Menu.pushSignUpUi(UserType.Trial);
        }

        protected void ShowDealerSignUp(MelcherButton caller)
        {
            EventBridge.Menu.pushSignUpUi(UserType.Dealer);
        }

        protected void ShowLogInScreen(MelcherButton caller)
        {
            EventBridge.Menu.pushLogInUi();
        }

        protected void SetUserRegistrationAllowed(bool isAllowed)
        {
            m_TitleLabel.gameObject.SetActive(isAllowed);

            // Process the button list. When user-registration is disallowed,
            // only the log-in button is shown
            Transform logInButtonTransform = m_LogInButton.transform;
            foreach (Transform child in m_ButtonListRoot)
            {
                child.gameObject.SetActive(isAllowed || child == logInButtonTransform);
            }
        }

        protected void SetOneDayTrialAllowed(bool isAllowed)
        {
            m_SignUpTrialButton.gameObject.SetActive(isAllowed);

            Transform signUpButtonTransform =
                m_SignUpTrialButton.GetRectTransform();

            // Set the visibility of the "OR" label separator to match the
            // visibility of the 1-day trial button.

            Transform buttonListTransform = signUpButtonTransform.parent;

            Transform buttonSeparator =
                buttonListTransform.GetChild(signUpButtonTransform.GetSiblingIndex() + 1);

            buttonSeparator.gameObject.SetActive(isAllowed);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(WelcomeUI))]

        [SerializeField] protected MelcherButton m_SignUpTrialButton = null;
        [SerializeField] protected MelcherButton m_SignUpDealerButton = null;
        [SerializeField] protected MelcherButton m_LogInButton = null;

        // These fields are used when user registration needs to be disabled
        [Space]
        [SerializeField] protected Transform m_TitleLabel = null;
        [SerializeField] protected Transform m_ButtonListRoot = null;

    }   // class WelcomeUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
