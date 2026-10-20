namespace Melcher
{
    using System;
    using Cysharp.Threading.Tasks;
    using TMPro;
    using UnityEngine;

    public class ProductSearchUI : MelcherUI
    {
                // =====================================================================
        // Properties
        // =====================================================================

        protected bool m_IsSearching = false;

        // =====================================================================
        // Methods Methods
        // =====================================================================
        protected override void Awake()
        {
            base.Awake();

            m_SearchButton.onClick += OnClickSearch;
            m_SearchCancelButton.onClick += OnClickSearchCancel;
            m_ToolbarCloseButton.onClick += OnClickToolbarClose;
            EventBridge.Designer.showProductSearchUi += DoShow;
        }

        protected void OnDestroy()
        {
            m_SearchButton.onClick -= OnClickSearch;
            m_SearchCancelButton.onClick -= OnClickSearchCancel;
            m_ToolbarCloseButton.onClick -= OnClickToolbarClose;
            EventBridge.Designer.showProductSearchUi -= DoShow;
        }

        private void OnClickSearch(MelcherButton button)
        {
            if(m_IsSearching)
            {
                return;
            }

            OnClickSearchAsync().Forget();
        }

        private async UniTaskVoid OnClickSearchAsync()
        {
            string keyword = m_SearchKeywordField.text;
            bool hasItems = false;

            m_ProductSearchToolbar.DestroyScrollContent();

            if(keyword == null || keyword.Length == 0)
            {
                Debug.Log("Search keyword is empty, ignoring search.");
                return;
            }

            DisableSearchBox();

            await UniTask.Yield();

            foreach (ProductInfo productInfo in ProFitGlobals.runtimeProducts)
            {
                if (SearchCriteria(productInfo, keyword))
                {
                    if(productInfo.visualId == null || productInfo.visualId.Length == 0)
                    {
                        MelcherLog.LogError("Product Visual ID is null or empty for product: " + productInfo.sku);
                        continue;
                    }

                    if(ProFitGlobals.isAppOptionGemini && productInfo.category == ProductCategory.Storage)
                    {
                        continue;
                    }

#if !UNITY_EDITOR
                    if(productInfo.name.StartsWith("ProductVisual_"))
                    {
                        continue;
                    }
#endif

                    ProductVisual visual =
                        m_ProductVisualDatabase.FindRuntimeVisualByID(
                            productInfo.visualId
                        );

                    if (!visual)
                    {
                        MelcherLog.LogError("Product ID not found in Product Visual Database: " + productInfo.visualId, m_ProductVisualDatabase);
                        continue;
                    }

                    ProductVisual doorStyleVisual = null;
                    if(ProFitGlobals.isAppOptionGemini)
                    {
                        Kitchen kitchen = Kitchen.instance;

                        if(productInfo.cabinetSectionCategoryId > 0 && productInfo.manufacturerId != kitchen.selectedBrandCatalog.cabinetManufacturerId)
                        {
                            continue;
                        }

                        doorStyleVisual =
                            CabinetSectionDoorUtility.FindDoorStyleVisual(
                                kitchen.productVisualDatabase.runtimeEntries,
                                visual,
                                kitchen.selectedBrandCatalog,
                                kitchen.selectedDoorStyleName
                            );
                    }

                    ToolbarProductButton productButton =
                        Instantiate(
                            m_ProductButtonPrefab,
                            m_ProductSearchToolbar.GetScrollContent()
                        );
                    productButton.name = productInfo.sku + "Button";
                    productButton.productInfo = productInfo;
                    productButton.overlaySprite = doorStyleVisual ? doorStyleVisual.info.thumbnail : null;

                    if (visual && visual.info.thumbnail)
                    {
                        productButton.sprite = visual.info.thumbnail;
                    }

                    productButton.SetErrorHighlightVisible(
                        !visual || productInfo.hasErrors
                    );

                    hasItems = true;
                }

            }

            EnableSearchBox();
            m_ProductSearchToolbar.ResetScrollToStart();
            
            if(hasItems)
            {
                m_ProductSearchToolbar.SetVisibility(ToolbarContent.Visibility.InForeground, ToolbarContent.Duration.Tween);
                m_DesignerToolbarToggle.isOn = true;
                DoHide();
            }
            else
            {
                EventBridge.Designer.showToast?.Invoke("No products found matching the search criteria.");
            }
        }

        private bool SearchCriteria(ProductInfo product, string keyword)
        {
            return product.sku.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                product.name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        private void DisableSearchBox()
        {
            m_IsSearching = true;
            m_SearchContentGroup.interactable = false;
            m_SearchButton.interactable = false;
        }

        private void EnableSearchBox()
        {
            m_IsSearching = false;
            m_SearchContentGroup.interactable = true;
            m_SearchButton.interactable = true;
        }
        
        private void OnClickToolbarClose(MelcherButton button)
        {
            m_ProductSearchToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Tween);
        }

        private void OnClickSearchCancel(MelcherButton button)
        {
            DoHide();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ProductSearchUI))]
        [SerializeField, MelcherAssetField]
        protected ProductVisualDatabase m_ProductVisualDatabase = null;

        [Space]
        [SerializeField] protected TMP_InputField m_SearchKeywordField = null;
        [SerializeField] protected ToolbarContent m_ProductSearchToolbar = null;

        [Space]
        [SerializeField] protected MelcherToggle m_DesignerToolbarToggle = null;

        [Space]
        [SerializeField] protected MelcherButton m_SearchButton = null;
        [SerializeField] protected MelcherButton m_SearchCancelButton = null;
        [SerializeField] protected MelcherButton m_ToolbarCloseButton = null;

        [Space]

        [SerializeField] protected CanvasGroup m_SearchContentGroup = null;

        [Space]
        [SerializeField] protected ToolbarProductButton m_ProductButtonPrefab = null;
    }   // class ProductSearchUI

}   // namespace Melcher

