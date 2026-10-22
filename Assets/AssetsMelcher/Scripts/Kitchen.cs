namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using Unity.Cinemachine;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.EventSystems;

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
            get => m_SessionStartTime;
            set => m_SessionStartTime = value;
        }

        public KitchenBuildingMethod selectedBuildingMethod => m_SelectedBuildingMethod;

        public BrandCatalog selectedBrandCatalog => m_SelectedBrandCatalog;

        public CountertopType selectedCountertopType => m_SelectedCountertopType;

        public CabinetToeKickType selectedToeKickType => m_SelectedToeKickType;

        public CabinetLayout layout => m_CurrentCabinetLayout;

        //public IReadOnlyList<CabinetSection> cabinetSections => m_CabinetSections;

        public KitchenCutoutClipping cutoutClipping => m_CutoutClipping;

        public float baseCabinetDepthInMeters => m_SelectedBaseCabinetDepthInMeters;


        /// <summary>
        /// The height of a cabinet from the floor to the top of the countertop.
        /// This value is currently hard coded because it is unnecessary to
        /// change the height.
        /// </summary>
        public float cabinetHeightInMeters => 36.0f * Units.Inches2Meters;

        public float countertopThicknessInMeters => m_SelectedCountertopThicknessInMeters;

        public CuboidPadding baseCountertopOverhangInMeters => m_SelectedCountertopOverhangInMeters;


        public bool doCabinetsHaveColumns => m_SelectedBuildingMethod == KitchenBuildingMethod.Sections;

        // TODO: Do something better than expose this directly. It is exposed
        //       so the ground can be temporarily hidden during screenshots
        public GameObject ground => m_Ground.gameObject;


        public string selectedDoorStyleName
        {
            get => m_SelectedDoorStyleName;

            // TODO: Remove the setter and add an event to EventBridge to
            //       handle changing the door style.
            set
            {
                // Temporarily disabled, client request.

                // if (value == "Shaker" && m_SelectedDoorStyleName != value)
                // {
                //     EventBridge.Designer.showNotification?.Invoke(
                //         title: "Shaker Doors",
                //         message: "Please note that the Shaker style adds a 5% premium to your cabinet choices.",
                //         onDismiss: null
                //     );
                // }

                m_SelectedDoorStyleName = value;

            }
        }

        public string selectedDoorHandleStyleName
        {
            get => m_SelectedDoorHandleStyleName;
            set
            {
                m_SelectedDoorHandleStyleName = value;
            }
        }

        public float endPanelWidthInMetersLeft
        {
            get => m_EndPanelWidthInMetersLeft;
            set => m_EndPanelWidthInMetersLeft = m_EndPanelWidthInMetersLeft == value ? 0f : value;
        }

        public float endPanelWidthInMetersRight
        {
            get => m_EndPanelWidthInMetersRight;
            set => m_EndPanelWidthInMetersRight = m_EndPanelWidthInMetersRight == value ? 0f : value;
        }

        public float backPanelDepthInMeters
        {
            get => m_BackPanelDepthInMeters;
            set => m_BackPanelDepthInMeters = m_BackPanelDepthInMeters == value ? 0f : value;
        }

        public void UpdateLayoutCabinetMeshes()
        {
            m_CurrentCabinetLayout.UpdateMeshes();
            m_CurrentCabinetLayout.UpdatePositions();
        }

        public CabinetLayout selectedCabinetLayout => m_SelectedCabinetLayout;
        public KitchenTemplate selectedKitchenTemplate => m_SelectedKitchenTemplate;
        public ProductInfo selectedCountertopFinish => m_SelectedCountertopFinish;
        public ProductInfo selectedCabinetFinish => m_SelectedCabinetFinish;
        public ProductInfo selectedFlooringFinish => m_SelectedFlooringFinish;

        public List<KitchenExtraFeature> selectedExtraFeatures => m_SelectedExtraFeatures;

        public ProductVisualDatabase productVisualDatabase => m_ProductVisualDatabase;

        protected bool canHaveToeKickEntry => selectedBrandCatalog.cabinetManufacturerId != Const.Gemini.ManufactersIds.AgoraBerlinGardens; // Removed berlin gardens toekick PDF entry
        public bool isAgoraBerlinGardens => selectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.AgoraBerlinGardens;
        public bool isEndura => selectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.Endura;
        public bool isToppin => selectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.Toppin;

        public string analyticsKey
        {
            get => m_AnalyticsKey;
            set => m_AnalyticsKey = value;
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
                (cabinetForRuleset != null && cabinetForRuleset.forceProductsToCenterBuildingMethods.HasFlag(m_SelectedBuildingMethod)) ||
                m_SelectedBuildingMethod == KitchenBuildingMethod.Sections ||
                m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog;
        }

        /// <summary>
        ///     Re-sync the ticker labels that show the current kitchen's
        ///     dimensions to the current kitchen size. This is necessary when
        ///     the kitchen's cabinet depth is changed from <see
        ///     cref="CabinetOtherSizesUI"/> mid-design.
        /// </summary>
        public void RefreshTickerLabels()
        {
            foreach (Transform child in m_TickerDimensionsRoot)
            {
                if (child.TryGetComponent(out CabinetTickerWidget ticker))
                {
                    ticker.RefreshLabel();
                }
            }
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

            RefreshTickerLabels();
        }

        public JsonDict SaveToJson()
        {
            // TODO: We're forcing an invariant culture at startup (see AppInitializer)
            //       but a better fix would be to enforce the invariant culture
            //       just for this function. Something like:
            //
            //      CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            //      CultureInfo originalUiCulture = Thread.CurrentThread.CurrentUICulture;
            //      using var restoreCulture = new ScopeGuard(
            //          () =>
            //          {
            //              Thread.CurrentThread.CurrentCulture = originalCulture;
            //              Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            //          }
            //      );
            //
            //      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            //      Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            List<string> dimensions = new List<string>();

            static void AddDimensions(List<string> dimensions, Vector3 sizeInMeters, bool showFraction)
            {
                string length, height, depth;

                if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
                {
                    length = Utility.MeasurementFormat.FormatMetersToCentimeters(sizeInMeters.x) + " ";
                    height = Utility.MeasurementFormat.FormatMetersToCentimeters(sizeInMeters.y) + " ";
                    depth = Utility.MeasurementFormat.FormatMetersToCentimeters(sizeInMeters.z) + " ";
                }
                else
                {
                    length = Utility.MeasurementFormat.FormatMetersToImperial(sizeInMeters.x, showFraction: showFraction);
                    height = Utility.MeasurementFormat.FormatMetersToImperial(sizeInMeters.y, showFeet: false, showFraction: showFraction);
                    depth = Utility.MeasurementFormat.FormatMetersToImperial(sizeInMeters.z, showFeet: false, showFraction: showFraction);
                }

                dimensions.Add($"{length} L x {height} H x {depth} D");
            }

            bool showFraction = false;

            if(ProFitGlobals.appRules.MeasurementFractionsFollowBrandCatalog && m_SelectedBrandCatalog != null)
            {
                showFraction = m_SelectedBrandCatalog.showFractionInMeasurements;
            }

            foreach (CabinetSpan span in layout.spans)
            {
                if (!span.IsSpace())
                {
                    Cuboid spanOutsideCuboid =
                        span.GetOutsideCuboidInSpanSpace(includeCountertop: false);

                    AddDimensions(dimensions, spanOutsideCuboid.size, showFraction);
                }
            }

            string cabinetFinishProductSku      = "";
            string countertopFinishProductSku   = "";
            string paverFinishProductSku        = "";
            string deckFinishProductSku         = "";

            if (m_SelectedCabinetFinish != null)
            {
                cabinetFinishProductSku = m_SelectedCabinetFinish.sku;
            }

            if (m_SelectedCountertopFinish != null)
            {
                countertopFinishProductSku = m_SelectedCountertopFinish.sku;
            }

            // For the flooring, we need to get the visual to know whether it is
            // paver or deck
            if (m_SelectedFlooringFinish != null)
            {
                ProductVisual flooringVisual =
                    m_ProductVisualDatabase.FindRuntimeVisualByID(
                        m_SelectedFlooringFinish.visualId
                    );

                if (flooringVisual)
                {
                    if (flooringVisual.info.flooringType == FlooringType.Stone)
                    {
                        paverFinishProductSku = m_SelectedFlooringFinish.sku;
                    }
                    else
                    {
                        deckFinishProductSku = m_SelectedFlooringFinish.sku;
                    }
                }
            }

            // Legacy model data: used by the server's PDF/quote generation.
            // This is nice because it completely separates data needed by the
            // server from data needed by the game. However, it's not nice too
            // because some code needs to be duplicated (e.g., custom cut-out
            // size).
            JsonList modelSaveDataJson = new List<object>();
            int cabinetSide = 0;
            foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (!cabinet.canHaveProducts)
                {
                    continue;
                }

                AddLegacyOccupantEntries(cabinet, cabinetSide, modelSaveDataJson);

                ++cabinetSide;
            }

            static void AddLegacyOccupantEntries(OccupiableVolume volume, int cabinetSide, JsonList modelSaveDataJson)
            {
                foreach (Occupant occupant in volume.occupants)
                {
                    JsonDict productPositionJson = new Dictionary<string, object>()
                    {
                        { "x", occupant.metersFromVolumeMinX },
                        { "y", 0.0f },
                        { "z", 0.0f },
                    };

                    JsonDict modelDataEntryJson = new Dictionary<string, object>()
                    {
                        { "productID", occupant.productInfo.sku },
                        { "productPosition", productPositionJson },
                        { "cabinetSide", cabinetSide }
                    };

                    Vector3 occupantCutOutSize = occupant.currentCutOutSizeInMeters;
                    Vector3 defaultCutOutSize = occupant.productInfo.cutOutSizeInMeters;
                    if (occupantCutOutSize != defaultCutOutSize)
                    {
                        // We only need (and intentionally want) to record an
                        // occupant's cut-out size if it differs from the default
                        // cut-out size. By not recording cut-out sizes when
                        // they're the same as the default, we allow app admins to
                        // change a product's cut-out size in the backend and
                        // have it be correctly updated when a design is
                        // loaded.

                        string w, h, d;

                        if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
                        {
                            w = Utility.MeasurementFormat.FormatMetersToCentimeters(occupantCutOutSize.x);
                            h = Utility.MeasurementFormat.FormatMetersToCentimeters(occupantCutOutSize.y);
                            d = Utility.MeasurementFormat.FormatMetersToCentimeters(occupantCutOutSize.z);
                        }
                        else
                        {
                            w = ImperialUnit.FromTotalMeters(occupantCutOutSize.x).ToString(showFeet: false);
                            h = ImperialUnit.FromTotalMeters(occupantCutOutSize.y).ToString(showFeet: false);
                            d = ImperialUnit.FromTotalMeters(occupantCutOutSize.z).ToString(showFeet: false);
                        }

                        string customCutOutSize =
                            $"{w} (W) x {h} (H) x {d} (D)";

                        modelDataEntryJson.Add("cutOutSize", customCutOutSize);
                    }

                    modelSaveDataJson.Add(modelDataEntryJson);

                    if (occupant.TryGetComponent(out CabinetShelf nestedVolume))
                    {
                        AddLegacyOccupantEntries(nestedVolume, cabinetSide, modelSaveDataJson);
                    }
                }
            }

            string counterTopHeight;

            if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
            {
                counterTopHeight = Utility.MeasurementFormat.FormatMetersToCentimeters(m_SelectedCountertopThicknessInMeters);
            }
            else
            {
                counterTopHeight = Utility.MeasurementFormat.FormatMetersToImperial(m_SelectedCountertopThicknessInMeters);
            }

            JsonList topMeasurements = new List<object>();
            JsonList frontMeasurements = new List<object>();

            cabinetSide = 0;
            foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (!cabinet.canHaveProducts)
                {
                    continue;
                }

                // Add an entry to the top measurements list for this cabinet
                JsonList cabinetTopMeasurementEntries = new List<object>();
                JsonDict cabinetTopMeasurements = new Dictionary<string, object>()
                {
                    { "section", cabinetSide },
                    { "entries", cabinetTopMeasurementEntries }
                };
                GetLegacyMeasurementsForCabinetSave(cabinet, cabinetTopMeasurementEntries, findTop: true);
                topMeasurements.Add(cabinetTopMeasurements);

                // Add an entry to the front measurements list for this cabinet
                JsonList cabinetFrontMeasurementEntries = new List<object>();
                JsonDict cabinetFrontMeasurements = new Dictionary<string, object>()
                {
                    { "section", cabinetSide },
                    { "entries", cabinetFrontMeasurementEntries }
                };
                GetLegacyMeasurementsForCabinetSave(cabinet, cabinetFrontMeasurementEntries, findTop: false);
                frontMeasurements.Add(cabinetFrontMeasurements);

                ++cabinetSide;
            }

            // Add list of cabinets for the server to itemize on a price sheet
            // in Gemini versions.

            List<int> cabinetIdList = new();

            // New version save data. This is what the app uses to reload the
            // design and it is completely separate from what the server reads
            // to construct the PDF.

            // Encode the save data per-cabinet
            JsonList cabinetList = new List<object>();
            foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (cabinet.cabinetSectionProductInfo != null &&
                    !string.IsNullOrEmpty(cabinet.cabinetSectionProductInfo.sku))
                {
                    if (cabinet.cabinetSectionVisual == null ||
                        cabinet.cabinetSectionVisual.info.sectionAppearsInDetailSheet)
                    {
                        cabinetIdList.Add(cabinet.cabinetSectionProductInfo.id);
                    }
                }

                string cabinetBackingSpriteName =
                    cabinet.backingSprite ? cabinet.backingSprite.name : null;

                JsonDict cabinetEntry = new Dictionary<string, object>()
                {
                    // These fields were added with the Sections building mode
                    // feature update. It allows kitchens to be saved and
                    // reconstructed when they don't exactly match a prebuilt
                    // layout.
                    { "cabinet_type", cabinet.cabinetType.ToString() },
                    { "normalized_anchor_to_previous_section", ProFitUtility.Vector3ToJson(cabinet.normalizedAnchorToPreviousSection) },
                    { "local_euler_angles", ProFitUtility.Vector3ToJson(cabinet.GetTransform().localEulerAngles) },
                    { "min_size_in_meters", ProFitUtility.Vector3ToJson(cabinet.minSizeInMeters) },
                    { "max_size_in_meters", ProFitUtility.Vector3ToJson(cabinet.maxSizeInMeters) },
                    { "backing_sprite", cabinetBackingSpriteName }
                };

                if (cabinet.Is(CabinetType.CornerWedge))
                {
                    int indexOfCabinetBeingWedged =
                        layout.FindCabinetIndex(cabinet.cornerCabinetBeingWedged);

                    cabinetEntry.Add("index_of_cabinet_being_wedged", indexOfCabinetBeingWedged);
                }

                if (cabinet.cabinetSectionProductInfo != null)
                {
                    cabinetEntry.Add("cabinet_server_id", cabinet.cabinetSectionProductInfo.id);
                    cabinetEntry.Add("cabinet_id", cabinet.cabinetSectionProductInfo.sku);
                }

                float lengthInMeters = cabinet.sizeInMetersWithoutAdditive.x;
                if (cabinet.cabinetType == CabinetType.Corner)
                {
                    // For corner pieces, the length may be stretched if a
                    // neighbour cabinet has a bar top. Use the default
                    // counter depth as the corner's size
                    lengthInMeters = m_SelectedBaseCabinetDepthInMeters;
                }

                cabinetEntry.Add("size_in_meters_x", lengthInMeters);

                AddOccupantsToJson(cabinetEntry, cabinet.occupants);

                cabinetList.Add(cabinetEntry);
            }

            JsonList cabinetSpanList = new List<object>();
            foreach (CabinetSpan span in layout.spans)
            {
                string backingProductSku = span.backing?.info.sku;

                JsonDict cabinetSpanEntry = new Dictionary<string, object>()
                {
                    { "index", span.index },
                    { "backing_product_id", backingProductSku },
                    { "depth_in_meters", span.baseCabinetDepthInMeters }
                };

                cabinetSpanList.Add(cabinetSpanEntry);
            }

            const float InchesToFeet = 1.0f / Units.InchesPerFoot;

            float cabinetLinearMeters = 0.0f;
            float cabinetSquareMeters = 0.0f;

            if (m_SelectedBuildingMethod == KitchenBuildingMethod.Standard ||
                m_SelectedBuildingMethod == KitchenBuildingMethod.Sections)
            {
                // The square area of Pro-Fit's cabinet finish is simpler
                // to calculate than the other build modes.

                // This algorithm calculates the square area of a "hidden face".
                // This is where one cabinet span butts up against another span.
                // We don't want to count that hidden face. To simplify the
                // algorithm, we also don't count the face on the right end of
                // the cabinet because it overlaps with the back-face of an
                // adjacent cabinet.

                //  +---------------------------+-------------------+
                //  |                           ^                   A
                //  |                           |                   A
                //  |                           B                   A
                //  |                           |                   A
                //  |                           |                   A
                //  |                           |                   A
                //  |                           V                   A
                //  +---------------------------+AAAAAAAAAAAAAAAAAAA+
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //                              |                   |
                //  +AAAAAAAAAAAAAAAAAAA--------+<--------B-------->+
                //  A                           A                   |
                //  A                           A                   |
                //  A                           A                   |
                //  A                           A                   |
                //  A                           A                   |
                //  A                           A                   |
                //  A                           A                   |
                //  +---------------------------+AAAAAAAAAAAAAAAAAAA+
                //
                //  Where A is always ignored when calculating the area of one
                //  span. A is re-added on the last span to "close it up".
                //
                //  B is ignored for any connected spans.


                bool isSpanConnectedToPreviousSpan = false;

                float cabinetHeight =
                    m_CurrentCabinetLayout.spans[0].cabinets[0].element.sizeInMeters.y;

                float hiddenFaceLinearMeters =
                    m_SelectedBaseCabinetDepthInMeters;

                float hiddenFaceSquareMeters =
                    hiddenFaceLinearMeters * cabinetHeight;

                foreach (CabinetSpan span in m_CurrentCabinetLayout.spans)
                {
                    if (span.IsSpace())
                    {
                        // Close off the last span's hidden faces.
                        cabinetLinearMeters += hiddenFaceLinearMeters;
                        cabinetLinearMeters += hiddenFaceLinearMeters;

                        cabinetSquareMeters += hiddenFaceSquareMeters;
                        cabinetSquareMeters += hiddenFaceSquareMeters;

                        isSpanConnectedToPreviousSpan = false;
                        continue;
                    }

                    if (span.backing &&
                        span.backing.hasRiserAndRiserCountertop &&
                        span.backing.riserMaterialMode == CabinetBacking.RiserMaterialMode.UseCabinetMaterial)
                    {
                        Cuboid riserCuboid = span.backing.riserCuboid;

                        cabinetLinearMeters += (riserCuboid.width + riserCuboid.depth) * 2.0f;

                        cabinetSquareMeters +=
                            riserCuboid.width * riserCuboid.height * 2.0f +
                            riserCuboid.depth * riserCuboid.height * 2.0f;
                    }

                    Cuboid spanOuterCuboid =
                        span.GetOutsideCuboidInSpanSpace(includeCountertop: false);

                    cabinetLinearMeters +=
                        (spanOuterCuboid.width * 2.0f) + spanOuterCuboid.depth; // Intentionally only adding left side

                    cabinetSquareMeters +=
                        spanOuterCuboid.width * spanOuterCuboid.height * 2.0f +
                        spanOuterCuboid.depth * spanOuterCuboid.height; // Intentionally only adding left side

                    cabinetLinearMeters -= hiddenFaceLinearMeters;
                    cabinetSquareMeters -= hiddenFaceSquareMeters;

                    if (isSpanConnectedToPreviousSpan)
                    {
                        cabinetLinearMeters -= hiddenFaceLinearMeters;
                        cabinetSquareMeters -= hiddenFaceSquareMeters;
                    }
                }

                // Close off the last span's two hidden faces (the face at the
                // right end and the face on the front at the right end).
                cabinetLinearMeters += hiddenFaceLinearMeters;
                cabinetLinearMeters += hiddenFaceLinearMeters;

                cabinetSquareMeters += hiddenFaceSquareMeters;
                cabinetSquareMeters += hiddenFaceSquareMeters;

                foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    Cuboid occupiableCuboid = cabinet.occupiableCuboid;

                    foreach (Occupant occupant in cabinet.occupants)
                    {
                        Cuboid cutOutCuboid = occupant.GetCutOutCuboidInVolumeSpace();

                        if (ProFitUtility.IsDistanceAlmostZero(occupiableCuboid.zMin - cutOutCuboid.zMin) ||
                            ProFitUtility.IsDistanceAlmostZero(occupiableCuboid.zMax - cutOutCuboid.zMax))
                        {
                            // The occupant is on the front or back of the
                            // cabinet. Therefore its cut-out area cuts into
                            // and should be subtracted from the total finish
                            // area.

                            cabinetSquareMeters -=
                                cutOutCuboid.width * cutOutCuboid.height;

                            // We do not apply the same subtraction to the
                            // linear distance because we are not checking
                            // anything to do with height.
                        }
                    }
                }
            }

            // Calculate the toe kick linear distance. This may also be
            // itemized on the price sheet.
            float toeKickLinearMeters = 0.0f;

            if (m_SelectedToeKickType != CabinetToeKickType.None)
            {
                bool isToeKickInset =
                    m_SelectedToeKickType == CabinetToeKickType.Inset;

                float? previousCabinetDepth = null;

                foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    if (!cabinet.canHaveLegs ||
                        (cabinet.cabinetSectionVisualPrefab &&
                        cabinet.cabinetSectionVisual.info.cabinetSectionFootingType == CabinetSectionFootingType.OnGround))
                    {
                        // Skip cabinets that directly rest on the ground.
                        previousCabinetDepth = null;
                        continue;
                    }

                    if (cabinet.cabinetType == CabinetType.Space)
                    {
                        continue;
                    }
                    else if (cabinet.cabinetType.IsMidSpanCabinetType(selectedBuildingMethod))
                    {
                        // Front and back of the cabinet.
                        int numberOfSides = 2;

                        // Unless it's WeatherStrong and this is a fridge or
                        // ice box cabinet. Then we only add the back.
                        if (selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                            selectedBrandCatalog.ruleset == BrandCatalogRuleset.WeatherStrong &&
                            cabinet.cabinetSectionVisual &&
                            cabinet.cabinetSectionVisual.info.productCategoriesAllowedInSection.Any(x => x == ProductCategorySpecific.Fridge || x == ProductCategorySpecific.IceBox))
                        {
                            numberOfSides = 1;
                        }

                        toeKickLinearMeters += cabinet.sizeInMeters.x * numberOfSides;
                    }
                    else if (cabinet.cabinetType == CabinetType.Corner)
                    {
                        // Outside faces of the corner.
                        toeKickLinearMeters += cabinet.sizeInMeters.x;
                        toeKickLinearMeters += cabinet.sizeInMeters.z;

                        if (isToeKickInset)
                        {
                            // The inner corner faces.
                            toeKickLinearMeters += Const.ToeKickInsetInMeters * 2.0f;
                        }
                    }
                    else if (cabinet.cabinetType.IsEnd())
                    {
                        // Three sides of the cabinet.
                        toeKickLinearMeters += cabinet.sizeInMeters.x * 2.0f;
                        toeKickLinearMeters += cabinet.sizeInMeters.z;

                        if (isToeKickInset)
                        {
                            toeKickLinearMeters -= Const.ToeKickInsetInMeters;
                        }
                    }
                    else
                    {
                        MelcherLog.LogError("Unhandled cabinet type: " + cabinet.cabinetType, cabinet);
                    }

                    if (!cabinet.cabinetType.IsEnd() && previousCabinetDepth.HasValue)
                    {
                        // Factor in any bumped out cabinets. They count for
                        // extra toe kick linear feet.

                        float cabinetDepthDifference =
                            Mathf.Abs(cabinet.sizeInMeters.z - previousCabinetDepth.Value);

                        toeKickLinearMeters += cabinetDepthDifference;
                    }

                    previousCabinetDepth =
                        (cabinet.cabinetType == CabinetType.Corner) ?
                            cabinet.sizeInMeters.x :
                            cabinet.sizeInMeters.z;
                }
            }

            // Calculate the square area in meters used by countertop material.
            //
            // This used to include (but doesn't any longer because of client
            // feedback) the cabinet backing (its riser and countertop, if
            // applicable) and cabinet shelves (their nested countertop and
            // interior walls which also share the countertop finish).
            //
            // Other occupants that are flush with the countertop have their
            // cut-out size subtracted from the total countertop area.

            float countertopAreaInSquareMeters = 0.0f;
            float countertopLinearLengthInMeters = 0.0f;

            foreach (CabinetSpan cabinetSpan in m_CurrentCabinetLayout.spans)
            {
                if (cabinetSpan.backing && cabinetSpan.backing.hasRiserAndRiserCountertop)
                {
                    if (cabinetSpan.backing.isRiserCountertopVisible)
                    {
                        Cuboid riserCountertopCuboid =
                            cabinetSpan.backing.GetCountertopCuboidInSpanSpace(default);

                        countertopAreaInSquareMeters += riserCountertopCuboid.width * riserCountertopCuboid.depth;
                    }

                    // Factor in the riser's height. This is temporarily
                    // disabled because it sounds like the countertop formula
                    // used in practice does not actually use this.
                    //if (cabinetSpan.backing.riserMaterialMode == CabinetBacking.RiserMaterialMode.UseCountertopMaterial)
                    //{
                    //    Cuboid riserCuboid = cabinetSpan.backing.riserCuboid;

                    //    countertopAreaInSquareMeters +=
                    //        (riserCuboid.width * riserCuboid.height) * 2 +
                    //        (riserCuboid.depth * riserCuboid.height) * 2;
                    //}
                }

                foreach (ElementIndex<Cabinet> i in cabinetSpan.cabinets)
                {
                    Cabinet cabinet = i.element;

                    Cuboid countertopCuboid = cabinet.countertopCuboid;

                    countertopAreaInSquareMeters +=
                        countertopCuboid.width * countertopCuboid.depth;

                    countertopLinearLengthInMeters += countertopCuboid.width;

                    // Subtract the voids left by the occupant's cut-outs. This
                    // was an assumption about how the countertop's cost is
                    // determined and it appears to be incorrect -- the
                    // customer pays for the cut-out area too. Therefore, this
                    // code is disabled.
                    //foreach (Occupant occupant in cabinet.occupants)
                    //{
                    //    Cuboid cutOutCuboid = occupant.GetCutOutCuboidInVolumeSpace();

                    //    if (ProFitUtility.IsDistanceAlmostZero(countertopCuboid.yMin - cutOutCuboid.yMax))
                    //    {
                    //        // The occupant is on the countertop. Therefore its
                    //        // cut-out area cuts into and should be subtracted
                    //        // from the total countertop area.
                    //        countertopAreaInSquareMeters -=
                    //            cutOutCuboid.width * cutOutCuboid.depth;
                    //    }

                    //    if (occupant.TryGetComponent(out CabinetShelf shelf))
                    //    {
                    //        Cuboid nestedCountertopCuboid = shelf.countertopCuboid;

                    //        // Plunge's nested countertop
                    //        countertopAreaInSquareMeters +=
                    //            nestedCountertopCuboid.width *
                    //            nestedCountertopCuboid.depth;

                    //        // Plunge walls
                    //        countertopAreaInSquareMeters +=
                    //            (cutOutCuboid.width * cutOutCuboid.height) +
                    //            (cutOutCuboid.depth * cutOutCuboid.height) * 2;
                    //    }
                    //}
                }
            }

            float countertopAreaInSquareFeet =
                countertopAreaInSquareMeters * (Units.Meters2Inches * Units.Meters2Inches) * (InchesToFeet * InchesToFeet);

            string countertopArea =
                ProFitGlobals.userProfile.activeUnits == MeasurementUnit.FeetAndInches ?
                    countertopAreaInSquareFeet + " sq. ft." :
                    countertopAreaInSquareMeters + " sq. m.";

            float countertopLinearLengthInFeet =
                countertopLinearLengthInMeters * Units.Meters2Inches * InchesToFeet;

            JsonDict CreateManualPriceSheetEntry(string productSku, string productName, float price)
            {
                return
                    new Dictionary<string, object>()
                    {
                        { "product_id", productSku },
                        { "name", productName },
                        { "price", price }
                    };
            }

            JsonList manualPriceSheetEntries = new List<object>();
            Dictionary<int, float> productIdToPriceChangeFactor = new ();

            // MARK: Stoll Price Sheet
            if (ProFitGlobals.appRules.PriceSheetExtras == PriceSheetExtras.StollCustomKitchen)
            {
                // All custom kitchen's have this fee.
                manualPriceSheetEntries.Add(
                    CreateManualPriceSheetEntry(
                        productSku: "OKSEFE",
                        productName: "Custom Kitchen Setup Fee",
                        price: 325.00f
                    )
                );

                if (selectedCountertopFinish != null)
                {
                    // Stoll's countertops are priced by as such:
                    //
                    // """
                    // The counter formula will always be the
                    // overall length x countertop depth which is always 30�
                    // divided by 144 x $$
                    //
                    // 8� 2� + 8� 2� = 16� 4� - 28�(CONTIGUOUS DEDUCTION) = 14�
                    //
                    // 14� X 12 = 168�
                    //
                    // 168� X 30�(CT DEPTH) = 5040
                    //
                    // 5040 / 35 SQ FT      // This should've been 144 (inches per sq. ft.), not 35
                    //
                    // 35SQ FT X $84 = $2940
                    //
                    // Whenever there is an l-shaped kitchen, 28� should be
                    // subtracted after overall is calculated for the butting up
                    // of the two kitchens. When it is an u-shaped 56� should be
                    // subtracted for both ends being butted up.
                    // """
                    //
                    // Source: https://3.basecamp.com/3293895/buckets/24690001/messages/8445955570#__recording_8497490596

                    float summedCountertopAreaInMeters = 0.0f;
                    CabinetSpan continguousPreviousSpan = null;

                    foreach (CabinetSpan span in layout.spans)
                    {
                        if (!span.IsSpace())
                        {
                            Cuboid outsideCuboid =
                                span.GetOutsideCuboidInSpanSpace(includeCountertop: true);

                            float countertopWidthInMeters = outsideCuboid.width;
                            float countertopDepthInMeters = outsideCuboid.depth;

                            if (continguousPreviousSpan != null)
                            {
                                // Part of the width was already accounted for
                                // in the previous contiguous span.
                                countertopWidthInMeters -= baseCabinetDepthInMeters;
                            }

                            if (span.backing && span.backing.hasRiserAndRiserCountertop)
                            {
                                Cuboid backingCountertopCuboid =
                                    span.backing.GetCountertopCuboidInSpanSpace(default);

                                countertopDepthInMeters += backingCountertopCuboid.depth;
                            }

                            summedCountertopAreaInMeters += countertopWidthInMeters * countertopDepthInMeters;
                        }

                        continguousPreviousSpan = span.IsSpace() ? null : span;
                    }

                    float stollCountertopAreaInSquareFeet =
                        summedCountertopAreaInMeters * (Units.Meters2Inches * Units.Meters2Inches) * (InchesToFeet * InchesToFeet);

                    // Countertops are priced by sq. ft.
                    manualPriceSheetEntries.Add(
                        CreateManualPriceSheetEntry(
                            selectedCountertopFinish.sku,
                            selectedCountertopFinish.name,
                            selectedCountertopFinish.priceCustomer * Mathf.CeilToInt((float)stollCountertopAreaInSquareFeet)
                        )
                    );
                }

                if (selectedCabinetFinish != null)
                {
                    // Stoll's cabinet finishes are priced according to
                    // the outside distance of each span summed.

                    float summedOutsideWidthInMeters = 0.0f;

                    foreach (CabinetSpan span in layout.spans)
                    {
                        if (span.IsSpace())
                        {
                            continue;
                        }

                        Cuboid outsideCuboid =
                            span.GetOutsideCuboidInSpanSpace();

                        summedOutsideWidthInMeters += outsideCuboid.width;
                    }

                    manualPriceSheetEntries.Add(
                        CreateManualPriceSheetEntry(
                            selectedCabinetFinish.sku,
                            selectedCabinetFinish.name,
                            selectedCabinetFinish.priceCustomer * Mathf.CeilToInt((summedOutsideWidthInMeters * Units.Meters2Inches) / Units.InchesPerFoot)
                        )
                    );
                }

                // Add the price of all custom cabinets
                foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
                {
                    ProductInfo cabinetProductInfo =
                        i.element.cabinetSectionProductInfo;

                    if (cabinetProductInfo == null ||
                        cabinetProductInfo.priceCustomer <= 0.0)
                    {
                        continue;
                    }

                    manualPriceSheetEntries.Add(
                        CreateManualPriceSheetEntry(
                            productSku: cabinetProductInfo.sku,
                            productName: cabinetProductInfo.name,
                            price: cabinetProductInfo.priceCustomer
                        )
                    );
                }

                // Add CORBEL for each end panel and center post on the back
                // side.

                float backsplashLinearMeters = 0.0f;
                int highBarTopBackColumnHalvesCount = 0;
                bool lastSpanHasHighBarTop = false;

                foreach (CabinetSpan span in layout.spans)
                {
                    bool includeFirstColumn = !lastSpanHasHighBarTop;
                    lastSpanHasHighBarTop = false;

                    if (!span.backing)
                    {
                        continue;
                    }

                    if (span.backing.info.category == ProductCategory.Backsplash)
                    {
                        backsplashLinearMeters += span.backing.riserCuboid.width;

                        continue;
                    }

                    if (span.backing.info.category != ProductCategory.BarTopHigh)
                    {
                        continue;
                    }

                    foreach (ElementIndex<Cabinet> i in span.cabinets)
                    {
                        Cabinet cabinet = i.element;

                        highBarTopBackColumnHalvesCount += cabinet.CountVisibleBackColumnHalves();
                    }

                    if (!includeFirstColumn)
                    {
                        // The previous span was a high bar top and therefore
                        // the first column on this span is shared by the last.
                        // Don't double count it.
                        --highBarTopBackColumnHalvesCount;
                    }
                }

                int highBarTopColumnCount = highBarTopBackColumnHalvesCount / 2;

                for (int i = 0; i < highBarTopColumnCount; ++i)
                {
                    manualPriceSheetEntries.Add(
                        CreateManualPriceSheetEntry(
                            productSku: "CORBEL",
                            productName: "Raised Bar Countertop Support Corbel",
                            price: 75.00f
                        )
                    );
                }

                int backsplashFeet = Mathf.CeilToInt(backsplashLinearMeters * Units.Meters2Inches / Units.InchesPerFoot);
                for (int i = 0; i < backsplashFeet; ++i)
                {
                    manualPriceSheetEntries.Add(
                        CreateManualPriceSheetEntry(
                            productSku: "OKBKSP",
                            productName: "Backsplash per Linear Foot",
                            price: 75.00f
                        )
                    );
                }
            }

            // MARK: Gemini Price Sheet
            if (ProFitGlobals.appRules.PriceSheetExtras == PriceSheetExtras.GeminiCatalog)
            {
                // When a Challenger Premium Finish Package is installed, the
                // cost of each cabinet is increased by $463.
                JsonDict premiumFinishPackageEntry =
                    selectedExtraFeatures.Contains(KitchenExtraFeature.ChallengerPremiumCabinetFinish) ?
                        CreateManualPriceSheetEntry(
                            productSku: "CPFP",
                            productName: "Challenger Premium Finish Package",
                            price: 463.00f
                        ) :
                        null;

                bool isShakerDoorSelected = m_SelectedDoorStyleName == "Shaker";
                bool isDecoGrain = m_SelectedCabinetFinish != null && m_SelectedCabinetFinish.manufacturerId == 140;

                // Number of end panels (i.e., number of ends that are not
                // neighbouring a fridge cabinet).
                int cabinetEndPanelCount = 0;

                CabinetSectionDoorHandleStyle style = m_SelectedBrandCatalog.cabinetSectionDoorHandleStyle;
                float handleBasePrice = 0f;

                if(style != null)
                {
                    handleBasePrice = style.baseHandlePrice;
                }

                foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    if (cabinet.cabinetType.IsEnd())
                    {
                        ++cabinetEndPanelCount;
                    }

                    // MARK: Door Handle Style
                    if(!string.IsNullOrEmpty(m_SelectedDoorHandleStyleName))
                    {
                        CabinetSectionDoorHandle handle = cabinet.GetComponentInChildren<CabinetSectionDoorHandle>();

                        if(handle != null)
                        {
                            for(int item = 0; item < handle.hanndleCount; item++)
                            {
                                 manualPriceSheetEntries.Add(
                                    CreateManualPriceSheetEntry(
                                        productSku: "HARDWARE",                                    
                                        productName: m_SelectedDoorHandleStyleName,
                                        price: handleBasePrice
                                    )
                                );
                            }
                        }
                    }

                    if (isShakerDoorSelected &&
                        cabinet.cabinetSectionProductInfo != null &&
                        cabinet.cabinetSectionVisual.info.expectingCabinetDoors &&
                        !productIdToPriceChangeFactor.ContainsKey(cabinet.cabinetSectionProductInfo.id))
                    {
                        // Increase the price of all Challenger Shaker cabinets
                        // by 5%
                        productIdToPriceChangeFactor.Add(
                            cabinet.cabinetSectionProductInfo.id,
                            1.05f
                        );
                    }

                    if(isDecoGrain &&
                        cabinet.cabinetSectionProductInfo != null &&
                        !productIdToPriceChangeFactor.ContainsKey(cabinet.cabinetSectionProductInfo.id))
                    {
                        // Increase the price of all Deco Grain cabinets by 25%
                        productIdToPriceChangeFactor.Add(
                            cabinet.cabinetSectionProductInfo.id,
                            1.25f
                        );
                    }

                    // Handle Challenger's premium finish package.

                    if (premiumFinishPackageEntry != null &&
                        cabinet.cabinetType != CabinetType.Space &&
                        !cabinet.cabinetType.IsEnd(includeTransitions: true))
                    {
                        manualPriceSheetEntries.Add(premiumFinishPackageEntry);
                    }

                    // Handle various custom SKUs for cabinets

                    const int FillerBaseCabinetCategoryId = 30;

                    if (cabinet.cabinetSectionProductInfo != null)
                    {
                        if (cabinet.cabinetSectionProductInfo.cabinetSectionCategoryId == FillerBaseCabinetCategoryId)
                        {
                            // Filler cabinets

                            JsonDict fillerEntry =
                                CreateManualPriceSheetEntry(
                                    productSku: cabinet.cabinetSectionProductInfo.sku,
                                    productName: cabinet.cabinetSectionProductInfo.name,
                                    price: cabinet.cabinetSectionProductInfo.priceCustomer
                                );

                            // Fillers are added twice:
                            // "Fillers are 1.5� thick (deep).  For simplicity
                            // sake, we could make it pull in a quantity of 2
                            // automatically (front and back)" -Melony
                            manualPriceSheetEntries.Add(fillerEntry);
                            manualPriceSheetEntries.Add(fillerEntry);
                        }

                        if (m_SelectedBrandCatalog.ruleset == BrandCatalogRuleset.Napoleon &&
                            cabinet.Is(CabinetType.Corner))
                        {
                            // Add 2 45-degree transitions for each Napoleon
                            // corner. We do this regardless of 45 or 90 degree
                            // corner because two 45-transitions are used to
                            // make a 90-degree turn.
                            // https://3.basecamp.com/3293895/buckets/37470979/messages/8842531021#__recording_8843049972
                            JsonDict transitionPiece =
                                CreateManualPriceSheetEntry(
                                    productSku: "IM-45T-CN",
                                    productName: "OASIS� 45� Transition Piece",
                                    price: ProFitGlobals.userProfile.currency == Currency.CAD ?
                                        99.0f :
                                        109.0f  // USD
                                );
                            manualPriceSheetEntries.Add(transitionPiece);
                            manualPriceSheetEntries.Add(transitionPiece);
                        }

                        if (isAgoraBerlinGardens &&
                            cabinet.Is(CabinetType.Corner))
                        {
                            JsonDict transitionPiece =
                                CreateManualPriceSheetEntry(
                                    productSku: cabinet.cabinetSectionProductInfo.sku,
                                    productName: cabinet.cabinetSectionProductInfo.name,
                                    price: cabinet.cabinetSectionProductInfo.priceCustomer
                                );
                            manualPriceSheetEntries.Add(transitionPiece);
                        }
                    }

                    // MARK: FORCED BERLIN GARDENS KAMADO GRILL END PANELS
                    if(isAgoraBerlinGardens &&
                        cabinet.cabinetSectionProductInfo != null &&
                        cabinet.cabinetSectionProductInfo.sku != null &&
                        (cabinet.cabinetSectionProductInfo.sku is "GADB" or "GADA"))
                    {
                         m_CurrentCabinetLayout.GetCabinetNeighbours(
                            cabinet,
                            out Cabinet leftNeighbour,
                            out Cabinet rightNeighbour
                        );

                        // product identity of the neighbour
                        // string leftSku  = leftNeighbour?.cabinetSectionProductInfo?.sku;
                        // string rightSku = rightNeighbour?.cabinetSectionProductInfo?.sku;

                        bool leftIsEnd  = leftNeighbour != null && leftNeighbour.cabinetType.IsEnd();
                        bool rightIsEnd = rightNeighbour != null && rightNeighbour.cabinetType.IsEnd();

                        bool isBesideACabinet = true;

                        EndPanelInfo endPanelInfo =
                                m_SelectedBrandCatalog.
                                cabinetRuntimeEndPanelInfo.
                                GetEndPanelInfoByTargetDepthOrDefault(cabinet.sizeInMeters.z - m_BackPanelDepthInMeters);

                        
                        // Only the left neighbour is an end cabinet: this side is already
                        // finished by it, so we only need one panel (-R) — 1 price added.
                        if(leftIsEnd && rightIsEnd == false)
                        {
                            manualPriceSheetEntries.Add(
                                CreateManualPriceSheetEntry(
                                    productSku: endPanelInfo.productId + "-R",
                                    productName: endPanelInfo.productName + "-R",
                                    price: endPanelInfo.dealerPrice
                                )
                            );

                            isBesideACabinet = false;
                        }

                        // Only the right neighbour is an end cabinet: mirror of the above,
                        // so we only need one panel (-L) — 1 price added.
                        if(rightIsEnd && leftIsEnd == false)
                        {
                            manualPriceSheetEntries.Add(
                                CreateManualPriceSheetEntry(
                                    productSku: endPanelInfo.productId + "-L",
                                    productName: endPanelInfo.productName + "-L",
                                    price: endPanelInfo.dealerPrice
                                )
                            );

                            isBesideACabinet = false;
                        }

                        // Both neighbours are end cabinets: both sides are already finished,
                        // so no panel is needed — 0 prices added.
                        if(leftIsEnd && rightIsEnd)
                        {
                            isBesideACabinet = false;
                        }

                        // Neither neighbour is an end cabinet (still beside a regular cabinet
                        // on both sides): both faces are exposed, so add both panels — 2 prices.
                        if(isBesideACabinet)
                        {
                            manualPriceSheetEntries.Add(
                                CreateManualPriceSheetEntry(
                                    productSku: endPanelInfo.productId + "-R",
                                    productName: endPanelInfo.productName + "-R",
                                    price: endPanelInfo.dealerPrice
                                )
                            );

                            manualPriceSheetEntries.Add(
                                CreateManualPriceSheetEntry(
                                    productSku: endPanelInfo.productId + "-L",
                                    productName: endPanelInfo.productName + "-L",
                                    price: endPanelInfo.dealerPrice
                                )
                            );
                        }
                    }

                    // MARK: Runtime End Panels
                    if(m_SelectedBrandCatalog.hasEndPanelsOption &&
                        cabinet.cabinetType.IsEnd() &&
                        cabinet.sizeInMeters.x > 0)
                    {
                        string suffix = string.Empty;
                        if(cabinet.cabinetType == CabinetType.EndLeft)
                        {
                            suffix = "-L";
                        }
                        else if(cabinet.cabinetType == CabinetType.EndRight)
                        {
                            suffix = "-R";
                        }

                        if(suffix != string.Empty)
                        {
                            EndPanelInfo endPanelInfo =
                                m_SelectedBrandCatalog.
                                cabinetRuntimeEndPanelInfo.
                                GetEndPanelInfoByTargetDepthOrDefault(cabinet.sizeInMeters.z - m_BackPanelDepthInMeters);

                            manualPriceSheetEntries.Add(
                                CreateManualPriceSheetEntry(
                                    productSku: endPanelInfo.productId + suffix,
                                    productName: endPanelInfo.productName + suffix,
                                    price: endPanelInfo.dealerPrice
                                )
                            );
                        }
                    }

                    // MARK: Runtime Back Panels
                    if(m_SelectedBrandCatalog.hasBackPanelsOption &&
                        selectedExtraFeatures.Contains(KitchenExtraFeature.CabinetBackPanels) &&
                        cabinet.cabinetSectionProductInfo != null &&
                        !cabinet.cabinetSectionVisual.info.IsCategoryAllowedInSection(ProductCategorySpecific.Fridge))
                    {
                        ProductInfo cabinetProductInfo = cabinet.cabinetSectionProductInfo;
                        float width = cabinet.sizeInMeters.x;
                        float depth = cabinet.sizeInMeters.z;
                        bool isCornerBackPanel = false;

                        if(cabinet.Is(CabinetType.Corner))
                        {
                            width = cabinet.sizeInMeters.x - m_BackPanelDepthInMeters;
                            depth = cabinet.sizeInMeters.z - m_BackPanelDepthInMeters;
                            isCornerBackPanel = true;
                        }

                        BrandCatalog.FridgePanelInfo backPanel;
                        if(isCornerBackPanel)
                        {
                            backPanel = m_SelectedBrandCatalog.FindFirstCabinetBackPanelByDepth(depth, cabinetProductInfo.cabinetSectionCategoryId);
                        }
                        else
                        {
                            backPanel = m_SelectedBrandCatalog.FindFirstCabinetBackPanel(width, cabinetProductInfo.cabinetSectionCategoryId);
                        }

                        JsonDict backPanelEntry;

                        if(backPanel != null)
                        {
                            backPanelEntry = CreateManualPriceSheetEntry(
                                productSku: backPanel.productId,                                    
                                productName: backPanel.productName,
                                price: backPanel.dealerPrice
                            );
                        }
                        else
                        {
                            BrandCatalog.FridgePanelInfo fallbackPanel = m_SelectedBrandCatalog.cabinetRuntimeBackPanelInfo.fallbackBackPanel;
                            string backPanelProductId = $"{backPanel.productId}-{backPanel.specificPanelWidthInMeters * Units.Meters2Inches}x{backPanel.panelHeight}";
                            string backPanelProductName = $"{backPanel.productName} {backPanel.specificPanelWidthInMeters * Units.Meters2Inches}\"W x {backPanel.panelHeight}\"H";

                            backPanelEntry = CreateManualPriceSheetEntry(
                                productSku: backPanelProductId,
                                productName: backPanelProductName,
                                price: backPanel.dealerPrice
                            );
                        }

                        manualPriceSheetEntries.Add(backPanelEntry);

                        if(isCornerBackPanel)
                        {
                            manualPriceSheetEntries.Add(backPanelEntry);
                        }
                    }

                    // Handle fridge panel SKUs

                    if (cabinet.occupants.Count <= 0 ||
                        cabinet.cabinetSectionVisual == null ||
                        !cabinet.cabinetSectionVisual.info.IsCategoryAllowedInSection(ProductCategorySpecific.Fridge))
                    {
                        continue;
                    }

                    Occupant fridge = cabinet.occupants[0];

                    ImperialUnit imperial =
                        ImperialUnit.FromTotalMeters(
                            fridge.currentCutOutSizeInMeters.x
                        );

                    layout.GetCabinetNeighbours(
                        cabinet,
                        out Cabinet leftCabinet,
                        out Cabinet rightCabinet
                    );

                    int fridgeEndPanelCount = 0;

                    if (!leftCabinet || leftCabinet.cabinetType.IsEnd())
                    {
                        --cabinetEndPanelCount;
                        ++fridgeEndPanelCount;
                    }

                    if (!rightCabinet || rightCabinet.cabinetType.IsEnd())
                    {
                        --cabinetEndPanelCount;
                        ++fridgeEndPanelCount;
                    }

                    float fridgeWidthInInches = (float)imperial.totalInches;

                    {
                        BrandCatalog.FridgePanelInfo fridgePanel =
                            selectedBrandCatalog.FindFirstFridgeBackPanel(
                                fridge.productInfo.productSizeInMeters.x
                            );

                        bool canAddFridgeBackPanel = CanAddFridgeBackPanelToPriceSheet();

                        if(m_SelectedBrandCatalog.ruleset == BrandCatalogRuleset.Napoleon && fridgeEndPanelCount > 0)
                        {
                            canAddFridgeBackPanel = false;
                        }

                        if (fridgePanel != null && canAddFridgeBackPanel)
                        {
                            string productId;
                            string productName;

                            productId = $"{fridgePanel.productId}-{fridgeWidthInInches}x{fridgePanel.panelHeight}";
                            productName = $"{fridgePanel.productName} {fridgeWidthInInches}\"W x {fridgePanel.panelHeight}\"H";


                            if (fridgePanel.specificPanelWidthInMeters > 0.0f)
                            {
                                if(isAgoraBerlinGardens)
                                {
                                    fridgeWidthInInches += 1.5f;
                                    productId = $"{fridgePanel.productId}x{cabinet.sizeInMeters.z * Units.Meters2Inches}x{cabinet.sizeInMeters.y * Units.Meters2Inches}";
                                    productName = fridgePanel.productName;
                                }
                                else
                                {
                                    // Use the panel's ID and name exactly since
                                    // they're pre-sized.
                                    productId = fridgePanel.productId;
                                    productName = fridgePanel.productName;
                                }

                                fridgeWidthInInches = fridgePanel.specificPanelWidthInMeters * Units.Meters2Inches;
                            }

                            manualPriceSheetEntries.Add(
                                CreateManualPriceSheetEntry(
                                    productSku: productId,
                                    productName: productName,
                                    price: fridgePanel.dealerPrice
                                )
                            );
                        }
                    }

                    {
                        // Fridge End Panel is removed in Agora Berlin Gardens and Toppin Price Sheet
                        if(isAgoraBerlinGardens || isToppin)
                        {
                            fridgeEndPanelCount = 0;
                        }

                        BrandCatalog.FridgePanelInfo endPanel =
                            selectedBrandCatalog.fridgeEndPanelInfo;

                        string productId = $"{endPanel.productId}-{selectedBrandCatalog.standardCabinetDepthInInches}x{endPanel.panelHeight}";
                        if (selectedBrandCatalog.ruleset == BrandCatalogRuleset.Challenger)
                        {
                            productId = $"{endPanel.productId}-{selectedBrandCatalog.standardCabinetDepthInInches}D";
                        }

                        string productName = $"{endPanel.productName} {endPanel.panelHeight}\"H x {selectedBrandCatalog.standardCabinetDepthInInches}\"D";

                        if (endPanel.specificPanelWidthInMeters > 0.0f)
                        {
                            fridgeWidthInInches = endPanel.specificPanelWidthInMeters * Units.Meters2Inches;

                            // Use the panel's ID and name exactly since
                            // they're pre-sized.
                            productId = endPanel.productId;
                            productName = endPanel.productName;
                        }

                        for (; fridgeEndPanelCount > 0; --fridgeEndPanelCount)
                        {
                            manualPriceSheetEntries.Add(
                                CreateManualPriceSheetEntry(
                                    productSku: productId,
                                    productName: productName,
                                    price: endPanel.dealerPrice
                                )
                            );
                        }
                    }
                }

                // Add cabinet end panels. Not every catalog uses these so we
                // branch based on whether it has a SKU.
                if (!string.IsNullOrWhiteSpace(selectedBrandCatalog.cabinetEndPanelInfo.productId))
                {
                    BrandCatalog.FridgePanelInfo cabinetEndPanelInfo =
                        selectedBrandCatalog.cabinetEndPanelInfo;

                    JsonDict cabinetEndPanelEntry =
                        CreateManualPriceSheetEntry(
                            productSku: cabinetEndPanelInfo.productId,
                            productName: cabinetEndPanelInfo.productName,
                            price: cabinetEndPanelInfo.dealerPrice
                        );

                    for (int i = 0; i < cabinetEndPanelCount; ++i)
                    {
                        manualPriceSheetEntries.Add(cabinetEndPanelEntry);
                    }
                }

                if(isAgoraBerlinGardens)
                {
                    JsonDict countertopPricing = TryGetCountertopPricing();

                    if(countertopPricing.ContainsKey("no_countertop") == false)
                    {
                        manualPriceSheetEntries.Add(countertopPricing);
                    }
                }

                if (selectedBrandCatalog.ruleset == BrandCatalogRuleset.Challenger)
                {
                    if (selectedCountertopFinish != null &&
                        ProFitGlobals.catalogData.FindManufacturerById(selectedCountertopFinish.manufacturerId, out ManufacturerInfo countertopFinishManufacturer) &&
                        ProFitGlobals.catalogData.FindManufacturerByName(selectedBrandCatalog.catalogName, out ManufacturerInfo challengerManufacturer) &&
                        countertopFinishManufacturer.id == challengerManufacturer.id)
                    {
                        // Add Challenger countertops to the price sheet.

                        // We need to account for different prices by
                        // countertop depth.

                        const int ChallengerBrushedStainlessFinishId = 1960;

                        List<ChallengerCountertopInfo> depthToPrice =
                            selectedCountertopFinish.id == ChallengerBrushedStainlessFinishId ?
                                new List<ChallengerCountertopInfo>()
                                {
                                    new ()
                                    {
                                        depthInInches = 13.0f,
                                        sku = "SST-13",
                                        description = "Countertop, 13\"D x 1.25\" Thick",
                                        pricePerLinearInch = 42.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 18.0f,
                                        sku = "SST-18",
                                        description = "Countertop, 18\"D x 1.25\" Thick",
                                        pricePerLinearInch = 55.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 29.0f,
                                        sku = "SST-29",
                                        description = "Countertop, 29\"D x 1.25\" Thick",
                                        pricePerLinearInch = 82.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 30.0f,
                                        sku = "SST-30",
                                        description = "Countertop, 30\"D x 1.25\" Thick",
                                        pricePerLinearInch = 86.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 33.0f,
                                        sku = "SST-33",
                                        description = "Countertop, 33\"D x 1.25\" Thick",
                                        pricePerLinearInch = 93.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 41.0f,
                                        sku = "SST-41",
                                        description = "Countertop, 41\"D x 1.25\" Thick",
                                        pricePerLinearInch = 118.0f,

                                    },
                                } :
                                new List<ChallengerCountertopInfo>()
                                {
                                    new ()
                                    {
                                        depthInInches = 13.0f,
                                        sku = "ACT-13",
                                        description = "Countertop, 13\"D x 1.25\" Thick",
                                        pricePerLinearInch = 19.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 18.0f,
                                        sku = "ACT-18",
                                        description = "Countertop, 18\"D x 1.25\" Thick",
                                        pricePerLinearInch = 25.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 29.0f,
                                        sku = "ACT-29",
                                        description = "Countertop, 29\"D x 1.25\" Thick",
                                        pricePerLinearInch = 38.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 30.0f,
                                        sku = "ACT-30",
                                        description = "Countertop, 30\"D x 1.25\" Thick",
                                        pricePerLinearInch = 41.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 33.0f,
                                        sku = "ACT-33",
                                        description = "Countertop, 33\"D x 1.25\" Thick",
                                        pricePerLinearInch = 44.0f,
                                    },
                                    new ()
                                    {
                                        depthInInches = 41.0f,
                                        sku = "ACT-41",
                                        description = "Countertop, 41\"D x 1.25\" Thick",
                                        pricePerLinearInch = 56.0f,

                                    },
                                };

                        float backsplashPricePerLinearInch =
                            m_SelectedCountertopFinish.id == ChallengerBrushedStainlessFinishId ?
                                12.0f : 5.0f;

                        float highBarTopPricePerLinearInch =
                            m_SelectedCountertopFinish.id == ChallengerBrushedStainlessFinishId ?
                                63.0f : 40.0f;

                        // Challenger uses a linear length formula.
                        // https://3.basecamp.com/3293895/buckets/37470979/messages/9033213434
                        float lastSpanDepthInMeters = 0.0f;
                        float lastSpanBackingDepth = 0.0f;
                        float totalLinearPricing = 0.0f;

                        foreach (CabinetSpan span in layout.spans)
                        {
                            Cuboid outerCuboid =
                                span.GetOutsideCuboidInSpanSpace(includeCountertop: true);

                            if (span.IsSpace())
                            {
                                lastSpanDepthInMeters = 0.0f;
                                lastSpanBackingDepth = 0.0f;
                                continue;
                            }

                            // Exclude any corner distance overlapped with the
                            // previous span.

                            float backEdgeLinearLengthInMeters =
                                outerCuboid.width - lastSpanDepthInMeters;

                            lastSpanDepthInMeters = outerCuboid.depth;

                            float lastSpanDepthInInches = lastSpanDepthInMeters * Units.Meters2Inches;

                            // A smudge value is used to ensure that the
                            // measured countertop depth isn't just a tiny
                            // amount deeper than its best candidate for the
                            // preset countertop depth prices.
                            const float Smudge = 0.001f;

                            // Find the price for this countertop depth.
                            foreach (ChallengerCountertopInfo info in depthToPrice)
                            {
                                if (info.depthInInches >= lastSpanDepthInInches - Smudge)
                                {
                                    float inchesWide = backEdgeLinearLengthInMeters * Units.Meters2Inches;

                                    totalLinearPricing += inchesWide * info.pricePerLinearInch;

                                    break;
                                }
                            }

                            // Include high bar tops as a separate linear
                            // length. Any corner distance that overlaps with
                            // a neighbouring cabinet span is only counted once.

                            float spanBackingDepth = 0.0f;

                            if (span.backing && span.backing.info.category != ProductCategory.BarTopLow)
                            {
                                float backingPricePerLinearInch =
                                    span.backing.info.category == ProductCategory.Backsplash ?
                                        backsplashPricePerLinearInch :
                                        highBarTopPricePerLinearInch;

                                Cuboid backingCuboid =
                                    span.backing.GetCountertopCuboidInSpanSpace(default);

                                spanBackingDepth = backingCuboid.depth;

                                totalLinearPricing += (backingCuboid.width - lastSpanBackingDepth) * Units.Meters2Inches * backingPricePerLinearInch;
                            }

                            lastSpanBackingDepth = spanBackingDepth;
                        }

                        JsonDict countertopFinishEntry =
                            CreateManualPriceSheetEntry(
                                selectedCountertopFinish.sku,
                                selectedCountertopFinish.name,
                                totalLinearPricing
                            );

                        manualPriceSheetEntries.Add(countertopFinishEntry);
                    }

                    if (toeKickLinearMeters > 0.0f
                        && canHaveToeKickEntry)
                    {
                        float toeKickLinearInches = toeKickLinearMeters * Units.Meters2Inches;

                        // The first few toe kicks don't follow a clear pattern
                        // but then it becomes 1 toe kick every 96 inches.
                        // https://3.basecamp.com/3293895/buckets/37470979/messages/8580501435#__recording_8619791518
                        int numberOfToeKicks =
                            toeKickLinearInches switch
                            {
                                <= 80.0f => 1,
                                <= 182.0f => 2,
                                <= 260.0f => 3,
                                <= 356.0f => 4,
                                // "Just continue with the assumption of an infinite amount of toe kicks so
                                // that it can continue to calculate within the parameters that Jason has
                                // sent us to keep increasing when needed."
                                _ => 4 + Mathf.CeilToInt((toeKickLinearInches - 356.0f) / 96.0f)
                            };

                        JsonDict toeKickEntry =
                            CreateManualPriceSheetEntry(
                                productSku: $"7TK-94W",
                                productName: $"Toe Kick, 94\" Powder Coated Aluminum",
                                price: 559.0f
                            );

                        for (int i = 0; i < numberOfToeKicks; ++i)
                        {
                            manualPriceSheetEntries.Add(toeKickEntry);
                        }
                    }
                }

                if (selectedBrandCatalog.ruleset == BrandCatalogRuleset.WeatherStrong &&
                    m_SelectedCabinetFinish != null)
                {
                    // WeatherStrong finishes require end vents.
                    if(m_SelectedBrandCatalog.cabinetManufacturerId == Const.Gemini.ManufactersIds.AmericanOutdoorCabinets)
                    {
                        manualPriceSheetEntries.Add(
                            CreateManualPriceSheetEntry(
                                productSku: $"AOBEP-2PK",
                                productName: $"Base Ends - 2-Pack",
                                price: 0.0f
                            )
                        );
                    }
                    else if(m_SelectedBrandCatalog.cabinetManufacturerId is
                        Const.Gemini.ManufactersIds.Endura or
                        Const.Gemini.ManufactersIds.AgoraBerlinGardens)
                    {

                    }
                    else
                    {
                        manualPriceSheetEntries.Add(
                            CreateManualPriceSheetEntry(
                                productSku: $"BEP28.530-2PK",
                                productName: $"28.5\"W x 30\"H Base End w/Vents - 2-Pack",
                                price: 0.0f
                            )
                        );
                    }

                    // WeatherStrong toe kicks:

                    // "To calculate the total amount of Toe Kick required, take the lengths
                    // of the front and back of the island, plus 54� for the ends. Divide the
                    // total by 96 and round up to the nearest whole number. That�s the number
                    // of TKF696 to order."
                    // https://3.basecamp.com/3293895/buckets/37470979/messages/8512024554#__recording_8573519126

                    const float WeatherStrongToeKickLengthInMeters =
                        96.0f * Units.Inches2Meters;

                    int numberOfToeKicks =
                        Mathf.CeilToInt(toeKickLinearMeters / WeatherStrongToeKickLengthInMeters);

                    if(isAgoraBerlinGardens)
                    {
                        numberOfToeKicks = 0;
                    }

                    // This is the price table provided to us:
                    // https://3.basecamp.com/3293895/buckets/37470979/messages/8512024554#__recording_8537340428
                    // Miami       Shell White            (MSW)    TKF648-DA         273.00
                    // Miami       Pitch Black            (MPB)    TKF648-PB         242.00
                    // Miami       Sapphire Blue          (MSB)    TKF648-SB         242.00
                    // Miami       Shoreline Gray         (MSG)    TKF648-SG         242.00
                    // Miami       Dark Ash               (MDA)    TKF648-SW         242.00
                    // Miami       Teak                   (MTK)    TKF648-TK         273.00
                    // Sanibel     Shell White            (SSW)    TKF696-DA         545.00
                    // Sanibel     Pitch Black            (SPB)    TKF696-PB         483.00
                    // Sanibel     Sapphire Blue          (SSB)    TKF696-SB         483.00
                    // Sanibel     Shoreline Gray         (SSG)    TKF696-SG         483.00
                    // Sanibel     Dark Ash               (SDA)    TKF696-SW         483.00
                    // Sanibel     Teak                   (STK)    TKF696-TK         545.00

                    JsonDict toeKickEntry =
                        CreateManualPriceSheetEntry(
                            productSku: $"TKF696",
                            productName: $"96\"W x 6\"H Toe Kick/Filler with 8 Clips",
                            price: 483.0f   // This is the most common price listed for 96" toe kick.
                        );

                    if(isEndura)
                    {
                        toeKickEntry = CreateManualPriceSheetEntry(
                            productSku: $"EN-TK96",
                            productName: $"96\"W x 5\"H Toe Kick/Filler",
                            price: 161.0f
                        );
                    }

                    for (int i = 0; i < numberOfToeKicks; ++i)
                    {
                        manualPriceSheetEntries.Add(toeKickEntry);
                    }
                }
            }

            List<string> extraFeatures = new List<string>();
            foreach (KitchenExtraFeature feature in m_SelectedExtraFeatures)
            {
                extraFeatures.Add(feature.ToString());
            }

            // Attach the list of reverse-discounts. E.g., Challenger's
            // Shaker cabinets have a 5% surcharge per cabinet.
            List<object> priceChanges = new List<object>();
            foreach ((int productId, float rate) in productIdToPriceChangeFactor)
            {
                priceChanges.Add(
                    new Dictionary<string, object>()
                    {
                        { "product_id", productId },
                        { "rate", rate }
                    }
                );
            }

            IDictionary<string, object> saveJson = new Dictionary<string, object>()
            {
                { "islandType", m_CurrentCabinetLayout.displayName },   // used by server
                { "islandDimensions", string.Join("\n", dimensions) },  // used by server
                { "cabinetFinish", cabinetFinishProductSku },           // used by server
                { "counterTopFinish", countertopFinishProductSku },     // used by server
                { "deckFinish", deckFinishProductSku },                 // used by server
                { "paverFinish", paverFinishProductSku },               // used by server
                { "modelSaveData", modelSaveDataJson },                 // used by server
                { "counterTopHeight", counterTopHeight },               // used by server

                { "top_measurements", topMeasurements },                // used by server
                { "front_measurements", frontMeasurements },            // used by server
                { "cabinet_list", cabinetIdList },                      // used by server
                { "manual_price_sheet_entries", manualPriceSheetEntries },// used by server
                { "countertop_area", countertopArea },                  // used by server

                { "price_changes", priceChanges },                      // used by server

                { "kitchen_layout_style", m_CurrentCabinetLayout.style.ToString() },
                { "kitchen_building_method", m_SelectedBuildingMethod.ToString() },
                { "countertop_type", m_SelectedCountertopType.ToString() },
                { "toe_kick_type", m_SelectedToeKickType.ToString() },
                { "cabinets", cabinetList },
                { "cabinet_spans", cabinetSpanList },
                { "base_cabinet_depth_in_meters", m_SelectedBaseCabinetDepthInMeters },
                { "countertop_thickness_in_meters", m_SelectedCountertopThicknessInMeters },
                { "extra_features", extraFeatures },

                // These values aren't used anymore by us but we'll send them
                // to the server in case they're used there
                { "islandEnum", 0 },
                { "backplashLeftActive", false },
                { "backsplashUpperActive", false },
                { "backsplashRightActive", false },
                { "barTopLeftActive", false },
                { "barTopUpperActive", false },
                { "barTopRightActive", false },
                { "leftLength", false },
                { "upperLength", false },
                { "rightLength", false },
                { "cabinetDepthLeft", 0 },
                { "cabinetDepthUpper", 0 },
                { "cabinetDepthRight", 0 },
                { "cabinetDepthInitial", 0 },
            };

            if (m_AnalyticsKey != null)
            {
                string analyticsKey = string.IsNullOrEmpty(m_AnalyticsKey) ? null : m_AnalyticsKey;
                saveJson.Add("analytics_key", analyticsKey);
            }

            // Add an optional "wants to be contacted" flag (exclusively used
            // by the Napoleon catalog in white label mode).

            if (ProFitGlobals.activeClientWantsToBeContacted.HasValue)
            {
                saveJson.Add("wants_to_be_contacted", ProFitGlobals.activeClientWantsToBeContacted.Value);
            }

            // Add an optional disclaimer that will appear on the PDF price
            // sheet.
            // https://3.basecamp.com/3293895/buckets/37470979/messages/8820074401#__recording_8823524482
            string disclaimer = null;

            if (m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog)
            {
                if (m_SelectedBrandCatalog.ruleset == BrandCatalogRuleset.Napoleon)
                {
                    disclaimer = "Countertop not included on this final quote.";
                }
            }
            else
            {
                disclaimer = ProFitGlobals.appRules.NonCatalogQuoteDisclaimer;
            }

            if (!string.IsNullOrWhiteSpace(disclaimer))
            {
                saveJson.Add("disclaimer", disclaimer);
            }

            if (selectedBuildingMethod == KitchenBuildingMethod.Sections)
            {
                // Stoll wants their cabinet finishes displayed in linear
                // feet.
                string finishLinearDistance =
                    ProFitGlobals.userProfile.activeUnits == MeasurementUnit.FeetAndInches ?
                        cabinetLinearMeters * Units.Meters2Inches * InchesToFeet + " ft" :
                        cabinetLinearMeters + " m";

                saveJson["cabinetFinishArea"] = finishLinearDistance;
            }
            else if (cabinetSquareMeters > 0.0f)
            {
                string finishSquareArea =
                    ProFitGlobals.userProfile.activeUnits == MeasurementUnit.FeetAndInches ?
                        cabinetSquareMeters * (Units.Meters2Inches * Units.Meters2Inches) * (InchesToFeet * InchesToFeet) + " ft�" :
                        cabinetSquareMeters + " m�";

                saveJson["cabinetFinishArea"] = finishSquareArea;
            }

            if (toeKickLinearMeters > 0.0f
                && canHaveToeKickEntry)
            {
                string toeKickLinearDistance =
                    ProFitGlobals.userProfile.activeUnits == MeasurementUnit.FeetAndInches ?
                        Utility.MeasurementFormat.FormatMetersToImperial(toeKickLinearMeters) :
                        Utility.MeasurementFormat.FormatMetersToCentimeters(toeKickLinearMeters);

                saveJson.Add("toe_kick_linear_distance", toeKickLinearDistance);
            }


            if (m_SelectedBrandCatalog)
            {
                saveJson.Add("brand_catalog", m_SelectedBrandCatalog.catalogName);
                saveJson.Add("brand_catalog_id", m_SelectedBrandCatalog.catalogId);
                saveJson.Add("door_style_name", m_SelectedDoorStyleName);
                saveJson.Add("door_handle_style_name", m_SelectedDoorHandleStyleName);
                saveJson.Add("end_panel_width_left_in_meters", m_EndPanelWidthInMetersLeft);
                saveJson.Add("end_panel_width_right_in_meters", m_EndPanelWidthInMetersRight);
                saveJson.Add("back_panel_depth_in_meters", m_BackPanelDepthInMeters);
            }


            //string saveContent = MiniJSON.Json.Serialize(saveJson, prettyPrint: true);
            //string tempSavePath = System.IO.Path.GetFullPath(
            //    System.IO.Path.Combine(Application.dataPath, "..", "tempsave.json")
            //);
            //System.IO.File.WriteAllText(tempSavePath, saveContent);
            //MelcherLog.Log("Wrote temp save to: " + tempSavePath);

            return saveJson;
        }

        protected bool CanAddFridgeBackPanelToPriceSheet()
        {
            return new int[]{
                Const.Gemini.ManufactersIds.AmericanOutdoorCabinets
                ,Const.Gemini.ManufactersIds.Endura
                }
                .Contains(selectedBrandCatalog.cabinetManufacturerId) == false;
        }

        protected JsonDict TryGetCountertopPricing()
        {
            if(selectedCountertopFinish == null)
            {
                return new Dictionary<string, object>()
                {
                    { "no_countertop", string.Empty }
                };
            }

            bool hasFoundFinishManufacturer = ProFitGlobals.catalogData.FindManufacturerById(selectedCountertopFinish.manufacturerId, out ManufacturerInfo countertopFinishManufacturer);

            if (hasFoundFinishManufacturer &&
                countertopFinishManufacturer.id == selectedBrandCatalog.cabinetManufacturerId)
            {
                // Add Challenger countertops to the price sheet.

                float usedPrice = selectedCountertopFinish.priceCustomer;

                const int ChallengerBrushedStainlessFinishId = 1960;

                float backsplashPricePerLinearInch =
                    m_SelectedCountertopFinish.id == ChallengerBrushedStainlessFinishId ?
                        12.0f : 5.0f;

                float highBarTopPricePerLinearInch =
                    m_SelectedCountertopFinish.id == ChallengerBrushedStainlessFinishId ?
                        63.0f : 40.0f;

                // Challenger uses a linear length formula.
                // https://3.basecamp.com/3293895/buckets/37470979/messages/9033213434
                float lastSpanDepthInMeters = 0.0f;
                float lastSpanBackingDepth = 0.0f;
                float totalLinearPricing = 0.0f;

                foreach (CabinetSpan span in layout.spans)
                {
                    Cuboid outerCuboid =
                        span.GetOutsideCuboidInSpanSpace(includeCountertop: true);

                    if (span.IsSpace())
                    {
                        lastSpanDepthInMeters = 0.0f;
                        lastSpanBackingDepth = 0.0f;
                        continue;
                    }

                    // Exclude any corner distance overlapped with the
                    // previous span.

                    float backEdgeLinearLengthInMeters =
                        outerCuboid.width - lastSpanDepthInMeters;

                    lastSpanDepthInMeters = outerCuboid.depth;

                    float inchesWide = backEdgeLinearLengthInMeters * Units.Meters2Inches;

                    totalLinearPricing += inchesWide * usedPrice;

                    // Include high bar tops as a separate linear
                    // length. Any corner distance that overlaps with
                    // a neighbouring cabinet span is only counted once.

                    float spanBackingDepth = 0.0f;

                    if (span.backing && span.backing.info.category != ProductCategory.BarTopLow)
                    {
                        float backingPricePerLinearInch =
                            span.backing.info.category == ProductCategory.Backsplash ?
                                backsplashPricePerLinearInch :
                                highBarTopPricePerLinearInch;

                        Cuboid backingCuboid =
                            span.backing.GetCountertopCuboidInSpanSpace(default);

                        spanBackingDepth = backingCuboid.depth;

                        totalLinearPricing += (backingCuboid.width - lastSpanBackingDepth) * Units.Meters2Inches * backingPricePerLinearInch;
                    }

                    lastSpanBackingDepth = spanBackingDepth;
                }

                return new Dictionary<string, object>()
                    {
                        { "product_id", selectedCountertopFinish.sku },
                        { "name", selectedCountertopFinish.name },
                        { "price", totalLinearPricing }
                    };
            }
            else
            {
                return new Dictionary<string, object>()
                    {
                        { "no_countertop", string.Empty }
                    };
            }
        }

        protected void AddOccupantsToJson(JsonDict volumeEntry, ReadOnlyCollection<Occupant> occupants)
        {
            JsonList volumeOccupants = new List<object>();
            volumeEntry.Add("occupants", volumeOccupants);

            foreach (Occupant occupant in occupants)
            {
                Vector3 occupantLocalPosition = occupant.GetLocalPositionInVolume();
                JsonDict localPosition = new Dictionary<string, object>()
                {
                    { "x", occupantLocalPosition.x },
                    { "y", occupantLocalPosition.y },
                    { "z", occupantLocalPosition.z }
                };

                JsonDict occupantEntry = new Dictionary<string, object>()
                {
                    { "product_server_id", occupant.productInfo.id },
                    { "product_id", occupant.productInfo.sku },
                    { "local_position", localPosition }
                };

                if (occupant.isLockedToCabinet)
                {
                    occupantEntry.Add("is_locked_to_cabinet", true);
                }

                Vector3 occupantCutOutSize = occupant.currentCutOutSizeInMeters;
                Vector3 defaultCutOutSize = occupant.productInfo.cutOutSizeInMeters;
                if (occupantCutOutSize != defaultCutOutSize)
                {
                    // We only need (and intentionally want) to record an
                    // occupant's cut-out size if it differs from the default
                    // cut-out size. By not recording cut-out sizes when
                    // they're the same as the default, we allow app admins to
                    // change a product's cut-out size in the backend and
                    // have it be correctly updated when a design is
                    // loaded.

                    JsonDict cutOutSize = new Dictionary<string, object>()
                    {
                        { "x", occupantCutOutSize.x },
                        { "y", occupantCutOutSize.y },
                        { "z", occupantCutOutSize.z }
                    };

                    occupantEntry.Add("cut_out_size_in_meters", cutOutSize);
                }

                if (occupant.TryGetComponent(out OccupiableVolume nestedVolume) &&
                    nestedVolume.occupants.Count > 0)
                {
                    AddOccupantsToJson(occupantEntry, nestedVolume.occupants);
                }

                volumeOccupants.Add(occupantEntry);
            }
        }

        public async UniTask<bool> LoadFromJson(JsonDict saveJson)
        {
            MelcherJsonObjectParser parser = new MelcherJsonObjectParser(saveJson);

            parser
                .GetValue("cabinetFinish", out string cabinetFinishProductSku)
                .GetValue("counterTopFinish", out string countertopFinishProductSku)
                .GetValue("deckFinish", out string deckFinishProductSku)
                .GetValue("paverFinish", out string paverFinishProductSku)
                .GetValue("kitchen_layout_style", out string kitchenLayoutStyleName)
                .GetValue("kitchen_building_method", out string kitchenBuildingMethodName, defaultValue: "")
                .GetValue("brand_catalog", out string brandCatalogName, defaultValue: null)
                .GetValue("brand_catalog_id", out int brandCatalogId, defaultValue: -1)
                .GetValue("door_style_name", out m_SelectedDoorStyleName, defaultValue: null)
                .GetValue("door_handle_style_name", out m_SelectedDoorHandleStyleName, defaultValue: "")
                .GetValue("end_panel_width_left_in_meters", out m_EndPanelWidthInMetersLeft, defaultValue: 0f)
                .GetValue("end_panel_width_right_in_meters", out m_EndPanelWidthInMetersRight, defaultValue: 0f)
                .GetValue("back_panel_depth_in_meters", out m_BackPanelDepthInMeters, defaultValue: 0f)
                .GetValue("countertop_type", out string countertopTypeName, defaultValue: "")
                .GetValue("toe_kick_type", out string toeKickType, defaultValue: "")
                .GetValue("base_cabinet_depth_in_meters", out float baseCabinetDepthInMeters)
                .GetValue("countertop_thickness_in_meters", out float countertopThicknessInMeters)
                .GetValue("analytics_key", out m_AnalyticsKey, defaultValue: null)
                .GetList("cabinets", out JsonList cabinetDataList)
                .GetList("cabinet_spans", out JsonList cabinetSpanList, errorIfNotFound: false)
                .GetList("extra_features", out JsonList extraFeaturesList, errorIfNotFound: false);

            if (parser.hasErrors)
            {
                MelcherLog.LogError(ProFitWeb.CombineParserErrorsIntoException(parser).Message, this);
                return false;
            }

            // Try to re-use the build method from the save. Saves that were
            // created before the Sections building method was rolled out won't
            // have this field and will be treated using the currently selected
            // building method.
            EnumInfo<KitchenBuildingMethod>.TryParse(
                kitchenBuildingMethodName,
                out m_SelectedBuildingMethod,
                defaultValue: KitchenBuildingMethod.Standard
            );

            if (m_SelectedBuildingMethod == 0)
            {
                m_SelectedBuildingMethod = KitchenBuildingMethod.Standard;
            }

            // The countertop overhang isn't saved, so reconstruct it from other
            // values.
            float countertopOverhangInMeters = 1.0f * Units.Inches2Meters;

            if (m_SelectedBuildingMethod == KitchenBuildingMethod.Sections)
            {
                // Stoll cabinets use 0.5" countertop overhang instead of 1".
                countertopOverhangInMeters *= 0.5f;
            }

            m_SelectedBrandCatalog = null;

            bool isCatalogMode = m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog;
            bool hasCatalogName = !string.IsNullOrEmpty(brandCatalogName);
            if (isCatalogMode != hasCatalogName)
            {
                MelcherLog.LogError($"Invalid save data: isCatalogMode={isCatalogMode}, hasCatalogName={hasCatalogName}", this);
                return false;
            }

            if (isCatalogMode)
            {
                // Recover the previously selected brand catalog.

                if (brandCatalogId >= 0)
                {
                    // Newer saves store the catalog by ID (and name, but
                    // prefer ID).
                    foreach (BrandCatalog brandCatalog in ProFitGlobals.runtimeBrandCatalogs)
                    {
                        if (brandCatalog.catalogId == brandCatalogId)
                        {
                            m_SelectedBrandCatalog = brandCatalog;
                            EventBridge.Designer.refreshCabinetLoadingUiImage();
                            break;
                        }
                    }
                }

                if (!m_SelectedBrandCatalog)
                {
                    // Older saves only store the catalog's name.

                    foreach (BrandCatalog brandCatalog in ProFitGlobals.runtimeBrandCatalogs)
                    {
                        if (brandCatalog.catalogName == brandCatalogName)
                        {
                            m_SelectedBrandCatalog = brandCatalog;
                            break;
                        }
                    }
                }

                if (!m_SelectedBrandCatalog)
                {
                    MelcherLog.LogError($"Could not find brand catalog with " +
                        $"name: {MelcherFormat.ConsoleString(brandCatalogName)} " +
                        $"and ID: {MelcherFormat.ConsoleNumericValue(brandCatalogId)}",
                        context: this
                    );
                    return false;
                }
            }

            m_SelectedCountertopOverhangInMeters =
                new CuboidPadding()
                {
                    left = countertopOverhangInMeters,
                    right = countertopOverhangInMeters,
                    towardMinZ = countertopOverhangInMeters,
                    towardMaxZ = countertopOverhangInMeters,
                    top = 0.0f,
                    bottom = 0.0f
                };

            // Re-generate the runtime product infos. These are dependent on
            // the design's selected building method (and catalog, if it
            // exists) so therefore they need to be refreshed before we begin
            // populating the kitchen.
            EventBridge.Designer.onBuildingMethodChanged?.Invoke(m_SelectedBuildingMethod);

            await EventBridge.Designer.onBrandCatalogChanged.InvokeIfNonEmpty(m_SelectedBrandCatalog);

            m_SelectedCabinetFinish = ProFitGlobals.catalogData.FindProductInfoBySku(cabinetFinishProductSku, findCabinetSection: false);
            m_SelectedCountertopFinish = ProFitGlobals.catalogData.FindProductInfoBySku(countertopFinishProductSku, findCabinetSection: false);

            string flooringId = !string.IsNullOrEmpty(deckFinishProductSku) ? deckFinishProductSku : paverFinishProductSku;
            m_SelectedFlooringFinish = ProFitGlobals.catalogData.FindProductInfoBySku(flooringId, findCabinetSection: false);

            m_SelectedBaseCabinetDepthInMeters = baseCabinetDepthInMeters;
            m_SelectedCountertopThicknessInMeters = countertopThicknessInMeters;

            // Convert the style name to a cabinet layout prefab
            // TODO: Move the prefab array to another script
            CabinetLayout cabinetLayoutPrefab =
                System.Array.Find(
                    m_LayoutPrefabs,
                    layout => layout.style.ToString() == kitchenLayoutStyleName
                );

            if (cabinetLayoutPrefab == null)
            {
                throw new System.InvalidOperationException($"Couldn't find cabinet layout prefab for layout {kitchenLayoutStyleName}");
            }

            m_SelectedCabinetLayout = cabinetLayoutPrefab;
            await CreateCabinetLayoutFromShape(m_SelectedCabinetLayout);

            // Try to load the countertop top from the save. Saves that were
            // created before the Sections building method was published won't
            // have this field and will be treated as using the default
            // countertop type.
            EnumInfo<CountertopType>.TryParse(
                countertopTypeName,
                out m_SelectedCountertopType,
                defaultValue: CountertopType.Default
            );

            // Try to load the toe kick type. Saves that were created before
            // Catalog building method was published won't have this field and
            // will default to not having a toe kick.
            EnumInfo<CabinetToeKickType>.TryParse(
                toeKickType,
                out m_SelectedToeKickType,
                defaultValue: CabinetToeKickType.None
            );

            bool remakeCabinets =
                m_SelectedBuildingMethod == KitchenBuildingMethod.Sections ||
                m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog;

            if (string.IsNullOrEmpty(kitchenBuildingMethodName))
            {
                // This is an old save made before building methods existed.
                // Force remaking the cabinets off because the save doesn't have
                // all the necessary info to create them from scratch.
                remakeCabinets = false;
            }

            if (remakeCabinets)
            {
                // Destroy all cabinets and remake them. This is necessary to:
                // - Set up all extra cabinets the player may have created.
                // - Reuse the CabinetLayout object to keep all its camera
                //   angle values.

                m_CurrentCabinetLayout.DestroyCabinets();
            }

            // First pass: Restore the length of the cabinets
            for (int i = 0; i < cabinetDataList.Count; ++i)
            {
                Utility.MemoryUtility.GarbageCollectOnLowMemoryDevices();

                JsonDict cabinetData = (JsonDict)cabinetDataList[i];
                parser.Reset(cabinetData)
                    .GetValue("size_in_meters_x", out float cabinetSizeInMetersX);

                if (parser.hasErrors)
                {
                    MelcherLog.LogError($"Error reading cabinet index {i}: " + ProFitWeb.CombineParserErrorsIntoException(parser).Message);
                    continue;
                }

                Cabinet cabinet = null;
                ProductInfo cabinetProductInfo = null;

                // Nullables. If they are null, the default values will be
                // used.
                Vector3? cabinetMinSizeInMeters = null;
                Vector3? cabinetMaxSizeInMeters = null;
                Vector3? cabinetLocalEulerAngles = null;

                if (remakeCabinets)
                {
                    Vector3 normalizedAnchorToPreviousSection = Vector3.zero;

                    Vector3 minSizeInMeters = Vector3.zero;
                    Vector3 maxSizeInMeters = Vector3.zero;
                    Vector3 localEulerAngles = Vector3.zero;

                    // Learn what type of cabinet needs to be created.
                    parser
                        .GetValue("cabinet_type", out string cabinetTypeName)
                        .GetAndPushObject("normalized_anchor_to_previous_section")
                            .GetValue("x", out normalizedAnchorToPreviousSection.x)
                            .GetValue("y", out normalizedAnchorToPreviousSection.y)
                            .GetValue("z", out normalizedAnchorToPreviousSection.z)
                        .PopObject()
                        .GetAndPushObject("local_euler_angles")
                            .GetValue("x", out localEulerAngles.x)
                            .GetValue("y", out localEulerAngles.y)
                            .GetValue("z", out localEulerAngles.z)
                        .PopObject()
                        .GetAndPushObject("min_size_in_meters")
                            .GetValue("x", out minSizeInMeters.x)
                            .GetValue("y", out minSizeInMeters.y)
                            .GetValue("z", out minSizeInMeters.z)
                        .PopObject()
                        .GetAndPushObject("max_size_in_meters")
                            .GetValue("x", out maxSizeInMeters.x)
                            .GetValue("y", out maxSizeInMeters.y)
                            .GetValue("z", out maxSizeInMeters.z)
                        .PopObject()
                        .GetValue("backing_sprite", out string cabinetBackingSpriteName)
                        .GetValue("cabinet_server_id", out int cabinetId, defaultValue: 0)
                        .GetValue("cabinet_id", out string cabinetSku, defaultValue: null);

                    if (parser.hasErrors)
                    {
                        MelcherLog.LogError($"Error remaking cabinet index {i}: " + ProFitWeb.CombineParserErrorsIntoException(parser).Message);
                        continue;
                    }

                    CabinetModelType cabinetModelType = CabinetModelType.Procedural;

                    if (cabinetId != 0 || !string.IsNullOrEmpty(cabinetSku))
                    {
                        // Try to lookup the cabinet using its permanent ID
                        // first (if available), then fallback to its SKU. The
                        // SKU may have changed so if the SKU lookup fails, try
                        // looking at the database of known SKUs to product IDs

                        if (cabinetId > 0)
                        {
                            cabinetProductInfo =
                                ProFitGlobals.catalogData.FindProductInfoById(
                                    cabinetId,
                                    findCabinetSection: true
                                );
                        }

                        if (cabinetProductInfo == null)
                        {
                            cabinetProductInfo =
                                ProFitGlobals.catalogData.FindProductInfoBySku(
                                    cabinetSku,
                                    findCabinetSection: true
                                );
                        }

                        if (cabinetProductInfo == null)
                        {
                            int? remappedCabinetId =
                                ProFitGlobals.appOptions.remappedProductIdDatabase.Lookup(cabinetSku);

                            if (remappedCabinetId.HasValue)
                            {
                                cabinetProductInfo =
                                    ProFitGlobals.catalogData.FindProductInfoById(
                                        remappedCabinetId.Value,
                                        findCabinetSection: true
                                    );
                            }
                        }

                        if (cabinetProductInfo == null)
                        {
                            MelcherLog.LogError($"Failed to find cabinet info for cabinet with SKU: {cabinetSku}");
                            continue;
                        }

                        if (!string.IsNullOrEmpty(cabinetProductInfo.visualId))
                        {
                            ProductVisual cabinetVisual =
                                productVisualDatabase.FindRuntimeVisualByID(
                                    cabinetProductInfo.visualId
                                );

                            if (cabinetVisual == null)
                            {
                                MelcherLog.LogError($"Failed to find product visual with ID: {cabinetProductInfo.visualId}");
                                continue;
                            }

                            cabinetModelType = cabinetVisual.info.sectionModelType;
                        }
                    }

                    cabinetMinSizeInMeters = minSizeInMeters;
                    cabinetMaxSizeInMeters = maxSizeInMeters;
                    cabinetLocalEulerAngles = localEulerAngles;

                    if (!System.Enum.TryParse(cabinetTypeName, ignoreCase: true, out CabinetType cabinetType))
                    {
                        if (cabinetTypeName == "CornerColumnRight")
                        {
                            cabinetType = CabinetType.StraightOnRightOfCorner;
                        }
                        else if (cabinetTypeName == "CornerColumnLeft")
                        {
                            cabinetType = CabinetType.StraightOnLeftOfCorner;
                        }
                        else
                        {
                            MelcherLog.LogError($"Error remaking cabinet index {i}: Unknown cabinet type '{cabinetTypeName}'");
                            continue;
                        }
                    }

                    Cabinet cabinetPrefab =
                        FindCabinetPrefab(
                            cabinetType,
                            cabinetModelType
                        );

                    if (!cabinetPrefab)
                    {
                        MelcherLog.LogError($"Error remaking cabinet index {i}: Cannot find prefab for cabinet type '{cabinetTypeName}'");
                        continue;
                    }

                    cabinet =
                        await m_CurrentCabinetLayout.InstantiateCabinet(
                            cabinetPrefab,
                            cabinetProductInfo,
                            kitchen: this
                        );

                    cabinet.SetToeKickType(m_SelectedToeKickType);

                    cabinet.normalizedAnchorToPreviousSection = normalizedAnchorToPreviousSection;

                    cabinet.backingSprite =
                        System.Array.Find(
                            m_CabinetBackingSprites,
                            sprite => sprite && sprite.name == cabinetBackingSpriteName
                        );
                }
                else
                {
                    cabinet = m_CurrentCabinetLayout.GetCabinetAtIndex(i);
                }

                parser.currentObject["exists"] = true;

                if (cabinet.cabinetType == CabinetType.Corner)
                {
                    // FIXME: This bit of code is intended to fix saves that
                    //        were generated with corner pieces that are too
                    //        long (due to bar top). In the future, this won't
                    //        be necessary because the save files do not
                    //        generate corners with the incorrect size anymore
                    cabinetSizeInMetersX = baseCabinetDepthInMeters;
                }

                // Apply the euler angles before adjustnig the cabinets size.
                // Otherwise, the cabinet span rebuild that happens when a
                // cabinet's size is changed will be unaware of this rotation
                // and create "phantom" spans with the last cabinet.
                if (cabinetLocalEulerAngles.HasValue)
                {
                    cabinet.GetTransform().localEulerAngles = cabinetLocalEulerAngles.Value;
                }

                Vector3 sizeInMeters = cabinet.sizeInMeters;
                sizeInMeters.x = cabinetSizeInMetersX;
                if (cabinetProductInfo != null)
                {
                    // Restore cabinets that use different heights. E.g.,
                    // Stoll's "Adjustable Smoker Cabinets"
                    sizeInMeters.y = cabinetProductInfo.productSizeInMeters.y;
                }
                else if (m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                    cabinet.Is(CabinetType.Corner, CabinetModelType.Procedural))
                {
                    // Restore the height of procedural corners in Catalog
                    // building method. Otherwise, those cabinets may be a
                    // different height from all the other authored cabinets.
                    sizeInMeters.y = m_SelectedBrandCatalog.standardCabinetHeightInMeters;
                }

                sizeInMeters.z = m_SelectedBaseCabinetDepthInMeters;
                cabinet.sizeInMeters = sizeInMeters;

                if (cabinet.cabinetType.IsHeightTransition() &&
                    m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                    m_SelectedBrandCatalog != null &&
                    m_SelectedBrandCatalog.cabinetSectionLegStyle)
                {
                    // Restore height transition cabinet's offset from the
                    // ground.
                    cabinet.cabinet.transform.position +=
                        Vector3.up * m_SelectedBrandCatalog.cabinetSectionLegStyle.heightInMeters;
                }

                cabinet.SetCountertopThickness(countertopThicknessInMeters);
                cabinet.SetCountertopOverhang(m_SelectedCountertopOverhangInMeters);

                if (cabinetMinSizeInMeters.HasValue)
                {
                    cabinet.minSizeInMeters = cabinetMinSizeInMeters.Value;
                }

                if (cabinetMaxSizeInMeters.HasValue)
                {
                    cabinet.maxSizeInMeters = cabinetMaxSizeInMeters.Value;
                }
            }

            // Reattach corner wedges to their intended corners.
            for (int i = 0; i < cabinetDataList.Count; ++i)
            {
                JsonDict cabinetData = (JsonDict)cabinetDataList[i];

                parser.Reset(cabinetData)
                    .GetValue("index_of_cabinet_being_wedged", out int indexOfCabinetBeingWedged, defaultValue: -1);

                if (indexOfCabinetBeingWedged < 0)
                {
                    continue;
                }

                Cabinet cabinetWedge = layout.GetCabinetAtIndex(i);

                cabinetWedge.cornerCabinetBeingWedged =
                    layout.GetCabinetAtIndex(indexOfCabinetBeingWedged);

                Vector3 sizeInMeters = cabinetWedge.sizeInMeters;
                sizeInMeters.z = sizeInMeters.x;
                cabinetWedge.sizeInMeters = sizeInMeters;
            }

            // Set the base cabinet depth. We do this here (for all spans)
            // before setting customized per-span depths in a moment to avoid
            // overwriting the custom depths.
            SetBaseCabinetDepth(m_SelectedBaseCabinetDepthInMeters);

            // Handle adding backings now that the cabinets are properly sized.
            // Backings are recorded in the cabinet spans but they may be
            // present in the cabinet data if this is an old save.
            if (cabinetSpanList != null)
            {
                for (int i = 0; i < cabinetSpanList.Count; ++i)
                {
                    JsonDict cabinetSpanData = (JsonDict)cabinetSpanList[i];

                    parser
                        .PushObject(i, cabinetSpanData)
                            .GetValue("index", out int cabinetSpanIndex)
                            .GetValue("backing_product_id", out string backingProductSku)
                            .GetValue("depth_in_meters", out float depthInMeters, defaultValue: 0.0f)
                        .PopObject();

                    if (depthInMeters > 0.0f)
                    {
                        CabinetSpan span = layout.spans[cabinetSpanIndex];
                        span.SetBaseCabinetDepth(depthInMeters);
                    }

                    if (backingProductSku != null)
                    {
                        await LoadBackingFromProductSku(
                            cabinetSpanIndex,
                            backingProductSku
                        );
                    }
                }
            }

            // Read the kitchen extras. These are simple flags used to
            // represent highly specific features to a certain kitchen. E.g.,
            // Challenger Design's "Premium Finish Package".
            m_SelectedExtraFeatures.Clear();
            if (extraFeaturesList != null)
            {
                for (int i = 0; i < extraFeaturesList.Count; ++i)
                {
                    if (!(extraFeaturesList[i] is string extraFeatureName))
                    {
                        continue;
                    }

                    if (!System.Enum.TryParse(extraFeatureName, out KitchenExtraFeature extraFeature))
                    {
                        MelcherLog.LogError(
                            $"Could not parse {MelcherFormat.ConsoleTypeName<KitchenExtraFeature>()} from " +
                            $"{MelcherFormat.ConsoleString(extraFeatureName)}"
                        );
                        continue;
                    }

                    m_SelectedExtraFeatures.Add(extraFeature);
                }
            }

            // Apply the saved back panel depth. Cabinets do not serialize their
            // individual depths so this is applied as an offset from their
            // current depth.
            if (!MelcherCommon.IsAlmostEqual(m_BackPanelDepthInMeters, 0.0f, 0.001f))
            {
                ApplyDeltaToAllCabinetDepths(m_BackPanelDepthInMeters);
            }

            m_CurrentCabinetLayout.UpdatePositions();
            m_CurrentCabinetLayout.UpdateMeshes();

            if (remakeCabinets)
            {
                RecreateResizeWidgets();
                UpdateCameraAndFramingGroups();
            }

            // Force the garbage collector to run to free up memory (iOS).
            // Without this, low-memory devices may run out of memory.
            Utility.MemoryUtility.GarbageCollectOnLowMemoryDevices();

            // Second pass: Restore the occupants
            for (int i = 0; i < cabinetDataList.Count; ++i)
            {
                JsonDict cabinetData = (JsonDict)cabinetDataList[i];

                Cabinet cabinet = m_CurrentCabinetLayout.GetCabinetAtIndex(i);

                await LoadOccupantsFromJson(parser, cabinetData, cabinet, requireOccupants: true);
            }

            // Force the garbage collector to run to free up memory (iOS).
            // Without this, low-memory devices may run out of memory.
            Utility.MemoryUtility.GarbageCollectOnLowMemoryDevices();

            // Restore the cabinet materials
            (Material cabinetMaterial, Material countertopMaterial) =
                await GetFinishMaterials(allowHighlightToReturn: false);

            await m_CurrentCabinetLayout.SetCabinetMaterials(
                cabinetMaterial,
                countertopMaterial,
                await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
            );

            // Restore the flooring finish
            await ChangeFinish(m_SelectedFlooringFinish, ProductCategory.FinishFloor);

            CenterTheGround();

            if(ProFitGlobals.appRules.ReappliesCountertopThicknessAfterLoad)
            {
                SetCountertopThickness(m_SelectedCountertopThicknessInMeters);
            }            

            // Try to refresh the cabinet layout (to fix an issue where the
            // ends of some cabinets are not sized correctly).
            m_CurrentCabinetLayout.UpdateMeshes();
            m_CurrentCabinetLayout.UpdatePositions();

            RefreshTickerLabels();

            // The kitchen is ready.
            await EventBridge.Designer.onKitchenReady.InvokeIfNonEmpty();

            return true;
        }

        protected async UniTask LoadOccupantsFromJson(MelcherJsonObjectParser parser, JsonDict volumeData, OccupiableVolume volume, bool requireOccupants)
        {
            parser.Reset(volumeData)
                .GetValue("exists", out bool cabinetExists, defaultValue: false)
                .GetList("occupants", out JsonList volumeOccupants);

            if (parser.hasErrors)
            {
                if (requireOccupants)
                {
                    MelcherLog.LogError($"Error reading occupants in volume: " + ProFitWeb.CombineParserErrorsIntoException(parser).Message);
                }

                return;
            }

            if (!cabinetExists)
            {
                // There were errors recreating the cabinet earlier so we will
                // skip it now.
                return;
            }

            foreach (JsonDict occupantEntry in volumeOccupants)
            {
                parser.Reset(occupantEntry)
                    .GetValue("product_server_id", out int productId, defaultValue: 0)
                    .GetValue("product_id", out string productSku, defaultValue: null)
                    .GetValue("is_locked_to_cabinet", out bool isLockedToCabinet, defaultValue: false);

                if (string.IsNullOrEmpty(productSku) && productId == 0)
                {
                    MelcherLog.LogError($"Error reading occupant: " + ProFitWeb.CombineParserErrorsIntoException(parser).Message);
                    continue;
                }

                float occupantLocalPositionX = default;
                float occupantLocalPositionY = float.NaN;
                float occupantLocalPositionZ = float.NaN;

                if (parser.TryGetAndPushObject("local_position", out JsonDict localPosition))
                {
                    parser
                        .GetValue("x", out occupantLocalPositionX)
                        .GetValue("y", out occupantLocalPositionY)
                        .GetValue("z", out occupantLocalPositionZ);

                    parser.PopObject();
                }
                else
                {
                    // Legacy saves: just load the x-position. This was
                    // before products could be nudged up and down
                    parser.GetValue("meters_from_cabinet_min_x", out occupantLocalPositionX);
                }

                Vector3 occupantCutOutSize = default;

                if (parser.TryGetAndPushObject("cut_out_size_in_meters", out JsonDict cutOutSize))
                {
                    parser
                        .GetValue("x", out occupantCutOutSize.x)
                        .GetValue("y", out occupantCutOutSize.y)
                        .GetValue("z", out occupantCutOutSize.z);

                    parser.PopObject();
                }

                if (parser.hasErrors)
                {
                    MelcherLog.LogError($"Failed to parse occupant in volume: " + ProFitWeb.CombineParserErrorsIntoException(parser).Message);
                    continue;
                }

                ProductInfo productInfo = null;

                if (productId > 0)
                {
                    // Newer saves record their products via the server ID
                    // as this is guaranteed not to change.
                    productInfo =
                        ProFitGlobals.catalogData.FindProductInfoById(
                            productId,
                            findCabinetSection: false
                        );
                }

                if (productInfo == null)
                {
                    // If no product server ID was recorded (or no matching
                    // product was found), try to lookup the product by its
                    // SKU.
                    productInfo =
                        ProFitGlobals.catalogData.FindProductInfoBySku(
                            productSku,
                            findCabinetSection: false
                        );

                    // HACK: To temporarily fix some re-named products IDs, we will
                    //       check a local database of known renames.
                    if (productInfo == null && ProFitGlobals.appOptions.remappedProductIdDatabase)
                    {
                        int? remappedProductId =
                            ProFitGlobals.appOptions.remappedProductIdDatabase.Lookup(productSku);

                        if (remappedProductId.HasValue)
                        {
                            ProductInfo remappedProductInfo =
                                ProFitGlobals.catalogData.FindProductInfoById(
                                    remappedProductId.Value,
                                    findCabinetSection: false
                                );

                            if (remappedProductInfo != null)
                            {
                                productInfo = remappedProductInfo;
                            }
                        }
                    }
                }

                if (productInfo == null)
                {
                    MelcherLog.LogError($"Unknown product SKU: {productSku}");
                    continue;
                }

                if (productInfo.category == ProductCategory.BarTopHigh ||
                    productInfo.category == ProductCategory.Backsplash)
                {
                    // Backings were stored as occupants at one point. Try to
                    // preserve legacy designs by intercepting these backings
                    // here.

                    Cabinet cabinet = volume as Cabinet;

                    if (!cabinet)
                    {
                        MelcherLog.LogWarning(
                            $"Found a cabinet backing saved within a cabinet " +
                            $"shelf. This should never happen. Backings are " +
                            $"saved to the cabinet span or, in older builds, " +
                            $"to the root volume (cabinet), never a shelf."
                        );
                    }
                    else
                    {
                        int cabinetSpanIndex =
                            layout.FindCabinetSpanIndex(cabinet);

                        await LoadBackingFromProductSku(cabinetSpanIndex, productSku);
                    }

                    continue;
                }

                ProductVisual productVisual =
                    m_ProductVisualDatabase.FindRuntimeVisualByID(
                        productInfo.visualId
                    );

                if (productVisual == null)
                {
                    MelcherLog.LogError($"Unknown product visual. SKU: {productSku}, visual ID: {productInfo.visualId}");
                    continue;
                }

                ProductVisualContent productVisualPrefab =
                    await ProductVisualContentLoader.FirstFromActiveScene()
                        .ProductVisualToContent(productVisual);

                Occupant occupant =
                    await volume.InstantiateAndInsertOccupantAt(
                        userInitiated: false,
                        productInfo,
                        productVisual,
                        productVisualPrefab,
                        InstantiateOccupant,
                        occupantLocalPositionX,
                        float.IsNaN(occupantLocalPositionY) ? null : occupantLocalPositionY,
                        float.IsNaN(occupantLocalPositionZ) ? null : occupantLocalPositionZ
                    );

                occupant.isLockedToCabinet = isLockedToCabinet;

                if (occupantCutOutSize != Vector3.zero)
                {
                    occupant.SetCutOutSizeInMeters(occupantCutOutSize);
                }

                if (occupant.TryGetComponent(out CabinetShelf shelf))
                {
                    parser.currentObject["exists"] = true;
                    await LoadOccupantsFromJson(parser, occupantEntry, shelf, requireOccupants: false);
                }
            }
        }

        /// <summary>
        ///     Instantiates a cabinet backing for the requested cabinet span.
        ///     This function is intended to be called within <see
        ///     cref="LoadFromJson(JsonDict)"/> and its related functions.
        /// </summary>
        protected async UniTask LoadBackingFromProductSku(int cabinetSpanIndex, string backingProductSku)
        {
            CabinetSpan span = layout.spans[cabinetSpanIndex];

            if (span.backing)
            {
                MelcherLog.LogError(
                    $"Attempted to load a backing for a cabinet span that " +
                    $"already has one.\n" +
                    $"\n" +
                    $"The current backing is: {span.backing}\n" +
                    $"The extra backing backing is: {backingProductSku}",
                    span.backing
                );
                return;
            }

            ProductInfo backingProductInfo =
                ProFitGlobals.catalogData.FindProductInfoBySku(
                    backingProductSku,
                    findCabinetSection: false
                );

            if (backingProductSku == null)
            {
                MelcherLog.LogError($"Unknown backing product SKU: {backingProductSku}");
                return;
            }

            CabinetBacking backingPrefab = null;

            // TODO: We need a better way to look up these backings now that
            //       they don't exist in the product visual catalog. Whatever
            //       we do to fix this here can be used in DesignerToolbarUI
            //       too. -Ryan
            switch (backingProductInfo.category)
            {
                case ProductCategory.Backsplash:
                    backingPrefab = m_BacksplashPrefab;
                    break;

                case ProductCategory.BarTopHigh:
                    backingPrefab = m_HighBarTopPrefab;

                    if (m_SelectedBrandCatalog &&
                        m_SelectedBrandCatalog.highBarTopOverridePrefab)
                    {
                        backingPrefab = m_SelectedBrandCatalog.highBarTopOverridePrefab;
                    }
                    break;

                case ProductCategory.BarTopLow:
                    backingPrefab = m_LowBarTopPrefab;
                    break;
            }

            if (!backingPrefab)
            {
                MelcherLog.LogError(
                    $"No backing prefab associated with product category " +
                    $"{backingProductInfo.category}. Backing product SKU: " +
                    $"{backingProductSku}"
                );

                return;
            }

            VolumeOccupantResult result =
                await span.TryInstantiateBacking(
                    backingProductInfo,
                    backingPrefab
                );

            if (result.status != OccupantCuboidStatus.Fits)
            {
                MelcherLog.LogError($"Could not create backing using SKU: {backingProductSku}");
            }
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
            string topInfoPhrase = "";

            if (m_SelectedCabinetLayout)
            {
                string combinedClientName = ProFitGlobals.TryGetClientCombinedName();

                string countertopThicknessDisplayFormat = "";

                MeasurementUnit units = ProFitGlobals.userProfile.activeUnits;

                if (units == MeasurementUnit.Centimeters)
                {
                    countertopThicknessDisplayFormat =
                        Utility.MeasurementFormat.FormatMetersToCentimeters(
                            m_SelectedCountertopThicknessInMeters
                        );
                }
                else
                {
                    countertopThicknessDisplayFormat =
                    Utility.MeasurementFormat.FormatMetersToImperial(m_SelectedCountertopThicknessInMeters);
                }

                topInfoPhrase = $"{combinedClientName}. {m_SelectedCabinetLayout.displayName}. Counter-top Thickness: {countertopThicknessDisplayFormat}";
            }

            m_TopInfoLabel.phrase = topInfoPhrase;
        }

        public async UniTask CreateCabinetLayoutFromShape(CabinetLayout prefab)
        {
            ProductVisualContentLoader.FirstFromActiveScene()
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

                if (m_SelectedBuildingMethod == KitchenBuildingMethod.Standard)
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
                else if (m_SelectedBuildingMethod == KitchenBuildingMethod.Sections)
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
                            cornerColumnSizeInMeters.z = m_SelectedBaseCabinetDepthInMeters;

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
                else if (m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog)
                {
                    m_SelectedBaseCabinetDepthInMeters = m_SelectedBrandCatalog.standardCabinetDepthInMeters;

                    // In Catalog building method, the default procedural
                    // cabinets must be replaced with custom modeled versions.

                    ProductInfo fallbackCabinetSectionInfo =
                        ProFitGlobals.catalogData.FindProductInfoById(
                                m_SelectedBrandCatalog.fallbackDefaultCabinetSectionProductServerId,
                                findCabinetSection: true
                            );

                    if (fallbackCabinetSectionInfo == null)
                    {
                        MelcherLog.LogError(
                            $"No fallback cabinet product ID found for \"{m_SelectedBrandCatalog.fallbackDefaultCabinetSectionProductServerId}\".",
                            m_SelectedBrandCatalog
                        );
                    }

                    if (!ProFitGlobals.catalogData.FindBrandCatalogInfoById(selectedBrandCatalog.catalogId, out BrandCatalogInfo brandCatalogInfo))
                    {
                        MelcherLog.LogError(
                            $"No brand catalog for ID \"{m_SelectedBrandCatalog.catalogId}\".",
                            m_SelectedBrandCatalog
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
                            m_SelectedBrandCatalog.fridgeEndPanelInfo.specificPanelWidthInMeters > 0.0f)
                        {
                            Vector3 sizeInMeters = cabinet.sizeInMeters;
                            sizeInMeters.x = m_SelectedBrandCatalog.fridgeEndPanelInfo.specificPanelWidthInMeters;
                            cabinet.sizeInMeters = sizeInMeters;
                        }

                        if (cabinet.cabinetType != CabinetType.Straight)
                        {
                            SetCabinetHeight(
                                cabinet,
                                selectedBrandCatalog.standardCabinetHeightInMeters
                            );

                            cabinet.InstantiateCabinetLegs(m_SelectedBrandCatalog);
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
                                    m_SelectedBrandCatalog
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
                                m_SelectedBrandCatalog
                            );

                            if (replacementCabinetInfo == null)
                            {
                                MelcherLog.LogError(
                                    $"No fallback cabinet product ID provided for Catalog {m_SelectedBrandCatalog.name}.",
                                    m_SelectedBrandCatalog
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

                        ProductVisual replacementCabinetVisual =
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

                SetBaseCabinetDepth(m_SelectedBaseCabinetDepthInMeters);
                SetCountertopThickness(m_SelectedCountertopThicknessInMeters);
                SetCountertopOverhang(m_SelectedCountertopOverhangInMeters);

                // Move cabinets to their proper anchors
                m_CurrentCabinetLayout.UpdatePositions();

                (Material cabinetMaterial, Material countertopMaterial) =
                    await GetFinishMaterials(allowHighlightToReturn: true);

                // Refresh the cabinets materials. This ensures Napoleon (and
                // any future catalogs that have a cabinet material override)
                // will have their cabinets start with the intended materials.

                await m_CurrentCabinetLayout.SetCabinetMaterials(
                    cabinetMaterial,
                    countertopMaterial,
                    await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
                );
            }

            RefreshTopTickerInfo();

            // Forget the previous selection. (Only the kitchen will still
            // be valid and we want it to start un-selected.)
            m_CurrentHighlightObject = null;

            UpdateCameraAndFramingGroups();

            RecreateResizeWidgets();
            RecreateTickerWidgets();

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
            m_SelectedBaseCabinetDepthInMeters = depthInMeters;

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
            m_SelectedCountertopThicknessInMeters = thicknessInMeters;

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
        public async UniTask<Occupant> InstantiateOccupant(ProductInfo productInfo, ProductVisual productVisual, ProductVisualContent productVisualPrefab)
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
                await GetFinishMaterials(allowHighlightToReturn: true);

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
            m_ProductBeingDragged = null;
            m_VolumeBeingResized = null;
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
            ProductVisual productVisual,
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

            m_PrimaryHighlightMaterial = ProFitGlobals.appOptions.appSkin.primarySelectionMaterial;
            m_SecondaryHighlightMaterial = ProFitGlobals.appOptions.appSkin.secondarySelectionMaterial;

            // Assume the default material is selected for each surface type
            m_SelectedCabinetFinish     = null;
            m_SelectedCountertopFinish  = null;
            m_SelectedFlooringFinish    = null;

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

            SetCabinetShelfResizeWidgetsVisible(target: null);

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
                    CreateKitchenWidget(
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
            EventBridge.Designer.onFinishMaterialChanged                += OnToolbarFinishProductSelected;
            EventBridge.Designer.onTryRepositionOccupant                += TryRepositionProduct;
            EventBridge.Designer.onTryReorderCabinet.OnInvoke           += TryReorderCabinet;
            EventBridge.Designer.tryToggleToeKicks                      += TryToggleToeKicks;
            EventBridge.Designer.onToeKickTypeChanged                   += OnToeKickTypeChanged;
            EventBridge.Designer.centerOccupantInVolume                 += OnCenterOccupantInVolumeClicked;

            // Products
            EventBridge.Designer.onOccupantInstantiated.OnInvoke        += OnOccupantInstantiated;
            EventBridge.Designer.onOccupantAddedToCabinet.OnInvoke      += OnOccupantAddedToCabinet;
            EventBridge.Designer.onOccupantRemovedFromCabinet.OnInvoke  += OnOccupantRemovedFromCabinet;
            EventBridge.Designer.onOccupantDragBegin                    += OnOccupantDragBegin;
            EventBridge.Designer.onOccupantDrag                         += OnOccupantDrag;
            EventBridge.Designer.onOccupantDragEnd.OnInvoke             += OnOccupantDragEnd;

            EventBridge.Designer.onTryHighlightObject.OnInvoke          += TryHighlightObject;

            foreach (Transform shelfWidgetTransform in m_CabinetShelfResizeWidgetRoot)
            {
                if (shelfWidgetTransform.TryGetComponent(out ResizerWidget shelfWidget))
                {
                    shelfWidget.onResizeBegin += OnCabinetShelfResizeBegin;
                    shelfWidget.onResizeEnd += OnCabinetShelfResizeEnd;
                    shelfWidget.onResized += OnCabinetShelfResized;
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
            EventBridge.Designer.onFinishMaterialChanged                -= OnToolbarFinishProductSelected;
            EventBridge.Designer.onTryRepositionOccupant                -= TryRepositionProduct;
            EventBridge.Designer.onTryReorderCabinet.OnInvoke           -= TryReorderCabinet;
            EventBridge.Designer.tryToggleToeKicks                      -= TryToggleToeKicks;
            EventBridge.Designer.onToeKickTypeChanged                   -= OnToeKickTypeChanged;
            EventBridge.Designer.centerOccupantInVolume                 -= OnCenterOccupantInVolumeClicked;

            // Products
            EventBridge.Designer.onOccupantInstantiated.OnInvoke        -= OnOccupantInstantiated;
            EventBridge.Designer.onOccupantAddedToCabinet.OnInvoke      -= OnOccupantAddedToCabinet;
            EventBridge.Designer.onOccupantRemovedFromCabinet.OnInvoke  -= OnOccupantRemovedFromCabinet;
            EventBridge.Designer.onOccupantDragBegin                    -= OnOccupantDragBegin;
            EventBridge.Designer.onOccupantDrag                         -= OnOccupantDrag;
            EventBridge.Designer.onOccupantDragEnd.OnInvoke             -= OnOccupantDragEnd;

            EventBridge.Designer.onTryHighlightObject.OnInvoke          -= TryHighlightObject;

            foreach (Transform shelfWidgetTransform in m_CabinetShelfResizeWidgetRoot)
            {
                if (shelfWidgetTransform.TryGetComponent(out ResizerWidget shelfWidget))
                {
                    shelfWidget.onResizeBegin -= OnCabinetShelfResizeBegin;
                    shelfWidget.onResizeEnd -= OnCabinetShelfResizeEnd;
                    shelfWidget.onResized -= OnCabinetShelfResized;
                }
            }
        }

        private void Restart()
        {
            OnRestart().Forget();

            async UniTaskVoid OnRestart()
            {
                m_SessionStartTime = System.DateTime.UtcNow;

                m_TopInfoLabel.phrase = "";

                SetCabinetShelfResizeWidgetsVisible(target: null);

                if (await TryLoadRequestedSave())
                {
                    TrySendOnBrandCatalogChanged();
                    return;
                }

                SetBuildingMethod(ProFitGlobals.appOptions.defaultBuildingMethod);
                SetCountertopType(CountertopType.Default);
                m_SelectedCabinetFinish = null;
                m_SelectedCountertopFinish = null;
                m_SelectedFlooringFinish = null;
                m_SelectedCabinetLayout = null;
                m_SelectedKitchenTemplate = null;
                m_SelectedToeKickType = CabinetToeKickType.None;
                m_SelectedExtraFeatures.Clear();
                m_AnalyticsKey = "";

                // Restore the default flooring material
                await ChangeFinish(m_SelectedFlooringFinish, ProductCategory.FinishFloor);

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
                else if (m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog)
                {
                    EventBridge.Designer.showBrandCatalogUi.Invoke();
                }
                else
                {
                    EventBridge.Designer.showCabinetShapeUi.Invoke();
                }
            }
        }

        protected async UniTask<bool> TryLoadRequestedSave()
        {
            if (!ProFitGlobals.activeClientId.HasValue ||
                !ProFitGlobals.activeQuoteId.HasValue)
            {
                return false;
            }

            int clientId = ProFitGlobals.activeClientId.Value;
            int quoteId = ProFitGlobals.activeQuoteId.Value;
            if (!ProFitGlobals.TryGetClientAndQuoteIndex(clientId, quoteId, out int clientIndex, out int quoteIndex))
            {
                return false;
            }

            UserClientQuote quote = ProFitGlobals.userClients[clientIndex].quotes[quoteIndex];

            using var clearLoadingSpinner =
                MelcherDeferred.Defer(
                    onDispose: static () => EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Hide)
                );

            EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Load);

            EventBridge.Designer.setSpinnerStatusMessage?.Invoke($"Preparing {quote.saveName}");

            return await LoadFromJson(quote.saveData);
        }

        protected async UniTask CreateKitchen()
        {
            using var clearLoadingSpinner =
                MelcherDeferred.Defer(
                    onDispose: static () => EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Hide)
                );

            EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Load);

            // Assume the default material is selected for each surface type
            if (m_SelectedKitchenTemplate)
            {
                EventBridge.Designer.setSpinnerStatusMessage?.Invoke(
                    $"Preparing from {m_SelectedKitchenTemplate.displayName} template"
                );

                Dictionary<string, object> saveJson =
                    (Dictionary<string, object>)
                        MiniJSON.Json.Deserialize(m_SelectedKitchenTemplate.saveData.text);

                float selectedCountertopThicknessInMeters = m_SelectedCountertopThicknessInMeters;

                BrandCatalog selectedBrandCatalog =
                    m_SelectedBrandCatalog ?
                        ProFitGlobals.runtimeBrandCatalogs.Find(x => x.catalogId == m_SelectedBrandCatalog.catalogId) :
                        null;

                await LoadFromJson(saveJson);

                // A non-null but empty analytics key. This ensures the kitchen
                // will be tracked for analytics (as opposed to a strictly null
                // analytics key read from a pre-analytics save file).
                m_AnalyticsKey = "";

                // Ensure the user-selected values are not erased after loading
                // a template file.
                SetCountertopThickness(selectedCountertopThicknessInMeters);

                await SetBrandCatalog(selectedBrandCatalog);

                RefreshTopTickerInfo();
            }
            else
            {
                EventBridge.Designer.setSpinnerStatusMessage?.Invoke(
                    $"Preparing from {m_SelectedCabinetLayout.displayName} layout"
                );

                await CreateCabinetLayoutFromShape(m_SelectedCabinetLayout);
            }

            // A non-null, but empty analytics token. This is used to signal to
            // the server that this design needs an analytics token assigned
            // when it is next saved. Legacy saves which lack an analytics token
            // (m_AnalyticsToken == null) will not be tracked for analytics.
            m_AnalyticsKey = "";

            // Apply brand catalog's default toe kick. Currently, this is only
            // necessary to enforce Napoleon's mandatory toe kick.
            if (m_CurrentCabinetLayout && m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog)
            {
                m_SelectedToeKickType = m_SelectedBrandCatalog.defaultToeKickType;
                EventBridge.Designer.onToeKickTypeChanged?.Invoke(m_SelectedToeKickType);
            }

            // Ensure the cabinet is the client's intended outside-edge length
            // (at time of writing, that is 15", as set as initial size on
            // straight cabinet prefab)
            if (m_CurrentCabinetLayout && m_SelectedBuildingMethod == KitchenBuildingMethod.Standard)
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
                            cabinetSizeInMeters.x -= m_SelectedBaseCabinetDepthInMeters;
                        }

                        if (rightCabinet && rightCabinet.cabinetType == CabinetType.Corner)
                        {
                            cabinetSizeInMeters.x -= m_SelectedBaseCabinetDepthInMeters;
                        }

                        cabinet.sizeInMeters = cabinetSizeInMeters;
                    }
                }

                m_CurrentCabinetLayout.UpdatePositions();
                m_CurrentCabinetLayout.UpdateMeshes();

                RefreshTickerLabels();

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
            m_SelectedBuildingMethod = buildingMethod;

            EventBridge.Designer.onBuildingMethodChanged?.Invoke(m_SelectedBuildingMethod);
        }

        protected async UniTask SetBrandCatalog(BrandCatalog brandCatalog)
        {
            m_SelectedBrandCatalog = brandCatalog;

            m_SelectedDoorStyleName =
                brandCatalog.defaultCabinetSectionDoorStyleName;

            m_SelectedDoorHandleStyleName = 
                brandCatalog.defaultCabinetSectionDoorHandleStyleName;

            m_EndPanelWidthInMetersLeft = 0f;
            m_EndPanelWidthInMetersRight = 0f;
            m_BackPanelDepthInMeters = 0f;

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
            m_SelectedCountertopType = countertopType;
        }

        protected void SetCabinetLayout(CabinetLayout layoutPrefab)
        {
            m_SelectedCabinetLayout = layoutPrefab;
        }

        protected void SetKitchenTemplate(KitchenTemplate template)
        {
            m_SelectedKitchenTemplate = template;
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
                occupant.gameObject == m_CurrentHighlightObject;

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
                backingToDestroy.gameObject == m_CurrentHighlightObject;

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
                        Cabinet.ResizeAxis.X | Cabinet.ResizeAxis.Z
                    );

                    Vector3 sizeInMeters = replacementCornerCabinet.sizeInMeters;
                    sizeInMeters.x = m_SelectedBaseCabinetDepthInMeters;
                    if (m_SelectedBrandCatalog)
                    {
                        // Ensure the cabinet's height matches the catalog's
                        // default value.
                        sizeInMeters.y = m_SelectedBrandCatalog.standardCabinetHeightInMeters;
                    }
                    sizeInMeters.z = m_SelectedBaseCabinetDepthInMeters;
                    replacementCornerCabinet.sizeInMeters = sizeInMeters;
                }

                if (m_CurrentHighlightObject == cabinetToDestroy.gameObject)
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
            m_SelectedCountertopOverhangInMeters = overhangInMeters;

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
        protected async UniTask UpdateCabinetHeightTransitions()
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
                await GetFinishMaterials(allowHighlightToReturn: true);

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

                if (selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                    selectedBrandCatalog &&
                    selectedBrandCatalog.cabinetSectionLegStyle)
                {
                    // Ensure height transitions are not on the ground in
                    // catalog building method.
                    heightTransitionCabinet.cabinet.transform.position +=
                        Vector3.up * selectedBrandCatalog.cabinetSectionLegStyle.heightInMeters;
                }

                heightTransitionCabinet.SetCountertopThickness(m_SelectedCountertopThicknessInMeters);
                heightTransitionCabinet.SetCountertopOverhang(m_SelectedCountertopOverhangInMeters);

                await layout.ReorderCabinet(heightTransitionCabinet, i.index + 1);

                Vector3 sizeInMeters = heightTransitionCabinet.sizeInMeters;
                sizeInMeters.x = Const.CabinetColumnWidthInMeters * 0.5f;
                sizeInMeters.z = m_SelectedBaseCabinetDepthInMeters;
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

        protected async UniTask OnCabinetResizedByWidget(ResizerWidget cabinetResizerWidget, float lengthMetersDelta)
        {
            Cabinet cabinet = (Cabinet)cabinetResizerWidget.target;

            if (cabinetResizerWidget.affectExtent == Extent.Min && !cabinet.Is(CabinetType.CornerWedge))
            {
                // Counter-offset the cuboid's position when adjusting the
                // minimum extent.

                Transform targetTransform =
                    cabinetResizerWidget.target.GetTransform();

                targetTransform.parent.position +=
                    targetTransform.TransformDirection(Vector3.right) * -lengthMetersDelta;
            }

            // REVIEW: I commented this out because it should happen automatically
            //         when `onCabinetResized` is invoked below. We're doing it
            //         twice, but maybe that is necessary? -Ryan
            //await OnCabinetResized(cabinet);

            // Emit the event about the cabinet being resized. This happens here
            // because it is after all validation is performed and the listener
            // will be assured the cabinet stays at the size it reports. (We
            // were doing this in Cabinet.TryToResizeLengthAsUser, but that
            // was too early.)
            await EventBridge.Designer.onCabinetResized.InvokeIfNonEmpty(cabinet);
        }

        private void OnCabinetResizeBegin(ResizerWidget caller)
        {
            if (m_VolumeBeingResized != null || m_ProductBeingDragged)
            {
                // Disallow resizing if another action is underway
                return;
            }

            m_VolumeBeingResized = caller.target;

            // Hide all other widgets
            var widgets =
                new ComponentsInChildrenEnumerator<KitchenWidget>(
                    m_CabinetResizeWidgetRoot
                );

            while (widgets.MoveNext())
            {
                widgets.Current.SetVisible(caller == widgets.Current);
            }
        }

        private void OnCabinetResizeEnd(ResizerWidget caller)
        {
            if (m_VolumeBeingResized == caller.target)
            {
                m_VolumeBeingResized = null;

                // Restore visibility of the resize widgets if the kitchen is
                // still selected

                SetCabinetResizeWidgetsVisible(true);// m_CurrentHighlightObject == gameObject);
            }
        }

        protected void OnCabinetShelfResizeBegin(ResizerWidget caller)
        {
            if (m_VolumeBeingResized != null || m_ProductBeingDragged)
            {
                // Disallow resizing if another action is underway
                return;
            }

            m_VolumeBeingResized = caller.target;

            var widgets =
                new ComponentsInChildrenEnumerator<KitchenWidget>(
                    m_CabinetShelfResizeWidgetRoot
                );

            while (widgets.MoveNext())
            {
                widgets.Current.SetVisible(caller == widgets.Current);
            }
        }

        protected void OnCabinetShelfResizeEnd(ResizerWidget caller)
        {
            if (m_VolumeBeingResized == caller.target)
            {
                m_VolumeBeingResized = null;

                SetCabinetShelfResizeWidgetsVisible(caller.target);
            }
        }

        /// <summary>
        ///     Forwards the "shelf was resized" event to the <see
        ///     cref="MeasurementOverlayManager"/> to keep the measurements to
        ///     the nearest left and right neighbouring products synchronized to
        ///     the shelf's new size.
        /// </summary>
        protected async UniTask OnCabinetShelfResized(ResizerWidget caller, float deltaLengthInMeters)
        {
            CabinetShelf resizedShelf = (CabinetShelf)caller.target;

            await EventBridge.Designer.onOccupantResized.InvokeIfNonEmpty(resizedShelf.occupant);
        }

        protected void OnInputCabinetSizeClicked(ButtonWidget caller)
        {
            EventBridge.Designer.showSetOccupiableVolumeWidthPrompt?.Invoke(caller.target);
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
                ProductVisual productVisual =
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
                    await ProductVisualContentLoader.FirstFromActiveScene()
                        .ProductVisualToContent(productVisual);

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

                    if (m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                        m_SelectedBrandCatalog != null &&
                        m_SelectedBrandCatalog.ruleset == BrandCatalogRuleset.WeatherStrong)
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

        private void RotateCameraToFrontOfCabinet(ICabinet cabinet)
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

                if (m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog)
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

                await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(m_CurrentHighlightObject);

                await EventBridge.Designer.onCabinetAddedByUser.InvokeIfNonEmpty(newCabinet);

                async UniTask CloneOccupants(OccupiableVolume from, OccupiableVolume to)
                {
                    foreach (Occupant occupantToClone in from.occupants)
                    {
                        Vector3 occupantLocalPosition =
                            occupantToClone.GetLocalPositionInVolume();

                        ProductVisual productVisual =
                            m_ProductVisualDatabase.FindRuntimeVisualByID(
                                occupantToClone.productInfo.visualId
                            );

                        if (!productVisual)
                        {
                            MelcherLog.LogError($"Did not find a product visual for visual ID: {occupantToClone.productInfo.visualId}");
                            continue;
                        }

                        ProductVisualContent productVisualPrefab =
                            await ProductVisualContentLoader.FirstFromActiveScene()
                                .ProductVisualToContent(productVisual);

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

                ProductVisual cabinetSectionVisual =
                    productVisualDatabase.FindRuntimeVisualByID(
                        cabinetSectionProductInfo.visualId
                    );

                if (!cabinetSectionVisual)
                {
                    MelcherLog.LogError($"No product visual found for a cabinet section named '{cabinetSectionProductInfo.name}' and using visual ID, {cabinetSectionProductInfo.visualId}");
                    return null;
                }

                ProductVisualContent cabinetSectionVisualContent =
                    await ProductVisualContentLoader.FirstFromActiveScene()
                        .ProductVisualToContent(cabinetSectionVisual);

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

                if (selectedBuildingMethod == KitchenBuildingMethod.Sections &&
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
            sectionSizeInMeters.z = m_SelectedBaseCabinetDepthInMeters;

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

            createdCabinet.SetToeKickType(m_SelectedToeKickType);

            (ResizerWidget left, ButtonWidget middle, ResizerWidget right) =
                CreateResizeWidgetsForCabinet(createdCabinet, Camera.main);

            left.SetVisible(false);
            middle.SetVisible(false);
            right.SetVisible(false);

            CenterTheGround();

            await EventBridge.Designer.onCabinetAddedByUser.InvokeIfNonEmpty(createdCabinet);

            return createdCabinet;
        }

        /// <summary>
        ///     Set the visibility of the widgets used to resize a cabinet. The
        ///     player can click and drag on these widgets to change the size
        ///     of the cabinet section.
        /// </summary>
        /// <param name="visible">
        ///     Whether to make the resizer widgets visible.
        /// </param>
        /// <param name="forceAffectAll">
        ///     When <see langword="true"/>, the visibility of *all* cabinet
        ///     widgets is modified to match <paramref name="visible"/>. The
        ///     default value is <see langword="false"/> which means the
        ///     widgets affected depends on the user's selection: if a cabinet
        ///     is selected (i.e., in Sections Building Method), only that
        ///     cabinet's widgets are changed.
        /// </param>
        private void SetCabinetResizeWidgetsVisible(bool visible, bool forceAffectAll = false)
        {
            var cabinetWidgets =
                new ComponentsInChildrenEnumerator<KitchenWidget>(
                    root: m_CabinetResizeWidgetRoot
                );

            if (forceAffectAll)
            {
                // Affect the visibility of all cabinets, regardless of the
                // current highlight. This is used to hide all the widgets when
                // they are first spawned. Oh, but we want to keep the widgets
                // visible if the user has a selection.
                while (cabinetWidgets.MoveNext())
                {
                    cabinetWidgets.Current.SetVisible(visible);
                }

                return;
            }

            // If the highlighted object is a cabinet, the user is in
            // Sections mode and only the selected cabinet will be highlighted.

            Cabinet mustMatch = null;
            m_CurrentHighlightObject?.TryGetComponent(out mustMatch);

            while (cabinetWidgets.MoveNext())
            {
                KitchenWidget widget = cabinetWidgets.Current;

                if (mustMatch && widget.target != mustMatch)
                {
                    // Skip this cabinet if it does not match the requested
                    // filter.
                    continue;
                }

                Cabinet cabinetForWidget =
                    (Cabinet)cabinetWidgets.Current.target;

                if (cabinetForWidget.isResizable && widget.isVisible != visible)
                {
                    cabinetWidgets.Current.SetVisible(visible);
                }
            }
        }

        private void SetCabinetShelfResizeWidgetsVisible(OccupiableVolume target)
        {
            if (target && target is CabinetShelf shelf && !shelf.isResizable)
            {
                target = null;
            }

            var shelfWidgets =
                new ComponentsInChildrenEnumerator<KitchenWidget>(
                    root: m_CabinetShelfResizeWidgetRoot
                );

            while (shelfWidgets.MoveNext())
            {
                shelfWidgets.Current.target = target;
                shelfWidgets.Current.SetVisible(target != null);
            }
        }

        private Camera GetCamera()
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
            if (m_SelectedBuildingMethod != KitchenBuildingMethod.Sections)
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
            if (m_SelectedBuildingMethod == KitchenBuildingMethod.Sections &&
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

        private void OnOccupantDragBegin(Occupant draggedOccupant, PointerEventData eventData)
        {
            if (m_VolumeBeingResized != null || m_ProductBeingDragged)
            {
                // Disallow dragging if another action is already underway
                eventData.pointerDrag = null;
                return;
            }

            Cabinet cabinet = (Cabinet)draggedOccupant.cabinet;

            bool didDetectDragPlane =
                ProFitUtility.CalculateCameraLocalDragPosition(
                    cabinet,
                    GetCamera(),
                    eventData,
                    out Vector3 _,
                    out Plane _
                );

            if (!didDetectDragPlane)
            {
                // Raycast missed the drag plane (this shouldn't happen)
                MelcherLog.LogWarning("Aborting product drag. Raycast didn't hit the calculated drag plane.", cabinet);
                eventData.pointerDrag = null;
                return;
            }

            MelcherLog.LogEditorOnly("Beginning drag");

            m_ProductBeingDragged = draggedOccupant;

            // Temporarily disable interactions on the product pointer layer
            m_CameraPhysicsRaycaster.eventMask &= ~m_ProductPointerInteractionMask;

            // Prevent resizing while dragging a product
            SetCabinetResizeWidgetsVisible(false);
            SetCabinetShelfResizeWidgetsVisible(target: null);
        }

        /// <summary>
        ///     Called every frame while the user is dragging an occupant along
        ///     an <see cref="OccupiableVolume"/>.
        /// </summary>
        /// <param name="draggedOccupant"></param>
        /// <param name="eventData"></param>
        private void OnOccupantDrag(Occupant draggedOccupant, PointerEventData eventData)
        {
            if (!m_LastOccupantDraggedTask.HasValue || m_LastOccupantDraggedTask.Value.Status != UniTaskStatus.Pending)
            {
                m_LastOccupantDraggedTask = OnOccupantDragAsync(draggedOccupant, eventData);
                m_LastOccupantDraggedTask.Value.Forget();
            }

            async UniTask OnOccupantDragAsync(Occupant draggedOccupant, PointerEventData eventData)
            {
                Camera mainCamera = GetCamera();
                Ray pointerRay = mainCamera.ScreenPointToRay(eventData.position);

                bool isDraggingShelf = draggedOccupant.TryGetComponent(out CabinetShelf _);

                OccupiableVolume startingVolume = draggedOccupant.occupiedVolume;

                // Check if the product is being dragged onto a different cabinet.
                // This is where we detect moving products between different cabinets.
                Cabinet nearestCabinet = null;
                float nearestCabinetDistance = float.MaxValue;
                foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    if (!cabinet.canHaveProducts)
                    {
                        continue;
                    }

                    if (draggedOccupant.isLockedToCabinet)
                    {
                        // Cannot drag locked occupants out of its cabinet.
                        continue;
                    }

                    OrientedBounds cabinetOrientedBounds = cabinet.GetOrientedBounds();

                    if (cabinetOrientedBounds.IntersectRay(pointerRay, out float distance) &&
                        distance < nearestCabinetDistance)
                    {
                        nearestCabinet = cabinet;
                        nearestCabinetDistance = distance;
                    }
                }

                CabinetShelf nearestShelf = null;
                float nearestShelfDistance = float.MaxValue;
                if (nearestCabinet && !isDraggingShelf)
                {
                    // Check for cabinet shelves to drag products into.
                    foreach (Occupant occupant in nearestCabinet.occupants)
                    {
                        if (occupant.TryGetComponent(out CabinetShelf shelf))
                        {
                            Cuboid shelfCabinetCuboid = shelf.resizerCuboid;

                            OrientedBounds shelfBounds =
                                new OrientedBounds(
                                    new Bounds(shelfCabinetCuboid.center, shelfCabinetCuboid.size),
                                    shelf.GetTransform().worldToLocalMatrix
                                );

                            shelfBounds.Draw(Color.magenta, 2.0f);

                            if (shelfBounds.IntersectRay(pointerRay, out float distanceToShelf) &&
                                distanceToShelf < nearestShelfDistance)
                            {
                                nearestShelf = shelf;
                                nearestShelfDistance = distanceToShelf;
                            }
                        }
                    }
                }

                if (nearestShelf && !ReferenceEquals(nearestShelf, draggedOccupant.occupiedVolume))
                {
                    bool didDetectDragPlane =
                        ProFitUtility.CalculateCameraLocalDragPosition(
                            nearestShelf,
                            mainCamera,
                            eventData,
                            out Vector3 futureLastCameraLocalDragPosition,
                            out Plane _
                        );

                    if (didDetectDragPlane)
                    {
                        Vector3 worldDragPosition =
                            mainCamera.transform.TransformPoint(
                                futureLastCameraLocalDragPosition
                            );

                        Vector3 volumeLocalDragPosition =
                            nearestShelf.GetTransform().InverseTransformPoint(
                                worldDragPosition
                            );

                        //Debug.DrawLine(mainCamera.transform.position, worldDragPosition, Color.blue, 0.1f);

                        float metersFromCabinetMinX =
                           DoDraggedOccupantSnapping(nearestShelf, draggedOccupant, volumeLocalDragPosition.x);

                        m_MetersFromVolumeMinX.value = metersFromCabinetMinX;

                        // TODO: FIXME: For now, we just assume the shelf is
                        //       top-mounted and the occupant is top-mounted.
                        m_MetersFromVolumeMinY.value = -nearestShelf.resizerCuboid.height;

                        bool wasProductAdded = false;

                        Cuboid? backingOccupancyInWorldSpace = null;

                        CabinetSpan spanOfShelfCabinet =
                            layout.FindCabinetSpan((Cabinet)nearestShelf.occupant.cabinet);

                        if (spanOfShelfCabinet.backing)
                        {
                            backingOccupancyInWorldSpace =
                                spanOfShelfCabinet.GetBackingOccupancyCuboidInWorldSpace(
                                    spanOfShelfCabinet.backing.info,
                                    spanOfShelfCabinet.backing
                                );
                        }

                        if (AreProductsForcedToCenterOfCabinets(cabinetForRuleset: null))
                        {
                            wasProductAdded =
                                await nearestShelf.TryAddOccupantCentered(
                                    draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    m_MetersFromVolumeMinX,
                                    m_MetersFromVolumeMinY
                                );
                        }
                        else
                        {
                            wasProductAdded =
                                await nearestShelf.TryAddOccupantNear(
                                    draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    m_MetersFromVolumeMinX,
                                    m_MetersFromVolumeMinY
                                );
                        }

                        if (wasProductAdded)
                        {
                            EventBridge.Designer.onOccupantDragSuccess?.Invoke(
                                draggedOccupant,
                                eventData
                            );
                            return;
                        }
                    }
                }


                // Try to transfer the product to the nearest hovered cabinet. We
                // do not do this if we are hovering over a cabinet shelf because
                // that transfer was already handled/checked.
                if (!nearestShelf && nearestCabinet && !ReferenceEquals(nearestCabinet, draggedOccupant.occupiedVolume))
                {
                    // Fetch the potential new local drag position for this
                    // occupant. If the occupant switches cabinets and this
                    // position isn't stored in m_LastCameraLocalDragPosition, the
                    // next frame where the product is dragged will jump the
                    // product based on the old cabinet's local space and look bad

                    bool didDetectDragPlane =
                        ProFitUtility.CalculateCameraLocalDragPosition(
                            nearestCabinet,
                            mainCamera,
                            eventData,
                            out Vector3 futureLastCameraLocalDragPosition,
                            out Plane _
                        );

                    if (didDetectDragPlane)
                    {
                        // A raycast from the cursor hit the cabinet's "drag plane".

                        Vector3 worldDragPosition =
                            mainCamera.transform.TransformPoint(
                                futureLastCameraLocalDragPosition
                            );

                        Vector3 volumeLocalDragPosition =
                            nearestCabinet.GetTransform().InverseTransformPoint(
                                worldDragPosition
                            );

                        //Debug.DrawLine(mainCamera.transform.position, worldDragPosition, Color.blue, 0.1f);

                        m_MetersFromVolumeMinX.value =
                           DoDraggedOccupantSnapping(nearestCabinet, draggedOccupant, volumeLocalDragPosition.x);

                        m_MetersFromVolumeMinY.value =
                            draggedOccupant.GetLocalPositionInVolume().y;

                        if (draggedOccupant.occupiedVolume.parentVolume != null ||
                            (!draggedOccupant.productVisual.info.anchor.IsAtBottom() && !MelcherCommon.IsAlmostEqual(draggedOccupant.cabinet.sizeInMeters.y, nearestCabinet.sizeInMeters.y, tolerance: 0.001f)))
                        {
                            // This occupant is in a shelf OR it is anchored
                            // relative to the top of its cabinet and is moving to
                            // a cabinet of a different height.
                            // TODO: FIXME: This is a quick hack for moving
                            //              occupants out of shelves. It assumes
                            //              that only top-mounted products will be
                            //              added to shelves.
                            float currentDistanceFromTop =
                                draggedOccupant.occupiedVolume.occupiableCuboid.yMax - draggedOccupant.GetLocalPositionInVolume().y;

                            m_MetersFromVolumeMinY.value = nearestCabinet.sizeInMeters.y - currentDistanceFromTop;
                        }

                        Cuboid? backingOccupancyInWorldSpace = null;

                        CabinetSpan spanOfNearestCabinet =
                            layout.FindCabinetSpan(nearestCabinet);

                        if (spanOfNearestCabinet.backing)
                        {
                            backingOccupancyInWorldSpace =
                                spanOfNearestCabinet.GetBackingOccupancyCuboidInWorldSpace(
                                    spanOfNearestCabinet.backing.info,
                                    spanOfNearestCabinet.backing
                                );
                        }

                        bool wasProductAdded;

                        if (AreProductsForcedToCenterOfCabinets(cabinetForRuleset: nearestCabinet))
                        {
                            wasProductAdded =
                                await nearestCabinet.TryAddOccupantCentered(
                                    draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    m_MetersFromVolumeMinX,
                                    m_MetersFromVolumeMinY
                                );
                        }
                        else
                        {
                            wasProductAdded =
                                await nearestCabinet.TryAddOccupantNear(
                                    draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    m_MetersFromVolumeMinX,
                                    m_MetersFromVolumeMinY
                                );
                        }

                        if (wasProductAdded)
                        {
                            EventBridge.Designer.onOccupantDragSuccess?.Invoke(
                                draggedOccupant,
                                eventData
                            );
                            return;
                        }
                    }
                }

                // By this point, all cases have been handled except for dragging
                // an occupant along the volume it is already attached to.

                if (AreProductsForcedToCenterOfCabinets(cabinetForRuleset: (Cabinet)draggedOccupant.cabinet))
                {
                    // Ignore dragging products within their cabinet when the
                    // kitchen is configured for products to always be centered
                    // within their cabinet.
                    return;
                }

                OccupiableVolume currentVolume = draggedOccupant.occupiedVolume;

                bool drag =
                    ProFitUtility.CalculateCameraLocalDragPosition(
                        currentVolume,
                        mainCamera,
                        eventData,
                        out Vector3 cameraLocalDragPosition,
                        out Plane cameraLocalDragPlane
                    );

                if (drag)
                {
                    Vector3 worldDragPosition = mainCamera.transform.TransformPoint(cameraLocalDragPosition);
                    Transform cabinetTransform = currentVolume.GetTransform();
                    Vector3 volumeLocalDragPosition = cabinetTransform.InverseTransformPoint(worldDragPosition);

                    m_MetersFromVolumeMinX.value =
                        DoDraggedOccupantSnapping(currentVolume, draggedOccupant, volumeLocalDragPosition.x);

                    m_MetersFromVolumeMinY.value = draggedOccupant.GetLocalPositionInVolume().y;

                    Cuboid? backingOccupancyInWorldSpace = null;

                    CabinetSpan spanOfCurrentCabinet =
                        layout.FindCabinetSpan((Cabinet)draggedOccupant.cabinet);

                    if (spanOfCurrentCabinet.backing)
                    {
                        backingOccupancyInWorldSpace =
                            spanOfCurrentCabinet.GetBackingOccupancyCuboidInWorldSpace(
                                spanOfCurrentCabinet.backing.info,
                                spanOfCurrentCabinet.backing
                            );
                    }

                    bool didMoveProduct =
                        currentVolume.TryMoveOccupantNear(
                            draggedOccupant,
                            backingOccupancyInWorldSpace,
                            ref m_MetersFromVolumeMinX.value,
                            ref m_MetersFromVolumeMinY.value
                        );

                    if (didMoveProduct)
                    {
                        EventBridge.Designer.onOccupantDragSuccess?.Invoke(
                            draggedOccupant,
                            eventData
                        );
                    }
                }
            }
        }

        protected void TrySendOnBrandCatalogChanged()
        {
            if(ProFitGlobals.appRules.RequiresBrandCatalog && m_SelectedBrandCatalog == null)
            {
                return;
            }

            EventBridge.Designer.onBrandCatalogChanged?.Invoke(m_SelectedBrandCatalog);
        }

        // Used to pass values "by ref" to async functions.
        private Ref<float> m_MetersFromVolumeMinX = new (0.0f);
        private Ref<float> m_MetersFromVolumeMinY = new (0.0f);

        private async UniTask OnOccupantDragEnd((Occupant draggedOccupant, PointerEventData eventData) args)
        {
            if (args.draggedOccupant == m_ProductBeingDragged)
            {
                m_ProductBeingDragged = null;

                // Restore interaction on the product pointer layer
                m_CameraPhysicsRaycaster.eventMask |= m_ProductPointerInteractionMask;

                // Re-enable resizing when product drag ends
                SetCabinetShelfResizeWidgetsVisible(m_CurrentHighlightObject.GetComponent<CabinetShelf>());

                if (!await TryToFitCabinetToOccupant(args.draggedOccupant))
                {
                    // This cabinet could not be expanded to fit the occupant.
                    // Try to find a better home.

                    // TODO: Return the occupant to its previous location.
                    //       -Ryan

                    foreach (CabinetSpan cabinetSpan in layout.spans)
                    {
                        Cuboid? backingOccupancyInWorldSpace = null;

                        if (cabinetSpan.backing)
                        {
                            backingOccupancyInWorldSpace =
                                cabinetSpan.GetBackingOccupancyCuboidInWorldSpace(
                                    cabinetSpan.backing.info,
                                    cabinetSpan.backing
                                );
                        }

                        foreach (ElementIndex<Cabinet> i in cabinetSpan.cabinets)
                        {
                            m_MetersFromVolumeMinX.value = 0.0f;
                            m_MetersFromVolumeMinY.value = 0.0f;

                            bool didAdd =
                                await i.element.TryAddOccupantCentered(
                                    args.draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    m_MetersFromVolumeMinX,
                                    m_MetersFromVolumeMinY
                                );

                            if (didAdd)
                            {
                                return;
                            }
                        }

                    }

                    MelcherLog.LogError("Failed to resize destination cabinet and could not locate a suitable alternative location.", args.draggedOccupant);
                }

                RotateCameraToFrontOfCabinet(args.draggedOccupant.cabinet);
            }
        }

        /// <summary>
        ///     In some building methods (Sections and Catalog), some products
        ///     are able to resize cabinets when the products are added to the
        ///     cabinet. E.g., fridges do this.
        /// </summary>
        /// <param name="occupant"></param>
        private async UniTask<bool> TryToFitCabinetToOccupant(Occupant occupant)
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

        /// <summary>
        /// Called when an object wants to be highlighted.
        /// <paramref name="highlightObject"/> may be an occupant or a cabinet.
        /// </summary>
        private async UniTask TryHighlightObject(GameObject highlightObject)
        {
            // Callers are allowed to request the same object highlighted
            // again. This allows the highlight to be refreshed if objects are
            // added when the user already has a selection.
            bool isNewHighlight = highlightObject != m_CurrentHighlightObject;

            if (m_SelectedBuildingMethod != KitchenBuildingMethod.Standard)
            {
                // We only allow double-clicking an occupant to select its
                // cabinet when the building mode uses occupant auto-centering,
                // this is because measurements within a cabinet are not
                // vital in that case.

                // This is to get around an issue with this current
                // implementation where the occupant is still draggable after
                // it's clicked twice and the cabinet is highlighted. When
                // dragging an occupant in this state, the measurement lines
                // are synchronized to the cabinet which is not useful to the
                // client. A fix may be to listen to pointer-up events, however
                // I believe I tried that a few months ago and ran into timing
                // issues with other pointer events. -Ryan

                if (!isNewHighlight &&
                    highlightObject &&
                    highlightObject.TryGetComponent(out Occupant highlightedOccupant))
                {
                    // Try to select its parent volume.
                    highlightObject = highlightedOccupant.occupiedVolume.gameObject;
                    isNewHighlight = true;
                }
            }

            if (m_ProductBeingDragged || m_VolumeBeingResized != null)
            {
                // Ignore highlight changes while dragging or resizing
                return;
            }

            // Remove the highlight from the last object
            if (m_CurrentHighlightObject && isNewHighlight)
            {
                await UpdateHighlightMaterial(
                    m_CurrentHighlightObject,
                    primaryHighlightMaterial: null,
                    secondaryHighlightMaterial: null
                );

                m_CurrentHighlightObject.TryGetComponent(out Occupant lastHighlightedProduct);

                if (lastHighlightedProduct)
                {
                    if (m_CurrentHighlightObject.TryGetComponent(out IOccupantHighlightChangedHandler occupantHighlightChanged))
                    {
                        await occupantHighlightChanged.OnOccupantHighlightChanged(
                            lastHighlightedProduct.cabinet,
                            highlight: null
                        );
                    }
                }
            }

            m_CurrentHighlightObject = highlightObject;

            // Apply the highlight to the new object
            if (m_CurrentHighlightObject)
            {
                await UpdateHighlightMaterial(
                    m_CurrentHighlightObject,
                    m_PrimaryHighlightMaterial,
                    m_SecondaryHighlightMaterial
                );

                m_CurrentHighlightObject.TryGetComponent(out Occupant currentHighlightedProduct);

                if (currentHighlightedProduct &&
                    m_CurrentHighlightObject.TryGetComponent(out IOccupantHighlightChangedHandler occupantHighlightChanged))
                {
                    await occupantHighlightChanged.OnOccupantHighlightChanged(
                        currentHighlightedProduct.cabinet,
                        m_PrimaryHighlightMaterial
                    );
                }
            }

            if (isNewHighlight)
            {
                await EventBridge.Designer.onHighlightChanged.InvokeIfNonEmpty(m_CurrentHighlightObject);
            }
        }

        protected void OnToolbarFinishProductSelected(ProductInfo finishProduct, ProductCategory fallbackFinishCategory)
        {
            ChangeFinish(finishProduct, fallbackFinishCategory).Forget();
        }

        protected async UniTask ChangeFinish(ProductInfo finishProduct, ProductCategory fallbackFinishCategory)
        {
            if (!fallbackFinishCategory.IsFinish())
            {
                MelcherLog.LogError(
                    $"Received {fallbackFinishCategory} but require a finish category."
                );
            }

            ProductCategory finishCategory = fallbackFinishCategory;
            ProductVisual finishProductVisual = null;

            Material finishMaterial = await ProFitGlobals.appProfile.GetDefaultKitchenMaterial();
            FlooringType flooringType = FlooringType.Wood;

            if (finishProduct != null)
            {
                finishCategory = finishProduct.category;

                finishProductVisual =
                    m_ProductVisualDatabase.FindRuntimeVisualByID(
                        finishProduct.visualId
                    );

                if (!finishProductVisual)
                {
                    MelcherLog.LogError(
                        $"No product visual found for finish '{finishProduct.name}' (id: {finishProduct.id}) with visual id '{finishProduct.visualId}'"
                    );
                }
                else
                {
                    ProductVisualContent finishVisualContent =
                        await ProductVisualContentLoader.FirstFromActiveScene()
                            .ProductVisualToContent(finishProductVisual);

                    finishMaterial = finishVisualContent.finishMaterial;
                    flooringType = finishProductVisual.info.flooringType;
                }
            }

            if (finishCategory == ProductCategory.FinishFloor && !finishProductVisual)
            {
                finishMaterial = ProFitGlobals.appOptions.appSkin.kitchenDefaultFlooringMaterial;
            }

            await UpdateCabinetHeightTransitions();

            switch (finishCategory)
            {
                case ProductCategory.FinishFloor:
                    m_SelectedFlooringFinish = finishProduct;

                    // Pick between the wood floor model or stone floor model
                    bool isStone = flooringType == FlooringType.Stone;
                    m_GroundStone.gameObject.SetActive(isStone);
                    m_GroundWood.gameObject.SetActive(!isStone);

                    m_GroundStone.sharedMaterial = finishMaterial;
                    m_GroundWood.sharedMaterial = finishMaterial;

                    // Intentional `return`
                    return;

                case ProductCategory.FinishCabinet:
                    m_SelectedCabinetFinish = finishProduct;

                    if (m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog && m_SelectedCabinetFinish.manufacturerId == 140)
                    {
                        EventBridge.Designer.showNotification?.Invoke(
                            title: "Deco Grain",
                            message: "Please note that the Deco Grain finish adds a 25% premium to your cabinet choices.",
                            onDismiss: null
                        );
                    }
                    break;

                case ProductCategory.FinishCountertop:
                    m_SelectedCountertopFinish = finishProduct;
                    break;

                default:
                    MelcherLog.LogError(
                        $"Unhandled finish category '{finishCategory}'",
                        finishProductVisual
                    );

                    // Intentional `return`
                    return;
            }

            if (m_CurrentHighlightObject != gameObject)
            {
                // The kitchen isn't selected so we can propagate the new
                // material without overriding the highlight material

                (Material cabinetMaterial, Material countertopMaterial) =
                    await GetFinishMaterials(allowHighlightToReturn: false);

                foreach (CabinetSpan span in m_CurrentCabinetLayout.spans)
                {
                    await span.SetCabinetMaterials(
                        m_CurrentHighlightObject,
                        cabinetMaterial,
                        countertopMaterial,
                        await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
                    );
                }
            }
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
                m_SelectedBuildingMethod == KitchenBuildingMethod.Sections ||
                m_SelectedBuildingMethod == KitchenBuildingMethod.Catalog,
                $"Cabinet reordering is assumed to not work in {m_SelectedBuildingMethod} building method."
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
            MelcherLog.Breadcrumb($"Building method: {m_SelectedBuildingMethod}");
            if (m_SelectedBuildingMethod != KitchenBuildingMethod.Catalog)
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
            //    MelcherCommon.IndexOf(toeKickTypes, m_SelectedToeKickType);

            //int newToeKickTypeIndex =
            //    (currentToeKickTypeIndex + 1) % toeKickTypes.Count;

            //m_SelectedToeKickType = toeKickTypes[newToeKickTypeIndex];

            m_SelectedToeKickType =
                m_SelectedToeKickType == CabinetToeKickType.None ?
                    CabinetToeKickType.Inset :
                    CabinetToeKickType.None;

            EventBridge.Designer.onToeKickTypeChanged?.Invoke(m_SelectedToeKickType);
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

            if (m_MeasurementOverlayManager.measureMode == MeasurementOverlayManager.MeasureMode.ToCabinetEdge)
            {
                // Center within the overall volume.

                Cuboid occupiableCuboid =
                    occupant.occupiedVolume.occupiableCuboid;

                metersFromVolumeMinX =
                    (occupiableCuboid.xMin + occupiableCuboid.xMax) * 0.5f;

                metersFromVolumeMinX -= occupiedCuboid.width * 0.5f;
            }
            else if (m_MeasurementOverlayManager.measureMode == MeasurementOverlayManager.MeasureMode.ToNearestOccupant)
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

        /// <summary>
        ///     Add or remove a highlight from an object. If <paramref
        ///     name="primaryHighlightMaterial"/> is null, the highlight will be
        ///     removed; otherwise, the highlight material will be set.
        /// </summary>
        /// <param name="highlightObject"></param>
        /// <param name="primaryHighlightMaterial">
        ///     The main highlight material. This is applied to the object
        ///     the player has selected. If this is <see langword="null"/>,
        ///     <paramref name="highlightObject"/> will be unhighlighted.
        /// </param>
        /// <param name="secondaryHighlightMaterial">
        ///     This material is applied to cabinets and countertops that are
        ///     not directly focused by a highlight. I.e., this is only
        ///     relevant when <see cref="KitchenBuildingMethod.Sections"/> is
        ///     enabled and the player has an individual cabinet selected.
        /// </param>
        private async UniTask UpdateHighlightMaterial(
            GameObject  highlightObject,
            Material    primaryHighlightMaterial,
            Material    secondaryHighlightMaterial)
        {
            (Material cabinetMaterial, Material countertopMaterial) =
                await GetFinishMaterials(allowHighlightToReturn: false);

            Material defaultKitchenMaterial =
               await ProFitGlobals.appProfile.GetDefaultKitchenMaterial();

            // Check if this is a product visual that is/was selected
            if (highlightObject.TryGetComponent(out Occupant occupant))
            {
                MelcherLog.Log(
                    "Highlighting product: " + occupant,
                    occupant,
                    severity: MelcherLogSeverity.Verbose
                );

                await occupant.productVisualContentInstance.SetModelHighlight(
                    primaryHighlightMaterial,
                    primaryHighlightMaterial ? primaryHighlightMaterial : cabinetMaterial,
                    countertopMaterial,
                    defaultKitchenMaterial
                );

                CabinetShelf shelf = null;
                if (primaryHighlightMaterial)
                {
                    shelf = highlightObject.GetComponent<CabinetShelf>();
                }

                SetCabinetShelfResizeWidgetsVisible(shelf);

                return;
            }
            else if (highlightObject.TryGetComponent(out CabinetBacking backing))
            {
                MelcherLog.Log(
                    "Highlighting backing: " + backing,
                    backing,
                    severity: MelcherLogSeverity.Verbose
                );

                Material activeCabinetMaterial =
                    primaryHighlightMaterial ? primaryHighlightMaterial : cabinetMaterial;

                Material activeCountertopMaterial =
                    primaryHighlightMaterial ? primaryHighlightMaterial : countertopMaterial;

                backing.OnCabinetMaterialsChanged(
                    activeCabinetMaterial,
                    activeCountertopMaterial
                );

                if (!backing.hasRiserAndRiserCountertop)
                {
                    CabinetSpan backingSpan = null;

                    foreach (CabinetSpan span in layout.spans)
                    {
                        if (span.backing == backing)
                        {
                            backingSpan = span;
                            break;
                        }
                    }

                    if (backingSpan == null)
                    {
                        MelcherLog.LogError($"Could not find a cabinet span that has the backing, {backing.name}", backing);
                        return;
                    }

                    // Alter the cabinets' countertop directly.
                    foreach (ElementIndex<Cabinet> i in backingSpan.cabinets)
                    {
                        Cabinet cabinet = i.element;

                        await cabinet.SetCabinetMaterials(
                           cabinetMaterial,
                           activeCountertopMaterial,
                           defaultKitchenMaterial
                        );
                    }
                }

                return;
            }

            // No product visual. This must be a cabinet or the kitchen.

            Material unfocusedCabinetMaterial       = cabinetMaterial;
            Material unfocusedCountertopMaterial    = countertopMaterial;

            bool isHighlighting = primaryHighlightMaterial;

            if (isHighlighting)
            {
                // A highlight material is provided so we must be highlighting
                // an object.

                cabinetMaterial = primaryHighlightMaterial;
                countertopMaterial = primaryHighlightMaterial;

                unfocusedCabinetMaterial = secondaryHighlightMaterial;
                unfocusedCountertopMaterial = secondaryHighlightMaterial;
            }

            MelcherLog.LogEditorOnly($"{(isHighlighting ? "Highlighting" : "Unhighlighting")} kitchen");

            CenterTheGround();

            // Hide the ground if we're highlighting or show the ground when
            // we're removing the highlight
            m_Ground.gameObject.SetActive(!isHighlighting);

            highlightObject.TryGetComponent(out Cabinet individualCabinet);

            // Show the resize widgets when the kitchen is selected
            SetCabinetResizeWidgetsVisible(isHighlighting);

            foreach (CabinetSpan span in m_CurrentCabinetLayout.spans)
            {
                foreach (ElementIndex<Cabinet> i in span.cabinets)
                {
                    Cabinet cabinet = i.element;

                    if (!individualCabinet || individualCabinet == cabinet)
                    {
                        await cabinet.SetCabinetMaterials(
                            cabinetMaterial,
                            countertopMaterial,
                            defaultKitchenMaterial
                        );
                    }
                    else
                    {
                        await cabinet.SetCabinetMaterials(
                            unfocusedCabinetMaterial,
                            unfocusedCountertopMaterial,
                            defaultKitchenMaterial
                        );
                    }
                }

                if (span.backing)
                {
                    if (!individualCabinet)
                    {
                        span.backing.OnCabinetMaterialsChanged(
                            cabinetMaterial,
                            countertopMaterial
                        );
                    }
                    else
                    {
                        span.backing.OnCabinetMaterialsChanged(
                            unfocusedCabinetMaterial,
                            unfocusedCountertopMaterial
                        );
                    }
                }
            }
        }

        /// <summary>
        ///     Set the kitchen cabinet's materials but without marking them as
        ///     selected. This is meant to be used temporarily when taking
        ///     screenshots of the kitchen.
        ///     <br /><br />
        ///
        ///     Pass <see langword="null"/> for either or both parameters to
        ///     reset that material to the currently selected finish.
        /// </summary>
        public async UniTask SetTemporaryFinishMaterials(Material cabinetMaterial, Material countertopMaterial, Material cutOutMaterial)
        {
            foreach (CabinetSpan span in layout.spans)
            {
                if (span.backing)
                {
                    span.backing.OnCabinetMaterialsChanged(
                        cabinetMaterial,
                        countertopMaterial
                    );
                }
            }

            // Iterate the cabinets separately from the spans to avoid
            // double-treating corner cabinets. Corner cabinets belong to two
            // cabinet spans.
            await m_CurrentCabinetLayout.SetCabinetMaterials(
                cabinetMaterial,
                countertopMaterial,
                await ProFitGlobals.appProfile.GetDefaultKitchenMaterial(),
                cutOutMaterial
            );
        }

        public async UniTask<(Material cabinetMaterial, Material countertopMaterial)> GetFinishMaterials(bool allowHighlightToReturn)
        {
            Material cabinetMaterial =
                await GetSelectedOrDefaultFinishMaterial(m_SelectedCabinetFinish);

            Material countertopMaterial =
                await GetSelectedOrDefaultFinishMaterial(m_SelectedCountertopFinish);

            if (allowHighlightToReturn && m_CurrentHighlightObject)
            {
                if (m_CurrentHighlightObject == gameObject)
                {
                    // The kitchen is selected. Return the highlight material so
                    // newly added products apply the proper material to their
                    // cut-outs
                    cabinetMaterial = m_PrimaryHighlightMaterial;
                    countertopMaterial = m_PrimaryHighlightMaterial;
                }
                else if (m_CurrentHighlightObject.TryGetComponent(out Cabinet cabinet))
                {
                    cabinetMaterial = m_SecondaryHighlightMaterial;
                    countertopMaterial = m_SecondaryHighlightMaterial;
                }
            }

            return (cabinetMaterial, countertopMaterial);
        }

        public async UniTask<Material> GetSelectedOrDefaultFinishMaterial(ProductInfo selectedFinish)
        {
            Material finishMaterial =
                await ProFitGlobals.appProfile.GetDefaultKitchenMaterial();

            if (selectedFinish != null)
            {
                // Lookup the material in the product catalog
                ProductVisual finishVisual =
                    m_ProductVisualDatabase.FindRuntimeVisualByID(
                        selectedFinish.visualId
                    );

                if (!finishVisual)
                {
                    MelcherLog.LogError(
                        $"No product visual found for finish '{selectedFinish.name}' (id: {selectedFinish.id}) with visual id '{selectedFinish.visualId}'"
                    );
                }
                else
                {
                    ProductVisualContent finishVisualPrefab =
                        await ProductVisualContentLoader.FirstFromActiveScene()
                            .ProductVisualToContent(finishVisual);

                    if (finishVisualPrefab != null && finishVisualPrefab.finishMaterial != null)
                    {
                        finishMaterial = finishVisualPrefab.finishMaterial;
                    }
                }
            }

            return finishMaterial;
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
        private void UpdateCameraAndFramingGroups()
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

        /// <summary>
        ///     Create the cabinet resize widgets.
        /// </summary>
        private void RecreateResizeWidgets()
        {
            // Remove the old widgets
            foreach (Transform child in m_CabinetResizeWidgetRoot)
            {
                if (child.TryGetComponent(out ResizerWidget oldResizerWidget))
                {
                    oldResizerWidget.onResizeBegin -= OnCabinetResizeBegin;
                    oldResizerWidget.onResizeEnd -= OnCabinetResizeEnd;
                    oldResizerWidget.onResized -= OnCabinetResizedByWidget;
                }

                Destroy(child.gameObject);
            }

            // Create new widgets for each resizable cabinet
            if (m_CurrentCabinetLayout)
            {
                Camera mainCamera = Camera.main;
                foreach (ElementIndex<Cabinet> i in m_CurrentCabinetLayout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    if (cabinet.isResizable)
                    {
                        CreateResizeWidgetsForCabinet(cabinet, mainCamera);
                    }
                }

                // Hide the resize widgets by default.
                SetCabinetResizeWidgetsVisible(false, forceAffectAll: true);
            }
        }

        protected (ResizerWidget left, ButtonWidget middle, ResizerWidget right) CreateResizeWidgetsForCabinet(Cabinet cabinet, Camera mainCamera)
        {
            // Resizer widgets normally anchor to the back of the cabinet but
            // with spaces and corner wedges, they anchor to the front.
            float zAnchor =
                cabinet.cabinetType is CabinetType.Space or CabinetType.CornerWedge ?
                    0.0f :
                    1.0f;

            ResizerWidget leftWidget =
                CreateResizerWidget(
                    "ResizeLeft_" + cabinet.name,
                    new Vector3(0.0f, 1.0f, zAnchor),
                    Extent.Min
                );

            ResizerWidget rightWidget =
                CreateResizerWidget(
                    "ResizeRight_" + cabinet.name,
                    new Vector3(1.0f, 1.0f, zAnchor),
                    Extent.Max
                );

            ButtonWidget middleWidget =
                CreateKitchenWidget(
                    m_InputCabinetSizeWidget,
                    widgetParent: m_CabinetResizeWidgetRoot,
                    mainCamera,
                    cabinet
                );

            middleWidget.name = "InputCabinetSizeWidget";
            middleWidget.normalizedTargetAnchor = new Vector3(0.5f, 1.0f, 1.0f);
            middleWidget.onClicked += OnInputCabinetSizeClicked;

            return (leftWidget, middleWidget, rightWidget);

            /// <summary>
            ///     Create a resizer for a cabinet.
            /// </summary>
            ResizerWidget CreateResizerWidget(string name, Vector3 anchor, Extent affectExtent)
            {
                ResizerWidget resizer =
                    CreateKitchenWidget(
                        m_ResizeCabinetWidgetPrefab,
                        m_CabinetResizeWidgetRoot,
                        mainCamera,
                        cabinet
                    );

                resizer.name = name;
                resizer.normalizedTargetAnchor = anchor;
                resizer.affectExtent = affectExtent;
                resizer.onResizeBegin += OnCabinetResizeBegin;
                resizer.onResizeEnd += OnCabinetResizeEnd;
                resizer.onResized += OnCabinetResizedByWidget;

                return resizer;
            }
        }

        protected ResizerWidget CreateCabinetResizer(Cabinet cabinet, Camera mainCamera)
        {
            ResizerWidget resizer =
                Instantiate(
                    m_ResizeCabinetWidgetPrefab,
                    parent: m_CabinetResizeWidgetRoot
                );

            resizer.target = cabinet;
            resizer.worldCamera = mainCamera;
            resizer.onResizeBegin += OnCabinetResizeBegin;
            resizer.onResizeEnd += OnCabinetResizeEnd;
            resizer.onResized += OnCabinetResizedByWidget;

            return resizer;
        }

        protected void RecreateTickerWidgets()
        {
            // Remove the old widgets
            foreach (Transform child in m_TickerDimensionsRoot)
            {
                Destroy(child.gameObject);
            }

            // Create new ticker widgets for each cabinet span that can change.
            if (!m_CurrentCabinetLayout)
            {
                return;
            }

            foreach (CabinetSpan span in m_CurrentCabinetLayout.spans)
            {
                foreach (ElementIndex<Cabinet> i in span.cabinets)
                {
                    Cabinet cabinet = i.element;

                    if (!cabinet.isResizable && !cabinet.canHaveProducts)
                    {
                        continue;
                    }

                    CabinetTickerWidget widget =
                        Instantiate(
                            m_TickerDimensionsPrefab,
                            m_TickerDimensionsRoot
                        );

                    widget.targetCabinetSpan = span.index;
                    widget.RefreshLabel();

                    // Skip to the next span.
                    break;
                }
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

        /// <summary>
        ///     This function is intended to be called when a product is
        ///     dragged by the user. It handles snapping the product laterally
        ///     to the center of other products, snapping the product to 1/8"
        ///     increments and constraining the occupant to the size of the
        ///     cabinet.
        /// </summary>
        /// <param name="occupiableVolume">
        ///     The volume the occupant is being moved into/within.
        /// </param>
        /// <param name="draggedOccupant">
        ///     The occupant being dragged. The occupant is not required to be
        ///     an occupant of <paramref name="occupiableVolume"/>.
        /// </param>
        /// <param name="volumeLocalDragPositionX">
        ///     The x-position of the user's cursor relative to <paramref
        ///     name="occupiableVolume"/>'s local-space. I.e., the distance in
        ///     meters that the cursor is along the volume's local x-axis.
        /// </param>
        /// <returns>
        ///     The calculated new <see cref="Occupant.metersFromCabinetMinX"/>
        ///     value for the dragged product.
        /// </returns>
        protected float DoDraggedOccupantSnapping(OccupiableVolume occupiableVolume, Occupant draggedOccupant, float volumeLocalDragPositionX)
        {
            float productWidthInMeters =
                occupiableVolume.CalculateOccupancyCuboidForOccupant(
                    draggedOccupant,
                    overridePositionInVolumeX: 0.0f
                ).width;

            // Calculate the product's position relative to the cursor so it is
            // horizontally centered on the cursor
            float occupantMetersFromVolumeMinX =
                volumeLocalDragPositionX - (productWidthInMeters * 0.5f);

            // Try to snap the occupant to the center of any nearby products
            occupantMetersFromVolumeMinX =
                SnapOccupantToOtherOccupantCenters(
                    occupiableVolume,
                    draggedOccupant,
                    occupantMetersFromVolumeMinX,
                    productWidthInMeters,
                    maxSnapDistanceInMeters: 4.0f * Units.Inches2Meters,
                    out Occupant snappedTo
                );

            if (!snappedTo)
            {
                // The product didn't snap to any nearby products so force it
                // to snap in 1/8" increments
                occupantMetersFromVolumeMinX =
                    SnapOccupantToVolumeIncrement(occupantMetersFromVolumeMinX);
            }

            // Ensure it's within the cabinet's bounds
            Cuboid occupiableCuboid = occupiableVolume.occupiableCuboid;

            OrientedBounds bounds =
                new OrientedBounds(
                    new Bounds(occupiableCuboid.center, occupiableCuboid.size),
                    occupiableVolume.GetTransform().worldToLocalMatrix
                );

            bounds.Draw(Color.red, duration: 3.0f, drawX: true);

            return Mathf.Clamp(occupantMetersFromVolumeMinX, occupiableCuboid.xMin, occupiableCuboid.xMax - productWidthInMeters);
        }

        /// <summary>
        /// Snap the point to a 1/8" increment. This is intended to be used
        /// when a player drags or positions a product. The goal is to simplify
        /// the measurements that are displayed in the plan photos.
        /// </summary>
        protected float SnapOccupantToVolumeIncrement(float metersFromVolumeMinX)
        {
            if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
            {
                return Mathf.Round(metersFromVolumeMinX * Units.Meters2Centimeters) * Units.Centimeters2Meters;
            }

            // Snap to nearest 1/8"
            return
                (float)ImperialUnit.FromTotalMeters(metersFromVolumeMinX)
                    .RoundToNearestDivision(8).totalMeters;
        }

        /// <summary>
        ///     Try to snap an occupant to the horizontal center of a nearby
        ///     occupant.
        ///     <br /><br />
        ///
        ///     This is intended to be called when a product is being dragged
        ///     by the user. Snapping to the center line of nearby products is
        ///     usually what a user wants.
        /// </summary>
        /// <param name="metersFromVolumeMinX">
        /// </param>
        /// <param name="occupiableVolume">
        /// </param>
        /// <param name="productWidthInMeters">
        /// </param>
        /// <param name="draggedOccupant">
        /// </param>
        /// <param name="maxSnapDistanceInMeters">
        ///     The maximum horizontal distance in meters that <paramref
        ///     name="occupant"/> may be from another occupant's center line and
        ///     still snap.
        /// </param>
        /// <param name="snappedTo">
        ///     Output argument. If a product was near enough to be snapped to,
        ///     this will be set to the product that was chosen. If no suitable
        ///     snap occupant was found, this will be <see langword="null"/>.
        /// </param>
        /// <returns></returns>
        protected float SnapOccupantToOtherOccupantCenters(
            OccupiableVolume    occupiableVolume,
            Occupant            draggedOccupant,
            float               metersFromVolumeMinX,
            float               productWidthInMeters,
            float               maxSnapDistanceInMeters,
            out Occupant        snappedTo)
        {
            snappedTo = null;

            // Get the center point of the dragged product. This will be
            // compared to other product's center position
            float centerPositionMetersFromVolumeMinX =
                metersFromVolumeMinX + productWidthInMeters * 0.5f;

            // Distance from the nearest snap position
            float closestCenterDistance = float.MaxValue;

            foreach (Occupant otherOccupant in occupiableVolume.occupants)
            {
                if (draggedOccupant == otherOccupant)
                {
                    // Skip our self
                    continue;
                }

                Cuboid otherOccupantSize =
                    occupiableVolume.CalculateOccupancyCuboidForOccupant(otherOccupant);

                float otherCenterPositionMetersFromCabinetMinX =
                    otherOccupant.metersFromVolumeMinX + (otherOccupantSize.width * 0.5f);

                float horizontalDistanceInMeters =
                    Mathf.Abs(otherCenterPositionMetersFromCabinetMinX - centerPositionMetersFromVolumeMinX);

                if (horizontalDistanceInMeters < Mathf.Min(maxSnapDistanceInMeters, closestCenterDistance))
                {
                    // The other occupant is close enough to snap to
                    snappedTo = otherOccupant;

                    closestCenterDistance = horizontalDistanceInMeters;

                    metersFromVolumeMinX =
                        otherCenterPositionMetersFromCabinetMinX - (productWidthInMeters * 0.5f);
                }
            }

            return metersFromVolumeMinX;
        }

        protected void GetLegacyMeasurementsForCabinetSave(Cabinet cabinet, JsonList measurementEntries, bool findTop)
        {
            float lastCutOutEndMetersFromCabinetMinX = 0.0f;
            foreach (Occupant occupant in cabinet.occupants)
            {
                // Skip backsplashes and bar tops for now
                if (occupant.productVisualContentInstance.TryGetComponent(out CabinetBacking _))
                {
                    continue;
                }

                // Filter products that are not on the side we're interested in
                bool isOnTop =
                    occupant.productVisual.info.anchor == ProductAnchor.OnTopAtFront ||
                    occupant.productVisual.info.anchor == ProductAnchor.OnTopAtBack;

                if (isOnTop != findTop)
                {
                    continue;
                }

                // Get the starting position of the cut out
                float cutOutStartMetersFromVolumeMinX = occupant.metersFromVolumeMinX + occupant.productVisualContentInstance.cabinetCutOut.transformHelper.offsetInMeters.x;
                float cutOutEndMetersFromVolumeMinX = cutOutStartMetersFromVolumeMinX + occupant.productVisualContentInstance.cabinetCutOut.transformHelper.sizeInMeters.x;

                // Add an entry for the space between the last and current product
                float leadingSpanInMeters = lastCutOutEndMetersFromCabinetMinX - cutOutStartMetersFromVolumeMinX;
                if (leadingSpanInMeters > 0.0f)
                {
                    JsonDict gapEntry = new Dictionary<string, object>()
                    {
                        { "inches", leadingSpanInMeters * Units.Meters2Inches },
                        { "product_id", "" }
                    };
                    measurementEntries.Add(gapEntry);
                }

                // Add an entry for the product
                JsonDict productEntry = new Dictionary<string, object>()
                {
                    { "inches", occupant.productVisualContentInstance.cabinetCutOut.transformHelper.sizeInInches.x },
                    { "product_id", occupant.productInfo.sku }
                };
                measurementEntries.Add(productEntry);

                lastCutOutEndMetersFromCabinetMinX = cutOutEndMetersFromVolumeMinX;
            }

            // Add an entry for any remaining space
            float trailingSpaceInMeters = cabinet.sizeInMeters.x - lastCutOutEndMetersFromCabinetMinX;
            if (trailingSpaceInMeters > 0.0f)
            {
                JsonDict tailEntry = new Dictionary<string, object>()
                {
                    { "inches", trailingSpaceInMeters * Units.Meters2Inches },
                    { "product_id", "" }
                };
                measurementEntries.Add(tailEntry);
            }
        }

        // ====================================================================
        // Static functions
        // ====================================================================

        static WidgetType CreateKitchenWidget<WidgetType>(WidgetType widgetPrefab, Transform widgetParent, Camera eventCamera, OccupiableVolume target)
            where WidgetType : KitchenWidget
        {
            WidgetType widget = Instantiate(widgetPrefab, widgetParent);

            widget.worldCamera = eventCamera;
            widget.target = target;

            return widget;
        }

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
        protected ProductVisualDatabase m_ProductVisualDatabase = null;

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
        private GameObject m_CurrentHighlightObject;

        protected Material m_PrimaryHighlightMaterial = null;
        protected Material m_SecondaryHighlightMaterial = null;

        /// <summary>
        ///     The cabinet volume currently being resized. This can be a <see
        ///     cref="Cabinet"/> or a <see cref="CabinetShelf"/> (or <see
        ///     langword="null"/> to indicate no volume is being resized).
        /// </summary>
        private OccupiableVolume m_VolumeBeingResized;
        private Occupant m_ProductBeingDragged;

        /// <summary>
        ///     The time at which the player started this design (new) or when
        ///     it was reopened for editing (loaded).
        /// </summary>
        private System.DateTime m_SessionStartTime;

        // Player's kitchen customizations:

        private KitchenBuildingMethod m_SelectedBuildingMethod;

        /// <summary>
        ///     The brand catalog that was selected in the Universal Kitchen
        ///     Designer app. This dictates which cabinet layouts and door
        ///     styles are available. It can only be chosen before a kitchen
        ///     design is started.
        /// </summary>
        private BrandCatalog m_SelectedBrandCatalog;

        /// <summary>
        ///     The template the user selected during the initial kitchen
        ///     setup. This is only used during Catalog building method and
        ///     effectively represents a save file the kitchen will use at the
        ///     start.
        /// </summary>
        private KitchenTemplate m_SelectedKitchenTemplate;

        /// <summary>
        ///     The name of the door style in-use during <see
        ///     cref="KitchenBuildingMethod.Catalog"/>.
        /// </summary>
        private string m_SelectedDoorStyleName;

        /// <summary>
        ///     The name of the door handle style in-use during <see
        ///     cref="KitchenBuildingMethod.Catalog"/>.
        /// </summary>
        private string m_SelectedDoorHandleStyleName;

        private float m_EndPanelWidthInMetersLeft;
        private float m_EndPanelWidthInMetersRight;

        /// <summary>
        ///     The extra depth added to the back of all cabinets when a back
        ///     panel is added (Gemini). This is normally 0. It allows us to
        ///     track how much depth to remove when the back panels are removed
        ///     and to correct the countertop overhang distance for authored
        ///     corner cabinets.
        /// </summary>
        private float m_BackPanelDepthInMeters;

        private CountertopType m_SelectedCountertopType;

        /// <summary>
        ///     The kitchen layout prefab the player selected during kitchen
        ///     setup. This is just the template that will be instantiated. See
        ///     <see cref="layout"/> for the actual instantiated layout.
        /// </summary>
        private CabinetLayout m_SelectedCabinetLayout;
        private ProductInfo m_SelectedCountertopFinish;
        private ProductInfo m_SelectedCabinetFinish;
        private ProductInfo m_SelectedFlooringFinish;

        private float m_SelectedBaseCabinetDepthInMeters;
        private float m_SelectedCountertopThicknessInMeters;

        /// <summary>
        ///     The amount the countertop's edge should overhang its cabinet.
        ///     This works because the countertop 27-slice mesh has a 1-inch
        ///     overhang beyond its represented size. (Everywhere apart from
        ///     the vertical axis.) This means that the Y-values can be left at
        ///     1.0 because it has no effect. Then, the X- and Z-components can
        ///     be set independently to control the overhang of the countertop
        ///     on its left and right edges and front and back edges,
        ///     respectively.
        /// </summary>
        private CuboidPadding m_SelectedCountertopOverhangInMeters;

        /// <summary>
        ///     Whether the user has chosen to enable toe kicks. Gemini-only.
        /// </summary>
        private CabinetToeKickType m_SelectedToeKickType;

        /// <summary>
        ///     A list of optional, extra features that were added to this
        ///     kitchen. At the moment, the only extra feature is Challenger's
        ///     "Premium Finish Package" which otherwise has no where to be
        ///     saved.
        /// </summary>
        private List<KitchenExtraFeature> m_SelectedExtraFeatures = new List<KitchenExtraFeature>();

        /// <summary>
        ///     A kludge that ensures the OnCabinetReordered callback that is
        ///     used to setup the cabinet height transition cabinets doesn't
        ///     recurse when new cabinets are added and reordered mid event.
        /// </summary>
        private bool m_IsUpdatingCabinetHeightTransitions;

        /// <summary>
        ///     A reference to the last drag task that was started. This is
        ///     used to prevent more than one OnOccupantDragged awaitables from
        ///     running at the same time.
        /// </summary>
        private UniTask? m_LastOccupantDraggedTask = default;

        /// <summary>
        ///     This is a token that is shared between all saves that are part
        ///     of the same lineage. When a new kitchen is saved for the first
        ///     time, the server assigns an analytics token. This is used to
        ///     merge the analytics from subsequent saves of this kitchen into
        ///     one report (e.g., how long the user has been designing this
        ///     kitchen, which products this kitchen ultimately decided to use,
        ///     etc.)
        /// </summary>
        private string m_AnalyticsKey;

        // =====================================================================
        // Static fields
        // =====================================================================

        protected static Kitchen s_KitchenInstance = default;

        // ====================================================================
        // Nested types
        // ====================================================================

        private struct ManualPriceSheetEntry
        {
            public string productId;
            public string productName;
            public float price;
        }   // struct ManualPriceSheetEntry

        private struct ChallengerCountertopInfo
        {
            public float depthInInches;
            public string sku;
            public string description;
            public float pricePerLinearInch;
        }

    }   // class Kitchen

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
