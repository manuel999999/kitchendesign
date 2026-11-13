namespace KitchenDesign.Utility
{
    using KitchenDesign.Scriptables;
    using KitchenDesign.Shared.Enums;
    using UnityEngine;
    using Conditional = System.Diagnostics.ConditionalAttribute;

    public static class AppLinks
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void OpenProductUrl(string serverHost, int productServerId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            const string Endpoint = "/admin/components/{product_id}/edit";

            Application.OpenURL(
                serverHost + Endpoint.Replace("{product_id}", productServerId.ToString())
            );
#endif
        }

        public static void OpenSubscriptionLandingPage(AppOptionsSO options, UserType userType)
        {
            string subscriptionUrl =
                userType switch
                {
                    UserType.Trial => options.subscriptionUrlForTrialAccount,
                    _ => options.subscriptionUrlForDealerAccount
                };

            Application.OpenURL(subscriptionUrl);
        }

        public static void ShowTermsAndConditions(string serverHost)
        {
            const string Endpoint = "/webgl/privacy-policy.html";

            Application.OpenURL(serverHost + Endpoint);
        }
    }
}
