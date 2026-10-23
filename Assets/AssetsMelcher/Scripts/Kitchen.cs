namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Products;
    using KitchenDesign.KitchenModel;
    using KitchenDesign.Save;
    using KitchenDesign.BuildingRules;
    using KitchenDesign.Shared.Interfaces;
    using Unity.Cinemachine;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using KitchenDesign.Geometry;
    using KitchenDesign.Serializables;

    using JsonDict = System.Collections.Generic.IDictionary<string, object>;
    using JsonList = System.Collections.Generic.IList<object>;

    /// <summary>
    ///     Unfortunately, the kitchen is a god class. It is responsible for
    ///     most of the glue between systems. It handles saving and loading the
    ///     user's designed layouts, dragging occupants between cabinets and
    ///     shelves, and storing the user's selected finish options.
    /// </summary>
    public class Kitchen : MelcherMonoBehaviour, IKitchen
    {
        // =====================================================================
        // Static properties
        // =====================================================================

        /// <summary>
        ///     This static property is intended to replace the need to call
        ///     `FindObjectOfType` to locate the kitchen. The property's value
        ///     is assigned in `Kitchen.Awake()`.
        /// </summary>
        public static Kitchen instance => s_KitchenInstance;

        // =====================================================================
        // Properties
        // =====================================================================

        public System.DateTime sessionStartTime
        {
            get => this._selections.sessionStartTime;
            set => this._selections.sessionStartTime = value;
        }

        public KitchenBuildingMethod selectedBuildingMethod => this._selections.buildingMethod;

        public IBuildingMethodRules buildingRules
        {
            get
            {
                if (this._buildingRules == null || this._buildingRules.Method != this._selections.buildingMethod)
                {
                    this._buildingRules = new BuildingMethodRules(this._selections.buildingMethod);
                }

                return this._buildingRules;
            }
        }

        public BrandCatalogSO selectedBrandCatalog => this._selections.brandCatalog;

        public CountertopType selectedCountertopType => this._selections.countertopType;

        public CabinetToeKickType selectedToeKickType => this._selections.toeKickType;

        public CabinetLayout layout => m_CurrentCabinetLayout;

        //public IReadOnlyList<CabinetSection> cabinetSections => m_CabinetSections;

        public KitchenCutoutClipping cutoutClipping => m_CutoutClipping;

        public float baseCabinetDepthInMeters => this._selections.baseCabinetDepthInMeters;


        /// <summary>
        /// The height of a cabinet from the floor to the top of the countertop.
        /// This value is currently hard coded because it is unnecessary to
        /// change the height.
        /// </summary>
        public float cabinetHeightInMeters => 36.0f * Units.Inches2Meters;

        public float countertopThicknessInMeters => this._selections.countertopThicknessInMeters;

        public CuboidPadding baseCountertopOverhangInMeters => this._selections.countertopOverhangInMeters;


        public bool doCabinetsHaveColumns => this.buildingRules.HasCabinetColumns;

        // TODO: Do something better than expose this directly. It is exposed
        //       so the ground can be temporarily hidden during screenshots
        public GameObject ground => m_Ground.gameObject;


        public string selectedDoorStyleName
        {
            get => this._selections.doorStyleName;

            // TODO: Remove the setter and add an event to EventBridge to
            //       handle changing the door style.
            set
            {
                // Temporarily disabled, client request.

                // if (value == "Shaker" && this._selections.doorStyleName != value)
                // {
                //     EventBridge.Designer.showNotification?.Invoke(
                //         title: "Shaker Doors",
                //         message: "Please note that the Shaker style adds a 5% premium to your cabinet choices.",
                //         onDismiss: null
                //     );
                // }

                this._selections.doorStyleName = value;

            }
        }

        public string selectedDoorHandleStyleName
        {
            get => this._selections.doorHandleStyleName;
            set
            {
                this._selections.doorHandleStyleName = value;
            }
        }

        public float endPanelWidthInMetersLeft
        {
            get => this._selections.endPanelWidthInMetersLeft;
            set => this._selections.endPanelWidthInMetersLeft = this._selections.endPanelWidthInMetersLeft == value ? 0f : value;
        }

        public float endPanelWidthInMetersRight
        {
            get => this._selections.endPanelWidthInMetersRight;
            set => this._selections.endPanelWidthInMetersRight = this._selections.endPanelWidthInMetersRight == value ? 0f : value;
        }

        public float backPanelDepthInMeters
        {
            get => this._selections.backPanelDepthInMeters;
            set => this._selections.backPanelDepthInMeters = this._selections.backPanelDepthInMeters == value ? 0f : value;
        }

        public void UpdateLayoutCabinetMeshes()
        {
            m_CurrentCabinetLayout.UpdateMeshes();
            m_CurrentCabinetLayout.UpdatePositions();
        }

        public CabinetLayout selectedCabinetLayout => this._selections.layoutPrefab;
        public KitchenTemplateSO selectedKitchenTemplate => this._selections.kitchenTemplate;
        public ProductInfo selectedCountertopFinish => this._selections.countertopFinish;
        public ProductInfo selectedCabinetFinish => this._selections.cabinetFinish;
        public ProductInfo selectedFlooringFinish => this._selections.flooringFinish;

        public List<KitchenExtraFeature> selectedExtraFeatures => this._selections.extraFeatures;

        public ProductVisualDatabaseSO productVisualDatabase => m_ProductVisualDatabase;


        public string analyticsKey
        {
            get => this._selections.analyticsKey;
            set => this._selections.analyticsKey = value;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Return <see langword="true"/> if products are intended to
        ///     always snap to the center of their current cabinet. This is
        ///     the case for Stoll and Universal Kitchen Designer versions of
        ///     the app.
        /// </summary>
        /// <param name="cabinetForRuleset">
        ///     Optional. If provided, this cabinet may override the normal
        ///     <see cref="KitchenBuildingMethod"/> rules. E.g., a corner wedge
        ///     cabinet forces its products to be centered regardless of the
        ///     current kitchen building method.
        /// </param>
        /// <returns></returns>
        public bool AreProductsForcedToCenterOfCabinets(Cabinet cabinetForRuleset)
        {
            return
                (cabinetForRuleset != null && cabinetForRuleset.forceProductsToCenterBuildingMethods.HasFlag(this._selections.buildingMethod)) ||
                this.buildingRules.ProductsCenteredInCabinets;
        }

        public void RefreshTickerLabels()
        {
            this._widgets.RefreshTickerLabels();
        }

        /// <summary>
        ///     Adds the requested delta amount to the depth of all cabinets
        ///     (and width of corner cabinets).
        ///     <br /><br />
        ///
        ///     This was added for handling the back panels feature which adds
        ///     3/4" to the back of all cabinets when applied.
        /// </summary>
        public void ApplyDeltaToAllCabinetDepths(float deltaCabinetDepthInMeters)
        {
            foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (cabinet.cabinetType.IsEnd())
                {
                    // Ignore ends. Their size is dictated by the
                    // depth of their connected neighbour which we
                    // will handle after this loop.
                    continue;
                }

                // Authored corner cabinets' countertops don't work well with
                // scaling (scaling the entire countertop will distort its
                // non-rectangular shape) but we can correct for this by
                // selectively scaling only the overhang edges.
                if (cabinet.Is(CabinetType.Corner, CabinetModelType.Authored))
                {
                    CuboidPadding countertopOverhangInMeters = cabinet.GetCountertopOverhangInMeters();

                    countertopOverhangInMeters.towardMaxZ += deltaCabinetDepthInMeters;
                    countertopOverhangInMeters.right += deltaCabinetDepthInMeters;

                    cabinet.SetCountertopOverhang(countertopOverhangInMeters);
                }

                Vector3 sizeInMeters = cabinet.sizeInMeters;

                sizeInMeters.z += deltaCabinetDepthInMeters;

                if (cabinet.Is(CabinetType.Corner))
                {
                    sizeInMeters.x += deltaCabinetDepthInMeters;
                }

                cabinet.sizeInMeters = sizeInMeters;
            }

            // Recompute all cabinets sizes and positions. This
            // ensures end cabinets can size to the correct cabinet
            // depth.
            UpdateLayoutCabinetMeshes();

            this._widgets.RefreshTickerLabels();
        }

        public JsonDict SaveToJson()
        {
            return this._saveWriter.Write(m_CurrentCabinetLayout);
        }
        public UniTask<bool> LoadFromJson(JsonDict saveJson)
        {
            return this._saveReader.LoadFromJson(saveJson);
        }

        public Transform GetTransform()
        {
            return m_Transform;
        }

        /// <summary>
        ///     Find the source prefab for a cabinet by its type. Optionally,
        ///     a cabinet model type can be specified. When omitted, only the
        ///     cabinet type is checked.
        /// </summary>
        public Cabinet FindCabinetPrefab(CabinetType type, CabinetModelType? modelType = null)
        {
            foreach (Cabinet cabinet in m_CabinetPrefabs)
            {
                if (!cabinet)
                {
                    continue;
                }

                if (cabinet.cabinetType != type)
                {
                    continue;
                }

                if (modelType.HasValue && cabinet.cabinetModelType != modelType)
                {
                    continue;
                }

                return cabinet;
            }

            return null;
        }

        public void LateTick()
        {
            if (GetCenterOfTargetGroup(out Vector3 targetWorldCenter))
            {
                m_OrbitCamera.orbitRoot.position = targetWorldCenter;
            }
        }

        public void RefreshTopTickerInfo()
        {
            this._widgets.RefreshTopTickerInfo();
        }

        public async UniTask CreateCabinetLayoutFromShape(CabinetLayout prefab)
        {
            ProFitGlobals.productContent
                .Purge();

            MelcherLog.LogEditorOnly("Trying to create cabinet layout from prefab: " + prefab, prefab);

            // Destroy the existing layout
            if (m_CurrentCabinetLayout)
            {
                Destroy(m_CurrentCabinetLayout.gameObject);
                m_CurrentCabinetLayout = null;
            }

            if (prefab)
            {
                m_CurrentCabinetLayout =
                    Instantiate(
                        prefab,
                        m_Transform.position,
                        m_Transform.rotation,
                        m_Transform
                    );

                m_CurrentCabinetLayout.SetKitchen(this);

                if (this.buildingRules.PadsCornersForAdjacentSpans)
                {
                    // In standard mode, we enforce extra padding in the
                    // corners to prevent objects on adjacent spans from
                    // intersecting.
                    // https://3.basecamp.com/3293895/buckets/37470979/messages/9067338116

                    int cabinetCount = layout.CountCabinets();

                    foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                    {
                        Cabinet cabinet = i.element;

                        if (cabinet.cabinetType != CabinetType.Straight)
                        {
                            continue;
                        }

                        m_CurrentCabinetLayout.GetCabinetNeighbours(
                            cabinet,
                            out Cabinet leftCabinet,
                            out Cabinet rightCabinet
                        );

                        CuboidPadding occupiablePaddingInMeters =
                            cabinet.occupiablePaddingInMeters;

                        if (leftCabinet && leftCabinet.cabinetType == CabinetType.Corner)
                        {
                            occupiablePaddingInMeters.left += Const.CabinetCornerOccupiablePaddingInMeters;
                        }

                        if (rightCabinet && rightCabinet.cabinetType == CabinetType.Corner)
                        {
                            occupiablePaddingInMeters.right += Const.CabinetCornerOccupiablePaddingInMeters;
                        }

                        cabinet.occupiablePaddingInMeters = occupiablePaddingInMeters;
                    }
                }
                else if (this.buildingRules.HasCabinetColumns)
                {
                    // In Sections mode, corners cabinets are flanked with
                    // extra sections that shim in the cabinet columns for the
                    // interior corners.

                    Cabinet cornerColumnLeftPrefab =
                        FindCabinetPrefab(CabinetType.StraightOnLeftOfCorner);

                    Cabinet cornerColumnRightPrefab =
                        FindCabinetPrefab(CabinetType.StraightOnRightOfCorner);

                    int cabinetCount = layout.CountCabinets();

                    // Iterate in reverse because this is inserting cabinets and
                    // iterating forward would result in our indices being
                    // invalidated.
                    for (int i = cabinetCount - 1; i >= 0; --i)
                    {
                        Cabinet cabinet = m_CurrentCabinetLayout.GetCabinetAtIndex(i);

                        if (cabinet.cabinetType != CabinetType.Corner)
                        {
                            continue;
                        }

                        Cabinet cornerColumnLeft =
                            await m_CurrentCabinetLayout.InstantiateCabinet(
                                cornerColumnLeftPrefab,
                                cabinetSectionProductInfo: null,
                                this
                            );

                        Cabinet cornerColumnRight =
                            await m_CurrentCabinetLayout.InstantiateCabinet(
                                cornerColumnRightPrefab,
                                cabinetSectionProductInfo: null,
                                this
                            );

                        cornerColumnLeft.GetTransform().localEulerAngles =
                            cabinet.GetTransform().localEulerAngles;

                        cornerColumnRight.GetTransform().localEulerAngles =
                            cabinet.GetTransform().localEulerAngles + new Vector3(0.0f, 90.0f, 0.0f);

                        // Ensure any section to the right of the
                        // soon-to-be-added right column is anchored to the
                        // corner column's right edge.
                        if (i + 1 < cabinetCount)
                        {
                            Cabinet cabinetAfterColumnRight =
                                m_CurrentCabinetLayout.GetCabinetAtIndex(i + 1);

                            cabinetAfterColumnRight.normalizedAnchorToPreviousSection = new Vector3(1.0f, 0.0f, 0.0f);
                        }

                        // Set the right column's sibling index first to avoid
                        // shifting what `i` points to when the left column is
                        // added.
                        cornerColumnRight.GetTransform().SetSiblingIndex(i + 1);    // after the corner
                        cornerColumnLeft.GetTransform().SetSiblingIndex(i);         // before the corner
                    }

                    // Make the default cabinet sections small to match the
                    // smaller section sizes used in Sections building method.
                    float straightSectionWidthInMeters =
                        (float)ImperialUnit.FromTotalInches(3.0f * Units.InchesPerFoot).totalMeters;

                    foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                    {
                        Cabinet cabinet = i.element;

                        if (cabinet.cabinetType == CabinetType.EndLeft ||
                            cabinet.cabinetType == CabinetType.EndRight)
                        {
                            Vector3 endSizeInMeters = cabinet.sizeInMeters;
                            endSizeInMeters.x = Const.CabinetColumnWidthInMeters * 0.5f;
                            cabinet.sizeInMeters = endSizeInMeters;
                        }

                        if (cabinet.cabinetType == CabinetType.StraightOnLeftOfCorner ||
                            cabinet.cabinetType == CabinetType.StraightOnRightOfCorner)
                        {
                            Vector3 cornerColumnSizeInMeters = cabinet.sizeInMeters;

                            // The corner column sections only need to be as
                            // wide as their column will render, which is half
                            // the width of a support column because the other
                            // half exists in the neighbouring cabinet.
                            cornerColumnSizeInMeters.x = Const.CabinetColumnWidthInMeters * 0.5f;
                            cornerColumnSizeInMeters.z = this._selections.baseCabinetDepthInMeters;

                            cabinet.sizeInMeters = cornerColumnSizeInMeters;
                        }

                        if (cabinet.cabinetType == CabinetType.Straight)
                        {
                            Vector3 cabinetSizeInMeters = cabinet.sizeInMeters;
                            cabinetSizeInMeters.x = straightSectionWidthInMeters;
                            cabinet.sizeInMeters = cabinetSizeInMeters;
                        }

                        m_CurrentCabinetLayout.ModifyCabinetForBuildingMethod(cabinet);
                    }

                    // Apply the cabinet size changes.
                    m_CurrentCabinetLayout.UpdatePositions();
                }
                else if (this.buildingRules.UsesBrandCatalog)
                {
                    this._selections.baseCabinetDepthInMeters = this._selections.brandCatalog.standardCabinetDepthInMeters;

                    // In Catalog building method, the default procedural
                    // cabinets must be replaced with custom modeled versions.

                    ProductInfo fallbackCabinetSectionInfo =
                        ProFitGlobals.catalogData.FindProductInfoById(
                                this._selections.brandCatalog.fallbackDefaultCabinetSectionProductServerId,
                                findCabinetSection: true
                            );

                    if (fallbackCabinetSectionInfo == null)
                    {
                        MelcherLog.LogError(
                            $"No fallback cabinet product ID found for \"{this._selections.brandCatalog.fallbackDefaultCabinetSectionProductServerId}\".",
                            this._selections.brandCatalog
                        );
                    }

                    if (!ProFitGlobals.catalogData.FindBrandCatalogInfoById(selectedBrandCatalog.catalogId, out BrandCatalogInfo brandCatalogInfo))
                    {
                        MelcherLog.LogError(
                            $"No brand catalog for ID \"{this._selections.brandCatalog.catalogId}\".",
                            this._selections.brandCatalog
                        );
                    }

                    int nextDefaultCabinetSectionIndex = 0;

                    int cabinetCount = m_CurrentCabinetLayout.CountCabinets();

                    // Replace the default kitchen layout's cabinets with
                    // ones specified by the catalog. These are chosen
                    // only once and in the order they are defined by the
                    // catalog.

                    for (int i = 0; i < cabinetCount; ++i)
                    {
                        Cabinet cabinet =
                            m_CurrentCabinetLayout.GetCabinetAtIndex(i);

                        if (cabinet.cabinetType.IsEnd() &&
                            this._selections.brandCatalog.fridgeEndPanelInfo.specificPanelWidthInMeters > 0.0f)
                        {
                            Vector3 sizeInMeters = cabinet.sizeInMeters;
                            sizeInMeters.x = this._selections.brandCatalog.fridgeEndPanelInfo.specificPanelWidthInMeters;
                            cabinet.sizeInMeters = sizeInMeters;
                        }

                        if (cabinet.cabinetType != CabinetType.Straight)
                        {
                            SetCabinetHeight(
                                cabinet,
                                selectedBrandCatalog.standardCabinetHeightInMeters
                            );

                            cabinet.InstantiateCabinetLegs(this._selections.brandCatalog);
                            continue;
                        }

                        // Find the first cabinet from the brand catalog's
                        // "default cabinet list" that can be used to replace
                        // this current cabinet.

                        ProductInfo replacementCabinetInfo = null;

                        while (nextDefaultCabinetSectionIndex < brandCatalogInfo.startingCabinetSectionServerIds.Count &&
                            replacementCabinetInfo == null)
                        {
                            int defaultSectionServerId =
                                brandCatalogInfo.startingCabinetSectionServerIds[nextDefaultCabinetSectionIndex];

                            MelcherLog.Log($"startingCabinetServerId={defaultSectionServerId}");

                            ++nextDefaultCabinetSectionIndex;

                            replacementCabinetInfo =
                                ProFitGlobals.catalogData.FindProductInfoById(
                                    defaultSectionServerId,
                                    findCabinetSection: true
                                );

                            if (replacementCabinetInfo == null)
                            {
                                MelcherLog.LogError(
                                    $"No product info found for default cabinet section #{nextDefaultCabinetSectionIndex}, server ID: {defaultSectionServerId}",
                                    this._selections.brandCatalog
                                );

                                continue;
                            }

                            bool canReplace =
                                layout.CanReplaceCabinetWithCabinetByProductInfo(
                                    productVisualDatabase,
                                    cabinet,
                                    replacementCabinetInfo
                                );

                            if (!canReplace)
                            {
                                replacementCabinetInfo = null;
                            }
                        }

                        if (replacementCabinetInfo == null)
                        {
                            // We didn't find a suitable replacement cabinet
                            // from the catalog's preferred default cabinet
                            // list. Try to fallback to the catalog's fallback
                            // cabinet.

                            replacementCabinetInfo = fallbackCabinetSectionInfo;

                            MelcherLog.LogError(
                                $"No replacement cabinet defined or found for cabinet at index {i}. Using fallback.",
                                this._selections.brandCatalog
                            );

                            if (replacementCabinetInfo == null)
                            {
                                MelcherLog.LogError(
                                    $"No fallback cabinet product ID provided for Catalog {this._selections.brandCatalog.name}.",
                                    this._selections.brandCatalog
                                );

                                continue;
                            }
                        }

                        Cabinet replacementCabinet =
                            await m_CurrentCabinetLayout.InstantiateCabinet(
                                m_CatalogCabinetPrefab,
                                replacementCabinetInfo,
                                this
                            );

                        ProductVisualSO replacementCabinetVisual =
                            productVisualDatabase.FindRuntimeVisualByID(
                                replacementCabinetInfo.visualId
                            );

                        if (replacementCabinetVisual)
                        {
                            // Ensure cabinets use their intended cabinet type.
                            // This is necessary to fix up blind corner
                            // cabinets because all cabinets are instantiated
                            // from the same straight cabinet prefab.
                            replacementCabinet.SetCabinetType(replacementCabinetVisual.info.sectionCabinetType);
                        }

                        SetCabinetHeight(
                            replacementCabinet,
                            replacementCabinetInfo != null ?
                                replacementCabinetInfo.productSizeInMeters.y :
                                selectedBrandCatalog.standardCabinetHeightInMeters
                            );

                        // Replicate the settings of the original cabinet so
                        // the new cabinet is a perfect replacement.
                        replacementCabinet.GetTransform().rotation =
                            cabinet.GetTransform().rotation;

                        replacementCabinet.normalizedAnchorToPreviousSection =
                            cabinet.normalizedAnchorToPreviousSection;

                        replacementCabinet.backingSprite = cabinet.backingSprite;

                        // Remove the placeholder cabinet and replace it with
                        // this one.
                        m_CurrentCabinetLayout.DestroyCabinetAtIndex(i);

                        replacementCabinet.GetTransform().SetSiblingIndex(i);
                    }

                    /// <summary>
                    ///     Nested function for applying the custom brand
                    ///     catalog cabinet height.
                    /// </summary>
                    void SetCabinetHeight(Cabinet cabinet, float cabinetHeight)
                    {
                        Vector3 sizeInMeters = cabinet.sizeInMeters;
                        sizeInMeters.y = cabinetHeight;
                        cabinet.sizeInMeters = sizeInMeters;
                    }
                }

                SetBaseCabinetDepth(this._selections.baseCabinetDepthInMeters);
                SetCountertopThickness(this._selections.countertopThicknessInMeters);
                SetCountertopOverhang(this._selections.countertopOverhangInMeters);

                // Move cabinets to their proper anchors
                m_CurrentCabinetLayout.UpdatePositions();

                (Material cabinetMaterial, Material countertopMaterial) =
                    await this._materials.GetFinishMaterials(allowHighlightToReturn: true);

                // Refresh the cabinets materials. This ensures Napoleon (and
                // any future catalogs that have a cabinet material override)
                // will have their cabinets start with the intended materials.

                await m_CurrentCabinetLayout.SetCabinetMaterials(
                    cabinetMaterial,
                    countertopMaterial,
                    await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
                );
            }

            this._widgets.RefreshTopTickerInfo();

            // Forget the previous selection. (Only the kitchen will still
            // be valid and we want it to start un-selected.)
            this._interaction.highlightObject = null;

            UpdateCameraAndFramingGroups();

            this._widgets.RecreateResizeWidgets();
            this._widgets.RecreateTickerWidgets();

            // Reset the ground
            if (m_CurrentCabinetLayout)
            {
                CenterTheGround();
            }
            m_Ground.gameObject.SetActive(m_CurrentCabinetLayout);

            // Notify listeners that the kitchen layout has changed. This is
            // useful to initialize the camera rotation depending on which
            // layout was chosen.
            EventBridge.Designer.onKitchenLayoutChanged?.Invoke(prefab, m_CurrentCabinetLayout);

        }

        /// <summary>
        ///     Set the default cabinet depth. The individual cabinets may have
        ///     a bigger depth if they have a bar top added.
        /// </summary>
        public void SetBaseCabinetDepth(float depthInMeters)
        {
            this._selections.baseCabinetDepthInMeters = depthInMeters;

            if (m_CurrentCabinetLayout)
            {
                // First pass: Change the depth of all cabinets and snap any
                // back-anchored products to the new back position.
                foreach (CabinetSpan span in m_CurrentCabinetLayout.spans)
                {
                    span.SetBaseCabinetDepth(depthInMeters);
                }

                m_CurrentCabinetLayout.UpdatePositions();

                // Second pass: Fix up size of any backings.
                foreach (CabinetSpan span in m_CurrentCabinetLayout.spans)
                {
                    if (span.backing)
                    {
                        span.UpdateBackingSizeAndPosition();
                    }
                }
            }
        }

        public void SetCountertopThickness(float thicknessInMeters)
        {
            this._selections.countertopThicknessInMeters = thicknessInMeters;

            if (m_CurrentCabinetLayout)
            {
                foreach (CabinetSpan span in m_CurrentCabinetLayout.spans)
                {
                    span.UpdateBackingSizeAndPosition();
                }

                foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    cabinet.SetCountertopThickness(thicknessInMeters);
                }

                // Ensure the height of the cabinets are updated to match up
                // with the new countertop height.
                m_CurrentCabinetLayout.UpdateMeshes();
            }
        }

        /// <summary>
        ///     This function instantiates products: i.e., it instantiates
        ///     a <see cref="ProductVisualContent"/> prefab and connects it with a
        ///     <see cref="ProductInfo"/>.
        /// </summary>
        /// <param name="productInfo"></param>
        /// <param name="productVisualPrefab"></param>
        /// <returns></returns>
        public async UniTask<Occupant> InstantiateOccupant(ProductInfo productInfo, ProductVisualSO productVisual, ProductVisualContent productVisualPrefab)
        {
            // Clone the visual. Add the product to it
            ProductVisualContent visualInstance = Instantiate(productVisualPrefab, m_Transform);
            visualInstance.SetProductInfo(productInfo);
            visualInstance.name = productVisualPrefab.name;   // Remove "(Clone)" from name

            // The prefab to be instantiated is our "runtime" prefab. It exists
            // in the DontDestroyOnLoad scene but is disabled. We must re-enable
            // the instantiated prefab so it is visible.
            visualInstance.gameObject.SetActive(true);

            Occupant occupant = visualInstance.gameObject.AddComponent<Occupant>();
            occupant.SetInfo(productInfo, productVisual, visualInstance);

            (Material cabinetMaterial, Material countertopMaterial) =
                await this._materials.GetFinishMaterials(allowHighlightToReturn: true);

            await occupant.SetCabinetMaterials(
                cabinetMaterial,
                countertopMaterial,
                await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
            );

            // Create a little mock up to show where this object's cuboid is
            Debug_AttachSizeVisualizer(occupant);

            return occupant;
        }

        /// <summary>
        ///     On Linux, there is an issue with input in the game view where
        ///     sometimes a drag can be stopped without the end drag event being
        ///     signaled. This function is a bandaid intended to be called in
        ///     the editor to fix this issue.
        /// </summary>
        public void Debug_ForgetSelection()
        {
            this._interaction.productBeingDragged = null;
            this._interaction.volumeBeingResized = null;
        }

        /// <summary>
        /// Create a 3D-cube to help visualize the space this occupant occupies.
        /// The cube is hidden by default and only becomes visible when a
        /// key combination is pressed. See <see cref="DesignerScene.OnStateEvent"/>
        /// for the key logic.
        /// </summary>
        protected void Debug_AttachSizeVisualizer(Occupant occupant)
        {
            OccupantOccupiedCuboidVisualization debugVisualizer =
                Instantiate(m_ProductCuboidVisualizerPrefab, occupant.productVisualContentInstance.GetTransform());

            MelcherLog.LogEditorOnly("Created debug thing", debugVisualizer);

            debugVisualizer.SetOccupant(occupant);
        }

        // TODO: Need a way to specifically target a cabinet?

        public async UniTask<Occupant> TryInstantiateProductAnywhere(
            ProductInfo productInfo,
            ProductVisualSO productVisual,
            ProductVisualContent productVisualPrefab,
            bool showMessageIfFailed)
        {
            MelcherLog.LogEditorOnly("Trying to add product: " + productInfo.name);

            Occupant addedProduct = null;
            bool cabinetDepthTooSmall = false;
            float requiredDepth = 0.0f;

            // Try to add the product to each cabinet until one succeeds
            foreach (CabinetSpan span in layout.spans)
            {
                Cuboid? backingOccupancyInWorldSpace = null;

                if (span.backing)
                {
                    backingOccupancyInWorldSpace =
                        span.GetBackingOccupancyCuboidInWorldSpace(
                            span.backing.info,
                            span.backing
                        );
                }

                foreach (ElementIndex<Cabinet> i in span.cabinets)
                {
                    Cabinet cabinet = i.element;

                    Occupant newOccupant = null;
                    VolumeOccupantResult result = default;

                    if (AreProductsForcedToCenterOfCabinets(cabinet))
                    {
                        (newOccupant, result) =
                            await cabinet.TryInstantiateOccupantCentered(
                                productInfo,
                                productVisual,
                                productVisualPrefab,
                                InstantiateOccupant,
                                backingOccupancyInWorldSpace
                            );


                        if(result.status == OccupantCuboidStatus.Fits)
                        {
                            // TryInstantiateOccupantCentered() above already attempts to center the
                            // occupant, but it doesn't always land centered. Re-centering here is a
                            // quick fix to catch the cases it misses.
                            TryCenterOccupantInVolume(newOccupant);
                        }

                    }
                    else
                    {
                        (newOccupant, result) =
                            await cabinet.TryInstantiateOccupantAnywhere(
                                productInfo,
                                productVisual,
                                productVisualPrefab,
                                InstantiateOccupant,
                                backingOccupancyInWorldSpace
                            );
                    }

                    if (newOccupant)
                    {
                        // The product was added so nothing further is needed.
                        return newOccupant;
                    }

                    if (result.status == OccupantCuboidStatus.TooFarForward ||
                        result.status == OccupantCuboidStatus.TooFarBack)
                    {
                        cabinetDepthTooSmall = true;

                        float productInsetOnZ = 0.0f;
                        if (productVisual.info.anchor.IsOnTop())
                        {
                            // Add the z-inset distance when calculating the
                            // required cabinet depth. Ignore the inset if it is
                            // less than zero because it has already been factored
                            // into the occupant's cuboid size
                            if (productVisual.info.initialInsetFromEdgeInMeters > 0.0f)
                            {
                                productInsetOnZ = productVisual.info.initialInsetFromEdgeInMeters;
                            }
                        }

                        MelcherLog.Log(
                            $"How are we calculating the required depth: depth in cabinet = {result.occupantCuboidWithinVolume.depth} ({result.occupantCuboidWithinVolume.depth * Units.Meters2Inches}, inset on z = {productInsetOnZ} ({productInsetOnZ * Units.Meters2Inches})",
                            severity: MelcherLogSeverity.Verbose
                        );

                        requiredDepth = Mathf.Max(requiredDepth, result.occupantCuboidWithinVolume.depth + productInsetOnZ);
                    }
                }
            }

            // At this point, we're just handling user feedback for a product
            // that failed to be added
            Debug.Assert(!addedProduct);

            if (showMessageIfFailed)
            {
                if (cabinetDepthTooSmall)
                {
                    string requiredSize =
                        Utility.MeasurementFormat.FormatMetersToImperial(
                            requiredDepth,
                            showFeet: false,
                            showFraction: true
                        );

                    if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
                    {
                        requiredSize =
                            Utility.MeasurementFormat.FormatMetersToCentimeters(requiredDepth);
                    }

                    // Prompt the player to change their cabinet depth if the
                    // product doesn't fit
                    EventBridge.Designer.showNotification.Invoke(
                        title: "Product Doesn't Fit",
                        message: "This product is too big for the current " +
                            "cabinet depth. Please create a new cabinet that " +
                            $"is at least {requiredSize} deep to place this product.",
                        onDismiss: null
                    );
                }
                else
                {
                    EventBridge.Designer.showNotification.Invoke(
                        title: "No Room Available",
                        message: "There is not enough cabinet space to place " +
                            "this product. Please extend the island or remove " +
                            $"other products to free up space.",
                        onDismiss: null
                    );
                }
            }

            MelcherLog.LogEditorOnly("Failed to add product: " + productInfo.name);

            return null;
        }

        private void Awake()
        {
            this._interaction = new KitchenInteractionState();
            this._widgets = new KitchenWidgetsControl(this, this._interaction, this._selections, m_TopInfoLabel, m_TickerDimensionsRoot, m_TickerDimensionsPrefab, m_InputCabinetSizeWidget, m_ResizeCabinetWidgetPrefab, m_CabinetResizeWidgetRoot, m_CabinetShelfResizeWidgetRoot, ProFitGlobals.session, ProFitGlobals.clientList);
            this._materials = new KitchenMaterialControl(this, this._interaction, this._selections, m_Ground, m_GroundWood, m_GroundStone, m_ProductVisualDatabase, ProFitGlobals.appProfile, ProFitGlobals.productContent);
            this._drag = new OccupantDragControl(this, this._interaction, m_CameraPhysicsRaycaster, m_ProductPointerInteractionMask, ProFitGlobals.session);
            this._materials.Connect(this._widgets);
            this._drag.Connect(this._widgets);
            this._saveWriter = new KitchenSaveWriter(this._selections, m_ProductVisualDatabase, ProFitGlobals.session, ProFitGlobals.clientList, ProFitGlobals.appRules, ProFitGlobals.catalogData);
            this._saveReader = new KitchenSaveReader(this, this._selections, m_ProductVisualDatabase, m_LayoutPrefabs, m_CabinetBackingSprites, m_BacksplashPrefab, m_HighBarTopPrefab, m_LowBarTopPrefab, ProFitGlobals.clientList, ProFitGlobals.catalogData, ProFitGlobals.appProfile, ProFitGlobals.appRules, ProFitGlobals.productContent);

            if (s_KitchenInstance)
            {
                MelcherLog.LogError(
                    $"A kitchen instance already exists but another is coming online. " +
                    $"The newer one will take priority for {MelcherFormat.ConsoleFieldName(nameof(instance))}.",
                    s_KitchenInstance
                );
            }

            s_KitchenInstance = this;

            m_Ground.gameObject.SetActive(false);

            // Assume the default material is selected for each surface type
            this._selections.cabinetFinish     = null;
            this._selections.countertopFinish  = null;
            this._selections.flooringFinish    = null;

            Camera eventCamera = GetCamera();

            ResizerWidget leftPlungeResizer =
                CreatePlungeWidget(
                    name: "LeftShelfResizer",
                    m_ResizeCabinetWidgetPrefab,
                    m_CabinetShelfResizeWidgetRoot,
                    eventCamera,
                    anchor: new (0.0f, 1.0f, 0.0f)
                );

            leftPlungeResizer.affectExtent = Extent.Min;

            ResizerWidget rightPlungeResizer =
                CreatePlungeWidget(
                    name: "RightShelfResizer",
                    m_ResizeCabinetWidgetPrefab,
                    m_CabinetShelfResizeWidgetRoot,
                    eventCamera,
                    anchor: new (1.0f, 1.0f, 0.0f)
                );

            rightPlungeResizer.affectExtent = Extent.Max;

            // This is a button that could allow a plunge's size to be set.
            // The reason this is commented out is because the current
            // "set cabinet width" code always adjusts the cabinet's Extent.Max
            // whereas a true solution would expand both extents and attempt to
            // make the plunge fit if it were to expand outside the cabinet.

            //ButtonWidget inputShelfSizeButton =
            //    CreatePlungeWidget(
            //        name: "InputShelfSizeWidget",
            //        m_InputCabinetSizeWidget,
            //        m_CabinetShelfResizeWidgetRoot,
            //        eventCamera,
            //        anchor: new (0.5f, 1.0f, 0.0f)
            //    );

            this._widgets.SetCabinetShelfResizeWidgetsVisible(target: null);

            /// <summary>
            ///     Create a kitchen widget for a cabinet plunge/shelf.
            /// </summary>
            static WidgetType CreatePlungeWidget<WidgetType>(
                string      name,
                WidgetType  widgetPrefab,
                Transform   instanceParent,
                Camera      eventCamera,
                Vector3     anchor)
                where WidgetType : KitchenWidget
            {
                WidgetType widget =
                    KitchenWidgetsControl.CreateKitchenWidget(
                        widgetPrefab,
                        instanceParent,
                        eventCamera,
                        target: null
                    );

                widget.name = name;

                widget.normalizedTargetAnchor = anchor;

                return widget;
            }
        }

        private void OnEnable()
        {
            m_CutoutClipping.Attach(this);

            EventBridge.Designer.restart                    += Restart;
            EventBridge.Designer.createKitchen.OnInvoke     += CreateKitchen;
            EventBridge.Designer.onWantCabinetLayout        += GetCabinetLayout;

            EventBridge.Designer.setBuildingMethod          += SetBuildingMethod;
            EventBridge.Designer.setBrandCatalog.OnInvoke   += SetBrandCatalog;
            EventBridge.Designer.setCountertopType          += SetCountertopType;
            EventBridge.Designer.setCabinetLayout           += SetCabinetLayout;
            EventBridge.Designer.setKitchenTemplate         += SetKitchenTemplate;
            EventBridge.Designer.setBaseCabinetDepth        += SetBaseCabinetDepth;
            EventBridge.Designer.setCountertopThickness     += SetCountertopThickness;
            EventBridge.Designer.setCountertopOverhang      += SetCountertopOverhang;

            EventBridge.Designer.onCabinetResized.OnInvoke              += OnCabinetResized;
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke          += OnCabinetAddedByUser;
            EventBridge.Designer.onCabinetReordered.OnInvoke            += OnCabinetReordered;
            EventBridge.Designer.onCabinetDestroyedByUser.OnInvoke      += OnCabinetDestroyedByUser;


            // UI
            EventBridge.Designer.onTryAddProduct                        += TryAddProduct;
            EventBridge.Designer.onTryCloneProduct                      += TryAddProduct;
            EventBridge.Designer.onTryCloneSection                      += TryCloneSection;
            EventBridge.Designer.onTryAddSection                        += OnToolbarAddSectionClicked;
            EventBridge.Designer.onTryDestroyOccupant.OnInvoke          += TryDestroyOccupant;
            EventBridge.Designer.onTryDestroyCabinetBacking.OnInvoke    += TryDestroyCabinetBacking;
            EventBridge.Designer.onTryDestroySection                    += TryDestroySection;
            EventBridge.Designer.onFinishMaterialChanged                += this._materials.OnToolbarFinishProductSelected;
            EventBridge.Designer.onTryRepositionOccupant                += TryRepositionProduct;
            EventBridge.Designer.onTryReorderCabinet.OnInvoke           += TryReorderCabinet;
            EventBridge.Designer.tryToggleToeKicks                      += TryToggleToeKicks;
            EventBridge.Designer.onToeKickTypeChanged                   += OnToeKickTypeChanged;
            EventBridge.Designer.centerOccupantInVolume                 += OnCenterOccupantInVolumeClicked;

            // Products
            EventBridge.Designer.onOccupantInstantiated.OnInvoke        += OnOccupantInstantiated;
            EventBridge.Designer.onOccupantAddedToCabinet.OnInvoke      += OnOccupantAddedToCabinet;
            EventBridge.Designer.onOccupantRemovedFromCabinet.OnInvoke  += OnOccupantRemovedFromCabinet;
            EventBridge.Designer.onOccupantDragBegin                    += this._drag.OnOccupantDragBegin;
            EventBridge.Designer.onOccupantDrag                         += this._drag.OnOccupantDrag;
            EventBridge.Designer.onOccupantDragEnd.OnInvoke             += this._drag.OnOccupantDragEnd;

            EventBridge.Designer.onTryHighlightObject.OnInvoke          += this._materials.TryHighlightObject;

            foreach (Transform shelfWidgetTransform in m_CabinetShelfResizeWidgetRoot)
            {
                if (shelfWidgetTransform.TryGetComponent(out ResizerWidget shelfWidget))
                {
                    shelfWidget.onResizeBegin += this._widgets.OnCabinetShelfResizeBegin;
                    shelfWidget.onResizeEnd += this._widgets.OnCabinetShelfResizeEnd;
                    shelfWidget.onResized += this._widgets.OnCabinetShelfResized;
                }
            }
        }

        private void OnDisable()
        {
            m_CutoutClipping.Detach();

            EventBridge.Designer.restart                    -= Restart;
            EventBridge.Designer.createKitchen.OnInvoke     -= CreateKitchen;
            EventBridge.Designer.onWantCabinetLayout        -= GetCabinetLayout;

            EventBridge.Designer.setBuildingMethod          -= SetBuildingMethod;
            EventBridge.Designer.setBrandCatalog.OnInvoke   -= SetBrandCatalog;
            EventBridge.Designer.setCabinetLayout           -= SetCabinetLayout;
            EventBridge.Designer.setKitchenTemplate         -= SetKitchenTemplate;
            EventBridge.Designer.setBaseCabinetDepth        -= SetBaseCabinetDepth;
            EventBridge.Designer.setCountertopThickness     -= SetCountertopThickness;
            EventBridge.Designer.setCountertopOverhang      -= SetCountertopOverhang;

            EventBridge.Designer.onCabinetResized.OnInvoke              -= OnCabinetResized;
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke          -= OnCabinetAddedByUser;
            EventBridge.Designer.onCabinetReordered.OnInvoke            -= OnCabinetReordered;
            EventBridge.Designer.onCabinetDestroyedByUser.OnInvoke      -= OnCabinetDestroyedByUser;

            // UI
            EventBridge.Designer.onTryAddProduct                        -= TryAddProduct;
            EventBridge.Designer.onTryCloneProduct                      -= TryAddProduct;
            EventBridge.Designer.onTryCloneSection                      -= TryCloneSection;
            EventBridge.Designer.onTryAddSection                        -= OnToolbarAddSectionClicked;
            EventBridge.Designer.onTryDestroyOccupant.OnInvoke          -= TryDestroyOccupant;
            EventBridge.Designer.onTryDestroyCabinetBacking.OnInvoke    -= TryDestroyCabinetBacking;
            EventBridge.Designer.onTryDestroySection                    -= TryDestroySection;
            EventBridge.Designer.onFinishMaterialChanged                -= this._materials.OnToolbarFinishProductSelected;
            EventBridge.Designer.onTryRepositionOccupant                -= TryRepositionProduct;
            EventBridge.Designer.onTryReorderCabinet.OnInvoke           -= TryReorderCabinet;
            EventBridge.Designer.tryToggleToeKicks                      -= TryToggleToeKicks;
            EventBridge.Designer.onToeKickTypeChanged                   -= OnToeKickTypeChanged;
            EventBridge.Designer.centerOccupantInVolume                 -= OnCenterOccupantInVolumeClicked;

            // Products
            EventBridge.Designer.onOccupantInstantiated.OnInvoke        -= OnOccupantInstantiated;
            EventBridge.Designer.onOccupantAddedToCabinet.OnInvoke      -= OnOccupantAddedToCabinet;
            EventBridge.Designer.onOccupantRemovedFromCabinet.OnInvoke  -= OnOccupantRemovedFromCabinet;
            EventBridge.Designer.onOccupantDragBegin                    -= this._drag.OnOccupantDragBegin;
            EventBridge.Designer.onOccupantDrag                         -= this._drag.OnOccupantDrag;
            EventBridge.Designer.onOccupantDragEnd.OnInvoke             -= this._drag.OnOccupantDragEnd;

            EventBridge.Designer.onTryHighlightObject.OnInvoke          -= this._materials.TryHighlightObject;

            foreach (Transform shelfWidgetTransform in m_CabinetShelfResizeWidgetRoot)
            {
                if (shelfWidgetTransform.TryGetComponent(out ResizerWidget shelfWidget))
                {
                    shelfWidget.onResizeBegin -= this._widgets.OnCabinetShelfResizeBegin;
                    shelfWidget.onResizeEnd -= this._widgets.OnCabinetShelfResizeEnd;
                    shelfWidget.onResized -= this._widgets.OnCabinetShelfResized;
                }
            }
        }

        private void Restart()
        {
            OnRestart().Forget();

            async UniTaskVoid OnRestart()
            {
                this._selections.sessionStartTime = System.DateTime.UtcNow;

                m_TopInfoLabel.phrase = "";

                this._widgets.SetCabinetShelfResizeWidgetsVisible(target: null);

                if (await TryLoadRequestedSave())
                {
                    TrySendOnBrandCatalogChanged();
                    return;
                }

                SetBuildingMethod(ProFitGlobals.appOptions.defaultBuildingMethod);
                SetCountertopType(CountertopType.Default);
                this._selections.cabinetFinish = null;
                this._selections.countertopFinish = null;
                this._selections.flooringFinish = null;
                this._selections.layoutPrefab = null;
                this._selections.kitchenTemplate = null;
                this._selections.toeKickType = CabinetToeKickType.None;
                this._selections.extraFeatures.Clear();
                this._selections.analyticsKey = "";

                // Restore the default flooring material
                await this._materials.ChangeFinish(this._selections.flooringFinish, ProductCategory.FinishFloor);

                // Destroy the old kitchen. Passing null will let us destroy the
                // old one without creating a new one
                await CreateCabinetLayoutFromShape(prefab: null);

                if (ProFitGlobals.session.ShouldUseWhiteLabelMode())
                {
                    EventBridge.Designer.showWhiteLabelClientInfoUi.Invoke();
                }
                else if (ProFitGlobals.appOptions.allowBuildingMethods)
                {
                    EventBridge.Designer.showBuildingMethodsUi.Invoke();
                }
                else if (this.buildingRules.UsesBrandCatalog)
                {
                    EventBridge.Designer.showBrandCatalogUi.Invoke();
                }
                else
                {
                    EventBridge.Designer.showCabinetShapeUi.Invoke();
                }
            }
        }

        protected UniTask<bool> TryLoadRequestedSave()
        {
            return this._saveReader.TryLoadRequestedSave();
        }

        protected async UniTask CreateKitchen()
        {
            using var clearLoadingSpinner =
                MelcherDeferred.Defer(
                    onDispose: static () => EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Hide)
                );

            EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Load);

            // Assume the default material is selected for each surface type
            if (this._selections.kitchenTemplate)
            {
                EventBridge.Designer.setSpinnerStatusMessage?.Invoke(
                    $"Preparing from {this._selections.kitchenTemplate.displayName} template"
                );

                Dictionary<string, object> saveJson =
                    (Dictionary<string, object>)
                        MiniJSON.Json.Deserialize(this._selections.kitchenTemplate.saveData.text);

                float selectedCountertopThicknessInMeters = this._selections.countertopThicknessInMeters;

                BrandCatalogSO selectedBrandCatalog =
                    this._selections.brandCatalog ?
                        ProFitGlobals.runtimeBrandCatalogs.Find(x => x.catalogId == this._selections.brandCatalog.catalogId) :
                        null;

                await LoadFromJson(saveJson);

                // A non-null but empty analytics key. This ensures the kitchen
                // will be tracked for analytics (as opposed to a strictly null
                // analytics key read from a pre-analytics save file).
                this._selections.analyticsKey = "";

                // Ensure the user-selected values are not erased after loading
                // a template file.
                SetCountertopThickness(selectedCountertopThicknessInMeters);

                await SetBrandCatalog(selectedBrandCatalog);

                this._widgets.RefreshTopTickerInfo();
            }
            else
            {
                EventBridge.Designer.setSpinnerStatusMessage?.Invoke(
                    $"Preparing from {this._selections.layoutPrefab.displayName} layout"
                );

                await CreateCabinetLayoutFromShape(this._selections.layoutPrefab);
            }

            // A non-null, but empty analytics token. This is used to signal to
            // the server that this design needs an analytics token assigned
            // when it is next saved. Legacy saves which lack an analytics token
            // (m_AnalyticsToken == null) will not be tracked for analytics.
            this._selections.analyticsKey = "";

            // Apply brand catalog's default toe kick. Currently, this is only
            // necessary to enforce Napoleon's mandatory toe kick.
            if (m_CurrentCabinetLayout && this.buildingRules.UsesBrandCatalog)
            {
                this._selections.toeKickType = this._selections.brandCatalog.defaultToeKickType;
                EventBridge.Designer.onToeKickTypeChanged?.Invoke(this._selections.toeKickType);
            }

            // Ensure the cabinet is the client's intended outside-edge length
            // (at time of writing, that is 15", as set as initial size on
            // straight cabinet prefab)
            if (m_CurrentCabinetLayout && this.buildingRules.SizesStraightCabinetsToOutsideEdge)
            {
                foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    // Resize all cabs to the intended outside-edge length
                    if (cabinet.cabinetType == CabinetType.Straight)
                    {
                        Vector3 cabinetSizeInMeters = cabinet.sizeInMeters;

                        m_CurrentCabinetLayout.GetCabinetNeighbours(
                            cabinet,
                            out Cabinet leftCabinet,
                            out Cabinet rightCabinet
                        );

                        if (leftCabinet && leftCabinet.cabinetType == CabinetType.Corner)
                        {
                            cabinetSizeInMeters.x -= this._selections.baseCabinetDepthInMeters;
                        }

                        if (rightCabinet && rightCabinet.cabinetType == CabinetType.Corner)
                        {
                            cabinetSizeInMeters.x -= this._selections.baseCabinetDepthInMeters;
                        }

                        cabinet.sizeInMeters = cabinetSizeInMeters;
                    }
                }

                m_CurrentCabinetLayout.UpdatePositions();
                m_CurrentCabinetLayout.UpdateMeshes();

                this._widgets.RefreshTickerLabels();

                CenterTheGround();
            }

            // The kitchen is ready.
            await EventBridge.Designer.onKitchenReady.InvokeIfNonEmpty();
        }

        protected void GetCabinetLayout(ref CabinetLayout cabinetLayout)
        {
            cabinetLayout = m_CurrentCabinetLayout;
        }

        protected void SetBuildingMethod(KitchenBuildingMethod buildingMethod)
        {
            this._selections.buildingMethod = buildingMethod;

            EventBridge.Designer.onBuildingMethodChanged?.Invoke(this._selections.buildingMethod);
        }

        protected async UniTask SetBrandCatalog(BrandCatalogSO brandCatalog)
        {
            this._selections.brandCatalog = brandCatalog;

            this._selections.doorStyleName =
                brandCatalog.defaultCabinetSectionDoorStyleName;

            this._selections.doorHandleStyleName = 
                brandCatalog.defaultCabinetSectionDoorHandleStyleName;

            this._selections.endPanelWidthInMetersLeft = 0f;
            this._selections.endPanelWidthInMetersRight = 0f;
            this._selections.backPanelDepthInMeters = 0f;

            // TODO: Display a sprite for the selected brand catalog in the
            //       designer scene.

            await EventBridge.Designer.onBrandCatalogChanged.InvokeIfNonEmpty(brandCatalog);
        }

        /// <summary>
        ///     Called when the user selects a countertop type from the
        ///     kitchen setup UI.
        /// </summary>
        protected void SetCountertopType(CountertopType countertopType)
        {
            this._selections.countertopType = countertopType;
        }

        protected void SetCabinetLayout(CabinetLayout layoutPrefab)
        {
            this._selections.layoutPrefab = layoutPrefab;
        }

        protected void SetKitchenTemplate(KitchenTemplateSO template)
        {
            this._selections.kitchenTemplate = template;
        }

        protected async UniTask TryDestroyOccupant(Occupant occupant)
        {
            if (!occupant)
            {
                return;
            }

            OccupiableVolume occupiedVolume = occupant.occupiedVolume;
            if (!occupiedVolume)
            {
                MelcherLog.LogError("Product should have an occupied volume.", occupant);
                return;
            }

            bool isOccupantHighlighted =
                occupant.gameObject == this._interaction.highlightObject;

            if (isOccupantHighlighted)
            {
                // Deselect the occupant before it is removed from its cabinet.
                // This ensure IOccupantHighlightChangedHandler can safely
                // access the product's cabinet if it needs to.
                await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(null);
            }

            if (!await occupiedVolume.TryRemoveOccupant(occupant))
            {
                MelcherLog.LogError("Could not remove occupant from cabinet.", occupant);

                if (isOccupantHighlighted)
                {
                    // We could not remove the occupant. Recover its selected
                    // state so the user can try to remove it again.
                    await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(occupant.gameObject);
                }

                return;
            }

            // We don't need the product anymore
            Destroy(occupant.gameObject);
        }

        protected async UniTask TryDestroyCabinetBacking(CabinetBacking backingToDestroy)
        {
            if (!backingToDestroy)
            {
                return;
            }

            bool isBackingHighlighted =
                backingToDestroy.gameObject == this._interaction.highlightObject;

            if (isBackingHighlighted)
            {
                // Deselect the occupant before it is removed from its cabinet.
                // This ensure IOccupantHighlightChangedHandler can safely
                // access the product's cabinet if it needs to.
                await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(null);
            }

            foreach (CabinetSpan span in layout.spans)
            {
                if (span.backing != backingToDestroy)
                {
                    continue;
                }

                if (!await span.TryDestroyBacking())
                {
                    EventBridge.Designer.showToast?.Invoke(
                        "The backing can't be removed due to some of the current products."
                    );

                    if (isBackingHighlighted)
                    {
                        // We could not remove the backing. Recover its
                        // selected state so the user can try to remove it
                        // again.
                        await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(backingToDestroy.gameObject);
                    }
                }

                break;
            }
        }

        protected void TryDestroySection(Cabinet cabinetToDestroy)
        {
            OnTryDestroySection(cabinetToDestroy).Forget();

            async UniTaskVoid OnTryDestroySection(Cabinet cabinetToDestroy)
            {
                // TODO: Move this behaviour to KitchenLayout.
                if (!cabinetToDestroy)
                {
                    return;
                }

                if (cabinetToDestroy.cabinetType.IsMidSpanCabinetType(selectedBuildingMethod) &&
                    !layout.CabinetHasMidSpanCabinetNeighbour(cabinetToDestroy))
                {
                    EventBridge.Designer.showToast("There must be at least one cabinet per side.", holdDuration: 1.5f);
                    return;
                }

                int cabinetToDestroyIndex = cabinetToDestroy.GetTransform().GetSiblingIndex();

                Cabinet replacementCornerCabinet = null;

                if (cabinetToDestroy.cabinetType == CabinetType.Corner)
                {
                    // We need to replace the authored corner with a procedural
                    // corner.

                    Cabinet cornerCabinetPrefab =
                        FindCabinetPrefab(
                            CabinetType.Corner,
                            CabinetModelType.Procedural
                        );

                    if (!cornerCabinetPrefab)
                    {
                        EventBridge.Designer.showToast("Could not find default corner cabinet prefab.", holdDuration: 1.5f);
                        return;
                    }

                    replacementCornerCabinet =
                        await layout.InstantiateCabinet(cornerCabinetPrefab, null, this);

                    replacementCornerCabinet.GetTransform().SetSiblingIndex(cabinetToDestroyIndex);

                    replacementCornerCabinet.normalizedAnchorToPreviousSection =
                        cabinetToDestroy.normalizedAnchorToPreviousSection;

                    replacementCornerCabinet.SetCountertopThickness(countertopThicknessInMeters);

                    replacementCornerCabinet.SetCountertopOverhang(baseCountertopOverhangInMeters);

                    replacementCornerCabinet.SetBaseCabinetDepth(
                        baseCabinetDepthInMeters,
                        CabinetResizeAxis.X | CabinetResizeAxis.Z
                    );

                    Vector3 sizeInMeters = replacementCornerCabinet.sizeInMeters;
                    sizeInMeters.x = this._selections.baseCabinetDepthInMeters;
                    if (this._selections.brandCatalog)
                    {
                        // Ensure the cabinet's height matches the catalog's
                        // default value.
                        sizeInMeters.y = this._selections.brandCatalog.standardCabinetHeightInMeters;
                    }
                    sizeInMeters.z = this._selections.baseCabinetDepthInMeters;
                    replacementCornerCabinet.sizeInMeters = sizeInMeters;
                }

                if (this._interaction.highlightObject == cabinetToDestroy.gameObject)
                {
                    await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(null);
                }

                // Ensure the right neighbour's anchor inherits the anchor of the
                // cabinet being removed. This prevents issues with corners
                // disappearing.

                Cabinet cabinetOnRight = layout.GetCabinetAtIndex(cabinetToDestroyIndex + 1);

                if (cabinetOnRight && cabinetOnRight.cabinetType.IsMidSpanCabinetType(selectedBuildingMethod))
                {
                    cabinetOnRight.normalizedAnchorToPreviousSection =
                        cabinetToDestroy.normalizedAnchorToPreviousSection;
                }

                int spanIndex = layout.FindCabinetSpanIndex(cabinetToDestroy);

                // Allow listeners to respond to the cabinet's deletion before
                // it is fully gone.
                EventBridge.Designer.onPreCabinetDestroyedByUser?.Invoke(cabinetToDestroy);

                // Remove the cabinet as a child from the layout. This is needed
                // to prevent the layout's UpdatePositions() from still treating
                // the deleted cabinet as valid for the rest of this frame and
                // leaving a gap.
                cabinetToDestroy.GetTransform().SetParent(null);
                Destroy(cabinetToDestroy.gameObject);

                layout.UpdatePositions();

                if (replacementCornerCabinet)
                {
                    // Ensure that the replacement corner inherits the existing
                    // cabinet backings.
                    await layout.ApplyCabinetBackingsToCabinet(replacementCornerCabinet);
                }

                layout.UpdateMeshes();

                await EventBridge.Designer.onCabinetDestroyedByUser.InvokeIfNonEmpty(spanIndex);
            }
        }

        private void SetCountertopOverhang(CuboidPadding overhangInMeters)
        {
            this._selections.countertopOverhangInMeters = overhangInMeters;

            if (m_CurrentCabinetLayout)
            {
                foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    cabinet.SetCountertopOverhang(overhangInMeters);
                }
            }
        }

        protected async UniTask OnCabinetResized(Cabinet caller)
        {
            if (m_CurrentCabinetLayout != null)
            {
                m_CurrentCabinetLayout.UpdateMeshes();
                m_CurrentCabinetLayout.UpdatePositions();
            }

            await UniTask.CompletedTask;
        }

        protected async UniTask OnCabinetAddedByUser(Cabinet addedCabinet)
        {
            await UpdateCabinetHeightTransitions();
        }

        /// <summary>
        ///     Invoked whenever a cabinet's order within the layout is
        ///     changed. This particular callback is used to handle the cabinet
        ///     height transitions -- it ensures that smaller cabinets have an
        ///     appropriate bridging section between them and taller cabinets.
        /// </summary>
        protected async UniTask OnCabinetReordered((Cabinet reorderedCabinet, int previousCabinetIndex, List<CabinetSpan> previousCabinetSpans) args)
        {
            if (!m_IsUpdatingCabinetHeightTransitions)
            {
                m_IsUpdatingCabinetHeightTransitions = true;

                using var _ =
                    MelcherDeferred.Defer(
                        () => m_IsUpdatingCabinetHeightTransitions = false
                    );

                await UpdateCabinetHeightTransitions();
            }
        }

        protected async UniTask OnCabinetDestroyedByUser(int cabinetSpanIndex)
        {
            await UpdateCabinetHeightTransitions();
        }

        /// <summary>
        ///     Iterate through all kitchen cabinets, creating and removing
        ///     cabinet height transitions sections where needed.
        /// </summary>
        internal async UniTask UpdateCabinetHeightTransitions()
        {
            if(m_CurrentCabinetLayout == null)
            {
                return;
            }

            // First pass: Remove any unnecessary height transition cabinets.
            ComponentsInChildrenEnumerator<Cabinet> cabinets =
                new ComponentsInChildrenEnumerator<Cabinet>(
                    m_CurrentCabinetLayout.GetTransform(),
                    reverse: true
                );

            while (cabinets.MoveNext())
            {
                Cabinet cabinet = cabinets.Current;

                if (!cabinet.cabinetType.IsHeightTransition())
                {
                    // Not a height transition cabinet so skip it.
                    continue;
                }

                m_CurrentCabinetLayout.GetCabinetNeighbours(
                    cabinet,
                    out Cabinet leftCabinet,
                    out Cabinet rightCabinet
                );

                // The following big check excludes valid cabinet height
                // transitions. A valid transition needs two neighbours (its
                // left must not be a end because it is redundant), its
                // neighbours must have different heights, and the transition
                // must be facing the correct direction.

                if (leftCabinet &&
                    rightCabinet &&
                    !leftCabinet.cabinetType.IsEnd(includeTransitions: true) &&
                    !rightCabinet.cabinetType.IsEnd(includeTransitions: true) &&
                    !MelcherCommon.IsAlmostEqual(leftCabinet.sizeInMeters.y, rightCabinet.sizeInMeters.y, tolerance: 0.001f) &&
                    ((leftCabinet.sizeInMeters.y < rightCabinet.sizeInMeters.y) == (cabinet.cabinetType == CabinetType.LowLeftToHighRight)))
                {
                    // This height transition is necessary and is already
                    // facing the correct direction.
                    continue;
                }

                m_CurrentCabinetLayout.DestroyCabinet(cabinet);
            }

            // Second pass: Add any missing height transition cabinets. This
            // loop will add height transition cabinets to the right of the
            // inspected cabinet if it is necessary.

            (Material cabinetMaterial, Material countertopMaterial) =
                await this._materials.GetFinishMaterials(allowHighlightToReturn: true);

            foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (!cabinet.cabinetType.IsMidSpanCabinetType(selectedBuildingMethod) &&
                    cabinet.cabinetType != CabinetType.Corner)
                {
                    continue;
                }

                Cabinet rightNeighbor =
                    m_CurrentCabinetLayout.GetCabinetNeighbour(
                        cabinet,
                        Extent.Max
                    );

                if (!rightNeighbor ||
                    rightNeighbor.cabinetType.IsHeightTransition() ||
                    MelcherCommon.IsAlmostEqual(cabinet.sizeInMeters.y, rightNeighbor.sizeInMeters.y, tolerance: 0.001f))
                {
                    // No reason to add a height transition: no neighbor,
                    // already has a height transition, or the heights are
                    // the same.
                    continue;
                }

                CabinetType heightTransitionType =
                    cabinet.sizeInMeters.y < rightNeighbor.sizeInMeters.y ?
                        CabinetType.LowLeftToHighRight :
                        CabinetType.HighLeftToLowRight;

                Cabinet heightTransitionPrefab =
                    FindCabinetPrefab(heightTransitionType);

                Cabinet heightTransitionCabinet =
                    await layout.InstantiateCabinet(
                        heightTransitionPrefab,
                        cabinetSectionProductInfo: null,
                        kitchen: this
                    );

                if (cabinet.cabinetType == CabinetType.Corner)
                {
                    heightTransitionCabinet.normalizedAnchorToPreviousSection = Vector3.zero;
                }

                if (this.buildingRules.UsesBrandCatalog &&
                    selectedBrandCatalog &&
                    selectedBrandCatalog.cabinetSectionLegStyle)
                {
                    // Ensure height transitions are not on the ground in
                    // catalog building method.
                    heightTransitionCabinet.cabinet.transform.position +=
                        Vector3.up * selectedBrandCatalog.cabinetSectionLegStyle.heightInMeters;
                }

                heightTransitionCabinet.SetCountertopThickness(this._selections.countertopThicknessInMeters);
                heightTransitionCabinet.SetCountertopOverhang(this._selections.countertopOverhangInMeters);

                await layout.ReorderCabinet(heightTransitionCabinet, i.index + 1);

                Vector3 sizeInMeters = heightTransitionCabinet.sizeInMeters;
                sizeInMeters.x = Const.CabinetColumnWidthInMeters * 0.5f;
                sizeInMeters.z = this._selections.baseCabinetDepthInMeters;
                heightTransitionCabinet.sizeInMeters = sizeInMeters;

                await heightTransitionCabinet.SetCabinetMaterials(
                    cabinetMaterial,
                    countertopMaterial,
                    await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
                );
            }

            layout.UpdateMeshes();
            layout.UpdatePositions();

        }

        /// <summary>
        /// Called when a user clicks Add after selecting a product in the
        /// product toolbar.
        /// </summary>
        protected void TryAddProduct(ProductInfo productInfo, System.Action<Occupant> onComplete)
        {
            OnTryAddProduct(productInfo).Forget();

            async UniTaskVoid OnTryAddProduct(ProductInfo productInfo)
            {
                ProductVisualSO productVisual =
                    m_ProductVisualDatabase.FindRuntimeVisualByID(
                        productInfo.visualId
                    );

                if (!productVisual)
                {
                    MelcherLog.LogError($"No product visual found for product '{productInfo.name}' ({productInfo.id})", this);

                    onComplete?.Invoke(null);

                    return;
                }

                ProductVisualContent productVisualPrefab =
                    await ProFitGlobals.productContent
                        .LoadContent(productVisual);

                Occupant addedOccupant =
                    await TryInstantiateProductAnywhere(
                        productInfo,
                        productVisual,
                        productVisualPrefab,
                        showMessageIfFailed: true
                    );

                using var completeHandler =
                    MelcherDeferred.Defer(
                        toCapture: (addedOccupant, onComplete),
                        onDispose: static captures => captures.onComplete?.Invoke(captures.addedOccupant)
                    );

                if (addedOccupant)
                {
                    if (addedOccupant.cabinet != null)
                    {
                        RotateCameraToFrontOfCabinet(addedOccupant.cabinet);
                    }

                    if (this.buildingRules.UsesBrandCatalog &&
                        this._selections.brandCatalog != null &&
                        this._selections.brandCatalog.ruleset == BrandCatalogRuleset.WeatherStrong)
                    {
                        bool isHeatProduct =
                            addedOccupant.productVisual.info.runtimePrefabCategory == ProductCategorySpecific.Burner ||
                            addedOccupant.productVisual.info.runtimePrefabCategory == ProductCategorySpecific.Grill ||
                            // addedOccupant.productVisual.info.runtimePrefabCategory == ProductCategorySpecific.Smoker || // disabled insulated jacket popup for smokers
                            addedOccupant.productVisual.info.runtimePrefabCategory == ProductCategorySpecific.Cookware;

                        if (isHeatProduct && addedOccupant.productVisual.info.isCountertopOven == false)
                        {
                            EventBridge.Designer.showNotification?.Invoke(
                                title: "Heat Product",
                                message: "An insulated jacket is required for this product.",
                                onDismiss: null
                            );
                        }
                    }
                }
            }
        }

        internal void RotateCameraToFrontOfCabinet(ICabinet cabinet)
        {
            // When an occupant is added to a cabinet that the camera is
            // not facing, the camera is rotated to face it. (Client request.)

            Vector3 cabinetFrontToBack =
                cabinet.GetTransform().forward;

            Debug.DrawRay(cabinet.GetTransform().position, cabinet.GetTransform().forward, Color.red, 5.0f);

            Vector3 orbitForward =
                m_OrbitCamera.orbitRoot.forward;

            Debug.DrawRay(m_OrbitCamera.orbitRoot.position, m_OrbitCamera.orbitRoot.forward, Color.green, 5.0f);

            float dot = Vector3.Dot(cabinetFrontToBack, orbitForward);

            if (dot < -0.05f)
            {
                float angleToRotateCamera =
                    Vector3.SignedAngle(cabinetFrontToBack, orbitForward, Vector3.up);

                if (angleToRotateCamera > 0.0f)
                {
                    angleToRotateCamera -=
                        m_OrbitCamera.rotationIntervalInDegrees;
                }
                else
                {
                    angleToRotateCamera +=
                        m_OrbitCamera.rotationIntervalInDegrees;
                }

                m_OrbitCamera.AdjustRotation(-angleToRotateCamera);
            }
        }

        /// <summary>
        ///     Called when the user selects a cabinet section and presses
        ///     the clone button.
        /// </summary>
        protected void TryCloneSection(Cabinet cabinetToClone)
        {
            OnTryCloneSection(cabinetToClone).Forget();

            async UniTaskVoid OnTryCloneSection(Cabinet cabinetToClone)
            {
                Cabinet newCabinetTemplateOverride = null;

                if (this.buildingRules.ClonedSectionsCopyTheirCabinet)
                {
                    newCabinetTemplateOverride = cabinetToClone;
                }

                Cabinet newCabinet =
                    await TryAddSection(cabinetToClone.cabinetSectionProductInfo, prioritizedCabinetToReplace: null);

                if (!newCabinet)
                {
                    return;
                }

                newCabinet.sizeInMeters = cabinetToClone.sizeInMetersWithoutAdditive;

                await CloneOccupants(from: cabinetToClone, to: newCabinet);

                layout.UpdateMeshes();
                layout.UpdatePositions();

                await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(this._interaction.highlightObject);

                await EventBridge.Designer.onCabinetAddedByUser.InvokeIfNonEmpty(newCabinet);

                async UniTask CloneOccupants(OccupiableVolume from, OccupiableVolume to)
                {
                    foreach (Occupant occupantToClone in from.occupants)
                    {
                        Vector3 occupantLocalPosition =
                            occupantToClone.GetLocalPositionInVolume();

                        ProductVisualSO productVisual =
                            m_ProductVisualDatabase.FindRuntimeVisualByID(
                                occupantToClone.productInfo.visualId
                            );

                        if (!productVisual)
                        {
                            MelcherLog.LogError($"Did not find a product visual for visual ID: {occupantToClone.productInfo.visualId}");
                            continue;
                        }

                        ProductVisualContent productVisualPrefab =
                            await ProFitGlobals.productContent
                                .LoadContent(productVisual);

                        Occupant newOccupant =
                            await to.InstantiateAndInsertOccupantAt(
                                userInitiated: false,
                                occupantToClone.productInfo,
                                productVisual,
                                productVisualPrefab,
                                InstantiateOccupant,
                                occupantLocalPosition.x,
                                occupantLocalPosition.y,
                                occupantLocalPosition.z
                            );

                        if (occupantToClone.currentCutOutSizeInMeters != Vector3.zero)
                        {
                            newOccupant.SetCutOutSizeInMeters(occupantToClone.currentCutOutSizeInMeters);
                        }

                        if (occupantToClone.TryGetComponent(out CabinetShelf shelfToClone))
                        {
                            await CloneOccupants(from: shelfToClone, newOccupant.GetComponent<CabinetShelf>());
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Called when the player clicks the Add Section toolbar button.
        ///     This button is only visible when <see
        ///     cref="KitchenBuildingMethod.Sections"/> is selected. A section
        ///     is a cabinet section.
        /// </summary>
        protected void OnToolbarAddSectionClicked(ProductInfo cabinetSectionProductInfo, Cabinet prioritizedCabinetToReplace)
        {
            TryAddSection(cabinetSectionProductInfo, prioritizedCabinetToReplace).Forget();
        }

        protected async UniTask<Cabinet> TryAddSection(ProductInfo cabinetSectionProductInfo, Cabinet prioritizedCabinetToReplace)
        {
            // TODO: Break the spawning logic out to its own function. This is
            //       currently invoking `onCabinetAddedByUser` twice (from
            //       `TryCloneSection`) and not updating the ground after a
            //       cabinet is cloned has been resized. -Ryan

            Cabinet cabinetPrefab = m_StraightCabinetPrefab;

            if (cabinetSectionProductInfo != null)
            {
                // A cabinet section with a custom model is being requested.

                MelcherLog.Log("Trying to add section with product info: " + cabinetSectionProductInfo.name);

                ProductVisualSO cabinetSectionVisual =
                    productVisualDatabase.FindRuntimeVisualByID(
                        cabinetSectionProductInfo.visualId
                    );

                if (!cabinetSectionVisual)
                {
                    MelcherLog.LogError($"No product visual found for a cabinet section named '{cabinetSectionProductInfo.name}' and using visual ID, {cabinetSectionProductInfo.visualId}");
                    return null;
                }

                ProductVisualContent cabinetSectionVisualContent =
                    await ProFitGlobals.productContent
                        .LoadContent(cabinetSectionVisual);

                if (cabinetSectionVisualContent.sectionModelPrefab)
                {
                    cabinetPrefab =
                        FindCabinetPrefab(
                            cabinetSectionVisual.info.sectionCabinetType,
                            CabinetModelType.Authored
                        );
                }
            }

            // Create a section that matches the current measurement options
            // applied to the kitchen.
            // TODO: What happens if the neighbour has a backing? -Ryan

            // The number of cabinets in the kitchen is limited such that
            // no span can be larger than 20'.

            int? insertIndex =
                layout.FindInsertIndexForCabinet(
                    cabinetPrefab,
                    cabinetSectionProductInfo,
                    prioritizedCabinetToReplace: prioritizedCabinetToReplace
                );

            if (!insertIndex.HasValue)
            {
                // Nowhere was found to insert a cabinet.

                if (this.buildingRules.BackingSpansTakeOnlyStandardHeightCabinets &&
                    cabinetSectionProductInfo != null &&
                    !MelcherCommon.IsAlmostEqual(cabinetSectionProductInfo.productSizeInMeters.y, 0.0f, tolerance: 0.001f) &&
                    !MelcherCommon.IsAlmostEqual(cabinetSectionProductInfo.productSizeInMeters.y, 36.0f * Units.Inches2Meters, tolerance: 0.001f) &&
                    layout.spans.Any(static span => span.backing))
                {

                    // The most likely issue is that the backing(s) are
                    // preventing the cabinet from being added.

                    EventBridge.Designer.showNotification?.Invoke(
                        title: "No Suitable Location",
                        message: "This cabinet cannot be added to a section " +
                            "that has a cabinet backing.",
                        onDismiss: null
                    );
                }
                else if (cabinetPrefab.cabinetType == CabinetType.Straight)
                {
                    EventBridge.Designer.showNotification?.Invoke(
                        title: "Max Kitchen Size Reached",
                        message: "The kitchen is at its maximum size. No " +
                            "further cabinet sections may be added.",
                        onDismiss: null
                    );
                }
                else
                {
                    EventBridge.Designer.showNotification?.Invoke(
                        title: "No Suitable Location",
                        message: "There is no suitable location for this " +
                            "cabinet to be added.",
                        onDismiss: null
                    );
                }

                return null;
            }


            Vector3 sectionSizeInMeters = cabinetPrefab.minSizeInMeters;

            sectionSizeInMeters.y = cabinetPrefab.initialSizeInMeters.y;
            sectionSizeInMeters.z = this._selections.baseCabinetDepthInMeters;

            if (cabinetSectionProductInfo != null)
            {
                // In Gemini builds, cabinets size is dictated by the cabinet
                // product info. Cabinets can have differing depths.

                sectionSizeInMeters.x =
                    cabinetSectionProductInfo.productSizeInMeters.x;

                sectionSizeInMeters.z =
                    cabinetSectionProductInfo.productSizeInMeters.z;
            }

            // Adding a corner swaps out the existing corner. Keep a reference
            // to the cabinet at the insert location before adding a new one
            // to limit the number of times the cabinet spans need to be
            // rebuilt.
            Cabinet currentCabinetAtIndex =
                m_CurrentCabinetLayout.GetCabinetAtIndex(insertIndex.Value);

            Quaternion currentCabientAtIndexRotation =
                currentCabinetAtIndex.GetTransform().rotation;

            Cabinet createdCabinet =
                await m_CurrentCabinetLayout.UserInstantiateCabinetAtIndex(
                    insertIndex.Value,
                    cabinetPrefab,
                    cabinetSectionProductInfo,
                    kitchen: this,
                    sectionSizeInMeters,
                    applySpanBackings: false,
                    newCabinet =>
                    {
                        // Allow new corner cabinets to replace old corner cabinets, even
                        // if a specific cabinet wasn't indicated to be replaced.
                        // This also replaces non-corners if the user went
                        // through the swap cabinet menu.
                        MelcherLog.Log($"prioritizedCabinetToReplace={(prioritizedCabinetToReplace != null ? prioritizedCabinetToReplace.gameObject : null)}", prioritizedCabinetToReplace);
                        MelcherLog.Log($"currentCabinetAtIndex={(currentCabinetAtIndex != null ? currentCabinetAtIndex.gameObject : null)}", currentCabinetAtIndex);
                        if (cabinetPrefab.cabinetType == CabinetType.Corner ||
                            currentCabinetAtIndex == prioritizedCabinetToReplace)
                        {
                            newCabinet.GetTransform().rotation =
                                currentCabientAtIndexRotation;

                            newCabinet.normalizedAnchorToPreviousSection =
                                currentCabinetAtIndex.normalizedAnchorToPreviousSection;

                            // Destroy the original corner.
                            m_CurrentCabinetLayout.DestroyCabinet(currentCabinetAtIndex);
                        }
                    }
                );

            {
                // This is done to ensure cabinets' intended rotation is
                // properly before rebuilding the cabinet spans.

                layout.UpdatePositions();
                layout.MarkSpansForRebuild();
                // Force rebuild spans.
                var _ = layout.spans;
                // It is now safe to apply any backings to the cabinet because
                // we have removed any temporary extraneous corner cabinet
                // (which otherwise interfere with the spans rebuild).
                await layout.ApplyCabinetBackingsToCabinet(createdCabinet);
                layout.UpdateMeshes();
            }

            createdCabinet.SetToeKickType(this._selections.toeKickType);

            (ResizerWidget left, ButtonWidget middle, ResizerWidget right) =
                this._widgets.CreateResizeWidgetsForCabinet(createdCabinet, Camera.main);

            left.SetVisible(false);
            middle.SetVisible(false);
            right.SetVisible(false);

            CenterTheGround();

            await EventBridge.Designer.onCabinetAddedByUser.InvokeIfNonEmpty(createdCabinet);

            return createdCabinet;
        }

        internal Camera GetCamera()
        {
            // TODO: Add a better way to get the camera
            //MelcherLog.LogWarning("TODO: Make GetCamera() better");
            return Camera.main;
        }

        private async UniTask OnOccupantInstantiated((Occupant newOccupant, bool userInitiated) args)
        {
            if (args.userInitiated && !await TryToFitCabinetToOccupant(args.newOccupant))
            {
                Cabinet cabinet = (Cabinet)args.newOccupant.cabinet;

                Cuboid occupiedCuboid =
                    cabinet.CalculateOccupancyCuboidForOccupant(args.newOccupant);

                float requiredWidthInMeters = occupiedCuboid.width;

                if (await cabinet.TryRemoveOccupant(args.newOccupant))
                {
                    Destroy(args.newOccupant.gameObject);
                }

                string requiredSize =
                    Utility.MeasurementFormat.FormatMetersToImperial(
                        requiredWidthInMeters,
                        showFeet: false,
                        showFraction: true
                    );

                if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
                {
                    requiredSize =
                        Utility.MeasurementFormat.FormatMetersToCentimeters(requiredWidthInMeters);
                }

                EventBridge.Designer.showNotification.Invoke(
                    title: "Product Doesn't Fit",
                    message: "This product is too wide for the existing " +
                        "cabinets. Please resize or add a wider cabinet " +
                        $"that is at least {requiredSize} wide to place " +
                        $"this product.",
                    onDismiss: null
                );
            }
        }

        private async UniTask OnOccupantAddedToCabinet(Occupant occupant)
        {
            if (!this.buildingRules.LimitsCabinetSizeToPreventBowing)
            {
                return;
            }

            if (!ReferenceEquals(occupant.occupiedVolume, occupant.cabinet))
            {
                // The occupant was added into a shelf. We can ignore this
                // because the shelf will handle the size.
                // parent cabinet.
                return;
            }

            Cabinet cabinet = (Cabinet)occupant.cabinet;

            if (cabinet.cabinetType == CabinetType.Corner)
            {
                // No resizing happens for Stoll's corner cabinets (e.g.,
                // 45-degree corners).
                return;
            }

            if (occupant.TryGetComponent(out CabinetShelf cabinetShelf) &&
                cabinetShelf.occupants.Count <= 0)
            {
                // Do not resize the cabinet to an empty shelf. This allows the
                // user to resize the shelf to add a burner more easily.
                return;
            }

            // In the real world, Stoll warns that products that are wider
            // than 15" may cause bowing from frequent opening and closing.
            // Therefore, cabinets are not allowed to be longer than 8"
            // on either side of 15" or wider product.

            Cuboid occupiedCuboid =
                occupant.occupiedVolume.CalculateOccupancyCuboidForOccupant(occupant);

            if (occupiedCuboid.width >= Const.Stoll.MinimumOccupantWidthInMetersCausingBowing)
            {
                // This product is wide enough to causing bowing. Limit
                // the cabinet's width.

                Cuboid occupiableCuboid = cabinet.occupiableCuboid;

                float maximumWidthInMeters =
                    occupiedCuboid.width - Const.OccupantMinimumHorizontalPaddingInMeters + (2.0f * Const.Stoll.MaximumUnoccupiedPaddingInMeters);

                if (cabinet.resizerCuboid.width >= maximumWidthInMeters)
                {
                    // This cabinet is wider than the recommended safe
                    // width therefore we need to resize it.

                    // The new occupant is moved as far left as possible.
                    // Then the cabinet is sized down (it might not be able
                    // to shrink as far as we like because of other
                    // products). Afterwards, the occupant will be centered
                    // in whatever size the cabinet is left with.

                    occupant.metersFromVolumeMinX = 0.0f;

                    float widthDeltaInMeters =
                        maximumWidthInMeters - occupant.cabinet.resizerCuboid.width;

                    bool didResize =
                        cabinet.TryToResizeLength(
                            ref widthDeltaInMeters,
                            Extent.Max,
                            forceAutoCenterProducts: false
                        );

                    if (didResize)
                    {
                        layout.UpdateMeshes();
                        layout.UpdatePositions();

                        await EventBridge.Designer.onCabinetResized.Invoke(cabinet);

                        // Center all other occupants.
                        foreach (Occupant otherOccupant in cabinet.occupants)
                        {
                            if (otherOccupant == occupant)
                            {
                                continue;
                            }

                            Cuboid otherOccupantCuboid =
                                cabinet.CalculateOccupancyCuboidForOccupant(otherOccupant);

                            otherOccupant.metersFromVolumeMinX =
                                (cabinet.resizerCuboid.width - otherOccupantCuboid.width) * 0.5f;
                        }
                    }

                    occupant.metersFromVolumeMinX =
                        (cabinet.occupiableCuboid.width - occupiedCuboid.width) * 0.5f + cabinet.occupiablePaddingInMeters.left;
                }

                Vector3 maxCabinetSizeInMeters = cabinet.maxSizeInMeters;
                maxCabinetSizeInMeters.x = maximumWidthInMeters;
                cabinet.maxSizeInMeters = maxCabinetSizeInMeters;
            }
        }

        private async UniTask OnOccupantRemovedFromCabinet(Occupant occupant)
        {
            if (this.buildingRules.LimitsCabinetSizeToPreventBowing &&
                ReferenceEquals(occupant.occupiedVolume, occupant.cabinet))
            {
                // Recover Stoll cabinet's original maximum size. The max size
                // might have been reduced to prevent bowing if a large product
                // was added.

                Cabinet cabinet = (Cabinet)occupant.cabinet;

                // Check if any large occupants still exist.
                foreach (Occupant otherOccupant in cabinet.occupants)
                {
                    if (otherOccupant == occupant)
                    {
                        // Skip the occupant being removed.
                        continue;
                    }

                    Cuboid occupiedCuboid =
                        occupant.occupiedVolume.CalculateOccupancyCuboidForOccupant(occupant);

                    if (occupiedCuboid.width >= Const.Stoll.MinimumOccupantWidthInMetersCausingBowing)
                    {
                        // A large enough occupant still exists to cause
                        // bowing. Therefore, do not reset the cabinet's max
                        // size to its default.
                        return;
                    }
                }

                Vector3 maxSizeInMeters = cabinet.maxSizeInMeters;
                maxSizeInMeters.x = Const.SectionBuildingMethodMaxCabinetLengthInMeters;
                cabinet.maxSizeInMeters = maxSizeInMeters;
            }

            await UniTask.CompletedTask;
        }

        protected void TrySendOnBrandCatalogChanged()
        {
            if(ProFitGlobals.appRules.RequiresBrandCatalog && this._selections.brandCatalog == null)
            {
                return;
            }

            EventBridge.Designer.onBrandCatalogChanged?.Invoke(this._selections.brandCatalog);
        }

        // Used to pass values "by ref" to async functions.

        /// <summary>
        ///     In some building methods (Sections and Catalog), some products
        ///     are able to resize cabinets when the products are added to the
        ///     cabinet. E.g., fridges do this.
        /// </summary>
        /// <param name="occupant"></param>
        internal async UniTask<bool> TryToFitCabinetToOccupant(Occupant occupant)
        {
            if (!occupant.productVisual.info.fitSectionToProductWidth ||
                !ReferenceEquals(occupant.occupiedVolume, occupant.cabinet))    // Dropped in a shelf. I.e., not directly on a cabinet.
            {
                return true;
            }

            Cabinet cabinetToResize = (Cabinet)occupant.cabinet;

            bool result = cabinetToResize.FitToProductWidth(specificOccupant: occupant);

            await EventBridge.Designer.onCabinetResized.InvokeIfNonEmpty(cabinetToResize);

            return result;
        }


        /// <inheritdoc cref="EventBridge.Designer.onTryRepositionOccupant" />
        protected void TryRepositionProduct(
            Occupant occupantToMove,
            Vector3 newVolumePosition,
            EventBridge.Designer.RepositionSnapMode snapMode,
            ref bool didMove)
        {
            OccupiableVolume occupiableVolume = occupantToMove.occupiedVolume;

            MelcherLog.Breadcrumb($"TryRepositionProduct");

            CabinetSpan span =
                layout.FindCabinetSpan((Cabinet)occupantToMove.cabinet);

            Cuboid? backingOccupancyInWorldSpace = null;

            if (span.backing)
            {
                // TODO: This was never hooked up to assign to
                //       `backingOccupanyInWorldSpace` as appears to be
                //       intended. Not changing it now in case it breaks
                //       something. -Ryan
                span.GetBackingOccupancyCuboidInWorldSpace(
                    span.backing.info,
                    span.backing
                );
            }

            // Try moving the product to the requested position
            didMove =
                occupiableVolume.TryMoveOccupantTo(
                    occupantToMove,
                    backingOccupancyInWorldSpace,
                    newVolumePosition.x,
                    newVolumePosition.y,
                    newVolumePosition.z
                );

            MelcherLog.Breadcrumb($"TryRepositionProduct returned " + didMove);
        }

        /// <summary>
        ///     This reorders the cabinet by moving it either closer to the
        ///     start of the cabinet (<see cref="Extent.Min"/>) or the end of
        ///     the cabinet (<see cref="Extent.Max"/>). The cabinet to-be-moved
        ///     must have at least one other straight cabinet within its span
        ///     to avoid a span with no usable space.
        /// </summary>
        /// <param name="cabinetToReorder"></param>
        /// <param name="moveTowardsExtent"></param>
        protected async UniTask TryReorderCabinet((Cabinet cabinetToReorder, Extent moveTowardsExtent) args)
        {
            if (!args.cabinetToReorder)
            {
                throw new System.ArgumentNullException(nameof(args.cabinetToReorder));
            }

            if (args.cabinetToReorder.cabinetType == CabinetType.Corner)
            {
                // TODO: We need find an available procedural corner and swap
                //       places with it. -Ryan
                return;
            }

            MelcherLog.Assert(
                this.buildingRules.CabinetsAreProducts,
                $"Cabinet reordering is assumed to not work in {this._selections.buildingMethod} building method."
            );

            if (args.cabinetToReorder.cabinetType.IsMidSpanCabinetType(selectedBuildingMethod) &&
                !layout.CabinetHasMidSpanCabinetNeighbour(args.cabinetToReorder))
            {
                EventBridge.Designer.showToast?.Invoke("This cabinet is not movable.");
                return;
            }

            int? insertCabinetAtIndex =
                layout.FindInsertIndexForCabinet(
                    cabinetToInsert: args.cabinetToReorder,
                    cabinetSectionProductInfo: args.cabinetToReorder.cabinetSectionProductInfo,
                    startIndex: args.cabinetToReorder.GetTransform().GetSiblingIndex(),
                    reverse: args.moveTowardsExtent == Extent.Min
                );

            CabinetType cabinetType = args.cabinetToReorder.cabinetType;

            if (!insertCabinetAtIndex.HasValue)
            {
                // Nowhere valid to move the cabinet.

                string toast =
                    "Cannot move the cabinet without making the kitchen too long.";

                if (cabinetType == CabinetType.StraightOnLeftOfCorner ||
                    cabinetType == CabinetType.StraightOnRightOfCorner ||
                    cabinetType == CabinetType.Corner)
                {
                    toast = "No valid corner found.";
                }

                EventBridge.Designer.showToast?.Invoke(toast);
                return;
            }

            // Copy the backing sprite of the cabinet we are moving next to.
            args.cabinetToReorder.backingSprite =
                layout.GetCabinetAtIndex(insertCabinetAtIndex.Value).backingSprite;

            await layout.ReorderCabinet(args.cabinetToReorder, insertCabinetAtIndex.Value);

            Cabinet previousCabinet = null;

            // Fix up the anchoring of the straight sections. They need an
            // anchor of (0,0,0) when trailing an end or corner.
            foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (previousCabinet && cabinet.cabinetType == CabinetType.Straight)
                {
                    cabinet.normalizedAnchorToPreviousSection =
                        previousCabinet.cabinetType == CabinetType.Corner ?
                            new Vector3(0.0f, 0.0f, 0.0f) :
                            new Vector3(1.0f, 0.0f, 0.0f);
                }

                previousCabinet = cabinet;
            }

            m_CurrentCabinetLayout.UpdateMeshes();
            m_CurrentCabinetLayout.UpdatePositions();
        }

        /// <summary>
        ///     Use the next toe kick type, wrapping around when the end is
        ///     reached. If this is not <see cref="KitchenBuildingMethod.Catalog"/>,
        ///     the method returns without doing anything.
        /// </summary>
        protected void TryToggleToeKicks()
        {
            MelcherLog.Breadcrumb($"Building method: {this._selections.buildingMethod}");
            if (!this.buildingRules.CanToggleToeKicks)
            {
                return;
            }

            //// Use the next toe kick type, wrapping around when the end is
            //// reached.

            //ReadOnlyCollection<CabinetToeKickType> toeKickTypes =
            //    EnumInfo<CabinetToeKickType>.values;

            //if (toeKickTypes.Count <= 0)
            //{
            //    MelcherLog.LogError($"No toe kick types defined in {nameof(CabinetToeKickType)}.", this);
            //    return;
            //}

            //int currentToeKickTypeIndex =
            //    MelcherCommon.IndexOf(toeKickTypes, this._selections.toeKickType);

            //int newToeKickTypeIndex =
            //    (currentToeKickTypeIndex + 1) % toeKickTypes.Count;

            //this._selections.toeKickType = toeKickTypes[newToeKickTypeIndex];

            this._selections.toeKickType =
                this._selections.toeKickType == CabinetToeKickType.None ?
                    CabinetToeKickType.Inset :
                    CabinetToeKickType.None;

            EventBridge.Designer.onToeKickTypeChanged?.Invoke(this._selections.toeKickType);
        }

        protected void OnToeKickTypeChanged(CabinetToeKickType toeKickType)
        {
            foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
            {
                i.element.SetToeKickType(toeKickType);
            }
        }

        protected void OnCenterOccupantInVolumeClicked(Occupant occupant)
        {
            bool didCenterOccupant = TryCenterOccupantInVolume(occupant);

            if (!didCenterOccupant)
            {
                EventBridge.Designer.showToast?.Invoke("Product could not be centered.");
            }
        }

        protected bool TryCenterOccupantInVolume(Occupant occupant)
        {
            Cuboid? backingOccupancyInWorldSpace = null;

            foreach (CabinetSpan span in m_CurrentCabinetLayout.spans)
            {
                if (span.backing &&
                    span.Contains((Cabinet)occupant.cabinet))
                {
                    backingOccupancyInWorldSpace =
                        span.GetBackingOccupancyCuboidInWorldSpace(
                            span.backing.info,
                            span.backing
                        );
                }
            }

            Cuboid occupiedCuboid =
                occupant.occupiedVolume.CalculateOccupancyCuboidForOccupant(occupant);

            // Calculate the position needed to center the product
            float metersFromVolumeMinX = occupant.metersFromVolumeMinX;

            if (m_MeasurementOverlayManager.measureMode == MeasureMode.ToCabinetEdge)
            {
                // Center within the overall volume.

                Cuboid occupiableCuboid =
                    occupant.occupiedVolume.occupiableCuboid;

                metersFromVolumeMinX =
                    (occupiableCuboid.xMin + occupiableCuboid.xMax) * 0.5f;

                metersFromVolumeMinX -= occupiedCuboid.width * 0.5f;
            }
            else if (m_MeasurementOverlayManager.measureMode == MeasureMode.ToNearestOccupant)
            {
                // Find the distance to the closest objects.

                Occupant leftNeighbor =
                    occupant.occupiedVolume.FindNearestOccupantInDirection(
                        occupant,
                        CabinetLocalDirection.Left,
                        out float metersToLeftNeighbor
                    );

                occupant.occupiedVolume.FindNearestOccupantInDirection(
                    occupant,
                    CabinetLocalDirection.Right,
                    out float metersToRightNeighbor
                );

                metersFromVolumeMinX += (-metersToLeftNeighbor + metersToRightNeighbor) * 0.5f;
            }

            bool didCenterOccupant =
                occupant.occupiedVolume.TryMoveOccupantTo(
                    occupant,
                    backingOccupancyInWorldSpace,
                    metersFromVolumeMinX
                );

            if (didCenterOccupant)
            {
                // Invoke the nudge event to cause the measurement overlay to
                // refresh.
                EventBridge.Designer.onOccupantNudged?.Invoke(occupant, Vector3.zero);
            }

            return didCenterOccupant;
        }

        public UniTask SetTemporaryFinishMaterials(Material cabinetMaterial, Material countertopMaterial, Material cutOutMaterial)
        {
            return this._materials.SetTemporaryFinishMaterials(cabinetMaterial, countertopMaterial, cutOutMaterial);
        }

        public UniTask<(Material cabinetMaterial, Material countertopMaterial)> GetFinishMaterials(bool allowHighlightToReturn)
        {
            return this._materials.GetFinishMaterials(allowHighlightToReturn);
        }

        public UniTask<Material> GetSelectedOrDefaultFinishMaterial(ProductInfo selectedFinish)
        {
            return this._materials.GetSelectedOrDefaultFinishMaterial(selectedFinish);
        }

        internal UniTask ChangeFinish(ProductInfo finishProduct, ProductCategory fallbackFinishCategory)
        {
            return this._materials.ChangeFinish(finishProduct, fallbackFinishCategory);
        }

        internal void RecreateResizeWidgets()
        {
            this._widgets.RecreateResizeWidgets();
        }

        /// <summary>
        ///     Setup the kitchen camera target group so it tracks/frames all of
        ///     the (persistent) kitchen's cabinets. If the kitchen doesn't have
        ///     a cabinet layout, the cameras are updated to look at a default
        ///     target group.
        ///     <br /><br />
        ///
        ///     A "persistent" cabinet is one that is a corner or end. They are
        ///     persistent in the sense that the player cannot remove these
        ///     during normal use, unlike a straight cabinet which the player
        ///     can remove when using the Sections building method.
        /// </summary>
        internal void UpdateCameraAndFramingGroups()
        {
            // Look at the empty group until the target group is setup
            m_OrbitCamera.virtualCamera.LookAt = m_EmptyCameraTargetGroup.transform;
            m_PhotoCoverView.LookAt = m_EmptyCameraTargetGroup.transform;

            if (m_CurrentCabinetLayout && m_CurrentCabinetLayout.GetTransform().childCount > 0)
            {
                // Setup the camera target group to compose all of the cabinets
                // into one view
                Transform layoutTransform = m_CurrentCabinetLayout.GetTransform();

                int childCount = layoutTransform.childCount;
                List<Transform> transformsToFrame = new List<Transform>(capacity: childCount);

                for (int i = 0; i < childCount; ++i)
                {
                    Transform child = layoutTransform.GetChild(i);

                    Cabinet cabinet = child.GetComponent<Cabinet>();

                    if (cabinet.isCornerOrEnd)
                    {
                        transformsToFrame.Add(child);
                    }
                }

                m_KitchenCameraTargetGroup.Targets.Clear();

                for (int i = 0; i < transformsToFrame.Count; ++i)
                {
                    m_KitchenCameraTargetGroup.Targets.Add(
                        new CinemachineTargetGroup.Target
                        {
                            Object = transformsToFrame[i],
                            Weight = 1.0f,
                            Radius = 2.0f
                        }
                    );
                }

                m_OrbitCamera.virtualCamera.LookAt = m_KitchenCameraTargetGroup.transform;
                m_PhotoCoverView.LookAt = m_KitchenCameraTargetGroup.transform;
            }
            else
            {
                // Clear the old cabinet references
                m_KitchenCameraTargetGroup.Targets.Clear();
            }
        }

        protected bool GetCenterOfTargetGroup(out Vector3 targetWorldCenter)
        {
            targetWorldCenter = Vector3.zero;

            // Calculate the center point of all targets
            bool foundPoint = false;
            Bounds bounds = new Bounds();
            foreach (CinemachineTargetGroup.Target target in m_KitchenCameraTargetGroup.Targets)
            {
                if (target == null || !target.Object)
                {
                    continue;
                }

                if (!foundPoint)
                {
                    foundPoint = true;
                    bounds = new Bounds(target.Object.position, size: Vector3.zero);
                }
                else
                {
                    bounds.Encapsulate(target.Object.position);
                }
            }

            if (foundPoint)
            {
                // Move the camera orbit to its center point
                targetWorldCenter = bounds.center;
                targetWorldCenter.y = 0.0f;
            }

            return foundPoint;
        }

        /// <summary>
        ///     Move the ground mesh to the center of the kitchen. This is
        ///     intended to be called when the kitchen is first created and
        ///     immediately before the ground is ever made visible.
        /// </summary>
        /// <remarks>
        ///     The ground is made invisible while the kitchen is being
        ///     resized as otherwise it would be possible to "walk" the
        ///     kitchen off of the ground via series of kitchen resizes OR the
        ///     ground would have to visibly slide to the center of the kitchen
        ///     as it is resized. It looks much cleaner to avoid both cases
        ///     by hiding the ground until the player has confirmed the
        ///     kitchen's size.
        /// </remarks>
        public void CenterTheGround()
        {
            if (GetCenterOfTargetGroup(out Vector3 targetWorldCenter))
            {
                m_Ground.position = targetWorldCenter;
            }
        }

        // ====================================================================
        // Static functions
        // ====================================================================

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected MelcherLocalizedText m_TopInfoLabel = null;
        [SerializeField] protected Transform m_TickerDimensionsRoot = null;
        [SerializeField, MelcherAssetField]
        protected CabinetTickerWidget m_TickerDimensionsPrefab = null;

        [Space]

        [SerializeField, MelcherAssetField]
        [Tooltip("The prefab that is instantiated that can be clicked to " +
            "edit the cabinet's size using a textbox.")]
        protected ButtonWidget m_InputCabinetSizeWidget = default;

        [SerializeField, MelcherAssetField]
        [Tooltip("The prefab that is instantiated for resizing cabinets and " +
            "cabinet shelves.")]
        protected ResizerWidget m_ResizeCabinetWidgetPrefab = default;

        [SerializeField]
        [Tooltip("The transform root where cabinet resizer widgets will be " +
            "childed.")]
        protected Transform m_CabinetResizeWidgetRoot = default;

        [SerializeField]
        [Tooltip("The transform root where cabinet shelf resizer widgets " +
            "will be childed.")]
        protected Transform m_CabinetShelfResizeWidgetRoot = default;

        [Space]

        [UnityEngine.Serialization.FormerlySerializedAs("m_ProductVisualCatalog")]
        [SerializeField, MelcherAssetField]
        protected ProductVisualDatabaseSO m_ProductVisualDatabase = null;

        [Space]

        [SerializeField] protected AnimationCurve m_MainCameraFovToShadowDistance = null;

        [Tooltip("Top down, orthographic view of the kitchen.")]
        [SerializeField] protected CinemachineCamera m_PlanView = null;

        [Tooltip("Camera used to screenshot the cover photo of the quote's PDF.")]
        [SerializeField] protected CinemachineCamera m_PhotoCoverView = null;

        [Tooltip("An empty target group for the camera to focus when the " +
            "kitchen doesn't have a cabinet layout assigned. This used " +
            "because TransformTargetGroup cannot be updated while they're " +
            "in-use and so we need one to swap when setting up a new group.")]
        [SerializeField] protected Transform m_EmptyCameraTargetGroup = null;

        [SerializeField] protected CinemachineTargetGroup m_KitchenCameraTargetGroup = null;

        [Space]

        [SerializeField] protected PhysicsRaycaster m_CameraPhysicsRaycaster = null;
        [SerializeField, MelcherLayerField] protected LayerMask m_ProductPointerInteractionMask;
        [SerializeField] protected Transform m_Ground = null;
        [SerializeField] protected MeshRenderer m_GroundWood = null;
        [SerializeField] protected MeshRenderer m_GroundStone = null;

        [Space]

        [SerializeField]
        [Tooltip("A prefab of a transparent cube that auto-sizes to match " +
            "the size of its associated occupant. Used for debug purposes " +
            "to visualize the cuboid occupied by a product.")]
        protected OccupantOccupiedCuboidVisualization m_ProductCuboidVisualizerPrefab = null;

        [Space]

        [Tooltip("This is a cabinet prefab that is instantiated and added " +
            "to the kitchen when the user, in \"sections building method\", " +
            "clicks the Cabinet Section button.")]
        [SerializeField, MelcherAssetField] protected Cabinet m_StraightCabinetPrefab = default;

        [Tooltip("This is a cabinet prefab that is instantiated and added " +
            "to the kitchen when the user, in \"catalog building method\", " +
            "clicks the Cabinet Section button.")]
        [SerializeField, MelcherAssetField] protected Cabinet m_CatalogCabinetPrefab = default;

        [SerializeField, MelcherAssetField] protected CabinetBacking m_BacksplashPrefab = default;
        [UnityEngine.Serialization.FormerlySerializedAs("m_BarTopPrefab")]
        [SerializeField, MelcherAssetField] protected CabinetBacking m_HighBarTopPrefab = default;
        [SerializeField, MelcherAssetField] protected CabinetBacking m_LowBarTopPrefab = default;

        [Space]

        // TODO: Move elsewhere. Added during crunch.
        [SerializeField] protected CabinetLayout[] m_LayoutPrefabs = default;

        [Tooltip("Used to reconstruct a kitchen saved in sections mode.")]
        [SerializeField] protected Cabinet[] m_CabinetPrefabs = default;

        [Tooltip("Used to reassign sprites to cabinets that are instantiated " +
            "during a load of a design saved using Sections building method.")]
        [SerializeField] protected Sprite[] m_CabinetBackingSprites = default;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        private Transform m_Transform = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected DesignerOrbitCamera m_OrbitCamera = default;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected MeasurementOverlayManager m_MeasurementOverlayManager = default;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================


        private readonly KitchenCutoutClipping m_CutoutClipping = new KitchenCutoutClipping();

        private CabinetLayout m_CurrentCabinetLayout;


        // Player's kitchen customizations:














        /// <summary>
        ///     A kludge that ensures the OnCabinetReordered callback that is
        ///     used to setup the cabinet height transition cabinets doesn't
        ///     recurse when new cabinets are added and reordered mid event.
        /// </summary>
        private readonly KitchenSelections _selections = new KitchenSelections();
        private KitchenSaveWriter _saveWriter;
        private BuildingMethodRules _buildingRules;
        private KitchenSaveReader _saveReader;
        private KitchenInteractionState _interaction;
        private KitchenWidgetsControl _widgets;
        private KitchenMaterialControl _materials;
        private OccupantDragControl _drag;

        private bool m_IsUpdatingCabinetHeightTransitions;


        // =====================================================================
        // Static fields
        // =====================================================================

        protected static Kitchen s_KitchenInstance = default;

        // ====================================================================
        // Nested types
        // ====================================================================

    }   // class Kitchen

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
