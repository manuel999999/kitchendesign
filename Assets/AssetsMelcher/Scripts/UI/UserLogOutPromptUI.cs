namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using KitchenDesign.Events;
    using KitchenDesign.Session;

    /// <summary>
    /// The prompt that appears when confirming if the user wants to sign out.
    /// </summary>
    public class UserLogOutPromptUI : MelcherUI
    {
        private SessionStatus _session;

        public void Initialize(SessionStatus session)
        {
            this._session = session;
        }

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

            this._session.LogOut();

            EventSession.OnWelcomeRequested.Raise();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(UserLogOutPromptUI))]

        [SerializeField] protected MelcherButton m_CancelButton = null;
        [SerializeField] protected MelcherButton m_LogOutButton = null;

    }   // class UserLogOutPromptUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-10
/// ===========================================================================
