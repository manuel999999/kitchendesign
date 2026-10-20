namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// A prompt that appears when a user is forcibly logged out of their
    /// account. E.g., their trial expires.
    /// </summary>
    public class KickedUserPromptUI : MelcherUI
    {
        public void SetReason(string kickReason)
        {
            m_PromptMessage.phrase = kickReason;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_ContinueButton.onClick += OnClickContinue;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_ContinueButton.onClick -= OnClickContinue;
        }

        protected void OnClickContinue(MelcherButton caller)
        {
            Hide();
        }

        [Header(nameof(KickedUserPromptUI))]

        [SerializeField] protected MelcherLocalizedText m_PromptMessage = null;
        [SerializeField] protected MelcherButton m_ContinueButton = null;

    }   // class KickedUserPromptUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-06
/// ===========================================================================
