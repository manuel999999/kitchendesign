namespace Melcher
{
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// A small inline button up a password input. Pressing the button
    /// switches the password field between non-secure (visible password
    /// characters) and secure mode (asterisks/bullet characters).
    /// </summary>
    public class RevealPasswordButton : MelcherMonoBehaviour
    {
        public const string PhraseShow = "$(eye) Show";
        public const string PhraseHide = "$(eye-slash) Hide";

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetSecure(bool secure)
        {
            m_InputField.contentType = secure ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            m_InputField.ForceLabelUpdate();

            m_Label.phrase = secure ? PhraseShow : PhraseHide;
        }

        public bool IsSecure()
        {
            return m_InputField.contentType == TMP_InputField.ContentType.Password;
        }

        protected void OnEnable()
        {
            m_Button.onClick += ToggleSecure;
            SetSecure(IsSecure());
        }

        protected void OnDisable()
        {
            m_Button.onClick -= ToggleSecure;
        }

        protected void ToggleSecure(MelcherButton caller)
        {
            SetSecure(!IsSecure());
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected TMP_InputField m_InputField = null;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected MelcherButton m_Button = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected MelcherLocalizedText m_Label  = null;

    }   // class RevealPasswordButton

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-25
/// ===========================================================================
