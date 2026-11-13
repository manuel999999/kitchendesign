namespace KitchenDesign.Utility
{
    using KitchenDesign.Shared.Enums;
    using UnityEngine;
    using Conditional = System.Diagnostics.ConditionalAttribute;

    public static class AppLinks
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void OpenProductUrl(int productServerId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            const string Endpoint = "/admin/components/{product_id}/edit";

            Application.OpenURL(
                ProFitGlobals.server.Connection.Host + Endpoint.Replace("{product_id}", productServerId.ToString())
            );
#endif
        }

        public static void OpenSubscriptionLandingPage()
        {
            string subscriptionUrl =
                ProFitGlobals.userProfile.userType switch
                {
                    UserType.Trial => ProFitGlobals.appOptions.subscriptionUrlForTrialAccount,
                    _ => ProFitGlobals.appOptions.subscriptionUrlForDealerAccount
                };

            Application.OpenURL(subscriptionUrl);
        }

        public static void ShowTermsAndConditions()
        {
            const string Endpoint = "/webgl/privacy-policy.html";

            Application.OpenURL(ProFitGlobals.server.Connection.Host + Endpoint);
        }
    }
}
