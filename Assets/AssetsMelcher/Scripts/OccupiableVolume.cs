namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Server;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    /// <summary>
    ///     Represents a 3D space that can be occupied by products. This may be
    ///     a cabinet section as a whole or some subregion of the cabinet
    ///     (e.g., a cabinet shelf). The volume can be resized lengthwise by
    ///     <see by cref="ResizerWidget"/>.
    ///     <br /><br />
    ///
    ///     There are three types of occupiable volumes: cabinets, cabinet spans
    ///     and shelves.
    /// </summary>
    public abstract class OccupiableVolume : MelcherMonoBehaviour
    {
        // ====================================================================
        // Static functions
        // ====================================================================

        /// <summary>
        ///     Transform a position that is local to one volume to a position
        ///     that is local to another volume. This temporarily transforms
        ///     the position to world space and then transforms it back:
        ///     input local -> world -> output local
        /// </summary>
        /// <param name="inputVolume">
        ///     The occupiable volume whose coordinate space defines the
        ///     coordinate space of <paramref name="inputSize"/>.
        /// </param>
        /// <param name="inputValue">
        ///     The value to transform.
        /// </param>
        /// <param name="outputVolume">
        ///     The occupiable volume whose coordinate space will be used as
        ///     the output transformation space.
        /// </param>
        /// <returns>
        ///     Value in <paramref name="outputVolume"/> space.
        /// </returns>
        public static Vector3 TransformPositionBetweenVolumes(
            OccupiableVolume inputVolume,
            Vector3 inputValue,
            OccupiableVolume outputVolume)
        {
            Vector3 valueInWorldSpace =
                inputVolume.GetTransform().TransformPoint(
                    inputValue
                );

            Vector3 valueInOutputLocalSpace =
                outputVolume.GetTransform().InverseTransformPoint(
                    valueInWorldSpace
                );

            return valueInOutputLocalSpace;
        }

        /// <summary>
        ///     Transform a size that is local to one cabinet to a size
        ///     that is local to another cabinet. This temporarily transforms the
        ///     the size to world space and then transforms it back:
        ///     input local -> world -> output local
        /// </summary>
        /// <param name="inputVolume">
        ///     The cabinet whose coordinate space defines the coordinate space
        ///     of <paramref name="inputSize"/>.
        /// </param>
        /// <param name="inputSize">
        ///     The value to transform.
        /// </param>
        /// <param name="outputVolume">
        ///     The cabinet whose coordinate space will be used as the output
        ///     transformation space.
        /// </param>
        /// <returns>
        ///     Value in <paramref name="outputVolume"/> space.
        /// </returns>
        public static Vector3 TransformSizeBetweenVolumes(
            OccupiableVolume inputVolume,
            Vector3 inputSize,
            OccupiableVolume outputVolume)
        {
            Vector3 sizeInWorldSpace =
                inputVolume.GetTransform().TransformDirection(
                    inputSize
                );

            Vector3 sizeInOutputLocalSpace =
                outputVolume.GetTransform().InverseTransformDirection(
                    sizeInWorldSpace
                );

            // The coordinate space conversion can introduce negative numbers.
            // We'll use Abs() to ensure the size is always positive
            sizeInOutputLocalSpace = MelcherCommon.Abs(sizeInOutputLocalSpace);

            return sizeInOutputLocalSpace;
        }

        /// <summary>
        ///     Transform a <see cref="CuboidPadding"/> that is relative to
        ///     <paramref name="inputVolume"/> to a padding that is in
        ///     <paramref name="outputVolume"/>'s space.
        /// </summary>
        public static CuboidPadding TransformCuboidPaddingBetweenVolumes(
            OccupiableVolume inputVolume,
            CuboidPadding inputPadding,
            OccupiableVolume outputVolume)
        {
            Vector3 inputMinExtent =
                new Vector3(inputPadding.left, inputPadding.bottom, inputPadding.towardMinZ);

            Vector3 inputMaxExtent =
                new Vector3(inputPadding.right, inputPadding.top, inputPadding.towardMaxZ);

            Vector3 outputMinExtent =
                TransformSizeBetweenVolumes(
                    inputVolume,
                    inputMinExtent,
                    outputVolume
                );

            Vector3 outputMaxExtent =
                TransformSizeBetweenVolumes(
                    inputVolume,
                    inputMaxExtent,
                    outputVolume
                );

            return new CuboidPadding()
            {
                left = outputMinExtent.x,
                right = outputMaxExtent.x,
                bottom = outputMinExtent.y,
                top = outputMaxExtent.y,
                towardMinZ = outputMinExtent.z,
                towardMaxZ = outputMaxExtent.z
            };
        }

        /// <summary>
        ///     Given an <see cref="Occupant"/>, calculate the cuboid it
        ///     occupies in the local space of <paramref name="toVolume"/>.
        /// </summary>
        /// <param name="occupant"></param>
        /// <param name="toVolume"></param>
        /// <returns></returns>
        public static Cuboid TransformOccupancyCuboidBetweenVolumes(Occupant occupant, OccupiableVolume toVolume)
        {
            OccupiableVolume fromVolume = occupant.occupiedVolume;

            Cuboid occupancyCuboid =
                fromVolume.CalculateOccupancyCuboidForOccupant(occupant);

            if (fromVolume != toVolume)
            {
                // fromVolume-Local -> World
                occupancyCuboid =
                    Cuboid.Transform(fromVolume.GetTransform(), occupancyCuboid);

                // World -> toVolume-Local
                occupancyCuboid =
                    Cuboid.InverseTransform(toVolume.GetTransform(), occupancyCuboid);
            }

            return occupancyCuboid;
        }

        /// <summary>
        ///     Return <see langword="true"/> if the occupant's occupancy cuboid
        ///     overlaps the cabinet backing's occupancy cuboid. If <paramref
        ///     name="backingOccupancyInWorldSpace"/> is <see langword="null"/>,
        ///     the function always returns <see langword="false"/>.
        /// </summary>
        // TODO: This is not compatible with cabinets at non-orthogonal angles,
        //       but 45-degree cabinets cannot have backings of their own so
        //       this may not matter. -Ryan
        protected static bool IsOccupantOverlappingBacking(
            Transform volumeTransform,
            Cuboid occupantOccupancyWithinVolume,
            Cuboid? backingOccupancyInWorldSpace)
        {
            if (!backingOccupancyInWorldSpace.HasValue)
            {
                return false;
            }

            Cuboid backingCuboidInVolumeSpace =
                backingOccupancyInWorldSpace.Value.InverseTransform(
                    volumeTransform
                );

            bool doesOverlapBacking =
                Cuboid.Overlaps(
                    occupantOccupancyWithinVolume,
                    backingCuboidInVolumeSpace
                );

            return doesOverlapBacking;
        }

        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        ///     The parent occupiable volume. This will typically be <see
        ///     langword="null"/> but some volumes are nestable and they will
        ///     return the volume they exist within. For example, <see
        ///     cref="CabinetShelf"/> can be nested within a <see
        ///     cref="Cabinet"/>.
        /// </summary>
        public abstract OccupiableVolume parentVolume { get; }

        /// <summary>
        ///     A cuboid in the volume's local space that represents the
        ///     entire cabinet or cabinet shelf cut-out. This is used to anchor
        ///     the <see cref="ResizerWidget"/>s to the correct locations.
        /// </summary>
        /// <value></value>
        public abstract Cuboid resizerCuboid { get; }

        /// <summary>
        ///     A cuboid in this volume's local space that represents the
        ///     area that it allows to be occupied.
        /// </summary>
        public abstract Cuboid occupiableCuboid { get; }

        public CuboidPadding occupiablePaddingInMeters
        {
            get => m_OccupiablePaddingInMeters;
            set => m_OccupiablePaddingInMeters = value;
        }

        /// <summary>
        ///     A read-only collection of all occupants added specifically to
        ///     this volume.
        /// </summary>
        public ReadOnlyCollection<Occupant> occupants => m_Occupants.readOnly;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Get a Transform that is used to orient occupants to the current
        ///     cabinet. This is used to match occupants to the 45-degree angle
        ///     of some corner cabinets.
        /// </summary>
        public Transform GetLocalOrientationForOccupants()
        {
            return m_OccupantOrientation;
        }

        public Transform GetTransform()
        {
            return m_Transform;
        }

        /// <summary>
        ///     This is called anytime a product may be added to this volume.
        ///     If this function returns <see langword="true"/>, the product
        ///     is allowed to to be added (although the add may still fail for
        ///     other reasons like lack of space). This is intended to support
        ///     denylists for volumes of different types, like cabinet shelves
        ///     which only accept <see cref="ProductCategory.Burner"/> products.
        /// </summary>
        /// <param name="productInfo"></param>
        /// <param name="productVisual"></param>
        /// <returns></returns>
        public abstract bool CanContainProduct(ProductInfo productInfo, ProductVisualInfoSO productVisualInfo, ProductVisualContent productVisualContent);

        /// <summary>
        ///     Called when the user attempts to resize a cuboid by dragging
        ///     its resizer widget.
        /// </summary>
        /// <param name="lengthMetersDelta">
        ///     The change (in meters) to make to the cabinet's length.
        ///     Negative numbers make it smaller and vice versa.
        /// </param>
        /// <returns>
        ///     If the resize succeeded, return <see langword="true"/>;
        ///     otherwise, return <see langword="false"/>.
        /// </returns>
        public abstract bool TryToResizeLengthAsUser(ref float lengthMetersDelta, Extent extent);

        /// <summary>
        ///     Determines whether this volume applies a product's horizontal
        ///     padding. This was added to ensure that shelves reliably prevent
        ///     horizontal product padding, even when loading products from a
        ///     save file.
        /// </summary>
        public virtual bool DoesApplyHorizontalPadding(ProductInfo productInfo)
        {
            // When a specific layout size is specified, do not add additional
            // horizontal padding.
            return !productInfo.doLayoutUsingProductSize;
        }

        /// <summary>
        ///     Calculate the cuboid that an occupant occupies (or would
        ///     occupy) in this volume's local space. This volume's coordinate
        ///     space may be nested within another <see
        ///     cref="OccupiableVolume"/>'s space. Use <see
        ///     cref="TransformSizeBetweenVolumes(OccupiableVolume, Vector3, OccupiableVolume)"/>
        ///     to convert a cuboid that is relative to one volume to another.
        /// </summary>
        /// <param name="occupant"></param>
        /// <param name="overridePositionInVolumeX"></param>
        /// <param name="overridePositionInVolumeY"></param>
        /// <param name="overridePositionInVolumeZ"></param>
        /// <param name="overrideVolumeSizeInMeters"></param>
        /// <returns></returns>
        public Cuboid CalculateOccupancyCuboidForOccupant(
            Occupant occupant,
            float? overridePositionInVolumeX = null,
            float? overridePositionInVolumeY = null,
            float? overridePositionInVolumeZ = null,
            Cuboid? overrideOccupiableCuboid = null)
        {
            Vector3 metersFromVolumeMin = occupant.GetLocalPositionInVolume();

            // If no override is provided, use the product's existing offset
            if (!overridePositionInVolumeX.HasValue)
            {
                overridePositionInVolumeX = metersFromVolumeMin.x;
            }

            if (!overridePositionInVolumeY.HasValue)
            {
                overridePositionInVolumeY = metersFromVolumeMin.y;
            }

            if (!overridePositionInVolumeZ.HasValue)
            {
                overridePositionInVolumeZ = metersFromVolumeMin.z;
            }

            Cuboid anchoredCuboid =
                CalculateOccupancyCuboidForProductInfo(
                    occupant.productInfo,
                    occupant.productVisual,
                    occupant.productVisualContentInstance,
                    overridePositionInVolumeX,
                    overridePositionInVolumeY,
                    overridePositionInVolumeZ,
                    overrideOccupiableCuboid
                );

            return anchoredCuboid;
        }

        /// <summary>
        ///     Calculate the area a product will occupy within an occupiable
        ///     volume. The calculated area is defined in the volume's local
        ///     coordinate space and is confined to it's bounds (<see
        ///     cref="occupiableCuboid"/>).
        ///     <br /><br />
        ///
        ///     Elsewhere in the app, these cuboids are enforced to never
        ///     overlap with other products.
        /// </summary>
        public Cuboid CalculateOccupancyCuboidForProductInfo(
            ProductInfo     productInfo,
            ProductVisualSO productVisual,
            ProductVisualContent productVisualPrefab,
            float?          positionInVolumeX = null,
            float?          positionInVolumeY = null,
            float?          positionInVolumeZ = null,
            Cuboid?         overrideOccupiableCuboid = null)
        {
            // Get the current cut-out size of the product. Some products, like
            // cabinet shelves, can be resized and therefore their cut-out can
            // differ from the default value stored in the ProductInfo.
            Vector3 occupiedSizeInMeters =
                productVisualPrefab.GetCurrentCutOutSizeInMeters(
                    defaultCutOutSizeInMeters: productInfo.cutOutSizeInMeters
                );

            if (productInfo.doLayoutUsingProductSize)
            {
                // Some products require more space than their cut-out would
                // suggest. In those cases, we use the product size to drive
                // how much space is needed.
                Vector3 productSizeInMeters = productInfo.productSizeInMeters;

                if (productSizeInMeters.x > 0.0f)
                {
                    occupiedSizeInMeters.x = productSizeInMeters.x;
                }

                if (productSizeInMeters.y > 0.0f)
                {
                    occupiedSizeInMeters.y = productSizeInMeters.y;
                }

                if (productSizeInMeters.z > 0.0f)
                {
                    occupiedSizeInMeters.z = productSizeInMeters.z;
                }
            }

            if (!overrideOccupiableCuboid.HasValue)
            {
                // Use the volume's current size when an override isn't
                // provided.
                overrideOccupiableCuboid = occupiableCuboid;
            }

            Cuboid occupiedCuboid =
                Occupant.GetCutOutCuboidInVisualSpace(
                    productVisual.info,
                    productVisualPrefab,
                    occupiedSizeInMeters
                );

            // Add the visual's local position to get the cuboid
            // in the volume's local space:
            occupiedCuboid.position +=
                productVisualPrefab.GetTransform().localPosition;

            // Clip the occupied size if using a negative inset.
            // TODO: Clip the occupied size if using a shelf distance (old
            //       comment from before CabinetShelf existed).
            if (productVisual.info.initialInsetFromEdgeInMeters < 0.0f)
            {
                switch (productVisual.info.anchor)
                {
                    case ProductAnchor.OnFrontAtBottom:
                    case ProductAnchor.OnBackAtBottom:
                        occupiedCuboid.yMin = occupiableCuboid.yMin;
                        break;

                    case ProductAnchor.OnFrontAtTop:
                    case ProductAnchor.OnBackAtTop:
                        occupiedCuboid.yMax = occupiableCuboid.yMax;
                        break;

                    case ProductAnchor.OnTopAtFront:
                        occupiedCuboid.zMin = occupiableCuboid.zMin;
                        break;

                    case ProductAnchor.OnTopAtBack:
                        occupiedCuboid.zMax = occupiableCuboid.zMax;
                        break;

                    default:
                        MelcherLog.LogError($"Unhandled product anchor '{productVisual.info.anchor}' for product \"{productInfo.name}\", ID {productInfo.id}", productVisualPrefab);
                        break;
                }
            }

            Cuboid anchoredCuboid = occupiedCuboid;

            // Only add top padding when the product is not flush with the
            // cabinet top and the padding will not otherwise cause the
            // product to become unplaceable.
            //
            // This value represents the tallest product height that will still
            // receive top-padding. (Taller products would just not fit in the
            // cabinet if they were padded.) Minimum plunge depth is to account
            // for space occupied by products that rest on the counter-top.
            float maximumProductHeightToPad =
                occupiableCuboid.height - productInfo.productPaddingInMeters.top - Occupant.MinimumPlungeDepth;

            float topPadding = 0.0f;

            if (!productVisual.info.anchor.IsOnTop() &&
                (productVisual.info.anchor != ProductAnchor.OnFrontAtTop && productVisual.info.initialInsetFromEdgeInMeters < 0.0f) &&
                anchoredCuboid.height < maximumProductHeightToPad)
            {
                // IsOnTop is checked to prevent items like BBQs and burners
                // having their bounds extended beyond the top of the cabinet.

                topPadding = productInfo.productPaddingInMeters.top;

                anchoredCuboid.yMax += topPadding;

                // We never apply padding to the bottom of products. This allows
                // fridges to remain flush with the bottom of the cabinet. It also
                // means we do not need to shift or center the product model
                // vertically in its cuboid.
            }

            // The amount to shift the product's cuboid to align with its anchor
            Vector3 cuboidPosition = anchoredCuboid.position;

            switch (productVisual.info.anchor)
            {
                case ProductAnchor.OnFrontAtBottom:
                    // Do nothing
                    break;

                case ProductAnchor.OnFrontAtTop:
                case ProductAnchor.OnTopAtFront:
                    cuboidPosition.y = overrideOccupiableCuboid.Value.yMax - anchoredCuboid.height;
                    break;

                case ProductAnchor.OnTopAtBack:
                case ProductAnchor.OnBackAtTop:
                    cuboidPosition.y = overrideOccupiableCuboid.Value.yMax - anchoredCuboid.height;
                    cuboidPosition.z = overrideOccupiableCuboid.Value.zMax - anchoredCuboid.depth;
                    break;

                case ProductAnchor.OnBackAtBottom:
                    cuboidPosition.z = overrideOccupiableCuboid.Value.zMax - anchoredCuboid.depth;
                    break;

                default:
                    MelcherLog.LogError($"Unhandled product anchor '{productVisual.info.anchor}' for product \"{productInfo.name}\", ID {productInfo.id}", productVisualPrefab);
                    break;
            }

            // Offset the cuboid by the product inset. This is only required
            // if the inset is greater than zero because negative numbers
            // have already been handled earlier in this function and values of
            // zero require no work. The inset slides the product along the
            // face it is anchored to towards the face opposite of it.
            // E.g. OnFrontAtBottom: inset moves product from bottom to top.
            //      OnTopAtBack: inset moves product from back to front.
            if (productVisual.info.initialInsetFromEdgeInMeters > 0.0f)
            {
                float adjustedInsetFromEdgeInMeters = productVisual.info.initialInsetFromEdgeInMeters;

                switch (productVisual.info.anchor)
                {
                    //case ProductAnchor.OnFrontAtBottom:
                    //case ProductAnchor.OnBackAtBottom:
                    case ProductAnchor.OnFrontAtTop:
                    case ProductAnchor.OnBackAtTop:
                        // If an inset from the edge is specified and the
                        // product is on the front or back cabinet face, we
                        // automatically subtract the vertical padding from the
                        // inset to compensate and precisely position the model
                        // at the intended height.
                        adjustedInsetFromEdgeInMeters -= topPadding;
                        if (adjustedInsetFromEdgeInMeters < 0.0f)
                        {
                            adjustedInsetFromEdgeInMeters = 0.0f;
                        }
                        break;

                    default:
                        // Do nothing for products mounted on the top because,
                        // for them, the inset does not affect the vertical
                        // padding
                        break;
                }

                switch (productVisual.info.anchor)
                {
                    case ProductAnchor.OnFrontAtBottom:
                    case ProductAnchor.OnBackAtBottom:
                        // Shift the product towards the volume top
                        //cuboidPosition.y += productVisual.insetFromEdgeInMeters;
                        if (!positionInVolumeY.HasValue)
                        {
                            positionInVolumeY = overrideOccupiableCuboid.Value.zMin + adjustedInsetFromEdgeInMeters;
                        }
                        break;

                    case ProductAnchor.OnFrontAtTop:
                    case ProductAnchor.OnBackAtTop:
                        // Shift the product towards the volume bottom
                        //cuboidPosition.y -= productVisual.insetFromEdgeInMeters;
                        if (!positionInVolumeY.HasValue)
                        {
                            positionInVolumeY = overrideOccupiableCuboid.Value.yMax - adjustedInsetFromEdgeInMeters;
                        }
                        break;

                    case ProductAnchor.OnTopAtFront:
                        // Shift towards the volume bottom
                        //cuboidPosition.z += productVisual.insetFromEdgeInMeters;
                        if (!positionInVolumeZ.HasValue)
                        {
                            positionInVolumeZ = overrideOccupiableCuboid.Value.zMin + adjustedInsetFromEdgeInMeters;
                        }
                        break;

                    case ProductAnchor.OnTopAtBack:
                        // Shift towards the cabinet front
                        //cuboidPosition.z -= productVisual.insetFromEdgeInMeters;
                        if (!positionInVolumeZ.HasValue)
                        {
                            positionInVolumeZ = overrideOccupiableCuboid.Value.zMin - adjustedInsetFromEdgeInMeters;
                        }
                        break;
                }
            }

            // Apply the mandatory horizontal padding if necessary
            if (DoesApplyHorizontalPadding(productInfo))
            {
                anchoredCuboid.xMax += productInfo.productPaddingInMeters.left;
                anchoredCuboid.xMax += productInfo.productPaddingInMeters.right;
            }

            // Now we apply the optional product positioning. We must check
            // if the cuboid's origin differs from the product's origin and
            // account for that.

            if (positionInVolumeX.HasValue)
            {
                // X is always relative to the same side (cuboid & product)
                cuboidPosition.x = positionInVolumeX.Value;
            }

            if (positionInVolumeY.HasValue)
            {
                switch (productVisual.info.anchor)
                {
                    case ProductAnchor.OnTopAtFront:
                    case ProductAnchor.OnTopAtBack:
                    case ProductAnchor.OnFrontAtTop:
                    case ProductAnchor.OnBackAtTop:
                        // The product's origin is at the top.
                        cuboidPosition.y = positionInVolumeY.Value - anchoredCuboid.height + topPadding - m_OccupiablePaddingInMeters.bottom;
                        break;

                    default:
                        // Both have an origin at the bottom.
                        cuboidPosition.y = positionInVolumeY.Value;
                        break;
                }
            }

            if (positionInVolumeZ.HasValue)
            {
                switch (productVisual.info.anchor)
                {
                    case ProductAnchor.OnBackAtBottom:
                    case ProductAnchor.OnBackAtTop:
                    case ProductAnchor.OnTopAtBack:
                        // The product's origin is at the back
                        cuboidPosition.z = positionInVolumeZ.Value - anchoredCuboid.depth;
                        break;

                    default:
                        // Both have an origin at the front.
                        cuboidPosition.z = positionInVolumeZ.Value;
                        break;
                }
            }

            anchoredCuboid.position = cuboidPosition;

            if (productVisualPrefab.TryGetComponent(out IProductVisualModifyOccupiedCuboidHandler modifyOccupiedCuboidHandler))
            {
                // Allow visuals to alter the size of their occupancy cuboid.
                modifyOccupiedCuboidHandler.OnModifyOccupiedCuboid(ref anchoredCuboid, this);
            }

            // This is to account for some floating point math inaccuracy
            // causing some valid positions to be seen as invalid. Example:
            // 0.8890001    <- Product's anchor
            // 0.889        <- Cabinet height
            if (MelcherCommon.IsAlmostEqual(anchoredCuboid.yMax, occupiableCuboid.yMax, tolerance: 0.00001f))
            {
                anchoredCuboid.yMax = occupiableCuboid.yMax;
            }

            //OrientedBounds orientedBounds =
            //    new OrientedBounds(
            //        new Bounds(anchoredCuboid.center, anchoredCuboid.size),
            //        m_Transform.worldToLocalMatrix
            //    );

            //orientedBounds.Draw(Color.yellow, 3.0f);

            return anchoredCuboid;
        }


        /// <summary>
        ///     Sorts the occupant list by the product's x-position. The
        ///     occupant list must always be sorted in this fashion. Call this
        ///     function whenever the volume has an occupant added, moved or
        ///     removed.
        /// </summary>
        protected void SortOccupantsByPosition()
        {
            m_Occupants.mutable.Sort(Occupant.CompareByVolumePosition);
        }

        public bool IsAnyOccupantBlockingShrink(ref float lengthDeltaInMeters, Extent extent, bool autoCenterProducts)
        {
            if (lengthDeltaInMeters > 0.0f)
            {
                MelcherLog.LogError(
                    $"{nameof(IsAnyOccupantBlockingShrink)} was called with a positive {nameof(lengthDeltaInMeters)}. " +
                        "This indicates the cabinet is growing, not shrinking.",
                    context: this
                );
                return false;
            }

            // Flip delta's sign so it is positive to simplify the code
            float metersToShrink = Mathf.Abs(lengthDeltaInMeters);

            if (m_Occupants.readOnly.Count > 0)
            {
                if (autoCenterProducts)
                {
                    // Find the widest occupant to determine how much space
                    // is available to shrink.

                    float widestOccupantWidthInMeters = float.MinValue;

                    foreach (Occupant occupant in m_Occupants.readOnly)
                    {
                        Cuboid occupantCuboid =
                            CalculateOccupancyCuboidForOccupant(occupant);

                        if (occupantCuboid.width > widestOccupantWidthInMeters)
                        {
                            widestOccupantWidthInMeters = occupantCuboid.width;
                        }
                    }

                    float allowableShrinkAmountInMeters =
                        occupiableCuboid.width - widestOccupantWidthInMeters;

                    if (metersToShrink > allowableShrinkAmountInMeters)
                    {
                        metersToShrink = allowableShrinkAmountInMeters;
                    }

                    if (metersToShrink <= 0.0f)
                    {
                        lengthDeltaInMeters = 0.0f;
                        return true;
                    }
                }
                else if (extent == Extent.Min)
                {
                    // Find first occupant that blocks resizing. I.e. the
                    // leftmost occupant
                    Occupant leftmostOccupant = null;
                    float leftmostOccupantX = float.MaxValue;

                    foreach (Occupant occupant in m_Occupants.readOnly)
                    {
                        if (occupant.metersFromVolumeMinX < leftmostOccupantX)
                        {
                            leftmostOccupant = occupant;
                            leftmostOccupantX = occupant.metersFromVolumeMinX;
                        }
                    }

                    if (!leftmostOccupant)
                    {
                        // No occupants (or they are just a backsplash/bar top)
                        // to block the shrink.
                        return false;
                    }

                    float leadingSpaceInMeters  =
                        leftmostOccupant.metersFromVolumeMinX - occupiableCuboid.xMin;

                    if (metersToShrink > leadingSpaceInMeters)
                    {
                        metersToShrink = leadingSpaceInMeters;
                    }

                    if (metersToShrink <= 0.0f)
                    {
                        lengthDeltaInMeters = 0.0f;
                        return true;
                    }
                }
                else
                {
                    // For resizing from the max extent, we need to scan every
                    // product to know which one has the furthest reach
                    Occupant furthestOccupant = null;
                    float furthestMaxX = 0.0f;

                    foreach (Occupant occupant in m_Occupants.readOnly)
                    {
                        float occupantMaxX =
                            CalculateOccupancyCuboidForOccupant(occupant).xMax;

                        if (occupantMaxX > furthestMaxX)
                        {
                            furthestOccupant = occupant;
                            furthestMaxX = occupantMaxX;
                        }
                    }

                    // Occupant is just a backsplash/bar top
                    if (furthestOccupant == null)
                    {
                        return false;
                    }

                    Cuboid lastOccupantCuboid =
                        CalculateOccupancyCuboidForOccupant(furthestOccupant);

                    float trailingSpaceInMeters = occupiableCuboid.xMax - furthestMaxX;

                    if (metersToShrink > trailingSpaceInMeters)
                    {
                        metersToShrink = trailingSpaceInMeters;
                    }

                    if (metersToShrink <= 0.0f)
                    {
                        lengthDeltaInMeters = 0.0f;
                        return true;
                    }
                }
            }

            // Use clamped delta and restore its negative sign
            lengthDeltaInMeters = -metersToShrink;
            return false;
        }

        /// <summary>
        /// Try to remove a product from this cabinet. If the product is found
        /// and successfully removed, this will clear the occupant's cabinet
        /// reference and then call <see cref="EventBridge.Designer.onOccupantRemovedFromCabinet"/>.
        /// </summary>
        /// <param name="occupant">Product to remove</param>
        /// <returns>
        /// Return true if the occupant was found and removed; otherwise, return
        /// false.
        /// </returns>
        public async UniTask<bool> TryRemoveOccupant(Occupant occupant)
        {
            if (occupant == null)
            {
                throw new System.ArgumentNullException(nameof(occupant));
            }

            int occupantIndex = m_Occupants.readOnly.IndexOf(occupant);
            if (occupantIndex < 0)
            {
                MelcherLog.LogError(
                    "The cabinet does not have this occupant. Cabinet name: " + name,
                    occupant
                );
                return false;
            }

            bool didRemove = m_Occupants.mutable.Remove(occupant);

            if (didRemove)
            {
                await EventBridge.Designer.onOccupantRemovedFromCabinet.InvokeIfNonEmpty(occupant);

                occupant.SetCabinetVolume(null, volumePositionX: 0.0f);

                await OnOccupantRemoved(occupant);
            }
            else
            {
                MelcherLog.LogError(
                    $"Failed to remove occupant from cabinet. Cabinet name {name}. Product name {occupant.name}",
                    occupant
                );
            }

            return didRemove;
        }


        /// <summary>
        /// Check if the cabinet's size can be changed without causing the
        /// existing occupants to move out of bounds. This is intended to
        /// handle bar tops because they change the depth of the cabinet
        /// when they are added or removed. The optional <paramref
        /// name="occupantToIgnore"/> parameter is used to exclude the bar top
        /// (or object affecting the cabinets size) from the check.
        /// </summary>
        /// <remarks>
        /// This may be able to handle non-depth size changes but that use-case
        /// wasn't needed at the time.
        /// </remarks>
        public bool CanVolumeResizeFitOccupants(CuboidPadding changeAmountInMeters, Occupant occupantToIgnore = null)
        {
            s_VolumeResizeTestCuboids.Clear();

            Cuboid proposedVolumeOccupiableCuboid =
                changeAmountInMeters.Inflate(occupiableCuboid);

            // Build a list of occupants with their new proposed anchored
            // position
            foreach (Occupant occupant in m_Occupants.readOnly)
            {
                if (occupant == occupantToIgnore)
                {
                    continue;
                }

                Cuboid reanchoredCuboid =
                    CalculateOccupancyCuboidForOccupant(
                        occupant,
                        overrideOccupiableCuboid: proposedVolumeOccupiableCuboid
                    );

                s_VolumeResizeTestCuboids.Add(
                    new ReanchoredOccupant(occupant, reanchoredCuboid)
                );
            }

            bool failed = false;

            // Verify the proposed positions have no overlaps
            int occupantCount = s_VolumeResizeTestCuboids.Count;
            for (int i = 0; i < occupantCount; ++i)
            {
                ReanchoredOccupant reanchoredOccupant = s_VolumeResizeTestCuboids[i];

                // Ensure the occupants will fit in the new size (skipping some
                // occupants that don't block a resize like a bar-top)
                bool doesFit =
                    ProFitUtility.IsCuboidContainedWithin(
                        reanchoredOccupant.cuboid,
                        proposedVolumeOccupiableCuboid
                    );

                if (!doesFit)
                {
                    MelcherLog.Log($"failing resize check on product: {reanchoredOccupant.occupant}", reanchoredOccupant.occupant);
                    failed = true;
                    continue;
                    //return false;
                }

                // Compare each unique pair of occupants' cuboids for overlaps
                for (int j = i + 1; j < occupantCount; ++j)
                {
                    if (Cuboid.Overlaps(
                        reanchoredOccupant.cuboid,
                        s_VolumeResizeTestCuboids[j].cuboid))
                    {
                        MelcherLog.Log($"failing resize check. Products would overlap. Product 1 {reanchoredOccupant.occupant}", reanchoredOccupant.occupant);
                        MelcherLog.Log($"                                              Product 2 {s_VolumeResizeTestCuboids[j].occupant}", s_VolumeResizeTestCuboids[j].occupant);
                        return false;
                    }
                }
            }

            if (failed)
            {
                return false;
            }

            return true;
        }

        public bool TryMoveOccupantNear(
            Occupant    occupant,
            Cuboid?     backingOccupancyInWorldSpace,
            ref float   metersFromVolumeMinX,
            ref float   metersFromVolumeMinY)
        {
            bool canFitProduct =
                CanMoveOccupantNear(
                    occupant,
                    backingOccupancyInWorldSpace,
                    ref metersFromVolumeMinX,
                    ref metersFromVolumeMinY
                );

            if (!canFitProduct)
            {
                return false;
            }

            return
                TryMoveOccupantTo(
                    occupant,
                    backingOccupancyInWorldSpace,
                    metersFromVolumeMinX,
                    metersFromVolumeMinY
                );
        }

        /// <summary>
        ///     This will check if a product can be moved near a requested
        ///     position. This function will try a basic fixup of the requested
        ///     position if it is blocked or out of bounds.
        /// </summary>
        /// <param name="metersFromVolumeMinX">
        ///     The suggested local x-position the occupant will be moved to in
        ///     the new volume (local to the new volume).
        /// </param>
        /// <param name="metersFromVolumeMinY">
        ///     The suggested local y-position the occupant will be moved to in
        ///     the new volume (local to the new volume).
        /// </param>
        public bool CanMoveOccupantNear(
            Occupant    occupant,
            Cuboid?     backingOccupancyInWorldSpace,
            ref float   metersFromVolumeMinX,
            ref float   metersFromVolumeMinY)
        {
            //using var drawScope = Cuboid.StartDrawScope();

            Cuboid occupantCuboid =
                CalculateOccupancyCuboidForOccupant(
                    occupant,
                    overridePositionInVolumeX: metersFromVolumeMinX,
                    overridePositionInVolumeY: metersFromVolumeMinY
                );

            Cuboid volumeOccupiableCuboid = occupiableCuboid;

            // Clamp the proposed position to be within the volume.
            float requestedMetersFromVolumeMinX =
                Mathf.Clamp(
                    metersFromVolumeMinX,
                    volumeOccupiableCuboid.xMin,
                    volumeOccupiableCuboid.xMax - occupantCuboid.width
                );

            Vector3 min = volumeOccupiableCuboid.min;
            Vector3 max = volumeOccupiableCuboid.max;
            min.y = (min.y + max.y) * 0.5f;
            max.y = min.y;
            min.z = 0.0f;
            max.z = 0.0f;

            Debug.DrawLine(
                GetTransform().TransformPoint(min),
                GetTransform().TransformPoint(max),
                Color.cyan,
                s_LineDuration
            );

            // Patch the occupant cuboid with the clamped x-position
            Vector3 anchoredPosition = occupantCuboid.position;
            anchoredPosition.x = requestedMetersFromVolumeMinX;
            occupantCuboid.position = anchoredPosition;

            {
                Cuboid c =
                    Cuboid.Transform(GetTransform(), occupantCuboid);
                c.Draw(Color.cyan, 1.0f);
                Debug.DrawLine(Vector3.zero, c.position, Color.cyan);
            }

            // First, can the occupant be added directly here?
            bool canAddDirectly =
                CanMoveOccupantTo(
                    occupant,
                    backingOccupancyInWorldSpace,
                    requestedMetersFromVolumeMinX,
                    metersFromVolumeMinY: metersFromVolumeMinY
                );

            if (canAddDirectly)
            {
                MelcherLog.Log(
                    "Product can be moved directly to volume x-location: " + requestedMetersFromVolumeMinX,
                    severity: MelcherLogSeverity.Verbose
                );

                return true;
            }

            // It can't be added here because something is blocking it.

            // If it's the backing, then there's no hope for it.
            // TODO: What if the product can be placed lower? Then it might
            //       fit under the backing.
            bool wouldOverlapBacking =
                IsOccupantOverlappingBacking(
                    GetTransform(),
                    occupantCuboid,
                    backingOccupancyInWorldSpace
                );

            if (wouldOverlapBacking)
            {
                MelcherLog.Log(
                    "Product would overlap cabinet's backing.",
                    severity: MelcherLogSeverity.Verbose
                );

                return false;
            }

            // Find the objects that are immediately blocking it and look to
            // their left and right for an opening.
            float minOverlappingX = float.MaxValue;
            float maxOverlappingX = float.MinValue;

            // Ensure the search starts from the outermost OccupiableVolume.
            // I.e., if the executing object is a CabinetShelf, we jump up one
            // or more levels to the Cabinet. This allows the search to spill
            // into all shelves and ensure none of their occupants overlap
            // either.
            OccupiableVolume cabinetVolume = this;

            while (cabinetVolume.parentVolume)
            {
                cabinetVolume = cabinetVolume.parentVolume;
            }

            foreach (Occupant otherOccupant in cabinetVolume.occupants)
            {
                // Ignore the occupant that is being moved
                if (occupant == otherOccupant)
                {
                    continue;
                }

                otherOccupant.TryGetComponent(out OccupiableVolume nestedVolume);
                if (nestedVolume && nestedVolume == occupant.occupiedVolume)
                {
                    // Search for the nearest spot inside this shelf
                    Cuboid nestedOccupiableCuboid = nestedVolume.occupiableCuboid;

                    // Intentional crossover between xMin <-> xMax. We're
                    // constraining the object within the bounds of another
                    // product (the shelf). Therefore, the shelf's minimum
                    // bounds can be treated like a max (blocker's right edge)
                    // and vice-versa.
                    minOverlappingX = Mathf.Min(minOverlappingX, nestedOccupiableCuboid.xMax);
                    maxOverlappingX = Mathf.Max(maxOverlappingX, nestedOccupiableCuboid.xMin);
                }
                else
                {
                    Cuboid otherOccupantCuboid =
                        TransformOccupancyCuboidBetweenVolumes(otherOccupant, this);

                    {
                        Cuboid c =
                            otherOccupantCuboid.Transform(GetTransform());
                        c.Draw(Color.grey, s_LineDuration, drawX: false);
                        Debug.DrawLine(Vector3.zero, c.min, Color.red, s_LineDuration);
                    }

                    if (Cuboid.Overlaps(occupantCuboid, otherOccupantCuboid))
                    {
                        minOverlappingX = Mathf.Min(minOverlappingX, otherOccupantCuboid.xMin - 0.001f);
                        maxOverlappingX = Mathf.Max(maxOverlappingX, otherOccupantCuboid.xMax);

                        otherOccupantCuboid.Transform(GetTransform()).Draw(Color.red, s_LineDuration, drawX: true, offset: 1);

                        MelcherLog.Log($"Other occupant xMin={otherOccupantCuboid.xMin}, xMax={otherOccupantCuboid.xMax}", severity: MelcherLogSeverity.Verbose);
                    }
                }

                if (nestedVolume)
                {
                    foreach (Occupant nestedOccupant in nestedVolume.occupants)
                    {
                        if (occupant == nestedOccupant)
                        {
                            continue;
                        }

                        Cuboid nestedOccupantCuboid =
                            TransformOccupancyCuboidBetweenVolumes(nestedOccupant, this);

                        nestedOccupantCuboid.Transform(GetTransform()).Draw(Color.grey, s_LineDuration, drawX: false);

                        if (Cuboid.Overlaps(occupantCuboid, nestedOccupantCuboid))
                        {
                            minOverlappingX = Mathf.Min(minOverlappingX, nestedOccupantCuboid.xMin - 0.001f);
                            maxOverlappingX = Mathf.Max(maxOverlappingX, nestedOccupantCuboid.xMax);

                            nestedOccupantCuboid.Transform(GetTransform()).Draw(Color.red, s_LineDuration, drawX: true, offset: 1);
                        }
                    }
                }
            }

            float distanceToMinX = Mathf.Abs((minOverlappingX - occupantCuboid.width) - requestedMetersFromVolumeMinX);
            float distanceToMaxX = Mathf.Abs(maxOverlappingX - requestedMetersFromVolumeMinX);

            //Transform t = m_Transform;
            //Vector3 o = t.position;
            //Vector3 r = o + t.right * requestedMetersFromVolumeMinX;
            //Debug.DrawLine(o, r, Color.yellow, 0.1f, depthTest: false);
            //Debug.DrawLine(
            //    o,
            //    r + Vector3.right * occupantCuboid.width,
            //    Color.cyan,
            //    0.1f,
            //    false
            //);
            //Debug.DrawLine(
            //    r,
            //    o + t.right * minOverlappingX + Vector3.up * 0.1f,
            //    Color.red,
            //    0.1f,
            //    false
            //);
            //Debug.DrawLine(
            //    r,
            //    o + t.right * maxOverlappingX + Vector3.up * 0.1f,
            //    Color.green,
            //    0.1f,
            //    false
            //);

            //MelcherLog.Log("Could not move product directly to: " + requestedMetersFromVolumeMinX + $". Next two best locations: left={minOverlappingX} right={maxOverlappingX}");

            if (distanceToMinX < distanceToMaxX)
            {
                // Try left
                bool canFitOnLeft =
                    CanMoveOccupantTo(
                        occupant,
                        backingOccupancyInWorldSpace,
                        minOverlappingX - occupantCuboid.width,
                        metersFromVolumeMinY: metersFromVolumeMinY
                    );

                if (canFitOnLeft)
                {
                    MelcherLog.Log("Left side was closer: moved to left", severity: MelcherLogSeverity.Verbose);
                    metersFromVolumeMinX = minOverlappingX - occupantCuboid.width;
                    return true;
                }

                //MelcherLog.Log("Left side was closer: could not move in any direction", logLevel: MelcherLogLevel.Verbose);
                return false;
            }

            // Try right
            bool canFitOnRight =
                CanMoveOccupantTo(
                    occupant,
                    backingOccupancyInWorldSpace,
                    maxOverlappingX,
                    metersFromVolumeMinY: metersFromVolumeMinY
                );

            if (canFitOnRight)
            {
                MelcherLog.Log("Right side was closer: moved to right", severity: MelcherLogSeverity.Verbose);
                metersFromVolumeMinX = maxOverlappingX;
                return true;
            }

            //MelcherLog.Log("Right side was closer: could not move in any direction", logLevel: MelcherLogLevel.Verbose);
            return false;
        }

        public bool TryMoveOccupantTo(
            Occupant occupant,
            Cuboid? backingOccupancyInWorldSpace,
            float metersFromVolumeMinX,
            float? metersFromVolumeMinY = null,
            float? metersFromVolumeMinZ = null)
        {
            bool canMove =
                CanMoveOccupantTo(
                    occupant,
                    backingOccupancyInWorldSpace,
                    metersFromVolumeMinX,
                    metersFromVolumeMinY,
                    metersFromVolumeMinZ
                );

            if (canMove)
            {
                Vector3 newMetersFromVolumeMin =
                    occupant.GetLocalPositionInVolume();

                newMetersFromVolumeMin.x = metersFromVolumeMinX;

                if (metersFromVolumeMinY.HasValue)
                {
                    newMetersFromVolumeMin.y = metersFromVolumeMinY.Value;
                }

                if (metersFromVolumeMinZ.HasValue)
                {
                    newMetersFromVolumeMin.z = metersFromVolumeMinZ.Value;
                }

                occupant.SetLocalPositionInCabinet(newMetersFromVolumeMin, GetLocalOrientationForOccupants());
                SortOccupantsByPosition();
            }

            return canMove;
        }

        /// <summary>
        ///     Check if an occupant can be moved somewhere.
        /// </summary>
        public bool CanMoveOccupantTo(
            Occupant    occupantBeingMoved,
            Cuboid?     backingOccupancyInWorldSpace,
            float       metersFromVolumeMinX,
            float?      metersFromVolumeMinY = null,
            float?      metersFromVolumeMinZ = null)
        {
            // Handle easy fail conditions first

            bool canContainProduct =
                CanContainProduct(
                    occupantBeingMoved.productInfo,
                    occupantBeingMoved.productVisual.info,
                    occupantBeingMoved.productVisualContentInstance
                );

            if (!canContainProduct)
            {
                MelcherLog.Log("Can't move because volume cannot contain product.", severity: MelcherLogSeverity.Verbose);
                return false;
            }

            Cuboid occupantCurrentCuboid =
                occupantBeingMoved.occupiedVolume
                    .CalculateOccupancyCuboidForOccupant(occupantBeingMoved);

            occupantCurrentCuboid
                .Transform(occupantBeingMoved.occupiedVolume.GetTransform())
                .Draw(Color.white, s_LineDuration);

            Cuboid occupantProposedCuboidInDestination =
                CalculateOccupancyCuboidForOccupant(
                    occupantBeingMoved,
                    metersFromVolumeMinX,
                    metersFromVolumeMinY,
                    metersFromVolumeMinZ
                );

            OrientedBounds bounds =
                new OrientedBounds(
                    new Bounds(occupantProposedCuboidInDestination.center, occupantProposedCuboidInDestination.size),
                    m_Transform.worldToLocalMatrix
                );
            bounds.Draw(Color.blue, 3.0f);

            if (!ProFitUtility.IsCuboidContainedWithin(toBeContained: occupantProposedCuboidInDestination, toDoContaining: occupiableCuboid))
            {
                MelcherLog.Log("Can't move because occupant cuboid would not be within occupiable volume.", severity: MelcherLogSeverity.Verbose);
                return false;
            }

            bool wouldOverlapBacking =
                IsOccupantOverlappingBacking(
                    GetTransform(),
                    occupantProposedCuboidInDestination,
                    backingOccupancyInWorldSpace
                );

            if (wouldOverlapBacking)
            {
                MelcherLog.Log("Can't move because occupant would overlap backing.", severity: MelcherLogSeverity.Verbose);
                return false;
            }

            // Start at the cabinet volume and iterate all children. This gives
            // access to all occupants in the cabinet including other shelves
            // and their nested occupants.
            OccupiableVolume destinationCabinetVolume = this;

            while (destinationCabinetVolume.parentVolume)
            {
                destinationCabinetVolume = destinationCabinetVolume.parentVolume;
            }

            // Compare the proposed location against all occupants in the
            // destination
            foreach (Occupant otherOccupant in destinationCabinetVolume.occupants)
            {
                if (IsOverlapping(occupantBeingMoved, occupantProposedCuboidInDestination, otherOccupant) == VolumeOverlap.Yes)
                {
                    return false;
                }
            }

            return true;
        }

        protected VolumeOverlap IsOverlapping(Occupant occupantBeingMoved, Cuboid proposedDestinationCuboid, Occupant possibleBlockingOccupant)
        {
            MelcherLog.Log($"We are moving: {occupantBeingMoved.name}, Maybe blocked by: {possibleBlockingOccupant.name}", possibleBlockingOccupant);

            if (occupantBeingMoved == possibleBlockingOccupant)
            {
                // Cannot block itself.
                return VolumeOverlap.No;
            }

            // There are 4 possible situations depending on which of the
            // objects (if any) are shelves.
            possibleBlockingOccupant.TryGetComponent(out CabinetShelf possibleBlockerAsShelf);
            occupantBeingMoved.TryGetComponent(out CabinetShelf shelfBeingMoved);

            if (shelfBeingMoved && possibleBlockerAsShelf)
            {
                // Situation 1:
                //      Both objects are shelves.

                Cuboid movingShelfBoundaryBox =
                    shelfBeingMoved.boundingCuboid                          //< Cuboid is local to the shelf product
                        .Transform(shelfBeingMoved.GetTransform())          //< Now in world-space relative to the shelf being moved
                        .InverseTransform(occupantBeingMoved.occupiedVolume.GetTransform());  //< Now is local to the volume it currently resides in

                {
                    // The shelf's current x-position must be replaced with the
                    // proposed x-position.
                    float proposedMetersFromVolumeMinX = proposedDestinationCuboid.position.x;

                    Vector3 shelfPosition = movingShelfBoundaryBox.position;
                    shelfPosition.x = proposedMetersFromVolumeMinX;
                    movingShelfBoundaryBox.position = shelfPosition;
                }

                {
                    Cuboid c = movingShelfBoundaryBox.Transform(GetTransform());
                    c.Draw(Color.grey, s_LineDuration, offset: 1);
                    Debug.DrawLine(Vector3.zero, c.min, Color.Lerp(Color.grey, Color.black, 0.2f));
                }

                // Do a broad-phase overlap test against the stationary shelf's
                // bounding box. The shelf's bounding box must be transformed
                // from its product-local cuboid to one that is local to the
                // calling volume.
                Cuboid stationaryShelfBoundingBox =
                    possibleBlockerAsShelf.boundingCuboid                   //< Cuboid is local to the shelf product
                        .Transform(possibleBlockerAsShelf.GetTransform())   //< Now in world-space
                        .InverseTransform(GetTransform());                  //< Now is local to the calling volume (the volume the occupant being moved is attempting to occupy)

                stationaryShelfBoundingBox.Transform(GetTransform()).Draw(Color.grey, s_LineDuration);

                if (!Cuboid.Overlaps(movingShelfBoundaryBox, stationaryShelfBoundingBox))
                {
                    // Early exit. Broad-phase test showed no overlap so none
                    // of its occupants overlap.
                    return VolumeOverlap.No;
                }

                Cuboid blockerOccupantCuboid =
                    TransformOccupancyCuboidBetweenVolumes(possibleBlockingOccupant, this);

                // Compare the moving shelf's occupied volume to the stationary
                // object.
                if (Cuboid.Overlaps(proposedDestinationCuboid, blockerOccupantCuboid))
                {
                    proposedDestinationCuboid.Transform(GetTransform()).Draw(Color.magenta, s_LineDuration, drawX: true, offset: 1);

                    blockerOccupantCuboid.Transform(GetTransform()).Draw(Color.red, s_LineDuration, drawX: true, offset: 1);

                    return VolumeOverlap.Yes;
                }

                Vector3 offsetOfShelfPositionToCabinetPosition =
                    new Vector3(
                        x: proposedDestinationCuboid.position.x,
                        y: resizerCuboid.height,
                        z: 0.0f
                    );

                int nestedMovingCount = shelfBeingMoved.occupants.Count;
                int nestedStationaryCount = possibleBlockerAsShelf.occupants.Count;

                for (int i = 0; i < nestedMovingCount; ++i)
                {
                    Occupant nestedMovingOccupant = shelfBeingMoved.occupants[i];

                    Cuboid nestedMovingOccupantCuboid =
                        shelfBeingMoved.CalculateOccupancyCuboidForOccupant(nestedMovingOccupant);

                    nestedMovingOccupantCuboid.position +=
                        offsetOfShelfPositionToCabinetPosition;

                    {
                        Cuboid c =
                            nestedMovingOccupantCuboid.Transform(GetTransform());
                        c.Draw(Color.magenta, s_LineDuration);
                        Debug.DrawLine(Vector3.zero, c.min, Color.magenta, s_LineDuration);
                    }

                    // Compare against the stationary shelf's cut-out
                    if (Cuboid.Overlaps(nestedMovingOccupantCuboid, blockerOccupantCuboid))
                    {
                        nestedMovingOccupantCuboid.Transform(GetTransform()).Draw(Color.magenta, s_LineDuration, drawX: true, offset: 1);

                        blockerOccupantCuboid.Transform(GetTransform()).Draw(Color.red, s_LineDuration, drawX: true, offset: 1);

                        return VolumeOverlap.Yes;
                    }

                    for (int j = 0; j < nestedStationaryCount; ++j)
                    {
                        Occupant nestedStationaryOccupant =
                            possibleBlockerAsShelf.occupants[j];

                        // Convert the item in shelf's cuboid from being local to
                        // the shelf to being local to the current volume (the one
                        // the item is being moved into).
                        Cuboid nestedStationaryOccupantCuboid =
                            possibleBlockerAsShelf.CalculateOccupancyCuboidForOccupant(nestedStationaryOccupant)
                                .Transform(possibleBlockerAsShelf.GetTransform())
                                .InverseTransform(GetTransform());

                        Cuboid c = nestedStationaryOccupantCuboid.Transform(GetTransform());
                        Debug.DrawLine(Vector3.zero, c.min, Color.red, s_LineDuration);
                        Debug.DrawLine(Vector3.zero, c.max, Color.green, s_LineDuration);

                        // Compare against the moving shelf's cut-out
                        if (Cuboid.Overlaps(nestedStationaryOccupantCuboid, proposedDestinationCuboid))
                        {
                            proposedDestinationCuboid.Transform(GetTransform()).Draw(Color.magenta, s_LineDuration, drawX: true, offset: 1);

                            nestedStationaryOccupantCuboid.Transform(GetTransform()).Draw(Color.red, s_LineDuration, drawX: true, offset: 1);

                            return VolumeOverlap.Yes;
                        }

                        if (Cuboid.Overlaps(nestedStationaryOccupantCuboid, nestedMovingOccupantCuboid))
                        {
                            nestedMovingOccupantCuboid.Transform(GetTransform()).Draw(Color.magenta, s_LineDuration, drawX: true, offset: 1);

                            nestedStationaryOccupantCuboid.Transform(GetTransform()).Draw(Color.red, s_LineDuration, drawX: true, offset: 1);

                            return VolumeOverlap.Yes;
                        }

                        nestedStationaryOccupantCuboid.Transform(GetTransform()).Draw(Color.grey, s_LineDuration);
                    }
                }
            }
            else if (shelfBeingMoved)
            {
                // Situation 2:
                //      The moving object is a shelf but the potential
                //      blocker is not. Each occupant in the moving shelf
                //      needs to be predicted where it will land and then
                //      overlap test from there.

                Cuboid shelfBoundingBox =
                    shelfBeingMoved.boundingCuboid                          //< Cuboid is local to the shelf product
                        .Transform(shelfBeingMoved.GetTransform())          //< Now in world-space relative to the shelf being moved
                        .InverseTransform(occupantBeingMoved.occupiedVolume.GetTransform());    //< Now is local to the volume the product already occupies

                {
                    // The shelf's current x-position must be replaced with the
                    // proposed x-position.
                    float proposedMetersFromVolumeMinX = proposedDestinationCuboid.position.x;

                    Vector3 shelfPosition = shelfBoundingBox.position;
                    shelfPosition.x = proposedMetersFromVolumeMinX;
                    shelfBoundingBox.position = shelfPosition;
                }

                {
                    Cuboid c = shelfBoundingBox.Transform(GetTransform());
                    c.Draw(Color.yellow, s_LineDuration, offset: 1);
                    Debug.DrawLine(Vector3.zero, c.min, Color.yellow, s_LineDuration);
                }

                // Compare the shelf's predicted bounding box to the stationary
                // blocking object. Because the blocker may be in a different
                // volume space than the object being moved, the blocker must
                // be transformed to the same relative coordinate space as the
                // proposed destination.

                Cuboid blockerOccupantCuboid =
                    TransformOccupancyCuboidBetweenVolumes(possibleBlockingOccupant, this);

                {
                    Cuboid c = blockerOccupantCuboid.Transform(GetTransform());
                    c.Draw(Color.green, s_LineDuration, offset: 1);
                    Debug.DrawLine(Vector3.zero, c.min, Color.green, s_LineDuration);
                }

                if (!Cuboid.Overlaps(shelfBoundingBox, blockerOccupantCuboid))
                {
                    // Early exit. Broad-phase test showed no overlap so none
                    // of the occupants overlap.
                    return VolumeOverlap.No;
                }

                // Compare the moving shelf's occupied volume to the stationary
                // object.
                if (Cuboid.Overlaps(proposedDestinationCuboid, blockerOccupantCuboid))
                {
                    proposedDestinationCuboid.Transform(GetTransform()).Draw(Color.magenta, s_LineDuration, drawX: true, offset: 1);

                    blockerOccupantCuboid.Transform(GetTransform()).Draw(Color.red, s_LineDuration, drawX: true, offset: 1);

                    return VolumeOverlap.Yes;
                }

                // This is the offset needed to shift the product-in-shelf's
                // shelf-local position to a position local to this volume.
                // If this offset is not applied to the product-in-shelf's
                // cuboid, the cuboid will appears below the cabinet because
                // shelves have their origin on their top (to anchor to the
                // top of the cabinet) whereas all other cabinets have their
                // origin at the bottom.
                Vector3 offsetOfShelfPositionToCabinetPosition =
                    new Vector3(
                        x: proposedDestinationCuboid.position.x,
                        y: resizerCuboid.height,
                        z: 0.0f
                    );

                foreach (Occupant nestedOccupant in shelfBeingMoved.occupants)
                {
                    Cuboid nestedOccupantCuboid =
                        shelfBeingMoved.CalculateOccupancyCuboidForOccupant(nestedOccupant);

                    nestedOccupantCuboid.position +=
                        offsetOfShelfPositionToCabinetPosition;

                    {
                        Cuboid c =
                            nestedOccupantCuboid.Transform(GetTransform());
                        c.Draw(Color.magenta, s_LineDuration);
                        Debug.DrawLine(Vector3.zero, c.min, Color.magenta, s_LineDuration);
                    }

                    if (Cuboid.Overlaps(nestedOccupantCuboid, blockerOccupantCuboid))
                    {
                        nestedOccupantCuboid.Transform(GetTransform()).Draw(Color.magenta, s_LineDuration, drawX: true, offset: 1);

                        blockerOccupantCuboid.Transform(GetTransform()).Draw(Color.red, s_LineDuration, drawX: true, offset: 1);

                        return VolumeOverlap.Yes;
                    }
                }
            }
            else if (possibleBlockerAsShelf)
            {
                // Situation 3:
                //      The moving object is not a shelf but the potential
                //      blocker is.

                // Do a broad-phase overlap test against the shelf's bounding
                // box. The shelf's bounding box must be transformed from its
                // product-local cuboid to one that is local to the calling
                // volume.
                Cuboid shelfBoundingBox =
                    possibleBlockerAsShelf.boundingCuboid                   //< Cuboid is local to the shelf product
                        .Transform(possibleBlockerAsShelf.GetTransform())   //< Now in world-space
                        .InverseTransform(GetTransform());                  //< Now is local to the calling volume (the volume the occupant being moved is attempting to occupy)

                shelfBoundingBox.Transform(GetTransform()).Draw(Color.grey, s_LineDuration);

                if (!Cuboid.Overlaps(shelfBoundingBox, proposedDestinationCuboid))
                {
                    // Early exit. Broad-phase test showed no overlap so none
                    // of its occupants overlap.
                    return VolumeOverlap.No;
                }

                if (this != possibleBlockerAsShelf)
                {
                    // The moving object isn't being placed into this shelf.
                    // Therefore, the moving occupant cannot overlap the
                    // shelf's cut-out which would have been allowed when an
                    // object occupies a shelf.

                    // Get the stationary shelf's cut-out in the moving object's
                    // coordinate space.
                    Cuboid shelfCutOutCuboid =
                        CalculateOccupancyCuboidForOccupant(possibleBlockingOccupant);

                    shelfBoundingBox.Transform(GetTransform()).Draw(Color.grey, s_LineDuration);

                    if (Cuboid.Overlaps(shelfCutOutCuboid, proposedDestinationCuboid))
                    {
                        shelfCutOutCuboid.Transform(GetTransform()).Draw(Color.red, 1.0f, drawX: true, offset: 1);

                        return VolumeOverlap.Yes;
                    }
                }

                // Check the rest of the shelf's items
                foreach (Occupant itemInShelf in possibleBlockerAsShelf.occupants)
                {
                    if (itemInShelf == occupantBeingMoved)
                    {
                        // Cannot block itself.
                        continue;
                    }

                    // Convert the item in shelf's cuboid from being local to
                    // the shelf to being local to the current volume (the one
                    // the item is being moved into).
                    Cuboid shelfItemCuboid =
                        possibleBlockerAsShelf.CalculateOccupancyCuboidForOccupant(itemInShelf)
                            .Transform(possibleBlockerAsShelf.GetTransform())
                            .InverseTransform(GetTransform());

                    Cuboid c = shelfItemCuboid.Transform(GetTransform());
                    Debug.DrawLine(Vector3.zero, c.min, Color.red, s_LineDuration);
                    Debug.DrawLine(Vector3.zero, c.max, Color.green, s_LineDuration);

                    if (Cuboid.Overlaps(shelfItemCuboid, proposedDestinationCuboid))
                    {
                        shelfItemCuboid.Transform(GetTransform()).Draw(Color.red, s_LineDuration);

                        return VolumeOverlap.Yes;
                    }

                    shelfItemCuboid.Transform(GetTransform()).Draw(Color.grey, s_LineDuration);
                }
            }
            else
            {
                // Situation 4:
                //      Simplest case: neither object is a shelf so a simple
                //      bounds check is sufficient.

                Cuboid blockerOccupantCuboid =
                    TransformOccupancyCuboidBetweenVolumes(possibleBlockingOccupant, this);

                Cuboid c = blockerOccupantCuboid.Transform(GetTransform());
                c.Draw(Color.grey, s_LineDuration, drawX: false, offset: 0);
                Debug.DrawLine(Vector3.zero, c.min, Color.yellow, s_LineDuration);

                if (Cuboid.Overlaps(proposedDestinationCuboid, blockerOccupantCuboid))
                {
                    MelcherLog.Log("Can't move because we overlap with " + possibleBlockingOccupant.name, possibleBlockingOccupant, severity: MelcherLogSeverity.Verbose);
                    c.Draw(Color.red, s_LineDuration, drawX: true, offset: 1);
                    return VolumeOverlap.Yes;
                }
            }

            return VolumeOverlap.No;
        }

        /// <summary>
        ///     Try to move an occupant into this volume (removing it from its
        ///     current volume if necessary) at a position where it is centered
        ///     within the volume.
        /// </summary>
        /// <param name="occupant"></param>
        /// <param name="metersFromVolumeMinX"></param>
        /// <param name="metersFromVolumeMinY"></param>
        /// <returns></returns>
        public async UniTask<bool> TryAddOccupantCentered(
            Occupant occupant,
            Cuboid? backingOccupancyInWorldSpace,
            Ref<float> metersFromVolumeMinX,
            Ref<float> metersFromVolumeMinY)
        {
            Cuboid occupantOccupyCuboid =
                occupant.occupiedVolume.CalculateOccupancyCuboidForOccupant(
                    occupant);

            // Calculate the x-position where the product will be centered
            // within the cabinet.
            metersFromVolumeMinX.value =
                occupiableCuboid.position.x + (occupiableCuboid.width - occupantOccupyCuboid.width) * 0.5f;

            bool canMoveToLocation =
                CanMoveOccupantTo(
                    occupant,
                    backingOccupancyInWorldSpace,
                    metersFromVolumeMinX.value,
                    metersFromVolumeMinY.value
                );

            if (!canMoveToLocation)
            {
                return false;
            }

            await InsertOccupantAt(occupant, metersFromVolumeMinX.value, metersFromVolumeMinY.value);
            return true;
        }

        /// <summary>
        ///     Try to move an occupant into this volume (removing it from its
        ///     current volume if necessary). If the exact position requested
        ///     is blocked, search within a short distance for an open spot.
        /// </summary>
        /// <param name="occupant"></param>
        /// <param name="metersFromVolumeMinX"></param>
        /// <param name="metersFromVolumeMinY"></param>
        /// <returns>
        ///     Return <see langword="true"/> if the occupant could be added to
        ///     the volume; otherwise, return <see langword="false"/>.
        /// </returns>
        public async UniTask<bool> TryAddOccupantNear(
            Occupant    occupant,
            Cuboid?     backingOccupancyInWorldSpace,
            Ref<float>  metersFromVolumeMinX,
            Ref<float>  metersFromVolumeMinY)
        {
            bool canFitProduct =
                CanMoveOccupantNear(
                    occupant,
                    backingOccupancyInWorldSpace,
                    ref metersFromVolumeMinX.value,
                    ref metersFromVolumeMinY.value
                );

            if (!canFitProduct)
            {
                return false;
            }

            await InsertOccupantAt(
                occupant,
                metersFromVolumeMinX.value,
                volumePositionY: metersFromVolumeMinY.value
            );

            return true;
        }


        public async UniTask<bool> TryAddOccupantAt(
            Occupant    occupant,
            Cuboid?     backingOccupancyCuboidInWorldSpace,
            float       metersFromVolumeMinX)
        {
            bool canFitProduct =
                CanContainAndFitProductAt(
                    occupant.productInfo,
                    occupant.productVisual,
                    occupant.productVisualContentInstance,
                    backingOccupancyCuboidInWorldSpace,
                    metersFromVolumeMinX,
                    out VolumeOccupantResult _
                );

            if (!canFitProduct)
            {
                return false;
            }

            await InsertOccupantAt(occupant, metersFromVolumeMinX);

            return true;
        }

        /// <summary>
        /// Adds an existing product/occupant to this cabinet. If the product
        /// is added to another cabinet, this will remove it from that cabinet
        /// before adding it to this cabinet.
        /// </summary>
        public async UniTask<bool> TryAddOccupantAnywhere(Occupant occupant, Cuboid? backingOccupancyInWorldSpace)
        {
            bool canAdd =
                CanAddOccupantAnywhere(
                    occupant.productInfo,
                    occupant.productVisual,
                    occupant.productVisualContentInstance,
                    backingOccupancyInWorldSpace,
                    out float metersFromCabinetMinX,
                    out VolumeOccupantResult _
                );

            if (!canAdd)
            {
                MelcherLog.Log("Cannot add product to cabinet: " + occupant.productInfo.name, this);
                return false;
            }

            await InsertOccupantAt(occupant, metersFromCabinetMinX);

            return true;
        }

        public async UniTask<Occupant> TryInstantiateOccupantAt(
            bool                        userInitiated,
            ProductInfo                 productInfo,
            ProductVisualSO               productVisual,
            ProductVisualContent        productVisualPrefab,
            InstantiateOccupantHandler  productInstantiator,
            Cuboid?                     backingOccupancyInWorldSpace,
            float                       metersFromVolumeMinX)
        {
            bool canFitProduct =
                CanContainAndFitProductAt(
                    productInfo,
                    productVisual,
                    productVisualPrefab,
                    backingOccupancyInWorldSpace,
                    metersFromVolumeMinX,
                    out VolumeOccupantResult _
                );

            if (!canFitProduct)
            {
                return null;
            }

            return
                await InstantiateAndInsertOccupantAt(
                    userInitiated,
                    productInfo,
                    productVisual,
                    productVisualPrefab,
                    productInstantiator,
                    metersFromVolumeMinX
                );
        }

        public async UniTask<(Occupant, VolumeOccupantResult)> TryInstantiateOccupantCentered(
            ProductInfo                 productInfo,
            ProductVisualSO               productVisual,
            ProductVisualContent        productVisualPrefab,
            InstantiateOccupantHandler  productInstantiator,
            Cuboid?                     backingOccupancyInWorldSpace)
        {
            Occupant newOccupant = null;
            VolumeOccupantResult result = default;

            Cuboid occupantOccupiedCuboid =
                CalculateOccupancyCuboidForProductInfo(
                    productInfo,
                    productVisual,
                    productVisualPrefab
                );

            using (var drawScope = Cuboid.StartDrawScope())
            {
                Cuboid c = occupantOccupiedCuboid.Transform(GetTransform());
                c.Draw(Color.magenta, Color.blue, 10.0f, offset: 3, step: Units.Inches2Meters);
                Debug.DrawLine(
                    Vector3.zero,
                    c.min,
                    Color.magenta,
                    10.0f
                );
            }

            // Calculate the position of the product's left edge that will
            // cause it to be centered.
            float metersFromCabinetMinX =
                (resizerCuboid.width - occupantOccupiedCuboid.width) * 0.5f;

            bool canFit =
                CanContainAndFitProductAt(
                    productInfo,
                    productVisual,
                    productVisualPrefab,
                    backingOccupancyInWorldSpace,
                    metersFromCabinetMinX,
                    out result
                );

            if (canFit)
            {
                newOccupant =
                    await TryInstantiateOccupantAt(
                        userInitiated: true,
                        productInfo,
                        productVisual,
                        productVisualPrefab,
                        productInstantiator,
                        backingOccupancyInWorldSpace,
                        metersFromCabinetMinX
                    );

                if (!newOccupant)
                {
                    // Try to add the product to any sub volumes (cabinet plunges)
                    foreach (Occupant occupant in occupants)
                    {
                        if (!occupant.TryGetComponent(out OccupiableVolume subVolume))
                        {
                            continue;
                        }

                        (newOccupant, result) =
                            await subVolume.TryInstantiateOccupantCentered(
                                productInfo,
                                productVisual,
                                productVisualPrefab,
                                productInstantiator,
                                backingOccupancyInWorldSpace
                            );

                        if (newOccupant)
                        {
                            break;
                        }
                    }
                }
            }

            return (newOccupant, result);
        }

        /// <summary>
        /// Creates a new product/occupant on this cabinet if there is room.
        /// </summary>
        public async UniTask<(Occupant newOccupant, VolumeOccupantResult result)> TryInstantiateOccupantAnywhere(
            ProductInfo                 productInfo,
            ProductVisualSO               productVisual,
            ProductVisualContent        productVisualPrefab,
            InstantiateOccupantHandler  productInstantiator,
            Cuboid?                     backingCuboidInWorldSpace)
        {
            Occupant occupant = null;
            VolumeOccupantResult result = default;

            bool canAddAnywhere =
                CanAddOccupantAnywhere(
                    productInfo,
                    productVisual,
                    productVisualPrefab,
                    backingCuboidInWorldSpace,
                    out float metersFromCabinetMinX,
                    out result
                );

            if (!canAddAnywhere)
            {
                MelcherLog.Log("Cannot add product to cabinet: " + productInfo.name + "\nReason: " + result.status, this);
                return (occupant, result);
            }

            occupant =
                await InstantiateOccupant(
                    productInfo,
                    productVisual,
                    productVisualPrefab,
                    productInstantiator
                );

            await InsertOccupantAt(occupant, metersFromCabinetMinX);

            await EventBridge.Designer.onOccupantInstantiated.InvokeIfNonEmpty((occupant, userInitiated: true));

            return (occupant, result);
        }

        /// <summary>
        ///     Check whether a product is allowed to be added to this volume
        ///     and fits in the requested location.
        /// </summary>
        public bool CanContainAndFitProductAt(
            ProductInfo     productInfo,
            ProductVisualSO productVisual,
            ProductVisualContent productVisualPrefab,
            Cuboid?         backingOccupancyInWorldSpace,
            float           metersFromVolumeMinX,
            out VolumeOccupantResult result)
        {
            result = default;
            result.productInfo = productInfo;
            result.productVisual = productVisual;
            result.volume = this;
            result.occupantCuboidWithinVolume =
                CalculateOccupancyCuboidForProductInfo(
                    productInfo,
                    productVisual,
                    productVisualPrefab,
                    metersFromVolumeMinX
                );

            Cuboid occupantCuboid = result.occupantCuboidWithinVolume;

            if (!CanContainProduct(productInfo, productVisual.info, productVisualPrefab))
            {
                result.status = OccupantCuboidStatus.CannotContainProduct;
                return false;
            }

            bool wouldOverlapBacking =
                IsOccupantOverlappingBacking(
                    GetTransform(),
                    result.occupantCuboidWithinVolume,
                    backingOccupancyInWorldSpace
                );

            if (wouldOverlapBacking)
            {
                result.status = OccupantCuboidStatus.OverlapsBacking;
                return false;
            }

            result.status =
                ProFitUtility.CuboidOverlapCheck(
                    toBeContained: result.occupantCuboidWithinVolume,
                    toDoContaining: occupiableCuboid
                );

            if (result.status != OccupantCuboidStatus.Fits)
            {
                MelcherLog.LogEditorOnly("Product is outside of cabinet: " + result.status);
                return false;
            }

            // Check if existing occupants overlap the proposed location.
            foreach (Occupant otherOccupant in m_Occupants.readOnly)
            {
                Cuboid otherOccupantCuboid =
                    CalculateOccupancyCuboidForOccupant(
                        otherOccupant
                    );

                if (Cuboid.Overlaps(otherOccupantCuboid, occupantCuboid))
                {
                    MelcherLog.Log(
                        "Overlaps with " + otherOccupant.productInfo.name,
                        context: otherOccupant,
                        severity: MelcherLogSeverity.Verbose
                    );

                    result.status = OccupantCuboidStatus.OverlapsAnotherOccupant;
                    result.blockingOccupant = otherOccupant;
                    return false;
                }

                if (otherOccupant.TryGetComponent(out CabinetShelf shelf))
                {
                    foreach (Occupant occupantInShelf in shelf.occupants)
                    {
                        // Get the cuboid the product in the shelf occupies,
                        // relative to the main cabinet.
                        Cuboid occupantInShelfCuboid =
                            shelf.CalculateOccupancyCuboidForOccupant(occupantInShelf);

                        occupantInShelfCuboid.position +=
                            otherOccupant.GetLocalPositionInVolume();

                        if (Cuboid.Overlaps(occupantInShelfCuboid, otherOccupantCuboid))
                        {
                            result.status = OccupantCuboidStatus.OverlapsAnotherOccupant;
                            result.blockingOccupant = occupantInShelf;
                            return false;
                        }
                    }
                }
            }

            {
                using var drawScope = Cuboid.StartDrawScope();
                occupantCuboid.Transform(GetTransform()).Draw(Color.blue, 10.0f);
                occupiableCuboid.Transform(GetTransform()).Draw(Color.green, 10.0f);
            }

            return true;
        }

        /// <summary>
        ///     Check if an occupant can be added anywhere on this cabinet.
        /// </summary>
        public bool CanAddOccupantAnywhere(
            ProductInfo                 productInfo,
            ProductVisualSO               productVisual,
            ProductVisualContent        productVisualPrefab,
            Cuboid?                     backingOccupancyInWorldSpace,
            out float                   metersFromVolumeMinX,
            out VolumeOccupantResult    result)
        {
            using var _ = Cuboid.StartDrawScope();

            metersFromVolumeMinX = occupiablePaddingInMeters.left;

            result = new VolumeOccupantResult();
            result.volume = this;
            result.productInfo = productInfo;
            result.productVisual = productVisual;
            result.occupantCuboidWithinVolume =
                CalculateOccupancyCuboidForProductInfo(
                    productInfo,
                    productVisual,
                    productVisualPrefab,
                    positionInVolumeX: metersFromVolumeMinX
                );

            {
                // Visualize the occupiable region as a blue wire box.
                OrientedBounds occupiableBounds =
                    new OrientedBounds(
                        new Bounds(occupiableCuboid.center, occupiableCuboid.size),
                        m_Transform.worldToLocalMatrix
                    );
                occupiableBounds.Draw(Color.blue, 2.0f);
            }

            // Always draw the result location once (in red) to handle the case
            // where placement fails.
            Color failedPlacementColor = Color.Lerp(Color.red, Color.white, 0.5f);
            DrawOccupantCuboid(result.occupantCuboidWithinVolume, this, failedPlacementColor);

            //MelcherLog.Log("TODO: Snap the calculated metersFromCabinetMinX to the nearest inch", severity: MelcherLogSeverity.Verbose);

            if (!CanContainProduct(productInfo, productVisual.info, productVisualPrefab))
            {
                // Easy fail condition: cabinet cannot have products.
                result.status = OccupantCuboidStatus.CannotContainProduct;
                return false;
            }

            // Ensure the product is compatible with the current backing.

            bool wouldOverlapBacking =
                IsOccupantOverlappingBacking(
                    GetTransform(),
                    result.occupantCuboidWithinVolume,
                    backingOccupancyInWorldSpace
                );

            if (wouldOverlapBacking)
            {
                result.status = OccupantCuboidStatus.OverlapsBacking;
                return false;
            }

            result.status =
                ProFitUtility.CuboidOverlapCheck(
                    toBeContained: result.occupantCuboidWithinVolume,
                    toDoContaining: occupiableCuboid
                );

            if (result.status != OccupantCuboidStatus.Fits)
            {
                MelcherLog.LogEditorOnly("Occupant is outside of occupiable cuboid");

                // If an occupant doesn't fit, draw the region that is outside
                // the occupiable area in red to better indicate where the
                // overlap is failing.
                Cuboid outOfBoundsRegion = result.occupantCuboidWithinVolume;

                switch (result.status)
                {
                    case OccupantCuboidStatus.TooFarLeft:
                        outOfBoundsRegion.xMax = occupiableCuboid.xMin;
                        break;

                    case OccupantCuboidStatus.TooFarRight:
                        outOfBoundsRegion.xMin = occupiableCuboid.xMax;
                        break;

                    case OccupantCuboidStatus.TooFarUp:
                        outOfBoundsRegion.yMin = occupiableCuboid.yMax;
                        break;

                    case OccupantCuboidStatus.TooFarDown:
                        outOfBoundsRegion.yMax = occupiableCuboid.yMin;
                        break;

                    case OccupantCuboidStatus.TooFarBack:
                        outOfBoundsRegion.zMin = occupiableCuboid.zMax;
                        break;

                    case OccupantCuboidStatus.TooFarForward:
                        outOfBoundsRegion.zMax = occupiableCuboid.zMin;
                        break;

                    default:
                        MelcherLog.LogErrorEditorOnly("No visualization handled for overlap check status: " + result.status);
                        break;
                }

                OrientedBounds outOfBoundsRegionToRender =
                    new OrientedBounds(
                        new Bounds(outOfBoundsRegion.center, outOfBoundsRegion.size),
                        m_Transform.worldToLocalMatrix
                    );
                outOfBoundsRegionToRender.Draw(Color.red, duration: 2.0f);

                return false;
            }

            // Check if any existing occupants overlap the horizontal slice
            // of the volume where this product would occupy: Calculate a
            // cuboid that has the size of the product but whose width is
            // extended to the length of the volume. This stretched cuboid is
            // used to find any products that may block it

            Cuboid stretchedCuboid = result.occupantCuboidWithinVolume;
            stretchedCuboid.width = occupiableCuboid.size.x;

            float nextMetersFromVolumeMinX = metersFromVolumeMinX;

            // Compare the stretched cuboid against all occupants
            foreach (Occupant otherOccupant in m_Occupants.readOnly)
            {
                MelcherLog.Breadcrumb("-- Checking against occupant: " + otherOccupant, otherOccupant);

                // Set the search position to the right of the last product
                // checked. On the first pass, we haven't checked a product yet
                // and this starts the search from the volume's left edge.
                metersFromVolumeMinX = nextMetersFromVolumeMinX;

                float nearestOccupantMinX = occupiableCuboid.xMax;

                // Draw the proposed position in red to indicate where the
                // insertion is being attempted. Later, if the location turns
                // out to be valid, the cuboid is redrawn in green.
                {
                    Cuboid adjustedCuboid = result.occupantCuboidWithinVolume;
                    adjustedCuboid.xMin = metersFromVolumeMinX;
                    adjustedCuboid.xMax = adjustedCuboid.xMin + result.occupantCuboidWithinVolume.width;
                    adjustedCuboid.position += Vector3.up * 0.01f;
                    DrawOccupantCuboid(adjustedCuboid, this, failedPlacementColor);

                    Debug.DrawLine(
                        Vector3.one * -100.0f,
                        m_Transform.TransformPoint(adjustedCuboid.position),
                        Color.red,
                        duration: 2.0f
                    );
                }

                Cuboid otherOccupantCuboid =
                    CalculateOccupancyCuboidForOccupant(
                        otherOccupant
                    );

                DrawOccupantCuboid(otherOccupantCuboid, this, Color.magenta);
                Debug.DrawLine(Vector3.one * 100.0f, m_Transform.TransformPoint(otherOccupantCuboid.position), Color.magenta, duration: 2.0f);

                bool overlapDetected = false;

                if (Cuboid.Overlaps(otherOccupantCuboid, stretchedCuboid))
                {
                    MelcherLog.Breadcrumb(
                        "Overlaps with " + otherOccupant.productInfo.name,
                        context: otherOccupant,
                        severity: MelcherLogSeverity.Verbose
                    );

                    nearestOccupantMinX =
                        Mathf.Min(
                            nearestOccupantMinX,
                            otherOccupantCuboid.xMin
                        );

                    nextMetersFromVolumeMinX =
                        Mathf.Max(
                            nextMetersFromVolumeMinX,
                            otherOccupantCuboid.xMax
                        );

                    overlapDetected = true;
                }

                if (otherOccupant.TryGetComponent(out CabinetShelf shelf))
                {
                    foreach (Occupant occupantInShelf in shelf.occupants)
                    {
                        // Get the cuboid the product in the shelf occupies,
                        // relative to the main cabinet.
                        Cuboid occupantInShelfCuboid =
                            shelf.CalculateOccupancyCuboidForOccupant(occupantInShelf);

                        occupantInShelfCuboid.position +=
                            otherOccupant.GetLocalPositionInVolume();

                        DrawOccupantCuboid(otherOccupantCuboid, this, Color.magenta);
                        Debug.DrawLine(Vector3.one * 100.0f, m_Transform.TransformPoint(otherOccupantCuboid.position), Color.magenta, duration: 2.0f);

                        if (Cuboid.Overlaps(occupantInShelfCuboid, stretchedCuboid))
                        {
                            MelcherLog.Breadcrumb(
                                "Overlaps with shelf occupant " + occupantInShelf.productInfo.name,
                                context: occupantInShelf,
                                severity: MelcherLogSeverity.Verbose
                            );

                            nearestOccupantMinX =
                                Mathf.Min(
                                    nearestOccupantMinX,
                                    occupantInShelfCuboid.xMin
                                );

                            nextMetersFromVolumeMinX =
                                Mathf.Max(
                                    nextMetersFromVolumeMinX,
                                    occupantInShelfCuboid.xMax
                                );

                            overlapDetected = true;
                        }
                    }
                }

                if (!overlapDetected)
                {
                    // The current occupant doesn't contribute to blocking and
                    // can be ignored. Check the next occupant.
                    continue;
                }

                float leadingFreeSpaceInMeters =
                    nearestOccupantMinX - metersFromVolumeMinX;

                MelcherLog.Breadcrumb($"leadingFreeSpace={leadingFreeSpaceInMeters}m vs width={result.occupantCuboidWithinVolume.width}");

                if (result.occupantCuboidWithinVolume.width <= leadingFreeSpaceInMeters)
                {
                    // The product fits in front of the nearest product(s).
                    MelcherLog.Breadcrumb("Fits in front");

                    // Draw the bounds again in green. This draws on top of the
                    // previously red bounds used to indicate the proposed location
                    // when the occupant does not fit.
                    Cuboid synchronizedCuboid = result.occupantCuboidWithinVolume;
                    synchronizedCuboid.xMin = metersFromVolumeMinX;
                    synchronizedCuboid.xMax = synchronizedCuboid.xMin + result.occupantCuboidWithinVolume.width;
                    DrawOccupantCuboid(synchronizedCuboid, this, Color.green);

                    result.occupantCuboidWithinVolume = synchronizedCuboid;
                    result.status = OccupantCuboidStatus.Fits;

                    return true;
                }
            }

            metersFromVolumeMinX = nextMetersFromVolumeMinX;

            {
                Cuboid synchronizedCuboid = result.occupantCuboidWithinVolume;
                synchronizedCuboid.xMin = metersFromVolumeMinX;
                synchronizedCuboid.xMax = synchronizedCuboid.xMin + result.occupantCuboidWithinVolume.width;
                result.occupantCuboidWithinVolume = synchronizedCuboid;

                Debug.DrawLine(
                    Vector3.one * 100.0f,
                    m_Transform.TransformPoint(synchronizedCuboid.position),
                    Color.black,
                    duration: 2.0f
                );

                synchronizedCuboid.position += Vector3.one * 0.001f;
                DrawOccupantCuboid(synchronizedCuboid, this, Color.black);
            }

            float trailingSpaceInMeters = occupiableCuboid.xMax - metersFromVolumeMinX;
            MelcherLog.Breadcrumb($"trailingFreeSpace={trailingSpaceInMeters}m vs width={result.occupantCuboidWithinVolume.width}");

            bool fitsInLeftoverSpace = result.occupantCuboidWithinVolume.width <= trailingSpaceInMeters;
            result.status = fitsInLeftoverSpace ? OccupantCuboidStatus.Fits : OccupantCuboidStatus.TooFarRight;
            MelcherLog.Breadcrumb($"fits after? {result.status}");

            DrawOccupantCuboid(result.occupantCuboidWithinVolume, this, fitsInLeftoverSpace ? Color.green : Color.black);

            return fitsInLeftoverSpace;

            static void DrawOccupantCuboid(Cuboid cuboid, OccupiableVolume volume, Color color)
            {
                OrientedBounds orientedBounds =
                    new OrientedBounds(
                        new Bounds(cuboid.center, cuboid.size),
                        volume.GetTransform().worldToLocalMatrix
                    );

                orientedBounds.Draw(color, duration: 2.0f);
            }

        }

        /// <summary>
        ///     Create a product at the requested position. This does not do any
        ///     bounds checking so products can be instantiated overlapping another
        ///     occupant or out of bounds.
        /// </summary>
        public async UniTask<Occupant> InstantiateAndInsertOccupantAt(
            bool                    userInitiated,
            ProductInfo             productInfo,
            ProductVisualSO           productVisual,
            ProductVisualContent    productVisualPrefab,
            InstantiateOccupantHandler productInstantiator,
            float                   volumePositionX,
            float?                  volumePositionY = null,
            float?                  volumePositionZ = null)
        {
            MelcherLog.Log($"Product: x:{volumePositionX} y:{volumePositionY} z:{volumePositionZ}");

            // Clone the visual. Add the product to it
            Occupant occupant =
                await InstantiateOccupant(
                    productInfo,
                    productVisual,
                    productVisualPrefab,
                    productInstantiator
                );

            await InsertOccupantAt(occupant, volumePositionX, volumePositionY, volumePositionZ);

            await EventBridge.Designer.onOccupantInstantiated.InvokeIfNonEmpty((occupant, userInitiated));

            return occupant;
        }

        /// <summary>
        ///     Create a product from its <see cref="ProductInfo" /> and <see
        ///     cref="ProductVisualContent" />. It is the caller's job to add the
        ///     occupant to the occupant list.
        /// </summary>
        protected async UniTask<Occupant> InstantiateOccupant(
            ProductInfo                 productInfo,
            ProductVisualSO  productVisual,
            ProductVisualContent         productVisualPrefab,
            InstantiateOccupantHandler  instantiateProduct)
        {
            Occupant occupant =
                await instantiateProduct.Invoke(productInfo, productVisual, productVisualPrefab);

            // Apply the product's inset to its position
            Vector3 occupantLocalPosition = occupant.GetLocalPositionInVolume();
            switch (occupant.productVisual.info.anchor)
            {
                case ProductAnchor.OnFrontAtBottom:
                    occupantLocalPosition.y = productVisual.info.initialInsetFromEdgeInMeters;
                    occupantLocalPosition.z = occupiableCuboid.zMin;
                    break;

                case ProductAnchor.OnBackAtBottom:
                    occupantLocalPosition.y = productVisual.info.initialInsetFromEdgeInMeters;
                    occupantLocalPosition.z = occupiableCuboid.zMax;
                    break;

                case ProductAnchor.OnFrontAtTop:
                    occupantLocalPosition.y = occupiableCuboid.yMax - productVisual.info.initialInsetFromEdgeInMeters;
                    occupantLocalPosition.z = occupiableCuboid.zMin;
                    break;

                case ProductAnchor.OnBackAtTop:
                    occupantLocalPosition.y = occupiableCuboid.yMax - productVisual.info.initialInsetFromEdgeInMeters;
                    occupantLocalPosition.z = occupiableCuboid.zMax;
                    break;

                case ProductAnchor.OnTopAtFront:
                    occupantLocalPosition.y = occupiableCuboid.yMax;
                    occupantLocalPosition.z = occupiableCuboid.zMin + productVisual.info.initialInsetFromEdgeInMeters;
                    break;

                case ProductAnchor.OnTopAtBack:
                    occupantLocalPosition.y = occupiableCuboid.yMax;
                    occupantLocalPosition.z = occupiableCuboid.zMax - productVisual.info.initialInsetFromEdgeInMeters;
                    break;

                default:
                    MelcherLog.LogError($"Unhandled product anchor '{productVisual.info.anchor}' for product \"{productInfo.name}\", ID {productInfo.id}", occupant);
                    break;
            }

            occupant.SetLocalPositionInCabinet(occupantLocalPosition, GetLocalOrientationForOccupants());

            return occupant;
        }


        public delegate bool FindNearestOccupantPredicate(Occupant otherOccupant);

        /// <summary>
        ///     Find the nearest occupant on a given side of a specific
        ///     occupant. This considers all occupants in the cabinet. It
        ///     checks cut-out sizes and only considers occupants that would
        ///     intersect if they were moved along the direction's axis. If no
        ///     neighbour is found, <paramref name="nearestDistance"/> will be
        ///     the distance to the edge of the cabinet.
        ///     <br /><br />
        ///
        ///     This is intended to be used by the kitchen plan screenshot
        ///     system to discover which pair of products should have a
        ///     measurement drawn between them.
        /// </summary>
        /// <param name="mainOccupant">
        ///     The occupant whose nearest neighbour will be located.
        /// </param>
        /// <param name="cabinetDirection">
        ///     The direction or side of <paramref name="mainOccupant"/> that
        ///     will be searched.
        /// </param>
        /// <param name="nearestDistance">
        ///     The distance of the nearest occupant's cut-out from <paramref
        ///     name="mainOccupant"/>.
        /// </param>
        /// <returns>
        ///     The nearest occupant in the requested direction or <see
        ///     langword="null"/> if none is found.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="cabinetDirection"/> is not a
        ///     recognized direction.
        /// </exception>
        public Occupant FindNearestOccupantInDirection(
            Occupant                        mainOccupant,
            CabinetLocalDirection           cabinetDirection,
            out float                       nearestDistance,
            FindNearestOccupantPredicate    predicate = null)
        {
            Cuboid volumeCuboid = occupiableCuboid;

            Cuboid mainOccupantCuboid =
                mainOccupant.GetCutOutCuboidInVolumeSpace();

            // This cuboid is stretched along the search direction and is used
            // to find overlaps with any other occupants in the cabinet
            Cuboid overlapCheckCuboid = mainOccupantCuboid;

            Occupant currentClosestProduct = null;
            float currentClosestDistance;   // determined according to cabinet direction
            System.Func<(Cuboid mainCuboid, Cuboid neighbourCuboid), float> getDistancePredicate;

            // Calculate where to look and how to determine which product is
            // closer depending on which cabinet direction was passed in
            switch (cabinetDirection)
            {
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(cabinetDirection));

                case CabinetLocalDirection.Up:
                    // Check the space above the occupant
                    overlapCheckCuboid.yMin = overlapCheckCuboid.yMax;
                    overlapCheckCuboid.yMax = volumeCuboid.yMax;
                    currentClosestDistance = volumeCuboid.yMax - mainOccupantCuboid.yMax;
                    getDistancePredicate = (data) =>
                    {
                        return data.neighbourCuboid.yMin - data.mainCuboid.yMax;
                    };
                    break;

                case CabinetLocalDirection.Down:
                    // Check the space below the occupant
                    overlapCheckCuboid.yMax = overlapCheckCuboid.yMin;
                    overlapCheckCuboid.yMin = volumeCuboid.yMin;
                    currentClosestDistance = mainOccupantCuboid.yMin - volumeCuboid.yMin;
                    getDistancePredicate = (data) =>
                    {
                        return data.mainCuboid.yMin - data.neighbourCuboid.yMax;
                    };
                    break;

                case CabinetLocalDirection.Left:
                    // Check the space to the left of the occupant
                    overlapCheckCuboid.xMax = overlapCheckCuboid.xMin;
                    overlapCheckCuboid.xMin = volumeCuboid.xMin;
                    currentClosestDistance = mainOccupantCuboid.xMin - volumeCuboid.xMin;
                    getDistancePredicate = (data) =>
                    {
                        return data.mainCuboid.xMin - data.neighbourCuboid.xMax;
                    };
                    break;

                case CabinetLocalDirection.Right:
                    // Check the space to the right of the occupant
                    overlapCheckCuboid.xMin = overlapCheckCuboid.xMax;
                    overlapCheckCuboid.xMax = volumeCuboid.xMax;
                    currentClosestDistance = volumeCuboid.xMax - mainOccupantCuboid.xMax;
                    getDistancePredicate = (data) =>
                    {
                        return data.neighbourCuboid.xMin - data.mainCuboid.xMax;
                    };
                    break;

                case CabinetLocalDirection.Front:
                    // Check the space in front of the occupant
                    overlapCheckCuboid.zMax = overlapCheckCuboid.zMin;
                    overlapCheckCuboid.zMin = volumeCuboid.zMin;
                    currentClosestDistance = mainOccupantCuboid.zMin - volumeCuboid.zMin;
                    getDistancePredicate = (data) =>
                    {
                        return data.mainCuboid.zMin - data.neighbourCuboid.zMax;
                    };
                    break;

                case CabinetLocalDirection.Back:
                    // Check the space back the occupant
                    overlapCheckCuboid.zMin = overlapCheckCuboid.zMax;
                    overlapCheckCuboid.zMax = volumeCuboid.zMax;
                    currentClosestDistance = volumeCuboid.zMax - mainOccupantCuboid.zMax;
                    getDistancePredicate = (data) =>
                    {
                        return data.neighbourCuboid.zMin - data.mainCuboid.zMax;
                    };
                    break;
            }

            // Start at the outermost volume so the search can spill into any
            // nested volumes and therefore compare against *all* occupants.
            OccupiableVolume cabinetVolume = this;
            if (cabinetVolume.parentVolume != null)
            {
                cabinetVolume = cabinetVolume.parentVolume;
            }

            foreach (Occupant otherOccupant in cabinetVolume.occupants)
            {
                if (otherOccupant == mainOccupant)
                {
                    // Skip ourself.
                    // TODO: This means a shelf can never find its occupants.
                    //       Is this fine?
                    continue;
                }

                if (predicate != null && !predicate.Invoke(otherOccupant))
                {
                    // The user-provided predicate rejected this occupant.
                    continue;
                }

                otherOccupant.TryGetComponent(out OccupiableVolume otherOccupantAsShelf);

                if (otherOccupantAsShelf == null || otherOccupantAsShelf != mainOccupant.occupiedVolume)
                {
                    // We can safely compare against this occupant because its
                    // not a shelf `mainOccupant` occupies. We don't want to
                    // compare occupants against the shelf they reside in
                    // because ...

                    Cuboid otherOccupantCuboid =
                        otherOccupant.GetCutOutCuboidInVolumeSpace();

                    if (Cuboid.Overlaps(otherOccupantCuboid, overlapCheckCuboid))
                    {
                        float distanceInMeters = getDistancePredicate((mainOccupantCuboid, otherOccupantCuboid));
                        if (distanceInMeters < currentClosestDistance)
                        {
                            currentClosestDistance = distanceInMeters;
                            currentClosestProduct = otherOccupant;
                        }
                    }
                }

                if (otherOccupantAsShelf != null)
                {
                    // Compare against the other occupants in the shelf.

                    foreach (Occupant nestedOccupant in otherOccupantAsShelf.occupants)
                    {
                        if (nestedOccupant == mainOccupant)
                        {
                            // Skip ourself. We're looking for other occupants
                            // that are close to us, not ourself.
                            continue;
                        }

                        if (predicate != null && !predicate.Invoke(nestedOccupant))
                        {
                            // The user-provided predicate rejected this occupant.
                            continue;
                        }

                        Cuboid nestedOccupantCuboid =
                            TransformOccupancyCuboidBetweenVolumes(nestedOccupant, toVolume: this);

                        if (Cuboid.Overlaps(nestedOccupantCuboid, overlapCheckCuboid))
                        {
                            float distanceInMeters = getDistancePredicate((mainOccupantCuboid, nestedOccupantCuboid));
                            if (distanceInMeters < currentClosestDistance)
                            {
                                currentClosestDistance = distanceInMeters;
                                currentClosestProduct = nestedOccupant;
                            }
                        }
                    }
                }
            }

            nearestDistance = currentClosestDistance;

            return currentClosestProduct;
        }

        /// <summary>
        ///     Insert the product as an occupant of this cabinet (removing the
        ///     product from its previous cabinet). This function does no error
        ///     checking regarding out-of-bounds or overlapping.
        /// </summary>
        protected async UniTask InsertOccupantAt(
            Occupant occupant,
            float volumePositionX,
            float? volumePositionY = null,
            float? volumePositionZ = null)
        {
            // Remove the product from its previous volume.
            OccupiableVolume currentVolume = occupant.occupiedVolume;

            if (currentVolume && !await currentVolume.TryRemoveOccupant(occupant))
            {
                MelcherLog.LogError(
                    "Failed to remove occupant from previous volume.",
                    occupant
                );
                return;
            }

            // Set the product as a child of this cabinet
            occupant.SetCabinetVolume(cabinetVolume: this, volumePositionX, volumePositionY, volumePositionZ);
            occupant.SetCutOutSizeInMeters(
                occupant.productVisualContentInstance.GetCurrentCutOutSizeInMeters(occupant.productInfo.cutOutSizeInMeters)
            );
            m_Occupants.mutable.Add(occupant);
            SortOccupantsByPosition();

            await OnOccupantAdded(occupant);

            // Let the kitchen handle cabinet resizing and neighbour changes
            await EventBridge.Designer.onOccupantAddedToCabinet.InvokeIfNonEmpty(occupant);
        }

        protected virtual async UniTask OnOccupantAdded(Occupant addedOccupant)
        {
            await UniTask.CompletedTask;
        }

        protected virtual async UniTask OnOccupantRemoved(Occupant removedOccupant)
        {
            await UniTask.CompletedTask;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Tooltip("A transform that represents the local orientation of " +
            "occupants in the volume. Importantly, occupants and the " +
            "occupant orientation transform are intended to be direct " +
            "children of the occupiable volume. That is, the occupant " +
            "orientation transform is intended to have no child transforms.")]
        [SerializeField]
        protected Transform m_OccupantOrientation = default;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Transform m_Transform = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected MelcherReadWriteList<Occupant> m_Occupants = new List<Occupant>();

        /// <summary>
        ///     The amount of empty space on all edges of the cabinet where
        ///     occupants are prevented from occupying.
        /// </summary>
        protected CuboidPadding m_OccupiablePaddingInMeters;

        // ====================================================================
        // Static fields
        // ====================================================================

        /// <summary>
        ///     A shared list used when calculating whether a volume can be
        ///     resized without overlapping its occupants.
        /// </summary>
        protected static List<ReanchoredOccupant> s_VolumeResizeTestCuboids = new List<ReanchoredOccupant>();

        protected const float s_LineDuration = 2.0f;

        // ====================================================================
        // Nested types
        // ====================================================================

        public delegate UniTask<Occupant> InstantiateOccupantHandler(
            ProductInfo info,
            ProductVisualSO productVisual,
            ProductVisualContent productVisualPrefab
        );

        protected readonly struct ReanchoredOccupant
        {
            public ReanchoredOccupant(Occupant occupant, Cuboid cuboid)
            {
                this.occupant = occupant;
                this.cuboid = cuboid;
            }

            public readonly Occupant occupant;
            public readonly Cuboid cuboid;

        }   // readonly struct ReanchoredOccupant


    }   // class OccupiableVolume

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-05-16
/// ===========================================================================
