namespace Melcher
{
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEngine;

    /// <summary>
    ///     A cabinet has a few meshes to render: the cabinet, the countertop,
    ///     and possibly a toe kick (Gemini-only). In Gemini, a cabinet may be
    ///     composed of more than one mesh as most of them are hand-authored
    ///     (as opposed to procedural) and therefeore have more intricate
    ///     designs with door handles, etc.
    ///     <br /><br />
    ///
    ///     A cabinet is an <see cref="OccupiableVolume"/> and therefore has a
    ///     list of occupants (if allowed) which are the products added to the
    ///     cabinet. These are sorted ascendingly by the product's position in
    ///     the cabinet's local space. The cabinet can be resized (optional)
    ///     and its countertop thickness can be changed too.
    /// </summary>
    public class Cabinet : OccupiableVolume
        , ICabinet
    {
        // ====================================================================
        // Static functions
        // ====================================================================

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        ///     The transform that is a parent of the cabinet's visual. This
        ///     is exposed for the sole purpose of allowing height transition
        ///     cabinets to be raised off the ground in the Catalog building
        ///     method.
        /// </summary>
        public GameObject cabinet => m_Cabinet;

        public CabinetType cabinetType => m_CabinetType;

        public Cabinet cornerCabinetBeingWedged
        {
            get => m_CornerBeingWedged;
            set
            {
                // Signal an error if the user tries to assign a cabinet that
                // isn't a corner.
                MelcherLog.Assert(!value || value.Is(CabinetType.Corner), "Only corner cabinets should be wedged.");

                m_CornerBeingWedged = value;
            }
        }

        public bool isResizable
        {
            get
            {
                // For Gemini, the refrigeration panels need to be resizable
                // while they contain no products. Once they have a fridge,
                // their size is adjusted and the cabinet cannot be resized
                // until the fridge is removed.
                if (m_IsUserResizable &&
                    m_Kitchen != null &&
                    m_Kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                    m_Occupants.readOnly.Count > 0 &&    // At least one product
                    m_CabinetSectionProductInfo != null &&
                    !string.IsNullOrEmpty(m_CabinetSectionProductInfo.visualId))
                {
                    if (m_CabinetSectionVisual.info.sectionModelType == CabinetModelType.Procedural &&
                        m_CabinetSectionVisual.info.productCategoriesAllowedInSection != null &&
                        m_CabinetSectionVisual.info.IsCategoryAllowedInSection(ProductCategorySpecific.Fridge))
                    {
                        // This is a fridge panel. It is not resizable now
                        // because it has at least one occupant.
                        return false;
                    }
                }

                return m_IsUserResizable;
            }
        }

        /// <summary>
        ///     Return <see langword="true"/> if the cabinet matches the
        ///     kitchen's standard cabinet height. This is used to check if a
        ///     cabinet is a Fixed Smoker Cabinet in Stoll, which is shorter
        ///     than regular cabinets and therefore affects how backings are
        ///     positioned.
        /// </summary>
        public bool isStandardHeight
        {
            get
            {
                float standardHeightInMeters = m_InitialSizeInches.y * Const.Inches2Meters;

                if (kitchen != null &&
                    kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                    kitchen.selectedBrandCatalog)
                {
                    standardHeightInMeters = kitchen.selectedBrandCatalog.standardCabinetHeightInMeters;
                }

                return MelcherCommon.IsAlmostEqual(sizeInMeters.y, standardHeightInMeters, tolerance: 0.001f);
            }
        }


        public CabinetModelType cabinetModelType => m_CabinetModelType;

        public KitchenBuildingMethod allowedFitToProductWidthBuildingMethods => m_AllowedFitToProductWidthBuildingMethods;

        public KitchenBuildingMethod forceProductsToCenterBuildingMethods => m_ForceProductsToCenterBuildingMethods;

        public bool canHaveProducts => m_CanHaveProducts;

        public bool canHaveLegs => m_CanHaveLegs;

        public Sprite backingSprite
        {
            get => m_BackingSprite;
            set => m_BackingSprite = value;
        }

        public IEnumerable<MeshRenderer> meshRenderers => GetMeshRenderers();

        /// <summary>
        ///     Get the size of the entire cabinet segment in meters. The
        ///     occupiable cuboid may differ from this (e.g., if the cabinet
        ///     is adjacent to a corner and therefore has a non-zero <see
        ///     cref="occupiablePaddingInMeters"/>).
        /// </summary>
        public Vector3 sizeInMeters
        {
            get => m_SizeInMeters;
            set
            {
                m_SizeInMeters = value;
                ResizeMeshToCabinetSize();
            }
        }

        public Vector3 sizeInMetersWithoutAdditive
        {
            get
            {
                if(cabinetSectionVisual &&
                    cabinetSectionVisual.info.sectionEndPanelSizeInches > 0 &&
                    kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
                {
                    Vector3 rawSizeInMeters = m_SizeInMeters;
                    rawSizeInMeters.x -= m_AppliedMidOfRunPaddingInMetersLeft + m_AppliedMidOfRunPaddingInMetersRight;

                    return rawSizeInMeters;
                    
                } else
                {
                    return m_SizeInMeters;
                }
            }
        }

        /// <summary>
        /// Return true if the cabinet is a corner or an end.
        /// </summary>
        public bool isCornerOrEnd =>
            m_CabinetType == CabinetType.Corner ||
            m_CabinetType == CabinetType.EndLeft ||
            m_CabinetType == CabinetType.EndRight;

        public bool hasSelectedEndPanel =>
            kitchen != null &&
            ((Is(CabinetType.EndLeft) && kitchen.selectedExtraFeatures.Contains(KitchenExtraFeature.CabinetLeftEndPanels)) ||
            (Is(CabinetType.EndRight) && kitchen.selectedExtraFeatures.Contains(KitchenExtraFeature.CabinetRightEndPanels)));

        public bool isEndPanelOnGround =>
            hasSelectedEndPanel &&
            kitchen.selectedBrandCatalog &&
            kitchen.selectedBrandCatalog.endPanelsTouchGround;

        public bool canOccludeSelectionClick => m_CanOccludeSelectionClick;

        public Vector3 initialSizeInMeters => m_InitialSizeInches * Const.Inches2Meters;
        public Vector3 minSizeInMeters
        {
            get => m_MinSizeInInches * Const.Inches2Meters;
            set => m_MinSizeInInches = value / Const.Inches2Meters;
        }

        public Vector3 maxSizeInMeters
        {
            get => m_MaxSizeInInches * Const.Inches2Meters;
            set => m_MaxSizeInInches = value / Const.Inches2Meters;
        }

        /// <summary>
        ///     Get or set the cabinet's anchor point. The setter does not
        ///     update the cabinet's position or cascade any changes to the
        ///     other cabinets. For that, call <see
        ///     cref="CabinetLayout.UpdatePositions"/> after the setter.
        ///     <br /><br />
        ///
        ///     This is a normalized position, {(0-1), (0-1), (0-1)}, relative
        ///     to the cuboid of the cabinet to this cabinet's left. The anchor
        ///     point represents where this cabinet's bottom-left-front corner
        ///     is positioned relative to its neighbour. For example, an anchor
        ///     of {1, 0, 0} will snap this cabinet's to its neighbour's
        ///     right-bottom-front corner.
        /// </summary>

        [System.Obsolete("This feature has been superseded by normalizedAnchorForNextSection, " +
            "which is cleaner because it does not require bookkeeping as " +
            "cabinets shifted and reordered within the kitchen. However, " +
            "we should postpone removing normalizedAnchorToPreviousSection " +
            "until we are confident all users have upgraded to a version " +
            "that is using normalizedAnchorForNextSection.")]
        public Vector3 normalizedAnchorToPreviousSection
        {
            get => m_NormalizedAnchorToPreviousSection;
            set => m_NormalizedAnchorToPreviousSection = value;
        }

        /// <summary>
        ///     This is the normalized position within the current cabinet's
        ///     local space where its right neighbouring cabinet will anchor.
        ///     This anchoring behaviour happens in <see
        ///     cref="CabinetLayout.UpdatePositions"/>.
        ///     <br /><br />
        ///
        ///     Most cabinets have a next-section anchor value of (1,0,0) which
        ///     means the cabinets are side-by-side. However, corners, use
        ///     (0,0,0) which allows their right neighbours to swivel on the
        ///     inner corner of corner cabinet.
        /// </summary>
        public Vector3 normalizedAnchorForNextSection
        {
            get => m_NormalizedAnchorForNextSection;
        }

        public IKitchen kitchen => m_Kitchen;

        public override Cuboid resizerCuboid => new Cuboid(m_SizeInMeters);

        /// <summary>
        ///     Get the cuboid that represents the full shape of the cabinet
        ///     segment (in the cabinet's own local space).
        /// </summary>
        public override Cuboid occupiableCuboid
        {
            get
            {
                Cuboid result =
                    occupiablePaddingInMeters.Deflate(resizerCuboid);

                if (m_CabinetSectionVisual)
                {
                    result =
                        m_CabinetSectionVisual.info.sectionOccupiableCuboidPaddingInMeters.Deflate(result);
                }

                if (kitchen.selectedBrandCatalog &&
                    kitchen.selectedBrandCatalog.cabinetSectionLegStyle &&
                    canHaveLegs &&
                    (m_CabinetSectionVisual == null || m_CabinetSectionVisual.info.cabinetSectionFootingType == CabinetSectionFootingType.Floating))
                {
                    // Account for the height of the kitchen legs
                    result.yMin +=
                        kitchen.selectedBrandCatalog.cabinetSectionLegStyle.heightInMeters;
                }

                return result;
            }
        }

        /// <summary>
        ///     A cabinet's parent volume is always <see langword="null"/>
        ///     because it is the topmost <see cref="OccupiableVolume"/ type.
        /// </summary>
        public override OccupiableVolume parentVolume => null;

        public Cuboid countertopCuboid => m_Countertop ? m_Countertop.cuboid : default;

        public ProductVisual cabinetSectionVisual => m_CabinetSectionVisual;

        /// <inheritdoc cref="m_CabinetSectionVisualPrefab"/>
        public ProductVisualContent cabinetSectionVisualPrefab => m_CabinetSectionVisualPrefab;

        public GameObject cabinetSectionInstance => m_CabinetSectionInstance;

        public string cabinetSectionName => m_CabinetSectionInstance.name;

        public ProductInfo cabinetSectionProductInfo => m_CabinetSectionProductInfo;

        // =====================================================================
        // Methods
        // =====================================================================

        public bool Is(CabinetType? cabinetType = null, CabinetModelType? cabinetModelType = null)
        {
            return
                (!cabinetType.HasValue || m_CabinetType == cabinetType) &&
                (!cabinetModelType.HasValue || m_CabinetModelType == cabinetModelType);
        }

        /// <summary>
        ///     Return <see langword="true"/> if this cabinet's "occupant
        ///     orientation transform" is using a non-zero yaw angle. This is
        ///     the case for 45-degree corner cabinets because the occupants
        ///     must align to the 45-degree face.
        /// </summary>
        public bool HasCustomOccupantOrientation()
        {
            if (!m_CabinetSectionVisualPrefab)
            {
                return false;
            }

            float occupantOrientationYaw =
                m_CabinetSectionVisualPrefab.sectionOccupantOrientation.localEulerAngles.y;

            // Ensure that rotations above 360 and below 0 are correctly
            // normalized to a range of 0-1
            float normalizedYaw = Mathf.Repeat(occupantOrientationYaw, 360.0f);
            normalizedYaw /= 360.0f;

            // Find the closest whole number so we can treat numbers close to 0
            // or close to 1 as the same angle (i.e., 0 yaw == 360 yaw).
            float nearest = Mathf.Round(normalizedYaw);

            return !MelcherCommon.IsAlmostEqual(normalizedYaw, nearest, tolerance: 0.001f);
        }

        /// <summary>
        ///     Check if this cabinet effectively has the same yaw rotation
        ///     (Y-axis) as another cabinet. This is useful for determining
        ///     if a cabinet matches the <see
        ///     cref="CabinetSpan.orientationAlignedCabinet"/> of its span.
        /// </summary>
        public bool IsYawSimilar(Cabinet compareToCabinet)
        {
            Vector3 myRotation = m_Transform.eulerAngles;

            Vector3 theirRotation = compareToCabinet.m_Transform.eulerAngles;

            const float Threshold = 0.001f;

            return MelcherCommon.IsAlmostEqual(myRotation.y, theirRotation.y, Threshold);
        }

        public void SetSizeFromInches(Vector3 inches)
        {
            sizeInMeters = inches * Const.Inches2Meters;
        }

        public void SetKitchen(IKitchen kitchen)
        {
            m_Kitchen = kitchen;
        }

        public void SetToeKickType(CabinetToeKickType toeKickType)
        {
            m_ToeKickType = toeKickType;

            RefreshToeKick();
        }

        /// <summary>
        ///     Count the number of visible columns. Each column returned here
        ///     represents one half of a full column. Therefore, the returned
        ///     value should eventually be divided by two for the proper column
        ///     count.
        /// </summary>
        public int CountVisibleBackColumnHalves()
        {
            int backColumns = 0;

            if (m_BackLeftColumn && m_BackLeftColumn.gameObject.activeSelf)
            {
                ++backColumns;
            }

            if (m_BackRightColumn && m_BackRightColumn.gameObject.activeSelf)
            {
                ++backColumns;
            }

            if (m_CornerColumn && m_CornerColumn.gameObject.activeSelf)
            {
                // The back corner column is always full size so count it as
                // two halves.
                backColumns += 2;
            }

            return backColumns;
        }

        public void SetColumnsVisible(bool isVisible)
        {
            m_FrontLeftColumn?.gameObject.SetActive(isVisible);
            m_FrontRightColumn?.gameObject.SetActive(isVisible);
            m_BackLeftColumn?.gameObject.SetActive(isVisible);
            m_BackRightColumn?.gameObject.SetActive(isVisible);
        }

        /// <summary>
        ///     Apply the kitchen's selected toe kick style setting to the
        ///     cabinet's visuals.
        /// </summary>
        public void RefreshToeKick()
        {
            CabinetToeKickType selectedToeKickType = CabinetToeKickType.None;
            float toeKickHeightInMeters = 0.0f;

            int toeKickIndex =
                m_CabinetSectionVisual ?
                    m_CabinetSectionVisual.info.sectionInsetToeKickIndex :
                    -1;

            Vector2 extraInsetDistanceInMeters =
                m_CabinetSectionVisual ?
                    m_CabinetSectionVisual.info.sectionProceduralInsetToeKickExtraInsetInMeters :
                    Vector2.zero;

            if (kitchen != null)
            {
                selectedToeKickType = kitchen.selectedToeKickType;

                if (kitchen.selectedBrandCatalog)
                {
                    toeKickHeightInMeters =
                        kitchen.selectedBrandCatalog.cabinetSectionLegStyle.heightInMeters;
                }
            }

            bool canHaveToeKick =
                !m_CabinetSectionVisual ||
                m_CabinetSectionVisual.info.sectionCanHaveToeKick;

            bool useProceduralToeKickLocalX =
                canHaveToeKick &&
                selectedToeKickType != CabinetToeKickType.None &&
                m_SizeInMeters.x > 0.0f &&
                toeKickIndex < 0;

            if (m_ToeKickLocalX)
            {
                // Setup the main toe kick. This spans from the cabinet's left-
                // most edge to its rightmost edge.

                Cuboid toeKickCuboidInMeters =
                    new Cuboid(
                        new()
                        {
                            x = m_SizeInMeters.x,
                            y = toeKickHeightInMeters,
                            z = m_SizeInMeters.z
                        }
                    );

                if (selectedToeKickType == CabinetToeKickType.Inset)
                {
                    toeKickCuboidInMeters.zMin += Const.ToeKickInsetInMeters;
                    toeKickCuboidInMeters.zMin += extraInsetDistanceInMeters.x;
                }

                m_ToeKickLocalX.sizeInMeters = toeKickCuboidInMeters.size;
                m_ToeKickLocalX.offsetInMeters = toeKickCuboidInMeters.position;

                m_ToeKickLocalX.gameObject.SetActive(useProceduralToeKickLocalX);
            }

            bool useProceduralToeKickLocalZ =
                useProceduralToeKickLocalX &&
                selectedToeKickType == CabinetToeKickType.Inset;

            if (m_ToeKickLocalZ)
            {
                // Setup the second part of the toe kick. This only exists in
                // corner cabinets and is used to form the inset L-shape which
                // is only needed for inset toe kicks.

                Cuboid toeKickCuboidInMeters =
                    new Cuboid(
                        new()
                        {
                            x = m_SizeInMeters.x,
                            y = toeKickHeightInMeters,
                            z = Const.ToeKickInsetInMeters
                        }
                    );

                toeKickCuboidInMeters.xMin += Const.ToeKickInsetInMeters;
                toeKickCuboidInMeters.xMin += extraInsetDistanceInMeters.y;
                toeKickCuboidInMeters.zMax += extraInsetDistanceInMeters.x;

                m_ToeKickLocalZ.sizeInMeters = toeKickCuboidInMeters.size;
                m_ToeKickLocalZ.offsetInMeters = toeKickCuboidInMeters.position;

                m_ToeKickLocalZ.gameObject.SetActive(useProceduralToeKickLocalZ);
            }

            for (int i = 0; i < m_InsetToeKickVariations.Count; ++i)
            {
                GameObject insetToeKickVariation = m_InsetToeKickVariations[i];

                // Our non-procedural toe kick models are sized to be 1 meter
                // tall at regular scale. This allows them to be easily resized
                // at runtime to match the cabinet leg height.
                Vector3 localScale = insetToeKickVariation.transform.localScale;
                localScale.y = toeKickHeightInMeters;
                insetToeKickVariation.transform.localScale = localScale;

                m_InsetToeKickVariations[i].SetActive(
                    selectedToeKickType == CabinetToeKickType.Inset && i == toeKickIndex
                );
            }
        }

        /// <summary>
        ///     Change the cabinet's internal type value.
        ///     <br /><br />
        ///
        ///     The cabinet type field was built with the intent that it would
        ///     always be readonly. However we ran into a situation with <see
        ///     cref="KitchenBuildingMethod.Catalog"/> where it new kitchens
        ///     are populated with different types of cabinets. Rather than
        ///     use a different prefab for every individual cabinet type, we
        ///     simply reassign the instantiated cabinet's type.
        /// </summary>
        public void SetCabinetType(CabinetType newCabinetType)
        {
            m_CabinetType.value = newCabinetType;
        }

        public void SetCabinetSectionProductInfo(ProductInfo cabinetSectionProductInfo)
        {
            if (m_CabinetSectionProductInfo != null)
            {
                MelcherLog.LogError("Only expecting a cabinet's section product info to be set once.", this);
                return;
            }

            m_CabinetSectionProductInfo = cabinetSectionProductInfo;

            if (m_CabinetSectionProductInfo != null)
            {
                string label =
                    kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections ?
                        m_CabinetSectionProductInfo.name :
                        m_CabinetSectionProductInfo.sku;

                m_InfoLabelFront.text = label;
                m_InfoLabelTop.text = label;
            }
        }

        public void SetBaseCabinetDepth(float baseDepthInMeters, ResizeAxis resizeAxis)
        {
            if (Is(CabinetType.CornerWedge))
            {
                // Corner wedge cabinets have a uniform depth to width ratio.
                // Their depth is directly controlled by their length to
                // preserve their 90-degree fit against the two neighbouring
                // cabinets. Therefore, we early exit to avoid breaking this
                // relationship.
                return;
            }

            float cornerBaseWidthInMeters = baseDepthInMeters;

            if (kitchen.selectedBuildingMethod is KitchenBuildingMethod.Catalog or KitchenBuildingMethod.Sections &&
                cabinetSectionProductInfo != null)
            {
                // Override the kitchen's base cabinet depth with the depth
                // specified by this cabinet.
                baseDepthInMeters = cabinetSectionProductInfo.productSizeInMeters.z;
                cornerBaseWidthInMeters = cabinetSectionProductInfo.productSizeInMeters.x;
            }

            // Cabinets need to be resized along their local Z-axis. Corner
            // cabinets also need their X-axis resized.
            float backingDepthInMetersZ = 0.0f;
            float backingDepthInMetersX = 0.0f;

            foreach (CabinetSpan span in kitchen.layout.spans)
            {
                if (!span.backing)
                {
                    // Skip this span entirely. It doesn't have a backing and
                    // we're looking for how much backings affects this
                    // cabinet's size.
                    continue;
                }

                if (!span.Contains(this))
                {
                    // This span and its backing cannot affect this cabinet so
                    // it can be ignored.
                    continue;
                }

                float backingDepthChangeInMeters =
                    span.backing.GetCabinetInflateDepthInMeters(
                        kitchen.selectedBuildingMethod
                    );

                if (IsYawSimilar(span.orientationAlignedCabinet))
                {
                    // The cabinet matches the span's orientation which means
                    // the span's backing affects the cabinet's z-axis.
                    backingDepthInMetersZ = backingDepthChangeInMeters;
                }
                else
                {
                    // The cabinet does not match the span's orientation. This
                    // can only mean the span's backing affects the cabinet's
                    // x-axis.
                    backingDepthInMetersX = backingDepthChangeInMeters;
                }
            }

            // TODO: Make this support changing the cabinet depth with products
            //       added

            if (resizeAxis.HasFlag(ResizeAxis.Z))
            {
                m_SizeInMeters.z = baseDepthInMeters + backingDepthInMetersZ;
            }

            if (resizeAxis.HasFlag(ResizeAxis.X) && Is(CabinetType.Corner))
            {
                // If this is a corner, we need to update the width to affect the
                // depth of the next span.
                m_SizeInMeters.x = cornerBaseWidthInMeters + backingDepthInMetersX;
            }

            ResizeMeshToCabinetSize();

            EventBridge.Designer.onCabinetResized?.Invoke(this);
        }

        public void SetCountertopThickness(float thicknessInMeters)
        {
            if (m_Countertop)
            {
                // Match the cabinet's size but use the countertop thickness/height
                Cuboid countertopCuboid = m_Countertop.cuboid;
                countertopCuboid.yMax = m_SizeInMeters.y;
                countertopCuboid.yMin = countertopCuboid.yMax - thicknessInMeters;
                m_Countertop.UpdateSlices(countertopCuboid);

                // Update products cut-outs
                foreach (Occupant occupant in m_Occupants.readOnly)
                {
                    if (occupant.TryGetComponent(out CabinetShelf shelf))
                    {
                        shelf.RefreshCountertopSize();
                    }

                    occupant.RefreshCutOuts();
                }
            }
        }

        public void SetCountertopOverhang(CuboidPadding overhangInMeters)
        {
            if (m_Countertop)
            {
                // Change the overhang of the countertop mesh. The mesh uses
                // one-inch as its base 1.0f scale, so convert meters to inches
                // to not over scale the overhang.
                m_Countertop.skinScale = overhangInMeters * Const.Meters2Inches;

                ResizeMeshToCabinetSize();

                // Update products cut-outs
                foreach (Occupant occupant in m_Occupants.readOnly)
                {
                    occupant.RefreshCutOuts();
                }
            }
        }

        public CuboidPadding GetCountertopOverhangInMeters()
        {
            if (m_Countertop)
            {
                return m_Countertop.skinScale * Const.Inches2Meters;
            }

            return default;
        }


        /// <summary>
        ///     Set the visibility of the countertop slices that extend in a
        ///     specific direction. This is used to clip off the overhang when
        ///     a cabinet backing is added.
        ///     <br /><br />
        ///
        ///     This function is intended to be called by a <see
        ///     cref="CabinetSpan"/> when a <see cref="CabinetBacking"/> is
        ///     added or removed. This function always has side-effects
        ///     (incrementing or decrementing internal counters) which are used
        ///     to reference count the visibility of the countertop's
        ///     individual slices.
        /// </summary>
        public void SetCountertopOverhangVisible(CabinetLocalDirection overhangDirectionToAffect, bool isVisible)
        {
            MelcherLog.Log($"Set cabinet {overhangDirectionToAffect} overhang visible = {isVisible}", this);
            const float Threshold = 0.001f;

            // A predicate that filters SlicedMesh.Slices to only those
            // anchored to the requested cabinet side/overhang direction.
            System.Func<Vector3, bool> doesSliceAnchorMatchOverhangDirection =
                overhangDirectionToAffect switch
                {
                    CabinetLocalDirection.Left =>
                        static (normalizedAnchor) => normalizedAnchor.x < Threshold,

                    CabinetLocalDirection.Right =>
                        static (normalizedAnchor) => normalizedAnchor.x > 1.0f - Threshold,

                    CabinetLocalDirection.Front =>
                        static (normalizedAnchor) => normalizedAnchor.z < Threshold,

                    CabinetLocalDirection.Back =>
                        static (normalizedAnchor) => normalizedAnchor.z > 1.0f - Threshold,

                    _ => throw new System.ArgumentException(
                        $"Unhandled cabinet direction: {overhangDirectionToAffect}",
                        nameof(overhangDirectionToAffect)
                    )
                };

            int sliceIndex = -1;

            foreach (SlicedMesh.Slice slice in m_Countertop.slices)
            {
                ++sliceIndex;

                if (!doesSliceAnchorMatchOverhangDirection(slice.normalizedAnchor))
                {
                    // Skip slices that are not match the requested overhang
                    // direction.
                    continue;
                }

                // Update the "reference count" that represents the number of
                // backings affecting this countertop slice. Each backing that
                // affects the slice hides it.

                if (isVisible)
                {
                    // isVisible==true -> The backing no longer hides it.
                    --m_CountertopSliceAffectedByBackingCount[sliceIndex];

                    MelcherLog.Assert(
                        m_CountertopSliceAffectedByBackingCount[sliceIndex] >= 0,
                        $"The variable that counts the number of {MelcherFormat.ConsoleTypeName<CabinetBacking>()}'s " +
                        $"affecting this cabinet's countertop was decremented below zero. This is not " +
                        $"intended to happen (logic error). It's possible that " +
                        $"{MelcherFormat.ConsoleMethodName(nameof(SetCountertopOverhang))} is being called more than " +
                        $"once while a backing is added or removed, it being called at other times that are " +
                        $"not anticipated, or an exception prevented an earlier increment from succeeding.",
                        context: this
                    );
                }
                else
                {
                    // isVisible==false -> The backing hides it.
                    ++m_CountertopSliceAffectedByBackingCount[sliceIndex];
                }

                foreach (MeshRenderer renderer in slice.meshRenderers)
                {
                    if (!renderer)
                    {
                        // The renderer might be null. E.g., it was part of a
                        // corner cabinet prefab which initially contains
                        // multiple countertops but selectively destroys all
                        // but one when it is instantiated with a specific
                        // cabinet model.
                        continue;
                    }

                    // The slice is only visible if no backings (not just this
                    // one) affect it.
                    renderer.enabled =
                        m_CountertopSliceAffectedByBackingCount[sliceIndex] <= 0;
                }
            }
        }

        public OrientedBounds GetOrientedBounds()
        {
            Cuboid localCabinetCuboid = resizerCuboid;

            return
                new OrientedBounds(
                    new Bounds(localCabinetCuboid.center, localCabinetCuboid.size),
                    m_Transform.worldToLocalMatrix
                );
        }

        public void ResizeMeshToCabinetSize()
        {
            FitToProductWidth();
            MidOfRunCabinetResize();

            if (kitchen != null && m_CabinetType.value.IsEnd(includeTransitions: true))
            {
                // Match end cabinets' depth/height to the intended neighbour.

                Extent neighbourOnExtent =
                    m_CabinetType.value switch
                    {
                        CabinetType.EndLeft or CabinetType.LowLeftToHighRight => Extent.Max,
                        CabinetType.EndRight or CabinetType.HighLeftToLowRight => Extent.Min,
                        _ => throw new System.InvalidOperationException($"Unsupported cabinet end type: {m_CabinetType.value}")
                    };

                Cabinet neighbour =
                    kitchen.layout.GetCabinetNeighbour(this, neighbourOnExtent);

                if (neighbour)
                {
                    if (kitchen.selectedBuildingMethod is KitchenBuildingMethod.Catalog or KitchenBuildingMethod.Sections)
                    {
                        // Gemini and Stoll have cabinets whose depths differ.

                        float neighbourDepthInMeters = neighbour.sizeInMeters.z;

                        if (neighbour.Is(CabinetType.Corner))
                        {
                            // Special case: Don't match the depth of a
                            // corner cabinet because it's depth is along a
                            // different axis.

                            if (neighbour.HasCustomOccupantOrientation())
                            {
                                // Special-special case: 45-degree corners are
                                // assumed to use the kitchen's standard depth.
                                neighbourDepthInMeters =
                                    kitchen.selectedBrandCatalog ?
                                        kitchen.selectedBrandCatalog.standardCabinetDepthInInches * Const.Inches2Meters :
                                        kitchen.baseCabinetDepthInMeters;
                            }
                            else
                            {
                                // If it's any other type of corner, use whichever is shorter
                                // as depth
                                if(neighbour.sizeInMeters.z > neighbour.sizeInMeters.x)
                                {
                                    neighbourDepthInMeters = neighbour.sizeInMeters.x;
                                }
                                else
                                {
                                    neighbourDepthInMeters = neighbour.sizeInMeters.z;
                                }
                            }
                        }

                        m_SizeInMeters.z = neighbourDepthInMeters;

                        if (kitchen.selectedBrandCatalog &&
                            kitchen.selectedBrandCatalog.ruleset == BrandCatalogRuleset.WeatherStrong)
                        {
                            m_SizeInMeters.x =
                                neighbour.IsProductCategoryAllowedInSection(ProductCategorySpecific.Fridge) ?
                                    0.0f :
                                    kitchen.selectedBrandCatalog.cabinetEndPanelInfo.specificPanelWidthInMeters;
                        }

                        // Ensure that end panels are resized to accommodate
                        // optional side cabinet panels (Catalog mode only).
                        if (kitchen.selectedBrandCatalog)
                        {
                            float endPanelWidthInMeters = 0f;

                            if (hasSelectedEndPanel)
                            {
                                endPanelWidthInMeters =
                                    kitchen.selectedBrandCatalog.cabinetRuntimeEndPanelInfo.specificPanelWidthInMeters;
                            }

                            m_SizeInMeters.x = endPanelWidthInMeters;
                        }
                    }

                    // Some cabinets (like Stoll's Adjustable Smoker Cabinets)
                    // are shorter than other cabinets. Cabinet ends must
                    // therefore be shortened or raised to meet any type of
                    // cabinet next to it.
                    m_SizeInMeters.y = neighbour.sizeInMeters.y;
                }
            }

            if (m_Cabinet && m_IsCabinetModelScalable)
            {
                Transform cabinetTransform = m_Cabinet.transform;

                Vector3 cabinetLocalScale = m_SizeInMeters;

                if (isEndPanelOnGround)
                {
                    Vector3 cabinetLocalPosition = cabinetTransform.localPosition;
                    cabinetLocalPosition.y = 0f;
                    cabinetTransform.localPosition = cabinetLocalPosition;
                }
                else if (kitchen != null &&
                    kitchen.selectedBrandCatalog &&
                    m_CabinetModelType == CabinetModelType.Procedural &&
                    (!m_CabinetSectionVisual || m_CabinetSectionVisual.info.cabinetSectionFootingType != CabinetSectionFootingType.OnGround))
                {
                    // Fix up the scale of procedural cabinets. Procedural
                    // cabinets normally scale the full height of sizeInMeters
                    // but when these cabinets are on legs (Catalog), we must
                    // account for them so the cabinet doesn't scale too tall.
                    cabinetLocalScale.y -=
                        kitchen.selectedBrandCatalog.cabinetSectionLegStyle.heightInMeters;
                }

                if (kitchen != null &&
                    m_CabinetModelType == CabinetModelType.Procedural)
                {
                    // To avoid z-fighting, the cabinet will not extend into
                    // the countertop. This whole system needs reworking
                    // because the countertop is always meant to rest on top of
                    // the cabinet, not "sink" down into it.
                    cabinetLocalScale.y -= kitchen.countertopThicknessInMeters;
                }

                cabinetTransform.localScale = cabinetLocalScale;

                static bool IsAlmostZero(float value)
                {
                    const float Threshold = 0.001f;

                    return MelcherCommon.IsAlmostEqual(value, 0.0f, Threshold);
                }

                bool isCabinetVisible =
                    !IsAlmostZero(m_SizeInMeters.x) &&
                    !IsAlmostZero(m_SizeInMeters.y) &&
                    !IsAlmostZero(m_SizeInMeters.z);

                if (!ProFitUtility.IsPrefab(gameObject))
                {
                    // Only modify the visibility of non-prefabs. This keeps
                    // assets from being modified unintentionally.

                    m_Cabinet.SetActive(isCabinetVisible);
                }
            }

            if (m_Countertop)
            {
                // Size the countertop to match the cabinet's size
                Cuboid newCountertopCuboid = new Cuboid(m_SizeInMeters);

                {
                    // Preserve the countertop thickness
                    Cuboid oldCountertopCuboid = m_Countertop.cuboid;
                    newCountertopCuboid.yMax = m_SizeInMeters.y;
                    newCountertopCuboid.yMin = newCountertopCuboid.yMax - oldCountertopCuboid.height;
                }

                if (Is(CabinetType.CornerWedge))
                {
                    // Extrude the countertop of corner wedge cabinets by an inch.
                    // (Add 2 inches because we're adjusting A and B in A^2 + B^2 = C^2)
                    newCountertopCuboid.zMin -= Const.Inches2Meters * 2.0f;
                    newCountertopCuboid.zMax += Const.Inches2Meters * 2.0f;
                    newCountertopCuboid.xMin -= Const.Inches2Meters * 2.0f;
                    newCountertopCuboid.xMax += Const.Inches2Meters * 2.0f;
                }

                // Resize the cabinet's countertop to stop immediately
                // where it crosses the backing riser. This is to prevent
                // the countertop from extending into the backing. This only
                // happens to cabinets that are of standard height because
                // otherwise shorter cabinets are unnecessarily modified to fit
                // a backing they do not reach.

                if (kitchen != null && isStandardHeight)
                {
                    foreach (CabinetSpan span in kitchen.layout.spans)
                    {
                        if (!span.Contains(this))
                        {
                            continue;
                        }

                        CabinetBacking backing = span.backing;

                        if (!backing || !backing.hasRiserAndRiserCountertop || !backing.cropCabinetCountertops)
                        {
                            continue;
                        }

                        CabinetLocalDirection overhangDirection =
                            span.TransformDirectionFromSpanToCabinet(
                                CabinetLocalDirection.Back,
                                this
                            );

                        switch (overhangDirection)
                        {
                            case CabinetLocalDirection.Back:
                                newCountertopCuboid.zMax -= backing.riserCuboid.depth;
                                break;

                            case CabinetLocalDirection.Right:
                                newCountertopCuboid.xMax -= backing.riserCuboid.depth;
                                break;

                            default:
                                MelcherLog.LogError($"Unhandled countertop overhang direction: {overhangDirection}", this);
                                break;
                        }
                    }
                }

                m_Countertop.UpdateSlices(newCountertopCuboid);
            }

            // Hide/show the toe kick based on the current setting but avoid
            // dirtying the prefab unnecessarily.
            if (!MelcherUnityUtility.IsPartOfAnyPrefab(this))
            {
                RefreshToeKick();
            }

            // Handle sizing the cabinet supports. If the kitchen is not
            // intended to have supports, they will be hidden.
            //
            // To properly size the supports, each support's neighbour in it's
            // adjacent cabinet must be found.

            TransformHelper neighbourOfFrontLeftColumn = null;
            TransformHelper neighbourOfBackLeftColumn = null;
            TransformHelper neighbourOfFrontRightColumn = null;
            TransformHelper neighbourOfBackRightColumn = null;

            Cabinet leftCabinet = null;
            Cabinet rightCabinet = null;

            bool areColumnsVisible = true;

            if (kitchen != null)
            {
                kitchen.layout.GetCabinetNeighbours(
                    centerCabinet: this,
                    out leftCabinet,
                    out rightCabinet
                );

                if (leftCabinet)
                {
                    if (leftCabinet.cabinetType == CabinetType.Corner)
                    {
                        //  +-------------------+-------------------+
                        //  |       Back        |       Back        |
                        //  |                   |                   |
                        //  |                   |                   |
                        //  | Left        Right | L  leftCabinet  R |
                        //  |                   |     (corner)      |
                        //  |                   |                   |
                        //  |       Front       |       Front       |
                        //  +-------------------+===================+
                        //                      ||      Left       ||
                        //                      ||                 ||
                        //                      ||                 ||
                        //                      ||F    `this`     B||
                        //                      ||                 ||
                        //                      ||                 ||
                        //                      ||      Right      ||
                        //                      +===================+
                        //
                        //  Showing how the cabinet vertices meet after a corner
                        //  section. `this` cabinet's back-left vertex overlaps
                        //  its left neighbour's front-right vertex.

                        neighbourOfFrontLeftColumn = leftCabinet.m_FrontLeftColumn;
                        neighbourOfBackLeftColumn = leftCabinet.m_FrontRightColumn;
                    }
                    else
                    {
                        //  +-------------------+===================+-------------------+
                        //  |       Back        ||      Back       ||       Back        |
                        //  |                   ||                 ||                   |
                        //  |                   ||                 ||                   |
                        //  | L  leftCabinet  R ||L    `this`     R|| Left        Right |
                        //  |    (non-corner)   ||                 ||                   |
                        //  |                   ||                 ||                   |
                        //  |       Front       ||      Front      ||       Front       |
                        //  +-------------------+===================+-------------------+
                        //
                        //  Showing how the cabinet corners meet after a
                        //  non-corner section. `this` cabinet's back-left
                        //  vertex overlaps its left neighbour's back-right
                        //  vertex.

                        neighbourOfFrontLeftColumn = leftCabinet.m_FrontRightColumn;
                        neighbourOfBackLeftColumn = leftCabinet.m_BackRightColumn;
                    }
                }

                if (rightCabinet)
                {
                    if (cabinetType == CabinetType.Corner)
                    {
                        //  +-------------------+===================+
                        //  |       Back        ||      Back       ||
                        //  |                   ||                 ||
                        //  |                   ||                 ||
                        //  | Left        Right ||L    `this`     R||
                        //  |                   ||    (corner)     ||
                        //  |                   ||                 ||
                        //  |       Front       ||      Front      ||
                        //  +-------------------+===================+
                        //                      |       Left        |
                        //                      |                   |
                        //                      |                   |
                        //                      | F  rightCabinet B |
                        //                      |                   |
                        //                      |                   |
                        //                      |       Right       |
                        //                      +-------------------+
                        //
                        //  Showing how a cabinet's vertices meet its right
                        //  neighbour, if the current section is a corner.
                        //  `this` cabinet's front-right vertex overlaps its
                        //  right neighbour's back-left vertex.

                        neighbourOfFrontRightColumn = rightCabinet.m_FrontLeftColumn;

                        // Our back-right corner has no neighbour.
                    }
                    else
                    {
                        //  +-------------------+===================+-------------------+
                        //  |       Back        ||      Back       ||       Back        |
                        //  |                   ||                 ||                   |
                        //  |                   ||                 ||                   |
                        //  | L               R ||L    `this`     R|| L  rightCabinet R |
                        //  |                   ||   (non-corner)  ||                   |
                        //  |                   ||                 ||                   |
                        //  |       Front       ||      Front      ||       Front       |
                        //  +-------------------+===================+-------------------+
                        //
                        //  Showing how the cabinet corners meet after a
                        //  non-corner section. `this` cabinet's back-right
                        //  vertex overlaps its left neighbour's back-left
                        //  vertex.

                        neighbourOfFrontRightColumn = rightCabinet.m_FrontLeftColumn;
                        neighbourOfBackRightColumn = rightCabinet.m_BackLeftColumn;
                    }
                }

                areColumnsVisible = kitchen.doCabinetsHaveColumns;
            }

            float countertopThicknessMeters =
                m_Kitchen != null ?
                    m_Kitchen.countertopThicknessInMeters :
                    0.0f;

            float defaultColumnHeight = m_SizeInMeters.y - countertopThicknessMeters;

            float backColumnHeight = defaultColumnHeight;

            if (m_Kitchen != null && isStandardHeight)
            {
                // Find the height of the tallest backing shared by this
                // cabinet. Only corner cabinets will have more than one
                // cabinet span and therefore potentially more than one
                // backing. The backing height is needed to stretch the
                // back columns to meet the top backing's countertop.
                float maxBackingRiserHeight = 0.0f;

                foreach (CabinetSpan span in m_Kitchen.layout.spans)
                {
                    if (span.backing &&
                        span.backing.hasRiserAndRiserCountertop &&
                        span.Contains(this)) // do `span.Contains()` last in `if` condition for slight optimization
                    {
                        const float SmudgeToAvoidZFighting = 0.001f;
                        maxBackingRiserHeight = Mathf.Max(maxBackingRiserHeight, countertopThicknessMeters + span.backing.riserCuboid.height - SmudgeToAvoidZFighting);
                    }
                }

                // Extend the back column through the main cabinet countertop
                // and to be flush with the riser's top. The backing's riser
                // intersects the cabinet by the countertop's height and
                // therefore to have the column extend to to the top of the
                // riser and not poke through the backings countertop, the
                // kitchen countertop's thickness must be subtracted.
                if (maxBackingRiserHeight > 0.0f)
                {
                    backColumnHeight += maxBackingRiserHeight - kitchen.countertopThicknessInMeters;
                }
            }

            if (m_FrontLeftColumn)
            {
                ResizeColumn(
                    m_FrontLeftColumn,
                    neighbourOfFrontLeftColumn,
                    new Vector3(0.0f, 0.0f, 0.0f),
                    defaultColumnHeight,
                    areColumnsVisible && (!leftCabinet || leftCabinet.cabinetType != CabinetType.HighLeftToLowRight)
                );
            }

            if (m_FrontRightColumn)
            {
                ResizeColumn(
                    m_FrontRightColumn,
                    neighbourOfFrontRightColumn,
                    new Vector3(1.0f, 0.0f, 0.0f),
                    defaultColumnHeight,
                    areColumnsVisible && (!rightCabinet || rightCabinet.cabinetType != CabinetType.LowLeftToHighRight)
                );
            }

            if (m_BackLeftColumn)
            {
                ResizeColumn(
                    m_BackLeftColumn,
                    neighbourOfBackLeftColumn,
                    new Vector3(0.0f, 0.0f, 1.0f),
                    backColumnHeight,
                    areColumnsVisible && (!leftCabinet || leftCabinet.cabinetType != CabinetType.HighLeftToLowRight)
                );
            }

            if (m_BackRightColumn)
            {
                ResizeColumn(
                    m_BackRightColumn,
                    neighbourOfBackRightColumn,
                    new Vector3(1.0f, 0.0f, 1.0f),
                    backColumnHeight,
                    areColumnsVisible && (!rightCabinet || rightCabinet.cabinetType != CabinetType.LowLeftToHighRight)
                );
            }

            if (m_CornerColumn && kitchen != null)
            {
                // Position the column used by corner cabinets. This column
                // is scaled in both the X and Z axis, which is unique compared
                // to the other columns. Additionally, the column must be inset
                // into the cabinet slightly.

                // The corner column is positioned directly under the
                // countertop's back-right overhang. The column has a 180
                // rotation so it scales towards the cabinet's origin point.

                Vector3 positionInMeters = m_SizeInMeters;
                positionInMeters.x += kitchen.baseCountertopOverhangInMeters.right;
                positionInMeters.z += kitchen.baseCountertopOverhangInMeters.towardMaxZ;
                positionInMeters.y = 0.0f;
                m_CornerColumn.offsetInMeters = positionInMeters;

                m_CornerColumn.sizeInMeters =
                    new Vector3(
                        x: Const.CabinetColumnWidthInMeters,
                        y: backColumnHeight,
                        z: Const.CabinetColumnWidthInMeters
                    );

                if (!ProFitUtility.IsPrefab(gameObject))
                {
                    m_CornerColumn.gameObject.SetActive(areColumnsVisible);
                }
            }

            foreach (CabinetSectionLeg leg in m_CabinetLegs)
            {
                leg.RefreshPosition();
            }

            for (int ventIndex = 0; ventIndex < m_CabinetVents.Count; ++ventIndex)
            {
                bool isAtTop = (ventIndex % 2) != 0;
                bool isOnRight = ventIndex >= 2;

                // Rotate the vent to point out the intended direction from
                // the cabinet.

                Transform vent = m_CabinetVents[ventIndex];

                vent.localEulerAngles =
                    new Vector3(0.0f, isOnRight ? 180.0f : 0.0f, 0.0f);

                // Use normalized coordinates to find the top or bottom
                // depth-midline position for the vent.

                float normalizedX = isOnRight ? 1.0f : 0.0f;

                Vector3 ventLocalPosition =
                    Cuboid.NormalizedToPoint(
                        resizerCuboid,
                        new Vector3(normalizedX, 0.0f, 0.5f)
                    );

                // Position the vent vertically along the cabinet side face
                // (uses an absolute offset for exact vent placement).

                float ventDistanceFromEdgeInMeters =
                    kitchen.selectedBrandCatalog.cabinetVentDistanceFromEdgeInMeters;

                ventLocalPosition.y =
                    isAtTop ?
                        resizerCuboid.yMax - ventDistanceFromEdgeInMeters :
                        ventDistanceFromEdgeInMeters + kitchen.selectedBrandCatalog.cabinetSectionLegStyle.heightInMeters;

                vent.localPosition = ventLocalPosition;
            }
        }

        public bool IsProductCategoryAllowedInSection(ProductCategorySpecific category)
        {
            if (cabinetSectionVisual)
            {
                foreach (MelcherStableEnum<ProductCategorySpecific> allowedCategory in m_CabinetSectionVisual.info.productCategoriesAllowedInSection)
                {
                    if (allowedCategory.value == category)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     This is used to fix-up measurements returned by <see
        ///     cref="OccupiableVolume.FindNearestOccupantInDirection(Occupant, CabinetLocalDirection, out float, FindNearestOccupantPredicate)"/>
        ///     where the volume is a cabinet and the distance should better
        ///     match the available visual distance to the end of the cabinet.
        ///     <br /><br />
        ///
        ///     In other words, this ensures the distance correctly accommodates
        ///     extra padding between the occupiable area of the cabinet and the
        ///     true end of the cabinet. It also ensures measurements are
        ///     correctly adjusted when the cabinet neighbours a corner wedge
        ///     cabinet.
        /// </summary>
        public float AdjustCabinetDistanceToEdgeOfVisualArea(float distanceInMeters, CabinetLocalDirection direction)
        {
            float padding;

            // We need to fixup the measurements when no product is
            // found to account for possible occupiable area padding
            // when the cabinet neighbours a corner piece.
            switch (direction)
            {
                default:
                    throw new System.Exception("Unhandled direction: " + direction);

                case CabinetLocalDirection.Left:
                    padding = occupiablePaddingInMeters.left;
                    break;

                case CabinetLocalDirection.Right:
                    padding = occupiablePaddingInMeters.right;
                    break;

                case CabinetLocalDirection.Down:
                    padding = occupiablePaddingInMeters.bottom;
                    break;

                case CabinetLocalDirection.Up:
                    padding = occupiablePaddingInMeters.top;
                    break;

                case CabinetLocalDirection.Front:
                    padding = occupiablePaddingInMeters.towardMaxZ;
                    break;

                case CabinetLocalDirection.Back:
                    padding = occupiablePaddingInMeters.towardMinZ;
                    break;
            }

            if (!Is(CabinetType.Corner) && direction.ToAxis() == MeasurementAxis.X)
            {
                CabinetSpan cabinetSpan = kitchen.layout.FindCabinetSpan(this);

                cabinetSpan.GetCornerWedges(
                    out Cabinet leftCornerWedge,
                    out Cabinet rightCornerWedge
                );

                kitchen.layout.GetCabinetNeighbours(
                    this,
                    out Cabinet leftCabinet,
                    out Cabinet rightCabinet
                );

                if ((direction == CabinetLocalDirection.Left && IsWedgedCorner(leftCabinet, leftCornerWedge)) ||
                    (direction == CabinetLocalDirection.Right && IsWedgedCorner(rightCabinet, rightCornerWedge)))
                {
                    // Avoid applying padding in this direction because the
                    // neighbour corner represents a wedged cabinet which
                    // effectively means that the padding does align with the
                    // reduced visual area available for products.

                    padding = 0.0f;
                }

                static bool IsWedgedCorner(Cabinet neighborCabinet, Cabinet neighborCabinetWedge)
                {
                    return
                        neighborCabinet != null &&
                        neighborCabinetWedge != null &&
                        neighborCabinetWedge.cornerCabinetBeingWedged == neighborCabinet;
                }
            }

            return distanceInMeters + padding;
        }

        /// <summary>
        ///     Try to resize this cabinet to the exact width of <paramref
        ///     name="specificOccupant"/>. If the parameter is omitted (i.e.,
        ///     <see langword="null"/>), the function will look for the first
        ///     occupant in the cabinet that has the <see
        ///     cref="ProductVisualContent.fitSectionToProductWidth"/> flag. If
        ///     none, exists nothing happens.
        /// </summary>
        /// <param name="specificOccupant">
        ///     The occupant that the cabinet should be fitted to. If null,
        ///     the function will look for an occupant that requests the
        ///     cabinet's size to fit the cabinet.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> to indicate the cabinet was resized or
        ///     doesn't need resizing. Otherwise, return <see
        ///     langword="false"/> to indicate there was an error.
        /// </returns>
        public bool FitToProductWidth(Occupant specificOccupant = null)
        {
            if (kitchen == null ||
                !allowedFitToProductWidthBuildingMethods.HasFlag(kitchen.selectedBuildingMethod))
            {
                // This cabinet doesn't fit to products in this building method.
                return true;
            }

            if (!specificOccupant)
            {
                // A specific occupant wasn't provided which means we will try
                // to locate the first occupant that will work.

                specificOccupant =
                    m_Occupants.mutable.Find(
                        static occupant => occupant.productVisual.info.fitSectionToProductWidth
                    );

                if (!specificOccupant)
                {
                    // No occupants require the cabinet to resize.
                    return true;
                }
            }

            float desiredCabinetWidthInMeters =
                CalculateOccupancyCuboidForOccupant(specificOccupant).width;

            float lengthMetersDelta =
                desiredCabinetWidthInMeters - occupiableCuboid.width;

            // Attempt to shrink/expand the cabinet to the minimum size
            // required to fit the dropped product.
            if (!MelcherCommon.IsAlmostEqual(lengthMetersDelta, 0.0f, 0.001f) &&
                !TryToResizeLength(ref lengthMetersDelta, Extent.Max))
            {
                MelcherLog.Log("Could not resize cabinet to fit occupant: " + desiredCabinetWidthInMeters);
                return false;
            }

            if (specificOccupant.productVisual.info.runtimePrefabCategory == ProductCategorySpecific.Fridge &&
                kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                kitchen.selectedBrandCatalog.fridgeBackPanelInfos != null)
            {
                // WeatherStrong has specific fridge panel sizes. Find the
                // one that is just large enough to fit this product.

                BrandCatalog.FridgePanelInfo fridgePanel =
                    kitchen.selectedBrandCatalog.FindFirstFridgeBackPanel(
                        desiredCabinetWidthInMeters
                    );

                if (fridgePanel == null)
                {
                    MelcherLog.Log("No fridge panel found for fridge width: " + desiredCabinetWidthInMeters);
                    return false;
                }

                if (fridgePanel.specificPanelWidthInMeters > 0.0f)
                {
                    desiredCabinetWidthInMeters = fridgePanel.specificPanelWidthInMeters;
                }

                kitchen.layout.GetCabinetNeighbours(
                    this,
                    out Cabinet leftCabinet,
                    out Cabinet rightCabinet
                );

                if (!leftCabinet || leftCabinet.cabinetType.IsEnd())
                {
                    float fridgePadding =
                        kitchen.selectedBrandCatalog.fridgeEndPanelInfo.specificPanelWidthInMeters;

                    TryToResizeLength(ref fridgePadding, Extent.Min, forceAutoCenterProducts: false);
                }

                if (!rightCabinet || rightCabinet.cabinetType.IsEnd())
                {
                    float fridgePadding =
                        kitchen.selectedBrandCatalog.fridgeEndPanelInfo.specificPanelWidthInMeters;

                    TryToResizeLength(ref fridgePadding, Extent.Max, forceAutoCenterProducts: false);
                }
            }

            return true;
        }

        // MARK: Cabinet resize when in mid of run
        public void MidOfRunCabinetResize()
        {
            if (cabinetSectionVisual &&
                cabinetSectionVisual.info.sectionEndPanelSizeInches > 0 &&
                kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
            {
                kitchen.layout.GetCabinetNeighbours(
                    this,
                    out Cabinet leftCabinet,
                    out Cabinet rightCabinet
                );

                if(leftCabinet && leftCabinet.cabinetType == CabinetType.HighLeftToLowRight)
                {
                    leftCabinet = kitchen.layout.GetCabinetNeighbour(leftCabinet, Extent.Min);
                }

                if(rightCabinet && rightCabinet.cabinetType == CabinetType.LowLeftToHighRight)
                {
                    rightCabinet = kitchen.layout.GetCabinetNeighbour(rightCabinet, Extent.Max);
                }

                // resize to default
                if(m_AppliedMidOfRunPaddingInMetersLeft > 0 || m_AppliedMidOfRunPaddingInMetersRight > 0)
                {
                    Vector3 newSizeInMeters = m_SizeInMeters;
                    newSizeInMeters.x -= m_AppliedMidOfRunPaddingInMetersLeft + m_AppliedMidOfRunPaddingInMetersRight;
                    m_SizeInMeters = newSizeInMeters; // local variable is used to prevent resizing the cabinet multiple times

                    m_AppliedMidOfRunPaddingInMetersLeft = 0;
                    m_AppliedMidOfRunPaddingInMetersRight = 0;
                }
                
                float leftCabinetPanelSize = leftCabinet && leftCabinet.cabinetSectionVisual ? leftCabinet.cabinetSectionVisual.info.sectionEndPanelSizeInches : 0;
                float rightCabinetPanelSize = rightCabinet && rightCabinet.cabinetSectionVisual ? rightCabinet.cabinetSectionVisual.info.sectionEndPanelSizeInches : 0;

                if (leftCabinet && leftCabinet.cabinetType.IsEnd() == false && leftCabinetPanelSize == 0)
                {
                    float endPadding =
                        cabinetSectionVisual.info.sectionEndPanelSizeInches * Const.Inches2Meters;

                    TryToResizeLength(ref endPadding, Extent.Min, forceAutoCenterProducts: true);
                    m_AppliedMidOfRunPaddingInMetersLeft = endPadding;
                }

                if (rightCabinet && rightCabinet.cabinetType.IsEnd() == false && rightCabinetPanelSize == 0)
                {
                    float endPadding =
                        cabinetSectionVisual.info.sectionEndPanelSizeInches * Const.Inches2Meters;

                    TryToResizeLength(ref endPadding, Extent.Max, forceAutoCenterProducts: true);
                    m_AppliedMidOfRunPaddingInMetersRight = endPadding;

                    
                }

                if(leftCabinet && rightCabinet)
                {
                    Debug.Log($"IS END {leftCabinet.cabinetType.IsEnd()}@{leftCabinet.name} {rightCabinet.cabinetType.IsEnd()}@{rightCabinet.name} {leftCabinetPanelSize} {rightCabinetPanelSize}", this);
                }

                if(m_AppliedMidOfRunPaddingInMetersLeft == 0 && m_AppliedMidOfRunPaddingInMetersRight == 0)
                {
                    float endPadding = 0f;

                    TryToResizeLength(ref endPadding, Extent.Max, forceAutoCenterProducts: true);
                }
            }
        }

        /// <summary>
        ///     Called at runtime and in the editor (via OnValidate()) to
        ///     synchronize the support column's size with the size of the
        ///     cabinet.
        /// </summary>
        /// <param name="column">
        ///     The helper object representing the column to be resized.
        /// </param>
        /// <param name="isContiguousWithNeighbour">
        ///     Whether this column represents half a column, with the other
        ///     half belonging to a neighbouring cabinet. This affects the width
        ///     of <paramref name="column"/>'s mesh.
        /// </param>
        /// <param name="normalizedAnchorWithinCabinet">
        ///     The origin point, normalized and relative to the cabinet's full
        ///     size where this support will be positioned.
        /// </param>
        /// <param name="areColumnsVisible">
        ///     Whether the column will be visible. Kitchens that are built in
        ///     the Stoll version of the app have columns, otherwise columns are
        ///     hidden.
        /// </param>
        protected void ResizeColumn(TransformHelper column, bool isContiguousWithNeighbour, Vector3 normalizedAnchorWithinCabinet, float columnHeight, bool areColumnsVisible)
        {
            Cuboid cabinetCuboid = new Cuboid(m_SizeInMeters);

            column.GetTransform().localPosition =
                Cuboid.NormalizedToPoint(cabinetCuboid, normalizedAnchorWithinCabinet);

            CuboidPadding countertopOverhangInMeters =
                m_Kitchen != null ? m_Kitchen.baseCountertopOverhangInMeters : CuboidPadding.one * Const.Inches2Meters;

            Vector3 supportSizeInMeters =
                new Vector3(
                    x: Const.CabinetColumnWidthInMeters,
                    y: columnHeight,
                    z: countertopOverhangInMeters.towardMaxZ
                );

            if (isContiguousWithNeighbour)
            {
                // Use half the support width because the support is contiguous
                // with its neighbour's half.
                supportSizeInMeters.x *= 0.5f;
            }

            column.sizeInMeters = supportSizeInMeters;

            if (!ProFitUtility.IsPrefab(gameObject))
            {
                // Only modify the visibility of non-prefabs. This keeps
                // assets from being modified unintentionally.

                column.gameObject.SetActive(areColumnsVisible);
            }
        }

        public override bool CanContainProduct(ProductInfo productInfo, ProductVisualInfo productVisualInfo, ProductVisualContent productVisualContent)
        {
            bool isProductAllowed = m_CanHaveProducts;

            if (isProductAllowed &&
                m_CabinetSectionProductInfo != null &&
                !productVisualInfo.ignoreCabinetProductWhitelist)
            {
                ProductVisual cabinetVisual =
                    kitchen.productVisualDatabase.FindRuntimeVisualByID(
                        m_CabinetSectionProductInfo.visualId
                    );

                isProductAllowed = cabinetVisual.info.IsCategoryAllowedInSection(productVisualInfo.runtimePrefabCategory);

                CabinetCompatibilityInfo cabinetCompatibility =
                    ProFitUtility.FindCompatibilityById(m_CabinetSectionProductInfo.compatibilityId);

                if (cabinetCompatibility != null)
                {
                    if (isProductAllowed && cabinetCompatibility.productTagIdDenylist.Count > 0)
                    {
                        // Enforce "must not have tag" constraint

                        bool tagIsInDenylist =
                            cabinetCompatibility.productTagIdDenylist.Any(
                                componentTag => productInfo.productTagIds.Contains(componentTag)
                            );

                        isProductAllowed = !tagIsInDenylist;
                    }

                    if (isProductAllowed && cabinetCompatibility.productTagIdAllowlist.Count > 0)
                    {
                        // Enforce "must have tag" constraint
                        bool tagIsInAllowlist =
                            cabinetCompatibility.productTagIdAllowlist.Any(
                                componentTag => productInfo.productTagIds.Contains(componentTag)
                            );

                        isProductAllowed = tagIsInAllowlist;
                    }
                }
            }

            // Handle special constraints imposed by 45-degree corner cabinets.
            // Namely, that the "width" of a 45-degree cabinet's front face is
            // much smaller than the width recognized by the Unity app.
            if (HasCustomOccupantOrientation() &&
                productVisualInfo.runtimePrefabCategory != ProductCategorySpecific.Smoker &&
                (!productVisualInfo.anchor.IsOnTop() || productInfo.cutOutSizeInMeters.y > Occupant.MinimumPlungeDepth))
            {
                Cuboid occupancyCuboid =
                    CalculateOccupancyCuboidForProductInfo(
                        productInfo,
                        cabinetSectionVisual,
                        productVisualContent
                    );

                if (occupancyCuboid.zMin > 0.0f)
                {
                    // This product is allowed in all cases because it is not
                    // flush with the front. I.e., we can ignore any
                    // constraints about fitting within the front face.
                    return true;
                }
                else if (productVisualInfo.anchor.IsOnFront() &&
                    !m_CabinetSectionVisual.info.sectionCanHaveFrontProducts)
                {
                    // Some cabinets don't have a single unified face (corner
                    // return's) and therefore any front-mounted product is
                    // forbidden.
                    return false;
                }

                // The occupant-orientation-transform in the cabinet prefab is
                // never changed by runtime code. Therefore we know it is
                // snapped to one of the bottom corners of the cabinet's
                // 45-degree face and can therefore use Pythagoras formula to
                // calculate the width of the 45-degree face. This assumption
                // is not true for L-shaped corners. In that case, the point is
                // snapped to the inner corner.

                float triangleLengthA =
                    m_CabinetSectionVisualPrefab.sectionOccupantOrientation.localPosition.magnitude;

                float widthOf45DegreeFace =
                    Mathf.Sqrt(triangleLengthA * triangleLengthA + triangleLengthA * triangleLengthA);

                isProductAllowed =
                    occupancyCuboid.width <= widthOf45DegreeFace;
            }

            return isProductAllowed;
        }

        /// <summary>
        ///     Attempt to adjust the size of the cabinet (regardless of
        ///     whether it is marked resizable). In most cases, this is not
        ///     intended to be called directly, instead
        ///     <see cref="TryToResizeLengthAsUser(ref float, Extent)"/> should
        ///     be preferred. The one case this function was specifically added
        ///     for is resizing (shrinking) a cabinet to fit a newly added
        ///     product. In Gemini, some cabinets are user resizable only while
        ///     no product is added but the product is already added by the
        ///     time that logic triggers meaning this function allows us to
        ///     bypass the <see cref="isResizable"/> check.
        /// </summary>
        /// <param name="lengthMetersDelta">
        ///     The change (in meters) to make to the cabinet's length.
        ///     Negative numbers make it smaller and vice versa.
        /// </param>
        /// <param name="extent"></param>
        /// <returns>
        ///     If the cabinet was resized at all, even if not the fully
        ///     requested amount, return <see langword="true"/>. Otherwise,
        ///     return <see langword="false"/>.
        /// </returns>
        public bool TryToResizeLength(ref float lengthMetersDelta, Extent extent, bool? forceAutoCenterProducts = null)
        {
            bool autoCenterProducts =
                kitchen.AreProductsForcedToCenterOfCabinets(cabinetForRuleset: this);

            if (forceAutoCenterProducts.HasValue)
            {
                autoCenterProducts = forceAutoCenterProducts.Value;
            }

            if (lengthMetersDelta < 0.0f &&
                IsAnyOccupantBlockingShrink(ref lengthMetersDelta, extent, autoCenterProducts))
            {
                // Can't become smaller if products are blocking it
                lengthMetersDelta = 0.0f;
                return false;
            }

            // Calculate the new target size
            Vector3 oldSizeInMeters = m_SizeInMeters;
            Vector3 newSizeInMeters = oldSizeInMeters;
            newSizeInMeters.x =
                Mathf.Clamp(
                    newSizeInMeters.x + lengthMetersDelta,
                    minSizeInMeters.x,
                    maxSizeInMeters.x
                );

            lengthMetersDelta = newSizeInMeters.x - m_SizeInMeters.x;

            if (MelcherCommon.IsAlmostEqual(lengthMetersDelta, 0.0f, 0.0001f))
            {
                // It's already at its min or max size
                return false;
            }

            m_SizeInMeters = newSizeInMeters;

            if (autoCenterProducts)
            {
                // Auto-center products. This was requested for Stoll's
                // Section building method. Auto-centering is also used by
                // Gemini (Catalog building method).

                // TODO: Was hoping to prevent OccupiableVolume's from knowing
                //       anything about CabinetSpan. But here, we need to know
                //       about the backing which is something only the span has
                //       access to. -Ryan
                CabinetSpan span = kitchen.layout.FindCabinetSpan(this);

                Cuboid? backingOccupancyInWorldSpace = null;

                if (span.backing)
                {
                    backingOccupancyInWorldSpace =
                        span.GetBackingOccupancyCuboidInWorldSpace(
                            span.backing.info,
                            span.backing
                        );
                }

                // Use a copy of the occupants list because the order of the
                // occupants in the original list may change while the loop is
                // iterated.

                s_OccupantsToCenter.Clear();
                s_OccupantsToCenter.AddRange(m_Occupants.readOnly);

                int occupantCount = s_OccupantsToCenter.Count;
                for (int i = 0; i < occupantCount; ++i)
                {
                    Occupant occupant = s_OccupantsToCenter[i];

                    Cuboid occupantOccupancyCuboid =
                        CalculateOccupancyCuboidForOccupant(occupant);

                    bool didCenterOccupant = false;

                    using (var drawScope = Cuboid.StartDrawScope())
                    {
                        //occupant.occupiedVolume.occupiableCuboid
                        //    .Transform(occupant.occupiedVolume.GetTransform())
                        //    .Draw(
                        //        new Color32(0xFF, 0xA5, 0x0, 0xFF), // Color.orange
                        //        duration: 10.0f,
                        //        0.08f,
                        //        drawX: true,
                        //        offset: 5
                        //    );

                        //occupant.occupiedVolume.resizerCuboid
                        //    .Transform(occupant.occupiedVolume.GetTransform())
                        //    .Draw(
                        //        Color.red,
                        //        duration: 10.0f,
                        //        0.08f,
                        //        drawX: true,
                        //        offset: 6
                        //    );

                        //Cuboid desiredCuboid = occupantOccupancyCuboid;
                        //Vector3 desiredPosition = desiredCuboid.position;
                        //desiredPosition.x = (resizerCuboid.width - occupantOccupancyCuboid.width) * 0.5f;
                        //desiredCuboid.position = desiredPosition;

                        //desiredCuboid
                        //    .Transform(occupant.occupiedVolume.GetTransform())
                        //    .Draw(
                        //        Color.green,
                        //        duration: 5.0f,
                        //        offset: 5
                        //    );

                        //Debug.DrawLine(Vector3.zero, desiredCuboid.position, Color.green, 5.0f);

                        didCenterOccupant =
                            TryMoveOccupantTo(
                                occupant,
                                backingOccupancyInWorldSpace,
                                metersFromVolumeMinX: (resizerCuboid.width - occupantOccupancyCuboid.width) * 0.5f
                            );
                    }


                    if (!didCenterOccupant)
                    {
                        MelcherLog.LogError(
                            "Occupant could not be recentered after resize", context: occupant
                        );
                    }
                    else if (occupant.metersFromVolumeMinX < 0.0f ||
                        occupant.metersFromVolumeMinX > occupiableCuboid.xMax - occupantOccupancyCuboid.width)
                    {
                        MelcherLog.LogError(
                            "Resize caused an occupant to go out of bounds",
                            context: occupant
                        );
                    }
                }
            }
            else if (extent == Extent.Min)
            {
                // When resizing a cabinet's minimum extent, the products must
                // be counter-offset so they stay in place. (Otherwise they slide
                // with the resizing edge.)

                int occupantCount = m_Occupants.readOnly.Count;
                for (int i = 0; i < occupantCount; ++i)
                {
                    Occupant occupant = m_Occupants.readOnly[i];

                    occupant.metersFromVolumeMinX += lengthMetersDelta;

                    if (occupant.metersFromVolumeMinX < 0.0f ||
                        occupant.metersFromVolumeMinX > newSizeInMeters.x - occupant.currentCutOutSizeInMeters.x)
                    {
                        MelcherLog.LogError(
                            "Resize caused an occupant to go out of bounds",
                            context: occupant
                        );
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Called when the user attempts to resize a cabinet by dragging
        ///     its resizer widget.
        /// </summary>
        /// <param name="lengthMetersDelta">
        ///     The change (in meters) to make to the cabinet's length.
        ///     Negative numbers make it smaller and vice versa.
        /// </param>
        public override bool TryToResizeLengthAsUser(ref float lengthMetersDelta, Extent extent)
        {
            if (!isResizable)
            {
                // Don't resize if not resizable
                lengthMetersDelta = 0.0f;
                return false;
            }

            bool didResize = TryToResizeLength(ref lengthMetersDelta, extent);

            if (didResize && Is(CabinetType.CornerWedge))
            {
                // Wedge cabinets must adjust their depth to match their
                // length.
                sizeInMeters += Vector3.forward * lengthMetersDelta;
            }

            return didResize;
        }

        /// <summary>
        ///     Set the rendered cabinet and countertop materials. It is
        ///     important to note this is normally done when the user selects
        ///     a cabinet material which is handled in <see cref="Kitchen"/>.
        /// </summary>
        /// <param name="productCabinetMaterialIdentifier">
        ///     A material that may exist in a product/occupant's visual
        ///     prefab and (if found) will be replaced with <paramref
        ///     name="cabinetMaterial"/>" in the product/occupant instance so
        ///     it matches the cabinet finish.
        /// </param>
        public async UniTask SetCabinetMaterials(
            Material cabinetMaterial,
            Material countertopMaterial,
            Material productCabinetMaterialIdentifier,
            Material cutOutMaterialOverride = null)
        {
            MelcherLog.LogEditorOnly($"Changing cab materials {name}: {cabinetMaterial} | {countertopMaterial}", this);

            // This material will only affect the cabinet meshes themselves. It
            // allows material of the cabinet to be different from the material
            // products on the cabinet use as their "cabinet material". This
            // was added to be able to change refrigeration cabinets (Gemini)
            // to use a stainless steel texture. This effect (stainless steel
            // fridge cabinets) has been reverted, but the backing code still
            // exists.

            Material cabinetMaterialExclusive = cabinetMaterial;

            AppSkin appSkin = ProFitGlobals.appOptions.appSkin;

            bool isCabinetHighlighting =
                cabinetMaterial == appSkin.primarySelectionMaterial ||
                cabinetMaterial == appSkin.secondarySelectionMaterial;

            if (!isCabinetHighlighting)
            {
                if (kitchen.selectedBrandCatalog && kitchen.selectedBrandCatalog.defaultCabinetFinishOverride)
                {
                    // The brand catalog has the ability to override any
                    // cabinet finish. This is used by the Napoleon cabinet to
                    // force the carbon coloured finish.
                    cabinetMaterial = kitchen.selectedBrandCatalog.defaultCabinetFinishOverride;
                    cabinetMaterialExclusive = cabinetMaterial;
                }

                if (m_CabinetSectionProductInfo != null)
                {
                    // Allow custom cabinet's material to be overwritten when they
                    // are not selected.

                    ProductVisual cabinetSectionVisual =
                        kitchen.productVisualDatabase.FindRuntimeVisualByID(
                            cabinetSectionProductInfo.visualId
                        );

                    if (cabinetSectionVisual)
                    {
                        // Load the cabinet section prefab.
                        ProductVisualContent cabinetSectionVisualPrefab =
                            await ProductVisualContentLoader.FirstFromActiveScene()
                                .ProductVisualToContent(cabinetSectionVisual);

                        Material sectionMaterialOverride =
                            cabinetSectionVisualPrefab.sectionMaterialOverride;

                        if (sectionMaterialOverride)
                        {
                            cabinetMaterialExclusive = sectionMaterialOverride;
                        }
                    }
                }
            }

            if (m_CabinetMeshRenderers != null)
            {
                if (m_CabinetModelType == CabinetModelType.Authored && m_CabinetSectionVisual)
                {
                    // This cabinet is using an authored section. Therefore,
                    // it may have specific materials that need to appear
                    // (unless the cabinet is being highlighted).

                    MeshRenderer[] prefabMeshRenderers =
                        m_CabinetSectionVisualPrefab.sectionModelPrefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

                    if (m_CabinetMeshRenderers.Count != prefabMeshRenderers.Length)
                    {
                        MelcherLog.LogError($"Mismatched material count between instance and prefab ({MelcherFormat.ConsoleNumericValue(m_CabinetMeshRenderers.Count)} vs {MelcherFormat.ConsoleNumericValue(prefabMeshRenderers.Length)}): " + m_CabinetSectionVisualPrefab, this);
                        MelcherLog.LogError($"Instance: {name}", this);
                        MelcherLog.LogError($"Prefab: {m_CabinetSectionVisualPrefab.name}", m_CabinetSectionVisualPrefab);
                    }
                    else
                    {
                        Material kitchenDefaultMaterial =
                            await ProFitGlobals.appProfile.GetDefaultKitchenMaterial();

                        for (int i = 0; i < prefabMeshRenderers.Length; ++i)
                        {
                            Material[] meshMaterials = prefabMeshRenderers[i].sharedMaterials;

                            for (int j = 0; j < meshMaterials.Length; ++j)
                            {
                                if (isCabinetHighlighting || meshMaterials[j] == kitchenDefaultMaterial)
                                {
                                    // The cabinet is highlighted (so we want
                                    // to use the highlight material) or this
                                    // is a "default cabinet" material which
                                    // means it should be substituted for the
                                    // actual selected cabinet finish (as
                                    // opposed to being a material that was
                                    // specifically assigned at edit-time to
                                    // this cabinet).

                                    meshMaterials[j] = cabinetMaterialExclusive;
                                }
                            }

                            m_CabinetMeshRenderers[i].sharedMaterials = meshMaterials;
                        }
                    }
                }
                else
                {
                    // This is a procedural cabinet. In this case, the cabinet
                    // always uses the requested material.
                    foreach (MeshRenderer renderer in m_CabinetMeshRenderers)
                    {
                        ProFitUtility.SetAllRendererMaterials(renderer, cabinetMaterialExclusive);
                    }
                }
            }

            if (m_CabinetDoorInstance)
            {
                await m_CabinetDoorInstance.SetCabinetMaterials(
                    cabinetMaterial,
                    countertopMaterial,
                    productCabinetMaterialIdentifier
                );
            }

            if (m_Countertop)
            {
                foreach (SlicedMesh.Slice slice in m_Countertop.slices)
                {
                    foreach (MeshRenderer sliceRenderer in slice.meshRenderers)
                    {
                        if (sliceRenderer)
                        {
                            ProFitUtility.SetAllRendererMaterials(sliceRenderer, countertopMaterial);
                        }
                    }
                }
            }

            if (m_ToeKickLocalX && m_ToeKickLocalX.TryGetComponent(out Renderer toeKickLocalX))
            {
                ProFitUtility.SetAllRendererMaterials(toeKickLocalX, cabinetMaterial);
            }

            if (m_ToeKickLocalZ && m_ToeKickLocalZ.TryGetComponent(out Renderer toeKickLocalZ))
            {
                ProFitUtility.SetAllRendererMaterials(toeKickLocalZ, cabinetMaterial);
            }

            foreach (GameObject insetToeKickVariation in m_InsetToeKickVariations)
            {
                if (insetToeKickVariation.TryGetComponent(out Renderer renderer))
                {
                    ProFitUtility.SetAllRendererMaterials(renderer, cabinetMaterial);
                }
            }

            UpdateColumnMaterial(m_FrontLeftColumn, cabinetMaterial);
            UpdateColumnMaterial(m_FrontRightColumn, cabinetMaterial);
            UpdateColumnMaterial(m_BackLeftColumn, cabinetMaterial);
            UpdateColumnMaterial(m_BackRightColumn, cabinetMaterial);

            UpdateColumnMaterial(m_CornerColumn, cabinetMaterial);

            foreach (Occupant product in m_Occupants.readOnly)
            {
                await product.SetCabinetMaterials(
                    cabinetMaterial,
                    countertopMaterial,
                    productCabinetMaterialIdentifier,
                    cutOutMaterialOverride
                );
            }

            static void UpdateColumnMaterial(TransformHelper column, Material cabinetMaterial)
            {
                if (column && column.TryGetComponent(out MeshRenderer columnRenderer))
                {
                    ProFitUtility.SetAllRendererMaterials(columnRenderer, cabinetMaterial);
                }
            }
        }

        public void SetCabinetSectionProductVisualPrefab(ProductVisual cabinetSectionVisual, ProductVisualContent cabinetSectionVisualPrefab)
        {
            m_CabinetSectionVisual = cabinetSectionVisual;
            m_CabinetSectionVisualPrefab = cabinetSectionVisualPrefab;
        }

        /// <summary>
        ///     Instantiates a custom cabinet section model for the cabinet.
        ///     This is intended for the Universal Kitchen Designer where the
        ///     cabinets do not yet have a cabinet model. E.g., Catalog building
        ///     method's "CabinetCatalogSection" prefab.
        /// </summary>
        /// <param name="brandCatalog">
        ///     The brand catalog that will be used to populate the cabinet's
        ///     feet. This is expected to be <see langword="null"/> except when
        ///     the selected building method is for <see
        ///     cref="KitchenBuildingMethod.Catalog"/>.
        /// </param>
        public void InstantiateCabinetModel(
            BrandCatalog brandCatalog,
            ProductVisualInfo cabinetDoorVisualInfo,
            ProductVisualContent cabinetDoorVisualPrefab,
            float sectionLengthInMeters)
        {
            if (m_CabinetSectionInstance != null)
            {
                throw new System.InvalidOperationException("Can only create a cabinet section model for a cabinet that doesn't already have one.");
            }

            if (!m_CabinetSectionVisualPrefab)
            {
                return;
            }

            Vector3 occupantOrientationLocalPosition =
                m_CabinetSectionVisualPrefab.sectionOccupantOrientation.localPosition;

            // Handle 45-degree corners. A normal, 0-degree occupant
            // orientation doesn't need any work (it is assumed to already be
            // located in the left-bottom-front corner of the cabinet).
            // However, 45-degree corner cabinets need their custom orientation
            // position calculated because there is no vertex for it to be
            // snapped to in the editor (plus, it doesn't overlap with the
            // scale origin anymore and thus cannot simply be "zeroed").
            if (HasCustomOccupantOrientation())
            {
                Vector3 customFrontFacePlaneNormal =
                    m_Transform.InverseTransformDirection(
                        m_CabinetSectionVisualPrefab.sectionOccupantOrientation.forward
                    );

                Plane customFrontFacePlane =
                    new Plane(
                        inNormal: customFrontFacePlaneNormal,
                        inPoint: occupantOrientationLocalPosition
                    );

                // This is a point that is centered on the 45-degree face of
                // the cabinet.
                Vector3 closestPointOnPlane =
                    customFrontFacePlane.ClosestPointOnPlane(Vector3.zero);

                // From the center of the 45-degree face, shift leftwards by
                // half the cabinet's size. This will give us the anchor that
                // properly centers the product on the 45-degree face.
                Vector3 localRight =
                    m_Transform.InverseTransformDirection(
                        m_CabinetSectionVisualPrefab.sectionOccupantOrientation.right
                    );

                occupantOrientationLocalPosition =
                    closestPointOnPlane - localRight * (cabinetSectionProductInfo.productSizeInMeters.x * 0.5f);
            }

            // Set the rotation and position of the helper transform that is
            // used to align this cabinet's occupants to a custom face, as is
            // necessary for 45-degree corner cabinets.
            m_OccupantOrientation.SetLocalPositionAndRotation(
                occupantOrientationLocalPosition,
                m_CabinetSectionVisualPrefab.sectionOccupantOrientation.localRotation
            );

            if (!m_CabinetSectionVisualPrefab.sectionModelPrefab)
            {
                // The product doesn't have a cabinet model. This is the case
                // when the cabinet uses a procedural mesh. E.g., Filler
                // cabinets in Catalog building method.
                return;
            }

            m_CabinetSectionInstance =
                Instantiate(
                    m_CabinetSectionVisualPrefab.sectionModelPrefab,
                    parent: m_Cabinet.transform
                );

            int activeCountertopVariationIndex =
                m_CabinetSectionVisual.info.sectionCountertopIndexInCabinetPrefab;

            for (int i = 0; i < m_CountertopVariations.Count; ++i)
            {
                bool isCountertopActive =
                    i == activeCountertopVariationIndex;

                if (!isCountertopActive)
                {
                    Destroy(m_CountertopVariations[i]);
                }
            }

            // HACK: Fixing a strange Danver model:
            // ProductVisual_CabinetSection_Storage_Danver_18InchStorageBaseCabinetsFullHeightDoorsLh
            m_CabinetSectionInstance.transform.localEulerAngles = Vector3.zero;

            m_CabinetSectionInstance.name = m_CabinetSectionVisualPrefab.name;

            Vector3 newSizeInMeters = sizeInMeters;
            newSizeInMeters.x = sectionLengthInMeters;
            sizeInMeters = newSizeInMeters;

            // Calculate the size of the cabinet including the cabinet legs
            // but excluding the countertop overhang.
            Vector3 cabinetBoundingBoxSizeInMeters =
                m_CabinetSectionVisualPrefab.modelCutOutExtentMax.localPosition;

            if (brandCatalog)
            {
                // Account for the height of the cabinet's legs, otherwise the
                // cabinet's height will be overcompensated and its top will
                // be scaled enough to push it out through the countertop.
                cabinetBoundingBoxSizeInMeters.y +=
                    brandCatalog.cabinetSectionLegStyle.heightInMeters;
            }

            cabinetBoundingBoxSizeInMeters.y +=
                kitchen.countertopThicknessInMeters;

            // Scale the cabinet section instance such that it is a 1m unit
            // cube. This ensures the instance's parent scale properly sizes
            // this custom mesh to the requested size.
            m_CabinetSectionInstance.transform.localScale =
                MelcherCommon.ComponentWiseDivide(
                    Vector3.one,
                    cabinetBoundingBoxSizeInMeters
                );

            MelcherLog.Assert(m_CabinetMeshRenderers.Count <= 0);

            MelcherSearch.AllFromTransformDownward(
                m_CabinetMeshRenderers,
                searchFrom: m_Cabinet.transform
            );

            // Apply the right layer to ensure the custom model supports
            // the cut-outs render feature.
            ProFitUtility.SetGameObjectLayerRecursively(
                m_Cabinet.transform,
                Weaver.Layers.Cabinet
            );

            RemakeCabinetDoor(cabinetDoorVisualInfo, cabinetDoorVisualPrefab);
        }

        public void InstantiateCabinetLegs(BrandCatalog brandCatalog)
        {
            if (!brandCatalog)
            {
                return;
            }

            bool isEnd = cabinetType.IsEnd();

            if (!m_CanHaveLegs && !isEnd)
            {
                // WeatherStrong's ends have some extra thickness to
                // account for their finished end panels and therefore are
                // actually visible and need to have their cabinet lifted
                // by the cabinet leg height to match their neighbours.
                return;
            }

            if (m_CabinetSectionVisual &&
                m_CabinetSectionVisual.info.cabinetSectionFootingType == CabinetSectionFootingType.OnGround)
            {
                return;
            }

            if (isEndPanelOnGround)
            {
                return;
            }

            CabinetSectionLegStyle legStyle =
                brandCatalog.cabinetSectionLegStyle;

            float cabinetLegHeight = legStyle.heightInMeters;

            m_Cabinet.transform.position += Vector3.up * cabinetLegHeight;

            if (isEnd)
            {
                // Ends don't have legs (yet).
                return;
            }

            if (m_CabinetSectionVisual &&
                m_CabinetSectionVisual.info.cabinetSectionFootingType != CabinetSectionFootingType.OnLegs)
            {
                return;
            }

            Vector2 legOffsetInMeters = legStyle.horizontalInsetFromCabinetEdgesInMeters;

            if (m_CabinetSectionVisual)
            {
                // Allow cabinets to override the leg's horizontal position.

                Vector2 legOffsetInMetersOverride =
                    m_CabinetSectionVisual.info.cabinetLegHorizontalInsetFromCabinetEdgesOverrideInMeters;

                if (float.IsNormal(legOffsetInMetersOverride.x))
                {
                    legOffsetInMeters.x = legOffsetInMetersOverride.x;
                }

                if (float.IsNormal(legOffsetInMetersOverride.y))
                {
                    legOffsetInMeters.y = legOffsetInMetersOverride.y;
                }
            }

            // Front-left leg
            if (m_CabinetType != CabinetType.Corner)
            {
                InstantiateLeg(
                    m_CabinetLegs,
                    legStyle,
                    this,
                    normalizedCabinetAnchor: new Vector3(0.0f, 0.0f, 0.0f),
                    offsetFromCabinetAnchorInMeters:
                        new Vector3(
                            legOffsetInMeters.x,
                            cabinetLegHeight,
                            legOffsetInMeters.y
                        )
                );
            }

            // Front-right leg
            InstantiateLeg(
                m_CabinetLegs,
                legStyle,
                this,
                normalizedCabinetAnchor: new Vector3(1.0f, 0.0f, 0.0f),
                offsetFromCabinetAnchorInMeters:
                    new Vector3(
                        -legOffsetInMeters.x,
                        cabinetLegHeight,
                        legOffsetInMeters.y
                    )
            );

            // Back-left leg
            InstantiateLeg(
                m_CabinetLegs,
                legStyle,
                this,
                normalizedCabinetAnchor: new Vector3(0.0f, 0.0f, 1.0f),
                offsetFromCabinetAnchorInMeters:
                    new Vector3(
                        legOffsetInMeters.x,
                        cabinetLegHeight,
                        -legOffsetInMeters.y
                    )
            );

            // Back-right leg
            InstantiateLeg(
                m_CabinetLegs,
                legStyle,
                this,
                normalizedCabinetAnchor: new Vector3(1.0f, 0.0f, 1.0f),
                offsetFromCabinetAnchorInMeters:
                    new Vector3(
                        -legOffsetInMeters.x,
                        cabinetLegHeight,
                        -legOffsetInMeters.y
                    )
            );

            /// <summary>
            ///     A local function used to instantiate and position a
            ///     cabinet leg.
            /// </summary>
            static void InstantiateLeg(
                List<CabinetSectionLeg> cabinetLegs,
                CabinetSectionLegStyle  legStyle,
                ICabinet                cabinet,
                Vector3                 normalizedCabinetAnchor,
                Vector3                 offsetFromCabinetAnchorInMeters)
            {
                CabinetSectionLeg newLeg =
                    CabinetSectionLeg.InstantiateLeg(
                        legStyle,
                        cabinet,
                        normalizedCabinetAnchor,
                        offsetFromCabinetAnchorInMeters
                    );

                if (newLeg == null)
                {
                    MelcherLog.LogError("Could not instantiate cabinet leg.", legStyle);
                    return;
                }

                cabinetLegs.Add(newLeg);

                newLeg.RefreshPosition();
            }
        }

        public void InstantiateCabinetVents(MeshRenderer ventPrefab, float ventDistanceFromEdgeInMeters)
        {
            for (int i = 0; i < 4; ++i)
            {
                MeshRenderer newVent = Instantiate(ventPrefab, parent: m_Transform);
                m_CabinetVents.Add(newVent.transform);
            }
        }

        /// <summary>
        ///     Destroy the current cabinet door (if it exists) and instantiate
        ///     a new new one from <paramref name="cabinetDoorVisualPrefab"/>.
        ///     This function does not update the materials of the created
        ///     cabinet door.
        /// </summary>
        public void RemakeCabinetDoor(ProductVisualInfo cabinetDoorVisualInfo, ProductVisualContent cabinetDoorVisualPrefab)
        {
            if (m_CabinetDoorInstance)
            {
                Destroy(m_CabinetDoorInstance.gameObject);
                m_CabinetDoorInstance = null;
            }

            if (cabinetDoorVisualPrefab)
            {
                m_CabinetDoorInstance =
                    Instantiate(
                        cabinetDoorVisualPrefab,
                        parent: m_Cabinet.transform
                    );

                // Undo the scaling of the parent so that the door renders using
                // its true size. This is nowhere near ideal, but it quickly
                // addresses the vertical positioning of the door because this
                // reuses m_Cabinet's position which already includes any
                // cabinet leg offset.

                // Force the cabinet door to preserve its 1,1,1 scale relative
                // to the world.
                Transform cabinetDoorInstanceTransform =
                    m_CabinetDoorInstance.transform;

                Vector3 localScale =
                    m_CabinetSectionInstance.transform.localScale;

                // 45 degree corner cabinet doors are scaled incorrectly
                // without the following line. Without this line, the doors
                // scale longer on their Z-axis and no longer line up with
                // their door frame. The following line ensures they preserve
                // their intended z-ratio.
                if (HasCustomOccupantOrientation())
                {
                    localScale.z = 1.0f / cabinetDoorInstanceTransform.localScale.z;
                }

                cabinetDoorInstanceTransform.localScale = localScale;

                // Disable the colliders on embedded storage because otherwise they
                // will block clicking the cabinet.
                m_CabinetDoorInstance.clickCollider.enabled = false;

                // Runtime prefabs are disabled by default and so must be
                // explicitly enabled after they are cloned.
                m_CabinetDoorInstance.gameObject.SetActive(true);
            }
        }

        /// <summary>
        ///     This is used to create the unremovable burner plunge that is
        ///     needed by Challenger's power burner base cabinets. If a cabinet
        ///     doesn't have the required flag (`sectionAlwaysHasPlunge`), this
        ///     function does nothing. Otherwise, a plunge is created and
        ///     centered within the cabinet.
        /// </summary>
        public async UniTask InstantiateCabinetPlunge()
        {
            if (!m_CabinetSectionVisual ||
                !m_CabinetSectionVisual.info.sectionAlwaysHasPlunge)
            {
                return;
            }

            string plungeProductSku = "Shelf:Top";

            // Try to add a plunge (shelf)
            ProductInfo plungeProductInfo =
                ProFitUtility.FindProductInfoBySku(plungeProductSku, findCabinetSection: false);

            if (plungeProductInfo == null)
            {
                MelcherLog.LogError($"Could not find product info for plunge. Product SKU: {plungeProductSku}");
                return;
            }

            ProductVisual plungeProductVisual =
                kitchen.productVisualDatabase.FindRuntimeVisualByID(
                    plungeProductInfo.visualId
                );

            if (!plungeProductVisual)
            {
                MelcherLog.LogError($"Could not find product visual for plunge. Product SKU: {plungeProductSku}, Visual ID: {plungeProductInfo.visualId}");
                return;
            }

            ProductVisualContent plungeVisualPrefab =
                await ProductVisualContentLoader.FirstFromActiveScene()
                    .ProductVisualToContent(plungeProductVisual);

            Cuboid? backingOccupancyInWorldSpace = null;

            CabinetSpan span = kitchen.layout.FindCabinetSpan(this);

            if (span.backing)
            {
                backingOccupancyInWorldSpace =
                    span.GetBackingOccupancyCuboidInWorldSpace(
                        span.backing.info,
                        span.backing
                    );
            }

            (Occupant plunge, VolumeOccupantResult result) =
                await TryInstantiateOccupantCentered(
                    plungeProductInfo,
                    plungeProductVisual,
                    plungeVisualPrefab,
                    kitchen.InstantiateOccupant,
                    backingOccupancyInWorldSpace
                );

            if (!plunge)
            {
                MelcherLog.LogError($"Could not instantiate plunge.");
                return;
            }

            plunge.isLockedToCabinet = true;

            // Resize the plunge to fill the width of the cabinet. These type
            // of plunges may need to be non-resizable and may need to extend
            // all the way to the cabinets' ends. This is to be determined.

            Vector3 cutOutSizeInMeters = plunge.currentCutOutSizeInMeters;

            // TODO: This may need to be a custom horizontal padding depending on the catalog.
            cutOutSizeInMeters.x = occupiableCuboid.width - Const.OccupantMinimumHorizontalPaddingInMeters;

            plunge.SetCutOutSizeInMeters(cutOutSizeInMeters);

            // Center the resized plunge within its cabinet.

            Vector3 cabinetSpacePositionOfOccupant =
                plunge.GetLocalPositionInVolume();

            cabinetSpacePositionOfOccupant.x =
                (resizerCuboid.width * 0.5f) - (cutOutSizeInMeters.x * 0.5f);

            plunge.SetLocalPositionInCabinet(
                cabinetSpacePositionOfOccupant,
                GetLocalOrientationForOccupants()
            );
        }

        public void SetInfoLabelVisibility(bool visible, ScreenshotAngle screenshotAngle)
        {
            bool frontIsVisible = visible && screenshotAngle == ScreenshotAngle.Front;
            bool topIsVisible = visible && screenshotAngle == ScreenshotAngle.Top;

            m_InfoLabelFront.gameObject.SetActive(frontIsVisible);
            m_InfoLabelTop.gameObject.SetActive(topIsVisible);

            m_InfoLabelFront.transform.localPosition =
                Cuboid.NormalizedToPoint(resizerCuboid, new Vector3(0.5f, 0.0f, 0.0f));
            m_InfoLabelFront.transform.localPosition -= Vector3.forward * 0.1f;
            m_InfoLabelFront.transform.localPosition += Vector3.down * 0.15f;

            m_InfoLabelTop.transform.localPosition =
                Cuboid.NormalizedToPoint(resizerCuboid, new Vector3(0.5f, 1.0f, 1.0f));
            m_InfoLabelTop.transform.localPosition += Vector3.up * 0.5f;
            m_InfoLabelFront.transform.localPosition += Vector3.forward * 0.2f;
        }

        private void Awake()
        {
            if (m_Countertop)
            {
                m_CountertopSliceAffectedByBackingCount =
                    new int[m_Countertop.slices.Count];
            }
            else
            {
                m_CountertopSliceAffectedByBackingCount = System.Array.Empty<int>();
            }

            SetSizeFromInches(m_InitialSizeInches);
        }

        private IEnumerable<MeshRenderer> GetMeshRenderers()
        {
            if (m_CabinetMeshRenderers != null)
            {
                foreach (MeshRenderer renderer in m_CabinetMeshRenderers)
                {
                    if (renderer)
                    {
                        yield return renderer;
                    }
                }
            }

            if (m_CountertopMeshRenderers != null)
            {
                foreach (MeshRenderer renderer in m_CountertopMeshRenderers)
                {
                    if (renderer)
                    {
                        yield return renderer;
                    }
                }
            }

            MeshRenderer toeKickRenderer;

            if (m_ToeKickLocalX && m_ToeKickLocalX.TryGetComponent(out toeKickRenderer))
            {
                yield return toeKickRenderer;
            }

            if (m_ToeKickLocalZ && m_ToeKickLocalZ.TryGetComponent(out toeKickRenderer))
            {
                yield return toeKickRenderer;
            }

            //if (m_FrontLeftSupport && m_FrontLeftSupport.TryGetComponent(out MeshRenderer leftSupportRenderer))
            //{
            //    yield return leftSupportRenderer;
            //}

            //if (m_FrontRightSupport && m_FrontRightSupport.TryGetComponent(out MeshRenderer rightSupportRenderer))
            //{
            //    yield return rightSupportRenderer;
            //}
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Tooltip("Whether this is a corner, straight, end, etc.")]
        [SerializeField]
        protected MelcherStableEnum<CabinetType> m_CabinetType = new (CabinetType.Unknown);

        [SerializeField]
        protected MelcherStableEnum<CabinetModelType> m_CabinetModelType = default;

        [SerializeField]
        protected MelcherStableEnum<KitchenBuildingMethod> m_AllowedFitToProductWidthBuildingMethods = default;

        [Tooltip("The kitchen building methods where this cabinet will force " +
            "its occupants to be centered, regardless of the building " +
            "method's normal centering rules. This is used to force " +
            "Pro-Fit's 45-degree corner wedges to center their products.")]
        [SerializeField]
        protected MelcherStableEnum<KitchenBuildingMethod> m_ForceProductsToCenterBuildingMethods = default;

        [Space]

        [Tooltip("This is the transform that will be scaled to match the " +
            "cabinet's current size. This may be 27-slice scalable cube " +
            "model (Pro-Fit, Stoll) or it may be an empty game object where " +
            "a custom cabinet section model will be childed (Universal" +
            "Kitchen Designer).")]
        [SerializeField] protected GameObject m_Cabinet = null;
        [SerializeField] protected SlicedMesh m_Countertop = null;

        // This is fragile should this feature ever used with an profile that
        // supports swapping out corners (Stoll, Gemini). The reference to the
        // corner would be lost when the cabinet is destroyed and remade.
        // However, this implementation works well for Pro-Fit because it allows
        // us to assign the wedged corner in the layout's prefab.
        [Tooltip("If this cabinet is a corner wedge, this indicates the " +
            "corner cabinet being wedged.")]
        [SerializeField] protected Cabinet m_CornerBeingWedged = null;

        [Tooltip("The mesh that is used to render the toe kick beneath the " +
            "cabinet and is sufficient in most cases. If this is blank, " +
            "the cabinet cannot display toe kicks.")]
        [SerializeField] protected TransformHelper m_ToeKickLocalX = null;

        [Tooltip("The mesh that is used to render the L-bit of a toe kick " +
            "when the cabinet is a corner. Most cabinets will not need this.")]
        [SerializeField] protected TransformHelper m_ToeKickLocalZ = null;

        [Header("Column")]

        [SerializeField] protected TransformHelper m_FrontLeftColumn = default;
        [SerializeField] protected TransformHelper m_FrontRightColumn = default;
        [SerializeField] protected TransformHelper m_BackLeftColumn = default;
        [SerializeField] protected TransformHelper m_BackRightColumn = default;

        [Space]

        [Tooltip("The mesh used for a corner cabinet's back-right column. It " +
            "is treated differently from the other columns because it will " +
            "never be contiguous with another column and it must scale in " +
            "both horizontal directions (and height, like all columns).")]
        [SerializeField] protected TransformHelper m_CornerColumn = default;

        [Space]

        [Tooltip("The graphic to use in the bar top and backsplash menus. " +
            "This is assigned in the cabinet layout prefabs. E.g., L or U.")]
        [SerializeField] protected Sprite m_BackingSprite = null;
        [UnityEngine.Serialization.FormerlySerializedAs("m_IsResizable")]
        [SerializeField] protected bool m_IsUserResizable = false;

        [Tooltip("Indicates whether the cabinet mesh renderer will scale " +
            "to match the size of the cabinet. For cabinets that are " +
            "custom modeled and already have a size (like those in the " +
            "Catalog building method), this field is off to ensure the " +
            "model is not distorted. For all other cabinets, this value is " +
            "left on.")]
        [SerializeField] protected bool m_IsCabinetModelScalable = true;

        [SerializeField] protected bool m_CanHaveProducts = false;

        [Tooltip("Whether the cabinet can have cabinet legs. Catalog " +
            "building method only. E.g., space cabinets never have legs.")]
        [SerializeField] protected bool m_CanHaveLegs = true;

        [Tooltip("When true, this cabinet can block the player from " +
            "selecting a cabinet behind it when determining which cabinet " +
            "the player clicked. This is set to false for cabinets that " +
            "should not have a hitbox: ends and spacers.")]
        [SerializeField] protected bool m_CanOccludeSelectionClick = true;

        [Space]

        [SerializeField]
        protected TMP_Text m_InfoLabelFront = default;

        [SerializeField]
        protected TMP_Text m_InfoLabelTop = default;

        [Space]
        [SerializeField] protected Vector3 m_InitialSizeInches = new Vector3(26.0f, 26.0f, 26.0f);
        [SerializeField] protected Vector3 m_MinSizeInInches = new Vector3(1.0f, 26.0f, 24.0f);
        [SerializeField] protected Vector3 m_MaxSizeInInches = new Vector3(240.0f, 26.0f, 30.0f);

        [Space]
        // REVIEW: This should be m_NormalizedAnchorForNextSection. Then it
        //         doesn't need to be updated anytime the preceding cabinet's
        //         type changes. -Ryan
        [SerializeField] protected Vector3 m_NormalizedAnchorToPreviousSection = Vector3.zero;
        [SerializeField] protected Vector3 m_NormalizedAnchorForNextSection = Vector3.zero;

        /// <summary>
        ///     A list of renders that belong to the cabinet mesh (i.e., not
        ///     the countertop). This is now necessary to cache the multiple
        ///     cabinet renders that are now possible since adding the Catalog
        ///     building method.
        /// </summary>
        [SerializeField]
        protected List<MeshRenderer> m_CabinetMeshRenderers = new ();

        [SerializeField, MelcherReadOnlyInInspector]
        protected List<MeshRenderer> m_CountertopMeshRenderers = new ();

        [Tooltip("Typically this is just one object, the default countertop, " +
            "but some cabinets such as authored corner cabinets need a " +
            "unique countertop shape depending on the cabinet's shape.")]
        [SerializeField]
        protected List<GameObject> m_CountertopVariations = new ();

        [Tooltip("Some cabinets (like 45 degree corner cabs) need a custom " +
            "toe-kick mesh to be accurately visualized. When this is the " +
            "case, the cabinet's product visual will indicate an inset " +
            "toe-kick index greater than or equal to zero. That index is " +
            "used to index this list.")]
        [SerializeField]
        protected List<GameObject> m_InsetToeKickVariations = new ();

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected IKitchen m_Kitchen;

        protected Vector3 m_SizeInMeters;

        /// <summary>
        ///     The amount of end-panel padding (in meters) that <see
        ///     cref="MidOfRunCabinetResize"/> has already applied to the Min
        ///     and Max extents of this cabinet. Tracked so that repeated calls
        ///     apply the padding relative to the cabinet's un-padded size
        ///     instead of stacking on top of the previous call's result.
        /// </summary>
        protected float m_AppliedMidOfRunPaddingInMetersLeft;
        protected float m_AppliedMidOfRunPaddingInMetersRight;

        /// <summary>
        ///     A 0-based number that indicates which "span" a cabinet belongs
        ///     to. A "span" is the group of cabinets that appear before a
        ///     corner section. This is intended to associate data with many
        ///     cabinets that are aligned side-by-side when <see
        ///     cref="KitchenBuildingMethod.Sections"/> building method is
        ///     used.
        /// </summary>
        protected int m_SpanIndex;

        /// <summary>
        ///     This counts the number of backings that are affecting each
        ///     countertop slice in the cabinet. The idea is that multiple
        ///     backings may affect the same countertop slice and therefore
        ///     the cabinet must track this to ensure one backing doesn't
        ///     re-enable visibility of a slice that is still hidden by another
        ///     backing.
        /// </summary>
        protected int[] m_CountertopSliceAffectedByBackingCount;

        protected ProductInfo m_CabinetSectionProductInfo;

        /// <summary>
        ///     The product visual that was used to create this cabinet.
        /// </summary>
        protected ProductVisual m_CabinetSectionVisual;

        /// <summary>
        ///     The prefab that was used to instantiate <see
        ///     cref="m_CabinetSectionInstance"/>. This will be null if the
        ///     cabinet is procedural.
        /// </summary>
        protected ProductVisualContent m_CabinetSectionVisualPrefab;

        /// <summary>
        ///     The instantiated, authored, cabinet section model. If a
        ///     cabinet is procedural, this will be <see langword="null"/>.
        /// </summary>
        protected GameObject m_CabinetSectionInstance;

        /// <summary>
        ///     Used in <see cref="KitchenBuildingMethod.Catalog"/>. A cabinet
        ///     section must have a cabinet door.
        /// </summary>
        protected ProductVisualContent m_CabinetDoorInstance;

        protected CabinetToeKickType m_ToeKickType;

        protected List<CabinetSectionLeg> m_CabinetLegs = new ();

        protected List<Transform> m_CabinetVents = new();

        /// <summary>
        ///     This list holds a copy of the occupants for the cabinet that is
        ///     currently being resized (<see cref="TryToResizeLength(ref float, Extent, bool?)"/>).
        ///     This is necessary because the order of the occupants can change
        ///     as the list is iterated when the products are repositioned.
        /// </summary>
        protected static List<Occupant> s_OccupantsToCenter = new ();

        // ====================================================================
        // Types
        // ====================================================================

        [System.Flags]
        public enum ResizeAxis
        {
            X   = 1 << 0,
            Z   = 1 << 2,
        }

#if UNITY_EDITOR
        // =====================================================================
        // Editor
        // =====================================================================

        protected override void OnValidate()
        {
            base.OnValidate();

            ResizeMeshToCabinetSize();

            s_MeshRenderersFoundOnValidate.Clear();

            if (m_Cabinet)
            {
                MelcherSearch.AllFromTransformDownward(
                    results: s_MeshRenderersFoundOnValidate,
                    searchFrom: m_Cabinet.transform
                );
            }

            if (!m_CabinetMeshRenderers.SequenceEqual(s_MeshRenderersFoundOnValidate))
            {
                m_CabinetMeshRenderers.Clear();
                m_CabinetMeshRenderers.AddRange(s_MeshRenderersFoundOnValidate);
                UnityEditor.EditorUtility.SetDirty(this);
            }

            s_MeshRenderersFoundOnValidate.Clear();

            foreach (GameObject countertopVariation in m_CountertopVariations)
            {
                if (!countertopVariation)
                {
                    continue;
                }

                MelcherSearch.AllFromTransformDownward(
                    results: s_MeshRenderersFoundOnValidate,
                    searchFrom: countertopVariation.transform,
                    includeInactiveGameObjects: true,
                    includeInactiveComponents: true
                );
            }

            if (!m_CountertopMeshRenderers.SequenceEqual(s_MeshRenderersFoundOnValidate))
            {
                m_CountertopMeshRenderers.Clear();
                m_CountertopMeshRenderers.AddRange(s_MeshRenderersFoundOnValidate);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        static protected List<MeshRenderer> s_MeshRenderersFoundOnValidate = new ();

#endif  // UNITY_EDITOR
    }   // class Cabinet

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
