namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using Cysharp.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    ///     Represents a placed instance of a product. An occupant pairs an
    ///     entry in the server's backend data (a <see cref="ProductInfo" />)
    ///     with an instance of a <see cref="ProductVisualContent"/>.
    ///     <br /><br />
    ///
    ///     An <see cref="Occupant"/> highlights when clicked and can be
    ///     dragged with the pointer. It forwards some events to optional
    ///     <see cref="IOccupantListener"/> components attached to its game
    ///     object.
    /// </summary>
    public class Occupant : MelcherMonoBehaviour
        , IPointerDownHandler
        , IBeginDragHandler
        , IDragHandler
        , IEndDragHandler
    {

        /// <summary>
        ///     The minimum amount of space the product will "plunge" into a
        ///     cabinet. This plunge can be from any face. E.g., a top-mounted
        ///     occupant plunges down whereas a front-mounted cabinet plunges
        ///     towards the cabinet's back.
        /// </summary>
        public const float MinimumPlungeDepth = Units.Inches2Meters;

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        ///     The backend server data that describes the product.
        /// </summary>
        public ProductInfo productInfo => m_ProductInfo;

        /// <summary>
        ///     The ProductVisual that this occupant uses.
        ///     <br /><br />
        ///
        ///     Combines the always-loaded <see cref="ProductVisualInfo"/> with
        ///     the loaded-as-needed <see cref="ProductVisualContent"/> (as an
        ///     asset address).
        /// </summary>
        public ProductVisualSO productVisual => m_ProductVisual;

        /// <summary>
        ///     The instantiated <see cref="ProductVisualContent" /> that is used to
        ///     represent the product in the kitchen.
        /// </summary>
        public ProductVisualContent productVisualContentInstance => m_ProductVisualContentInstance;

        public float metersFromVolumeMinX
        {
            get => m_LocalPositionInVolumeSpace.x;
            set
            {
                m_LocalPositionInVolumeSpace.x = value;

                Transform visualTransform = m_ProductVisualContentInstance.GetTransform();
                visualTransform.localPosition = m_LocalPositionInVolumeSpace;
            }
        }

        public Vector3 currentCutOutSizeInMeters =>
            productVisualContentInstance.GetCurrentCutOutSizeInMeters(productInfo.cutOutSizeInMeters);

        /// <summary>
        ///     The cabinet the product directly or indirectly belongs to. This
        ///     is expected to be the topmost <see cref="OccupiableVolume"/> in
        ///     the product's <see cref="occupiedVolume"/> linked list.
        /// </summary>
        public ICabinet cabinet
        {
            get
            {
                OccupiableVolume current = m_OccupiedVolume;

                while (current != null && current.parentVolume != null)
                {
                    current = current.parentVolume;
                }

                return current as ICabinet;
            }
        }

        /// <summary>
        ///     The occupiable volume this product is directly attached to.
        ///     E.g., a <see cref="Cabinet"/> or <see cref="CabinetShelf"/>.
        /// </summary>
        // TODO: This should be an interface if we're going for the "no
        //       cyclical references" restriction similar to ICabinet and
        //       IKitchen. I.e., Occupant knows about OccupiableVolumes and
        //       OccupiableVolume knows about and has a list of Occupants.
        public OccupiableVolume occupiedVolume => m_OccupiedVolume;

        public bool isLockedToCabinet
        {
            get => m_IsLockedToCabinet;
            set => m_IsLockedToCabinet = value;
        }

        // ====================================================================
        // Static functions
        // ====================================================================

        /// <summary>
        ///     A comparison function that can be used to sort occupants by
        ///     their local position in their <see cref="OccupiableVolume"/>
        ///     using the following priority (highest priority to least):
        ///     left-to-right, bottom-to-top, front-to-back.
        ///     <br /><br />
        ///
        ///     This assumes the two occupants have the same <see
        ///     cref="occupiedVolume"/>.
        /// </summary>
        public static int CompareByVolumePosition(Occupant a, Occupant b)
        {
            MelcherLog.Assert(a.occupiedVolume == b.occupiedVolume, "Cannot compare volume position because the occupants are in different volumes.");

            Vector3 aPosition = a.GetLocalPositionInVolume();
            Vector3 bPosition = b.GetLocalPositionInVolume();

            return CompareRawVector(aPosition, bPosition);
        }

        /// <summary>
        ///     A comparison function that can be used to sort occupants by
        ///     their position relative to their cabinet. This is intended to
        ///     allow product positions to be compared when they belong to the
        ///     same cabinet but may or may not be in the same shelf.
        ///
        ///     This isn't Currently, this is only used by <see
        ///     cref="KitchenScreenshotManager"/> to discover unique occupants
        ///     and eliminate duplicate measurement pairs.
        /// </summary>
        /// <returns></returns>
        public static int CompareByCabinetPosition(Occupant a, Occupant b)
        {
            MelcherLog.Assert(a.cabinet == b.cabinet, "Cannot compare cabinet position because the occupants are in different cabinets.");

            Vector3 aPosition =
                a.occupiedVolume
                    .GetTransform()
                    .TransformPoint(a.GetLocalPositionInVolume());

            aPosition =
                a.cabinet.GetTransform().InverseTransformPoint(aPosition);

            Vector3 bPosition =
                b.occupiedVolume
                    .GetTransform()
                    .TransformPoint(b.GetLocalPositionInVolume());

            bPosition =
                b.cabinet.GetTransform().InverseTransformPoint(bPosition);

            return CompareRawVector(aPosition, bPosition);
        }

        /// <summary>
        ///     Compares two vectors in with priority given in the following
        ///     order: x-channel, y-channel, z-channel.
        ///     <br /><br />
        ///
        ///     This behaviour is reused by <see
        ///     cref="CompareByVolumePosition(Occupant, Occupant)"/> and <see
        ///     cref="CompareByCabinetPosition(Occupant, Occupant)"/>.
        /// </summary>
        protected static int CompareRawVector(Vector3 a, Vector3 b)
        {
            int compare = a.x.CompareTo(b.x);
            if (compare != 0)
            {
                return compare;
            }

            compare = a.y.CompareTo(b.y);
            if (compare != 0)
            {
                return compare;
            }

            compare = a.z.CompareTo(b.z);
            return compare;
        }

        /// <summary>
        ///     Calculate the product's adjusted cut-out cuboid. The cut-out
        ///     may be adjusted to prevent impossible cut-out sizes (e.g., 0).
        /// </summary>
        /// <param name="visualContent">
        ///     The visual prefab (or prefab instance) whose cuboid will be
        ///     calculated.
        /// </param>
        /// <param name="defaultCutOutSize">
        ///     The current cut-out size of the product. This value may come
        ///     from a <see cref="ProductInfo.cutOutSizeInMeters"/> if the
        ///     product isn't added to a cabinet yet, or it may be the
        ///     product's current <see cref="ProductVisualContent.GetCurrentCutOutSizeInMeters()"/>
        ///     value.
        /// </param>
        /// <returns></returns>
        public static Cuboid GetCutOutCuboidInVisualSpace(ProductVisualInfoSO visualInfo, ProductVisualContent visualContent, Vector3 defaultCutOutSize)
        {
            Vector3 adjustedCutOutSize = defaultCutOutSize;

            {
                // The cut-out size is inflated to match the collider size
                // if the cut-out size is not specified in the web dashboard.
                //
                // Cut-out sizes are vital because they are the basis for how
                // much space a product occupies in a cabinet. If a product is
                // missing its cut-out size, products "occupied space" will be
                // infinitesimally small, clip into other products, or fail to
                // register as an overlap when dragging products around.
                //
                // We address this by inflating the cut-out to match the
                // visual's collider size, which works as a nice estimate of
                // the amount of space to occupy. The cavity depth (distance
                // into the cabinet, depending on which face the product is
                // mounted to) is set to 1-inch if it is unspecified. This is
                // so it occupies /some/ space to prevent egregious overlaps
                // with products on another face, but not so much to block
                // otherwise plausible placement with nearby occupants.

                Vector3 colliderSize = visualContent.GetColliderSize();

                if (ProFitUtility.IsDistanceAlmostZero(adjustedCutOutSize.x))
                {
                    // Use the collider width as the cut-out distance
                    adjustedCutOutSize.x = colliderSize.x;
                }

                switch (visualInfo.anchor)
                {
                    case ProductAnchor.OnFrontAtBottom:
                    case ProductAnchor.OnFrontAtTop:
                    case ProductAnchor.OnBackAtTop:
                    case ProductAnchor.OnBackAtBottom:
                        if (ProFitUtility.IsDistanceAlmostZero(adjustedCutOutSize.z))
                        {
                            adjustedCutOutSize.z = MinimumPlungeDepth;
                        }

                        if (ProFitUtility.IsDistanceAlmostZero(adjustedCutOutSize.y))
                        {
                            // Use the collider height as the cut-out distance
                            adjustedCutOutSize.y = colliderSize.y;
                        }
                        break;

                    case ProductAnchor.OnTopAtFront:
                    case ProductAnchor.OnTopAtBack:
                        if (ProFitUtility.IsDistanceAlmostZero(adjustedCutOutSize.y))
                        {
                            adjustedCutOutSize.y = MinimumPlungeDepth;
                        }

                        if (ProFitUtility.IsDistanceAlmostZero(adjustedCutOutSize.z))
                        {
                            // Use the collider depth as the cut-out distance
                            adjustedCutOutSize.z = colliderSize.z;
                        }
                        break;

                    default:
                        MelcherLog.LogError($"Unhandled product anchor '{visualInfo.anchor}' for product visual \"{visualContent.name}\", ID {visualInfo.id}", visualContent);
                        break;
                }
            }

            Cuboid cutOutCuboid =
                new Cuboid(
                    position: visualContent.scaleOrigin.localPosition,
                    size: adjustedCutOutSize
                );

            // Correct for non-centered cut-out (x-axis only). This happens if
            // the product's ScaleOrigin and ModelCutOutBegin do not overlap
            // when projected onto the XZ plane
            cutOutCuboid.position += Vector3.right * visualContent.modelCutOutExtentMin.localPosition.x;

            if (!visualInfo.anchor.IsAtBottom())
            {
                // The product is anchored by its top so the cuboid must be
                // corrected. Shift it down by its height.
                cutOutCuboid.position += Vector3.down * cutOutCuboid.height;
            }

            // TODO: Flip Z position if m_Anchor is at back?

            return cutOutCuboid;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Initialize the product using a particular product info and visual.
        /// </summary>
        /// <param name="productInfo"></param>
        /// <param name="newProductVisualInstance">
        /// A unique instantiation of the product visual, specifically for this
        /// product.
        /// </param>
        public void SetInfo(ProductInfo productInfo, ProductVisualSO newProductVisual, ProductVisualContent newProductVisualInstance)
        {
            MelcherLog.LogEditorOnly(nameof(SetInfo), this);

            if (productInfo == null)
            {
                throw new ArgumentNullException(nameof(productInfo));
            }

            if (newProductVisual == null)
            {
                throw new ArgumentNullException(nameof(newProductVisual));
            }

            // Ensure this function is only called with a legit prefab instance.
            // I.e., not a prefab asset.

            if (Utility.RuntimePrefabs.IsPrefab(newProductVisualInstance.gameObject))
            {
                throw new ArgumentException(
                    "Product visual instance must be a prefab instance and not a prefab asset.",
                    nameof(newProductVisualInstance)
                );
            }

            m_ProductInfo = productInfo;
            m_ProductVisual = newProductVisual;
            m_ProductVisualContentInstance = newProductVisualInstance;
            m_ProductVisualContentInstance.SetProductVisibility(true);
            m_ProductVisualContentInstance.SetCutOutSizeInMeters(m_ProductVisual.info, productInfo.cutOutSizeInMeters);

            // Get a truncated product name. Some of the product names are
            // really long!
            const int MaxNameLengthToDisplay = 30;
            string truncatedProductName = productInfo.name;
            if (truncatedProductName.Length > MaxNameLengthToDisplay)
            {
                truncatedProductName = truncatedProductName.Substring(0, MaxNameLengthToDisplay - 3) + "...";
            }

            if (!ProFitGlobals.catalogData.FindManufacturerById(productInfo.manufacturerId, out ManufacturerInfo manufacturerInfo))
            {
                manufacturerInfo.name = "Unknown Manufacturer";
            }

            // TODO: Truncate the manufacturer name?

            // Show the (truncated) product name, manufacturer and product ID
            m_ProductVisualContentInstance.SetCutOutInfoLabelText(
                $"<font-weight=\"300\">{truncatedProductName}</font-weight>\n{manufacturerInfo.name} - {productInfo.sku}"
            );
        }

        /// <summary>
        /// Tell the product which cabinet it belongs to. This directly sets
        /// the product's cabinet reference without removing the product from
        /// its previous cabient. Pass <see langword="null" /> to clear the
        /// cabinet reference.
        /// </summary>
        public void SetCabinetVolume(
            OccupiableVolume   cabinetVolume,
            float           volumePositionX,
            float?          volumePositionY = null,
            float?          volumePositionZ = null)
        {
            if (cabinetVolume == m_OccupiedVolume)
            {
                // Do nothing if this is the same cabinet
                return;
            }

            if (m_OccupiedVolume != null)
            {
                // Notify listeners when we're changing cabinets
                NotifyOccupantListeners(
                    listener => listener.OnRemovedFromCabinet(cabinet)
                );
            }

            m_OccupiedVolume = cabinetVolume;

            if (m_OccupiedVolume != null)
            {
                Vector3 scaleOriginLocalPosition =
                    m_ProductVisualContentInstance.scaleOrigin.localPosition;

                // Center the product within its occupied space.
                scaleOriginLocalPosition.x = 0.0f;

                if (productInfo.doLayoutUsingProductSize)
                {
                    scaleOriginLocalPosition.x = (productInfo.productSizeInMeters.x - productInfo.cutOutSizeInMeters.x) * 0.5f;
                }
                else if (m_OccupiedVolume.DoesApplyHorizontalPadding(productInfo))
                {
                    scaleOriginLocalPosition.x = productInfo.productPaddingInMeters.left;
                }

                m_ProductVisualContentInstance.scaleOrigin.localPosition = scaleOriginLocalPosition;

                Transform visualTransform = m_ProductVisualContentInstance.GetTransform();

                visualTransform.SetParent(
                    cabinetVolume.GetTransform(),
                    worldPositionStays: false
                );

                // Update and restore the product's position
                Vector3 localPositionInVolume = GetLocalPositionInVolume();

                localPositionInVolume.x = volumePositionX;

                if (volumePositionY.HasValue)
                {
                    localPositionInVolume.y = volumePositionY.Value;
                }

                if (volumePositionZ.HasValue)
                {
                    localPositionInVolume.z = volumePositionZ.Value;
                }

                SetLocalPositionInCabinet(localPositionInVolume, m_OccupiedVolume.GetLocalOrientationForOccupants());

                MelcherLog.LogEditorOnly("Notifying listeners OnAddedToCabinet", this);
                NotifyOccupantListeners(
                    listener => listener.OnAddedToCabinet(cabinet)
                );
            }
        }

        /// <summary>
        ///     This function assigns the cut-out size directly to the visual
        ///     but then calls <see cref="RefreshCutOuts"/> to allow a cabinet's
        ///     <see cref="ProductVisualContent.CabinetCutOutMode"/> to override how
        ///     the cut-out is displayed.
        /// </summary>
        public void SetCutOutSizeInMeters(Vector3 cutOutSizeInMeters)
        {
            productVisualContentInstance.SetCutOutSizeInMeters(m_ProductVisual.info, cutOutSizeInMeters, notifyListeners: true);
            RefreshCutOuts();
        }

        public void RefreshCutOuts()
        {
            // TODO: Update the render layer of the cut-out faces according to
            //       the product's ICabinetVolume. Products in shelves need
            //       their cut-outs changed to render correctly.

            bool cutOutFront = IsCutOutFlushWithVolume(CabinetLocalDirection.Front);
            bool cutOutTop = IsCutOutFlushWithVolume(CabinetLocalDirection.Up);
            bool cutOutBack = IsCutOutFlushWithVolume(CabinetLocalDirection.Back);

            // Allow the visual to override adjacent faces from being cut-out
            if (!m_ProductVisual.info.allowAdjacentCutOutWhenFlush)
            {
                // Forbid the adjacent faces from being cut-out
                if (m_ProductVisual.info.anchor.IsOnTop())
                {
                    cutOutFront = false;
                    cutOutBack = false;
                }
                else
                {
                    cutOutTop = false;
                }
            }

            // Calculate the countertop thickness. If the product's cut-out
            // isn't at the top, the thickness will be 0 to hide the
            // countertop cut-out
            float countertopThickness = 0.0f;
            if (cutOutTop)
            {
                countertopThickness = cabinet.kitchen.countertopThicknessInMeters;
            }

            // Handle the overhang size. If the product's cut-out is flush
            // with the front or back of the cabinet, the overhang must be
            // added to the countertop cut-out size

            // TODO: This doesn't handle a product that spans the entire
            //       depth of the cabinet. I.e., the overhang is only
            //       adjusted to the front or to the back
            float countertopOverhangInMeters = 0.0f;
            bool isOnOrAtFront = m_ProductVisual.info.anchor.IsOnOrAtFront();
            if (isOnOrAtFront && cutOutFront)
            {
                // The cut-out should extend to cut-away the overhang
                countertopOverhangInMeters = cabinet.kitchen.baseCountertopOverhangInMeters.towardMinZ;
            }
            else if (!isOnOrAtFront && cutOutBack)
            {
                // The cut-out should extend to cut-away the overhang
                countertopOverhangInMeters = cabinet.kitchen.baseCountertopOverhangInMeters.towardMaxZ;
            }

            bool cabinetCutOutFront = cutOutFront;
            bool cabinetCutOutTop = false;
            bool cabinetCutOutBack = false;

            float cabinetCutOutMetersToKeepBelowCountertop = 0.0f;

            if (m_OccupiedVolume is Cabinet occupiedCabinet &&
                occupiedCabinet.cabinetSectionVisual)
            {
                if (occupiedCabinet.cabinetSectionVisual.info.sectionModelType == CabinetModelType.Authored &&
                    !occupiedCabinet.cabinetSectionVisual.info.authoredSectionUsesCabinetCutOut)
                {
                    // In Gemini mode, some authored cabinets explicitly disable
                    // cabinet cut-outs of their occupants. This is done because
                    // those cabinets have an open window at the front and so only
                    // the countertop needs a cut-out.

                    cabinetCutOutTop = false;
                    cabinetCutOutBack = false;
                }

                CabinetCutOutMode cutOutMode =
                    occupiedCabinet.cabinetSectionVisual.info.sectionCutOutMode;

                if (!m_ProductVisual.info.anchor.IsAtBottom())
                {
                    // These rules currently only apply to occupants that
                    // touch the countertop. Therefore, the `!IsAtBottom()`
                    // check.

                    if (cutOutMode == CabinetCutOutMode.StollFixedSmokerCabinet)
                    {
                        // This is a Stoll fixed smoker cabinet. The front edge of the
                        // countertop is kept for support (do not cut into overhang).

                        cutOutFront = false;
                        countertopOverhangInMeters = 0.0f;

                        cabinetCutOutMetersToKeepBelowCountertop =
                            Const.Stoll.FixedSmokerCabinetCutOutMetersToKeepBelowCountertop;
                    }
                    else if (cutOutMode == CabinetCutOutMode.NoCutOuts ||
                        (cutOutMode == CabinetCutOutMode.NoSmokerCutOuts && m_ProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Smoker))
                    {
                        // Disable all cut-outs.

                        cutOutFront = false;
                        cutOutTop = false;
                        cutOutBack = false;

                        cabinetCutOutFront = false;
                        cabinetCutOutTop = false;
                        cabinetCutOutBack = false;
                    }
                }
            }

            m_ProductVisualContentInstance.SetCountertopSize(
                countertopThickness,
                countertopOverhangInMeters,
                cabinetCutOutMetersToKeepBelowCountertop
            );

            productVisualContentInstance.countertopCutOut.SetActiveStencils(
                cutOutFront,
                cutOutTop,
                cutOutBack
            );

            productVisualContentInstance.cabinetCutOut.SetActiveStencils(
                cabinetCutOutFront,
                cabinetCutOutTop,
                cabinetCutOutBack
            );
        }

        public void MoveToAnchorPosition()
        {
            // Anchor the product to the cabinet
            Transform visualTransform = productVisualContentInstance.GetTransform();
            Vector3 localAnchoredPosition = visualTransform.localPosition;
            switch (m_ProductVisual.info.anchor)
            {
                case ProductAnchor.OnFrontAtBottom:
                    localAnchoredPosition.y = 0.0f;
                    localAnchoredPosition.z = 0.0f;
                    break;

                case ProductAnchor.OnFrontAtTop:
                case ProductAnchor.OnTopAtFront:
                    localAnchoredPosition.y = cabinet.sizeInMeters.y;
                    localAnchoredPosition.z = 0.0f;
                    break;

                case ProductAnchor.OnBackAtTop:
                case ProductAnchor.OnTopAtBack:
                    localAnchoredPosition.y = cabinet.sizeInMeters.y;
                    localAnchoredPosition.z = cabinet.sizeInMeters.z;
                    break;

                case ProductAnchor.OnBackAtBottom:
                    localAnchoredPosition.y = 0.0f;
                    localAnchoredPosition.z = cabinet.sizeInMeters.z;
                    break;

                default:
                    MelcherLog.LogError("Unhandled anchor: " + m_ProductVisual.info.anchor);
                    break;
            }
            visualTransform.localPosition = localAnchoredPosition;
        }

        /// <summary>
        ///     Change the materials of the product's cabinet cut-out faces.
        /// </summary>
        /// <param name="cabinetMaterial">
        ///     The material to apply to the interior cabinet faces.
        /// </param>
        /// <param name="countertopMaterial">
        ///     The material to apply to the interior countertop faces.
        /// </param>
        /// <param name="cutOutMaterialOverride">
        ///     Allows the material on the cut-out's interior faces to be set to
        ///     something different. We use this in the CAD screenshots to
        ///     ensure the cut-outs are darker without needing lighting.
        /// </param>
        public async UniTask SetCabinetMaterials(
            Material cabinetMaterial,
            Material countertopMaterial,
            Material productCabinetMaterialIdentifier,
            Material cutOutMaterialOverride = null)
        {
            MelcherLog.LogEditorOnly(nameof(SetCabinetMaterials));

            // For the plan screenshots, we want the cut-outs to render in one
            // colour (full white). But products that fit to their cabinet
            // width (bar top, back splash) should continue to render as
            // normal
            // TODO: Move this block into KitchenScreenshotManager. This won't
            //       be tripped anymore for cabinet backings here because they
            //       aren't added as occupants anymore.
            if (!productVisualContentInstance.TryGetComponent(out CabinetBacking _))
            {
                if (cutOutMaterialOverride)
                {
                    // Use the override cut-out material
                    cabinetMaterial = cutOutMaterialOverride;
                    countertopMaterial = cutOutMaterialOverride;
                }
            }

            await m_ProductVisualContentInstance.SetCabinetMaterials(
                cabinetMaterial,
                countertopMaterial,
                productCabinetMaterialIdentifier
            );

            NotifyOccupantListeners(
                listener => listener.OnCabinetMaterialsChanged(cabinetMaterial, countertopMaterial)
            );

            if (TryGetComponent(out CabinetShelf shelf))
            {
                foreach (Occupant nestedOccupant in shelf.occupants)
                {
                    await nestedOccupant.SetCabinetMaterials(
                        cabinetMaterial,
                        countertopMaterial,
                        productCabinetMaterialIdentifier,
                        cutOutMaterialOverride
                    );
                }
            }
        }

        /// <summary>
        /// Get the position of the product within it's cabinet's local space.
        /// The position is the product's origin point whose position relative
        /// to the rest of the product varies by the product's anchoring.
        /// </summary>
        public Vector3 GetLocalPositionInVolume()
        {
            return m_LocalPositionInVolumeSpace;
        }

        /// <summary>
        /// Set the position of the product. This function also saves an
        /// internal copy of the position that is accessible from <see
        /// cref="GetLocalPositionInVolume"/> even when the product is re-
        /// parented. This is used to preserve some position information (such
        /// as height) when an occupant is dragged between cabinets.
        /// <br /><br />
        ///
        /// This function blindly assigns the new position without doing any
        /// error checking.
        /// </summary>
        /// <param name="cabinetOccupantOrientation">
        ///     This is a kludge to rotate and/or position products for special
        ///     case cabinets. For example, the 45-degree corner cabinets.
        /// </param>
        public void SetLocalPositionInCabinet(Vector3 positionInCabinetSpace, Transform cabinetOccupantOrientation)
        {
            m_LocalPositionInVolumeSpace = positionInCabinetSpace;

            Transform visualTransform = m_ProductVisualContentInstance.GetTransform();

            Vector3 worldSpacePosition =
                cabinetOccupantOrientation.TransformPoint(positionInCabinetSpace);

            visualTransform.position = worldSpacePosition;
            visualTransform.localRotation = cabinetOccupantOrientation.localRotation;

            productVisualContentInstance.RefreshCutOutVisibility();
        }

        /// <summary>
        /// Get the product's cut-out size in it's occupied volume's space. The
        /// cut-out cuboid is clamped so it doesn't extend beyond the cabinet's
        /// bounds.
        /// </summary>
        public Cuboid GetCutOutCuboidInVolumeSpace()
        {
            Cuboid cutOutCuboid =
                GetCutOutCuboidInVisualSpace(
                    m_ProductVisual.info,
                    m_ProductVisualContentInstance,
                    currentCutOutSizeInMeters
                );

            // Add the visuals' local position to get the cuboid
            // in the cabinet's local space:
            cutOutCuboid.position += productVisualContentInstance.GetTransform().localPosition;

            // Clamp the cut-out to the cabinet's bounds
            //cutOutCuboid = Cuboid.Clamp(cutOutCuboid, new Cuboid(cabinet.sizeInMeters));

            return cutOutCuboid;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the countertop cut-out should be
        /// rendered. This happens when the cut-out is flush with the top of
        /// the cabinet. Otherwise, this returns <see langword="false"/>.
        /// </summary>
        /// <returns></returns>
        public bool ShouldShowCountertopCutOut()
        {
            return IsCutOutFlushWithVolume(CabinetLocalDirection.Up);
        }

        /// <summary>
        /// Return <see langword="true"/> if the product's cut-out is flush
        /// with the side of the volume indicated by <paramref
        /// name="volumeFaceNormal"/>.
        ///
        /// This function lets us calculate whether a product should appear
        /// in the plan photos from a particular angle. E.g., if we're
        /// capturing a front image of a cabinet, we only want to render
        /// cut-outs of products that are flush with <see
        /// cref="CabinetLocalDirection.Front"/>.
        /// </summary>
        public bool IsCutOutFlushWithVolume(CabinetLocalDirection volumeFaceNormal)
        {
            ProductAnchor anchor = m_ProductVisual.info.anchor;

            Func<Cuboid, float> getCuboidEdge = null;

            switch (volumeFaceNormal)
            {
                default:
                    throw new NotImplementedException($"Unhandled {nameof(CabinetLocalDirection)} value: {volumeFaceNormal}");

                case CabinetLocalDirection.Left:
                    getCuboidEdge = static (cuboid) => cuboid.xMin;
                    break;

                case CabinetLocalDirection.Right:
                    getCuboidEdge = static (cuboid) => cuboid.xMax;
                    break;

                case CabinetLocalDirection.Up:
                    if (anchor.IsOnTop())
                    {
                        return true;
                    }
                    getCuboidEdge = static (cuboid) => cuboid.yMax;
                    break;

                case CabinetLocalDirection.Down:
                    getCuboidEdge = static (cuboid) => cuboid.yMin;
                    break;

                case CabinetLocalDirection.Front:
                    if (anchor.IsOnFront())
                    {
                        return true;
                    }
                    getCuboidEdge = static (cuboid) => cuboid.zMin;
                    break;

                case CabinetLocalDirection.Back:
                    if (!anchor.IsOnFront() && !anchor.IsOnTop())
                    {
                        return true;
                    }
                    getCuboidEdge = static (cuboid) => cuboid.zMax;
                    break;
            }

            Cuboid cabinetCuboid = occupiedVolume.occupiableCuboid;
            Cuboid cutOutCuboid = GetCutOutCuboidInVolumeSpace();
            const float Tolerance = Units.Inches2Meters * (1.0f / 32.0f);

            return MelcherCommon.IsAlmostEqual(
                getCuboidEdge.Invoke(cabinetCuboid),
                getCuboidEdge.Invoke(cutOutCuboid),
                Tolerance
            );
        }

        public void SetIsNested(bool isNested)
        {
            MelcherLog.Log(name + ": Set is nested: " + isNested);

            int interiorLayer = LayerMask.NameToLayer(isNested ? "NestedCutOutInterior" : "CutOutInterior");

            productVisualContentInstance.cabinetCutOut.SetInteriorLayer(interiorLayer);
            productVisualContentInstance.countertopCutOut.SetInteriorLayer(interiorLayer);
        }

        protected void NotifyOccupantListeners(Action<IOccupantListener> listenerAction)
        {
            if (listenerAction == null)
            {
                throw new ArgumentNullException(nameof(listenerAction));
            }

            if (s_IsListenerListInUse)
            {
                // Guard against recursive calls. We're sharing this list of
                // listeners with all products and need to ensure it isn't
                // modified while we're using it / iterating it
                throw new InvalidOperationException(
                    $"{nameof(Occupant)}.{nameof(s_OccupantListeners)} can " +
                    $"only be used by one {nameof(Occupant)} at a time"
                );
            }

            // Mark the list as in-use and setup a scope guard to automatically
            // revert the in-use flag when the scope exits
            s_IsListenerListInUse = true;
            using var _ =
                MelcherDeferred.Defer(
                    () => s_IsListenerListInUse = false
                );

            GetComponents(s_OccupantListeners);
            MelcherLog.LogEditorOnly($"Found {s_OccupantListeners.Count} listeners");

            foreach (IOccupantListener listener in s_OccupantListeners)
            {
                listenerAction?.Invoke(listener);
            }
        }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            // TODO: Remove this
            //if (m_Cabinet != null)
            //{
            //    Cabinet cabinet = (Cabinet)m_Cabinet;
            //    Cuboid cabinetAreaLocalSpace = cabinet.CalculateCabinetSpaceCuboidForProduct(this);
            //    Cuboid cabinetAreaWorldSpace = Cuboid.Transform(m_Cabinet.GetTransform(), cabinetAreaLocalSpace);
            //    Gizmos.color = Color.red;
            //    Gizmos.DrawWireCube(cabinetAreaWorldSpace.center, cabinetAreaWorldSpace.size);
            //}
        }
#endif  // if UNITY_EDITOR

        // =====================================================================
        // IPointerDownHandler
        // =====================================================================
        #region IPointerDownHandler

        public void OnPointerDown(PointerEventData eventData)
        {
            EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(gameObject).Forget();
        }

        #endregion  // IPointerDownHandler

        // =====================================================================
        // IBeginDragHandler
        // =====================================================================
        #region IBeginDragHandler

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!m_ProductVisual.info.isDraggable)
            {
                eventData.pointerDrag = null;
                return;
            }

            EventBridge.Designer.onOccupantDragBegin?.Invoke(this, eventData);
        }

        #endregion  // IBeginDragHandler

        // =====================================================================
        // IDragHandler
        // =====================================================================
        #region IDragHandler

        public void OnDrag(PointerEventData eventData)
        {
            EventBridge.Designer.onOccupantDrag?.Invoke(this, eventData);
        }

        #endregion  // IDragHandler

        // =====================================================================
        // IEndDragHandler
        // =====================================================================
        #region IEndDragHandler

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_LastDragTask.HasValue || m_LastDragTask.Value.Status != UniTaskStatus.Pending)
            {
                m_LastDragTask = EventBridge.Designer.onOccupantDragEnd.InvokeIfNonEmpty((this, eventData));
                m_LastDragTask.Value.Forget();
            }
        }

        #endregion  // IEndDragHandler

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        /// <summary>
        ///     The business data (or Product Data Model) in use by this
        ///     product. I.e., the product's name, ID, price, size, etc. This
        ///     data is provided by the server.
        /// </summary>
        protected ProductInfo m_ProductInfo;

        /// <summary>
        ///     The product visual data that represents the this occupant.
        /// </summary>
        protected ProductVisualSO m_ProductVisual;

        /// <summary>
        ///     A unique instantiation of the product's content prefab for this
        ///     object.
        /// </summary>
        protected ProductVisualContent m_ProductVisualContentInstance;

        /// <summary>
        ///     The <see cref="OccupiableVolume"/> that is most directly
        ///     connected this product. I.e., if this product is within a
        ///     cabinet shelf, its volume will be the shelf, which in turn will
        ///     have a cabinet as its volume. Products will generally be
        ///     attached to directly to a cabinet and thus this will be a
        ///     <see cref="Cabinet"/>.
        /// </summary>
        protected OccupiableVolume m_OccupiedVolume;

        /// <summary>
        /// The local position of the product in cabinet space. This is saved
        /// in a separate field to preserve values between re-parenting. This
        /// value does not include any custom rotation that would be applied by
        /// a 45-degree corner cabinet.
        /// </summary>
        protected Vector3 m_LocalPositionInVolumeSpace;

        /// <summary>
        ///     This marks an occupant as being permanently attached to its
        ///     cabinet. A locked occupant cannot be moved to a different
        ///     cabinet and can only be removed if its cabinet is destroyed.
        /// </summary>
        protected bool m_IsLockedToCabinet;

        private UniTask? m_LastDragTask;

        // =====================================================================
        // Static fields
        // =====================================================================

        /// <summary>
        /// For memory considerations, we share one list for all <see
        /// cref="Occupant"/> messages to <see cref="IOccupantListener"/>s.
        /// </summary>
        protected static List<IOccupantListener> s_OccupantListeners =
            new List<IOccupantListener>();

        /// <summary>
        /// Indicates whether <see cref="s_OccupantListeners"/> is in use. The
        /// list must only be used by one <see cref="Occupant"/> at a time. This
        /// field lets us detect if this requirement ever fails.
        /// </summary>
        protected static bool s_IsListenerListInUse;

    }   // class Occupant

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
