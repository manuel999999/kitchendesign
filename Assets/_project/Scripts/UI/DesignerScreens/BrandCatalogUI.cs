namespace KitchenDesign.UI.DesignerScreens
{
    using KitchenDesign.Events;
    using KitchenDesign.Session;
    using KitchenDesign.CatalogData;
    using Melcher;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Enums;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using KitchenDesign.KitchenModel;

    /// <summary>
    ///     Prompts the user to choose a brand catalog at the start of the app.
    ///     This choice is permanent as soon the kitchen setup completes and
    ///     stays until a new kitchen is started.
    ///     <br /><br />
    ///
    ///     This is one of several pop-ups that may appear at the start of the
    ///     game. This UI is intended to be skipped if the <see
    ///     cref="AppOptionsSO"/> in Resources is not configured for Gemini.
    /// </summary>
    public class BrandCatalogUI : MelcherUI
    {
        private Kitchen _kitchen;
        private AppProfileSO _profile;
        private AppOptionsSO _options;
        private CatalogDataStatus _catalogData;
        private SessionStatus _session;

        public void Initialize(Kitchen kitchen, AppProfileSO profile, AppOptionsSO options, CatalogDataStatus catalogData, SessionStatus session)
        {
            this._kitchen = kitchen;
            this._profile = profile;
            this._options = options;
            this._catalogData = catalogData;
            this._session = session;

            this._catalogData.ExecuteWhenLoaded(this.PopulateRuntimeBrandCatalogs);
        }

        [SerializeField] private ProductVisualDatabaseSO productVisualDatabase;

        // =====================================================================
        // Methods
        // =====================================================================

        public void ClearForm()
        {
            m_BrandCatalogToggleGroup.allowSwitchOff = true;
            m_BrandCatalogToggleGroup.SetAllTogglesOff(sendCallback: false);
            m_NextButton.interactable = false;

            Transform toggleGroupTransform = m_BrandCatalogToggleGroup.transform;

            // Destroy the existing catalog cards so they can be recreated.
            // This is simpler than optimizing for pooling/reusing elements.
            for (int i = toggleGroupTransform.childCount - 1; i >= 0; --i)
            {
                Destroy(toggleGroupTransform.GetChild(i).gameObject);
            }

            List<BrandCatalogToggle> catalogCards = new List<BrandCatalogToggle>();

            // Recreate the catalog cards that this user has access to
            foreach (BrandCatalogSO brandCatalog in this._catalogData.RuntimeBrandCatalogs)
            {
                bool allowed =
                    this._session.UserProfile.adminSelectedBrandCatalogIds == null ||
                    this._session.UserProfile.adminSelectedBrandCatalogIds.Count <= 0 ||
                    this._session.UserProfile.adminSelectedBrandCatalogIds
                        .Contains(brandCatalog.catalogId);

                if (!allowed)
                {
                    // Do not create a UI option for this catalog because it
                    // was forbidden to user in via the dashboard.
                    continue;
                }

                BrandCatalogToggle catalogCard =
                    Instantiate(m_BrandCatalogTogglePrefab, toggleGroupTransform);

                catalogCard.brandCatalog = brandCatalog;
                catalogCard.toggleGroup = m_BrandCatalogToggleGroup;

                catalogCards.Add(catalogCard);

                bool comingSoon =
                    brandCatalog.fallbackDefaultCabinetSectionProductServerId == 0 &&
                    string.IsNullOrWhiteSpace(brandCatalog.fallbackDefaultCabinetSectionProductSku) &&
                    this._catalogData.FindBrandCatalogInfoById(brandCatalog.catalogId, out BrandCatalogInfo brandCatalogInfo) &&
                    !this._catalogData.RuntimeProducts.Any(x => x.category == ProductCategory.CabinetSection && x.manufacturerId == brandCatalogInfo.cabinetManufacturerId);

                catalogCard.SetComingSoon(comingSoon);
            }

            // Sort the catalog cards in the order they appear in the brand
            // catalog logo database.
            catalogCards.Sort(
                (a, b) =>
                {
                    GroupDatabaseSO catalogLogoDatabase =
                        this._profile.brandCatalogLogoDatabase;

                    int sortOrderA = catalogLogoDatabase.GetSortOrder(a.brandCatalog.catalogId);
                    int sortOrderB = catalogLogoDatabase.GetSortOrder(b.brandCatalog.catalogId);

                    if (sortOrderA == sortOrderB)
                    {
                        return 0;
                    }

                    return sortOrderA < sortOrderB ? -1 : 1;
                }
            );

            for (int i = 0; i < catalogCards.Count - 1; ++i)
            {
                catalogCards[i].transform.SetSiblingIndex(i);
            }
        }

        private void PopulateRuntimeBrandCatalogs()
        {
            // Create new runtime brand catalogs for all custom
            // server-defined brand catalogs.

            // TODO: This should really being using `runtimeProducts`
            //       rather than `sourceProducts`, but the runtime list
            //       isn't populated yet. -Ryan

            List<ProductInfo> cabinetSectionProductInfos =
                this._catalogData.SourceProducts.Where(
                    static x => x != null && x.category == ProductCategory.CabinetSection
                ).ToList();

            List<ProductVisualSO> cabinetSectionVisuals = new ();
            List<ProductVisualSO> cabinetDoorVisuals = new ();

            foreach (ProductVisualSO productVisual in this.productVisualDatabase.runtimeEntries)
            {
                if (!productVisual)
                {
                    continue;
                }

                switch (productVisual.info.runtimePrefabCategory)
                {
                    case ProductCategorySpecific.CabinetSection:
                        cabinetSectionVisuals.Add(productVisual);
                        break;

                    case ProductCategorySpecific.CabinetDoor:
                        cabinetDoorVisuals.Add(productVisual);
                        break;

                    default:
                        break;
                }
            }

            foreach (BrandCatalogSO brandCatalog in this._catalogData.RuntimeBrandCatalogs)
            {
                brandCatalog.PopulateRuntimeData(
                    cabinetSectionProductInfos,
                    cabinetSectionVisuals,
                    cabinetDoorVisuals,
                    this._catalogData.DoorStyles
                );
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_BrandCatalogToggleGroup.onToggleChanged += OnBrandCatalogToggleChanged;

            m_BackButton.onClick += OnBackButtonClicked;
            m_NextButton.onClick += OnNextButtonClicked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_BrandCatalogToggleGroup.onToggleChanged -= OnBrandCatalogToggleChanged;

            m_BackButton.onClick -= OnBackButtonClicked;
            m_NextButton.onClick -= OnNextButtonClicked;
        }

        protected void OnBrandCatalogToggleChanged(MelcherToggleGroup group, IMelcherToggle toggle)
        {
            if (toggle.isOn)
            {
                OnBrandCatalogToggleChangedAsync(toggle).Forget();
            }

            async UniTask OnBrandCatalogToggleChangedAsync(IMelcherToggle toggle)
            {
                m_BrandCatalogToggleGroup.allowSwitchOff = false;
                m_NextButton.interactable = true;

                BrandCatalogToggle brandCatalogToggle =
                    MelcherSearch.FirstFromTransformUpward<BrandCatalogToggle>(
                        ((Component)toggle).transform
                    );

                BrandCatalogSO selectedCatalog = brandCatalogToggle.brandCatalog;

                using var raycasterEnabler =
                    MelcherDeferred.Defer(
                        toCapture: m_Raycaster,
                        onDispose: static raycaster => raycaster.enabled = true
                    );

                // Block the UI until the async completes.
                m_Raycaster.enabled = false;

                await this._kitchen.SetBrandCatalog(selectedCatalog);
            }
        }

        protected void OnBackButtonClicked(MelcherButton button)
        {
            m_CanvasGroup.interactable = false;

            if (this._session.ShouldUseWhiteLabelMode())
            {
                Hide();

                EventDesigner.OnWhiteLabelClientInfoRequested.Raise();
            }
            else if (this._options.allowBuildingMethods)
            {
                Hide();

                EventDesigner.OnBuildingMethodsRequested.Raise();
            }
            else
            {
                EventSession.OnMenuSceneRequested.Raise(new MenuSceneRequestInfo(null));
            }
        }

        protected void OnNextButtonClicked(MelcherButton caller)
        {
            if (m_BrandCatalogToggleGroup.IsAnyToggleOn())
            {
                Hide();

                EventDesigner.OnCabinetShapesRequested.Raise();
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(CabinetShapeUI))]

        [SerializeField] protected MelcherToggleGroup m_BrandCatalogToggleGroup = null;

        [Space]
        [SerializeField] protected MelcherButton m_BackButton = null;
        [SerializeField] protected MelcherButton m_NextButton = null;

        [Tooltip("This prefab is instantiated for each catalog that is " +
            "defined in the backend.")]
        [SerializeField]
        protected BrandCatalogToggle m_BrandCatalogTogglePrefab = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        private UniTask? _activeCatalogChangeTask;

    }   // class BrandCatalogUI

}   // namespace KitchenDesign.UI.DesignerScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-06-17
/// ===========================================================================
