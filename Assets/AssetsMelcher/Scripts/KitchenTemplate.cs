namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using UnityEngine;

    [CreateAssetMenu(fileName = nameof(KitchenTemplate), menuName = "Melcher Studios/Pro-Fit/Kitchen Template")]
    public class KitchenTemplate : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public BrandCatalog brandCatalog => m_BrandCatalog;
        public KitchenStyle style => m_KitchenStyle;
        public Sprite previewSprite => m_PreviewSprite;
        public TextAsset saveData => m_SaveData;
        public string displayName => m_DisplayName;

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        protected BrandCatalog m_BrandCatalog = default;

        [SerializeField]
        protected MelcherStableEnum<KitchenStyle> m_KitchenStyle = new (KitchenStyle.L);

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_PreviewImage")]
        protected Sprite m_PreviewSprite = default;

        [SerializeField]
        protected TextAsset m_SaveData = default;

        [SerializeField]
        protected string m_DisplayName = default;

    }   // class KitchenTemplate

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-12-02
/// ===========================================================================
