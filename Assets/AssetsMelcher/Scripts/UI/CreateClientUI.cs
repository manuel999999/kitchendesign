namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;
    using TMPro;

    /// <summary>
    /// This manages the UI state of the Create Client page. This is the page
    /// that is displayed in the Menu Scene and differs from the white label
    /// client page shown in the designer page.
    /// </summary>
    public class CreateClientUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Set all the fields to be empty. Scroll the form to the top.
        /// </summary>
        public void ClearForm()
        {
            m_ScrollView.verticalNormalizedPosition = 1.0f; // Top

            m_FirstNameField.text       = string.Empty;
            m_LastNameField.text        = string.Empty;
            m_EmailField.text           = string.Empty;
            m_AddressField.text         = string.Empty;
            m_CityField.text            = string.Empty;
            m_StateField.text           = string.Empty;
            m_ZipCodeField.text         = string.Empty;
            m_PhoneNumberField.text     = string.Empty;

            m_MessageBox.Clear();
        }

        protected override void Awake()
        {
            base.Awake();

            m_AutoFillButton.gameObject.SetActive(Application.isEditor || Debug.isDebugBuild);

            // Ensure the list's mask is enabled during runtime. It's nice to
            // have it off during development so we can see the whole list
            m_ScrollView.viewport.GetComponent<Mask>().enabled = true;

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

            m_AutoFillButton.onClick += AutoFillForm;
            m_ContinueButton.onClick += TryCreateClient;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_AutoFillButton.onClick -= AutoFillForm;
            m_ContinueButton.onClick -= TryCreateClient;
        }

        protected void AutoFillForm(MelcherButton caller)
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
#endif  // if UNITY_EDITOR
        }


        /// <summary>
        /// Called when the user clicks the Continue button. It attempts to
        /// register a new client with the data currently entered in the
        /// fields.
        /// </summary>
        /// <param name="caller"></param>
        protected void TryCreateClient(MelcherButton caller)
        {
            m_MessageBox.Clear();
            EventBridge.Menu.showFullScreenSpinner?.Invoke(true);

            foreach (var kv in m_NameToSelectable)
            {
                kv.Value.colors = m_FieldColorBlockDefault;
            }

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
                onSuccess: OnSuccess,
                onError: OnError
            );

            void OnSuccess(UserClient client)
            {
                EventBridge.Menu.showFullScreenSpinner?.Invoke(false);
                ClearForm();

                // Mark the new client as active so any quotes will belong to
                // them
                ProFitGlobals.userClients.Add(client);
                ProFitGlobals.activeClientId = client.id;
                ProFitGlobals.activeQuoteId = null;

                MelcherSceneManager.LoadSceneAsync(
                    MelcherSceneRequest.FromName("DesignerScene"),
                    MelcherSceneLoadMode.Single
                );

                EventBridge.Menu.showFullScreenSpinner?.Invoke(true);
            }

            void OnError(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.showFullScreenSpinner?.Invoke(false);
                MelcherLog.LogError(error);

                if (!jsonValue.isObject)
                {
                    return;
                }

                MelcherJsonObjectParser parser = new MelcherJsonObjectParser(jsonValue.jsonObject);

                if (parser.TryGetErrorMap("errors", out Dictionary<string, List<string>> errorMap))
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
                                MelcherLog.LogError($"{issue.Key}: {e}");
                                MelcherLog.Log($"{e}");
                                errorBuilder.AppendLine(e);
                            }
                        }
                        else
                        {
                            MelcherLog.LogError("Unhandled error map: " + issue.Key);
                        }
                    }

                    m_MessageBox.Error(errorBuilder.ToString());

                    return;
                }
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(CreateClientUI))]

        [SerializeField] protected ScrollRect m_ScrollView = null;

        [SerializeField] protected MelcherButton m_AutoFillButton = null;
        [SerializeField] protected TMP_InputField m_FirstNameField = null;
        [SerializeField] protected TMP_InputField m_LastNameField = null;
        [SerializeField] protected TMP_InputField m_EmailField = null;
        [SerializeField] protected TMP_InputField m_AddressField = null;
        [SerializeField] protected TMP_InputField m_CityField = null;
        [SerializeField] protected TMP_InputField m_StateField = null;
        [SerializeField] protected TMP_InputField m_ZipCodeField = null;
        [SerializeField] protected TMP_InputField m_PhoneNumberField = null;
        [SerializeField] protected MelcherButton m_ContinueButton = null;

        [Space]
        [SerializeField] protected InlineMessageBox m_MessageBox = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected ColorBlock m_FieldColorBlockDefault;
        protected ColorBlock m_FieldColorBlockError;

        protected Dictionary<string, Selectable> m_NameToSelectable;

    }   // class CreateClientUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
