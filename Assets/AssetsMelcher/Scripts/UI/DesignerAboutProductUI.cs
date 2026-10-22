namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A prompt for the designer scene to ensure a user fully intends an
    /// action (quit or restart).
    /// </summary>
    public class DesignerAboutProductUI : BasePromptUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ShowProductInfo(ProductInfo productInfo, ProductVisual productVisual)
        {
            if (!ProFitGlobals.catalogData.FindManufacturerById(productInfo.manufacturerId, out ManufacturerInfo manufacturerInfo))
            {
                manufacturerInfo.name = "Unknown Manufacturer";
            }

            m_SelectedProductServerId = productInfo.id;
            m_SelectedProductVisual = productVisual;

            m_NameLabel.phrase  = productInfo.name;
            m_IdLabel.phrase    = manufacturerInfo.name + " - " + productInfo.sku;

            m_PriceLabel.phrase =
                productInfo.priceCustomer > 0.0f ?
                productInfo.priceCustomer.ToString("C2") :
                "";

            // Hide the image if we don't have an icon
            m_Image.sprite = productVisual.info.thumbnail;
            m_Image.gameObject.SetActive(m_Image.sprite);

            Sprite overlaySprite = null;
            if (productInfo.category == ProductCategory.CabinetSection)
            {
                Kitchen kitchen = Kitchen.instance;

                ProductVisual cabinetDoorVisual =
                    CabinetSectionDoorUtility.FindDoorStyleVisual(
                        kitchen.productVisualDatabase.runtimeEntries,
                        productVisual,
                        kitchen.selectedBrandCatalog,
                        kitchen.selectedDoorStyleName
                    );

                overlaySprite = cabinetDoorVisual?.info.thumbnail;
            }

            m_ImageOverlay.sprite = overlaySprite;
            m_ImageOverlay.gameObject.SetActive(overlaySprite);

            // Scroll the description view back to the top
            m_DescriptionScrollView.horizontalNormalizedPosition = 1.0f;
            m_DescriptionLabel.phrase = productInfo.description;

            Show();
        }

        protected override void Awake()
        {
            base.Awake();

            bool showDeveloperButtons = Application.isEditor || Debug.isDebugBuild;

            m_ProductServerLinkButton.gameObject.SetActive(showDeveloperButtons);
            m_ProductAssetLinkButton.gameObject.SetActive(showDeveloperButtons);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EventBridge.Designer.onAboutProduct += ShowProductInfo;

            m_ProductServerLinkButton.onClick += OnClickServerLink;
            m_ProductAssetLinkButton.onClick += OnClickAssetLink;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EventBridge.Designer.onAboutProduct -= ShowProductInfo;

            m_ProductServerLinkButton.onClick -= OnClickServerLink;
            m_ProductAssetLinkButton.onClick -= OnClickAssetLink;
        }

        protected void OnClickServerLink(MelcherButton caller)
        {
            ProFitUtility.OpenProductUrl(m_SelectedProductServerId);
        }

        protected void OnClickAssetLink(MelcherButton caller)
        {
#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(m_SelectedProductVisual);

            // Open the asset for viewing/editing.
            UnityEditor.AssetDatabase.OpenAsset(m_SelectedProductVisual.contentAddress.editorOnlyCachedAsset);
#endif  // if UNITY_EDITOR
        }


        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DesignerAboutProductUI))]

        [UnityEngine.Serialization.FormerlySerializedAs("m_ProductVisualCatalog")]
        [SerializeField, MelcherAssetField]
        protected ProductVisualDatabase m_ProductVisualDatabase = null;

        [Space]
        [SerializeField] protected MelcherLocalizedText m_NameLabel = null;
        [SerializeField] protected MelcherLocalizedText m_IdLabel = null;
        [SerializeField] protected MelcherLocalizedText m_PriceLabel = null;
        [SerializeField] protected Image m_Image = null;
        [SerializeField] protected Image m_ImageOverlay = null;
        [SerializeField] protected ScrollRect m_DescriptionScrollView = null;
        [SerializeField] protected MelcherLocalizedText m_DescriptionLabel = null;
        [SerializeField] protected MelcherButton m_ProductServerLinkButton = null;
        [SerializeField] protected MelcherButton m_ProductAssetLinkButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        private int m_SelectedProductServerId;
        private ProductVisual m_SelectedProductVisual;

    }   // class DesignerAboutProductUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
