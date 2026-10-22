namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using System.Linq;

    public class UserProfileUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void Refresh()
        {
            //m_ScrollView.verticalNormalizedPosition = 1.0f; // Top

            UserProfile userProfile = ProFitGlobals.userProfile;

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

            m_KioskModeToggle.SetIsOnWithoutNotify(ProFitGlobals.isKioskModeEnabled);
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
                ProFitGlobals.user.userType == UserType.Dealer ||
                    ProFitGlobals.user.userType == UserType.Offline
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

            EventBridge.Menu.showMultipleChoiceUi.Invoke(
                choices: unitChoices.Select(x => x.ToDisplayName()),
                onChoice: (int? index) =>
                {
                    if (index.HasValue)
                    {
                        MeasurementUnit chosenUnit = unitChoices[index.Value];

                        UserProfile newProfile = ProFitGlobals.userProfile;

                        newProfile.preferredUnits = chosenUnit;

                        UpdateProfile(newProfile, password: null, confirmPassword: null);
                    }
                },
                title: "Preferred units?"
            );
        }

        protected void ChangeBusinessName(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit business name?",
                "Business Name",
                ProFitGlobals.userProfile.businessName,
                onAccept: (newBusinessName, _) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;
                    newProfile.businessName = newBusinessName;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeContactName(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit contact's name?",
                "Contact's First Name",
                ProFitGlobals.userProfile.contactFirstName,
                "Contact's Last Name",
                ProFitGlobals.userProfile.contactLastName,
                onAccept: (newFirstName, newLastName) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;
                    newProfile.contactFirstName = newFirstName;
                    newProfile.contactLastName = newLastName;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeContactPhoneNumber(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit contact's name?",
                "Contact's Phone Number",
                PhoneNumber.Format(ProFitGlobals.userProfile.contactPhoneNumber),
                contentType: TMP_InputField.ContentType.Custom,
                onAccept: (newFormattedPhoneNumber, _) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;
                    newProfile.contactPhoneNumber = PhoneNumber.Unformat(newFormattedPhoneNumber);

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeContactInfo(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                title: "Edit contact email or website?",
                header1: "Contact Email or Website",
                value1: ProFitGlobals.userProfile.contactInfo,
                contentType: TMP_InputField.ContentType.EmailAddress,
                onAccept: (newInfo, _) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;

                    newProfile.contactInfo = newInfo;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeSignInEmail(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit sign in email?",
                "Sign In Email",
                ProFitGlobals.userProfile.signInEmail,
                contentType: TMP_InputField.ContentType.EmailAddress,
                onAccept: (newEmail, _) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;
                    newProfile.signInEmail = newEmail;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangePassword(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit password?",
                header1: "Password",
                value1: "",
                header2: "Confirm Password",
                value2: "",
                contentType: TMP_InputField.ContentType.Password,
                onAccept: (password, confirmPassword) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;
                    UpdateProfile(newProfile, password, confirmPassword);
                }
            );
        }

        protected void ChangeAddress(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit address?",
                "Address",
                ProFitGlobals.userProfile.address,
                onAccept: (newAddress, _) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;
                    newProfile.address = newAddress;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeCity(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit city?",
                "City",
                ProFitGlobals.userProfile.city,
                onAccept: (newCity, _) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;
                    newProfile.city = newCity;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeState(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit state?",
                "State",
                ProFitGlobals.userProfile.state,
                onAccept: (newState, _) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;
                    newProfile.state = newState;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ChangeZipCode(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit zip code?",
                "Zip Code",
                ProFitGlobals.userProfile.zipCode,
                onAccept: (newZipCode, _) =>
                {
                    UserProfile newProfile = ProFitGlobals.userProfile;
                    newProfile.zipCode = newZipCode;

                    UpdateProfile(newProfile, password: null, confirmPassword: null);
                }
            );
        }

        protected void ToggleKioskMode(IMelcherToggle caller)
        {
            m_KioskModeToggle.SetIsOnWithoutNotify(ProFitGlobals.isKioskModeEnabled);

            EventBridge.Menu.showKioskKeypad?.Invoke(
                onSuccess: () =>
                {
                    ProFitGlobals.isKioskModeEnabled = !ProFitGlobals.isKioskModeEnabled;
                    m_KioskModeToggle.SetIsOnWithoutNotify(ProFitGlobals.isKioskModeEnabled);
                }
            );
        }

        protected void ShowDeleteAccountPrompt(MelcherButton caller)
        {
            EventBridge.Menu.showDeleteAccountUi.Invoke();
        }

        protected void UpdateProfile(UserProfile newProfile, string password = null, string confirmPassword = null)
        {
            EventBridge.Menu.showFullScreenSpinner?.Invoke(true);

            ProFitWeb.UpdateProfile(
                ProFitGlobals.user,
                newProfile,
                password,
                confirmPassword,
                onSuccess: userProfile =>
                {
                    EventBridge.Menu.showFullScreenSpinner?.Invoke(false);

                    ProFitGlobals.userProfile = newProfile;
                    Refresh();

                    EventBridge.Menu.hideEditUserProfileUi.Invoke();
                },
                onError: (request, error, jsonValue) =>
                {
                    EventBridge.Menu.showFullScreenSpinner?.Invoke(false);

                    EventBridge.Menu.setEditUserProfileError.Invoke(error.Message);
                }
            );
        }

        protected void PushManufacturerWhitelistUI(MelcherButton caller)
        {
            EventBridge.Menu.pushManufacturerWhitelistUi.Invoke();
        }

        protected void LogOut(MelcherButton caller)
        {
            EventBridge.Menu.showUserLogOutPrompt.Invoke();
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
