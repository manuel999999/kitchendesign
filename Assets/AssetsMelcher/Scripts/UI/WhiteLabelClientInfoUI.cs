namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using MEC;
    using KitchenDesign.KitchenModel;

    using Random = UnityEngine.Random;
    using System.Text;

    /// <summary>
    /// This script manages the UI in a WebGL white-label build. I.e., the
    /// app will bypass the log-in feature and allow anyone to create their
    /// own kitchen design.
    /// </summary>
    public class WhiteLabelClientInfoUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ClearForm()
        {
            m_FirstNameField.text       = string.Empty;
            m_LastNameField.text        = string.Empty;
            m_EmailField.text           = string.Empty;
            m_PhoneNumberField.text     = string.Empty;

            m_AddressField.text         = string.Empty;
            m_CityField.text            = string.Empty;
            m_StateField.text           = string.Empty;
            m_ZipCodeField.text         = string.Empty;

            m_WantsFileEmailed.isOn     = true;
            m_WantsToBeContacted.isOn   = false;
            m_DidAcceptTerms.isOn       = false;
        }

        protected override void Awake()
        {
            base.Awake();

            m_AutoFillButton.gameObject.SetActive(Debug.isDebugBuild);

            string authorizedDomain =
                ProFitGlobals.userProfile.authorizedDomain;

#if UNITY_EDITOR
            MelcherLog.Log("userType = " + ProFitGlobals.userProfile.userType);
            if (ProFitGlobals.userProfile.userType is UserType.Offline or UserType.None &&
                ProFitGlobals.appOptions.simulateWhiteLabelMode)
            {
                authorizedDomain = ProFitGlobals.appOptions.simulatedWhiteLabelDomain;
            }
#endif

            // The "I want to be contacted" checkbox is only used by Napoleon
            // in white label mode.
            bool isWhiteLabeledNapoleon =
                ProFitGlobals.session.ShouldUseWhiteLabelMode() &&
                authorizedDomain.Contains("napoleon.com", StringComparison.OrdinalIgnoreCase);

            m_WantsFileEmailed.gameObject.SetActive(isWhiteLabeledNapoleon);

            // Napoleon was using an opt-in system for clients to be contacted
            // by a dealer but have since switched to treating all clients as
            // wishing to be contacted:
            // https://3.basecamp.com/3293895/buckets/37470979/messages/10089918329
            m_WantsToBeContacted.gameObject.SetActive(false);

            // Cache the original field outline color
            m_FieldColorBlockDefault = m_FirstNameField.colors;

            // Create an outline color to mark a field as having an error
            m_FieldColorBlockError = m_FieldColorBlockDefault;
            m_FieldColorBlockError.normalColor = Const.ErrorColor;
            m_FieldColorBlockError.highlightedColor = Const.ErrorColor;
            m_FieldColorBlockError.pressedColor = Color.Lerp(Const.ErrorColor, Color.white, 0.3f);


            // Map the field key names used to communicate with the server to
            // the actual controls
            m_NameToSelectable = new Dictionary<string, Selectable>()
            {
                { "first_name",     m_FirstNameField },
                { "last_name",      m_LastNameField },
                { "email",          m_EmailField },
                { "phone_number",   m_PhoneNumberField },
                { "address",        m_AddressField },
                { "city",           m_CityField },
                { "state",          m_StateField },
                { "zip_code",       m_ZipCodeField },
            };
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_AutoFillButton.onClick += OnClickAutoFill;
            m_ShowTermsAndConditionsButton.onClick += OnClickShowTermsAndConditions;
            m_NextButton.onClick += OnClickNext;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_AutoFillButton.onClick -= OnClickAutoFill;
            m_ShowTermsAndConditionsButton.onClick -= OnClickShowTermsAndConditions;
            m_NextButton.onClick -= OnClickNext;
        }

        protected void OnClickAutoFill(MelcherButton caller)
        {
#if UNITY_EDITOR
            m_FirstNameField.text   = Utility.RandomNamesForEditor.RandomName();
            m_LastNameField.text    = Utility.RandomNamesForEditor.RandomName();
            m_EmailField.text       = m_FirstNameField.text + m_LastNameField.text + Random.Range(50, 99) + "@fake.com";
            m_PhoneNumberField.text = PhoneNumber.Format(PhoneNumber.Random());
            m_AddressField.text     = Random.Range(1, 9999) + " " + Utility.RandomNamesForEditor.RandomPlace();
            m_CityField.text        = Utility.RandomNamesForEditor.RandomPlace();
            m_StateField.text       = Utility.RandomNamesForEditor.RandomPlace();
            m_ZipCodeField.text     = Random.Range(10000, 99999).ToString();

            m_WantsFileEmailed.isOn = true;
            m_WantsToBeContacted.isOn = true;
            m_DidAcceptTerms.isOn = true;
#endif  // if UNITY_EDITOR
        }

        protected void OnClickShowTermsAndConditions(MelcherButton caller)
        {
            ProFitUtility.ShowTermsAndConditions();
        }

        protected void OnClickNext(MelcherButton caller)
        {
            if (!m_DidAcceptTerms.isOn)
            {
                m_MessageBoxUi.ShowWithError("The terms and conditions must be accepted to proceed.");
                return;
            }

            Timing.RunCoroutine(Routine());

            IEnumerator<float> Routine()
            {
                // There is no active user until this function succeeds. The
                // active client becomes whoever entered their info

                ProFitGlobals.activeClientId = null;

                UserClient? existingClient = null;

                // Prevent UI interactions during a web request
                m_CanvasGroup.interactable = false;

                using var restoreInteractable =
                    MelcherDeferred.Defer(
                        toCapture: m_CanvasGroup,
                        onDispose: canvasGroup => canvasGroup.interactable = true
                    );

                {
                    // Get the full client list so we can check if this is a
                    // new or similar user

                    MelcherWebCoroutine request =
                        ProFitGlobals.server.Clients.FindClientByEmail(
                            ProFitGlobals.user,
                            clientEmail: m_EmailField.text,
                            onComplete: (foundClient, newUserClient) =>
                            {
                                if (foundClient)
                                {
                                    existingClient = newUserClient;
                                }
                            }
                        );

                    yield return Timing.WaitUntilDone(request);
                }

                if (existingClient.HasValue)
                {
                    // The client already exists (at least, their email does).
                    // Update the existing client with their current details.

                    UserClient pendingClient    = existingClient.Value;
                    pendingClient.firstName     = m_FirstNameField.text;
                    pendingClient.lastName      = m_LastNameField.text;
                    pendingClient.email         = m_EmailField.text;
                    pendingClient.phoneNumber   = m_PhoneNumberField.text;
                    pendingClient.address       = m_AddressField.text;
                    pendingClient.city          = m_CityField.text;
                    pendingClient.state         = m_StateField.text;
                    pendingClient.zipCode       = m_ZipCodeField.text;

                    MelcherWebCoroutine request =
                        ProFitGlobals.server.Clients.UpdateClient(
                            ProFitGlobals.user,
                            pendingClient,
                            onSuccess: SetActiveClient,
                            onError: ShowError
                        );

                    yield return Timing.WaitUntilDone(request);
                }
                else
                {
                    MelcherWebCoroutine request =
                        ProFitGlobals.server.Clients.CreateClient(
                            ProFitGlobals.user,
                            m_FirstNameField.text,
                            m_LastNameField.text,
                            m_EmailField.text,
                            PhoneNumber.Unformat(m_PhoneNumberField.text),
                            m_AddressField.text,
                            m_CityField.text,
                            m_StateField.text,
                            m_ZipCodeField.text,
                            suppressEmail: Application.isEditor,
                            onSuccess: SetActiveClient,
                            onError: ShowError
                        );

                    yield return Timing.WaitUntilDone(request);
                }

                if (ProFitGlobals.activeClientId == null)
                {
                    yield break;
                }

                ProFitGlobals.activeClientWantsToBeContacted =
                    m_WantsToBeContacted.gameObject.activeSelf ?
                        m_WantsToBeContacted.isOn :
                        null;

                Hide();

                if (ProFitGlobals.appOptions.allowBuildingMethods)
                {
                    EventBridge.Designer.showBuildingMethodsUi.Invoke();
                }
                else if (Kitchen.instance.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
                {
                    EventBridge.Designer.showBrandCatalogUi.Invoke();
                }
                else
                {
                    EventBridge.Designer.showCabinetShapeUi.Invoke();
                }
            }

            void ShowError(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherLog.LogError(error);

                if (!jsonValue.isObject)
                {
                    return;
                }

                MelcherJsonObjectParser parser =
                    new MelcherJsonObjectParser(jsonValue.jsonObject);

                if (parser.TryGetErrorMap(key: "errors", out Dictionary<string, List<string>> errorMap))
                {
                    StringBuilder errorBuilder = new StringBuilder();

                    foreach (var issue in errorMap)
                    {
                        if (issue.Key == "terms")
                        {
                            // TODO: Red-out the terms checkbox
                            errorBuilder.AppendLine("You must agree to the terms.");
                        }
                        else if (m_NameToSelectable.TryGetValue(issue.Key, out Selectable control))
                        {
                            control.colors = m_FieldColorBlockError;

                            foreach (string e in issue.Value)
                            {
                                errorBuilder.AppendLine(e);
                            }
                        }
                        else
                        {
                            MelcherLog.LogError("Unhandled error map: " + issue.Key);
                        }
                    }

                    m_MessageBoxUi.ShowWithError(errorBuilder.ToString());

                    return;
                }
            }

            /// <summary>
            ///     Called when the white label client registration form is
            ///     created or updated. This assigns the created/updated client
            ///     as the active client.
            /// </summary>
            void SetActiveClient(UserClient activeClient)
            {
                ProFitGlobals.userClients.Clear();
                ProFitGlobals.userClients.Add(activeClient);

                ProFitGlobals.activeClientId = activeClient.id;
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(CabinetShapeUI))]

        [Space]
        [SerializeField] protected MelcherButton m_AutoFillButton = null;
        [SerializeField] protected TMP_InputField m_FirstNameField = null;
        [SerializeField] protected TMP_InputField m_LastNameField = null;
        [SerializeField] protected TMP_InputField m_EmailField = null;
        [SerializeField] protected TMP_InputField m_PhoneNumberField = null;
        [SerializeField] protected TMP_InputField m_AddressField = null;
        [SerializeField] protected TMP_InputField m_CityField = null;
        [SerializeField] protected TMP_InputField m_StateField = null;
        [SerializeField] protected TMP_InputField m_ZipCodeField = null;
        [SerializeField] protected MelcherToggle m_WantsFileEmailed = null;
        [SerializeField] protected MelcherToggle m_WantsToBeContacted = null;
        [SerializeField] protected MelcherButton m_ShowTermsAndConditionsButton = null;
        [SerializeField] protected MelcherToggle m_DidAcceptTerms = null;

        [Space]
        [SerializeField] protected MelcherButton m_NextButton = null;

        [Space]
        [SerializeField] protected InlineMessageBoxPromptUI m_MessageBoxUi = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected ColorBlock m_FieldColorBlockDefault;
        protected ColorBlock m_FieldColorBlockError;

        protected Dictionary<string, Selectable> m_NameToSelectable;

    }   // class WhiteLabelClientUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-30
/// ===========================================================================
