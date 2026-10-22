namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class ClientProfileUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void SetFields(int clientId)
        {
            m_ScrollView.verticalNormalizedPosition = 1.0f; // Top
            m_ClientId = clientId;
            Refresh();
        }

        public void Refresh()
        {
            if (ProFitGlobals.TryGetClientIndex(m_ClientId, out int clientIndex))
            {
                m_ClientProfile = ProFitGlobals.userClients[clientIndex];
            }
            else
            {
                m_ClientProfile = default;
            }

            m_ClientFirstNameLabel.phrase   = m_ClientProfile.firstName;
            m_ClientLastNameLabel.phrase    = m_ClientProfile.lastName;
            m_ClientEmailLabel.phrase       = m_ClientProfile.email;
            m_ClientPhoneNumberLabel.phrase = PhoneNumber.Format(m_ClientProfile.phoneNumber);

            m_AddressLabel.phrase           = m_ClientProfile.address;
            m_CityLabel.phrase              = m_ClientProfile.city;
            m_StateLabel.phrase             = m_ClientProfile.state;
            m_ZipCodeLabel.phrase           = m_ClientProfile.zipCode;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_ClientFirstNameButton.onClick     += ChangeClientName;
            m_ClientLastNameButton.onClick      += ChangeClientName;
            m_ClientPhoneNumberButton.onClick   += ChangeClientPhoneNumber;
            m_ClientEmailButton.onClick         += ChangeClientEmail;

            m_AddressButton.onClick             += ChangeAddress;
            m_CityButton.onClick                += ChangeCity;
            m_StateButton.onClick               += ChangeState;
            m_ZipCodeButton.onClick             += ChangeZipCode;

            m_DeleteClientButton.onClick        += PushDeleteClientUI;

            m_MailToButton.onClick              += Email;
            m_CallButton.onClick                += Phone;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_ClientFirstNameButton.onClick     -= ChangeClientName;
            m_ClientLastNameButton.onClick      -= ChangeClientName;
            m_ClientPhoneNumberButton.onClick   -= ChangeClientPhoneNumber;
            m_ClientEmailButton.onClick         -= ChangeClientEmail;

            m_AddressButton.onClick             -= ChangeAddress;
            m_CityButton.onClick                -= ChangeCity;
            m_StateButton.onClick               -= ChangeState;
            m_ZipCodeButton.onClick             -= ChangeZipCode;

            m_DeleteClientButton.onClick        -= PushDeleteClientUI;

            m_MailToButton.onClick              -= Email;
            m_CallButton.onClick                -= Phone;
        }

        protected void ChangeClientName(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit client's name?",
                "Client's First Name",
                m_ClientProfile.firstName,
                "Client's Last Name",
                m_ClientProfile.lastName,
                onAccept: (newFirstName, newLastName) =>
                {
                    UserClient newProfile = m_ClientProfile;
                    newProfile.firstName = newFirstName;
                    newProfile.lastName = newLastName;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeClientPhoneNumber(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit client's phone number?",
                "Client's Phone Number",
                PhoneNumber.Format(m_ClientProfile.phoneNumber),
                contentType: TMP_InputField.ContentType.Custom,
                keyboardType: TouchScreenKeyboardType.NumberPad,
                onAccept: (newFormattedPhoneNumber, _) =>
                {
                    UserClient newProfile = m_ClientProfile;
                    newProfile.phoneNumber = PhoneNumber.Unformat(newFormattedPhoneNumber);

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeClientEmail(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit contact's email?",
                "Client's Email",
                m_ClientProfile.email,
                contentType: TMP_InputField.ContentType.EmailAddress,
                onAccept: (newEmail, _) =>
                {
                    UserClient newProfile = m_ClientProfile;
                    newProfile.email = newEmail;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeAddress(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit address?",
                "Address",
                m_ClientProfile.address,
                onAccept: (newAddress, _) =>
                {
                    UserClient newProfile = m_ClientProfile;
                    newProfile.address = newAddress;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeCity(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit city?",
                "City",
                m_ClientProfile.city,
                onAccept: (newCity, _) =>
                {
                    UserClient newProfile = m_ClientProfile;
                    newProfile.city = newCity;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeState(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit state?",
                "State",
                m_ClientProfile.state,
                onAccept: (newState, _) =>
                {
                    UserClient newProfile = m_ClientProfile;
                    newProfile.state = newState;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeZipCode(MelcherButton caller)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                "Edit zip code?",
                "Zip Code",
                m_ClientProfile.zipCode,
                contentType: TMP_InputField.ContentType.Alphanumeric,
                onAccept: (newZipCode, _) =>
                {
                    UserClient newProfile = m_ClientProfile;
                    newProfile.zipCode = newZipCode;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void UpdateProfile(UserClient newProfile)
        {
            EventBridge.Menu.showFullScreenSpinner?.Invoke(true);

            ProFitWeb.UpdateClient(
                ProFitGlobals.user,
                newProfile,
                onSuccess: userClient =>
                {
                    EventBridge.Menu.showFullScreenSpinner?.Invoke(false);

                    if (!ProFitGlobals.TryGetClientIndex(m_ClientId, out int clientIndex))
                    {
                        return;
                    }

                    ProFitGlobals.userClients[clientIndex] = userClient;

                    Refresh();

                    EventBridge.Menu.hideEditUserProfileUi.Invoke();
                },
                onError: (builder, error, jsonValue) =>
                {
                    EventBridge.Menu.showFullScreenSpinner?.Invoke(false);

                    EventBridge.Menu.setEditUserProfileError.Invoke(error.Message);
                }
            );
        }

        protected void PushDeleteClientUI(MelcherButton caller)
        {
            EventBridge.Menu.showDeleteClientUi?.Invoke(m_ClientId);
        }

        protected void Email(MelcherButton caller)
        {
            Application.OpenURL("mailto:" + m_ClientProfile.email);
        }

        protected void Phone(MelcherButton caller)
        {
            Application.OpenURL("tel:" + m_ClientProfile.phoneNumber);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ClientProfileUI))]

        [SerializeField] protected ScrollRect m_ScrollView = null;
        [Space]

        [SerializeField] protected MelcherButton m_ClientFirstNameButton = null;
        [SerializeField] protected MelcherLocalizedText m_ClientFirstNameLabel = null;

        [SerializeField] protected MelcherButton m_ClientLastNameButton = null;
        [SerializeField] protected MelcherLocalizedText m_ClientLastNameLabel = null;

        [SerializeField] protected MelcherButton m_ClientPhoneNumberButton = null;
        [SerializeField] protected MelcherLocalizedText m_ClientPhoneNumberLabel = null;
        [SerializeField] protected MelcherButton m_CallButton = null;

        [SerializeField] protected MelcherButton m_ClientEmailButton = null;
        [SerializeField] protected MelcherLocalizedText m_ClientEmailLabel = null;
        [SerializeField] protected MelcherButton m_MailToButton = null;

        [Space]

        [SerializeField] protected MelcherButton m_AddressButton = null;
        [SerializeField] protected MelcherLocalizedText m_AddressLabel = null;

        [SerializeField] protected MelcherButton m_CityButton = null;
        [SerializeField] protected MelcherLocalizedText m_CityLabel = null;

        [SerializeField] protected MelcherButton m_StateButton = null;
        [SerializeField] protected MelcherLocalizedText m_StateLabel = null;

        [SerializeField] protected MelcherButton m_ZipCodeButton = null;
        [SerializeField] protected MelcherLocalizedText m_ZipCodeLabel = null;

        [Space]
        [SerializeField] protected MelcherButton m_DeleteClientButton = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected ColorBlock m_FieldColorBlockDefault;
        protected ColorBlock m_FieldColorBlockError;
        protected UserType m_SignUpUserType;

        protected Dictionary<string, Selectable> m_NameToSelectable;

        protected int m_ClientId;
        protected UserClient m_ClientProfile;

    }   // class ClientProfileUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
