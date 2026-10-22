namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using KitchenDesign.AppRules;
    using KitchenDesign.CatalogData;
    using KitchenDesign.Session;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Interfaces;

    public static class ProFitGlobals
    {
        private static AppProfile s_AppProfile;
        private static IAppRules s_AppRules;
        private static LaunchOptions s_LaunchOptions;
        private static CatalogDataStatus s_CatalogData;
        private static ClientListStatus s_ClientList;
        private static SessionStatus s_Session;

        public static void Bind(
            AppProfile appProfile,
            IAppRules appRules,
            LaunchOptions launchOptions,
            CatalogDataStatus catalogData,
            ClientListStatus clientList,
            SessionStatus session)
        {
            s_AppProfile = appProfile;
            s_AppRules = appRules;
            s_LaunchOptions = launchOptions;
            s_CatalogData = catalogData;
            s_ClientList = clientList;
            s_Session = session;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBindings()
        {
            Bind(null, null, null, null, null, null);
        }

        public static AppProfile appProfile
        {
            get
            {
                if (s_AppProfile == null)
                {
                    s_AppProfile = Resources.Load<AppProfile>(nameof(AppProfile));

                    if (!s_AppProfile)
                    {
                        MelcherLog.LogError("Failed to load app profile.");
                    }
                }

                return s_AppProfile;
            }
        }

        public static AppOptions appOptions => appProfile.appOptions;

        public static IAppRules appRules => s_AppRules ??= AppRulesFactory.Create(appOptions.userFilter);

        public static LaunchOptions launchOptions => s_LaunchOptions ??= LaunchOptions.FromEnvironment(appOptions);

        public static CatalogDataStatus catalogData => s_CatalogData ??= new CatalogDataStatus(appProfile);

        public static ClientListStatus clientList => s_ClientList ??= new ClientListStatus();

        public static SessionStatus session => s_Session ??= new SessionStatus(appProfile, clientList, catalogData);

        public static bool isVerbose => launchOptions.IsVerbose;

        public static LogInState logInCoroutineState
        {
            get => session.LogInState;
            set => session.LogInState = value;
        }

        public static bool useWhiteLabelMode
        {
            get => session.UseWhiteLabelMode;
            set => session.UseWhiteLabelMode = value;
        }

        public static bool isKioskModeEnabled
        {
            get => session.IsKioskModeEnabled;
            set => session.IsKioskModeEnabled = value;
        }

        public static User user
        {
            get => session.User;
            set => session.User = value;
        }

        public static UserProfile userProfile
        {
            get => session.UserProfile;
            set => session.UserProfile = value;
        }

        public static List<UserClient> userClients
        {
            get => clientList.UserClients;
            set => clientList.UserClients = value;
        }

        public static int? activeClientId
        {
            get => clientList.ActiveClientId;
            set => clientList.ActiveClientId = value;
        }

        public static bool? activeClientWantsToBeContacted
        {
            get => clientList.ActiveClientWantsToBeContacted;
            set => clientList.ActiveClientWantsToBeContacted = value;
        }

        public static int? activeQuoteId
        {
            get => clientList.ActiveQuoteId;
            set => clientList.ActiveQuoteId = value;
        }

        public static List<CabinetSectionCategoryInfo> cabinetSectionCategories => catalogData.CabinetSectionCategories;

        public static List<BrandCatalogInfo> serverBrandCatalogs => catalogData.ServerBrandCatalogs;

        public static List<BrandCatalog> runtimeBrandCatalogs => catalogData.RuntimeBrandCatalogs;

        public static List<ManufacturerInfo> manufacturers => catalogData.Manufacturers;

        public static List<ProductGroupInfo> productGroups => catalogData.ProductGroups;

        public static List<ProductInfo> sourceProducts => catalogData.SourceProducts;

        public static List<ProductInfo> runtimeProducts => catalogData.RuntimeProducts;

        public static Dictionary<int, List<int>> baseProductIdToProductVariantIds => catalogData.BaseProductIdToProductVariantIds;

        public static void ExecuteWhenReady(Action action)
        {
            catalogData.ExecuteWhenLoaded(action);
        }

        public static void AddRuntimeProduct(ProductInfo productInfo)
        {
            catalogData.AddRuntimeProduct(productInfo);
        }

        public static void ClearRuntimeProducts()
        {
            catalogData.ClearRuntimeProducts();
        }

        public static bool TryGetClientAndQuoteIndex(int clientId, int quoteId, out int clientIndex, out int quoteIndex, bool logErrorIfNotFound = true)
        {
            return clientList.TryGetClientAndQuoteIndex(clientId, quoteId, out clientIndex, out quoteIndex, logErrorIfNotFound);
        }

        public static bool TryGetClientIndex(int clientId, out int clientIndex, bool logErrorIfNotFound = true)
        {
            return clientList.TryGetClientIndex(clientId, out clientIndex, logErrorIfNotFound);
        }

        public static string TryGetClientCombinedName()
        {
            return clientList.TryGetClientCombinedName();
        }

        public static void ForgetRememberMeData()
        {
            session.ForgetRememberMeData();
        }

        public static void LogOut()
        {
            session.LogOut();
        }

        public static void KickUser(string kickReason)
        {
            session.KickUser(kickReason);
        }
    }
}
