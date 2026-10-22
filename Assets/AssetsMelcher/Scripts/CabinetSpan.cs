namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using Cysharp.Threading.Tasks;
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    ///     A <see cref="CabinetSpan"/> is a way of referring to a group of
    ///     cabinets. Some <see cref="Occupant"/>s, like <see
    ///     cref="CabinetBacking"/>s, need to span the length of multiple
    ///     cabinets.
    /// </summary>
    public class CabinetSpan
    {
        // =====================================================================
        // Constructors
        // =====================================================================

        public CabinetSpan(IKitchen kitchen, int spanIndex)
        {
            this.kitchen = kitchen;
            index = spanIndex;
        }

        // =====================================================================
        // Properties
        // =====================================================================

        public IKitchen kitchen { get; protected set; }

        /// <summary>
        ///     The index of the cabinet span in <see
        ///     cref="CabinetLayout.spans"/>.
        /// </summary>
        public int index { get; protected set; }

        public ReadOnlyCollection<ElementIndex<Cabinet>> cabinets =>
            m_Cabinets.readOnly;

        public CabinetBacking backing => m_CabinetBacking;

        public Cabinet orientationAlignedCabinet => m_OrientationAlignedCabinet;

        public float baseCabinetDepthInMeters => m_BaseCabinetDepthInMeters;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Return <see langword="true"/> if the cabinet index belongs to
        ///     this span.
        /// </summary>
        public bool Contains(int cabinetIndex)
        {
            return Contains(cabinetIndex, out int _);
        }

        /// <inheritdoc cref="Contains(int)"/>
        /// <param name="localIndexInSpan">
        ///     The index of the located cabinet or -1 if not found.
        /// </param>
        public bool Contains(int cabinetIndex, out int localIndexInSpan)
        {
            localIndexInSpan = -1;

            int index = 0;
            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                if (i.index == cabinetIndex)
                {
                    localIndexInSpan = index;
                    return true;
                }

                ++index;
            }

            return false;
        }

        /// <summary>
        ///     Return <see langword="true"/> if the cabinet belongs to this
        ///     span.
        /// </summary>
        public bool Contains(Cabinet cabinet)
        {
            return Contains(cabinet, out int _);
        }

        /// <inheritdoc cref="Contains(Cabinet)"/>
        /// <param name="localIndexInSpan">
        ///     The index of the located cabinet or -1 if not found.
        /// </param>
        public bool Contains(Cabinet cabinet, out int localIndexInSpan)
        {
            localIndexInSpan = -1;

            int index = 0;
            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                if (i.element == cabinet)
                {
                    localIndexInSpan = index;
                    return true;
                }

                ++index;
            }

            return false;
        }

        /// <summary>
        ///     Get a cuboid in the cabinet span's local space that represents
        ///     the space of all cabinets in the span, including corners and
        ///     ends.
        /// </summary>
        /// <param name="includeCountertop">
        ///     When this is <see langword="true"/>, the returned cuboid
        ///     includes the left, right, front and back countertop overhang.
        /// </param>
        public Cuboid GetOutsideCuboidInSpanSpace(bool includeCountertop = false)
        {
            // TODO: This function is where I realized we may not need the
            //       Cuboid type, but I haven't looked further. -Ryan

            Bounds outsideBoundsInSpanSpace = default;

            bool firstPass = true;

            System.Span<Vector3> cabinetCuboidCorners = stackalloc Vector3[8];

            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                Cabinet cabinet = i.element;

                Cuboid cabinetCuboid = cabinet.resizerCuboid;

                if (includeCountertop)
                {
                    CuboidPadding countertopOverhangInMeters =
                        cabinet.GetCountertopOverhangInMeters();

                    // Ensure top and bottom overhang is zero as it doesn't
                    // make any sense and could otherwise affect the accuracy
                    // of the calculated cabinet cuboid.
                    countertopOverhangInMeters.bottom = 0.0f;
                    countertopOverhangInMeters.top = 0.0f;

                    cabinetCuboid =
                        countertopOverhangInMeters.Inflate(cabinetCuboid);
                }

                // To account for a cabinet span potentially being at a
                // non-orthogonal angle, we need to avoid transforming the
                // bounds as a whole; this would result in distortion. To
                // solve this, we will get and transform the corner vertices of
                // each cabinet's cuboid individually from their local cabinet
                // space to the span's space and build the span's bounds from
                // these transformed points. This ensures the bounds encapsulate
                // each cabinet without distortion.

                // Get the cabinet cuboid's corners in the cabinet's local
                // space.
                MelcherUnityUtility.CalculateAABBCorners(
                    cabinetCuboid.min,
                    cabinetCuboid.max,
                    cabinetCuboidCorners
                );

                // Convert cabinet-space corners into span-space.
                for (int j = 0; j < cabinetCuboidCorners.Length; ++j)
                {
                    Vector3 worldSpacePoint =
                        cabinet.GetTransform().TransformPoint(cabinetCuboidCorners[j]);

                    Vector3 spanSpacePoint =
                        m_OrientationAlignedCabinet.GetTransform().InverseTransformPoint(worldSpacePoint);

                    cabinetCuboidCorners[j] = spanSpacePoint;
                }

                if (firstPass)
                {
                    // No bounds started yet so make one starting from a
                    // single point.

                    outsideBoundsInSpanSpace = new Bounds(cabinetCuboidCorners[0], size: Vector3.zero);
                    foreach (Vector3 corner in cabinetCuboidCorners.Slice(1))
                    {
                        outsideBoundsInSpanSpace.Encapsulate(corner);
                    }
                }
                else
                {
                    // Expand the existing bounds to include each of the new
                    // points.

                    foreach (Vector3 corner in cabinetCuboidCorners)
                    {
                        outsideBoundsInSpanSpace.Encapsulate(corner);
                    }
                }

                firstPass = false;
            }

            return new Cuboid(outsideBoundsInSpanSpace.min, outsideBoundsInSpanSpace.size);
        }

        /// <summary>
        ///     Get a cuboid in cabinet span space that represents the footprint
        ///     area that <paramref name="backing"/>'s riser will match. This
        ///     footprint has zero height.
        /// </summary>
        /// <param name="backingInfo">
        ///     The product info of <paramref name="backing"/>. This contains
        ///     some of the measurements for the backing's model.
        /// </param>
        /// <param name="backing">
        ///     The backing (prefab or instance) whose potential/current
        ///     footprint will be calculated.
        /// </param>
        public Cuboid GetBackingRiserFootprintCuboidInSpanSpace(
            ProductInfo backingInfo,
            CabinetBacking backing)
        {
            Cuboid backingCutOutInSpanSpace =
                GetOutsideCuboidInSpanSpace();

            if (!m_CabinetBacking)
            {
                // Only apply inflation once.

                backingCutOutInSpanSpace.zMax +=
                    backing.GetCabinetInflateDepthInMeters(kitchen.selectedBuildingMethod);
            }

            Cuboid backingCuboidInSpanSpace = backingCutOutInSpanSpace;

            // A footprint has zero height. This ensures the returned cuboid's
            // y-min and y-max are flush with the top of the countertop, which
            // is useful to the callers of this function.
            backingCuboidInSpanSpace.yMin = backingCuboidInSpanSpace.yMax;

            float riserDepthInMeters = backingInfo.productSizeInMeters.z;

            float? highBarTopRiserDepthInMeters = ProFitGlobals.appRules.HighBarTopRiserDepthInMeters;

            if (highBarTopRiserDepthInMeters.HasValue &&
                backingInfo.category == ProductCategory.BarTopHigh)
            {
                riserDepthInMeters = highBarTopRiserDepthInMeters.Value;
            }

            backingCuboidInSpanSpace.zMin =
                backingCuboidInSpanSpace.zMax - riserDepthInMeters;

            return backingCuboidInSpanSpace;
        }

        /// <summary>
        ///     Get a cuboid in world-space that represents the area the backing
        ///     occupies within all cabinets in the span.
        /// </summary>
        public Cuboid GetBackingOccupancyCuboidInWorldSpace(
            ProductInfo backingInfo,
            CabinetBacking backing)
        {
            Cuboid backingCuboidInSpanSpace =
                GetBackingRiserFootprintCuboidInSpanSpace(
                    backingInfo,
                    backing
                );

            // Force a minimum 1" height. Otherwise, the height is 0" and
            // any overlap checks with occupants will fail because we're at
            // the cabinet's Y-value extreme (top).
            backingCuboidInSpanSpace.yMin -= 1.0f * Units.Inches2Meters;

            return
                backingCuboidInSpanSpace.Transform(
                    m_OrientationAlignedCabinet.GetTransform()
                );
        }

        public void Add(Cabinet cabinet, int cabinetIndex)
        {
            m_Cabinets.mutable.Add(ElementIndex.From(cabinet, cabinetIndex));

            // TODO: This check probably won't handle Galley style kitchens.
            //       Their bends do not use corner sections.
            //       -- OR maybe they will. We only send in a corner section
            //       here from CabinetLayout if there is a corner section.
            if (!m_OrientationAlignedCabinet)
            {
                m_OrientationAlignedCabinet =
                    m_Cabinets.readOnly.FirstOrDefault(
                        static x =>
                            x.element.cabinetType != CabinetType.Corner
                    ).element;
            }
        }

        /// <summary>
        ///     Reset the cabinet indices for this span to <see
        ///     langword="null"/>. This is intended to happen whenever the
        ///     cabinet layout is modified (new cabinet added, moved, removed,
        ///     etc.): the spans are cleared and then rebuilt by the cabinet
        ///     layout.
        /// </summary>
        public void Clear()
        {
            m_Cabinets.mutable.Clear();
            m_OrientationAlignedCabinet = null;
        }

        /// <summary>
        ///     Try to create a cabinet backing that occupies the length of
        ///     the cabinet span.
        /// </summary>
        public async UniTask<VolumeOccupantResult> TryInstantiateBacking(
            ProductInfo                 backingInfo,
            CabinetBacking              backingPrefab)
        {
            VolumeOccupantResult result = default;

            MelcherLog.Log("TryInstantiateBacking");

            if (backingInfo == null)
            {
                MelcherLog.LogError(
                    $"The {MelcherFormat.ConsoleTypeName<CabinetBacking>()} " +
                    $"does not specify a type or doesn't have a " +
                    $"corresponding {MelcherFormat.ConsoleTypeName<ProductInfo>()}.",
                    backingPrefab
                );

                result.status = OccupantCuboidStatus.DataError;
                return result;
            }

            if (m_CabinetBacking)
            {
                // There can only be one backing per span.

                result.status = OccupantCuboidStatus.OverlapsBacking;
                return result;
            }

            // Ensure that all cabinets are of standard height. Currently,
            // cabinet backings are mutually exclusive with non-standard
            // cabinet heights.

            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                if (!i.element.isStandardHeight)
                {
                    result.status = OccupantCuboidStatus.NotCompatible;
                    return result;
                }
            }

            // Check that all cabinets in the span can be safely inflated by
            // the backing without issues to the existing occupants.

            Vector3 inflateCabinetSizeByMetersInSpanSpace =
                GetCabinetInflateSizeInMetersFromBacking(backingPrefab);

            if (inflateCabinetSizeByMetersInSpanSpace.sqrMagnitude > 0.0f)
            {
                foreach (ElementIndex<Cabinet> i in cabinets)
                {
                    Cabinet cabinet = i.element;

                    if (!cabinet.canHaveProducts)
                    {
                        continue;
                    }

                    Vector3 cabinetSizeChange =
                        OccupiableVolume.TransformSizeBetweenVolumes(
                            m_OrientationAlignedCabinet,
                            inflateCabinetSizeByMetersInSpanSpace,
                            cabinet
                        );

                    CuboidPadding cabinetPerSideSizeChange = new CuboidPadding();
                    cabinetPerSideSizeChange.right = cabinetSizeChange.x;
                    cabinetPerSideSizeChange.top = cabinetSizeChange.y;
                    cabinetPerSideSizeChange.towardMaxZ = cabinetSizeChange.z;

                    if (!cabinet.CanVolumeResizeFitOccupants(cabinetPerSideSizeChange))
                    {
                        result.status = OccupantCuboidStatus.CannotResizeCabinet;
                        return result;
                    }
                }
            }

            // Find if the backing would overlap any occupants in the span.

            Cuboid backingCuboidInWorldSpace =
                GetBackingOccupancyCuboidInWorldSpace(backingInfo, backingPrefab);

            // Force minimum 1" height. If the height is 0", the bounds check
            // will because we're at the cabinet's Y-value extreme (top).
            backingCuboidInWorldSpace.yMin -= 1.0f * Units.Inches2Meters;

            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                Cabinet cabinet = i.element;

                if (!cabinet.canHaveProducts)
                {
                    continue;
                }

                Cuboid backingCuboidInCabinetSpace =
                    Cuboid.InverseTransform(
                        cabinet.GetTransform(),
                        backingCuboidInWorldSpace
                    );

                foreach (Occupant occupant in cabinet.occupants)
                {
                    ProductAnchor occupantAnchor = occupant.productVisual.info.anchor;

                    if(occupantAnchor == ProductAnchor.OnFrontAtBottom)
                    {
                        continue;
                    }
                    
                    Cuboid occupantCuboid =
                        cabinet.CalculateOccupancyCuboidForOccupant(occupant);

                    if (Cuboid.Overlaps(occupantCuboid, backingCuboidInCabinetSpace))
                    {
                        result.blockingOccupant = occupant;
                        result.status = OccupantCuboidStatus.OverlapsAnotherOccupant;
                        MelcherLog.LogVerbose("Backing would overlap");
                        return result;
                    }
                }
            }

            // At this point, we know the backing fits across all cabinets in
            // the span. It will now be created and fit to the cabinet.

            m_CabinetBacking =
                GameObject.Instantiate(
                    backingPrefab,
                    m_OrientationAlignedCabinet.GetTransform()
                );

            if (!m_CabinetBacking)
            {
                MelcherLog.LogError($"Could not instantiate backing: {backingPrefab}", backingPrefab);

                result.status = OccupantCuboidStatus.InstantiateError;

                return result;
            }

            m_CabinetBacking.SetProductInfo(backingInfo);

            // Match the kitchen's current finishes.
            (Material cabinetMaterial, Material countertopMaterial) =
                await kitchen.GetFinishMaterials(allowHighlightToReturn: true);

            m_CabinetBacking.OnCabinetMaterialsChanged(
                cabinetMaterial,
                countertopMaterial
            );

            // Apply the cabinet size change.
            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                Cabinet cabinet = i.element;

                await ApplyCabinetBackingToCabinet(cabinet, m_CabinetBacking, add: true);
            }

            return result;
        }

        public Vector3 GetCabinetInflateSizeInMetersFromBacking(CabinetBacking backing)
        {
            float inflateCabinetDepthByMeters =
                backing.GetCabinetInflateDepthInMeters(kitchen.selectedBuildingMethod);

            Vector3 inflateCabinetSizeByMetersInSpanSpace =
                new Vector3(0.0f, 0.0f, inflateCabinetDepthByMeters);

            return inflateCabinetSizeByMetersInSpanSpace;
        }

        public async UniTask ApplyCabinetBackingToCabinet(Cabinet cabinet, CabinetBacking backing, bool add)
        {
            MelcherLog.Breadcrumb($"add:{add}, cab:{cabinet.name}, backing:{backing}", cabinet);

            if (!backing)
            {
                return;
            }

            if (backing.cropCabinetCountertops)
            {
                CabinetLocalDirection overhangDirection =
                    TransformDirectionFromSpanToCabinet(CabinetLocalDirection.Back, cabinet);

                cabinet.SetCountertopOverhangVisible(overhangDirection, isVisible: !add);
            }

            {
                // We need to adjust the size of all cabinets in the span
                // (including ends and corners). The leading corner in the
                // span (if it exists) is using a different rotation than
                // the rest of the cabinets. The inflation size delta must
                // be transformed to ensure its direction is relative to the
                // same world direction for each cabinet.

                Vector3 localSizeChangeInMeters =
                    OccupiableVolume.TransformSizeBetweenVolumes(
                        m_OrientationAlignedCabinet,
                        GetCabinetInflateSizeInMetersFromBacking(backing),
                        cabinet
                    );

                if (!add)
                {
                    localSizeChangeInMeters = -localSizeChangeInMeters;
                }

                cabinet.sizeInMeters += localSizeChangeInMeters;
            }

            ApplyCabinetBackingToCountertop(cabinet, backing, add);

            await EventBridge.Designer.onCabinetResized.InvokeIfNonEmpty(cabinet);
        }

        public CabinetLocalDirection TransformDirectionFromSpanToCabinet(CabinetLocalDirection spanRelativeDirection, Cabinet cabinet)
        {
            Vector3 spanRelativeDirectionAsVector =
                spanRelativeDirection.ToVector3();

            Vector3 spanRelativeDirectionAsVectorInWorldSpace =
                orientationAlignedCabinet
                    .GetTransform()
                    .TransformDirection(spanRelativeDirectionAsVector);

            Vector3 spanRelativeDirectionAsVectorInCabinetSpace =
                cabinet
                    .GetTransform()
                    .InverseTransformDirection(spanRelativeDirectionAsVectorInWorldSpace);

            return CabinetLocalDirectionUtil.FromVector3(spanRelativeDirectionAsVectorInCabinetSpace);
        }

        /// <summary>
        ///     Return a <see cref="CuboidPadding"/> that represents the amount
        ///     the cabinet's direct countertop overhang should be changed by
        ///     the current backing.
        ///     <br /><br />
        ///
        ///     I.e., not the size of the countertop nested /within/ the backing
        ///     but the size delta of the cabinet's countertop itself. Some
        ///     backings don't have a nested countertop (e.g., low bar top) and
        ///     so they modify their cabinet's countertop directly.
        ///     <br /><br />
        ///
        ///     The returned padding is transformed such that the values are
        ///     correctly rotated for any cabinet in the span, even if their
        ///     local rotation doesn't match the cabinet span's "orientation
        ///     aligned cabinet."
        /// </summary>
        public CuboidPadding GetBackingDirectCountertopOverhangDeltaInMetersForCabinet(CabinetBacking backing, Cabinet padRelativeTo)
        {
            if (!backing || backing.hasRiserAndRiserCountertop)
            {
                return CuboidPadding.zero;
            }

            CuboidPadding backingCountertopOverhangInMetersDelta =
                backing.countertopOverhangInMeters;

            if (padRelativeTo != cabinets[0].element)
            {
                // This cabinet is somewhere further right from the
                // span's leftmost cabinet. We won't modify the left
                // overhang to ensure it doesn't clip into the left
                // neighbour.
                backingCountertopOverhangInMetersDelta.left = 0.0f;
            }

            if (padRelativeTo != cabinets[^1].element)
            {
                // This cabinet is somewhere further left from the
                // span's rightmost cabinet. We won't modify the right
                // overhang to ensure it doesn't clip into the right
                // neighbour.
                backingCountertopOverhangInMetersDelta.right = 0.0f;
            }

            return
                OccupiableVolume.TransformCuboidPaddingBetweenVolumes(
                    m_OrientationAlignedCabinet,
                    backingCountertopOverhangInMetersDelta,
                    padRelativeTo
                );
        }

        public async UniTask<bool> TryDestroyBacking()
        {
            MelcherLog.Log("TryDestroyBacking");

            if (!m_CabinetBacking)
            {
                return true;
            }

            // Get the change in size all cabinets will adjust by when this
            // backing is removed.

            float inflateCabinetDepthByMeters =
                m_CabinetBacking.GetCabinetInflateDepthInMeters(kitchen.selectedBuildingMethod);

            Vector3 inflateCabinetSizeByMetersInSpanSpace =
                new Vector3(0.0f, 0.0f, inflateCabinetDepthByMeters);

            if (inflateCabinetDepthByMeters != 0.0f)
            {
                // Check that all cabinets in the span can be safely deflated by
                // the backing without issues to the existing occupants.

                foreach (ElementIndex<Cabinet> i in cabinets)
                {
                    Cabinet cabinet = i.element;

                    if (!cabinet.canHaveProducts)
                    {
                        continue;
                    }

                    Vector3 cabinetSizeChange =
                        OccupiableVolume.TransformSizeBetweenVolumes(
                            m_OrientationAlignedCabinet,
                            inflateCabinetSizeByMetersInSpanSpace,
                            cabinet
                        );

                    CuboidPadding cabinetPerSideSizeChange = new CuboidPadding();
                    cabinetPerSideSizeChange.right = cabinetSizeChange.x;
                    cabinetPerSideSizeChange.top = cabinetSizeChange.y;
                    cabinetPerSideSizeChange.towardMaxZ = cabinetSizeChange.z;

                    if (!cabinet.CanVolumeResizeFitOccupants(-cabinetPerSideSizeChange))
                    {
                        return false;
                    }
                }
            }

            // Mark this cabinet span as not having a backing before updating
            // the neighbour cabinet spans. This is to ensure the neighbours
            // properly observe that this backing was removed. Otherwise they
            // will expand to fit a backing that is being removed.

            CabinetBacking removedBacking = m_CabinetBacking;

            using var _ =
                MelcherDeferred.Defer(
                    toCapture: removedBacking,
                    onDispose: static removedBacking => GameObject.Destroy(removedBacking.gameObject)
                );

            m_CabinetBacking.GetTransform().SetParent(null);

            m_CabinetBacking = null;

            // Un-apply the cabinet size change.
            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                Cabinet cabinet = i.element;

                await ApplyCabinetBackingToCabinet(cabinet, removedBacking, add: false);
            }

            return true;
        }

        /// <summary>
        ///     This resizes the backing (riser & countertop) to fit both the
        ///     cabinets in its span and its adjacent backings. It is intended
        ///     to be called when the backing is added to a cabinet span or one
        ///     of its adjacent backings change.
        /// </summary>
        /// <remarks>
        ///     Resize the backing model(s) to fit the length of the cabinet.
        ///     The backing will also extend to the cabinet's neighbours if
        ///     they are corners. The backing model's countertop will also be
        ///     extended to match it's adjacent backings (if they exist) to
        ///     ensure the corners connect. This implementation has the
        ///     backings overlap but because the cabinets are textured with
        ///     world-space tri-planar shaders and the highlight material has
        ///     a Z-bias/offset, we don't have to worry about z-fighting.
        /// </remarks>
        // TODO: Z-fighting is now an issue. Some countertop finishes align
        //       their texture with the cabinet's rotation. Corner countertop
        //       connections will have flicker.
        public void UpdateBackingSizeAndPosition()
        {
            if (!m_CabinetBacking)
            {
                return;
            }

            // Start by re-parenting the backing to the primary cabinet in its
            // span. Otherwise, the player may have moved the cabinet which has
            // the backing as a child and the Transform relation between the
            // backing and its intended span will be wrong.
            SetParentOfBacking(m_OrientationAlignedCabinet.GetTransform());

            m_CabinetBacking.GetTransform()
                .SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Calculate the backing's countertop thickness. This may differ
            // from the kitchen's normal countertop thickness depending on
            // options selected by the user at the start of the designer scene.

            float countertopThicknessInMeters =
                kitchen.countertopThicknessInMeters;

            bool allowCountertop = true;

            if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections &&
                m_CabinetBacking.info.category == ProductCategory.Backsplash)
            {
                // Backsplashes in Stoll builds have no countertop.
                allowCountertop = false;
            }

            m_CabinetBacking.SetRiserCountertopVisible(allowCountertop);

            if (!allowCountertop)
            {
                // If the riser countertop isn't used, its countertop thickness
                // must be set to zero to ensure the riser itself occupies the
                // full backing height. Otherwise, the riser will be short
                // however thick the countertop was expected to be.
                countertopThicknessInMeters = 0.0f;
            }

            if (m_CabinetBacking.hasRiserAndRiserCountertop &&
                kitchen.selectedCountertopType == CountertopType.Stoll)
            {
                const float MaxStollBackingCountertopThicknessInMeters = 2.0f * Units.Inches2Meters;

                if (countertopThicknessInMeters > MaxStollBackingCountertopThicknessInMeters)
                {
                    countertopThicknessInMeters = MaxStollBackingCountertopThicknessInMeters;
                }
            }

            float? highBarTopCountertopThicknessInMeters = ProFitGlobals.appRules.HighBarTopCountertopThicknessInMeters;

            if (highBarTopCountertopThicknessInMeters.HasValue &&
                m_CabinetBacking.info.category == ProductCategory.BarTopHigh)
            {
                countertopThicknessInMeters = highBarTopCountertopThicknessInMeters.Value;
            }

            // Calculate the riser's size. It spans from the leftmost cabinet
            // in the span to the rightmost cabinet.

            Cuboid riserCuboidInSpanSpace =
                GetBackingRiserFootprintCuboidInSpanSpace(
                    m_CabinetBacking.info,
                    m_CabinetBacking
                );

            riserCuboidInSpanSpace.height =
                m_CabinetBacking.info.productSizeInMeters.y - countertopThicknessInMeters;

            // Calculate the riser's countertop size. It must extend to meet
            // the countertop of it's adjacent backings (if they exist).

            Cuboid countertopCuboidInSpanSpace = riserCuboidInSpanSpace;

            countertopCuboidInSpanSpace.height = countertopThicknessInMeters;

            countertopCuboidInSpanSpace.position +=
                Vector3.up * riserCuboidInSpanSpace.height;

            // Calculate the size of the backing's default countertop, with
            // respect to its own overhang.
            countertopCuboidInSpanSpace =
                m_CabinetBacking.countertopOverhangInMeters
                    .Inflate(countertopCuboidInSpanSpace);

            float? highBarTopCountertopDepthInMeters = ProFitGlobals.appRules.HighBarTopCountertopDepthInMeters;

            if (highBarTopCountertopDepthInMeters.HasValue &&
                backing.info.category == ProductCategory.BarTopHigh)
            {
                countertopCuboidInSpanSpace.depth = highBarTopCountertopDepthInMeters.Value;
            }

            // Factor in the overhang of the backing's neighbouring backings.
            // This will ensure bar tops, with their long overhangs, meet
            // properly in the corners.
            foreach (CabinetSpan span in kitchen.layout.spans)
            {
                if (Mathf.Abs(span.index - index) != 1)
                {
                    // Skip spans that are not neighbours to this span.
                    continue;
                }

                if (!span.backing)
                {
                    // This neighbour span doesn't have a backing to match, but
                    // it may have extra countertop overhang that needs to be
                    // accounted for in Sections building method.

                    if (!span.IsSpace())
                    {
                        CuboidPadding countertopOverhangInMeters =
                            kitchen.baseCountertopOverhangInMeters;

                        if (span.index < index)
                        {
                            // Left neighbour.
                            countertopCuboidInSpanSpace.xMin -=
                                countertopOverhangInMeters.towardMaxZ;
                        }
                        else
                        {
                            // Right neighbour.
                            countertopCuboidInSpanSpace.xMax +=
                                countertopOverhangInMeters.towardMaxZ;
                        }
                    }

                    continue;
                }

                if (m_CabinetBacking.hasRiserAndRiserCountertop != span.backing.hasRiserAndRiserCountertop)
                {
                    // These backings are at different heights (e.g., low bar
                    // top versus high bar top). We will not attempt to merge
                    // their countertops together.
                    continue;
                }

                // Get the neighbour's countertop cuboid of its backing in the
                // same coordinate space as the current backing. This cuboid
                // does not have the correct y-position but it is not needed
                // here.
                Cuboid neighbourBackingCutOutCuboidInNeighbourSpanSpace =
                    span.GetBackingRiserFootprintCuboidInSpanSpace(
                        span.m_CabinetBacking.info,
                        span.m_CabinetBacking
                    );

                Cuboid neighbourBackingCountertopCuboidInNeighbourSpanSpace =
                    span.m_CabinetBacking.countertopOverhangInMeters
                        .Inflate(neighbourBackingCutOutCuboidInNeighbourSpanSpace);

                Cuboid neighbourBackingCountertopCuboidInThisSpanSpace =
                    neighbourBackingCountertopCuboidInNeighbourSpanSpace
                        .Transform(span.m_OrientationAlignedCabinet.GetTransform())
                        .InverseTransform(m_OrientationAlignedCabinet.GetTransform());

                countertopCuboidInSpanSpace.xMin =
                    Mathf.Min(
                        neighbourBackingCountertopCuboidInThisSpanSpace.xMin,
                        countertopCuboidInSpanSpace.xMin
                    );

                countertopCuboidInSpanSpace.xMax =
                    Mathf.Max(
                        neighbourBackingCountertopCuboidInThisSpanSpace.xMax,
                        countertopCuboidInSpanSpace.xMax
                    );
            }

            // Apply the calculated backing model sizes.
            m_CabinetBacking.SetModelSize(
                riserCuboidInSpanSpace,
                countertopCuboidInSpanSpace,
                kitchen.countertopThicknessInMeters
            );
        }

        /// <summary>
        ///     Return <see langword="true"/> if this cabinet span is empty or
        ///     consists entirely of <see cref="CabinetType.Space"/> cabinets.
        ///     This is intended to be used to determine if a cabinet span's
        ///     neighbours have extra countertop overhang (Sections building
        ///     method) to extend its own countertop. If the neighbour is a
        ///     space, then the countertop doesn't need to be extended.
        /// </summary>
        public bool IsSpace()
        {
            bool anySpaceCabinets = false;
            bool anyMidCabinets = false;

            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                if (i.element.cabinetType == CabinetType.Space)
                {
                    anySpaceCabinets = true;
                }
                else if (i.element.cabinetType.IsMidSpanCabinetType(kitchen.selectedBuildingMethod))
                {
                    anyMidCabinets = true;
                }
            }

            return anySpaceCabinets && !anyMidCabinets;
        }

        /// <summary>
        ///     Return <see langword="true"/> if this cabinet span represents
        ///     a corner wedge. A corner wedge span only has one cabinet: a
        ///     corner wedge cabinet.
        /// </summary>
        public bool IsCornerWedge()
        {
            return cabinets.Count == 1 && cabinets[0].element.Is(CabinetType.CornerWedge);
        }

        /// <summary>
        ///     Return <see langword="true"/> if the proposed <paramref
        ///     name="depthInMeters"/> works for all existing products in the
        ///     kitchen. Otherwise, return <see langword="false"/>.
        /// </summary>
        public bool CanResizeBaseCabinetDepth(float depthInMeters)
        {
            // Test if the requested size will fit all existing products.
            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                Cabinet cabinet = i.element;

                Cuboid proposedCabinetSize = cabinet.occupiableCuboid;

                if (cabinet.Is(CabinetType.Corner) &&
                    !cabinet.IsYawSimilar(orientationAlignedCabinet))
                {
                    proposedCabinetSize.xMax = depthInMeters;
                }
                else
                {
                    proposedCabinetSize.zMax = depthInMeters;
                }

                if (!CanOccupantsFitInNewCabinetDepth(cabinet, proposedCabinetSize))
                {
                    EventBridge.Designer.showToast?.Invoke(
                        "Cannot use this cabinet depth with the currently selected products.",
                        holdDuration: 2.5f
                    );

                    return false;
                }
            }

            return true;

            static bool CanOccupantsFitInNewCabinetDepth(Cabinet cabinet, Cuboid proposedCabinetSize)
            {
                foreach (Occupant occupant in cabinet.occupants)
                {
                    Cuboid occupiedCuboid = cabinet.CalculateOccupancyCuboidForOccupant(occupant);

                    if (proposedCabinetSize.zMax < occupiedCuboid.zMax)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        ///     Set the depth of all cabinets in this span.
        /// </summary>
        /// <param name="depthInMeters"></param>
        public void SetBaseCabinetDepth(float depthInMeters)
        {
            m_BaseCabinetDepthInMeters = depthInMeters;

            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                Cabinet cabinet = i.element;

                cabinet.SetBaseCabinetDepth(
                    m_BaseCabinetDepthInMeters,
                    cabinet.IsYawSimilar(orientationAlignedCabinet) ? CabinetResizeAxis.Z : CabinetResizeAxis.X
                );

                foreach (Occupant occupant in cabinet.occupants)
                {
                    if (!occupant.productVisual.info.anchor.IsOnOrAtFront())
                    {
                        // Products that are anchored to the back must be
                        // moved. This is a quick way of handling it.
                        occupant.MoveToAnchorPosition();
                    }
                }
            }
        }

        public async UniTask SetCabinetMaterials(
            GameObject currentHighlightObject,
            Material cabinetMaterial,
            Material countertopMaterial,
            Material productCabinetMaterialIdentifier)
        {
            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                Cabinet cabinet = i.element;

                // In Sections building method, individual cabinets can be
                // selected. Check that we do not stomp over a selected
                // cabinet's highlight.
                if (cabinet.gameObject != currentHighlightObject)
                {
                    await cabinet.SetCabinetMaterials(
                        cabinetMaterial,
                        countertopMaterial,
                        productCabinetMaterialIdentifier
                    );
                }
            }

            if (m_CabinetBacking && currentHighlightObject != m_CabinetBacking.gameObject)
            {
                m_CabinetBacking.OnCabinetMaterialsChanged(cabinetMaterial, countertopMaterial);
            }
        }

        /// <summary>
        ///     Called when any cabinet in the current kitchen is resized.
        ///     This is used to keep the backing's model size properly
        ///     synchronized to the cabinet's size.
        /// </summary>
        /// <param name="resizedCabinet"></param>
        protected void OnCabinetResized(Cabinet resizedCabinet)
        {
            bool contains = (Contains(resizedCabinet));
            MelcherLog.Log("OnCabinetResized. Contains=" + contains);
            // If one of our cabinets is resizing, handle it immediately
            if (contains)
            {
                UpdateBackingSizeAndPosition();
            }
        }

        /// <summary>
        ///     Change the parent of the cabinet span's backing if the backing
        ///     happens to be a child of <paramref
        ///     name="cabinetBeingDestroyed"/>. This prevents the backing from
        ///     being removed unintentionally, if the user happens to destroy
        ///     <see cref="m_OrientationAlignedCabinet"/>.
        /// </summary>
        public void RelocateBackingFromDestructingCabinet(Cabinet cabinetBeingDestroyed)
        {
            if (!m_CabinetBacking)
            {
                // No backing to relocate.
                return;
            }

            Transform cabinetBackingTransform = m_CabinetBacking.GetTransform();

            if (cabinetBackingTransform != cabinetBeingDestroyed.GetTransform())
            {
                // Backing is already on a different cabinet.
                return;
            }

            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                Cabinet otherCabinet = i.element;

                if (otherCabinet == cabinetBeingDestroyed)
                {
                    continue;
                }

                if (otherCabinet.cabinetType == CabinetType.Corner)
                {
                    // Skip corner cabinets. They may be rotated differently
                    // from the rest of the cabinets.
                    continue;
                }

                cabinetBackingTransform.SetParent(otherCabinet.GetTransform());

                break;
            }
        }

        protected void SetParentOfBacking(Transform newParent)
        {
            m_CabinetBacking.GetTransform().SetParent(newParent);
            m_CabinetBacking.GetTransform().SetSiblingIndex(0);
        }

        protected void ApplyCabinetBackingToCountertop(Cabinet cabinet, CabinetBacking backing, bool add)
        {
            // For backings that don't have a riser or countertop, the
            // backing's countertop changes are applied directly to the
            // cabinets' countertop.

            if (!backing)
            {
                return;
            }

            CuboidPadding backingCountertopOverhangInMetersDelta =
                GetBackingDirectCountertopOverhangDeltaInMetersForCabinet(backing, cabinet);

            CuboidPadding countertopOverhangInMeters =
                cabinet.GetCountertopOverhangInMeters();

            if (!add)
            {
                backingCountertopOverhangInMetersDelta = -backingCountertopOverhangInMetersDelta;
            }

            countertopOverhangInMeters.left += backingCountertopOverhangInMetersDelta.left;
            countertopOverhangInMeters.right += backingCountertopOverhangInMetersDelta.right;
            countertopOverhangInMeters.top += backingCountertopOverhangInMetersDelta.top;
            countertopOverhangInMeters.bottom += backingCountertopOverhangInMetersDelta.bottom;
            countertopOverhangInMeters.towardMinZ += backingCountertopOverhangInMetersDelta.towardMinZ;
            countertopOverhangInMeters.towardMaxZ += backingCountertopOverhangInMetersDelta.towardMaxZ;

            cabinet.SetCountertopOverhang(countertopOverhangInMeters);
        }

        public void GetCornerWedges(out Cabinet leftCornerWedge, out Cabinet rightCornerWedge)
        {
            leftCornerWedge = null;
            rightCornerWedge = null;

            foreach (CabinetSpan span in kitchen.layout.spans)
            {
                if (!span.IsCornerWedge())
                {
                    continue;
                }

                Cabinet wedge = span.cabinets[0].element;

                if (!Contains(wedge.cornerCabinetBeingWedged, out int indexOfCabinetBeingWedgedInItsSpan))
                {
                    continue;
                }

                if (indexOfCabinetBeingWedgedInItsSpan == 0)
                {
                    leftCornerWedge = wedge;
                }
                else
                {
                    rightCornerWedge = wedge;
                }
            }
        }

        // ====================================================================
        // Fields
        // ====================================================================

        /// <summary>
        ///     Contains all the cabinets that belong to this span. Some
        ///     cabinets may belong to more than one span. For example, corners.
        ///     The `index` member in each entry refers to the index of the
        ///     cabinet within the layout.
        /// </summary>
        protected MelcherReadWriteList<ElementIndex<Cabinet>> m_Cabinets = new ();

        /// <summary>
        ///     This is the first cabinet section in the cabinet span that is
        ///     not a cabinet corner. It's rotation is representative of all
        ///     cabinets in the span except for a possible leading corner
        ///     section whose rotation will match that of the preceding cabinet
        ///     span. This field is used to convert the adjustment sizes of
        ///     cabinet backings so they are correctly applied to the leading
        ///     corner section.
        /// </summary>
        protected Cabinet m_OrientationAlignedCabinet;

        /// <summary>
        ///     The bar top or backsplash (or nothing) that runs this length
        ///     of cabinets.
        /// </summary>
        protected CabinetBacking m_CabinetBacking;

        protected float m_BaseCabinetDepthInMeters;

    }   // class CabinetSpan

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-11-08
/// ===========================================================================
