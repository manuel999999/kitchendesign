namespace KitchenDesign.Scriptables
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Scriptables;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;
    using KitchenDesign.Serializables;

    /// <summary>
    ///     This is the app-local information about a Brand Catalog. Not to be
    ///     confused with a <see cref="BrandCatalogInfo"/>, which is a server
    ///     provided view of the brand catalog and contains much less
    ///     information than this scriptable object.
    /// </summary>
    [CreateAssetMenu(fileName = "BrandCatalog", menuName = "Melcher Studios/Pro-Fit/Brand Catalog")]
    public class BrandCatalogSO : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public string catalogName
        {
            get => m_CatalogName;
            set => m_CatalogName = value;
        }

        public string displayName =>
            string.IsNullOrEmpty(m_CatalogLabel) ? m_CatalogName : m_CatalogLabel;

        public int catalogId
        {
            get => m_CatalogId;
            set => m_CatalogId = value;
        }

        public BrandCatalogRuleset ruleset => m_Ruleset.value;

        public int cabinetManufacturerId => m_CabinetManufacturerId;

        public Sprite BrandCatalogLoadingImage => m_BrandCatalogLoadingImage;

        public string defaultCabinetSectionDoorStyleName =>
            m_DefaultCabinetSectionDoorStyleName;

        public string defaultCabinetSectionDoorHandleStyleName =>
            m_DefaultCabinetSectionDoorHandleStyleName;

        public CabinetToeKickType defaultToeKickType => m_DefaultToeKickType;

        public RiserMaterialMode backingRiserMaterialMode => m_BackingRiserMaterialMode;

        public bool showToeKickButton => m_ShowToeKickButton;

        public bool hasDoorStyles => m_HasDoorStyles;

        public bool hasDoorHandleStyles => m_HasDoorHandleStyles;

        public bool hasCabinetFinishes => m_HasCabinetFinishes;

        public bool hasEndPanelsOption => m_HasEndPanelsOption;
        
        public bool hasBackPanelsOption => m_HasBackPanelsOption;

        public bool hasGrillsWithInsulatedJackets => m_HasGrillsWithInsulatedJackets;

        public bool hasBurnersWithInsulatedJackets => m_HasBurnersWithInsulatedJackets;

        public bool hasCookware => m_HasCookware;

        public bool hasSinksAndFridges => m_HasSinksAndFridges;

        public bool hasBacksplashes => m_HasBacksplashes;

        public bool hasHighBarTops => m_HasHighBarTops;

        public bool hasLowBarTops => m_HasLowBarTops;

        public bool hasGrills => m_HasGrills;

        public bool hasBurners => m_HasBurners;

        public Material defaultCabinetFinishOverride =>
            m_DefaultCabinetFinishOverride;

        public CabinetSectionDoorStyleSO cabinetSectionDoorStyle =>
            m_CabinetSectionDoorStyle;

        public CabinetSectionDoorHandleStyleSO cabinetSectionDoorHandleStyle =>
            m_CabinetSectionDoorHandleStyle;

        public CabinetSectionLegStyleSO cabinetSectionLegStyle =>
            m_CabinetSectionLegStyle;

        public CabinetBacking highBarTopOverridePrefab =>
            m_HighBarTopOverridePrefab;

        public ReadOnlyCollection<int> whitelistedHighBarTopCountertopManufacturers =>
            m_WhitelistedHighBarTopCountertopManufacturers.readOnly;

        public float standardCabinetDepthInMeters =>
            m_StandardCabinetDepthInInches * Units.Inches2Meters;

        public float standardCabinetDepthInInches =>
            m_StandardCabinetDepthInInches;

        public float standardCabinetHeightInMeters =>
            m_StandardCabinetHeightInInches * Units.Inches2Meters;


        public float highBarTopOverhangInInches => m_HighBarTopOverhangInInches;

        public float highBarTopOverhangInMeters => m_HighBarTopOverhangInInches * Units.Inches2Meters;

        public float lowBarTopLengthInInches => m_LowBarTopLengthInInches;

        public float lowBarTopLengthInMeters => m_LowBarTopLengthInInches * Units.Inches2Meters;

        public float cabinetCountertopThicknessInInches => m_CabinetCountertopThicknessInInches;

        public string fallbackDefaultCabinetSectionProductSku =>
            m_FallbackDefaultCabinetSectionProductSku;

        public int fallbackDefaultCabinetSectionProductServerId =>
            m_FallbackDefaultCabinetSectionProductServerId;

        public ReadOnlyCollection<DoorStyleInfo> doorStyles => m_DoorStyles.readOnly;
        public ReadOnlyCollection<DoorStyleInfo> doorHandleStyles => m_DoorHandleStyles.readOnly;

        public MeshRenderer cabinetVentPrefab => m_CabinetVentPrefab;

        public float cabinetVentDistanceFromEdgeInMeters => m_CabinetVentDistanceFromEdgeInInches * Units.Inches2Meters;

        public bool showFractionInMeasurements => m_ShowFractionInMeasurements;

        public bool endPanelsTouchGround => m_EndPanelsTouchGround;

        public IReadOnlyCollection<FridgePanelInfo> fridgeBackPanelInfos => m_FridgeBackPanelInfos;

        public FridgePanelInfo fridgeEndPanelInfo => m_FridgeEndPanelInfo;

        public FridgePanelInfo cabinetEndPanelInfo => m_CabinetEndPanelInfo;

        public EndPanelsInfo cabinetRuntimeEndPanelInfo => m_CabinetRuntimeEndPanelInfo;

        public BackPanelsInfo cabinetRuntimeBackPanelInfo => m_CabinetRuntimeBackPanelInfo;

        public ReadOnlyCollection<KitchenTemplateSO> kitchenTemplates => m_KitchenTemplates.readOnly;

        public BrandCatalogInfo info => ProFitGlobals.serverBrandCatalogs.Find(x => x.id == m_CatalogId);

        // ====================================================================
        // Methods
        // ====================================================================

        public void PopulateRuntimeData(
            IReadOnlyList<ProductInfo> cabinetSectionProductInfos,
            IReadOnlyList<ProductVisualSO> cabinetSectionVisuals,
            IReadOnlyList<ProductVisualSO> cabinetDoorVisuals)
        {
            m_DoorStyles.mutable.Clear();
            m_DoorHandleStyles.mutable.Clear();

            RegisterDoorHandleStyles();
            CabinetSectionDoorUtility.TryInitialize(this);

            foreach (ProductInfo cabinetSectionProductInfo in cabinetSectionProductInfos)
            {
                if (cabinetSectionProductInfo.manufacturerId == cabinetManufacturerId)
                {
                    if (string.IsNullOrEmpty(cabinetSectionProductInfo.visualId))
                    {
                        // We are going to skip printing an error message if the
                        // visual ID is completely empty. This is because there
                        // are seemingly a lot of products that have been added
                        // to the dashboard recently which do not have a product
                        // visual ID, intentional maybe, so there's no point in
                        // polluting the console logs here.
                        continue;
                    }

                    ProductVisualSO cabinetSectionVisual =
                        Products.ProductVisualLookups.FindById(
                            cabinetSectionVisuals,
                            cabinetSectionProductInfo.visualId
                        );

                    if (!cabinetSectionVisual)
                    {
                        MelcherLog.LogError($"Could not find product visual for cabinet section {cabinetSectionProductInfo.name} using visual ID: {cabinetSectionProductInfo.visualId}");
                        continue;
                    }

                    if (!cabinetSectionVisual.info.expectingCabinetDoors)
                    {
                        continue;
                    }

                    string cabinetDoorNamePrefix = cabinetSectionVisual.info.name + "@";

                    string usedCustomDoorId = CabinetSectionDoorUtility.FindMappedDoorId(cabinetSectionVisual.info.id);

                    if(usedCustomDoorId != string.Empty && cabinetSectionDoorStyle != null)
                    {
                        cabinetDoorNamePrefix = $"ProductVisual_{usedCustomDoorId}@";
                    }

                    bool didRegisterAnyDoors = false;

                    foreach (ProductVisualSO doorVisual in cabinetDoorVisuals)
                    {
                        if (doorVisual.info.name.StartsWith(cabinetDoorNamePrefix))
                        {
                            RegisterDoorStyle(doorVisual);
                            didRegisterAnyDoors = true;
                        }
                    }

                    if (!didRegisterAnyDoors)
                    {
                        MelcherLog.LogError($"Did not find a door whose name started with: {cabinetDoorNamePrefix}");
                    }
                }
            }
        }

        protected void RegisterDoorStyle(ProductVisualSO doorStyleVisual)
        {
            // Look for an existing door style collection for this style name.
            DoorStyleInfo doorStyleInfo =
                MelcherCommon.Find(
                    m_DoorStyles.readOnly,
                    x => x.name == doorStyleVisual.info.sectionDoorStyleName
                );

            if (doorStyleInfo == null)
            {
                // No door style collection exists with this name so make one.
                doorStyleInfo =
                    new DoorStyleInfo(
                        doorStyleVisual.info.sectionDoorStyleName,
                        doorStyleVisual.info.sectionDoorStyleDisplayName
                    );

                m_DoorStyles.mutable.Add(
                    doorStyleInfo
                );

                MelcherLog.Log($"Adding {doorStyleVisual.info.sectionDoorStyleName} door style to {catalogName} catalog");
            }

            doorStyleInfo.AddVisual(doorStyleVisual);
        }

        protected void RegisterDoorHandleStyles()
        {
            if(m_CabinetSectionDoorHandleStyle == default)
            {
                return;
            }

            Transform defaultHandle = m_CabinetSectionDoorHandleStyle.defaultPrefab.prefab;

            int childCount = defaultHandle.childCount;

            for(int i = 0; i < childCount; i++)
            {
                string handleName = defaultHandle.GetChild(i).name;   
                string[] split = handleName.Split('@');

                if(split.Length < 2)
                {
                    continue;
                }

                string handleStyle = split[1];

                DoorStyleInfo doorHandleStyleInfo =
                    new DoorStyleInfo(
                        handleStyle
                    );

                m_DoorHandleStyles.mutable.Add(
                    doorHandleStyleInfo
                );

                MelcherLog.Log($"Adding {handleStyle} door handle style to {catalogName} catalog");
            }
        }

        public FridgePanelInfo FindFirstFridgeBackPanel(float requiredWidthInMeters)
        {
            foreach (FridgePanelInfo fridgePanel in m_FridgeBackPanelInfos)
            {
                if (fridgePanel == null)
                {
                    continue;
                }

                float specificPanelWidthInMeters =
                    fridgePanel.specificPanelWidthInMeters;

                if (specificPanelWidthInMeters <= 0.0f ||
                    requiredWidthInMeters <= specificPanelWidthInMeters)
                {
                    return fridgePanel;
                }
            }

            return null;
        }

        public FridgePanelInfo FindFirstCabinetBackPanel(float requiredWidthInMeters, int cabinetSectionCategoryId)
        {         
            foreach(BackPanelInfo backPanelInfo in m_CabinetRuntimeBackPanelInfo.backPanels)
            {
                if(backPanelInfo.categoryId != cabinetSectionCategoryId)
                {
                    continue;
                }

                foreach(FridgePanelInfo fridgePanel in backPanelInfo.backPanels)
                {
                    if (fridgePanel == null)
                    {
                        continue;
                    }

                    float specificPanelWidthInMeters =
                        fridgePanel.specificPanelWidthInMeters;

                    if (requiredWidthInMeters == specificPanelWidthInMeters)
                    {
                        return fridgePanel;
                    }
                }
            }

            return FindFirstCabinetBackPanel(requiredWidthInMeters);
        }

        public FridgePanelInfo FindFirstCabinetBackPanelByDepth(float requiredDepthInMeters, int cabinetSectionCategoryId)
        {         
            foreach(BackPanelInfo backPanelInfo in m_CabinetRuntimeBackPanelInfo.backPanels)
            {
                if(backPanelInfo.categoryId != cabinetSectionCategoryId)
                {
                    continue;
                }

                foreach(FridgePanelInfo fridgePanel in backPanelInfo.backPanels)
                {
                    if (fridgePanel == null)
                    {
                        continue;
                    }

                    float specificPanelWidthInMeters =
                        fridgePanel.specificPanelWidthInMeters;

                    if (requiredDepthInMeters == specificPanelWidthInMeters)
                    {
                        return fridgePanel;
                    }
                }
            }

            return FindFirstCabinetBackPanel(requiredDepthInMeters);
        }

        public FridgePanelInfo FindFirstCabinetBackPanel(float requiredWidthInMeters)
        {
            FridgePanelInfo[] collection = m_CabinetRuntimeBackPanelInfo.defaultBackPanels.backPanels;

            if(collection.Length == 0)
            {
                return null;
            }

            foreach(FridgePanelInfo fridgePanel in collection)
            {
                if (fridgePanel == null)
                {
                    continue;
                }

                float specificPanelWidthInMeters =
                    fridgePanel.specificPanelWidthInMeters;

                if (requiredWidthInMeters == specificPanelWidthInMeters)
                {
                    return fridgePanel;
                }
            }

            return null;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        protected string m_CatalogName = default;

        [Tooltip("The name shown in the catalog selection window. Leave " +
            "empty to show the catalog name.")]
        [SerializeField]
        protected string m_CatalogLabel = default;

        [SerializeField]
        protected int m_CatalogId = default;

        [Tooltip("Indicate how this catalog will resolve certain " +
            "features. E.g., Challenger catalogs do not add any padding to " +
            "their grills.")]
        [SerializeField]
        protected MelcherStableEnum<BrandCatalogRuleset> m_Ruleset =
            new MelcherStableEnum<BrandCatalogRuleset>(BrandCatalogRuleset.Unknown);

        [Tooltip("This is the ID of the manufacturer whose cabinets are " +
            "exclusively allowed in this catalog. This value is used to " +
            "match up the server-defined brand catalogs (BrandCatalogInfo) " +
            "with the app-local brand catalog (BrandCatalog).")]
        [SerializeField]
        protected int m_CabinetManufacturerId = default;

        [SerializeField]
        protected Sprite m_BrandCatalogLoadingImage = default;

        [Space]

        [SerializeField]
        protected string m_DefaultCabinetSectionDoorStyleName = "";

        [SerializeField]
        protected string m_DefaultCabinetSectionDoorHandleStyleName = "";

        [SerializeField]
        protected MelcherStableEnum<CabinetToeKickType> m_DefaultToeKickType = new (CabinetToeKickType.None);

        [SerializeField]
        private MelcherStableEnum<RiserMaterialMode> m_BackingRiserMaterialMode = new (RiserMaterialMode.KeepUnchanged);

        [SerializeField]
        protected bool m_ShowToeKickButton = true;

        [SerializeField]
        protected bool m_HasDoorStyles = true;

        [SerializeField]
        protected bool m_HasDoorHandleStyles = true;

        [SerializeField]
        protected bool m_HasCabinetFinishes = true;

        [SerializeField]
        protected bool m_HasEndPanelsOption = false;

        [SerializeField]
        protected bool m_HasBackPanelsOption = false;

        [SerializeField]
        protected bool m_HasGrillsWithInsulatedJackets = true;

        [SerializeField]
        protected bool m_HasBurnersWithInsulatedJackets = true;

        [SerializeField]
        protected bool m_HasCookware = true;

        [SerializeField]
        protected bool m_HasSinksAndFridges = true;

        [SerializeField]
        protected bool m_HasBacksplashes = true;

        [SerializeField]
        protected bool m_HasHighBarTops = true;

        [SerializeField]
        protected bool m_HasLowBarTops = true;

        [SerializeField]
        protected bool m_HasGrills = true;

        [SerializeField]
        protected bool m_HasBurners = true;

        [Tooltip("An optional material that is applied in place of the " +
            "default cabinet finish for this catalog. This is used by the " +
            "Napoleon catalog to match the corner sections to the authored " +
            "cabinets' carbon colour.")]
        [SerializeField]
        protected Material m_DefaultCabinetFinishOverride = default;

        [SerializeField]
        protected CabinetSectionDoorStyleSO m_CabinetSectionDoorStyle = default;

        [SerializeField]
        protected CabinetSectionDoorHandleStyleSO m_CabinetSectionDoorHandleStyle = default;

        [SerializeField]
        protected CabinetSectionLegStyleSO m_CabinetSectionLegStyle = default;

        [Tooltip("A custom high bar top used by this kitchen.")]
        [SerializeField, MelcherAssetField]
        protected CabinetBacking m_HighBarTopOverridePrefab = default;

        [Tooltip("By default, this array is empty, which means all countertop " +
            "manufacturers are allowed. If this array is not empty, only the " +
            "manufacturers with the specified IDs are allowed.")]
        [SerializeField]
        protected MelcherReadWriteList<int> m_WhitelistedHighBarTopCountertopManufacturers = new ();

        /// <summary>
        ///     The height of a standard cabinet. This is used to restore the
        ///     correct cabinet height for procedural corner cabinets that
        ///     don't otherwise have ProductInfo to refer to.
        /// </summary>
        [SerializeField]
        protected float m_StandardCabinetHeightInInches = 35.0f;

        /// <summary>
        ///     The depth of a standard cabinet. This is used to implement
        ///     bumped out cabinets.
        /// </summary>
        [SerializeField]
        protected float m_StandardCabinetDepthInInches = 28.0f;

        [SerializeField]
        protected float m_CabinetCountertopOverhangInInches = 1.0f;

        [SerializeField]
        private float m_HighBarTopOverhangInInches = 10.0f;

        [SerializeField]
        private float m_LowBarTopLengthInInches = 15.0f;

        /// <summary>
        /// Leave value to zero to allow countertop thickness selection
        /// </summary>
        [SerializeField]
        protected float m_CabinetCountertopThicknessInInches = 0f;

        [Space]

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_FallbackDefaultCabinetSectionProductId")]
        protected string m_FallbackDefaultCabinetSectionProductSku = default;

        [SerializeField]
        protected int m_FallbackDefaultCabinetSectionProductServerId = default;

        [SerializeField, MelcherAssetField]
        protected MeshRenderer m_CabinetVentPrefab = default;

        [SerializeField]
        protected float m_CabinetVentDistanceFromEdgeInInches = 2.0f;

        [SerializeField]
        protected bool m_ShowFractionInMeasurements = false;

        [SerializeField]
        protected bool m_EndPanelsTouchGround = false;

        [SerializeField]
        protected FridgePanelInfo[] m_FridgeBackPanelInfos = default;

        [Tooltip("A product that is automatically added to the PDF's manual " +
            "price list anytime a fridge cabinet neighbours a cabinet end.")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_FridgeSidePanelInfo")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_EndPanelInfo")]
        [SerializeField]
        protected FridgePanelInfo m_FridgeEndPanelInfo = default;

        [Tooltip("The width of an end cabinet (left to right). Some " +
            "catalogs, like Napoleon, have ends with actual width to them " +
            "which we account for here.")]
        [SerializeField]
        protected FridgePanelInfo m_CabinetEndPanelInfo = default;

        [SerializeField]
        protected EndPanelsInfo m_CabinetRuntimeEndPanelInfo = default;

        [SerializeField]
        protected BackPanelsInfo m_CabinetRuntimeBackPanelInfo = default;

        [SerializeField]
        protected MelcherReadWriteList<KitchenTemplateSO> m_KitchenTemplates = new ();

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected MelcherReadWriteList<DoorStyleInfo> m_DoorStyles = new ();
        protected MelcherReadWriteList<DoorStyleInfo> m_DoorHandleStyles = new ();

        // ====================================================================
        // Nested types
        // ====================================================================


        // TODO: Rename to "CabinetPanel" or something. This is to describe
        //       panels that attach to fridges at the end of a cabinet, or just
        //       a normal end panel on a regular cabinet.

        // ====================================================================
        // Editor-only
        // ====================================================================

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            // Default assign the catalog's name based on its asset's filename
            const string CatalogPrefix = "BrandCatalog_";

            if (string.IsNullOrEmpty(m_CatalogName) &&
                name.StartsWith(CatalogPrefix))
            {
                string catalogNameFromAssetName =
                    name.Substring(startIndex: CatalogPrefix.Length);

                if (m_CatalogName != catalogNameFromAssetName)
                {
                    m_CatalogName = catalogNameFromAssetName;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }

#endif  // if UNITY_EDITOR

    }   // class BrandCatalog

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-06-04
/// ===========================================================================
