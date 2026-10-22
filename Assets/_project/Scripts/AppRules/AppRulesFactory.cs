using KitchenDesign.Shared.Enums;
using System;
using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign.AppRules
{
    public static class AppRulesFactory
    {
        public static IAppRules Create(UserFilter userFilter)
        {
            return userFilter switch
            {
                UserFilter.ProFit => new ProFitAppRules(),
                UserFilter.Stoll => new StollAppRules(),
                UserFilter.Gemini => new GeminiAppRules(),
                _ => throw new InvalidOperationException($"No app rules for user filter '{userFilter}'.")
            };
        }
    }
}
