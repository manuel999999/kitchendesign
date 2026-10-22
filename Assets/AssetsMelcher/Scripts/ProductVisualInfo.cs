namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using System;
    using UnityEngine;

    /// <summary>
    ///     Lightweight product visual data that remains loaded at all times.
    ///     Mainly, this separates plain-old-data from the heavier mesh and
    ///     texture data.
    ///     <br /><br />
    ///
    ///     See <see cref="ProductVisualContent"/> for the heavy content.
    /// </summary>
    [CreateAssetMenu(fileName ="", menuName = "Melcher Studios/Pro-Fit/Product Visual Info")]
    public class ProductVisualInfo : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public string id => m_Id;
        public Sprite thumbnail => m_Thumbnail;

        /// <summary>
        ///     This is a more specific categorization of the product that is
        ///     intended to be used to make edits to the runtime product visual
        ///     prefabs, without needing to know anything about the associated
        ///     <see cref="ProductInfo"/>s.
        ///     <br /><br />
        ///
        ///     This differs from <see cref="ProductInfo.category"/> because it
        ///     will not change (unlike a product info's category which is set
        ///     by the user in the dashboard) and provides much more granular
        ///     sorting (e.g., the fridge category).
        /// </summary>
        public ProductCategorySpecific runtimePrefabCategory => m_RuntimePrefabCategory;

        public ProductAnchor anchor => m_Anchor;

        public float initialInsetFromEdgeInMeters
        {
            get => m_InitialInsetFromEdgeInInches * Units.Inches2Meters;

            [System.Obsolete("This is a hack to quickly re-try adding " +
                "products at different vertical positions. This whole " +
                "field should actually be replaced with " +
                "ProductInfo.anchorOffsetInMeters. This is now also " +
                "used to change product offsets depending on which " +
                "building mode is selected.")]
            set => m_InitialInsetFromEdgeInInches = value * Units.Meters2Inches;
        }

        public bool hidesBackCountertopEdge => m_HidesBackCountertopEdge;
        public bool fitSectionToProductWidth => m_FitSectionToProductWidth;
        public bool ignoreCabinetProductWhitelist => m_IgnoreCabinetProductWhitelist;
        public bool useCabinetMaterial => m_UseCabinetMaterial;
        public bool isDraggable => m_IsDraggable;
        public bool allowUpAndDownMovement => m_AllowUpAndDownMovement;
        public bool isCountertopOven => m_IsCountertopOven;
        public bool allowAdjacentCutOutWhenFlush => m_AllowAdjacentCutOutWhenFlush;
        public bool requireManualSelection => m_RequireManualSelection;
        public CabinetModelType sectionModelType => m_SectionModelType;
        public FlooringType flooringType => m_FlooringType;
        public CabinetType sectionCabinetType => m_SectionCabinetType;
        public int sectionCountertopIndexInCabinetPrefab => m_SectionCountertopIndex;
        public int sectionInsetToeKickIndex => m_SectionInsetToeKickIndex;

        public Vector2 sectionProceduralInsetToeKickExtraInsetInMeters =>
                m_SectionProceduralInsetToeKickExtraInsetInInches2 * Units.Inches2Meters;

        public CuboidPadding sectionOccupiableCuboidPaddingInInches =>
            m_SectionOccupiableCuboidPaddingInInches;

        public CuboidPadding sectionOccupiableCuboidPaddingInMeters =>
            m_SectionOccupiableCuboidPaddingInInches * Units.Inches2Meters;
        public Vector2 cabinetLegHorizontalInsetFromCabinetEdgesOverrideInMeters =>
            m_CabinetLegHorizontalInsetFromCabinetEdgesOverrideInInches * Units.Inches2Meters;

        public bool expectingCabinetDoors => m_ExpectingCabinetDoors;
        public bool sectionAppearsInDetailSheet => m_SectionAppearsInDetailSheet;
        public CabinetSectionFootingType cabinetSectionFootingType => m_CabinetSectionFootingType;
        public bool authoredSectionUsesCabinetCutOut => m_AuthoredSectionUsesCabinetCutOut;
        public bool sectionUsesCabinetVents => m_SectionUsesCabinetVents;
        public bool sectionCanHaveToeKick => m_SectionCanHaveToeKick;
        public bool sectionCanHaveFrontProducts => m_SectionCanHaveFrontProducts;
        public bool sectionAlwaysHasPlunge => m_SectionAlwaysHasPlunge;
        public CabinetCutOutMode sectionCutOutMode => m_SectionCutOutMode;

        public MelcherStableEnum<ProductCategorySpecific>[] productCategoriesAllowedInSection
        {
            get => m_ProductCategoriesAllowedInSection;
#if UNITY_EDITOR
            set => m_ProductCategoriesAllowedInSection = value;
#endif  // if UNITY_EDITOR
        }

        public float forcedCabinetCuboidDepthInMeters => m_ForcedCabinetCuboidDepthInMeters;
        public string brandCatalogName => m_BrandCatalogName;
        public string sectionDoorStyleName => m_SectionDoorStyleName;
        public string sectionDoorStyleDisplayName => m_SectionDoorStyleDisplayName;
        public float sectionEndPanelSizeInches => m_SectionEndPanelSizeInches;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Check if a product category is allowed to fit in this cabinet
        ///     section. This does not check if the object invoking the
        ///     function is actually a cabinet. Intended to make it easier to
        ///     work with the MelcherStableEnum array that is used for
        ///     <see cref="m_ProductCategoriesAllowedInSectionSpecificStable"/>.
        /// </summary>
        public bool IsCategoryAllowedInSection(ProductCategorySpecific category)
        {
            foreach (MelcherStableEnum<ProductCategorySpecific> allowed in m_ProductCategoriesAllowedInSection)
            {
                if (category == allowed.value)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsProductManufacturerAllowedInSection(int manufacturerId)
        {
            foreach (int allowedManufacturer in m_ProductManufacturersAllowedInSection)
            {
                if (allowedManufacturer == manufacturerId)
                {
                    return true;
                }
            }

            return false;
        }

        // ====================================================================
        // Fields
        // ====================================================================

        [Tooltip("The identifier for the visual. This is a string that " +
            "ProductInfo's (entered in the server backend) can reference to " +
            "dictate how they are displayed in-app.")]
        [SerializeField]
        private string m_Id;

        [Tooltip("Thumbnail icon for the 'Add a Product' screen.")]
        [SerializeField]
        protected Sprite m_Thumbnail = null;

        [Tooltip("This value is intended to be used to group or identify" +
            "ProductVisuals by their function prior to being referenced by " +
            "a ProductInfo. Normally, to know which group a product " +
            "belongs to, the app must obtain a list of ProductInfos from " +
            "the server. However there are situations where we need to " +
            "modify product visuals according to their product group " +
            "before their ProductInfo's category is known.")]
        [SerializeField]
        private MelcherStableEnum<ProductCategorySpecific> m_RuntimePrefabCategory;

        // --------------------------------------------------------------------
        [Header("Occupant")]

        [Tooltip("Defines where the product attaches/anchors to the cabinet.")]
        [SerializeField]
        private MelcherStableEnum<ProductAnchor> m_Anchor = new (ProductAnchor.OnTopAtFront);

        [Tooltip("The distance in inches that the product is slid into the " +
            "cabinet and away from it's anchored edge. E.g., OnTopAtFront " +
            "will have an inset that pushes the product along the positive-Z " +
            "axis (towards the back of the cabinet).")]
        [SerializeField]
        private float m_InitialInsetFromEdgeInInches;

        [Tooltip("Whether the cut-out on the secondary cabinet face (the " +
            "face adjacent/sharing an edge with the mount reference) can be " +
            "used. When true, the secondary face's cut-out will be used if " +
            "the inset is less than or equal to zero (i.e. the product is " +
            "flush with the adjacent face). When this is false, the cut-out " +
            "out on the adjacent face is never used.")]
        [SerializeField]
        private bool m_AllowAdjacentCutOutWhenFlush = true;

        [Tooltip("When enabled, this product's collider is turned off to " +
            "prevent the pointer from selecting the product through the " +
            "event system. This product will then only be selected if no " +
            "other products (excluding cabinets) are in the way.")]
        [SerializeField]
        private bool m_RequireManualSelection;

        [Tooltip("If true, having this product occupy a cabinet will hide " +
            "the back countertop edge of the attached cabinet and its " +
            "immediate neighbours (extensions).")]
        [SerializeField]
        private bool m_HidesBackCountertopEdge;

        [Tooltip("If true, any cabinet this product is added to will be " +
            "resized (width-wise) to match this product's width. Currently, " +
            "this is limited to Sections and Catalog building methods.")]
        [SerializeField]
        private bool m_FitSectionToProductWidth;

        [Tooltip("If true, this product can be placed on Universal Kitchen " +
            "Designer cabinets, even if the product's category doesn't match " +
            "the cabinet's product category whitelist.")]
        [SerializeField]
        private bool m_IgnoreCabinetProductWhitelist;

        [Tooltip("If true, this product's material will match the current " +
            "cabinet material (or kitchen highlight if the kitchen is " +
            "selected).")]
        // TODO: Remove this? We now look for special materials assigned to the
        //       original prefab and replace those with the cabinet material.
        //       Still looks to be used.
        [SerializeField]
        private bool m_UseCabinetMaterial;

        [Tooltip("Whether the product can be dragged. Some products, like " +
            "bar tops, can't be moved.")]
        [SerializeField]
        private bool m_IsDraggable = true;

        [Tooltip("Whether the product can be moved up or down a vertical  " +
            "face of the cabinet. The product must be mounted to the front " +
            "or back cabinet face. If the product has the correct anchor " +
            "and is selected, the product toolbar will show a Move Up and " +
            "Move Down button.")]
        [SerializeField]
        private bool m_AllowUpAndDownMovement;

        [Tooltip("If true, this object's cut-out will render while the " +
            "product's model is rendered. This is needed for some products " +
            "who don't fully seal against the cabinet. When this is on, no " +
            "clamping is performed on the product model's scale. This is " +
            "to ensure the visual model lines up with the cut-out.")]
        [SerializeField]
        private bool m_RenderProductWithCutOut;

        [SerializeField]
        private bool m_IsCountertopOven;

        // --------------------------------------------------------------------
        [Header("Finish")]

        [Tooltip("If a product in the backend, belonging to the flooring " +
            "category, uses this visual, this value informs the kitchen " +
            "which mesh to use to render the floor. If the product does not " +
            "belong to the Flooring category, this value is ignored.")]
        [SerializeField]
        private MelcherStableEnum<FlooringType> m_FlooringType = new (FlooringType.Wood);

        // --------------------------------------------------------------------
        [Header("Section")]

        [SerializeField]
        private MelcherStableEnum<CabinetModelType> m_SectionModelType = new (CabinetModelType.Authored);

        [SerializeField]
        private MelcherStableEnum<CabinetType> m_SectionCabinetType = new (CabinetType.Straight);

        [Tooltip("The cabinet prefabs contain the countertop. This field is " +
            "used to indicate if a special countertop model is required by " +
            "this cabinet model that is different from the default. It is " +
            "an index into the cabinet prefab's ")]
        [SerializeField]
        private int m_SectionCountertopIndex;

        [Tooltip("To accommodate non-standard cabinet shapes, products can " +
            "specify which toe kick model to use. The default index is -1 " +
            "which indicates a procedural toe kick will be used that is " +
            "made up of 1 or 2 cubes.")]
        [SerializeField]
        private int m_SectionInsetToeKickIndex = -1;

        [Tooltip("This is an optional length used by procedural inset " +
            "toe kicks (sectionInsetToeKickIndex == 0) that shifts the " +
            "x-local toe cube further towards the back of the cabinet (and " +
            "likewise, the z-local toe kick further to the right) to " +
            "accommodate cabinets models with two front faces that meet in " +
            "an L-shape. E.g., WeatherStrong's Corner Return Base.")]
        [SerializeField]
        private Vector2 m_SectionProceduralInsetToeKickExtraInsetInInches2;

        [Tooltip("The amount of padding to apply to the cabinet's " +
            "occupiable cuboid. This is used to prevent cabinets that have " +
            "doors and drawers from having products overlap those features.")]
        [SerializeField]
        private CuboidPadding m_SectionOccupiableCuboidPaddingInInches;

        [Tooltip("An optional offset that is used when spawning cabinet " +
            "legs. This is useful for cabinets that have table legs that " +
            "otherwise may not align with the default offset. This field " +
            "uses NaN to encode 'no specified'. Ideally, we would use " +
            "a nullable type but those are not rendered in the inspector.")]
        [SerializeField]
        private Vector2 m_CabinetLegHorizontalInsetFromCabinetEdgesOverrideInInches = new Vector2(float.NaN, float.NaN);

        [SerializeField]
        protected MelcherStableEnum<CabinetSectionFootingType> m_CabinetSectionFootingType = new(CabinetSectionFootingType.OnLegs);

        [Tooltip("Whether this cabinet section was designed to have a " +
            "separate door/drawer model added to it. For most cabinets, this " +
            "this will be true but there are speciality cabinets that have " +
            "no doors. Currently, this only controls whether an error is " +
            "printed when cabinet doors are being swapped.")]
        [SerializeField]
        private bool m_ExpectingCabinetDoors;

        [Tooltip("Whether this cabinet section should appear in the quote's " +
            "detail sheet. This is currently true for all cabinets except " +
            "for the refrigerator cabinet.")]
        [SerializeField]
        private bool m_SectionAppearsInDetailSheet = true;

        [Tooltip("Determines whether occupants' cabinet cut-out is drawn " +
            "when this is an authored cabinet section. This is almost " +
            "always true, but some cabinets in Gemini have an open front " +
            "face and therefore only the countertop needs a cut-out.")]
        [SerializeField]
        private bool m_AuthoredSectionUsesCabinetCutOut = true;

        [SerializeField]
        private bool m_SectionUsesCabinetVents = true;

        [Tooltip("Whether this cabinet section can display a toe kick. Some " +
            "cabinets, like table extensions, have an open bottom where toe " +
            "kicks don't make any sense.")]
        [SerializeField]
        private bool m_SectionCanHaveToeKick = true;

        [Tooltip("Whether this cabinet section can have occupants that " +
            "are placed on the front of the cabinet. Some corner cabinets " +
            "are made up of two parts in an L-Shape and therefore do not " +
            "have a unified front face for front-mount products to occupy.")]
        [SerializeField]
        private bool m_SectionCanHaveFrontProducts = true;

        [Tooltip("Whether this cabinet section spawns with a burner plunge " +
            "that can't be removed. This is used by Challenger's power " +
            "burner base cabinets.")]
        [SerializeField]
        private bool m_SectionAlwaysHasPlunge;

        [Tooltip("Allows cabinets to specify a way of handling its cut-outs.")]
        [SerializeField]
        private MelcherStableEnum<CabinetCutOutMode> m_SectionCutOutMode = new (CabinetCutOutMode.Normal);

        [SerializeField]
        private MelcherStableEnum<ProductCategorySpecific>[] m_ProductCategoriesAllowedInSection;

        [SerializeField]
        private int[] m_ProductManufacturersAllowedInSection;

        [Tooltip("Overrides the depth of this section's occupiable cuboid " +
            "when a product is being forced to be compatible with the " +
            "cabinet. Some cabinets are shallower than the product we need " +
            "to place in them, so the fit check would otherwise reject the " +
            "product. Set this to the depth (in meters) the section should " +
            "pretend to have so the forced product still fits. Leave at 0 " +
            "to use the section's real depth.")]
        [SerializeField]
        private float m_ForcedCabinetCuboidDepthInMeters;

        [SerializeField]
        private string m_BrandCatalogName;

        // --------------------------------------------------------------------
        [Header("Section Door")]

        [Tooltip("If this is a section door, this field is the name of the " +
            "style this door represents. This will match the part of the " +
            "asset name that follows the @ symbol. Otherwise, this is blank.")]
        [SerializeField]
        protected string m_SectionDoorStyleName = default;

        [SerializeField]
        protected string m_SectionDoorStyleDisplayName = default;

        [Header("End Panel")]
        [Tooltip(@"If this cabinet is in the middle of a run, this is the
            size of the end panel that is used to close off the cabinet.")]
        [SerializeField]
        protected float m_SectionEndPanelSizeInches = default;


#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        public void EditorOnlySetId(string id)
        {
            m_Id = id;
        }

        public void EditorOnlySetBrandCatalogName(string name)
        {
            m_BrandCatalogName = name;
        }

        public void EditorOnlySetDoorStyleName(string name)
        {
            m_SectionDoorStyleName = name;
        }

        public void EditorOnlySetExpectingDoors(bool expectingDoors)
        {
            m_ExpectingCabinetDoors = expectingDoors;
        }

        public void EditorOnlySetSectionOccupiableCuboidPaddingInInches(CuboidPadding paddingInInches)
        {
            m_SectionOccupiableCuboidPaddingInInches = paddingInInches;
        }

        public void EditorOnlySetThumbnail(Sprite thumbnail)
        {
            m_Thumbnail = thumbnail;
        }

        public void EditorOnlySetRuntimePrefabCategory(ProductCategorySpecific specificCategory)
        {
            m_RuntimePrefabCategory.value = specificCategory;
        }

        public void EditorOnlySetExpectingCabinetDoorss(bool expect)
        {
            m_ExpectingCabinetDoors = expect;
        }

        public static class PropertyName
        {
            public const string Id = nameof(m_Id);
            public const string Anchor = nameof(m_Anchor);
            public const string Thumbnail = nameof(m_Thumbnail);
        }

#endif  // if UNITY_EDITOR

    }   // class ProductVisualInfo

}   // namespace KitchenDesign

