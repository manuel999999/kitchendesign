namespace KitchenDesign
{
    using KitchenDesign.UI.DesignerScreens;
    using KitchenDesign.UI.Views;
    using KitchenDesign.Session;
    using KitchenDesign.CatalogData;
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Interfaces;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Serializables;
    using KitchenDesign.Products;
    using Cysharp.Threading.Tasks;
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.SceneManagement;
    using KitchenDesign.Cabinets;
    using KitchenDesign.KitchenModel;
    using KitchenDesign.Occupancy;
    using KitchenDesign.Events;

    /// <summary>
    ///     Contains the "root" logic of the DesignerScene. It knows about the
    ///     kitchen and other components, and works to pass events between
    ///     them.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class DesignerScreensControl : MelcherMonoBehaviour
    {
        private KitchenEvents _events;

        public void ListenTo(KitchenEvents events)
        {
            if (this._events != null && this.isActiveAndEnabled)
            {
                this.RemoveKitchenListeners();
            }

            this._events = events;

            if (this._events != null && this.isActiveAndEnabled)
            {
                this.AddKitchenListeners();
            }
        }

        private void AddKitchenListeners()
        {
            this._events.restart += ForgetCabinetSettings;
            this._events.onBuildingMethodChanged += OnBuildingMethodChanged;
            this._events.onBrandCatalogChanged.OnInvoke += OnBrandCatalogChanged;
            this._events.onCabinetSwapStart += OnCabinetSwapStart;
            this._events.onHighlightChanged.OnInvoke += OnHighlightChanged;
            this._events.onCabinetAddedByUser.OnInvoke += OnCabinetAddedByUser;
        }

        private void RemoveKitchenListeners()
        {
            this._events.restart -= ForgetCabinetSettings;
            this._events.onBuildingMethodChanged -= OnBuildingMethodChanged;
            this._events.onBrandCatalogChanged.OnInvoke -= OnBrandCatalogChanged;
            this._events.onCabinetSwapStart -= OnCabinetSwapStart;
            this._events.onHighlightChanged.OnInvoke -= OnHighlightChanged;
            this._events.onCabinetAddedByUser.OnInvoke -= OnCabinetAddedByUser;
        }

        private AppOptionsSO _options;
        private IAppRules _rules;
        private CatalogDataStatus _catalogData;
        private SessionStatus _session;
        private ProductVisualContentStatus _productContent;

        [SerializeField] private ProductSearchUI productSearch;
        [SerializeField] private DesignerAboutProductUI aboutProductPrompt;
        [SerializeField] private SetVolumeSizeUI setVolumeSizePrompt;
        [SerializeField] private DesignerConfirmUI confirmPrompt;
        [SerializeField] private DesignerSaveUI savePrompt;
        [SerializeField] private DesignerDevUI devUi;

        // ====================================================================
        // Methods
        // ====================================================================

        public void Initialize(AppSystems systems)
        {
            this._options = systems.Options;
            this._rules = systems.Rules;
            this._catalogData = systems.CatalogData;
            this._session = systems.Session;
            this._productContent = systems.ProductContent;
            this._productContent.ContentLoaded += OnProductVisualContentLoaded;

            m_ToolbarUi.Initialize(m_Kitchen, systems.Profile, systems.Rules, systems.LaunchOptions, systems.CatalogData, systems.Session, systems.ProductContent, this.confirmPrompt, this.aboutProductPrompt);
            m_DesignerNavigationUi.Initialize(m_Kitchen, systems.Options, systems.Server, systems.CatalogData, systems.ClientList, systems.Session, this.confirmPrompt, this.savePrompt);
            m_BuildingMethodsUi.Initialize(m_Kitchen, systems.Session);
            m_BrandCatalogUi.Initialize(m_Kitchen, systems.Profile, systems.Options, systems.CatalogData, systems.Session);
            m_CabinetShapeUi.Initialize(m_Kitchen, systems.Options, systems.Session);
            m_KitchenTemplateUi.Initialize(m_Kitchen);
            m_CabinetOtherSizesUi.Initialize(m_Kitchen, systems.Session);
            m_WhiteLabelClientInfoUi.Initialize(m_Kitchen, systems.Options, systems.Server, systems.ClientList, systems.Session);
            this.productSearch.Initialize(m_Kitchen, systems.Rules, systems.CatalogData);
            this.aboutProductPrompt.Initialize(m_Kitchen, systems.CatalogData);
            this.setVolumeSizePrompt.Initialize(systems.Session);
            this.devUi.Initialize(systems.Options, systems.CatalogData, systems.ProductContent);

            // Hide the debug cuboid visualizations by default
            OccupantOccupiedCuboidVisualization.isVisible = false;

            if (this._session.UserProfile.IsTrialActive())
            {
                // Show trial users a countdown in the designer scene
                this.ShowTrialCountdownUI();
            }

            this._catalogData.ExecuteWhenLoaded(OnGameDataReady);
        }

        private void Update()
        {
            {
                // if (Input.GetKey(KeyCode.T))
                // {
                //     string tempSavePath = System.IO.Path.GetFullPath(
                //         System.IO.Path.Combine(Application.dataPath, "..", "tempsave.json")
                //     );
                //     string jsonText = System.IO.File.ReadAllText(tempSavePath);

                //     JsonDict json = (JsonDict)MiniJSON.Json.Deserialize(jsonText);

                //     m_Kitchen.LoadFromJson(json);
                // }


                // Debug keys to toggle visibility of cuboids
                const KeyCode DebugKeyModifier = KeyCode.Backslash;
                const KeyCode DebugKey1 = KeyCode.Alpha1;
                const KeyCode DebugKey2 = KeyCode.Alpha2;

                if (CheckKeyCombo(DebugKeyModifier, DebugKey1))
                {
                    OccupantOccupiedCuboidVisualization.isVisible = !OccupantOccupiedCuboidVisualization.isVisible;
                    MelcherLog.LogEditorOnly($"Set {nameof(OccupantOccupiedCuboidVisualization)}.isVisible to {OccupantOccupiedCuboidVisualization.isVisible}");
                }
                else if (CheckKeyCombo(DebugKeyModifier, DebugKey2))
                {
                    OccupiableVolumeCuboidVisualization.isVisible = !OccupiableVolumeCuboidVisualization.isVisible;
                    MelcherLog.LogEditorOnly($"Set {nameof(OccupiableVolumeCuboidVisualization)}.isVisible to {OccupiableVolumeCuboidVisualization.isVisible}");
                }

#if UNITY_EDITOR
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    m_Kitchen.Debug_ForgetSelection();
                }
                else if (Input.GetKeyDown(KeyCode.F6))
                {
                    KitchenScreenshotManager screenshotManager = FindObjectOfType<KitchenScreenshotManager>();
                    screenshotManager.TakeScreenshots(m_Kitchen).Forget();
                }
#endif  // if UNITY_EDITOR
            }

            static bool CheckKeyCombo(KeyCode key1, KeyCode key2)
            {
                return
                    (Input.GetKeyDown(key1) && Input.GetKey(key2)) ||
                    (Input.GetKeyDown(key2) && Input.GetKey(key1));
            }
        }

        private void LateUpdate()
        {
            m_Kitchen.LateTick();
        }

        private void Awake()
        {
            RenderSettings.skybox =
                ProFitGlobals.appOptions.appSkin.designerSceneSkyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }

        private void Start()
        {
            // Only allow landscape orientations
            Screen.autorotateToPortrait             = false;
            Screen.autorotateToPortraitUpsideDown   = false;
            Screen.autorotateToLandscapeRight       = true;
            Screen.autorotateToLandscapeLeft        = true;

            // Switch the screen to landscape
            if (Screen.orientation != ScreenOrientation.LandscapeLeft &&
                Screen.orientation != ScreenOrientation.LandscapeRight)
            {
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            }
        }

        private void OnDestroy()
        {
            if (this._productContent == null)
            {
                return;
            }

            this._productContent.ContentLoaded -= OnProductVisualContentLoaded;
        }

        protected void OnGameDataReady()
        {
#if UNITY_EDITOR
            if (this._session.User.userType == UserType.None)
            {
                MelcherLog.LogEditorOnly("Forcing user into offline mode because the design scene was started directly.");

                User user = this._session.User;
                user.userType = UserType.Offline;
                this._session.User = user;

                // Test preferred units without needing to sign in.
                UserProfile profile = this._session.UserProfile;
                profile.preferredUnits =
                    this._options.simulatedPreferredUnits;
                if (profile.preferredUnits == MeasurementUnit.None)
                {
                    profile.preferredUnits = MeasurementUnit.FeetAndInches;
                }
                this._session.UserProfile = profile;
            }
#endif  // if UNITY_EDITOR

            // Begin the "setup kitchen" UI process, but only after all the
            // global data (like product infos) has been loaded.
            m_Kitchen.events.restart.Invoke();
        }

        protected void OnEnable()
        {
            if (this._events != null)
            {
                this.AddKitchenListeners();
            }
            EventDesigner.OnWhiteLabelClientInfoRequested.AddListener(this.ShowWhiteLabelClientInfoUI);
            EventDesigner.OnBuildingMethodsRequested.AddListener(this.ShowBuildingMethodsUI);
            EventDesigner.OnBrandCatalogsRequested.AddListener(this.ShowBrandCatalogUI);
            EventDesigner.OnCabinetShapesRequested.AddListener(this.ShowCabinetShapeUI);
            EventDesigner.OnKitchenTemplatesRequested.AddListener(this.ShowKitchenTemplateUI);
            EventDesigner.OnCabinetSizesRequested.AddListener(this.ShowCabinetOtherSizesUI);
            EventDesigner.OnLoadingScreenRequested.AddListener(this.ShowLoadingUI);
            EventDesigner.OnLoadingImageRefreshRequested.AddListener(this.RefreshCabinetLoadingUiImage);
            EventDesigner.OnLoadingMessageRequested.AddListener(this.OnLoadingMessageRequested);

            EventSession.OnMenuSceneRequested.AddListener(this.OnMenuSceneRequested);
            EventSession.OnLoggedOut.AddListener(this.HideTrialCountdownUI);
        }

        protected void OnDisable()
        {
            if (this._events != null)
            {
                this.RemoveKitchenListeners();
            }
            EventDesigner.OnWhiteLabelClientInfoRequested.RemoveListener(this.ShowWhiteLabelClientInfoUI);
            EventDesigner.OnBuildingMethodsRequested.RemoveListener(this.ShowBuildingMethodsUI);
            EventDesigner.OnBrandCatalogsRequested.RemoveListener(this.ShowBrandCatalogUI);
            EventDesigner.OnCabinetShapesRequested.RemoveListener(this.ShowCabinetShapeUI);
            EventDesigner.OnKitchenTemplatesRequested.RemoveListener(this.ShowKitchenTemplateUI);
            EventDesigner.OnCabinetSizesRequested.RemoveListener(this.ShowCabinetOtherSizesUI);
            EventDesigner.OnLoadingScreenRequested.RemoveListener(this.ShowLoadingUI);
            EventDesigner.OnLoadingImageRefreshRequested.RemoveListener(this.RefreshCabinetLoadingUiImage);
            EventDesigner.OnLoadingMessageRequested.RemoveListener(this.OnLoadingMessageRequested);

            EventSession.OnMenuSceneRequested.RemoveListener(this.OnMenuSceneRequested);
            EventSession.OnLoggedOut.RemoveListener(this.HideTrialCountdownUI);
        }

        private void OnMenuSceneRequested(MenuSceneRequestInfo info)
        {
            if (info.OnLoaded == null)
            {
                this.LoadMenuScene(null);
                return;
            }

            System.Action onLoaded = info.OnLoaded;
            this.LoadMenuScene((_, _) => onLoaded());
        }

        protected void LoadMenuScene(UnityAction<Scene, LoadSceneMode> onLoaded)
        {
            // We're very memory constrained on iOS, to the point that we were
            // unable to change to a blank scene due to the memory overhead of
            // loading a new scene. To claw back some memory, we're trying to
            // to flush any unneeded data before the transition. This same
            // memory flushing will repeat in the intermediate scene too.

            Resources.UnloadUnusedAssets();

            GC.Collect();

            GC.WaitForPendingFinalizers();

            MelcherSceneManager.LoadSceneAsync(
                MelcherSceneRequest.FromName("IntermediateDesignerToMenuScene"),
                MelcherSceneLoadMode.Single,
                onLoaded: onLoaded
            );
        }

        protected void ForgetCabinetSettings()
        {
            m_BuildingMethodsUi.ClearForm();
            m_BrandCatalogUi.ClearForm();
            m_CabinetShapeUi.ClearForm();
            m_KitchenTemplateUi.ClearForm();
            m_CabinetOtherSizesUi.ClearForm();

            SetCabinetBeingSwapped(null);
        }

        protected void ShowLoadingUI(DesignerLoadingMode mode)
        {
            if (mode == DesignerLoadingMode.Hide)
            {
                m_LoadingUi.Hide();
            }
            else
            {
                // When loading, we display a static image. When saving, we
                // show a spinner.
                m_LoadingUi.loadingSprite =
                    mode == DesignerLoadingMode.Load ?
                        GetLoadingImage(m_Kitchen.selectedBrandCatalog) :
                        null;

                m_LoadingUi.Show();
            }
        }

        protected void RefreshCabinetLoadingUiImage()
        {
            m_LoadingUi.loadingSprite = GetLoadingImage(m_Kitchen.selectedBrandCatalog);
            m_LoadingUi.UpdateLoadingImage();
        }

        protected Sprite GetLoadingImage(BrandCatalogSO catalog)
        {
            if(this._rules.RequiresBrandCatalog)
            {
                if(catalog != null && catalog.BrandCatalogLoadingImage != null)
                {
                    return catalog.BrandCatalogLoadingImage;
                }
                else
                {
                    return this._options.designerSceneLoadingImage;
                }
            }
            else
            {
                return this._options.designerSceneLoadingImage;
            }
        }

        private void OnLoadingMessageRequested(LoadingMessageInfo info)
        {
            m_LoadingUi.SetStatusMessage(info.Message);
        }

        protected void ShowWhiteLabelClientInfoUI()
        {
            m_WhiteLabelClientInfoUi.Show();
        }

        protected void ShowBuildingMethodsUI()
        {
            m_BuildingMethodsUi.Show();
        }

        protected void ShowBrandCatalogUI()
        {
            m_BrandCatalogUi.Show();
        }

        protected void ShowCabinetShapeUI()
        {
            m_CabinetShapeUi.Show();
        }

        protected void ShowKitchenTemplateUI()
        {
            m_KitchenTemplateUi.Show();
        }

        protected void ShowCabinetOtherSizesUI(CabinetSizesMode mode)
        {
            CabinetLayout layout = m_Kitchen.selectedCabinetLayout;

            if (mode != CabinetSizesMode.ModifyExistingCabinet)
            {
                layout = m_Kitchen.selectedCabinetLayout;
            }

            m_CabinetOtherSizesUi.SetMode(mode, layout);
            m_CabinetOtherSizesUi.Show();
        }

        /// <summary>
        /// Show a timer to indicate when the user's trial expires.
        /// </summary>
        protected void ShowTrialCountdownUI()
        {
            m_TrialCountdownUi.Show();
        }

        /// <summary>
        /// Hide the timer timer. This is called if the user upgrades their
        /// trial to a subscription and they were previously using a trial.
        /// </summary>
        protected void HideTrialCountdownUI()
        {
            m_TrialCountdownUi.Hide();
        }

        protected void OnProductVisualContentLoaded(AssetAddress<ProductVisualContent> address, LoadedProductContent newlyLoadedProductVisualContent)
        {
            RuntimeProductOverrides.ApplySmokerCutOutHeight(
                this._catalogData,
                m_ProductVisualDatabase,
                this._rules,
                m_Kitchen.selectedBuildingMethod,
                address,
                newlyLoadedProductVisualContent
            );
        }

        protected void OnBuildingMethodChanged(KitchenBuildingMethod newBuildingMethod)
        {
            RuntimeProductOverrides.RebuildRuntimeProducts(
                this._catalogData,
                m_ProductVisualDatabase,
                this._rules,
                newBuildingMethod
            );
        }

        protected async UniTask OnBrandCatalogChanged(BrandCatalogSO selectedBrandCatalog)
        {
            RuntimeProductOverrides.ApplyBrandCatalogPadding(
                this._catalogData,
                m_ProductVisualDatabase,
                selectedBrandCatalog
            );

            await UniTask.CompletedTask;
        }

        /// <summary>
        ///     Called when the player selects a cabinet and then clicks the
        ///     swap cabinet button on the toolbar. This feature is only
        ///     available in the Catalog building method and is intended to
        ///     allow cabinets to be swapped out with similar-sized cabinets.
        /// </summary>
        /// <param name="cabinetBeingSwapped"></param>
        protected void OnCabinetSwapStart(Cabinet cabinetBeingSwapped)
        {
            // Mark this cabinet as the target being swapped.

            SetCabinetBeingSwapped(cabinetBeingSwapped);

            // Show the cabinet layout UI, but limited to cabs that are
            // compatible in this location.

            m_Kitchen.layout.GetCabinetNeighbours(
                cabinetBeingSwapped,
                out Cabinet leftCabinet,
                out Cabinet rightCabinet
            );

            Func<ProductInfo, bool> filterCabinets =
                potentialProductInfo =>
                    {
                        if (cabinetBeingSwapped.cabinetSectionProductInfo.sku == potentialProductInfo.sku)
                        {
                            // Don't allow swapping to the same product.
                            return false;
                        }

                        ProductVisualSO potentialProductVisual =
                            m_Kitchen.productVisualDatabase.FindRuntimeVisualByID(
                                potentialProductInfo.visualId
                            );

                        if (!potentialProductVisual)
                        {
                            return false;
                        }

                        bool potentialIsCorner =
                            potentialProductVisual.info.sectionCabinetType == CabinetType.Corner;

                        bool currentIsCorner =
                            cabinetBeingSwapped.cabinetType == CabinetType.Corner;

                        if (potentialIsCorner != currentIsCorner)
                        {
                            // Disallow mixing corners with non-corners.
                            return false;
                        }

                        // Disallow corner cabinets where there is not a compatible corner.
                        CabinetType potentialCabinetType =
                            potentialProductVisual.info.sectionCabinetType;

                        if (potentialCabinetType == CabinetType.StraightOnLeftOfCorner)
                        {
                            if (!rightCabinet || rightCabinet.cabinetType != CabinetType.Corner)
                            {
                                // No corner here for this potential cabinet.
                                return false;
                            }

                            Cabinet cabinetOnOtherSideOfCorner =
                                cabinetBeingSwapped.kitchen.layout.GetCabinetNeighbour(
                                    rightCabinet,
                                    Extent.Max
                                );

                            if (cabinetOnOtherSideOfCorner && cabinetOnOtherSideOfCorner.cabinetType == CabinetType.StraightOnRightOfCorner)
                            {
                                // Corner already occupied by blind cabinet.
                                return false;
                            }
                        }
                        else if (potentialCabinetType == CabinetType.StraightOnRightOfCorner)
                        {
                            if (!leftCabinet || leftCabinet.cabinetType != CabinetType.Corner)
                            {
                                // No corner here for this potential cabinet.
                                return false;
                            }

                            Cabinet cabinetOnOtherSideOfCorner =
                                cabinetBeingSwapped.kitchen.layout.GetCabinetNeighbour(
                                    leftCabinet,
                                    Extent.Min
                                );

                            if (cabinetOnOtherSideOfCorner && cabinetOnOtherSideOfCorner.cabinetType == CabinetType.StraightOnLeftOfCorner)
                            {
                                // Corner already occupied by blind cabinet.
                                return false;
                            }
                        }

                        return true;
                    };

            m_ToolbarUi.ShowCabinetLayoutToolbar(filterCabinets);
        }

        protected async UniTask OnHighlightChanged(GameObject highlightedObject)
        {
            SetCabinetBeingSwapped(null);

            await UniTask.CompletedTask;
        }

        protected void SetCabinetBeingSwapped(Cabinet cabinet)
        {
            bool wasCanceled = m_CabinetBeingSwapped && !cabinet;

            m_CabinetBeingSwapped = cabinet;

            if (wasCanceled)
            {
                m_Kitchen.events.onCabinetSwapCanceled?.Invoke();
            }
        }

        /// <summary>
        ///     When the user adds a new cabinet after starting a cabinet swap,
        ///     the to-be-swapped cabinet is removed and the new cabinet is
        ///     moved into the other's place.
        /// </summary>
        /// <param name="newCabinet"></param>
        protected async UniTask OnCabinetAddedByUser(Cabinet newCabinet)
        {
            if (!m_CabinetBeingSwapped)
            {
                return;
            }

            SetCabinetBeingSwapped(null);

            await m_Kitchen.TryHighlightObject(null);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [UnityEngine.Serialization.FormerlySerializedAs("m_ProductVisualCatalog")]
        [SerializeField] protected ProductVisualDatabaseSO m_ProductVisualDatabase = default;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private Kitchen m_Kitchen = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private WhiteLabelClientInfoUI m_WhiteLabelClientInfoUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private BuildingMethodUI m_BuildingMethodsUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private BrandCatalogUI m_BrandCatalogUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private CabinetShapeUI m_CabinetShapeUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private KitchenTemplateUI m_KitchenTemplateUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private CabinetOtherSizesUI m_CabinetOtherSizesUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private DesignerNavigationUI m_DesignerNavigationUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private DesignerToolbarUI m_ToolbarUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private TrialCountdownUI m_TrialCountdownUi = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected LoadingUI m_LoadingUi = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected Cabinet m_CabinetBeingSwapped;

    }   // class DesignerScreensControl

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-01
/// ===========================================================================
