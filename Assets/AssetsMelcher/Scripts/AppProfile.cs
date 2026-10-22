namespace KitchenDesign
{
    using Melcher;
    using Cysharp.Threading.Tasks;
    using System.Collections.ObjectModel;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    ///     This class is responsible for pointing at the <see
    ///     cref="AppOptions"/> that will actually be used by the app.
    ///     This allows multiple app options to exist and be easily swapped out
    ///     (e.g., building a Stoll build or Pro-Fit build) without needing to
    ///     update every field in the AppOptions object.
    /// </summary>
    [CreateAssetMenu(fileName = "ProFitAppProfile", menuName = "Melcher Studios/Pro-Fit/App Profile")]
    public class AppProfile : MelcherScriptableObject
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public AppOptions appOptions => m_AppOptions;

        public ServerType targetServerType => m_TargetServerType;

        public int defaultManufacturerId =>
            m_AppOptions.GetDefaultManufacturerId(m_TargetServerType);

        public string serverDomain =>
            m_AppOptions.GetServerDomain(m_TargetServerType);

        public GroupDatabase manufacturerDatabase =>
            m_AppOptions.GetManufacturerDatabase(m_TargetServerType);

        public GroupDatabase productGroupDatabase =>
            m_AppOptions.GetProductGroupDatabase(m_TargetServerType);

        public UniversalRenderPipelineAsset screenshotQuality => m_ScreenshotQuality;

        public UniversalRenderPipelineAsset runtimeQuality => m_RuntimeQuality;


        public ReadOnlyCollection<BrandCatalog> presetBrandCatalogs => m_PresetBrandCatalogs.readOnly;

        public GroupDatabase brandCatalogLogoDatabase =>
            m_AppOptions.GetBrandCatalogLogoDatabase(m_TargetServerType);

#if UNITY_EDITOR

        public UnityEditor.SceneAsset screenshotScene => m_ScreenshotScene;

#endif  // if UNITY_EDITOR

        // ====================================================================
        // Methods
        // ====================================================================

        public async UniTask<Material> GetDefaultKitchenMaterial()
        {
            if (!m_KitchenDefaultMaterialOperation.IsValid())
            {
                m_KitchenDefaultMaterialOperation =
                    Addressables.LoadAssetAsync<Material>(m_KitchenDefaultMaterial);
            }

            await m_KitchenDefaultMaterialOperation;

            return m_KitchenDefaultMaterialOperation.Result;
        }

        public async UniTask<Material> GetDefaultOccupantMaterial()
        {
            if (!m_OccupantDefaultMaterialOperation.IsValid())
            {
                m_OccupantDefaultMaterialOperation =
                    Addressables.LoadAssetAsync<Material>(m_OccupantDefaultMaterial);
            }

            await m_OccupantDefaultMaterialOperation;

            return m_OccupantDefaultMaterialOperation.Result;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField]
        protected AppOptions m_AppOptions = default;

        [SerializeField]
        protected ServerType m_TargetServerType = default;

        [Space]

        [Tooltip("A nicer quality intended to be used only when rendering " +
            "screenshots.")]
        [SerializeField]
        protected UniversalRenderPipelineAsset m_ScreenshotQuality = default;

        [Tooltip("A quality level that is performant for general use on " +
            "all platforms: PC, mobile, and web.")]
        [SerializeField]
        protected UniversalRenderPipelineAsset m_RuntimeQuality = default;

        [Tooltip("Used by Product Visuals that sometimes inherit the " +
            "the cabinet finish on some of their renderers (Stoll storage). " +
            "These products use one material to \"mark\" these renderers " +
            "but that material needs to be swapped for the default product " +
            "material when the product should not inherit the cabinet " +
            "finish. This is intended to be a brushed stainless steel " +
            "material.")]
        [SerializeField]
        protected AssetAddress<Material> m_OccupantDefaultMaterial = default;

        [Tooltip("The material that is used by default on kitchens. This is " +
            "also used to \"mark\" occupants with faces that should be " +
            "rendered with the same material as the cabinet finish.")]
        [SerializeField]
        protected AssetAddress<Material> m_KitchenDefaultMaterial = default;

        [Space]

        [Tooltip("These are the preset brand catalogs that are setup in the " +
            "Unity editor. At runtime, these may be cloned by server-defined " +
            "catalogs and will be accessible from ProFitGlobals.runtimeBrandCatalogs")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_BrandCatalogs")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_BrandCatalogDatabase")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_BrandCatalogPresets")]
        protected MelcherReadWriteList<BrandCatalog> m_PresetBrandCatalogs = new ();

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        private AsyncOperationHandle<Material> m_OccupantDefaultMaterialOperation;

        private AsyncOperationHandle<Material> m_KitchenDefaultMaterialOperation;


#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        /// <summary>
        ///     The asset path of the finishes folder. This is relative to the
        ///     project's root.
        /// </summary>
        public string finishesFolderPath => UnityEditor.AssetDatabase.GetAssetPath(m_FinishesFolder);

        /// <summary>
        ///     The asset path of the models folder. This is relative to the
        ///     project's root.
        /// </summary>
        public string productModelFolderPath => UnityEditor.AssetDatabase.GetAssetPath(m_ProductModelFolder);

        /// <summary>
        ///     The asset path of the cabinet section's models folder. This is
        ///     relative to the project's root.
        /// </summary>
        public string cabinetSectionModelFolderPath => UnityEditor.AssetDatabase.GetAssetPath(m_CabinetSectionModelFolder);

        /// <summary>
        ///     The asset path of the product visual folder. This is relative
        ///     to the project's root.
        /// </summary>
        public string productVisualFolderPath => UnityEditor.AssetDatabase.GetAssetPath(m_ProductVisualFolder);

        public Shader triplanarShader => m_TriplanarShader;

        public Shader litShader => m_LitShader;

        public Shader simpleLitShader => m_SimpleLitShader;

        public Shader unlitShader => m_UnlitShader;

        public Material editorOnlyDefaultKitchenMaterial => m_KitchenDefaultMaterial.editorOnlyCachedAsset;

        public Material editorOnlyDefaultOccupantMaterial => m_OccupantDefaultMaterial.editorOnlyCachedAsset;

        [Header("Editor-only")]

        [SerializeField]
        protected UnityEditor.SceneAsset m_ScreenshotScene = default;

        [Space]


        [SerializeField]
        private UnityEditor.DefaultAsset m_FinishesFolder;

        [SerializeField]
        private UnityEditor.DefaultAsset m_ProductModelFolder;

        [SerializeField]
        private UnityEditor.DefaultAsset m_CabinetSectionModelFolder;

        [SerializeField]
        private UnityEditor.DefaultAsset m_ProductVisualFolder;

        [Space]

        [SerializeField]
        private Shader m_TriplanarShader;

        [SerializeField]
        private Shader m_LitShader;

        [SerializeField]
        private Shader m_SimpleLitShader;

        [SerializeField]
        private Shader m_UnlitShader;

#endif  // if UNITY_EDITOR

    }   // class ProFitAppProfile

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic
/// Initial creation date: 2024-01-23
/// ===========================================================================
