namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using System.Linq;
    using KitchenDesign.Events;
    using KitchenDesign.Session;

    public class UserProfileUI : Menu
    {
        private SessionStatus _session;
        private ServerClient _server;
        private EditUserProfileUI _editProfilePrompt;
        private EditUserProfileMultipleChoiceUI _multipleChoicePrompt;
        private KioskPinUI _kioskPinPrompt;

        public void Initialize(SessionStatus session, ServerClient server, EditUserProfileUI editProfilePrompt, EditUserProfileMultipleChoiceUI multipleChoicePrompt, KioskPinUI kioskPinPrompt)
        {
            this._session = session;
            this._server = server;
            this._editProfilePrompt = editProfilePrompt;
            this._multipleChoicePrompt = multipleChoicePrompt;
            this._kioskPinPrompt = kioskPinPrompt;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public void Refresh()
        {
            //m_ScrollView.verticalNormalizedPosition = 1.0f; // Top

            UserProfile userProfile = this._session.UserProfile;

            // Only display the measurement units option when the user is not
            // forced to use inches.
            m_UnitMeasurementsGroup.gameObject.SetActive(!userProfile.isInchesOnly);

            m_UnitMeasurementsLabel.phrase      = userProfile.preferredUnits.ToDisplayName();

            m_BusinessNameLabel.phrase          = userProfile.businessName;

            m_ContactFirstNameLabel.phrase      = userProfile.contactFirstName;
            m_ContactLastNameLabel.phrase       = userProfile.contactLastName;
            m_ContactInfoLabel.phrase           = userProfile.contactInfo;
            m_ContactPhoneNumberLabel.phrase    = PhoneNumber.Format(userProfile.contactPhoneNumber);
            m_SignInEmailLabel.phrase           = userProfile.signInEmail;

            m_AddressLabel.phrase               = userProfile.address;
            m_CityLabel.phrase                  = userProfile.city;
            m_StateLabel.phrase                 = userProfile.state;
            m_ZipCodeLabel.phrase               = userProfile.zipCode;

            m_KioskModeToggle.SetIsOnWithoutNotify(this._session.IsKioskModeEnabled);
        }

        protected override void Awake()
        {
            base.Awake();

            // Automatically enable the list mask when the game starts. It's
            // nice to have the mask disabled in the editor while working on it
            m_ScrollView.viewport.GetComponent<Mask>().enabled = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_UnitMeasurementsButton.onClick    += ChangeUnitsForMeasurements;

            m_BusinessNameButton.onClick        += ChangeBusinessName;

            m_ContactFirstNameButton.onClick    += ChangeContactName;
            m_ContactLastNameButton.onClick     += ChangeContactName;
            m_ContactPhoneNumberButton.onClick  += ChangeContactPhoneNumber;
            m_ContactInfoButton.onClick         += ChangeContactInfo;

            m_SignInEmailButton.onClick         += ChangeSignInEmail;
            m_PasswordButton.onClick            += ChangePassword;

            m_AddressButton.onClick             += ChangeAddress;
            m_CityButton.onClick                += ChangeCity;
            m_StateButton.onClick               += ChangeState;
            m_ZipCodeButton.onClick             += ChangeZipCode;

            m_KioskModeToggle.onValueChanged    += ToggleKioskMode;

            m_DeleteAccountButton.onClick       += ShowDeleteAccountPrompt;

            m_ManufacturerWhitelistButton.onClick += PushManufacturerWhitelistUI;

            m_LogOutButton.onClick              += LogOut;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_UnitMeasurementsButton.onClick    -= ChangeUnitsForMeasurements;

            m_BusinessNameButton.onClick        -= ChangeBusinessName;

            m_ContactFirstNameButton.onClick    -= ChangeContactName;
            m_ContactLastNameButton.onClick     -= ChangeContactName;
            m_ContactPhoneNumberButton.onClick  -= ChangeContactPhoneNumber;
            m_ContactInfoButton.onClick         -= ChangeContactInfo;

            m_SignInEmailButton.onClick         -= ChangeSignInEmail;
            m_PasswordButton.onClick            -= ChangePassword;

            m_AddressButton.onClick             -= ChangeAddress;
            m_CityButton.onClick                -= ChangeCity;
            m_StateButton.onClick               -= ChangeState;
            m_ZipCodeButton.onClick             -= ChangeZipCode;

            m_KioskModeToggle.onValueChanged    -= ToggleKioskMode;

            m_DeleteAccountButton.onClick       -= ShowDeleteAccountPrompt;

            m_ManufacturerWhitelistButton.onClick -= PushManufacturerWhitelistUI;

            m_LogOutButton.onClick              -= LogOut;
        }

        protected override void DoShow()
        {
            // Only show the dealers' controls if the user is a dealer
            m_DealerOnlyControls.gameObject.SetActive(
                this._session.User.userType == UserType.Dealer ||
                    this._session.User.userType == UserType.Offline
            );

            base.DoShow();
        }

        protected void ChangeUnitsForMeasurements(MelcherButton caller)
        {
            List<MeasurementUnit> unitChoices =
                new(
                    MelcherEnumInfo<MeasurementUnit>.values
                        .Where(v => v != MeasurementUnit.None)
                );

            this._multipleChoicePrompt.SetupAndShow(
                choices: unitChoices.Select(x => x.ToDisplayName()),
                onChoose: (int? index) =>
                {
                    if (index.HasValue)
                    {
                        MeasurementUnit chosenUnit = unitChoices[index.Value];

                        UserProfile newProfile = this._session.UserProfile;

                        newProfile.preferredUnits = chosenUnit;

                        UpdateProfile(newProfile, password: null, confirmPassword: null);
                    }
                },
                title: "Preferred units?"
            );
        }

        protected void ChangeBusinessName(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit business name?",
                "Business Name",
                this._session.UserProfile.businessName,
                onAccept: (newBusinessName, _) =>
                {
                    UserProfile newProfile = this._session.UserProfile;
                    newProfile.businessName = newBusinessName;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeContactName(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit contact's name?",
                "Contact's First Name",
                this._session.UserProfile.contactFirstName,
                "Contact's Last Name",
                this._session.UserProfile.contactLastName,
                onAccept: (newFirstName, newLastName) =>
                {
                    UserProfile newProfile = this._session.UserProfile;
                    newProfile.contactFirstName = newFirstName;
                    newProfile.contactLastName = newLastName;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeContactPhoneNumber(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit contact's name?",
                "Contact's Phone Number",
                PhoneNumber.Format(this._session.UserProfile.contactPhoneNumber),
                contentType: TMP_InputField.ContentType.Custom,
                onAccept: (newFormattedPhoneNumber, _) =>
                {
                    UserProfile newProfile = this._session.UserProfile;
                    newProfile.contactPhoneNumber = PhoneNumber.Unformat(newFormattedPhoneNumber);

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeContactInfo(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                title: "Edit contact email or website?",
                header1: "Contact Email or Website",
                value1: this._session.UserProfile.contactInfo,
                contentType: TMP_InputField.ContentType.EmailAddress,
                onAccept: (newInfo, _) =>
                {
                    UserProfile newProfile = this._session.UserProfile;

                    newProfile.contactInfo = newInfo;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeSignInEmail(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit sign in email?",
                "Sign In Email",
                this._session.UserProfile.signInEmail,
                contentType: TMP_InputField.ContentType.EmailAddress,
                onAccept: (newEmail, _) =>
                {
                    UserProfile newProfile = this._session.UserProfile;
                    newProfile.signInEmail = newEmail;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangePassword(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit password?",
                header1: "Password",
                value1: "",
                header2: "Confirm Password",
                value2: "",
                contentType: TMP_InputField.ContentType.Password,
                onAccept: (password, confirmPassword) =>
                {
                    UserProfile newProfile = this._session.UserProfile;
                    UpdateProfile(newProfile, password, confirmPassword);
                }
            );
        }

        protected void ChangeAddress(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit address?",
                "Address",
                this._session.UserProfile.address,
                onAccept: (newAddress, _) =>
                {
                    UserProfile newProfile = this._session.UserProfile;
                    newProfile.address = newAddress;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeCity(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit city?",
                "City",
                this._session.UserProfile.city,
                onAccept: (newCity, _) =>
                {
                    UserProfile newProfile = this._session.UserProfile;
                    newProfile.city = newCity;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeState(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit state?",
                "State",
                this._session.UserProfile.state,
                onAccept: (newState, _) =>
                {
                    UserProfile newProfile = this._session.UserProfile;
                    newProfile.state = newState;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeZipCode(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit zip code?",
                "Zip Code",
                this._session.UserProfile.zipCode,
                onAccept: (newZipCode, _) =>
                {
                    UserProfile newProfile = this._session.UserProfile;
                    newProfile.zipCode = newZipCode;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ToggleKioskMode(IMelcherToggle caller)
        {
            m_KioskModeToggle.SetIsOnWithoutNotify(this._session.IsKioskModeEnabled);

            this._kioskPinPrompt.ShowKioskKeypad(
                onSuccess: () =>
                {
                    this._session.IsKioskModeEnabled = !this._session.IsKioskModeEnabled;
                    m_KioskModeToggle.SetIsOnWithoutNotify(this._session.IsKioskModeEnabled);
                }
            );
        }

        protected void ShowDeleteAccountPrompt(MelcherButton caller)
        {
            EventMenu.OnDeleteAccountRequested.Raise();
        }

        protected void UpdateProfile(UserProfile newProfile, string password = null, string confirmPassword = null)
        {
            EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(true));

            this._server.Accounts.UpdateProfile(
                this._session.User,
                newProfile,
                password,
                confirmPassword,
                onSuccess: userProfile =>
                {
                    EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(false));

                    this._session.UserProfile = newProfile;
                    Refresh();

                    this._editProfilePrompt.Finished();
                },
                onError: (request, error, jsonValue) =>
                {
                    EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(false));

                    this._editProfilePrompt.SetError(error.Message);
                }
            );
        }

        protected void PushManufacturerWhitelistUI(MelcherButton caller)
        {
            EventMenu.OnManufacturerWhitelistRequested.Raise();
        }

        protected void LogOut(MelcherButton caller)
        {
            EventMenu.OnLogOutPromptRequested.Raise();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(UserProfileUI))]

        [SerializeField] protected ScrollRect m_ScrollView = null;

        [Space]

        [SerializeField] protected RectTransform m_UnitMeasurementsGroup = default;
        [SerializeField] protected MelcherButton m_UnitMeasurementsButton = null;
        [SerializeField] protected MelcherLocalizedText m_UnitMeasurementsLabel = null;

        [Space]

        [SerializeField] protected MelcherButton m_BusinessNameButton = null;
        [SerializeField] protected MelcherLocalizedText m_BusinessNameLabel = null;

        [Space]

        [SerializeField] protected MelcherButton m_ContactFirstNameButton = null;
        [SerializeField] protected MelcherLocalizedText m_ContactFirstNameLabel = null;

        [SerializeField] protected MelcherButton m_ContactLastNameButton = null;
        [SerializeField] protected MelcherLocalizedText m_ContactLastNameLabel = null;

        [SerializeField] protected MelcherButton m_ContactPhoneNumberButton = null;
        [SerializeField] protected MelcherLocalizedText m_ContactPhoneNumberLabel = null;

        [UnityEngine.Serialization.FormerlySerializedAs("m_ContactEmailButton")]
        [SerializeField] protected MelcherButton m_ContactInfoButton = null;
        [UnityEngine.Serialization.FormerlySerializedAs("m_ContactEmailLabel")]
        [SerializeField] protected MelcherLocalizedText m_ContactInfoLabel = null;

        [Space]

        [SerializeField] protected MelcherButton m_SignInEmailButton = null;
        [SerializeField] protected MelcherLocalizedText m_SignInEmailLabel = null;

        [SerializeField] protected MelcherButton m_PasswordButton = null;

        [Space]

        [SerializeField] protected MelcherButton m_AddressButton = null;
        [SerializeField] protected MelcherLocalizedText m_AddressLabel = null;

        [SerializeField] protected MelcherButton m_CityButton = null;
        [SerializeField] protected MelcherLocalizedText m_CityLabel = null;

        [SerializeField] protected MelcherButton m_StateButton = null;
        [SerializeField] protected MelcherLocalizedText m_StateLabel = null;

        [SerializeField] protected MelcherButton m_ZipCodeButton = null;
        [SerializeField] protected MelcherLocalizedText m_ZipCodeLabel = null;

        [SerializeField] protected MelcherToggle m_KioskModeToggle = null;

        [SerializeField] protected MelcherButton m_DeleteAccountButton = null;

        [Space]
        [SerializeField] protected RectTransform m_DealerOnlyControls = null;
        [SerializeField] protected MelcherButton m_ManufacturerWhitelistButton = null;

        [Space]
        [SerializeField] protected MelcherButton m_LogOutButton = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected ColorBlock m_FieldColorBlockDefault;
        protected ColorBlock m_FieldColorBlockError;
        protected UserType m_SignUpUserType;

        protected Dictionary<string, Selectable> m_NameToSelectable;

    }   // class UserProfileUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
