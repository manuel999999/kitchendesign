namespace KitchenDesign.UI.MenuScreens
{
    using KitchenDesign.UI.Views;
    using KitchenDesign.Utility;
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using KitchenDesign.Events;
    using KitchenDesign.Session;

    public class ClientProfileUI : Menu
    {
        private SessionStatus _session;
        private ClientListStatus _clientList;
        private ServerClient _server;
        private EditUserProfileUI _editProfilePrompt;

        public void Initialize(SessionStatus session, ClientListStatus clientList, ServerClient server, EditUserProfileUI editProfilePrompt)
        {
            this._session = session;
            this._clientList = clientList;
            this._server = server;
            this._editProfilePrompt = editProfilePrompt;
        }

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
            if (this._clientList.TryGetClientIndex(m_ClientId, out int clientIndex))
            {
                _clientProfile = this._clientList.UserClients[clientIndex];
            }
            else
            {
                _clientProfile = default;
            }

            m_ClientFirstNameLabel.phrase   = _clientProfile.firstName;
            m_ClientLastNameLabel.phrase    = _clientProfile.lastName;
            m_ClientEmailLabel.phrase       = _clientProfile.email;
            m_ClientPhoneNumberLabel.phrase = PhoneNumber.Format(_clientProfile.phoneNumber);

            m_AddressLabel.phrase           = _clientProfile.address;
            m_CityLabel.phrase              = _clientProfile.city;
            m_StateLabel.phrase             = _clientProfile.state;
            m_ZipCodeLabel.phrase           = _clientProfile.zipCode;
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
            this._editProfilePrompt.SetContent(
                "Edit client's name?",
                "Client's First Name",
                _clientProfile.firstName,
                "Client's Last Name",
                _clientProfile.lastName,
                onAccept: (newFirstName, newLastName) =>
                {
                    UserClient newProfile = _clientProfile;
                    newProfile.firstName = newFirstName;
                    newProfile.lastName = newLastName;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeClientPhoneNumber(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit client's phone number?",
                "Client's Phone Number",
                PhoneNumber.Format(_clientProfile.phoneNumber),
                contentType: TMP_InputField.ContentType.Custom,
                keyboardType: TouchScreenKeyboardType.NumberPad,
                onAccept: (newFormattedPhoneNumber, _) =>
                {
                    UserClient newProfile = _clientProfile;
                    newProfile.phoneNumber = PhoneNumber.Unformat(newFormattedPhoneNumber);

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeClientEmail(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit contact's email?",
                "Client's Email",
                _clientProfile.email,
                contentType: TMP_InputField.ContentType.EmailAddress,
                onAccept: (newEmail, _) =>
                {
                    UserClient newProfile = _clientProfile;
                    newProfile.email = newEmail;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeAddress(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit address?",
                "Address",
                _clientProfile.address,
                onAccept: (newAddress, _) =>
                {
                    UserClient newProfile = _clientProfile;
                    newProfile.address = newAddress;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeCity(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit city?",
                "City",
                _clientProfile.city,
                onAccept: (newCity, _) =>
                {
                    UserClient newProfile = _clientProfile;
                    newProfile.city = newCity;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeState(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit state?",
                "State",
                _clientProfile.state,
                onAccept: (newState, _) =>
                {
                    UserClient newProfile = _clientProfile;
                    newProfile.state = newState;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void ChangeZipCode(MelcherButton caller)
        {
            this._editProfilePrompt.SetContent(
                "Edit zip code?",
                "Zip Code",
                _clientProfile.zipCode,
                contentType: TMP_InputField.ContentType.Alphanumeric,
                onAccept: (newZipCode, _) =>
                {
                    UserClient newProfile = _clientProfile;
                    newProfile.zipCode = newZipCode;

                    UpdateProfile(newProfile);
                }
            );
        }

        protected void UpdateProfile(UserClient newProfile)
        {
            EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(true));

            this._server.Clients.UpdateClient(
                this._session.User,
                newProfile,
                onSuccess: userClient =>
                {
                    EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(false));

                    if (!this._clientList.TryGetClientIndex(m_ClientId, out int clientIndex))
                    {
                        return;
                    }

                    this._clientList.UserClients[clientIndex] = userClient;

                    Refresh();

                    this._editProfilePrompt.Finished();
                },
                onError: (builder, error, jsonValue) =>
                {
                    EventSession.OnFullScreenSpinnerRequested.Raise(new SpinnerInfo(false));

                    this._editProfilePrompt.SetError(error.Message);
                }
            );
        }

        protected void PushDeleteClientUI(MelcherButton caller)
        {
            EventMenu.OnDeleteClientRequested.Raise(new ClientInfo(m_ClientId));
        }

        protected void Email(MelcherButton caller)
        {
            Application.OpenURL("mailto:" + _clientProfile.email);
        }

        protected void Phone(MelcherButton caller)
        {
            Application.OpenURL("tel:" + _clientProfile.phoneNumber);
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
        protected UserClient _clientProfile;

    }   // class ClientProfileUI

}   // namespace KitchenDesign.UI.MenuScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
