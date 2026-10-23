namespace KitchenDesign.Cabinets
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Server;
    using Cysharp.Threading.Tasks;
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using KitchenDesign.Geometry;
    using KitchenDesign.Serializables;
    using KitchenDesign.Shared.Interfaces;
    using KitchenDesign.KitchenModel;

    /// <summary>
    ///     This script is attached to the Backsplash and Bar Top prefabs. It
    ///     is responsible for sizing those product's to fit the cabinet's
    ///     length and meet at the corners with adjacent backings.
    /// </summary>
    public class CabinetBacking : MelcherMonoBehaviour
        , IPointerDownHandler
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public ProductInfo info => m_ProductInfo;

        /// <summary>
        ///     Return <see langword="true"/> if the backing has a riser and
        ///     countertop of its own. Otherwise, the backing modifies the
        ///     countertop of the cabinets in its span.
        /// </summary>
        public bool hasRiserAndRiserCountertop => m_Riser && m_RiserCountertop;

        public bool cropCabinetCountertops => m_CropCabinetCountertops;

        public bool isRiserCountertopVisible => m_RiserCountertop && m_RiserCountertop.gameObject.activeSelf;

        public RiserMaterialMode riserMaterialMode
        {
            get => m_RiserMaterialMode;
            set => m_RiserMaterialMode.value = value;
        }

        public CuboidPadding countertopOverhangInMeters
        {
            get => m_CountertopOverhangInInches * Units.Inches2Meters;
            set => m_CountertopOverhangInInches = value * Units.Meters2Inches;
        }

        public Cuboid riserCuboid => m_Riser.cuboid;

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetProductInfo(ProductInfo productInfo)
        {
            m_ProductInfo = productInfo;
        }

        public Transform GetTransform()
        {
            return m_Transform;
        }

        public float GetCabinetInflateDepthInMeters(IBuildingMethodRules buildingRules)
        {
            float inches =
                buildingRules.UsesStollBuilderDistances ?
                    m_InflateCabinetDepthByInches.stoll :
                    m_InflateCabinetDepthByInches.proFit;

            return inches * Units.Inches2Meters;
        }

        /// <summary>
        ///     Get the cuboid for the cabinet backing's countertop, in the
        ///     span's local space. This works because the backing is always
        ///     parented to <see cref="CabinetSpan.orientationAlignedCabinet"/>
        ///     which is the same transform that represents span space.
        /// </summary>
        public Cuboid GetCountertopCuboidInSpanSpace(Cuboid defaultCountertopCuboid)
        {
            if (m_RiserCountertop)
            {
                return m_RiserCountertop.cuboid;
            }

            // TODO: Do we need to pass in the cabinet's actual countertop
            //       cuboid so it can be returned here? -Ryan
            return default;
        }

        /// <summary>
        ///     This must be called manually because backings are no longer
        ///     <see cref="Occupant"/>s.
        /// </summary>
        public void OnCabinetMaterialsChanged(IKitchen kitchen, Material cabinetMaterial, Material countertopMaterial)
        {

            RiserMaterialMode riserMaterialMode = m_RiserMaterialMode;

            if (kitchen.buildingRules.BackingRiserUsesCabinetMaterial)
            {
                riserMaterialMode = RiserMaterialMode.UseCabinetMaterial;
            }

            if (kitchen.selectedBrandCatalog && kitchen.selectedBrandCatalog.defaultCabinetFinishOverride)
            {
                cabinetMaterial = kitchen.selectedBrandCatalog.defaultCabinetFinishOverride;
            }

            if (m_Riser && riserMaterialMode != RiserMaterialMode.KeepUnchanged)
            {
                if (riserMaterialMode == RiserMaterialMode.UseCountertopMaterial)
                {
                    cabinetMaterial = countertopMaterial;
                }

                // Apply the cabinet materials to the riser and riser countertop
                Renderer[] riserRenderers = m_Riser.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in riserRenderers)
                {
                    Utility.RendererUtility.SetAllRendererMaterials(r, cabinetMaterial);
                }
            }

            if (m_RiserCountertop)
            {
                Renderer[] countertopRenderers = m_RiserCountertop.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in countertopRenderers)
                {
                    Utility.RendererUtility.SetAllRendererMaterials(r, countertopMaterial);
                }
            }
        }

        public void SetModelSize(Cuboid riserCuboidInSpanSpace, Cuboid countertopCuboidInSpanSpace, float countertopThicknessInMeters)
        {
            if (m_Riser)
            {
                Cuboid riserCuboid = riserCuboidInSpanSpace;

                riserCuboid.yMin -= countertopThicknessInMeters;

                m_Riser.UpdateSlices(riserCuboid);
            }

            if (m_RiserCountertop)
            {
                m_RiserCountertop.UpdateSlices(countertopCuboidInSpanSpace);
            }

            // Match the click hitbox collider to the model size.

            Bounds colliderBounds =
                new Bounds(
                    riserCuboidInSpanSpace.center,
                    riserCuboidInSpanSpace.size
                );

            colliderBounds.Encapsulate(
                new Bounds(
                    countertopCuboidInSpanSpace.center,
                    countertopCuboidInSpanSpace.size
                )
            );

            m_Collider.center = colliderBounds.center;
            m_Collider.size = colliderBounds.size;
        }

        /// <summary>
        ///     Set whether the backing's riser countertop is visible. In Stoll
        ///     builds, backsplashes do not have countertops.
        /// </summary>
        public void SetRiserCountertopVisible(bool isVisible)
        {
            m_RiserCountertop?.gameObject.SetActive(isVisible);
        }

        protected void Awake()
        {
            if (m_Riser)
            {
                CuboidPadding skinScale = m_Riser.skinScale;
                skinScale.bottom = 1.0f / m_RiserModelHeightInMeters;
                m_Riser.skinScale = skinScale;
            }
        }


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
        // Inspector-serialized fields
        // =====================================================================

        [Tooltip("A model asset that is used to display the backing. This is " +
            "used to recover the model's original materials when the " +
            "kitchen's finishes are cleared.")]
        [SerializeField, MelcherAssetField] protected GameObject m_ModelPrefab = null;

        [Tooltip("The root transform that contains the backing's mesh" +
            "renderers. This is used to limit the component search to " +
            "the parent of the backings mesh renderers.")]
        [SerializeField] protected Transform m_ScaleOrigin = default;

        [Tooltip("The cabinet-like portion that sits on top of the real " +
            "cabinet. The riser belongs to the cabinet backing.")]
        [SerializeField] protected SlicedMesh m_Riser = null;

        [Tooltip("The countertop portion belonging to the cabinet backing.")]
        [SerializeField] protected SlicedMesh m_RiserCountertop = null;

        [Space]

        [Tooltip("The height of the riser model. This is used to calculate " +
            "the scale necessary for the model to meet the countertop " +
            "vertically.")]
        [SerializeField]
        protected float m_RiserModelHeightInMeters = 1.0f;

        [Tooltip("When true, cabinets affected by this backing will have " +
            "their countertops readjusted so as to not extend " +
            "beneath/through the backing's riser. For some special types " +
            "of bar tops (e.g., Challenger Designs'), the backing is " +
            "hoisted on decorative legs which the cabinets countertop must " +
            "continue through.")]
        [SerializeField]
        protected bool m_CropCabinetCountertops = true;

        [Tooltip("Determines what material is applied to the backing's riser.")]
        [SerializeField]
        protected MelcherStableEnum<RiserMaterialMode> m_RiserMaterialMode = new MelcherStableEnum<RiserMaterialMode>(RiserMaterialMode.UseCabinetMaterial);

        [Space]

        [Tooltip("The additional distance to extend the countertop along " +
            "the four local axises (left, right, front, back)")]
        [SerializeField] protected CuboidPadding m_CountertopOverhangInInches = default;

        [Tooltip("The amount (in inches) to expand the depth of all cabinets " +
            "in a span when this backing is attached to the cabinet. E.g. a " +
            "bar top adds 10 inches of depth.")]
        [SerializeField] protected BuilderSpecificDistance m_InflateCabinetDepthByInches = default;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Transform m_Transform = default;

        [Tooltip("The collider that enables the backing to be selected.")]
        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down, minDistance: 1)]
        protected BoxCollider m_Collider = default;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        /// <summary>
        ///     Information about the backing as recorded in the server. This
        ///     allows details about the backing, like its size, to change via
        ///     the dashboard and without needing to make a new app build.
        /// </summary>
        protected ProductInfo m_ProductInfo;

        // =====================================================================
        // Nested types
        // =====================================================================



    }   // class CabinetBacking

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-04
/// ===========================================================================
