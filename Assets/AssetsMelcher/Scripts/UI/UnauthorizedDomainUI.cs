namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    public class UnauthorizedDomainUI : MelcherUI
    {
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
            ProFitUtility.OpenSubscriptionLandingPage();
        }

        [Header(nameof(UnauthorizedDomainUI))]

        [SerializeField] protected MelcherLocalizedText m_AuthorizedDomainsLabel = null;
        [SerializeField] protected MelcherButton m_SubscribeButton = null;

    }   // class UnauthorizedDomainUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-06
/// ===========================================================================
