namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using System.Collections.Generic;
    using UnityEngine;
    using MEC;

    /// <summary>
    /// The prompt that shows up when a new user signs in.
    /// </summary>
    public class PayNowUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void SetFields(string email, string password)
        {
            m_Email = email;
            m_Password = password;
        }

        protected override void Awake()
        {
            base.Awake();

            if (!MelcherUnityUtility.isTargetMobilePlatform)
            {
                MelcherLog.LogEditorOnly("Using pay now mode");
                m_Button.GetComponentInChildren<MelcherLocalizedText>().phrase = "Pay Now";
                m_IsPaymentMode = true;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Button.onClick += OnClickButton;
            m_DeleteAccountButton.onClick += OnClickDeleteAccountButton;

            m_MessageLabel.onPostProcess += OnPostProcessMessage;

            m_MessageLabel.RefreshText();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_Button.onClick -= OnClickButton;
            m_DeleteAccountButton.onClick -= OnClickDeleteAccountButton;

            m_MessageLabel.onPostProcess -= OnPostProcessMessage;
        }

        protected override void DoShow()
        {
            base.DoShow();

            if (m_IsPaymentMode)
            {
                Timing.RunCoroutine(WaitForPayment());
            }
        }

        protected IEnumerator<float> WaitForPayment()
        {
            UserProfile userProfile = default;

            // This coroutine pings the server every couple seconds to
            // see if the user has made a payment/been approved
            do
            {
                yield return Timing.WaitForSeconds(3.0f);

                if (ProFitGlobals.user.userType == UserType.None)
                {
                    // Break out. The user can delete their account on the
                    // Pay Now page so we need to hide it if that happens
                    break;
                }

                var getProfileRequest =
                    ProFitWeb.GetProfile(
                        ProFitGlobals.user.apiToken,
                        isOffline: ProFitGlobals.user.userType == UserType.Offline,
                        onSuccess: (newUserProfile) =>
                        {
                            userProfile = newUserProfile;
                        },
                        onError: (builder, error, jsonValue) =>
                        {
                            MelcherLog.LogError(error.Message);
                        }
                    );

                yield return Timing.WaitUntilDone(getProfileRequest);

            } while (!userProfile.IsSubscriptionOrTrialActive());

            Hide();

            if (userProfile.IsSubscriptionOrTrialActive())
            {
                ProFitGlobals.userProfile = userProfile;
                EventBridge.Menu.showLogInUiAndSignIn.Invoke(m_Email, m_Password);
            }
        }

        protected void OnClickButton(MelcherButton caller)
        {
            if (m_IsPaymentMode)
            {
                ProFitUtility.OpenSubscriptionLandingPage();
            }
            else
            {
                Hide();
            }
        }

        protected void OnClickDeleteAccountButton(MelcherButton caller)
        {
            EventBridge.Menu.showDeleteAccountUi.Invoke();
        }

        protected void OnPostProcessMessage(MelcherLocalizedText caller, ref string phrase)
        {
            string appName = ProFitGlobals.appOptions.appDisplayName;

            phrase = phrase.Replace("%APPNAME%", appName);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(PayNowUI))]

        [SerializeField] protected MelcherLocalizedText m_TitleLabel = null;
        [SerializeField] protected MelcherLocalizedText m_MessageLabel = null;
        [SerializeField] protected MelcherButton m_Button = null;
        [SerializeField] protected MelcherButton m_DeleteAccountButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected bool m_IsPaymentMode;
        protected string m_Email;
        protected string m_Password;

    }   // class PayNowUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
