namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// The prompt that appears when confirming if the user wants to sign out.
    /// </summary>
    public class UserLogOutPromptUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        protected override void OnEnable()
        {
            base.OnEnable();

            m_CancelButton.onClick  += OnClickCancel;
            m_LogOutButton.onClick  += OnClickLogOut;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_CancelButton.onClick  -= OnClickCancel;
            m_LogOutButton.onClick  -= OnClickLogOut;
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            Hide();
        }

        protected void OnClickLogOut(MelcherButton caller)
        {
            Hide();

            ProFitGlobals.LogOut();

            EventBridge.Menu.showWelcomeUi.Invoke(MelcherUITransitionStyle.Regress);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(UserLogOutPromptUI))]

        [SerializeField] protected MelcherButton m_CancelButton = null;
        [SerializeField] protected MelcherButton m_LogOutButton = null;

    }   // class UserLogOutPromptUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-10
/// ===========================================================================
