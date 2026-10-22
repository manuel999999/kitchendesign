namespace KitchenDesign
{
    using Melcher;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using MEC;

    using JsonList = System.Collections.Generic.IList<object>;
    using JsonDict = System.Collections.Generic.IDictionary<string, object>;

    /// <summary>
    ///     Global data used throughout the app. This object initializes at
    ///     the start of the app and allows other scripts to trigger an action
    ///     upon its initialization completing. See <see
    ///     cref="ExecuteWhenReady(Action)"/>.
    /// </summary>
    public static class ProFitGlobals
    {
        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        ///     Whether the <see cref="ProFitGlobals"/> system has initialized.
        /// </summary>
        public static bool isInitialized { get; private set; }

        /// <summary>
        ///     Whether the user's log in process has downloaded all necessary
        ///     data. This was added to allow the user to begin navigating
        ///     the main menu before they downloaded all the products.
        /// </summary>
        public static LogInCoroutineState logInCoroutineState { get; set; }

        /// <summary>
        ///     <see langword="true"/> if the app should use white label mode
        ///     in the designer scene. This is used instead of <see
        ///     cref="UserProfile.isWhiteLabeled"/> to simplify the need to
        ///     modify <see cref="UserProfile.isWhiteLabeled"/> depending on
        ///     the platform/app mode.
        /// </summary>
        public static bool useWhiteLabelMode { get; set; }

        /// <summary>
        ///     Kiosk mode protects sensitive areas of the app with a PIN.
        ///     Currently, those sensitive areas are the client list and the
        ///     account's profile menu.
        /// </summary>
        public static bool isKioskModeEnabled
        {
            get => s_IsKioskModeEnabled;
            set
            {
                s_IsKioskModeEnabled = value;

                string kioskModePrefsKey = GetKioskModePlayerPrefKey(user);
                PlayerPrefs.SetInt(kioskModePrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        ///     Whether the app is running in verbose mode (which can be seen
        ///     as a debug mode: more log statements, development buttons,
        ///     etc.)
        /// </summary>
        public static bool isVerbose { get; set; }

        /// <summary>
        ///     The active user.
        /// </summary>
        public static User user
        {
            get => m_User;
            set
            {
                if (value.userType != m_User.userType)
                {
                    MelcherLog.LogEditorOnly("Changing user type to " + value.userType);
                }

                m_User = value;

                // Recover the kiosk-mode preference from the device's local
                // storage.

                // REVIEW: This is exploitable. The registry can be changed by
                //         users who do not know the PIN or account password.
                //         -Ryan

                string kioskModePrefsKey =
                    GetKioskModePlayerPrefKey(value);

                s_IsKioskModeEnabled =
                    PlayerPrefs.GetInt(kioskModePrefsKey) != 0;
            }
        }
        private static User m_User;

        /// <summary>
        ///     The active user's profile.
        /// </summary>
        public static UserProfile userProfile;

        /// <summary>
        ///     The list of clients that belong to the active user.
        /// </summary>
        public static List<UserClient> userClients = new List<UserClient>(capacity: 0);

        /// <summary>
        ///     The list of cabinet section categories defined by the server.
        ///     This is used by the Gemini server (Catalog building method).
        /// </summary>
        public static List<CabinetSectionCategoryInfo> cabinetSectionCategories = new (capacity: 0);

        /// <summary>
        ///     The list of brand catalogs as defined by the server.
        ///     This is used by the Gemini server (Catalog building method).
        /// </summary>
        public static List<BrandCatalogInfo> serverBrandCatalogs = new (capacity: 0);

        /// <summary>
        ///     The list of brand catalog objects. These items contain many
        ///     more settings than are exposed in the front end and therefore
        ///     supersede <see cref="BrandCatalogInfo"/>.
        ///     <br /><br />
        ///
        ///     This list is initially populated with the entries from <see
        ///     cref="AppProfile.presetBrandCatalogs"/> but is also expanded to
        ///     include server-defined catalogs (these will be cloned from
        ///     the best-matching preset).
        /// </summary>
        public static List<BrandCatalog> runtimeBrandCatalogs = new List<BrandCatalog>();

        /// <summary>
        ///     The list of known manufacturers. This is populated during two
        ///     events:
        ///     <br /><br />
        ///
        ///     1.  <see cref="Init">On app startup</see> (by loading from a
        ///         cached list of manufacturers). This may be out-of-date.
        ///         <br />
        ///
        ///     2.  On user sign-in.<br />
        ///
        ///     <br />
        ///
        ///     There is also a Manufacturer Database asset in Resources.
        ///     It binds the server manufacturer names to a sprite and display
        ///     text.
        /// </summary>
        public static List<ManufacturerInfo> manufacturers = new List<ManufacturerInfo>(capacity: 0);

        /// <summary>
        ///     The list of known product groups. Like <see
        ///     cref="manufacturers"/>, this is populated during two events.
        ///     <br /><br />
        ///
        ///     Product groups are a way to organize collections of products
        ///     that may be built by different manufacturers.
        /// </summary>
        public static List<ProductGroupInfo> productGroups = new List<ProductGroupInfo>(capacity: 0);

        /// <summary>
        ///     The list of product tags used to set compatibilities between
        ///     products and cabinets in Gemini builds.
        /// </summary>
        public static List<ProductTagInfo> productTags = new List<ProductTagInfo>(capacity: 0);

        public static List<CabinetCompatibilityInfo> cabinetCompatibilities = new List<CabinetCompatibilityInfo>(capacity: 0);

        /// <summary>
        ///     The list of known products. Products are managed in the server
        ///     front-end each references a product visual by its ID. (Product
        ///     visuals are defined in the product visual database, which is a
        ///     project-local asset.)
        /// </summary>
        public static List<ProductInfo> sourceProducts = new List<ProductInfo>(capacity: 0);

        /// <summary>
        ///     It turns out we need to override some values that were entered
        ///     in the dashboard. Currently, this is just the bartop model
        ///     whose size is informed by the <see cref="ProductInfo"/>'s
        ///     product size, and it differs between Pro-Fit and Stoll.
        ///
        ///     Use <see cref="AddRuntimeProduct"/> and <see
        ///     cref="ClearRuntimeProducts"/> to modify this list so that the
        ///     lookup tables stay in sync.
        /// </summary>
        public static List<ProductInfo> runtimeProducts = new List<ProductInfo>(capacity: 0);

        /// <summary>
        ///     Lookup tables for <see cref="runtimeProducts"/>. Cabinet sections
        ///     and products are stored in the same list and can share a server
        ///     ID, so the key also records whether the entry is a cabinet
        ///     section.
        /// </summary>
        private static readonly Dictionary<(int id, bool isCabinetSection), ProductInfo> s_RuntimeProductsById = new ();

        /// <inheritdoc cref="s_RuntimeProductsById"/>
        private static readonly Dictionary<(string sku, bool isCabinetSection), ProductInfo> s_RuntimeProductsBySku = new ();

        public static void AddRuntimeProduct(ProductInfo productInfo)
        {
            runtimeProducts.Add(productInfo);

            bool isCabinetSection = productInfo.category == ProductCategory.CabinetSection;

            // TryAdd keeps the first entry, matching the previous FindIndex
            // behaviour when there are duplicates.
            s_RuntimeProductsById.TryAdd((productInfo.id, isCabinetSection), productInfo);

            if (productInfo.sku != null)
            {
                s_RuntimeProductsBySku.TryAdd((productInfo.sku, isCabinetSection), productInfo);
            }
        }

        public static void ClearRuntimeProducts()
        {
            runtimeProducts.Clear();
            s_RuntimeProductsById.Clear();
            s_RuntimeProductsBySku.Clear();
        }

        public static ProductInfo FindRuntimeProductById(int productServerId, bool findCabinetSection)
        {
            s_RuntimeProductsById.TryGetValue((productServerId, findCabinetSection), out ProductInfo result);
            return result;
        }

        public static ProductInfo FindRuntimeProductBySku(string sku, bool findCabinetSection)
        {
            if (string.IsNullOrEmpty(sku))
            {
                return null;
            }

            s_RuntimeProductsBySku.TryGetValue((sku, findCabinetSection), out ProductInfo result);
            return result;
        }

        /// <summary>
        ///     This maps the product IDs that represent base products to their
        ///     list of product variants. Products that are variants already
        ///     know their base (<see cref="ProductInfo.baseProductId"/>) so
        ///     this dictionary helps us navigate in the opposite direction.
        /// </summary>
        public static Dictionary<int, List<int>> baseProductIdToProductVariantIds = new ();

        /// <inheritdoc cref="s_ActiveClientId"/>
        public static int? activeClientId
        {
            get => s_ActiveClientId;
            set
            {
                if (value != s_ActiveClientId)
                {
                    MelcherLog.LogEditorOnly("Active client ID is now: " + value);
                    s_ActiveClientId = value;
                }
            }
        }

        /// <summary>
        ///     This is the ID of the client that was selected, either in the
        ///     Client List or by self sign up in WebGL's white labeled mode.
        /// </summary>
        private static int? s_ActiveClientId;

        /// <summary>
        ///     Napoleon previously required clients to opt-into being contacted
        ///     by a dealer. This has since changed (Napoleon clients are always
        ///     contacted now), but we'll keep the feature here in case it is
        ///     needed in the future.
        /// </summary>
        public static bool? activeClientWantsToBeContacted
        {
            get => s_ActiveClientWantsToBeContacted;
            set => s_ActiveClientWantsToBeContacted = value;
        }

        private static bool? s_ActiveClientWantsToBeContacted;

        /// <inheritdoc cref="s_ActiveQuoteId"/>
        public static int? activeQuoteId
        {
            get => s_ActiveQuoteId;
            set
            {
                if (value != s_ActiveQuoteId)
                {
                    MelcherLog.LogEditorOnly("Active quote ID is now: " + value);
                    s_ActiveQuoteId = value;
                }
            }
        }

        /// <summary>
        ///     This is the ID of the last selected quote. This communicates
        ///     which design should be loaded between the menu scene and the
        ///     designer scene.
        /// </summary>
        private static int? s_ActiveQuoteId;

        public static AppProfile appProfile
        {
            get
            {
                AppProfile value = Resources.Load<AppProfile>(nameof(AppProfile));
                if (!value)
                {
                    MelcherLog.LogError("Failed to load app profile.");
                }
                return value;
            }
        }

        /// <summary>
        ///     The options for the app that help differentiate the types of
        ///     builds (e.g., Pro-Fit vs Stoll). This field is only populated
        ///     at runtime. It is declared here to avoid making the options
        ///     a singleton, in case it is necessary to write tests using a
        ///     different set of app options.
        /// </summary>
        /// <remarks>
        ///     If you need access to these options at edit-time, see <see
        ///     cref="LoadRuntimeAppOptions"/>.
        /// </remarks>
        public static AppOptions appOptions => appProfile.appOptions;

        /// <summary>
        ///     <see langword="true"/> if the app is currently configured for
        ///     the Stoll build. See <see cref="appOptions"/> and <see
        ///     cref="UserFilter"/>.
        /// </summary>
        public static bool isAppOptionGemini = appOptions.userFilter == UserFilter.Gemini;
        public static bool isAppOptionProFit = appOptions.userFilter == UserFilter.ProFit;
        public static bool isAppOptionStoll = appOptions.userFilter == UserFilter.Stoll;

        // ====================================================================
        // Static methods
        // ====================================================================

        /// <summary>
        ///     Load data that is required by the game to ensure it is always
        ///     present at runtime. The data that is loaded may be replaced
        ///     with newer data loaded over the network after the user logs in.
        ///     <br /><br />
        ///
        ///     The necessary data currently includes:
        ///     <br /><br />
        ///
        ///     - The list manufacturers<br />
        ///     - The list products<br />
        /// </summary>
        public static void Init()
        {
            // Our initialization takes time (it sends web requests) so start
            // a coroutine to handle it
            Timing.RunCoroutine(CoroutineForInit());

            IEnumerator<float> CoroutineForInit()
            {
                isInitialized = false;

                // Reset all fields. This is only relevant in the Unity editor
                // where these may be storing values from the previous session.
                logInCoroutineState = LogInCoroutineState.NotStarted;
                isKioskModeEnabled = false;
                useWhiteLabelMode = false;
                user = default;
                userProfile = default;
                activeClientId = null;
                activeQuoteId = null;
                activeClientWantsToBeContacted = null;
                cabinetSectionCategories.Clear();
                manufacturers.Clear();
                productGroups.Clear();
                userClients.Clear();
                sourceProducts.Clear();
                ClearRuntimeProducts();
                baseProductIdToProductVariantIds.Clear();

                // Read the fallback manufacturer JSON. The manufacturer
                // list is necessary for parsing the product JSON.
                {
                    if (LoadFallbackWebResponse(ProFitWeb.FallbackManufacturersFileName) is JsonDict response)
                    {
                        Exception error =
                            ProFitWeb.TryParseManufacturers(
                                new MelcherJsonObjectParser(response),
                                out manufacturers
                            );

                        if (error != null)
                        {
                            MelcherLog.LogError("Error parsing fallback manufacturers JSON: " + error.Message);
                        }
                    }
                }

                // Read the fallback product groups JSON. The product groups
                // are necessary for parsing the product JSON.
                {
                    if (LoadFallbackWebResponse(ProFitWeb.FallbackProductGroupsFileName) is JsonDict response)
                    {
                        Exception error =
                            ProFitWeb.TryParseProductGroups(
                                new MelcherJsonObjectParser(response),
                                out productGroups
                            );

                        if (error != null)
                        {
                            MelcherLog.LogError("Error parsing fallback product groups JSON: " + error.Message);
                        }
                    }
                }

                // Read the fallback cabinet section categories JSON.
                {
                    if (LoadFallbackWebResponse(ProFitWeb.FallbackCabinetSectionCategoriesFileName) is JsonDict response)
                    {
                        Exception error =
                            ProFitWeb.TryParseCabinetSectionCategoryList(
                                new MelcherJsonObjectParser(response),
                                out cabinetSectionCategories
                            );

                        if (error != null)
                        {
                            MelcherLog.LogError("Error parsing fallback cabinet section categories JSON: " + error.Message);
                        }
                    }
                }


                // Read the fallback brand catalogs JSON
                if (appOptions.cabinetDataAvailability >= AppOptions.CabinetDataAvailability.ClientOnly)
                {
                    if (LoadFallbackWebResponse(ProFitWeb.FallbackBrandCatalogsFileName) is JsonDict response)
                    {
                        Exception error =
                            ProFitWeb.TryParseBrandCatalogs(
                                new MelcherJsonObjectParser(response),
                                out serverBrandCatalogs
                            );

                        if (error != null)
                        {
                            MelcherLog.LogError("Error parsing fallback brand catalogs JSON: " + error.Message);
                        }
                    }
                }

                // Read the fallback tags response.
                if (appOptions.cabinetDataAvailability >= AppOptions.CabinetDataAvailability.ClientOnly)
                {
                    if (LoadFallbackWebResponse(ProFitWeb.FallbackTagsFileName) is JsonDict response)
                    {
                        Exception error =
                            ProFitWeb.TryParseTags(
                                new MelcherJsonObjectParser(response),
                                out productTags
                            );

                        if (error != null)
                        {
                            MelcherLog.LogError("Error parsing fallback product tags JSON: " + error.Message);
                        }
                    }
                }

                // Read the fallback compatibilities response.
                {
                    if (LoadFallbackWebResponse(ProFitWeb.FallbackCompatibilitiesFileName) is JsonDict response)
                    {
                        Exception error =
                            ProFitWeb.TryParseCompatibilities(
                                new MelcherJsonObjectParser(response),
                                out cabinetCompatibilities
                            );

                        if (error != null)
                        {
                            MelcherLog.LogError("Error parsing fallback compatibilities JSON: " + error.Message);
                        }
                    }
                }

                // Read the fallback product JSON.
                {
                    if (LoadFallbackWebResponse(ProFitWeb.FallbackProductListFileName) is JsonDict response)
                    {
                        Exception error =
                            ProFitWeb.TryParseProductList(
                                new MelcherJsonObjectParser(response),
                                manufacturers,
                                out sourceProducts
                            );

                        if (error != null)
                        {
                            MelcherLog.LogError("Error parsing fallback product JSON: " + error.Message);
                        }
                    }

                }

                // Read the fallback cabinet data
                if (appOptions.cabinetDataAvailability >= AppOptions.CabinetDataAvailability.ClientOnly)
                {
                    if (LoadFallbackWebResponse(ProFitWeb.FallbackCabinetListFileName) is JsonDict response)
                    {
                        List<ProductInfo> cabinetList = null;

                        Exception error =
                            ProFitWeb.TryParseCabinetList(
                                new MelcherJsonObjectParser(response),
                                manufacturers,
                                cabinetSectionCategories,
                                out cabinetList
                            );

                        if (error != null)
                        {
                            MelcherLog.LogError("Error parsing fallback cabinet list JSON: " + error.Message);
                        }

                        if (cabinetList != null)
                        {
                            sourceProducts.AddRange(cabinetList);
                        }
                    }
                }

                LoadAndApplyProductOverrides();

                BuildProductVariantLists();

                // Try to connect with the Pro-Fit API server

                bool hostIsReachable = false;

                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    MelcherLog.LogWarning($"The internet is not reachable.");
                }
                else
                {
                    // Make a request to the server to see if our protocol is good
                    var firstRequest =
                        MelcherWebRequestBuilder.Get(ProFitWeb.Host)
                            .OnCompleted(
                                onCompleted: (_, error) =>
                                {
                                    hostIsReachable = error == null;
                                }
                            )
                            .Send();

                    yield return Timing.WaitUntilDone(firstRequest);

                    if (!hostIsReachable)
                    {
                        // Switch our protocol and try again
                        ProFitWeb.UseHttps = !ProFitWeb.UseHttps;
                        MelcherLog.LogWarning($"Changed {nameof(ProFitWeb.UseHttps)} to " + ProFitWeb.UseHttps);

                        // Confirm that switching HTTP/HTTPS solved it
                        var secondRequest =
                            MelcherWebRequestBuilder.Get(ProFitWeb.Host)
                                .OnCompleted(
                                    onCompleted: (_, error) =>
                                    {
                                        hostIsReachable = error == null;
                                    }
                                )
                                .Send();

                        yield return Timing.WaitUntilDone(secondRequest);
                    }

                    if (!hostIsReachable)
                    {
                        MelcherLog.LogError("The server is not reachable: " + ProFitWeb.domain);
                    }
                }

                if (hostIsReachable)
                {
                    MelcherLog.Log("Found server at: " + ProFitWeb.Host);

                    if (appOptions.cabinetDataAvailability >= AppOptions.CabinetDataAvailability.Server)
                    {
                        // Request a fresh copy of the brand catalogs
                        var request =
                            ProFitWeb.GetBrandCatalogs(
                                onSuccess: static newBrandCatalogs => serverBrandCatalogs = newBrandCatalogs,
                                onError: (builder, error, jsonValue) =>
                                {
                                    MelcherLog.LogError(error.Message);
                                }
                            );

                        yield return Timing.WaitUntilDone(request);

                        PopulateRuntimeBrandCatalogs();
                    }


                    {
                        // Request a fresh copy of the manufacturers
                        var request =
                            ProFitWeb.GetManufacturers(
                                onSuccess: newManufacturers => manufacturers = newManufacturers,
                                onError: (builder, error, jsonValue) =>
                                {
                                    MelcherLog.LogError(error.Message);
                                }
                            );

                        yield return Timing.WaitUntilDone(request);
                    }

                    {
                        // Request a copy of the product groups
                        var request =
                            ProFitWeb.GetProductGroups(
                                onSuccess: newProductGroupsList => productGroups = newProductGroupsList,
                                onError: (builder, error, jsonValue) =>
                                {
                                    MelcherLog.LogError(error.Message);
                                }
                            );

                        yield return Timing.WaitUntilDone(request);
                    }

                    if (appOptions.cabinetDataAvailability >= AppOptions.CabinetDataAvailability.Server)
                    {
                        // Request a copy of the product tags
                        var request =
                            ProFitWeb.GetTags(
                                onSuccess: newProductTagsList => productTags = newProductTagsList,
                                onError: (builder, error, jsonValue) =>
                                {
                                    MelcherLog.LogError(error.Message);
                                }
                            );

                        yield return Timing.WaitUntilDone(request);
                    }

                    if (appOptions.cabinetDataAvailability >= AppOptions.CabinetDataAvailability.Server)
                    {
                        // Request a copy of the cabinet compatibilities
                        var request =
                            ProFitWeb.GetCompatibilities(
                                onSuccess: newCabinetCompatibilities => cabinetCompatibilities = newCabinetCompatibilities,
                                onError: (builder, error, jsonValue) =>
                                {
                                    MelcherLog.LogError(error.Message);
                                }
                            );

                        yield return Timing.WaitUntilDone(request);
                    }
                }

#if UNITY_EDITOR
                int fakeProductNumber = 0;

                // To help ensure ALL products look good on the cabinet, i.e., even
                // those that haven't been added in the server back-end yet, create
                // fake ProductInfos for any unused product models.
                string[] productVisualDatabaseGuids =
                    UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ProductVisualDatabase)}");

                MelcherLog.Assert(productVisualDatabaseGuids.Length == 1,
                    "This bit of code assumes there is only one product visual database. Please update the code to select the correct catalog."
                );

                string productVisualDatabasePath =
                    UnityEditor.AssetDatabase.GUIDToAssetPath(productVisualDatabaseGuids[0]);

                ProductVisualDatabase productVisualDatabase =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<ProductVisualDatabase>(productVisualDatabasePath);

                // Find the default manufacturer to contain these fake products.
                int defaultManufacturerIndex =
                    manufacturers.FindIndex(
                        static x => x.id == appProfile.defaultManufacturerId
                    );

                if (defaultManufacturerIndex < 0)
                {
                    throw new Exception("Couldn't find default manufacturer index.");
                }

                int defaultManufacturerId = manufacturers[defaultManufacturerIndex].id;

                MelcherLog.LogVerbose($"Using manufacturer ID {defaultManufacturerId} as the default. It's name is: {manufacturers[defaultManufacturerIndex].name}");

                System.Text.StringBuilder stringBuilder = new ();

                stringBuilder.AppendLine("<Visual Name> :: <Visual ID> :: <Category>");

                foreach (ProductVisual visual in productVisualDatabase.runtimeEntries)
                {
                    ProductVisualContent visualContent =
                        visual.contentAddress.editorOnlyCachedAsset;

                    if (!visualContent || !(visualContent.productModelPrefab || visualContent.finishMaterial))
                    {
                        // Skip this visual because it's not a model and this
                        // feature is specifically about testing models.
                        // Materials are being included too now. Since adding
                        // addressables, it's much more work to go through the
                        // full process of adding products to the dashboard.
                        continue;
                    }

                    int indexOfFirstProductUsingVisual =
                        sourceProducts.FindIndex(
                            x => x.visualId == visual.info.id
                        );

                    if (indexOfFirstProductUsingVisual >= 0)
                    {
                        // Skip this visual because it is already used and will be
                        // viewable as part of the other model batches.
                        continue;
                    }

                    Vector3 modelCutOutSizeInMeters =
                        visualContent.modelCutOutExtentMax.localPosition - visualContent.modelCutOutExtentMin.localPosition;

                    // Deliberately make the fake ProductInfo's cut-out size
                    // different from the models so it's easier to spot cut-out
                    // scaling issues. We use a smaller value to help products
                    // fit on cabinets where they may not otherwise fit.
                    modelCutOutSizeInMeters *= 0.8f;

                    sourceProducts.Add(
                        new ProductInfo()
                        {
                            id = -1 - fakeProductNumber, // negative so the fake IDs don't stomp over real IDs
                            sku = "UnusedVisual_" + fakeProductNumber,
                            manufacturerId = defaultManufacturerId,
                            groupId = 0,    // no group
                            name = visualContent.name,
                            category = visual.info.runtimePrefabCategory.ToProductCategory(),
                            cutOutSizeInMeters = modelCutOutSizeInMeters,
                            visualId = visual.info.id,
                            doesExistInServerDashboard = false  // This is a "fake" product that will appear at the front of the batch list.
                        }
                    );

                    ++fakeProductNumber;

                    stringBuilder.AppendLine(
                        $"{visual.name} :: {visual.info.id} :: {sourceProducts[^1].category}"
                    );
                }

                string outPath =
                    ProFitUtility.Editor.OverwriteSpecificLogFile(
                        "issues-unused-product-visuals.txt",
                        stringBuilder.ToString()
                    );

                MelcherLog.LogErrorEditorOnly("Wrote unused product visuals to: " + outPath);

#endif  // if UNITY_EDITOR

                isInitialized = true;

                MelcherLog.LogEditorOnly("Executing queued actions...");
                s_QueuedActions?.Invoke();
                s_QueuedActions = null;
                MelcherLog.LogEditorOnly("Finished queued actions");
            }

        }

        /// <summary>
        ///     Nested function. Tries to load a resource from the fallback
        ///     resource folder and interpret it as JSON. This is intended to
        ///     help load the cached (aka fallback) web data in case the user
        ///     doesn't have an internet connection.
        /// </summary>
        public static JsonDict LoadFallbackWebResponse(string fileName)
        {
            string fallbackResourcePath =
                ProFitUtility.GetFallbackJsonPath(appProfile, fileName);

            // The fallback data is expected to be a cached, plaintext JSON
            // response. The file is created whenever the relevant endpoint
            // is called in the editor.
            TextAsset fallbackResource =
                Resources.Load<TextAsset>(fallbackResourcePath);

            using var unloadFallbackResource =
                MelcherDeferred.Defer(
                    toCapture: fallbackResource,
                    onDispose: static resource => Resources.UnloadAsset(resource)
                );

            if (fallbackResource == null)
            {
                MelcherLog.LogError($"Error loading fallback {fileName}");
                return null;
            }

            if (MiniJSON.Json.Deserialize(fallbackResource.text) is not JsonDict jsonDict)
            {
                MelcherLog.LogError($"Error deserializing fallback {fileName}");
                return null;
            }

            return jsonDict;
        }

        /// <summary>
        /// Execute a function as soon as the <see cref="Init"/> function is
        /// complete. If the function hasn't started or is underway, the
        /// action will be queued up to execute once the init finishes.
        /// </summary>
        public static void ExecuteWhenReady(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(paramName: nameof(action));
            }

            if (action != null)
            {
                if (isInitialized)
                {
                    // The class is ready so execute the action immediately.
                    action.Invoke();
                }
                else
                {
                    s_QueuedActions += action;
                }
            }
        }

        /// <summary>
        /// Given a client ID and an ID of one of their quotes, try to look up
        /// the index of each object in their respective lists.
        /// </summary>
        public static bool TryGetClientAndQuoteIndex(int clientId, int quoteId, out int clientIndex, out int quoteIndex, bool logErrorIfNotFound = true)
        {
            quoteIndex = -1;

            return TryGetClientIndex(clientId, out clientIndex, logErrorIfNotFound) &&
                TryGetClientQuoteIndex(clientIndex, quoteId, out quoteIndex, logErrorIfNotFound);
        }

        public static bool TryGetClientIndex(int clientId, out int clientIndex, bool logErrorIfNotFound = true)
        {
            clientIndex = userClients.FindIndex(
                x => x.id == clientId
            );

            if (clientIndex < 0 && logErrorIfNotFound)
            {
                MelcherLog.LogError($"Did not find client with ID {clientId}");
            }

            return clientIndex >= 0;
        }

        public static bool TryGetClientQuoteIndex(int clientIndex, int quoteId, out int quoteIndex, bool logErrorIfNotFound = true)
        {
            quoteIndex = userClients[clientIndex].quotes.FindIndex(
                x => x.id == quoteId
            );

            if (quoteIndex < 0 && logErrorIfNotFound)
            {
                MelcherLog.LogError($"Did not find quote with ID {quoteId} in client #{userClients[clientIndex].id}");
            }

            return quoteIndex >= 0;
        }

        public static string TryGetClientCombinedName()
        {
            string clientName = "Guest";
            if (activeClientId.HasValue &&
                TryGetClientIndex(activeClientId.Value, out int clientIndex))
            {
                UserClient client = userClients[clientIndex];

                // Combine the client's first and last name together with a
                // space. If either is blank, leave it off
                string[] names = new []{ client.firstName, client.lastName };

                clientName = string.Join(" ", names);
            }

            return clientName;
        }

        /// <summary>
        /// Clears the app's memory of any users that were logged in with
        /// "Remember Me" enabled. This means a user will need to sign in with
        /// their email and password the next time the app launches.
        /// </summary>
        public static void ForgetRememberMeData()
        {
            MelcherLog.LogEditorOnly("Removing session token from PlayerPrefs");
            PlayerPrefs.DeleteKey(Const.PlayerPrefs_Token);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Called when the user explicitly logs out on their own accord or
        /// when their trial/subscription has lapsed.
        /// </summary>
        public static void LogOut()
        {
            user = default;
            userProfile = default;
            userClients.Clear();
            sourceProducts.Clear();
            baseProductIdToProductVariantIds.Clear();

            if (logInCoroutineState == LogInCoroutineState.Finished)
            {
                logInCoroutineState = LogInCoroutineState.NotStarted;
            }

            ForgetRememberMeData();
            StopAutoKickCoroutine();

            EventBridge.Menu.hideTrialCountdownUi?.Invoke();
            EventBridge.Designer.hideTrialCountdownUi?.Invoke();
        }

        /// <summary>
        /// Start the global coroutine that watches the signed-in user's
        /// account status and kicks them when it no longer has a valid trial
        /// and subscription. This is intended to be called whenever a new
        /// user signs in, and to be followed up by a call to <see
        /// cref="StopAutoKickCoroutine"/> if the user logs out.
        /// </summary>
        public static void StartAutoKickCoroutine()
        {
            Debug.Assert(!s_AutoKickUserCoroutine.IsValid);

            s_AutoKickUserCoroutine = Timing.RunCoroutine(KickUserWhenPaymentExpires());

            /// <summary>
            /// This coroutine is started when an account logs in. It ensures the
            /// user account has a valid trial duration or has paid for a
            /// subscription. If both conditions are not met, the user is forced
            /// out of the app.
            /// </summary>
            IEnumerator<float> KickUserWhenPaymentExpires()
            {
                // Loop forever or until StopAutoKickCoroutine() is called
                while (ProFitGlobals.user.id != 0)
                {
                    bool wasTrialActive = userProfile.IsTrialActive();
                    yield return Timing.WaitForSeconds(20.0f);

                    // Check the server to see if our trial was extended or we
                    // became a fully subscribed user
                    yield return Timing.WaitUntilDone(
                        ProFitWeb.GetProfile(
                            user.apiToken,
                            user.userType == UserType.Offline,
                            onSuccess: (newUserProfile) =>
                            {
                                userProfile = newUserProfile;
                            },
                            onError: null
                        )
                    );

                    if (userProfile.IsSubscriptionActive())
                    {
                        // The user's account is still in good standing. Ensure
                        // that the trial counter is hid in case a trial user
                        // upgrades to a paid subscription
                        EventBridge.Menu.hideTrialCountdownUi?.Invoke();

                        continue;
                    }

                    if (!userProfile.IsTrialActive())
                    {
                        KickUser(
                            kickReason: wasTrialActive ? "Trial expired." : "Subscription expired."
                        );
                        yield break;
                    }
                }
            }

        }

        /// <summary>
        ///     Intended to be called after the main product list is parsed.
        /// </summary>
        public static Exception LoadAndApplyProductOverrides()
        {
            // Read the offline product list. These are products that
            // do not exist in the dashboard (most commonly because the
            // server does not have the required features yet).
            string productOverridesListPath =
                ProFitUtility.GetFallbackJsonPath(
                    appProfile,
                    ProFitWeb.ProductOverridesListFileName
                );

            TextAsset productOverridesListJson =
                Resources.Load<TextAsset>(
                    productOverridesListPath
                );

            if (!productOverridesListJson)
            {
                return new Exception("Failed to load product overrides list.");
            }

            JsonDict response = null;

            List<ProductInfo> overrideProductList = null;

            response =
                (JsonDict)MiniJSON.Json.Deserialize(
                    productOverridesListJson.text
                );

            if (response == null)
            {
                return new Exception("Failed to deserialize product override JSON.");
            }

            Exception error =
                ProFitWeb.TryParseProductList(
                    new MelcherJsonObjectParser(response),
                    manufacturers,
                    out overrideProductList
                );

            if (error != null)
            {
                // This is not a fatal error and flow will still continue
                // into the `onSuccess` callback. Product overrides are allowed
                // to fail to load because they are only intended for
                // development purposes.

                MelcherLog.LogError(error.Message);
                return error;
            }

            foreach (ProductInfo overrideProductInfo in overrideProductList)
            {
                int existingProductIndex =
                    sourceProducts.FindIndex(
                        x => x.sku == overrideProductInfo.sku
                    );

                if (existingProductIndex < 0)
                {
                    MelcherLog.Log(
                        $"Adding a new product info for {MelcherFormat.ConsoleString(overrideProductInfo.name)} ({overrideProductInfo.manufacturerId}).",
                        severity: MelcherLogSeverity.Verbose
                    );

                    sourceProducts.Add(overrideProductInfo);

                    continue;
                }

                MelcherLog.Log(
                    $"Overriding product info for {MelcherFormat.ConsoleString(sourceProducts[existingProductIndex].name)}.",
                    severity: MelcherLogSeverity.Verbose
                );

                ProductInfo originalProduct = sourceProducts[existingProductIndex];

                if (!string.IsNullOrEmpty(overrideProductInfo.name))
                {
                    originalProduct.name = overrideProductInfo.name;
                }

                if (!string.IsNullOrEmpty(overrideProductInfo.description))
                {
                    originalProduct.description = overrideProductInfo.description;
                }

                if (!string.IsNullOrEmpty(overrideProductInfo.visualId))
                {
                    originalProduct.visualId = overrideProductInfo.visualId;
                }

                if (!MelcherCommon.IsAlmostEqual(overrideProductInfo.productSizeInMeters, Vector3.zero, tolerance: 0.001f))
                {
                    originalProduct.productSizeInMeters = overrideProductInfo.productSizeInMeters;
                }

                if (overrideProductInfo.minWidthInMeters.HasValue)
                {
                    originalProduct.minWidthInMeters = overrideProductInfo.minWidthInMeters.Value;
                }

                if (overrideProductInfo.maxWidthInMeters.HasValue)
                {
                    originalProduct.maxWidthInMeters = overrideProductInfo.maxWidthInMeters.Value;
                }

                if (overrideProductInfo.productTagIds.Count > 0)
                {
                    originalProduct.productTagIds = overrideProductInfo.productTagIds;
                }

                if (overrideProductInfo.compatibilityId != 0)
                {
                    originalProduct.compatibilityId = overrideProductInfo.compatibilityId;
                }

            }

            return null;
        }

        /// <summary>
        ///     Build the association between base products and all the products
        ///     in their variant chain. This is intended to be called after all
        ///     products are known to the user. This should be called for both
        ///     online and offline users.
        /// </summary>
        public static void BuildProductVariantLists()
        {
            baseProductIdToProductVariantIds.Clear();

            // Create the map between base product IDs and their
            // product variants.
            foreach (ProductInfo productInfo in sourceProducts)
            {
                int baseProductId = productInfo.baseProductId;

                if (baseProductId != 0)
                {
                    List<int> productVariantIds =
                        baseProductIdToProductVariantIds.GetValueOrDefault(baseProductId);

                    if (productVariantIds == null)
                    {
                        productVariantIds = new ();

                        // The variants list includes the base
                        // product ID as the first item. This makes
                        // working with the whole collection simpler
                        // elsewhere (e.g., in the product toolbar.)
                        productVariantIds.Add(baseProductId);

                        baseProductIdToProductVariantIds[baseProductId] = productVariantIds;
                    }

                    productVariantIds.Add(productInfo.id);
                }
            }
        }


        /// <summary>
        /// Stop the coroutine that is watching the signed-in user's account
        /// status. This is intended to be called whenever the user logs out.
        /// </summary>
        private static void StopAutoKickCoroutine()
        {
            Timing.KillCoroutines(s_AutoKickUserCoroutine);
            s_AutoKickUserCoroutine = default;
        }

        /// <summary>
        /// This is called when a user's account expires. E.g., the trial
        /// period ends or the user ends their subscription (this later case
        /// isn't handled yet).
        /// </summary>
        public static void KickUser(string kickReason)
        {
            LogOut();

            MelcherSceneRequest menuSceneRequest =
                MelcherSceneRequest.FromName("MenuScene");

            if (!MelcherSceneManager.TryGetScene(menuSceneRequest, out var _))
            {
                MelcherLog.Log("Menu isn't loaded");

                EventBridge.Designer.loadMenuScene.Invoke(
                    onLoaded: (_, _) =>
                    {
                        ShowKickPrompt();
                    }
                );
            }
            else
            {
                // Return to welcome screen
                EventBridge.Menu.showWelcomeUi.Invoke(MelcherUITransitionStyle.Regress);

                ShowKickPrompt();
            }


            void ShowKickPrompt()
            {
                EventBridge.Menu.showUserKickedPrompt?.Invoke(kickReason);
            }
        }

        /// <summary>
        ///     Populate the runtime brand catalogs. These are the preset
        ///     catalogs plus clones of the presets for the server-defined
        ///     catalogs.
        ///     <br /><br />
        ///
        ///     This is intended to be called anytime <see
        ///     cref="serverBrandCatalogs"/> is updated (but not while the
        ///     player is designing a kitchen, as it might cause a dangling
        ///     brand catalog reference). This function populates
        ///     <see cref="runtimeBrandCatalogs"/> so there is an entry for
        ///     every valid item in <see cref="serverBrandCatalogs"/>.
        /// </summary>
        public static void PopulateRuntimeBrandCatalogs()
        {
            runtimeBrandCatalogs.Clear();

            foreach (BrandCatalogInfo serverBrandCatalog in serverBrandCatalogs)
            {
                foreach (BrandCatalog presetBrandCatalog in appProfile.presetBrandCatalogs)
                {
                    if (serverBrandCatalog.id == presetBrandCatalog.catalogId)
                    {
                        // This server ID matches a preset exactly. Just use
                        // the preset.

                        runtimeBrandCatalogs.Add(presetBrandCatalog);
                        break;
                    }

                    if (serverBrandCatalog.cabinetManufacturerId == presetBrandCatalog.cabinetManufacturerId)
                    {
                        // This cabinet manufacturer ID matches the cabinet
                        // manufacturer ID of a preset. This must be the
                        // catalog its based on. Clone it and add it to the
                        // list.

                        BrandCatalog serverDefinedCatalog =
                            UnityEngine.Object.Instantiate(presetBrandCatalog);

                        serverDefinedCatalog.catalogName = serverBrandCatalog.name;
                        serverDefinedCatalog.catalogId = serverBrandCatalog.id;

                        runtimeBrandCatalogs.Add(serverDefinedCatalog);

                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Return a key for PlayerPrefs that stores the current user's
        ///     kiosk-mode preference.
        ///     <br /><br />
        ///
        ///     The kiosk-mode preference is saved locally on the device. This
        ///     enables the same account to be reused in many places with
        ///     whatever security is appropriate for the installation.
        /// </summary>
        private static string GetKioskModePlayerPrefKey(User user)
        {
            return $"User.{user.id}.isKioskModeEnabled";
        }


        // =====================================================================
        // Static fields
        // =====================================================================

        /// <summary>
        /// A list of actions that have been requested to execute as soon as
        /// <see cref="Init"/>'s spawned coroutine finishes. Actions are
        /// appended in <see cref="ExecuteWhenReady(Action)"/>.
        /// </summary>
        private static Action s_QueuedActions;

        /// <summary>
        /// The coroutine watching the logged in user's account status. It will
        /// kick the user back to the main menu if their subscription or trial
        /// expires.
        /// </summary>
        private static CoroutineHandle s_AutoKickUserCoroutine;

        private static bool s_IsKioskModeEnabled;

        // ====================================================================
        // Types
        // ====================================================================

        public enum LogInCoroutineState
        {
            NotStarted,
            Running,
            Finished,
        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("Tools/Melcher Studios/Pro-Fit/Dump sinks with zero depth")]
        private static void DumpSinksWithZeroDepth()
        {
            foreach (ProductInfo productInfo in sourceProducts)
            {
                if (productInfo.category != ProductCategory.Sink)
                {
                    continue;
                }

                if (productInfo.cutOutSizeInMeters.z < 0.01f)
                {
                    MelcherLog.Log(
                        $"Fridge {MelcherFormat.ConsoleString(productInfo.name)} ({MelcherFormat.ConsoleString(productInfo.sku)}) has a depth of {productInfo.cutOutSizeInMeters.z}m"
                    );
                }
            }
        }

#endif  // if UNITY_EDITOR

    }   // class ProFitGlobals

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-02
/// ===========================================================================
