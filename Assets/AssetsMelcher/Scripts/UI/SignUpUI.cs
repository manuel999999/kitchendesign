namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;
    using TMPro;

    public class SignUpUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void SetUserType(UserType userType)
        {
            if (m_SignUpUserType != userType)
            {
                m_SignUpUserType = userType;
                m_Title.text = userType == UserType.Dealer ? "Dealer Sign Up" : "1-Day Subscription";
                ClearForm();
            }
        }

        public void ClearForm()
        {
            m_ScrollView.verticalNormalizedPosition = 1.0f; // Top

            ResetInputFieldColors();

            m_BusinessNameField.text    = string.Empty;
            m_AddressField.text         = string.Empty;
            m_CityField.text            = string.Empty;
            m_StateField.text           = string.Empty;
            m_ZipCodeField.text         = string.Empty;
            m_FirstNameField.text       = string.Empty;
            m_LastNameField.text        = string.Empty;
            m_PhoneNumberField.text     = string.Empty;
            m_ContactInfoField.text     = string.Empty;
            m_SignInEmailField.text     = string.Empty;
            m_PasswordField.text        = string.Empty;
            m_ConfirmPasswordField.text = string.Empty;

            m_UseContactEmailButton.gameObject.SetActive(true);
            m_RevealPasswordButton.SetSecure(true);
            m_RevealConfirmPasswordButton.SetSecure(true);

            m_TermsOfServiceCheckbox.isOn = false;

            m_MessageBox.Clear();
        }

        protected override void Awake()
        {
            base.Awake();

            // Automatically re-activate the mask at startup. This lets us see
            // the full sign up page in the editor
            m_ScrollView.viewport.GetComponent<Mask>().enabled = true;

            m_AutoFillButton.gameObject.SetActive(Debug.isDebugBuild);

            m_FieldColorBlockDefault = m_BusinessNameField.colors;
            m_FieldColorBlockError = m_FieldColorBlockDefault;
            m_FieldColorBlockError.normalColor = Const.ErrorColor;
            m_FieldColorBlockError.highlightedColor = Const.ErrorColor;
            m_FieldColorBlockError.pressedColor = Color.Lerp(Const.ErrorColor, Color.white, 0.3f);

            m_NameToSelectable = new Dictionary<string, Selectable>()
            {
                { "business_name", m_BusinessNameField },
                { "contact_first_name", m_FirstNameField },
                { "contact_last_name", m_LastNameField },
                { "contact_email", m_ContactInfoField },
                { "contact_url", m_ContactInfoField },
                { "contact_phone_number", m_PhoneNumberField },
                { "address", m_AddressField },
                { "city", m_CityField },
                { "state", m_StateField },
                { "zip_code", m_ZipCodeField },
                { "email", m_SignInEmailField },
                { "password", m_PasswordField },
                { "confirm_password", m_ConfirmPasswordField }
            };
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_AutoFillButton.onClick += AutoFillForm;
            m_UseContactEmailButton.onClick += UseContactEmail;
            m_TermsOfServiceButton.onClick += ShowTermsOfService;
            m_SignUpButton.onClick += TrySignUp;

            m_ContactInfoField.onValueChanged.AddListener(OnContactInfoChanged);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_AutoFillButton.onClick -= AutoFillForm;
            m_UseContactEmailButton.onClick -= UseContactEmail;
            m_TermsOfServiceButton.onClick -= ShowTermsOfService;
            m_SignUpButton.onClick -= TrySignUp;

            m_ContactInfoField.onValueChanged.RemoveListener(OnContactInfoChanged);
        }

        protected override void DoShow()
        {
            // Hide the password fields when returning to not expose the user's details
            m_PasswordField.contentType = TMP_InputField.ContentType.Password;
            m_PasswordField.ForceLabelUpdate();

            m_ConfirmPasswordField.contentType = TMP_InputField.ContentType.Password;
            m_ConfirmPasswordField.ForceLabelUpdate();

            base.DoShow();
        }

        protected void AutoFillForm(MelcherButton caller)
        {
#if UNITY_EDITOR
            string firstName = Utility.RandomNamesForEditor.RandomName();
            string lastName = Utility.RandomNamesForEditor.RandomName();
            string email = firstName + lastName + Random.Range(50, 99) + "@fake.com";

            m_BusinessNameField.text= Utility.RandomNamesForEditor.RandomName() + "-" + Utility.RandomNamesForEditor.RandomPlace();
            m_FirstNameField.text   = firstName;
            m_LastNameField.text    = lastName;
            m_ContactInfoField.text = email;
            m_SignInEmailField.text = email;
            m_PhoneNumberField.text = PhoneNumber.Format(PhoneNumber.Random());
            m_AddressField.text     = Random.Range(1, 9999) + " " + Utility.RandomNamesForEditor.RandomPlace();
            m_CityField.text        = Utility.RandomNamesForEditor.RandomPlace();
            m_StateField.text       = Utility.RandomNamesForEditor.RandomPlace();
            m_ZipCodeField.text     = Random.Range(1000, 99999).ToString();

            m_PasswordField.text        = "Pass@123";
            m_ConfirmPasswordField.text = "Pass@123";
#endif  // if UNITY_EDITOR
        }

        protected void UseContactEmail(MelcherButton caller)
        {
            m_SignInEmailField.text = m_ContactInfoField.text;
        }

        protected void ShowTermsOfService(MelcherButton caller)
        {
            ProFitUtility.ShowTermsAndConditions();
        }

        protected void TrySignUp(MelcherButton caller)
        {
            // TODO: Block all interactions while we are doing web request

            m_MessageBox.Clear();

            ResetInputFieldColors();


            string contactEmail = "";   // not null for backwards compatibility with version that required a contact email
            string contactUrl = null;

            string contactInfo = m_ContactInfoField.text;
            if (contactInfo.Contains('@'))
            {
                contactEmail = contactInfo;
            }
            else
            {
                contactUrl = contactInfo;
            }

            ProFitWeb.SignUp(
                userType:           m_SignUpUserType,
                userFilter:         ProFitGlobals.appOptions.userFilter,
                businessName:       m_BusinessNameField.text,
                contactFirstName:   m_FirstNameField.text,
                contactLastName:    m_LastNameField.text,
                contactEmail:       contactEmail,
                contactUrl:         contactUrl,
                contactPhoneNumber: PhoneNumber.Unformat(m_PhoneNumberField.text),
                referrer:           m_ReferredByField.text,
                address:            m_AddressField.text,
                city:               m_CityField.text,
                state:              m_StateField.text,
                zipCode:            m_ZipCodeField.text,
                signInEmail:        m_SignInEmailField.text,
                password:           m_PasswordField.text,
                confirmPassword:    m_ConfirmPasswordField.text,
                didAcceptTerms:     m_TermsOfServiceCheckbox.isOn,
                suppressEmail:      Application.isEditor,
                onSuccess:          OnSuccess,
                onError:            OnError
            );

            void OnSuccess()
            {
                // Try to sign in as the new user
                EventBridge.Menu.showLogInUiAndSignIn?.Invoke(
                    m_SignInEmailField.text,
                    m_PasswordField.text
                );

                ClearForm();
            }

            void OnError(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = new MelcherJsonObjectParser(jsonValue.jsonObject);

                if (parser.TryGetErrorMap("errors", out Dictionary<string, List<string>> errorMap))
                {
                    foreach (var issue in errorMap)
                    {
                        if (issue.Key == "terms")
                        {
                            // TODO: Red-out the terms checkbox
                        }
                        else if (m_NameToSelectable.TryGetValue(issue.Key, out Selectable control))
                        {
                            if (issue.Key == "password")
                            {
                                m_ConfirmPasswordField.colors = m_FieldColorBlockError;
                            }

                            control.colors = m_FieldColorBlockError;

                            foreach (string e in issue.Value)
                            {
                                MelcherLog.LogError($"{issue.Key}: {e}");
                            }
                        }
                        else
                        {
                            MelcherLog.LogError("Unhandled error map: " + issue.Key);
                        }
                    }
                }

                m_MessageBox.ErrorFromWebRequest(builder, error, jsonValue);
            }
        }

        protected void OnContactInfoChanged(string contactInfo)
        {
            bool isContactInfoAnEmail = contactInfo.Contains('@');
            m_UseContactEmailButton.gameObject.SetActive(isContactInfoAnEmail);
        }

        protected void ResetInputFieldColors()
        {
            foreach (var kv in m_NameToSelectable)
            {
                kv.Value.colors = m_FieldColorBlockDefault;
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(SignUpUI))]

        [SerializeField] protected TMP_Text m_Title = null;
        [SerializeField] protected ScrollRect m_ScrollView = null;

        [Space]
        [SerializeField] protected MelcherButton m_AutoFillButton = null;

        [Space]
        [SerializeField] protected TMP_InputField m_BusinessNameField = null;
        [SerializeField] protected TMP_InputField m_AddressField = null;
        [SerializeField] protected TMP_InputField m_CityField = null;
        [SerializeField] protected TMP_InputField m_StateField = null;
        [SerializeField] protected TMP_InputField m_ZipCodeField = null;
        [SerializeField] protected TMP_InputField m_FirstNameField = null;
        [SerializeField] protected TMP_InputField m_LastNameField = null;
        [SerializeField] protected TMP_InputField m_PhoneNumberField = null;
        [UnityEngine.Serialization.FormerlySerializedAs("m_ContactEmailField")]
        [SerializeField] protected TMP_InputField m_ContactInfoField = null;
        [SerializeField] protected TMP_InputField m_ReferredByField = null;
        [SerializeField] protected MelcherButton m_UseContactEmailButton = null;

        [Space]
        [SerializeField] protected TMP_InputField m_SignInEmailField = null;
        [SerializeField] protected RevealPasswordButton m_RevealPasswordButton = null;
        [SerializeField] protected TMP_InputField m_PasswordField = null;
        [SerializeField] protected RevealPasswordButton m_RevealConfirmPasswordButton = null;
        [SerializeField] protected TMP_InputField m_ConfirmPasswordField = null;
        [SerializeField] protected MelcherToggle m_TermsOfServiceCheckbox = null;
        [SerializeField] protected MelcherButton m_TermsOfServiceButton = null;

        [Space]
        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected MelcherButton m_SignUpButton = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected ColorBlock m_FieldColorBlockDefault;
        protected ColorBlock m_FieldColorBlockError;
        protected UserType m_SignUpUserType;

        protected Dictionary<string, Selectable> m_NameToSelectable;

    }   // class SignUpUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
