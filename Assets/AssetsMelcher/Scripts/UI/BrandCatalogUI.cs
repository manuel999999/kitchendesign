namespace KitchenDesign
{
    using Melcher;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    ///     Prompts the user to choose a brand catalog at the start of the app.
    ///     This choice is permanent as soon the kitchen setup completes and
    ///     stays until a new kitchen is started.
    ///     <br /><br />
    ///
    ///     This is one of several pop-ups that may appear at the start of the
    ///     game. This UI is intended to be skipped if the <see
    ///     cref="AppOptions"/> in Resources is not configured for Gemini.
    /// </summary>
    public class BrandCatalogUI : MelcherUI
    {
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
            foreach (BrandCatalog brandCatalog in ProFitGlobals.runtimeBrandCatalogs)
            {
                bool allowed =
                    ProFitGlobals.userProfile.adminSelectedBrandCatalogIds == null ||
                    ProFitGlobals.userProfile.adminSelectedBrandCatalogIds.Count <= 0 ||
                    ProFitGlobals.userProfile.adminSelectedBrandCatalogIds
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
                    ProFitGlobals.catalogData.FindBrandCatalogInfoById(brandCatalog.catalogId, out BrandCatalogInfo brandCatalogInfo) &&
                    !ProFitGlobals.runtimeProducts.Any(x => x.category == ProductCategory.CabinetSection && x.manufacturerId == brandCatalogInfo.cabinetManufacturerId);

                catalogCard.SetComingSoon(comingSoon);
            }

            // Sort the catalog cards in the order they appear in the brand
            // catalog logo database.
            catalogCards.Sort(
                static (a, b) =>
                {
                    GroupDatabase catalogLogoDatabase =
                        ProFitGlobals.appProfile.brandCatalogLogoDatabase;

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

        protected override void Awake()
        {
            ProFitGlobals.ExecuteWhenReady(
                () =>
                {
                    // Create new runtime brand catalogs for all custom
                    // server-defined brand catalogs.

                    //foreach (BrandCatalogInfo serverBrandCatalog in ProFitGlobals.serverBrandCatalogs)
                    //{
                    //    ProFitGlobals.appProfile.brandCatalogPresets
                    //}

                    // TODO: This should really being using `runtimeProducts`
                    //       rather than `sourceProducts`, but the runtime list
                    //       isn't populated yet. -Ryan

                    List<ProductInfo> cabinetSectionProductInfos =
                        ProFitGlobals.sourceProducts.Where(
                            static x => x != null && x.category == ProductCategory.CabinetSection
                        ).ToList();

                    List<ProductVisual> cabinetSectionVisuals = new ();
                    List<ProductVisual> cabinetDoorVisuals = new ();

                    foreach (ProductVisual productVisual in Kitchen.instance.productVisualDatabase.runtimeEntries)
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

                    foreach (BrandCatalog brandCatalog in ProFitGlobals.runtimeBrandCatalogs)
                    {
                        brandCatalog.PopulateRuntimeData(
                            cabinetSectionProductInfos,
                            cabinetSectionVisuals,
                            cabinetDoorVisuals
                        );
                    }
                }
            );
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

                BrandCatalog selectedCatalog = brandCatalogToggle.brandCatalog;

                using var raycasterEnabler =
                    MelcherDeferred.Defer(
                        toCapture: m_Raycaster,
                        onDispose: static raycaster => raycaster.enabled = true
                    );

                // Block the UI until the async completes.
                m_Raycaster.enabled = false;

                await EventBridge.Designer.setBrandCatalog.Invoke(selectedCatalog);
            }
        }

        protected void OnBackButtonClicked(MelcherButton button)
        {
            m_CanvasGroup.interactable = false;

            if (ProFitGlobals.session.ShouldUseWhiteLabelMode())
            {
                Hide();

                EventBridge.Designer.showWhiteLabelClientInfoUi.Invoke();
            }
            else if (ProFitGlobals.appOptions.allowBuildingMethods)
            {
                Hide();

                EventBridge.Designer.showBuildingMethodsUi.Invoke();
            }
            else
            {
                EventBridge.Designer.loadMenuScene.Invoke();
            }
        }

        protected void OnNextButtonClicked(MelcherButton caller)
        {
            if (m_BrandCatalogToggleGroup.IsAnyToggleOn())
            {
                Hide();

                EventBridge.Designer.showCabinetShapeUi.Invoke();
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

        private UniTask? m_ActiveCatalogChangeTask;

    }   // class BrandCatalogUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-06-17
/// ===========================================================================
