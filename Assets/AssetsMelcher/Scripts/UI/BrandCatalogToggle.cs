namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Serializables;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    ///     References a <see cref="BrandCatalog"/>. This is intended to be
    ///     attached to the toggle that the user picks to choose a particular
    ///     catalog.
    /// </summary>
    public class BrandCatalogToggle : MelcherMonoBehaviour
    {
        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        ///     Get/set the brand catalog. This should be assigned a value
        ///     from <see cref="ProFitGlobals.runtimeBrandCatalogs"/>.
        /// </summary>
        public BrandCatalog brandCatalog
        {
            get => m_BrandCatalog;
            set
            {
                m_BrandCatalog = value;
                m_CatalogLabel.phrase = m_BrandCatalog ? m_BrandCatalog.displayName : "<null>";

                Sprite catalogLogo = null;

                // Try to lookup a logo for this (possibly) user-defined catalog.
                if (m_BrandCatalog &&
                    ProFitGlobals.appProfile.brandCatalogLogoDatabase.Find(brandCatalog.catalogId, out GroupInfo catalogLogoInfo))
                {
                    catalogLogo = catalogLogoInfo.sprite;
                }

                if (!catalogLogo)
                {
                    // No logo was found, fall back to a default image.
                    catalogLogo = ProFitGlobals.appProfile.brandCatalogLogoDatabase.defaultSprite;
                }

                m_CatalogImage.sprite = catalogLogo;

                // If no image, show the "image coming soon" text.
                m_ImageComingSoonLabel.gameObject.SetActive(!catalogLogo);
            }
        }

        public MelcherToggleGroup toggleGroup
        {
            get => m_Toggle.toggleGroup;
            set => m_Toggle.toggleGroup = value;
        }

        // ====================================================================
        // Methods
        // ====================================================================

        public void SetComingSoon(bool comingSoon)
        {
            m_Toggle.enabled = !comingSoon;
            m_CatalogImage.material = comingSoon ? m_GreyedOutImageMaterial : null;
            m_CatalogComingSoonLabel.gameObject.SetActive(comingSoon);
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        protected BrandCatalog m_BrandCatalog;

        [SerializeField]
        protected Image m_CatalogImage;

        [SerializeField]
        protected MelcherLocalizedText m_ImageComingSoonLabel = default;

        [SerializeField]
        protected MelcherLocalizedText m_CatalogComingSoonLabel = default;

        [SerializeField]
        protected MelcherLocalizedText m_CatalogLabel = default;

        [Tooltip("The material that is used to grey out the image when the " +
            "catalog is not fully defined (i.e., coming soon).")]
        [SerializeField]
        protected Material m_GreyedOutImageMaterial;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down)]
        protected MelcherToggle m_Toggle = default;

    }   // class BrandCatalogToggle

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-06-19
/// ===========================================================================
