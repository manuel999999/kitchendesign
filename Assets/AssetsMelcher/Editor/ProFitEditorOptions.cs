namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CreateAssetMenu(fileName = "ProFitEditorOptions", menuName = "Melcher Studios/Pro-Fit/Editor Options")]

    public class ProFitEditorOptions : MelcherScriptableObject
    {
        // =====================================================================
        // Static functions
        // =====================================================================

        public static ProFitEditorOptions Get()
        {
            if (!s_Instance)
            {
                string[] guids = AssetDatabase.FindAssets($"t:{nameof(ProFitEditorOptions)}");
                for (int i = 0; i < guids.Length && !s_Instance; ++i)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    s_Instance = AssetDatabase.LoadAssetAtPath<ProFitEditorOptions>(assetPath);
                }
            }

            return s_Instance;
        }

        // =====================================================================
        // Properties
        // =====================================================================

        public string dataRelativeProductModelsFolder => m_ProductModelsFolder.dataRelativePath;
        public string dataRelativeCabinetSectionModelsFolder => m_CabinetSectionModelsFolder.dataRelativePath;
        public string dataRelativeCabinetSectionLegModelsFolder => m_CabinetSectionLegModelsFolder.dataRelativePath;
        public string dataRelativeFinishesFolder => m_FinishesFolder.dataRelativePath;
        public string dataRelativeProductVisualsFolder => m_ProductVisualsFolder.dataRelativePath;

        public Material defaultKitchenMaterial => m_DefaultKitchenMaterial;
        public Material defaultProductMaterial => m_DefaultProductMaterial;
        public Material productCabinetMaterialIdentifier => m_ProductCabinetMaterialIdentifier;

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Tooltip("The material to apply to cabinet sections.")]
        [SerializeField]
        protected Material m_DefaultKitchenMaterial = default;

        [Tooltip("Replace Unity's default FBX material with this on FBX's " +
            "that are inside the product model folder.")]
        [SerializeField]
        protected Material m_DefaultProductMaterial = default;

        [Tooltip("Replace Unity's default FBX material with this on FBX's " +
            "that are inside the product model folder, and that represent a " +
            "Cabinet Door product. Cabinet doors are used in Catalog mode " +
            "and are intended to match the cabinet's finish.")]
        [SerializeField]
        protected Material m_ProductCabinetMaterialIdentifier = default;

        [UnityEngine.Serialization.FormerlySerializedAs("m_Folder")]
        [SerializeField]
        protected MelcherPathField<DefaultAsset> m_ProductModelsFolder = default;

        [SerializeField]
        protected MelcherPathField<DefaultAsset> m_CabinetSectionModelsFolder = default;

        [SerializeField]
        protected MelcherPathField<DefaultAsset> m_CabinetSectionLegModelsFolder = default;

        [SerializeField]
        protected MelcherPathField<DefaultAsset> m_FinishesFolder = default;

        [SerializeField]
        protected MelcherPathField<DefaultAsset> m_ProductVisualsFolder = default;

        // =====================================================================
        // Static fields
        // =====================================================================

        protected static ProFitEditorOptions s_Instance;

    }   // class ProFitEditorOptions

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-23
/// ===========================================================================
