namespace KitchenDesign.Scriptables
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Serializables;
    using Cysharp.Threading.Tasks;
    using System.Collections.ObjectModel;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.ResourceManagement.AsyncOperations;

    [CreateAssetMenu(fileName = "ProFitAppProfile", menuName = "Melcher Studios/Pro-Fit/App Profile")]
    public class AppProfileSO : MelcherScriptableObject
    {
        public AppOptionsSO appOptions => m_AppOptions;

        public ServerType targetServerType => m_TargetServerType;

        public int defaultManufacturerId =>
            m_AppOptions.GetDefaultManufacturerId(m_TargetServerType);

        public string serverDomain =>
            m_AppOptions.GetServerDomain(m_TargetServerType);

        public GroupDatabaseSO manufacturerDatabase =>
            m_AppOptions.GetManufacturerDatabase(m_TargetServerType);

        public GroupDatabaseSO productGroupDatabase =>
            m_AppOptions.GetProductGroupDatabase(m_TargetServerType);

        public UniversalRenderPipelineAsset screenshotQuality => m_ScreenshotQuality;

        public UniversalRenderPipelineAsset runtimeQuality => m_RuntimeQuality;

        public ReadOnlyCollection<BrandCatalogSO> presetBrandCatalogs => m_PresetBrandCatalogs.readOnly;

        public GroupDatabaseSO brandCatalogLogoDatabase =>
            m_AppOptions.GetBrandCatalogLogoDatabase(m_TargetServerType);

#if UNITY_EDITOR

        public UnityEditor.SceneAsset screenshotScene => m_ScreenshotScene;

#endif

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

        public string GetAppResourcesName()
        {
            string appType = m_AppOptions.name.Substring("AppOptions_".Length);

            if (m_TargetServerType != ServerType.Production)
            {
                appType += $"_{m_TargetServerType}";
            }

            return appType;
        }

        public string GetFallbackJsonPath(string fileNameWithoutExtension)
        {
            const string FallbackResourceFolder = "FallbackJSON/";

            return System.IO.Path.Combine(GetAppResourcesName(), FallbackResourceFolder, fileNameWithoutExtension);
        }

        [SerializeField]
        protected AppOptionsSO m_AppOptions = default;

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
        protected MelcherReadWriteList<BrandCatalogSO> m_PresetBrandCatalogs = new ();

        private AsyncOperationHandle<Material> m_OccupantDefaultMaterialOperation;

        private AsyncOperationHandle<Material> m_KitchenDefaultMaterialOperation;

#if UNITY_EDITOR

        public string finishesFolderPath => UnityEditor.AssetDatabase.GetAssetPath(m_FinishesFolder);

        public string productModelFolderPath => UnityEditor.AssetDatabase.GetAssetPath(m_ProductModelFolder);

        public string cabinetSectionModelFolderPath => UnityEditor.AssetDatabase.GetAssetPath(m_CabinetSectionModelFolder);

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

#endif
    }
}

