namespace Melcher
{
    using JetBrains.Annotations;
    using UnityEngine;

    /// <summary>
    ///
    /// </summary>
    [CreateAssetMenu(fileName = "AppOptions", menuName = "Melcher Studios/Pro-Fit/App Options")]
    public partial class AppOptions : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public string appDisplayName => m_AppDisplayName;

        public string fileSystemName => m_FileSystemName;

        public string versionSuffix => m_VersionSuffix;

        public AppSkin appSkin => m_AppSkin;

        public UserFilter userFilter => m_UserFilter;

        public bool allowOneDayTrial => m_AllowOneDayTrial;

        public bool allowBuildingMethods => m_AllowBuildingMethods;

        public bool allowDealerMarkupPercentChanges => m_AllowDealerMarkupPercentChanges;

        public KitchenBuildingMethod defaultBuildingMethod => m_DefaultBuildingMethod;

        public Sprite homeScreenImage => m_HomeScreenImage;

        public Sprite designerSceneLoadingImage => m_DesignerSceneLoadingImage;

        public string subscriptionUrlForDealerAccount => m_SubscriptionUrlForDealerAccount;

        public string subscriptionUrlForTrialAccount => m_SubscriptionUrlForTrialAccount;

        public CabinetDataAvailability cabinetDataAvailability => m_CabinetDataAvailability;

        public ProductIdRemapDatabase remappedProductIdDatabase => m_RemappedProductIdDatabase;

        // ====================================================================
        // Methods
        // ====================================================================

        public int GetDefaultManufacturerId(ServerType serverType)
        {
            return serverType switch
            {
                ServerType.Production => m_DefaultManufacturerId,
                ServerType.Test => m_TestDefaultManufacturerId,
                _ => throw new System.InvalidOperationException($"Cannot provide default manufacturer ID because server type '{serverType}' is not handled.")
            };
        }

        public string GetServerDomain(ServerType serverType)
        {
            return serverType switch
            {
                ServerType.Production => m_ProductionServerDomain,
                ServerType.Test => m_TestServerDomain,
                _ => throw new System.InvalidOperationException($"Cannot provide server domain because server type '{serverType}' is not handled.")
            };
        }

        public GroupDatabase GetManufacturerDatabase(ServerType serverType)
        {
            return serverType switch
            {
                ServerType.Production => m_ManufacturerDatabase,
                ServerType.Test => m_TestManufacturerDatabase,
                _ => throw new System.InvalidOperationException($"Cannot provide manufacturer database because server type '{serverType}' is not handled.")
            };
        }

        public GroupDatabase GetProductGroupDatabase(ServerType serverType)
        {
            return serverType switch
            {
                ServerType.Production => m_ProductGroupDatabase,
                ServerType.Test => m_TestProductGroupDatabase,
                _ => throw new System.InvalidOperationException($"Cannot provide product group database because server type '{serverType}' is not handled.")
            };
        }

        public GroupDatabase GetBrandCatalogLogoDatabase(ServerType serverType)
        {
            return serverType switch
            {
                ServerType.Production => m_BrandCatalogLogoDatabase,
                ServerType.Test => m_TestBrandCatalogLogoDatabase,
                _ => throw new System.InvalidOperationException($"Cannot provide brand catalog logo database because server type '{serverType}' is not handled.")
            };
        }

