namespace KitchenDesign.UI.MenuScreens
{
    using KitchenDesign.Utility;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Session;
    using Melcher;
    using UnityEngine;

    public class UnauthorizedDomainUI : MelcherUI
    {
        private SessionStatus _session;
        private AppOptionsSO _options;

        public void Initialize(SessionStatus session, AppOptionsSO options)
        {
            this._session = session;
            this._options = options;
        }

        public void SetFields(string authorizedDomain)
        {
            string authorizedDomains = "geminikitchendesigner.com";
            if (!string.IsNullOrEmpty(authorizedDomain))
            {
                authorizedDomains += "\n" + authorizedDomain;
            }
            m_AuthorizedDomainsLabel.phrase = authorizedDomains;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_SubscribeButton.onClick += ShowSubscriptionPage;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_SubscribeButton.onClick -= ShowSubscriptionPage;
        }

        protected void ShowSubscriptionPage(MelcherButton caller)
        {
            AppLinks.OpenSubscriptionLandingPage(this._options, this._session.UserProfile.userType);
        }

        [Header(nameof(UnauthorizedDomainUI))]

        [SerializeField] protected MelcherLocalizedText m_AuthorizedDomainsLabel = null;
        [SerializeField] protected MelcherButton m_SubscribeButton = null;

    }   // class UnauthorizedDomainUI

}   // namespace KitchenDesign.UI.MenuScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-06
/// ===========================================================================
