namespace KitchenDesign.UI.DesignerScreens
{
    using KitchenDesign.Events;
    using KitchenDesign.CatalogData;
    using KitchenDesign.Shared.Interfaces;
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Server;
    using System;
    using Cysharp.Threading.Tasks;
    using TMPro;
    using UnityEngine;
    using KitchenDesign.KitchenModel;

    public class ProductSearchUI : MelcherUI
    {
        private IAppRules _rules;
        private CatalogDataStatus _catalogData;

        private Kitchen _kitchen;

        public void Initialize(Kitchen kitchen, IAppRules rules, CatalogDataStatus catalogData)
        {
            this._rules = rules;
            this._catalogData = catalogData;
            this._kitchen = kitchen;
        }

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
            EventDesigner.OnProductSearchRequested.AddListener(this.DoShow);
        }

        protected void OnDestroy()
        {
            m_SearchButton.onClick -= OnClickSearch;
            m_SearchCancelButton.onClick -= OnClickSearchCancel;
            m_ToolbarCloseButton.onClick -= OnClickToolbarClose;
            EventDesigner.OnProductSearchRequested.RemoveListener(this.DoShow);
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
#if UNITY_EDITOR
                Debug.Log("Search keyword is empty, ignoring search.");
#endif
                return;
            }

            DisableSearchBox();

            await UniTask.Yield();

            foreach (ProductInfo productInfo in this._catalogData.RuntimeProducts)
            {
                if (SearchCriteria(productInfo, keyword))
                {
                    if(productInfo.visualId == null || productInfo.visualId.Length == 0)
                    {
                        MelcherLog.LogError("Product Visual ID is null or empty for product: " + productInfo.sku);
                        continue;
                    }

                    if(!this._rules.ShowsStorageInProductSearch && productInfo.category == ProductCategory.Storage)
                    {
                        continue;
                    }

#if !UNITY_EDITOR
                    if(productInfo.name.StartsWith("ProductVisual_"))
                    {
                        continue;
                    }
#endif

                    ProductVisualSO visual =
                        m_ProductVisualDatabase.FindRuntimeVisualByID(
                            productInfo.visualId
                        );

                    if (!visual)
                    {
                        MelcherLog.LogError("Product ID not found in Product Visual Database: " + productInfo.visualId, m_ProductVisualDatabase);
                        continue;
                    }

                    ProductVisualSO doorStyleVisual = null;
                    if(this._rules.RequiresBrandCatalog)
                    {
                        Kitchen kitchen = this._kitchen;

                        if(productInfo.cabinetSectionCategoryId > 0 && productInfo.manufacturerId != kitchen.selectedBrandCatalog.cabinetManufacturerId)
                        {
                            continue;
                        }

                        doorStyleVisual =
                            this._catalogData.DoorStyles.FindDoorStyleVisual(
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
                    productButton.SetProduct(productInfo, this._catalogData.GetProductVariantList(productInfo));
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
                m_ProductSearchToolbar.SetVisibility(ToolbarContentVisibility.InForeground, ToolbarContentDuration.Tween);
                m_DesignerToolbarToggle.isOn = true;
                DoHide();
            }
            else
            {
                EventDesigner.OnToastRequested.Raise(new ToastInfo("No products found matching the search criteria."));
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
            m_ProductSearchToolbar.SetVisibility(ToolbarContentVisibility.Collapsed, ToolbarContentDuration.Tween);
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
        protected ProductVisualDatabaseSO m_ProductVisualDatabase = null;

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

}   // namespace KitchenDesign.UI.DesignerScreens

