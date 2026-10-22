namespace KitchenDesign
{
    using Melcher;
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
            ProductVisualContentLoader.FirstFromActiveScene()
                .onProductVisualContentLoaded += OnProductVisualContentLoaded;

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
            ProductVisualContentLoader contentLoader =
                ProductVisualContentLoader.FirstFromActiveScene();

            if (contentLoader)
            {
                // We were getting a null reference exception here. It seems
                // FirstFromActiveScene() fails during a scene transition.
                contentLoader.onProductVisualContentLoaded -= OnProductVisualContentLoaded;
            }

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

        protected Sprite GetLoadingImage(BrandCatalog catalog)
        {
            if(ProFitGlobals.isAppOptionGemini)
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

        /// <summary>
        ///     Called when a product visual's addressable content is loaded.
        /// </summary>
        /// <param name="productVisual"></param>
        /// <param name="newlyLoadedProductVisualContent"></param>
        protected void OnProductVisualContentLoaded(AssetAddress<ProductVisualContent> address, ProductVisualContentLoader.LoadedContent newlyLoadedProductVisualContent)
        {
            KitchenBuildingMethod selectedBuildingMethod = m_Kitchen.selectedBuildingMethod;

            if (selectedBuildingMethod != KitchenBuildingMethod.Catalog && !IsStollBuild())
            {
                return;
            }

            ProductVisual runtimeProductVisual = null;

            foreach (ProductVisual i in m_ProductVisualDatabase.runtimeEntries)
            {
                if (i.contentAddress == address)
                {
                    runtimeProductVisual = i;
                    break;
                }
            }

            foreach (ProductInfo runtimeProductInfo in ProFitGlobals.runtimeProducts)
            {
                if (runtimeProductInfo.visualId == runtimeProductVisual.info.id)
                {
                    ModifySmokerContentCutOutHeight(
                        runtimeProductInfo.sourceProductInfo,
                        runtimeProductVisual.sourceProductVisual.info,
                        newlyLoadedProductVisualContent.sourceContentPrefab,
                        runtimeProductInfo,
                        runtimeProductVisual.info,
                        newlyLoadedProductVisualContent.runtimeContentPrefab,
                        14.0f * Const.Inches2Meters
                    );
                }
            }
        }

        /// <summary>
        ///     Creates and alters the runtime products depending on which
        ///     build mode is selected. E.g., bar tops do not extend the
        ///     cabinet in Stoll builds. Also, fridges have tighter padding in
        ///     Stoll builds. Etc.
        /// </summary>
        protected void OnBuildingMethodChanged(KitchenBuildingMethod newBuildingMethod)
        {
            bool isStollBuild =
                ProFitGlobals.isAppOptionStoll;

            int stollManufacturerId = -1;

            if (GetStollManufacturer(out ManufacturerInfo stollInfo))
            {
                stollManufacturerId = stollInfo.id;
            }

            // Make copies of the server-parsed product data. This allows the
            // app to override some values on the copies while maintaining the
            // ability to restore the original server values if necessary.

            // TODO: This feels error-prone. It would be better to populate the
            //       runtime products list once and then reset those entries to
            //       match their source's values here. -Ryan
            ProFitGlobals.ClearRuntimeProducts();

            foreach (ProductInfo sourceProductInfo in ProFitGlobals.sourceProducts)
            {
                ProductInfo runtimeProductInfo = new (sourceProductInfo);

                ProFitGlobals.AddRuntimeProduct(runtimeProductInfo);

                ProductVisual sourceProductVisual =
                    ProFitUtility.FindProductVisual(
                        m_ProductVisualDatabase.sourceEntries,
                        runtimeProductInfo.visualId
                    );

                if (!sourceProductVisual)
                {
                    // This is a visual that we disabled (likely for memory
                    // optimization) and therefore isn't available. Skip it.
                    continue;
                }

                ProductVisual runtimeProductVisual =
                    m_ProductVisualDatabase.FindRuntimeVisualByID(
                        runtimeProductInfo.visualId
                    );

                if (!runtimeProductVisual)
                {
                    MelcherLog.LogError("Did not find runtime visual for " +  runtimeProductInfo.visualId);
                    continue;
                }


                if (isStollBuild)
                {
                    ModifyProductInfoForStollBuild(
                        stollManufacturerId,
                        sourceProductInfo,
                        sourceProductVisual.info,
                        runtimeProductInfo,
                        runtimeProductVisual.info
                    );
                }
                else if (newBuildingMethod == KitchenBuildingMethod.Catalog)
                {
                    ModifyProductInfoForCatalogBuildingMethod(
                        sourceProductInfo,
                        sourceProductVisual.info,
                        runtimeProductInfo,
                        runtimeProductVisual.info
                    );
                }
                else
                {
                    runtimeProductVisual.info.initialInsetFromEdgeInMeters =
                        sourceProductVisual.info.initialInsetFromEdgeInMeters;
                }
            }

            MelcherLog.Assert(
                ProFitGlobals.sourceProducts.Count == ProFitGlobals.runtimeProducts.Count,
                $"Failed to create an equal number of runtime product info as there are source product info. (Source={ProFitGlobals.sourceProducts.Count} vs Runtime={ProFitGlobals.runtimeProducts.Count}). " +
                $"These must be exactly one runtime entry for each source entry."
            );
        }

        /// <summary>
        ///     This function is invoked on each product that is registered.
        ///     Its purpose function is to apply any Stoll-specific changes to
        ///     the product.
        /// </summary>
        /// <param name="sourceProductInfo"></param>
        /// <param name="sourceProductVisual"></param>
        /// <param name="runtimeProductInfo"></param>
        /// <param name="runtimeProductVisual"></param>
        /// <param name="stollManufacturerId"></param>
        protected void ModifyProductInfoForStollBuild(
            int                 stollManufacturerId,
            ProductInfo         sourceProductInfo,
            ProductVisualInfo   sourceVisualInfo,
            ProductInfo         runtimeProductInfo,
            ProductVisualInfo   runtimeVisualInfo)
        {
            if (runtimeProductInfo.sku == "Backing:BarTop")
            {
                // Stoll's bar top risers are 3" deep.

                Vector3 productSizeInMeters =
                    runtimeProductInfo.productSizeInMeters;

                productSizeInMeters.z = 3.0f * Const.Inches2Meters;

                runtimeProductInfo.productSizeInMeters = productSizeInMeters;
            }

            if (!runtimeVisualInfo)
            {
                return;
            }

            if (runtimeVisualInfo.runtimePrefabCategory == ProductCategorySpecific.Fridge)
            {
                // Pro-Fit said that a quarter inch or no padding is fine
                // for fridges. This was later requested to be no padding.
                const float StollFridgeHorizontalPaddingInInches = 0.0f;

                runtimeProductInfo.productPaddingInMeters.left = StollFridgeHorizontalPaddingInInches * Const.Inches2Meters;
                runtimeProductInfo.productPaddingInMeters.right = StollFridgeHorizontalPaddingInInches * Const.Inches2Meters;
            }

            // Apply the custom minimum offsets for Stoll products.

            if (runtimeVisualInfo.anchor.IsAtBottom() &&
                runtimeVisualInfo.initialInsetFromEdgeInMeters > 0.0f)
            {
                // The distance between the bottom of the cabinet and the start of
                // a cut-out for a Stoll product.
                float stollBottomProductBottomPadding = 4.0f * Const.Inches2Meters;

                // The distance between the bottom of the cabinet and the start of
                // a cut-out for a non-Stoll product.
                float nonStollBottomProductBottomPadding = 4.0f * Const.Inches2Meters;

                runtimeVisualInfo.initialInsetFromEdgeInMeters =
                    runtimeProductInfo.manufacturerId == stollManufacturerId ?
                        stollBottomProductBottomPadding :
                        nonStollBottomProductBottomPadding;
            }
        }

        /// <summary>
        ///     This function is invoked on each product that is registered.
        ///     Its purpose function is to apply any Catalog-specific changes
        ///     to the product.
        /// </summary>
        protected void ModifyProductInfoForCatalogBuildingMethod(
            ProductInfo         sourceProductInfo,
            ProductVisualInfo   sourceVisualInfo,
            ProductInfo         runtimeProductInfo,
            ProductVisualInfo   runtimeVisualInfo)
        {
            if (runtimeProductInfo == null)
            {
                return;
            }

        }

        private void ModifySmokerContentCutOutHeight(
            ProductInfo         sourceProductInfo,
            ProductVisualInfo   sourceVisualInfo,
            ProductVisualContent sourceVisualContent,
            ProductInfo         runtimeProductInfo,
            ProductVisualInfo   runtimeVisualInfo,
            ProductVisualContent runtimeVisualContent,
            float               desiredCutOutHeightInMeters)
        {
            if (runtimeVisualInfo.runtimePrefabCategory != ProductCategorySpecific.Smoker)
            {
                return;
            }

            // Setup to shift the size of products cut-outs, or restore the
            // original cut-out size.

            Vector3 adjustmentOffsetInMeters = Vector3.zero;

            float cutOutHeightInMeters =
                sourceVisualContent.modelCutOutExtentMax.localPosition.y -
                sourceVisualContent.modelCutOutExtentMin.localPosition.y;

            if (runtimeVisualInfo.anchor == ProductAnchor.OnFrontAtTop &&
                runtimeVisualInfo.initialInsetFromEdgeInMeters <= 0.0f)
            {
                // This is a smoker that plunges into its cabinet from the
                // top. We want to adjust its plunge depth to fit the requested
                // 14" depth.

                adjustmentOffsetInMeters =
                    Vector3.up * (cutOutHeightInMeters - desiredCutOutHeightInMeters);

                cutOutHeightInMeters = desiredCutOutHeightInMeters;
            }

            runtimeProductInfo.cutOutSizeInMeters.y = cutOutHeightInMeters;

            runtimeVisualContent.modelCutOutExtentMin.localPosition =
                sourceVisualContent.modelCutOutExtentMin.localPosition + adjustmentOffsetInMeters;

            int modelCount = sourceVisualContent.scaleOrigin.childCount;

            for (int i = 0; i < modelCount; ++i)
            {
                Transform sourceModel = sourceVisualContent.scaleOrigin.GetChild(i);
                Transform runtimeModel = runtimeVisualContent.scaleOrigin.GetChild(i);

                runtimeModel.localPosition = sourceModel.localPosition + adjustmentOffsetInMeters;
            }
        }

        /// <summary>
        ///     Apply changes that are specific to a brand catalog. Currently,
        ///     this is only that Challenger Designs requires a mandatory 2.5"
        ///     of padding on the left and right of grills.
        /// </summary>
        protected async UniTask OnBrandCatalogChanged(BrandCatalog selectedBrandCatalog)
        {
            ProductVisualContentLoader contentLoader =
                ProductVisualContentLoader.FirstFromActiveScene();

            foreach (ProductInfo runtimeProductInfo in ProFitGlobals.runtimeProducts)
            {
                if (string.IsNullOrEmpty(runtimeProductInfo.visualId))
                {
                    // We are going to skip printing a warning message if the
                    // visual ID is completely empty. This is because there
                    // are seemingly a lot of products that have been added
                    // to the dashboard recently which do not have a product
                    // visual ID, intentional maybe, so there's no point in
                    // polluting the console logs here.
                    continue;
                }

                ProductVisual runtimeProductVisual =
                    m_ProductVisualDatabase.FindRuntimeVisualByID(
                        runtimeProductInfo.visualId
                    );

                if (!runtimeProductVisual)
                {
                    // This is likely a product visual that was unlinked from
                    // the database (to save memory). Log a warning anyway:
                    MelcherLog.LogWarningEditorOnly($"Visual ID not found in the product visual database: {runtimeProductInfo.visualId}");
                    continue;
                }

                float paddingInMeters = runtimeProductInfo.sourceProductInfo.productPaddingInMeters.left;

                bool isHeatedProduct =
                    runtimeProductVisual.info.runtimePrefabCategory is
                        ProductCategorySpecific.Grill or
                        ProductCategorySpecific.GrillWithInsulatedJacket or
                        ProductCategorySpecific.Cookware or
                        ProductCategorySpecific.Smoker;

                if (selectedBrandCatalog)
                {
                    // TODO: Should we do this for every product category?
                    if (isHeatedProduct ||
                        runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Fridge)
                    {
                        // By default, make most products have no padding in
                        // catalog mode. Challenger requires padding for its
                        // grills and fridges though and this is handled
                        // shortly hereafter.
                        paddingInMeters = 0.0f;
                    }

                    if (selectedBrandCatalog.ruleset == BrandCatalogRuleset.Challenger)
                    {
                        if (runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Fridge)
                        {
                            // Challenger fridges have a 1/4" of padding on the
                            // left and right.
                            // https://3.basecamp.com/3293895/buckets/37470979/messages/9033213434#__recording_9140977268
                            paddingInMeters = 0.25f * Const.Inches2Meters;
                        }
                        else if (runtimeProductVisual.info.runtimePrefabCategory != ProductCategorySpecific.Smoker)
                        {
                            // Otherwise, all products except for smokers (which
                            // come in their own housing) use 3" padding on
                            // the left and right.
                            // https://3.basecamp.com/3293895/buckets/37470979/messages/9033213434#__recording_9140931189
                            paddingInMeters = Const.Gemini.Challenger.OccupantMinimumHorizontalPaddingInMeters * 0.5f;
                        }
                    }

                    if (runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Fridge)
                    {
                        // Fix for z-fighting when a fridge is next to an open shelf.
                        // TODO: Consider adding a flag to ProductVisual to indicate
                        //       whether a cabinet is 'see-through' and then ensure a
                        //       fridge panel is added if it is next to a see-through
                        //       cabinet. -Ryan
                        const float MinimumPaddingInMeters = (1 / 128.0f) * Const.Inches2Meters;
                        paddingInMeters = MinimumPaddingInMeters;
                    }

                    if (runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Shelf)
                    {
                        paddingInMeters = 0.0f;
                    }

                    bool isHeatedOrBurnerProduct =
                        runtimeProductVisual.info.runtimePrefabCategory is
                            ProductCategorySpecific.Grill or
                            ProductCategorySpecific.GrillWithInsulatedJacket or
                            ProductCategorySpecific.Cookware or
                            ProductCategorySpecific.Burner or
                            ProductCategorySpecific.BurnerWithInsulatedJacket or
                            ProductCategorySpecific.Smoker;

                    // MARK: Custom Catalog Padding
                    if(isHeatedOrBurnerProduct)
                    {
                        if(selectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.AgoraBerlinGardens) // agora
                        {
                            if(runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Smoker)
                            {
                                paddingInMeters = 0f;
                            }
                            else
                            {
                                paddingInMeters = 2.25f * Const.Inches2Meters;
                            }
                        } 
                        else if(selectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.Toppin) // toppin
                        {
                            paddingInMeters = 3f * Const.Inches2Meters;
                        }
                        else if(selectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.Endura) // endura
                        {
                            paddingInMeters = 3f * Const.Inches2Meters;
                        }
                        else if(selectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.WeatherStrong) // weather strong
                        {
                            paddingInMeters = 3f * Const.Inches2Meters;
                        }
                        else if(selectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.WeatherStrongSelect) // weather strong select
                        {
                            paddingInMeters = 3f * Const.Inches2Meters;
                        }
                        else if(selectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.Naturekast) // naturekast
                        {
                            paddingInMeters = 3f * Const.Inches2Meters;
                        }
                    }
                }

                runtimeProductInfo.productPaddingInMeters.left = paddingInMeters;
                runtimeProductInfo.productPaddingInMeters.right = paddingInMeters;
            }

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

                        ProductVisual potentialProductVisual =
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

        protected static bool IsStollBuild()
        {
            return ProFitGlobals.isAppOptionStoll;
        }

        protected static bool GetStollManufacturer(out ManufacturerInfo stollInfo)
        {
            return ProFitUtility.FindManufacturerByName("Stoll", out stollInfo);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [UnityEngine.Serialization.FormerlySerializedAs("m_ProductVisualCatalog")]
        [SerializeField] protected ProductVisualDatabase m_ProductVisualDatabase = default;

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