        public bool IsKitchenStyleAllowed(KitchenStyle kitchenStyle)
        {
            foreach (MelcherStableEnum<KitchenStyle> forbiddenKitchenStyle in m_DisallowedKitchenStyles)
            {
                if (forbiddenKitchenStyle.value == kitchenStyle)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnDisable()
        {
            PlayerPrefs.Save();
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Header("Runtime")]

        [SerializeField]
        protected string m_AppDisplayName = default;

        [Tooltip("A simple (preferably one-word) name that is used to " +
            "filter files or assets specific to this app.")]
        [SerializeField]
        protected string m_FileSystemName = default;

        [Tooltip("Used to disambiguate the different app profiles now that " +
            "some of them use identical looking UI skins.")]
        [SerializeField]
        protected string m_VersionSuffix;

        [Tooltip("What type of UI theme should be used.")]
        [SerializeField]
        protected AppSkin m_AppSkin = default;

        // TODO: Look at making this a scriptable object. Enums break if the
        //       order of their defined values ever change.
        [Tooltip("This controls how new users are tagged to be filtered " +
            "in the dashboard. This value should match whichever site " +
            "the app is being built for. If it matches none of the " +
            "companies, consider adding a new filter to UserFilter or use " +
            "Pro-Fit as a default.")]
        [SerializeField]
        protected UserFilter m_UserFilter = default;

        [SerializeField]
        protected int m_DefaultManufacturerId = 0;

        [SerializeField]
        protected int m_TestDefaultManufacturerId = 0;

        [Tooltip("Whether users are allowed to register using the 1-day " +
            "trial option on the Welcome UI.")]
        [SerializeField]
        protected bool m_AllowOneDayTrial = true;

        [Tooltip("Whether the app will prompt the user to choose a building " +
            "method like Sections mode.")]
        [SerializeField]
        protected bool m_AllowBuildingMethods = true;

        [Tooltip("Whether this app will display the dealer markup percentages " +
            "in the manufacturer list and allow the user to edit their values.")]
        [SerializeField]
        protected bool m_AllowDealerMarkupPercentChanges = default;

        [Tooltip("The types of kitchen styles that are disallowed by this " +
            "profile. This is primarily used to limit Diagonal-L style " +
            "kitchens to Pro-Fit only.")]
        [SerializeField]
        protected MelcherStableEnum<KitchenStyle>[] m_DisallowedKitchenStyles;

        [Tooltip("The default building method to use. If the app does not " +
            "allow building methods (`m_AllowBuildingMethods` is false) then " +
            "this building mode is permanent.")]
        [SerializeField]
        protected MelcherStableEnum<KitchenBuildingMethod> m_DefaultBuildingMethod = new (KitchenBuildingMethod.Standard);

        [SerializeField]
        protected Sprite m_HomeScreenImage = default;

        [SerializeField]
        protected Sprite m_DesignerSceneLoadingImage = default;

        [Space]

        [Tooltip("The server's base domain. No scheme (http://, https://) " +
            "or trailing slash. E.g., \"app.profitoutdoorliving.com\".")]
        [SerializeField]
        protected string m_ProductionServerDomain = default;

        [Tooltip("The server's base domain. No scheme (http://, https://) " +
            "or trailing slash. E.g., \"profit.melcher.ca\".")]
        [SerializeField]
        protected string m_TestServerDomain = default;


        [Tooltip("The URL new users that sign up for a dealer account are " +
            "taken to.")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_AppSubscriptionLandingPageUrl")]
        protected string m_SubscriptionUrlForDealerAccount = default;

        [SerializeField]
        protected string m_SubscriptionUrlForTrialAccount = default;

        [SerializeField]
        protected MelcherStableEnum<CabinetDataAvailability> m_CabinetDataAvailability = new (CabinetDataAvailability.None);

        [Space]

        [SerializeField]
        protected GroupDatabase m_ManufacturerDatabase = default;

        [SerializeField]
        protected GroupDatabase m_ProductGroupDatabase = default;

        [SerializeField]
        protected GroupDatabase m_BrandCatalogLogoDatabase = default;

        [Space]

        [SerializeField]
        protected GroupDatabase m_TestManufacturerDatabase = default;

        [SerializeField]
        protected GroupDatabase m_TestProductGroupDatabase = default;

        [SerializeField]
        protected GroupDatabase m_TestBrandCatalogLogoDatabase = default;

        [Space]

        [Tooltip("This is a database of known product SKUs and their " +
            "corresponding product IDs. This is used to recover old save " +
            "files that recorded products via their SKUs which are not " +
            "guaranteed to be constant.")]
        [SerializeField]
        protected ProductIdRemapDatabase m_RemappedProductIdDatabase = default;

        // ====================================================================
        // Nested types
        // ====================================================================

        public enum CabinetDataAvailability
        {
            /// <summary>
            ///     No cabinet data available for this app profile.
            /// </summary>
            None,

            /// <summary>
            ///     In Stoll, the dashboard does not support cabinet data.
            ///     However, Pro-Fit still wants authored cabinets here. To
            ///     compensate, we store the cabinet data locally with the app.
            /// </summary>
            ClientOnly,

            /// <summary>
            ///     The server has the ability to define and return cabinet
            ///     data to the app. Used by Gemini.
            /// </summary>
            Server
        }

    }   // class AppOptions

}   // namespace Melcher
