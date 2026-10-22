namespace KitchenDesign
{
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

    /// <summary>
    ///     Contains the "root" logic of the DesignerScene. It knows about the
    ///     kitchen and other components, and works to pass events between
    ///     them.
    /// </summary>
    public class DesignerScene : MelcherStateMachineState
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public override void OnStateAdded(IMelcherStateMachineState fromState)
        {
            // Hide the debug cuboid visualizations by default
            OccupantOccupiedCuboidVisualization.isVisible = false;

            if (ProFitGlobals.userProfile.IsTrialActive())
            {
                // Show trial users a countdown in the designer scene
                EventBridge.Designer.showTrialCountdownUi.Invoke();
            }

            ProFitGlobals.ExecuteWhenReady(OnGameDataReady);
        }

        public override MelcherEventChainResult OnStateEvent(object eventData)
        {
            if (eventData == MelcherStateMachineBuiltInEvent.UnityUpdate)
            {
                // Ensure there is only one async instance of this function at
                // a time because it was originally synchronous and untested
                // if more than one is active at a time.
                if (!m_HandleOtherKitchenSelectionsTask.HasValue || m_HandleOtherKitchenSelectionsTask.Value.Status.IsCompleted())
                {
                    m_HandleOtherKitchenSelectionsTask = HandleOtherKitchenSelections();
                }

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
            else if (eventData == MelcherStateMachineBuiltInEvent.UnityLateUpdate)
            {
                m_Kitchen.LateTick();
            }

            return MelcherEventChainResult.Continue;

            static bool CheckKeyCombo(KeyCode key1, KeyCode key2)
            {
                return
                    (Input.GetKeyDown(key1) && Input.GetKey(key2)) ||
                    (Input.GetKeyDown(key2) && Input.GetKey(key1));
            }
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

        protected void OnGameDataReady()
        {
#if UNITY_EDITOR
            if (ProFitGlobals.user.userType == UserType.None)
            {
                MelcherLog.LogEditorOnly("Forcing user into offline mode because the design scene was started directly.");

                User user = ProFitGlobals.user;
                user.userType = UserType.Offline;
                ProFitGlobals.user = user;

                // Test preferred units without needing to sign in.
                UserProfile profile = ProFitGlobals.userProfile;
                profile.preferredUnits =
                    ProFitGlobals.appOptions.simulatedPreferredUnits;
                if (profile.preferredUnits == MeasurementUnit.None)
                {
                    profile.preferredUnits = MeasurementUnit.FeetAndInches;
                }
                ProFitGlobals.userProfile = profile;
            }
#endif  // if UNITY_EDITOR

            // Begin the "setup kitchen" UI process, but only after all the
            // global data (like product infos) has been loaded.
            EventBridge.Designer.restart.Invoke();
        }

        protected void OnEnable()
        {
            ProFitGlobals.productContent.ContentLoaded += OnProductVisualContentLoaded;

            EventBridge.Designer.loadMenuScene              += LoadMenuScene;
            EventBridge.Designer.restart                    += ForgetCabinetSettings;
            EventBridge.Designer.showWhiteLabelClientInfoUi += ShowWhiteLabelClientInfoUI;
            EventBridge.Designer.showBuildingMethodsUi      += ShowBuildingMethodsUI;
            EventBridge.Designer.showBrandCatalogUi         += ShowBrandCatalogUI;
            EventBridge.Designer.showCabinetShapeUi         += ShowCabinetShapeUI;
            EventBridge.Designer.showKitchenTemplateUi      += ShowKitchenTemplateUI;
            EventBridge.Designer.showCabinetDetailsUi       += ShowCabinetOtherSizesUI;
            EventBridge.Designer.showTrialCountdownUi       += ShowTrialCountdownUI;
            EventBridge.Designer.hideTrialCountdownUi       += HideTrialCountdownUI;
            EventBridge.Designer.onBuildingMethodChanged    += OnBuildingMethodChanged;
            EventBridge.Designer.onBrandCatalogChanged.OnInvoke += OnBrandCatalogChanged;
            EventBridge.Designer.onCabinetSwapStart         += OnCabinetSwapStart;
            EventBridge.Designer.onHighlightChanged.OnInvoke += OnHighlightChanged;
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke += OnCabinetAddedByUser;
            EventBridge.Designer.showLoadingUi              += ShowLoadingUI;
            EventBridge.Designer.refreshCabinetLoadingUiImage      += RefreshCabinetLoadingUiImage;
            EventBridge.Designer.setSpinnerStatusMessage    += SetSpinnerStatusMessage;
        }

        protected void OnDisable()
        {
            ProFitGlobals.productContent.ContentLoaded -= OnProductVisualContentLoaded;

            EventBridge.Designer.loadMenuScene              -= LoadMenuScene;
            EventBridge.Designer.restart                    -= ForgetCabinetSettings;
            EventBridge.Designer.showWhiteLabelClientInfoUi -= ShowWhiteLabelClientInfoUI;
            EventBridge.Designer.showBuildingMethodsUi      -= ShowBuildingMethodsUI;
            EventBridge.Designer.showBrandCatalogUi         -= ShowBrandCatalogUI;
            EventBridge.Designer.showCabinetShapeUi         -= ShowCabinetShapeUI;
            EventBridge.Designer.showKitchenTemplateUi      -= ShowKitchenTemplateUI;
            EventBridge.Designer.showCabinetDetailsUi       -= ShowCabinetOtherSizesUI;
            EventBridge.Designer.showTrialCountdownUi       -= ShowTrialCountdownUI;
            EventBridge.Designer.hideTrialCountdownUi       -= HideTrialCountdownUI;
            EventBridge.Designer.onBuildingMethodChanged    -= OnBuildingMethodChanged;
            EventBridge.Designer.onBrandCatalogChanged.OnInvoke -= OnBrandCatalogChanged;
            EventBridge.Designer.onCabinetSwapStart         -= OnCabinetSwapStart;
            EventBridge.Designer.onHighlightChanged.OnInvoke -= OnHighlightChanged;
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke -= OnCabinetAddedByUser;
            EventBridge.Designer.showLoadingUi              -= ShowLoadingUI;
            EventBridge.Designer.refreshCabinetLoadingUiImage      -= RefreshCabinetLoadingUiImage;
            EventBridge.Designer.setSpinnerStatusMessage    -= SetSpinnerStatusMessage;
        }

        /// <summary>
        ///     Check for the user selecting cabinets or clearing their
        ///     selection. The "other selections" (as referred to by the
        ///     function's name) are anything other than kitchen occupants.
        /// </summary>
        protected async UniTask HandleOtherKitchenSelections()
        {
            if (Input.GetMouseButtonDown(0) && !MelcherScreenEvents.IsAnyPointerOverGameObject())
            {
                // Close the hamburger menu
                m_DesignerNavigationUi.CloseHamburgerMenu();

                // Manually check against all cabinets rather than use a
                // collider box on the cabinets. This is to avoid issues where a
                // collider box would be unaware of the cut-outs.

                // One downside to this approach is the user can select objects
                // through a cabinet. We may need to revisit this and/or add
                // pointer-blocking geo to the inside of the cabinets. TODO

                if (m_Kitchen && m_Kitchen.layout != null)
                {
                    Ray pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);

                    float nearestCabinetDistance = float.PositiveInfinity;
                    Cabinet nearestCabinet = null;

                    float nearestOccupantDistance = float.PositiveInfinity;
                    Occupant nearestOccupant = null;

                    // Check if the raycast intersects any of the cabinets
                    foreach (ElementIndex<Cabinet> i in m_Kitchen.layout.EnumerateCabinets())
                    {
                        Cabinet cabinet = i.element;

                        // Some objects are marked as being manually selected.
                        // Currently, this is just cabinet shelves but the
                        // goal of this is to avoid the shelf's own collider
                        // from occluding the colliders of occupants within
                        // the shelf.
                        foreach (Occupant occupant in cabinet.occupants)
                        {
                            if (!occupant.productVisual.info.requireManualSelection)
                            {
                                continue;
                            }

                            Bounds b = occupant.productVisualContentInstance.clickCollider.bounds;
                            MelcherLog.Log("size: " + occupant.productVisualContentInstance.clickCollider.bounds.size);
                            Debug.DrawLine(Vector3.zero, b.min, Color.yellow, 3.0f);
                            Debug.DrawLine(Vector3.zero, b.max, Color.magenta, 3.0f);

                            if (occupant.productVisualContentInstance.clickCollider.bounds.IntersectRay(pointerRay, out float occupantDistance) &&
                                occupantDistance < nearestOccupantDistance)
                            {
                                Debug.DrawLine(pointerRay.origin, pointerRay.GetPoint(occupantDistance), Color.blue, 5.0f);
                                nearestOccupantDistance = occupantDistance;
                                nearestOccupant = occupant;
                            }
                        }

                        if (cabinet.GetOrientedBounds().IntersectRay(pointerRay, out float cabinetDistance) &&
                            cabinetDistance < nearestCabinetDistance)
                        {
                            bool allowCabinet;

                            switch (m_Kitchen.selectedBuildingMethod)
                            {
                                default:
                                    throw new System.InvalidOperationException("Unhandled kitchen building method: " + m_Kitchen.selectedBuildingMethod);

                                case KitchenBuildingMethod.Standard:
                                    allowCabinet = cabinet.cabinetType != CabinetType.Space;
                                    break;

                                case KitchenBuildingMethod.Sections:
                                    // Ignore corners, ends, and spacers.
                                    // `canHaveProducts` currently satisfies
                                    // every case we need, but its good to be
                                    // explicit in case products can be added
                                    // to corners in the future.
                                    allowCabinet = cabinet.canHaveProducts;
                                    break;

                                case KitchenBuildingMethod.Catalog:
                                    if (cabinet.Is(CabinetType.Corner, CabinetModelType.Procedural))
                                    {
                                        m_Kitchen.layout.GetCabinetNeighbours(
                                            cabinet,
                                            out Cabinet leftCabinet,
                                            out Cabinet rightCabinet
                                        );

                                        // Select the blind cabinet attached to
                                        // this cabinet because it represents
                                        // the corner.
                                        if (leftCabinet && leftCabinet.Is(CabinetType.StraightOnLeftOfCorner))
                                        {
                                            cabinet = leftCabinet;
                                            allowCabinet = true;
                                        }
                                        else if (rightCabinet && rightCabinet.Is(CabinetType.StraightOnRightOfCorner))
                                        {
                                            cabinet = rightCabinet;
                                            allowCabinet = true;
                                        }
                                        else
                                        {
                                            allowCabinet = false;
                                        }
                                    }
                                    else
                                    {
                                        allowCabinet =
                                            cabinet.canHaveProducts ||
                                            cabinet.Is(CabinetType.StraightOnLeftOfCorner) ||
                                            cabinet.Is(CabinetType.StraightOnRightOfCorner) ||
                                            cabinet.Is(CabinetType.Corner, CabinetModelType.Authored);
                                    }
                                    break;
                            }

                            // Select the kitchen cabinets
                            Debug.DrawLine(
                                pointerRay.origin,
                                pointerRay.GetPoint(cabinetDistance),
                                allowCabinet ? Color.green : Color.red,
                                5.0f
                            );

                            if (allowCabinet || cabinet.canOccludeSelectionClick)
                            {
                                // If the cabinet isn't allowed and is set to
                                // occlude selections, we want to save its
                                // click distance so further away cabinets are
                                // blocked/ignored for selection.

                                nearestCabinetDistance = cabinetDistance;
                                nearestCabinet = allowCabinet ? cabinet : null;
                            }
                        }
                    }

                    if (nearestOccupant)
                    {
                        MelcherLog.Log("Found nearest occupant : " + nearestOccupant.name, nearestOccupant);
                    }
                    else
                    {
                        MelcherLog.Log("No manual occupant found");
                    }

                    if (nearestOccupant)
                    {
                        await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(nearestOccupant.gameObject);
                        return;
                    }

                    if (nearestCabinet)
                    {
                        GameObject objectToHighlight = nearestCabinet.gameObject;

                        if (m_Kitchen.selectedBuildingMethod == KitchenBuildingMethod.Standard)
                        {
                            // When not in Sections mode, the entire kitchen
                            // is selected.
                            objectToHighlight = m_Kitchen.gameObject;
                        }

                        await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(objectToHighlight);
                        return;
                    }
                }

                // The raycast hit nothing so clear the selection
                await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(null);
            }
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

        protected void ShowLoadingUI(EventBridge.Designer.LoadingUIMode mode)
        {
            if (mode == EventBridge.Designer.LoadingUIMode.Hide)
            {
                m_LoadingUi.Hide();
            }
            else
            {
                // When loading, we display a static image. When saving, we
                // show a spinner.
                m_LoadingUi.loadingSprite =
                    mode == EventBridge.Designer.LoadingUIMode.Load ?
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
            if(ProFitGlobals.appRules.RequiresBrandCatalog)
            {
                if(catalog != null && catalog.BrandCatalogLoadingImage != null)
                {
                    return catalog.BrandCatalogLoadingImage;
                }
                else
                {
                    return ProFitGlobals.appOptions.designerSceneLoadingImage;
                }
            }
            else
            {
                return ProFitGlobals.appOptions.designerSceneLoadingImage;
            }
        }

        protected void SetSpinnerStatusMessage(string statusMessage)
        {
            m_LoadingUi.SetStatusMessage(statusMessage);
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

        protected void ShowCabinetOtherSizesUI(CabinetOtherSizesUI.Mode mode)
        {
            CabinetLayout layout = m_Kitchen.selectedCabinetLayout;

            if (mode != CabinetOtherSizesUI.Mode.ModifyExistingCabinet)
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
                ProFitGlobals.catalogData,
                m_ProductVisualDatabase,
                ProFitGlobals.appRules,
                m_Kitchen.selectedBuildingMethod,
                address,
                newlyLoadedProductVisualContent
            );
        }

        protected void OnBuildingMethodChanged(KitchenBuildingMethod newBuildingMethod)
        {
            RuntimeProductOverrides.RebuildRuntimeProducts(
                ProFitGlobals.catalogData,
                m_ProductVisualDatabase,
                ProFitGlobals.appRules,
                newBuildingMethod
            );
        }

        protected async UniTask OnBrandCatalogChanged(BrandCatalogSO selectedBrandCatalog)
        {
            RuntimeProductOverrides.ApplyBrandCatalogPadding(
                ProFitGlobals.catalogData,
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
                EventBridge.Designer.onCabinetSwapCanceled?.Invoke();
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

            await EventBridge.Designer.onTryHighlightObject.Invoke(null);
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

        private UniTask? m_HandleOtherKitchenSelectionsTask;

    }   // class DesignerScene

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-01
/// ===========================================================================
