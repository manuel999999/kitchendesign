namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     This is a UI canvas that contains an In-line Message Box (for
    ///     displaying server response errors) and an OK button. It is used by
    ///     the <see cref="WhiteLabelClientInfoUI"/> to display sign up errors
    ///     because there isn't enough room to display an in-line message box
    ///     on <see cref="WhiteLabelClientInfoUI"/>.
    /// </summary>
    public class InlineMessageBoxPromptUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ClearForm()
        {
            m_MessageBox.Clear();
        }

        public void ShowWithError(string error)
        {
            m_MessageBox.Error(error);
            Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_OkButton.onClick += OnClickOk;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_OkButton.onClick -= OnClickOk;
        }

        protected void OnClickOk(MelcherButton caller)
        {
            Hide();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [MelcherHeader]

        [Space]
        [SerializeField] protected InlineMessageBox m_MessageBox = default;
        [SerializeField] protected MelcherButton m_OkButton = null;

    }   // class InlineMessageBoxPromptUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-22
/// ===========================================================================
