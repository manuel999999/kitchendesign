namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using TMPro;

    public class EditProfilePromptRow : MelcherMonoBehaviour
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public string value => m_InputField.text;

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetFields(string header, string value, TMP_InputField.ContentType contentType, TouchScreenKeyboardType keyboardType, TMP_InputValidator inputValidator)
        {
            m_Header.phrase = header;

            bool isPassword = contentType == TMP_InputField.ContentType.Password;
            m_RevealPasswordButton.gameObject.SetActive(isPassword);
            m_RevealPasswordButton.SetSecure(isPassword);

            m_InputField.text = value;
            m_InputField.contentType = contentType;
            m_InputField.keyboardType = keyboardType;

            switch (contentType)
            {
                case TMP_InputField.ContentType.Custom:
                    // Use the custom input validator. This will select the
                    // phone number validator as its the only custom validator
                    // used.
                    m_InputField.characterValidation = TMP_InputField.CharacterValidation.CustomValidator;
                    break;

                case TMP_InputField.ContentType.EmailAddress:
                    m_InputField.characterValidation = TMP_InputField.CharacterValidation.EmailAddress;
                    break;

                default:
                    m_InputField.characterValidation = TMP_InputField.CharacterValidation.None;
                    break;
            }

            m_InputField.inputValidator = inputValidator;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected MelcherLocalizedText m_Header = null;
        [SerializeField] protected RevealPasswordButton m_RevealPasswordButton = null;
        [SerializeField] protected TMP_InputField m_InputField = null;
    }   // class EditProfilePromptRow

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-02
/// ===========================================================================
