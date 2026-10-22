namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    ///     This script is attached to products that are cabinet shelves. I.e.,
    ///     the dropped countertop section where power burners are often
    ///     nested. (I could not find an official name for this cabinet
    ///     feature.) This script manages the shelf's selection effects, the
    ///     resize widgets and the destruction of nested products.
    /// </summary>
    // TODO: Rename this to CabinetPlunge.
    public class CabinetShelf : OccupiableVolume
        , IOccupantListener
        , IOccupantCutOutSizeChangedHandler
        , IOccupantHighlightChangedHandler
        , IProductVisualModifyOccupiedCuboidHandler
    {
        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        ///     Get the <see cref="Occupant"/> component that belongs to this
        ///     shelf.
        /// </summary>
        public Occupant occupant => GetComponent<Occupant>();

        public bool isResizable
        {
            get
            {
                Kitchen kitchen = Kitchen.instance;

                // For Stoll, the stainless steel burner plunge need to be
                // resizable it contains no products. Once they have a burner,
                // their size is adjusted and the volume cannot be resized
                // until the burner is removed.
                if (kitchen != null &&
                    kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections &&
                    m_FitWidthToOccupant &&
                    m_Occupants.readOnly.Count > 0) // At least one product
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        ///     Return a cuboid that can be used for positioning the resize
        ///     widgets at the shelf's exact corners. This also doubles as the
        ///     cut-out size of the cuboid. The cuboid is in the occupant's
        ///     local space.
        /// </summary>
        public override Cuboid resizerCuboid
        {
            get
            {
                Vector3 cutOutSizeInMeters =
                    occupant.currentCutOutSizeInMeters;

                Vector3 cutOutOffsetInMeters =
                    new Vector3(
                        Const.OccupantMinimumHorizontalPaddingInMeters * 0.5f,
                        // Move the cuboid down the y-axis by its own height to
                        // make its top flush with the top of the countertop.
                        -cutOutSizeInMeters.y,
                        0.0f
                    );

                return
                    new Cuboid(cutOutOffsetInMeters, cutOutSizeInMeters);
            }
        }


        /// <summary>
        ///     Return a cuboid in the shelf's local space that represents
        ///     the cut-out part of the shelf. This ensures the resizer widgets
        ///     are properly offset to account for the horizontal padding of
        ///     the occupant.
        /// </summary>
        public override Cuboid occupiableCuboid
        {
            get
            {
                Occupant shelf = occupant;

                // This implementation assumes shelves are only ever mounted on
                // top.
                MelcherLog.Assert(shelf.productVisual.info.anchor.IsOnTop());

                Vector3 cutOutSizeInMeters = shelf.currentCutOutSizeInMeters;

                Vector3 cabinetSizeInMeters = shelf.cabinet.sizeInMeters;

                Vector3 occupiableSize =
                    new Vector3(
                        cutOutSizeInMeters.x,
                        cabinetSizeInMeters.y - cutOutSizeInMeters.y,
                        cutOutSizeInMeters.z
                    );

                // Some kitchen catalogs do not enforce product padding. To be
                // accurate, the cabinet shelf must inherit the runtime padding
                // from its product info.
                Vector3 occupiableVolumeOffset =
                    new Vector3(
                        occupant.productInfo.productPaddingInMeters.left,
                        -cabinetSizeInMeters.y,
                        0.0f
                    );

                return new Cuboid(occupiableVolumeOffset, occupiableSize);
            }
        }

        public override OccupiableVolume parentVolume => occupant.occupiedVolume;

        /// <summary>
        ///     Return a cached bounding box that encapsulates the shelf's
        ///     cut-out size and all of it's occupants. It is intended to be
        ///     used during broad-phase overlap checks. This cuboid is local
        ///     to the shelf product itself and not the cabinet volume it is
        ///     occupying.
        /// </summary>
        public Cuboid boundingCuboid => m_BoundingCuboid;

        /// <summary>
        ///     Get the nested countertop's cuboid in the shelf's local space.
        /// </summary>
        public Cuboid countertopCuboid =>
            new Cuboid(m_NestedCountertop.offsetInMeters, m_NestedCountertop.sizeInMeters);


        // =====================================================================
        // Methods
        // =====================================================================

        public override bool CanContainProduct(ProductInfo productInfo, ProductVisualInfo productVisualInfo, ProductVisualContent productVisualContent)
        {
            // Limit shelves to containing only burners.
            return productInfo.category == ProductCategory.Burner;
        }

        public override bool TryToResizeLengthAsUser(ref float lengthMetersDelta, Extent extent)
        {
            Occupant shelf = occupant;

            Cabinet cabinet = (Cabinet)shelf.cabinet;

            Cuboid shelfOccupiedCabinetSpace =
                cabinet.CalculateOccupancyCuboidForOccupant(shelf);

            bool autoCenterProducts =
                cabinet.kitchen.AreProductsForcedToCenterOfCabinets(cabinet);

            if (lengthMetersDelta < 0.0f && IsAnyOccupantBlockingShrink(ref lengthMetersDelta, extent, autoCenterProducts))
            {
                // Can't become smaller if occupants are blocking it.
                lengthMetersDelta = 0.0f;
                return false;
            }

            Vector3 oldSizeInMeters = occupant.currentCutOutSizeInMeters;

            // Determine how large the shelf could become if the current
            // extent being resized was maxed out. The calculated value will be
            // used to prevent the shelf from expanding into other occupants on
            // the cabinet and to prevent it from extending outside of the
            // cabinet's volume.

            float availableSpaceInMetersOnLeft = 0.0f;
            float availableSpaceInMetersOnRight = 0.0f;

            if (autoCenterProducts || extent == Extent.Min)
            {
                availableSpaceInMetersOnLeft =
                    GetAvailableResizeSpaceInDirection(
                        cabinet,
                        shelf,
                        CabinetLocalDirection.Left
                    );
            }

            if (autoCenterProducts || extent == Extent.Max)
            {
                availableSpaceInMetersOnRight =
                    GetAvailableResizeSpaceInDirection(
                        cabinet,
                        shelf,
                        CabinetLocalDirection.Right
                    );
            }

            float maximumWidthInMeters =
                oldSizeInMeters.x + availableSpaceInMetersOnLeft + availableSpaceInMetersOnRight;

            // The minimum size of a cabinet shelf in meters.
            float minimumWidthInMeters = Units.Inches2Meters * m_MinimumWidthInInches;

            Vector3 newSizeInMeters = oldSizeInMeters;
            newSizeInMeters.x =
                Mathf.Clamp(
                    value: newSizeInMeters.x + lengthMetersDelta,
                    min: minimumWidthInMeters,
                    max: maximumWidthInMeters
                );

            lengthMetersDelta = newSizeInMeters.x - oldSizeInMeters.x;

            if (Mathf.Approximately(lengthMetersDelta, 0.0f))
            {
                // It's already at its min or max size.
                return false;
            }

            occupant.SetCutOutSizeInMeters(newSizeInMeters);

            if (autoCenterProducts)
            {
                // Center the shelf within its cabinet.
                Cuboid shelfOccupancyCuboid =
                    parentVolume.CalculateOccupancyCuboidForOccupant(shelf);

                shelf.metersFromVolumeMinX =
                    (parentVolume.resizerCuboid.width - shelfOccupancyCuboid.width) * 0.5f;

                // TODO: Was hoping to prevent OccupiableVolume's from knowing
                //       anything about CabinetSpan. But here, we need to know
                //       about the backing which is something only the span has
                //       access to. -Ryan
                CabinetSpan span =
                    occupant.cabinet.kitchen.layout.FindCabinetSpan(
                        (Cabinet)occupant.cabinet
                    );

                Cuboid? backingOccupancyInWorldSpace = null;

                if (span.backing)
                {
                    backingOccupancyInWorldSpace =
                        span.GetBackingOccupancyCuboidInWorldSpace(
                            span.backing.info,
                            span.backing
                        );
                }

                // Center the shelf's products.

                int occupantCount = m_Occupants.readOnly.Count;
                for (int i = 0; i < occupantCount; ++i)
                {
                    Occupant occupant = m_Occupants.readOnly[i];

                    Cuboid occupantOccupancyCuboid =
                        CalculateOccupancyCuboidForOccupant(occupant);

                    bool didCenterOccupant =
                        TryMoveOccupantTo(
                            occupant,
                            backingOccupancyInWorldSpace,
                            occupiableCuboid.position.x + (occupiableCuboid.width - occupantOccupancyCuboid.width) * 0.5f
                        );

                    if (!didCenterOccupant)
                    {
                        MelcherLog.LogError(
                            "Occupant could not be recentered after resize", context: occupant
                        );
                    }
                    else if (occupant.metersFromVolumeMinX < 0.0f ||
                        occupant.metersFromVolumeMinX > occupiableCuboid.width - occupantOccupancyCuboid.width)
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
                // To resize the cuboid from the minimum-extent, its position
                // must be counter-offset by the amount it resized.
                occupant.metersFromVolumeMinX -= lengthMetersDelta;

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

            /// <summary>
            ///     Nested function for calculating how much space is available
            ///     on a given side of a cabinet shelf until the next occupant
            ///     is hit or until the containing cabinet's edge is hit.
            /// </summary>
            static float GetAvailableResizeSpaceInDirection(Cabinet cabinet, Occupant shelf, CabinetLocalDirection expandInDirection)
            {
                bool didHitOccupant =
                    cabinet.FindNearestOccupantInDirection(
                        shelf,
                        expandInDirection,
                        out float availableSpaceInMeters
                    );

                if (didHitOccupant)
                {
                    // Hit another product. We must account for the minimum
                    // spacing between both occupants.
                    availableSpaceInMeters -= Const.OccupantMinimumHorizontalPaddingInMeters;
                }
                else
                {
                    // Hit the edge of the cabinet. We only need to account for
                    // the minimum horizontal spacing for one product (the
                    // shelf).
                    availableSpaceInMeters -= Const.OccupantMinimumHorizontalPaddingInMeters * 0.5f;
                }

                return availableSpaceInMeters;
            }
        }

        public override bool DoesApplyHorizontalPadding(ProductInfo productInfo)
        {
            if (occupant.cabinet.kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections &&
                m_FitWidthToOccupant)
            {
                return false;
            }

            return base.DoesApplyHorizontalPadding(productInfo);
        }

        public void RefreshCountertopSize()
        {
            Occupant shelf = occupant;

            IKitchen kitchen =
                shelf.cabinet.kitchen;

            // Fix the countertop cut-out. By default, it is set to a height of
            // 0 inches because the visual has no knowledge of the kitchen.
            // Calling this forces it to use the correct value.
            shelf.RefreshCutOuts();

            Vector3 cutOutSizeInMeters = shelf.currentCutOutSizeInMeters;

            Cuboid countertopCuboid =
                new Cuboid()
                {
                    position = new (
                        x: occupant.productInfo.productPaddingInMeters.left,
                        y: -cutOutSizeInMeters.y,
                        z: 0.0f
                    ),
                    size = cutOutSizeInMeters
                };

            countertopCuboid.height = 0.0f;

            if (m_BottomShape.value == BottomShape.Flat)
            {
                // Use a non-zero height to prevent zero length mesh normals
                // which would cause rendering issues.
                countertopCuboid.yMin -= 0.001f;
            }
            else if (m_BottomShape.value == BottomShape.Countertop)
            {
                // Match kitchen's countertop thickness and apply the
                // countertop overlap.
                countertopCuboid.yMin -= kitchen.countertopThicknessInMeters;
                countertopCuboid.zMin -= kitchen.baseCountertopOverhangInMeters.towardMinZ;
            }
            else
            {
                MelcherLog.LogError($"Unhandled cabinet shelf bottom shape: {m_BottomShape.value}", context: this);
            }

            m_NestedCountertop.sizeInMeters = countertopCuboid.size;
            m_NestedCountertop.offsetInMeters = countertopCuboid.position;

            occupant.productVisualContentInstance.FitColliderToModel(occupant.productInfo.cutOutSizeInMeters);
        }

        /// <summary>
        ///     Calculate the bounding box that encapsulates the shelf's
        ///     cut-out and all of its children. This bounding box can be
        ///     used during broad-phase overlap checks with other occupants.
        /// </summary>
        protected void RecalculateBoundingBox()
        {
            Occupant myOccupant = occupant;

            // Get the volume that just the shelf (not any of its occupants)
            // occupies in its cabinet.
            Cuboid occupiedSpaceInVolume =
                myOccupant.occupiedVolume.CalculateOccupancyCuboidForOccupant(myOccupant);

            // Make `occupiedSpaceInVolume` relative to the shelf itself. This
            // needs to use the negative height of the volume as the y-position
            // to correctly anchor the cuboid similar to the shelf's cut-out.
            m_BoundingCuboid = occupiedSpaceInVolume;
            m_BoundingCuboid.position = new Vector3(0.0f, -m_BoundingCuboid.height, 0.0f);

            foreach (Occupant occupant in occupants)
            {
                Cuboid occupantCuboid =
                    CalculateOccupancyCuboidForOccupant(occupant);

                m_BoundingCuboid = m_BoundingCuboid.Encapsulate(occupantCuboid);
            }

            // DEBUG: Drawing the cuboid to be sure it's local to this product.
            //m_BoundingCuboid.Transform(GetTransform()).Draw(Color.yellow, 4.0f);
        }

        protected override async UniTask OnOccupantAdded(Occupant addedOccupant)
        {
            // Setup the occupant to render its cut-out faces using a different
            // stencil layer to better nest with the shelf's cut-out.
            addedOccupant.SetIsNested(true);

            if (m_FitWidthToOccupant)
            {
                // This shelf resizes to its occupant's size.

                Cuboid occupantCuboid =
                    CalculateOccupancyCuboidForOccupant(addedOccupant);

                // First the shelf's depth
                Vector3 shelfDesiredCutOutSize = occupant.currentCutOutSizeInMeters;

                shelfDesiredCutOutSize.z =
                    occupantCuboid.depth + (m_FitWidthToOccupantPaddingInInches * Units.Inches2Meters);

                occupant.SetCutOutSizeInMeters(
                    shelfDesiredCutOutSize
                );

                // Next the shelf's width
                float desiredWidthInMeters =
                    occupantCuboid.width  + (m_FitWidthToOccupantPaddingInInches * Units.Inches2Meters * 2.0f);

                float currentWidthInMeters = occupiableCuboid.width;

                float deltaWidthInMeters = desiredWidthInMeters - currentWidthInMeters;

                TryToResizeLengthAsUser(ref deltaWidthInMeters, Extent.Max);

                // Force the parent cabinet to resize to contain the shelf.
                // (I.e., apply Stoll's minimum unoccupied sizes.)
                await EventBridge.Designer.onOccupantAddedToCabinet.InvokeIfNonEmpty(occupant);
            }

            RecalculateBoundingBox();
        }

        protected override async UniTask OnOccupantRemoved(Occupant removedOccupant)
        {
            removedOccupant.SetIsNested(false);

            // Recover the shelf's original cut-out size.
            {
                Vector3 shelfDesiredCutOutSize =
                    occupant.currentCutOutSizeInMeters;

                shelfDesiredCutOutSize.z = occupant.productInfo.cutOutSizeInMeters.z;

                occupant.SetCutOutSizeInMeters(
                    shelfDesiredCutOutSize
                );
            }

            RecalculateBoundingBox();

            await UniTask.CompletedTask;
        }

        // ====================================================================
        // IOccupantListener
        // ====================================================================
        #region IOccupantListener

        public void OnAddedToCabinet(ICabinet cabinet)
        {
            RefreshCountertopSize();

            RecalculateBoundingBox();
        }

        /// <summary>
        ///     Synchronize the kitchen's finish to the shelf's nested
        ///     countertop. The countertop section is not part of the regular
        ///     cut-out system (it's additive whereas the cut-outs are
        ///     subtractive) and thus needs to be handled manually.
        /// </summary>
        /// <param name="cabinetMaterial">
        /// </param>
        /// <param name="countertopMaterial">
        /// </param>
        public async UniTask OnCabinetMaterialsChanged(Material cabinetMaterial, Material countertopMaterial)
        {
            MelcherLog.Log("Setting shelf's countertop material to " + countertopMaterial, context: countertopMaterial, severity: MelcherLogSeverity.Verbose);

            if (m_MaterialOverride)
            {
                // Some shelves (Stainless Steel Burner Plunge Kit) have a
                // custom material that overrides the normal finishes.
                cabinetMaterial = m_MaterialOverride;
                countertopMaterial = m_MaterialOverride;
            }

            // The client requested that cabinet plunges use the countertop
            // material.
            await occupant.productVisualContentInstance.SetCabinetMaterials(
                countertopMaterial,
                countertopMaterial,
                await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()

            );

            Utility.RendererUtility.SetAllRendererMaterials(
                m_NestedCountertop.GetComponent<Renderer>(),
                countertopMaterial
            );
        }

        public void OnFitToCabinet(ICabinet cabinet)
        {
        }

        public void OnRemovedFromCabinet(ICabinet cabinet)
        {
        }

        #endregion  // IOccupantListener

        // ====================================================================
        // IOccupantCutOutChangedHandler
        // ====================================================================
        #region IOccupantCutOutChangedHandler

        /// <summary>
        ///     Set the size of the shelf's nested countertop section to match
        ///     the size of the shelf's cut-out.
        /// </summary>
        public void OnOccupantCutOutSizeChanged(Vector3 newCutOutSizeInMeters)
        {
            Occupant shelf = occupant;

            if (shelf.cabinet == null || shelf.cabinet.kitchen == null)
            {
                // We need a reference to the kitchen to know how to size the
                // nested countertop overhang and thickness.
                return;
            }

            RefreshCountertopSize();

            RecalculateBoundingBox();
        }

        #endregion  // IOccupantCutOutChangedHandler


        // ====================================================================
        // IOccupantHighlightChangedHandler
        // ====================================================================
        #region IOccupantHighlightChangedHandler

        /// <summary>
        ///     Highlight the interior faces of the cut-out. Normally, only a
        ///     product's model highlights and the interior faces stay looking
        ///     like the rest of the kitchen.
        /// </summary>
        /// <param name="cabinet"></param>
        /// <param name="highlightMaterial"></param>
        public async UniTask OnOccupantHighlightChanged(ICabinet cabinet, Material highlightMaterial)
        {
            bool isHighlighted = highlightMaterial != null;

            Material cabinetMaterial = highlightMaterial;
            Material countertopMaterial = highlightMaterial;

            if (!isHighlighted)
            {
                // Use the original kitchen materials to remove the highlight.
                (cabinetMaterial, countertopMaterial) =
                    await occupant.cabinet.kitchen.GetFinishMaterials(allowHighlightToReturn: false);

                if (m_MaterialOverride)
                {
                    // Some shelves (Stainless Steel Burner Plunge Kit) have a
                    // custom material that overrides the normal finishes.
                    cabinetMaterial = m_MaterialOverride;
                    countertopMaterial = m_MaterialOverride;
                }

                // The client requested that cabinet plunges use the countertop
                // material.
                cabinetMaterial = countertopMaterial;
            }

            await occupant.productVisualContentInstance.SetCabinetMaterials(
                cabinetMaterial,
                countertopMaterial,
                await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
            );

            Utility.RendererUtility.SetAllRendererMaterials(
                m_NestedCountertop.GetComponent<Renderer>(),
                countertopMaterial
            );
        }

        #endregion  // IOccupantHighlightChangedHandler

        // =====================================================================
        // IProductVisualModifyOccupiedCuboidHandler
        // =====================================================================
        #region IProductVisualModifyOccupiedCuboidHandler

        /// <summary>
        ///     This method is called whenever a <see cref="Cabinet"/> attempts
        ///     to calculate the cuboid that a <see cref="CabinetShelf"/>
        ///     occupies.
        ///     <br /><br />
        ///
        ///     This method modifies the shelf's occupied cuboid so that it
        ///     also encompasses the thickness of the nested countertop.
        /// </summary>
        /// <param name="occupiedCuboid"></param>
        /// <param name="volumeDoingTesting"></param>
        public void OnModifyOccupiedCuboid(ref Cuboid occupiedCuboid, OccupiableVolume volumeDoingTesting)
        {
            Kitchen kitchen = Kitchen.instance;

            occupiedCuboid.yMin -= kitchen.countertopThicknessInMeters;

            // Add a bit of padding beneath the countertop.
            occupiedCuboid.yMin -= 2.0f * Units.Inches2Meters;

            // TODO: The following line should get used whenever we add
            //       mandatory vertical padding between products. -Ryan
            // TODO: Or maybe this isn't necessary. The 2" from the above line
            //       will be more than the 1.5" of the minimum vertical
            //       padding. -Ryan
            //occupiedCuboid.yMin -= Const.OccupantMinimumVerticalPaddingInMeters;
        }

        #endregion  // IProductVisualModifyOccupiedCuboidHandler

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Tooltip("The minimum width the shelf is allowed to resize to in " +
            "inches.")]
        [SerializeField]
        protected float m_MinimumWidthInInches = 24.0f;

        [Tooltip("An optional material that will be applied to the " +
            "countertop and inside faces of the shelf.")]
        [SerializeField]
        protected Material m_MaterialOverride = default;

        [SerializeField]
        protected MelcherStableEnum<BottomShape> m_BottomShape = new (BottomShape.Countertop);

        [SerializeField]
        protected TransformHelper m_NestedCountertop = default;

        [Tooltip("Whether the plunge should shrink its width to match the " +
            "occupant added to it.")]
        [SerializeField]
        protected bool m_FitWidthToOccupant = default;

        [Tooltip("The length in inches to pad on the left and right of " +
            "the plunge when Fit Width To Occupant is enabled. E.g., 3\" " +
            "will preserve 3\" on the left and 3\" on the right.")]
        [SerializeField]
        protected float m_FitWidthToOccupantPaddingInInches = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        ///     A cuboid that represents a bounding box around the shelf's
        ///     cut-out and all of its occupants. This is recalculated whenever
        ///     an occupant is added or removed.
        /// </summary>
        protected Cuboid m_BoundingCuboid;

        // ====================================================================
        // Nested types
        // ====================================================================

        /// <summary>
        ///     Different ways the bottom of the shelf will be sized.
        /// </summary>
        public enum BottomShape
        {
            /// <summary>
            ///     The bottom of the shelf is a countertop and therefore has
            ///     a thickness and lip that matches the kitchen's settings.
            /// </summary>
            Countertop,

            /// <summary>
            ///     The bottom of the shelf is flat and does not have lip.
            /// </summary>
            Flat,
        }

    }   // class CabinetShelf

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-05-12
/// ===========================================================================
