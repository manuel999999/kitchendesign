namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Interfaces;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using DG.Tweening;
    using System;
    using Cysharp.Threading.Tasks;

    /// <summary>
    ///     Manages the full toolbar state.
    /// </summary>
    /// <remarks>
    ///     I'd like to split this into separate classes that each manage their
    ///     own toolbar content: Categories, Manufactures, Products, Actions,
    ///     etc.
    /// </remarks>
    public class DesignerToolbarUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     This is invoked when the user has a cabinet section selected
        ///     and chooses the "swap out" option. This sets up the toolbar to
        ///     display the cabinet section groups so the user is able to
        ///     choose a cabinet replacement.
        /// </summary>
        /// <param name="predicate">
        ///     Filters which product infos are allowed to appear in the
        ///     toolbar (and therefore the cabinet types are also filtered).
        /// </param>
        public void ShowCabinetLayoutToolbar(Func<ProductInfo, bool> predicate)
        {
            m_CategoryBreadcrumb.phrase = m_CabinetSectionButton.phrase;
            m_CategoryBreadcrumb.sprite = m_CabinetSectionButton.sprite;

            // Update the toolbars to show the available cabinets. Hide the
            // selected object toolbar as it otherwise renders over the
            // cabinet list toolbar.
            m_CategoryToolbar.SetVisibility(ToolbarContent.Visibility.InBackground, ToolbarContent.Duration.Immediate);
            m_ManufacturerToolbar.SetVisibility(ToolbarContent.Visibility.InForeground, ToolbarContent.Duration.Immediate);
            m_ProductToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Immediate);
            m_ProductSearchToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Immediate);
            m_ProductActionToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Immediate);

            m_SelectedProductToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Tween);

            // Ensure the toolbar (that contains the cabinet products) is
            // visible.
            SetToolbarVisibility(true, ToolbarContent.Duration.Tween);
            m_ToolbarToggleButton.SetIsOnWithoutNotify(true);

            m_ToolbarCabinetProductPredicate = predicate;

            PopulateCabinetSectionGroupsToolbarForCatalog(m_CabinetSectionButton);
        }

        protected override void Awake()
        {
            base.Awake();

            m_CabinetFinishesButtonDefaultSiblingIndex =
                m_CabinetFinishesButton.transform.GetSiblingIndex();

            m_ProductAddButtonSpinner.SetVisible(false);
        }

        protected override void Start()
        {
            base.Start();

            // Re-enable the UI masks. These masks are manually added by the
            // developer in the inspector. Developers can disable the masks in
            // the scene to better see the UI in the editor, and they will
            // automatically reactivate when the app starts.
            foreach (Mask mask in m_ToolbarMasks)
            {
                if (mask)
                {
                    mask.enabled = true;
                }
            }

            // Setup the initial UI state (Needs to wait for the layout system
            // to refresh.)
            MEC.Timing.CallDelayed(
                MEC.Timing.WaitForOneFrame,
                () => ResetToolbar()
            );

            ProFitGlobals.ExecuteWhenReady(SetupCategoryToolbar);
        }

        protected void SetupCategoryToolbar()
        {
            Kitchen kitchen = Kitchen.instance;

            // Populate the category toolbar. Find categories that we have
            // access to
            IEnumerable<ProductCategory> availableCategories =
                ProFitGlobals.runtimeProducts
                    //.Where(productInfo => IsWhiteListed(productInfo.manufacturer))
                    .Select(productInfo => productInfo.category)
                    .Distinct()
                    .OrderBy(category => category);

            ComponentsInChildrenEnumerator<ToolbarCategoryButton> categoryButtons =
                new (
                    m_CategoryToolbar.GetScrollContent()
                );

            while (categoryButtons.MoveNext())
            {
                ToolbarCategoryButton categoryButton = categoryButtons.Current;

                // Treat all buttons as enabled unless they are explicitly
                // disabled.
                categoryButton.gameObject.SetActive(true);

                if (categoryButton == m_StainlessSteelBurnerPlungeButton)
                {
                    categoryButton.gameObject.SetActive(
                        ProFitGlobals.appRules.ShowsPlungeCategory
                    );
                    continue;
                }

                if (categoryButton.category == ProductCategory.Shelf)
                {
                    // Plunge/shelf is not available in Catalog building
                    // method.
                    categoryButton.gameObject.SetActive(kitchen.selectedBuildingMethod != KitchenBuildingMethod.Catalog);
                    continue;
                }

                if (categoryButton == m_ToeKickButton)
                {
                    // Toe kick button is only allowed in Gemini builds.
                    categoryButton.gameObject.SetActive(
                        kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog
                    );
                    continue;
                }

                if (categoryButton == m_CabinetSectionButton)
                {
                    if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections)
                    {
                        // Always allow the add section button in Sections
                        // building method.
                        continue;
                    }
                    else if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
                    {
                        // Always allow the add section button in Catalog
                        // building method.
                        categoryButton.sprite = m_CabinetSectionButtonSprite;
                        categoryButton.phrase = "Add Cabinets";
                        continue;
                    }
                    else
                    {
                        // Disallow cabinet sections in standard building
                        // method.
                        categoryButton.gameObject.SetActive(false);
                        continue;
                    }
                }

                if (categoryButton == m_DoorStylesButton)
                {
                    // Door style button is only available in catalog mode.
                    m_DoorStylesButton.gameObject.SetActive(
                        kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                        kitchen.selectedBrandCatalog &&
                        kitchen.selectedBrandCatalog.hasDoorStyles
                    );
                    continue;
                }

                if (categoryButton == m_DoorHandleStylesButton)
                {
                    // Door Handle style button is only available in catalog mode.
                    m_DoorHandleStylesButton.gameObject.SetActive(
                        kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                        kitchen.selectedBrandCatalog &&
                        kitchen.selectedBrandCatalog.hasDoorHandleStyles
                    );
                    continue;
                }

                if (categoryButton == m_EndPanelsButton)
                {
                    // End panels button is only available in catalog mode.
                    m_EndPanelsButton.gameObject.SetActive(
                        kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                        kitchen.selectedBrandCatalog &&
                        kitchen.selectedBrandCatalog.hasEndPanelsOption
                    );
                    continue;
                }

                if (categoryButton == m_BackPanelsButton)
                {
                    // Back panels button is only available in catalog mode.
                    m_BackPanelsButton.gameObject.SetActive(
                        kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                        kitchen.selectedBrandCatalog &&
                        kitchen.selectedBrandCatalog.hasBackPanelsOption
                    );
                    continue;
                }

                if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
                {
                    switch (categoryButton.category)
                    {
                        case ProductCategory.Accessory:
                        case ProductCategory.Shelf:
                        case ProductCategory.Storage:
                            // No accessories, shelves (plunge), or storage
                            // products in Catalog mode. The cabinets come with
                            // storage built-in.
                            categoryButton.gameObject.SetActive(false);
                            continue;

                        case ProductCategory.FinishCabinet:
                            categoryButton.gameObject.SetActive(
                                kitchen.selectedBrandCatalog ? kitchen.selectedBrandCatalog.hasCabinetFinishes : true
                            );
                            continue;
                    }
                }
                else if (
                    categoryButton.category == ProductCategory.Accessory &&
                    ProFitGlobals.session.ShouldUseWhiteLabelMode() &&
                    ProFitGlobals.session.GetWhiteLabelAuthorizedDomain() != null &&
                    ProFitGlobals.session.GetWhiteLabelAuthorizedDomain().Contains("napoleon.com", StringComparison.OrdinalIgnoreCase))
                {
                    // Accessory option is not available to white-labeled
                    // Napoleon.
                    // https://3.basecamp.com/3293895/buckets/37470979/messages/9067338116

                    categoryButton.gameObject.SetActive(false);
                    continue;
                }

                // Make the category button active if there's at least one
                // manufacturer/product using it
                categoryButton.gameObject.SetActive(
                    availableCategories.Contains(categoryButtons.Current.category)
                );
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EventBridge.Designer.restart                    += ResetToolbar;
            EventBridge.Designer.onHighlightChanged.OnInvoke += OnHighlightChanged;
            EventBridge.Designer.onBuildingMethodChanged    += OnBuildingMethodChanged;
            EventBridge.Designer.onBrandCatalogChanged.OnInvoke += OnBrandCatalogChanged;
            EventBridge.Designer.onKitchenReady.OnInvoke    += OnKitchenReady;
            EventBridge.Designer.onCabinetSwapStart         += OnCabinetSwapStart;
            EventBridge.Designer.onCabinetSwapCanceled      += OnCabinetSwapCanceled;

            m_ToolbarToggleButton.onValueChanged += ToggleToolbar;

            ToolbarCategoryButton.onClick += OnProductCategoryButtonClicked;
            ToolbarGroupButton.onClick += OnManufacturerButtonClicked;
            ToolbarProductButton.onClick += OnProductClicked;

            m_ProductAddButton.onClick += AddProduct;
            m_ProductInfoButton.onClick += ShowProductInfo;
            m_ProductPreviousVariantButton.onClick += ShowPreviousProductVariant;
            m_ProductNextVariantButton.onClick += ShowNextProductVariant;
            m_ProductVariantsButton.onClick += ShowProductVariants;

            m_BreadcrumbButtonQuickHint.onManuallyClosed += OnBreadcrumbButtonQuickHintManuallyClosed;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EventBridge.Designer.restart                    -= ResetToolbar;
            EventBridge.Designer.onHighlightChanged.OnInvoke -= OnHighlightChanged;
            EventBridge.Designer.onBuildingMethodChanged    -= OnBuildingMethodChanged;
            EventBridge.Designer.onBrandCatalogChanged.OnInvoke -= OnBrandCatalogChanged;
            EventBridge.Designer.onKitchenReady.OnInvoke    -= OnKitchenReady;
            EventBridge.Designer.onCabinetSwapStart         -= OnCabinetSwapStart;
            EventBridge.Designer.onCabinetSwapCanceled      -= OnCabinetSwapCanceled;

            m_ToolbarToggleButton.onValueChanged -= ToggleToolbar;

            ToolbarCategoryButton.onClick -= OnProductCategoryButtonClicked;
            ToolbarGroupButton.onClick -= OnManufacturerButtonClicked;
            ToolbarProductButton.onClick -= OnProductClicked;

            m_ProductAddButton.onClick -= AddProduct;
            m_ProductInfoButton.onClick -= ShowProductInfo;
            m_ProductVariantsButton.onClick -= ShowProductVariants;

            m_BreadcrumbButtonQuickHint.onManuallyClosed -= OnBreadcrumbButtonQuickHintManuallyClosed;
        }

        protected void ToggleToolbar(IMelcherToggle caller)
        {
            if (m_SelectedProductToolbar.visibility == ToolbarContent.Visibility.InForeground)
            {
                // Clear the highlight
                EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(null).Forget();
                return;
            }

            SetToolbarVisibility(caller.isOn, ToolbarContent.Duration.Tween);

            TMP_Text toggleText = m_ToolbarToggleButton.GetComponentInChildren<TMP_Text>();
            toggleText.text = caller.isOn ? "Close" : "Add Product";
        }

        protected void SetToolbarVisibility(bool visible, ToolbarContent.Duration duration)
        {
            Ease offsetEase = Ease.InQuad;

            if (!visible)
            {
                offsetEase = Ease.OutQuad;
            }

            if (duration == ToolbarContent.Duration.Immediate)
            {
                // Immediately snap to the requested position
                m_ToolbarAccordion.Snap(visible);
            }
            else
            {
                // Tween to the requested position
                const float TweenSeconds = 0.25f;
                MelcherLog.Log("Tweening toolbar. Visible = " + visible, severity: MelcherLogSeverity.Verbose);
                m_ToolbarAccordion.TweenUsingDuration(visible, TweenSeconds, offsetEase);
            }
        }

        protected void ShowCategoryToolbar()
        {
            m_CategoryToolbar.SetVisibility(ToolbarContent.Visibility.InForeground, ToolbarContent.Duration.Tween);
            m_ManufacturerToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Tween);
            m_ProductToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Tween);
            m_ProductActionToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Tween);
        }

        /// <summary>
        ///     Return true if the manufacturer ID has been whitelisted by the
        ///     admin in the Pro-Fit backend. This is used to check whether a
        ///     manufacturer button or a product button should appear normally
        ///     (return <see langword="true"/>) or should be greyed-out (return
        ///     <see langword="false"/>).
        /// </summary>
        /// <param name="manufacturerId"></param>
        protected bool IsManufacturerIdAdminWhitelisted(int manufacturerId)
        {
            UserProfile profile = ProFitGlobals.userProfile;

            if (ProFitGlobals.catalogData.FindManufacturerById(manufacturerId, out ManufacturerInfo manufacturer))
            {
                if (!ProFitGlobals.appRules.IncludesStollManufacturer &&
                    manufacturer.name.Equals("Stoll", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (profile.adminSelectedManufacturerIds == null ||
                profile.adminSelectedManufacturerIds.Count <= 0)
            {
                // Treat all manufacturers as whitelisted if none are. This is
                // convenient because admins do not need to update every dealer
                // if a new manufacturer is added.
                return true;
            }

            return profile.adminSelectedManufacturerIds.Contains(manufacturerId);
        }

        /// <summary>
        /// Called when the user selects a product category.
        /// </summary>
        protected void OnProductCategoryButtonClicked(ToolbarCategoryButton caller)
        {
            Kitchen kitchen = Kitchen.instance;

            if (caller && caller == m_CategoryBreadcrumb)
            {
                s_BreadcrumbButtonHintTaught = true;
                m_BreadcrumbButtonQuickHint.Close();

                ShowCategoryToolbar();
                return;
            }

            // Handle special case, instant-activate, buttons. These are
            // actions that will have no effect on the current UI status.
            if (caller && caller.category == ProductCategory.Shelf)
            {
                string plungeProductSku = "Shelf:Top";

                if (caller == m_StainlessSteelBurnerPlungeButton)
                {
                    plungeProductSku = "SARBKT";
                }

                // Try to add a plunge (shelf)
                ProductInfo plungeProductInfo =
                    GetAvailableProductsInProductCategory(ProductCategory.Shelf)
                        .FirstOrDefault(x => x.sku == plungeProductSku);

                MelcherLog.Assert(plungeProductInfo != null,
                    $"{plungeProductSku} does not exist in website's backend."
                );

                EventBridge.Designer.onTryAddProduct.Invoke(plungeProductInfo, onComplete: null);

                return;
            }
            else if (caller == m_ToeKickButton)
            {
                EventBridge.Designer.tryToggleToeKicks?.Invoke();

                return;
            }

            if (caller)
            {
                // TODO: Maybe only reset the scroll view if this is a new
                //       manufacturer (or category) being selected.
                m_ManufacturerToolbar.ResetScrollToStart();

                // Make the breadcrumb look like the category that was selected
                m_CategoryBreadcrumb.phrase = caller.phrase;
                m_CategoryBreadcrumb.sprite = caller.sprite;

                m_CategoryBreadcrumb.category = caller.category;
            }

            m_CategoryToolbar.SetVisibility(ToolbarContent.Visibility.InBackground, ToolbarContent.Duration.Tween);
            m_ManufacturerToolbar.SetVisibility(ToolbarContent.Visibility.InForeground, ToolbarContent.Duration.Tween);
            m_ProductToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Tween);
            m_ProductActionToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Tween);

            if (!s_BreadcrumbButtonHintTaught && !m_BreadcrumbButtonQuickHint.isVisible)
            {
                m_BreadcrumbButtonQuickHint.Show(delayDuration: 0.8f);
            }

            // Early exit if caller is null. This means we're returning to the
            // toolbar from elsewhere and its current status is good
            if (caller == null)
            {
                return;
            }

            RectTransform scrollContent = m_ManufacturerToolbar.GetScrollContent();

            // Clear the manufacturer toolbar
            m_ManufacturerToolbar.DestroyScrollContent();

            // Special cases that require specific changes to the UI:
            // Cabinet Sections, Cabinet Doors, and Backings (like backsplash
            // or bar top)
            if (caller.category == ProductCategory.CabinetSection)
            {
                m_ToolbarCabinetProductPredicate = null;


                KitchenBuildingMethod buildingMethod = Kitchen.instance.selectedBuildingMethod;

                if (buildingMethod == KitchenBuildingMethod.Sections)
                {
                    PopulateCabinetSectionGroupsToolbarForSections(caller);
                }
                else if (buildingMethod == KitchenBuildingMethod.Catalog)
                {
                    PopulateCabinetSectionGroupsToolbarForCatalog(caller);
                }
                else
                {
                    MelcherLog.LogError("Unhandled building method: " + buildingMethod);
                }

                return;
            }
            else if (caller.category == ProductCategory.CabinetDoor)
            {
                // Display a list of available door styles (for this catalog).
                OnCabinetDoorButtonClicked(caller);
                return;
            }
            else if (caller.category == ProductCategory.CabinetDoorHandle)
            {
                // Display a list of available door handle styles (for this catalog).
                OnCabinetDoorHandleButtonClicked(caller);
                return;
            }
            else if (caller.category == ProductCategory.CabinetEndPanels)
            {
                // Display a list of available end panels.
                OnCabinetEndPanelsClicked(caller);
                return;
            }
            else if (caller.category == ProductCategory.CabinetBackPanels)
            {
                // Toggle Back Panels
                CreateToolbarButton(m_ManufacturerToolbar.GetScrollContent(), "BackPanelButton", "Back Panel", 0, ToolbarGroupButton.IdType.CabinetBackPanels);
                return;
            }
            else if (caller.category.IsBacking())
            {
                // For backsplashes and bar tops, we'll manually create
                // buttons for them

                // Get the cabinet layout so we know how many buttons to
                // create
                CabinetLayout cabinetLayout = null;
                EventBridge.Designer.onWantCabinetLayout.Invoke(ref cabinetLayout);

                foreach (CabinetSpan span in cabinetLayout.spans)
                {
                    foreach (ElementIndex<Cabinet> i in span.cabinets)
                    {
                        Cabinet cabinet = i.element;

                        if (!cabinet.canHaveProducts)
                        {
                            continue;
                        }

                        Sprite sprite = cabinet.backingSprite;
                        if (!sprite)
                        {
                            continue;
                        }

                        // Create a backing button for the cabinet
                        ToolbarGroupButton button = Instantiate(m_ManufacturerButtonPrefab, scrollContent);
                        button.name = caller.category + cabinet.name + "Button";
                        button.targetId = 0;  // No manufacturer
                        button.targetCabinetSpan = span;
                        button.SetIcon(sprite, m_BackingIconColor);

                        break;
                    }
                }

                return;
            }

            // Populate the manufacturer toolbar

            // Get the products that match this product category. We will use
            // the list of valid products to determine the list of
            // manufacturers and product groups to display.
            IEnumerable<ProductInfo> productsInCategory =
                GetAvailableProductsInProductCategory(caller.category);

            {
                // Find the product groups to display in the toolbar
                IEnumerable<int> productGroupIdsInCategory =
                    productsInCategory
                        .Where(static productInfo => productInfo.groupId != 0)         // Select products that have a group
                        .Select(static productInfo => productInfo.groupId)
                        .Distinct()
                        .OrderBy(static productGroupId => ProFitGlobals.appProfile.productGroupDatabase.GetSortOrder(productGroupId));

                AddButtonsForIds(
                    buttonParent:           scrollContent,
                    buttonPrefab:           m_ManufacturerButtonPrefab,
                    idType:                 ToolbarGroupButton.IdType.ProductGroup,
                    ids:                    productGroupIdsInCategory,
                    appLocalDatabase:        ProFitGlobals.appProfile.productGroupDatabase,
                    findServerDataIndex:    id => ProFitGlobals.productGroups.FindIndex(productGroup => productGroup.id == id),
                    getServerDataName:      serverDataIndex => ProFitGlobals.productGroups[serverDataIndex].name,
                    onButtonSpawned:        null
                );
            }

            {
                // Find the manufacturers to display in the toolbar
                IEnumerable<int> manufacturerIdsInCategory =
                    productsInCategory
                        .Where(productInfo => productInfo.groupId == 0)         // Select products that don't have a group
                        .Select(productInfo => productInfo.manufacturerId)
                        .Distinct()
                        .OrderBy(
                            static manufacturerId =>
                            {
                                // Sort by the order defined in the dashboard.
                                if (ProFitGlobals.catalogData.FindManufacturerById(manufacturerId, out ManufacturerInfo manufacturer) &&
                                    manufacturer.order >= 0)
                                {
                                    return manufacturer.order;
                                }

                                // No order defined in the dashboard, fallback
                                // to the internal sort order.
                                return ProFitGlobals.appProfile.manufacturerDatabase.GetSortOrder(manufacturerId);
                            }
                        );

                if (caller.category == ProductCategory.FinishCabinet)
                {
                    // Apply custom filtering to cabinet finishes:
                    //  - Stoll requested they only see cabinet finishes made
                    //    by Stoll.
                    //  - Gemini only wants cabinet finishes from the active
                    //    catalog.

                    string manufacturerNameToLimitTo = null;

                    if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections)
                    {
                        manufacturerNameToLimitTo = "Stoll";
                    }
                    else if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
                    {
                        manufacturerNameToLimitTo =
                            kitchen.selectedBrandCatalog.catalogName;
                    }

                    if (!string.IsNullOrEmpty(manufacturerNameToLimitTo))
                    {
                        if (!ProFitGlobals.catalogData.FindManufacturerByName(manufacturerNameToLimitTo, out ManufacturerInfo allowedManufacturer))
                        {
                            MelcherLog.LogError($"Unable to find manufacturer by name '{manufacturerNameToLimitTo}'");
                        }
                        else
                        {
                            manufacturerIdsInCategory =
                                manufacturerIdsInCategory.Where(
                                    manufacturerId =>
                                    {
                                        bool allowed =
                                            manufacturerId == allowedManufacturer.id;

#if UNITY_EDITOR
                                        // In the editor, allow undefined
                                        // finish visuals to be tested.

                                        if (ProFitGlobals.catalogData.FindManufacturerById(manufacturerId, out ManufacturerInfo manufacturerInfo) &&
                                            string.IsNullOrWhiteSpace(manufacturerInfo.name))
                                        {
                                            MelcherLog.Breadcrumb("Allowing default manufacturer.");
                                            allowed = true;
                                        }
#endif  // if UNITY_EDITOR

                                        return allowed;
                                    }
                                );
                        }
                    }
                }

                // Special rule for Gemini: Catalogs can restrict which
                // countertop finishes are allowed when a high bar top is
                // present.
                if (caller.category == ProductCategory.FinishCountertop &&
                    kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                    kitchen.selectedBrandCatalog.whitelistedHighBarTopCountertopManufacturers.Count > 0 &&
                    kitchen.layout.spans.Any(static span => span.backing && span.backing.info.category == ProductCategory.BarTopHigh))
                {
                    manufacturerIdsInCategory =
                        manufacturerIdsInCategory.Where(
                            manufacturerId => kitchen.selectedBrandCatalog.whitelistedHighBarTopCountertopManufacturers.Contains(manufacturerId)
                        );
                }

                IAppRules appRules = ProFitGlobals.appRules;

                manufacturerIdsInCategory =
                    manufacturerIdsInCategory.Where(
                        manufacturerId => appRules.IsManufacturerIdAvailable(manufacturerId)
                    );

                AddButtonsForIds(
                    buttonParent:           scrollContent,
                    buttonPrefab:           m_ManufacturerButtonPrefab,
                    idType:                 ToolbarGroupButton.IdType.Manufacturer,
                    ids:                    manufacturerIdsInCategory,
                    appLocalDatabase:        ProFitGlobals.appProfile.manufacturerDatabase,
                    findServerDataIndex:    id => ProFitGlobals.manufacturers.FindIndex(manufacturer => manufacturer.id == id),
                    getServerDataName:      serverDataIndex => ProFitGlobals.manufacturers[serverDataIndex].name,
                    onButtonSpawned:        null
                );
            }

            if (caller.category == ProductCategory.FinishCabinet &&
                kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                kitchen.selectedBrandCatalog.ruleset == BrandCatalogRuleset.Challenger)
            {
                // Challenger has an optional premium finish option.
                ToolbarGroupButton premiumFinishButton = Instantiate(m_ManufacturerButtonPrefab, scrollContent);
                premiumFinishButton.name = "ChallengerPremiumFinishButton";
                premiumFinishButton.targetId = 0;
                premiumFinishButton.targetIdType = ToolbarGroupButton.IdType.ChallengerPremiumCabinetFinish;

                UpdateChallengerPremiumCabinetFinishPackageButtonLabel(
                    premiumFinishButton,
                    kitchen.selectedExtraFeatures.Contains(KitchenExtraFeature.ChallengerPremiumCabinetFinish)
                );
            }

            if (caller.category == ProductCategory.Finish || caller.category == ProductCategory.FinishCabinet)
            {
                // Add the options to clear the finishes to the end
                ToolbarGroupButton clearCabinetButton = Instantiate(m_ManufacturerButtonPrefab, scrollContent);
                clearCabinetButton.name = "ClearCabinetFinishButton";
                clearCabinetButton.targetId = 0;
                clearCabinetButton.targetIdType = ToolbarGroupButton.IdType.ResetCabinet;
                clearCabinetButton.phrase = "Clear Cabinet Finish";
            }

            if (caller.category == ProductCategory.Finish || caller.category == ProductCategory.FinishCountertop)
            {
                ToolbarGroupButton clearCountertopButton = Instantiate(m_ManufacturerButtonPrefab, scrollContent);
                clearCountertopButton.name = "ClearCountertopFinishButton";
                clearCountertopButton.targetId = 0;
                clearCountertopButton.targetIdType = ToolbarGroupButton.IdType.ResetCountertop;
                clearCountertopButton.phrase = "Clear Countertop Finish";
            }

            if (caller.category == ProductCategory.Finish || caller.category == ProductCategory.FinishFloor)
            {
                ToolbarGroupButton clearFlooringButton = Instantiate(m_ManufacturerButtonPrefab, scrollContent);
                clearFlooringButton.name = "ClearFlooringFinishButton";
                clearFlooringButton.targetId = 0;
                clearFlooringButton.targetIdType = ToolbarGroupButton.IdType.ResetFlooring;
                clearFlooringButton.phrase = "Clear Flooring Finish";
            }

            /// <summary>
            ///     This nested function is used to create buttons for the
            ///     product groups and manufacturers. Both button types are
            ///     very similar and can share this setup code.
            /// </summary>
            /// <param name="idType">
            ///     The type of button to create. It should only be <see
            ///     cref="ToolbarGroupButton.IdType.Manufacturer"/> or
            ///     <see cref="ToolbarGroupButton.IdType.ProductGroup"/>.
            ///     This affects the fallback name used if an ID is not known
            ///     by the database or the server.
            /// </param>
            /// <param name="ids">
            ///     A collection of IDs to create buttons for. These IDs
            ///     correspond to manufacturers or product groups.
            /// </param>
            /// <param name="appLocalDatabase">
            ///     The app-local group database that knows which icons to use
            ///     with each ID. The database does not need to have an entry
            ///     for every ID. The ones that do not have an entry will
            ///     fallback to the GroupDatabase's default icon.
            /// </param>
            /// <param name="findServerDataIndex">
            ///     A delegate that takes an ID argument and returns the index
            ///     into the corresponding server data that matches the ID.
            ///     <br /><br />
            ///
            ///     E.g., this function may lookup a <see cref="ProductGroupInfo"/>
            ///     in the global <see cref="ProFitGlobals.productGroups"/>
            ///     list. By using a delegate, this function can be agnostic to
            ///     the type, <see cref="ProductGroupInfo"/>, and the type for
            ///     manufacturer info.
            /// </param>
            /// <param name="getServerDataName">
            ///     A delegate that takes an index into the server data list as
            ///     an argument and returns the name of the server data at that
            ///     index.
            ///     <br /><br />
            ///
            ///     E.g., this function may lookup a <see cref="ProductGroupInfo"/>
            ///     in the global <see cref="ProFitGlobals.productGroups"/>
            ///     list. By using a delegate, this function can be agnostic to
            ///     the type, <see cref="ProductGroupInfo"/>, and the type for
            ///     manufacturer info.
            /// </param>
            /// <param name="onButtonSpawned">
            ///     An optional callback that is invoked after a button is
            ///     created. This callback can be used to set whether the
            ///     button is greyed out (or anything else).
            /// </param>
            void AddButtonsForIds(
                RectTransform               buttonParent,
                ToolbarGroupButton          buttonPrefab,
                ToolbarGroupButton.IdType   idType,
                IEnumerable<int>            ids,
                GroupDatabase               appLocalDatabase,
                Func<int /* id */, int>     findServerDataIndex,
                Func<int /* serverDataIndex */, string> getServerDataName,
                Action<ToolbarGroupButton>  onButtonSpawned)
            {
                foreach (int id in ids)
                {
                    GroupDatabase.GroupInfo buttonInfo;

                    // Use the app-local database as the basis for the name and
                    // icon. This is superseded by server provided data if it
                    // is available. This allows the client to control the text
                    // that appears in the app. The icon must come from the
                    // app-local database as there isn't a feature to upload or
                    // download manufacturer logos.

                    if (!appLocalDatabase.Find(id, out buttonInfo))
                    {
                        MelcherLog.LogError("Did not find group with ID: " + id + " in database: " + appLocalDatabase.name, appLocalDatabase);
                    }

                    int serverDataIndex = findServerDataIndex.Invoke(id); // Basically calling something like: ProFitGlobals.manufacturers.FindIndex(manufacturer => manufacturer.id == id);

                    if (serverDataIndex >= 0 && !buttonInfo.useLocalCommentAsOverride)
                    {
                        // Use user defined name
                        buttonInfo.comment = getServerDataName.Invoke(serverDataIndex);
                    }

                    if (string.IsNullOrWhiteSpace(buttonInfo.comment))
                    {
                        buttonInfo.comment = $"{idType} {id}";
                    }

                    ToolbarGroupButton button = Instantiate(buttonPrefab, buttonParent);
                    button.name = buttonInfo.comment + "Button";
                    button.targetId = id;
                    button.targetIdType = idType;
                    button.SetIcon(buttonInfo.sprite);

                    if (buttonInfo.showCommentOnButton)
                    {
                        button.phrase = buttonInfo.comment;
                    }

                    onButtonSpawned?.Invoke(button);
                }
            }
        }

        protected IEnumerable<ProductInfo> GetAvailableCabinetSections(Func<ProductInfo, bool> predicate)
        {
            Kitchen kitchen = Kitchen.instance;
            IEnumerable<ProductInfo> availableCabinetSections;

            availableCabinetSections =
                m_AvailableProductsByProductCategory[ProductCategory.CabinetSection];

            if (availableCabinetSections == null)
            {
                availableCabinetSections = Array.Empty<ProductInfo>();
            }

            if (predicate != null)
            {
                availableCabinetSections =
                    availableCabinetSections.Where(predicate);
            }

            return availableCabinetSections;
        }

        /// <summary>
        ///     Called when the user clicks the Add Cabinet button the toolbar
        ///     (<see cref="KitchenBuildingMethod.Sections"/> only).
        ///     <br /><br />
        ///
        ///     Populates the toolbar with a button for each custom cabinet
        ///     type supported in Stoll: Normal cabinet, open shelf cabinet,
        ///     and smoker cabinets (TO DO).
        /// </summary>
        protected void PopulateCabinetSectionGroupsToolbarForSections(ToolbarCategoryButton caller)
        {
            m_ManufacturerToolbar.DestroyScrollContent();

            RectTransform scrollContent =
                m_ManufacturerToolbar.GetScrollContent();

            InstantiateButton(
                m_ManufacturerButtonPrefab,
                scrollContent,
                buttonLabel: "Normal Cabinet",
                cabinetSectionCategory: default
            );

            IEnumerable<ProductInfo> availableCabinetSections =
                GetAvailableProductsInProductCategory(ProductCategory.CabinetSection);

            IEnumerable<int> cabinetSectionCategoryIds =
                availableCabinetSections
                    .Select(static x => x.cabinetSectionCategoryId)
                    .Distinct()
                    .OrderBy(
                        cabinetSectionCategoryId =>
                        {
                            bool foundCategory =
                                CatalogData.CatalogDataStatus.FindCabinetSectionCategoryById(
                                    ProFitGlobals.cabinetSectionCategories,
                                    cabinetSectionCategoryId,
                                    out CabinetSectionCategoryInfo cabinetSectionCategory
                                );

                            return foundCategory ? cabinetSectionCategory.sortOrder : 1000;
                        }
                    );

            foreach (int cabinetSectionCategoryId in cabinetSectionCategoryIds)
            {
                bool foundCategory =
                    CatalogData.CatalogDataStatus.FindCabinetSectionCategoryById(
                        ProFitGlobals.cabinetSectionCategories,
                        cabinetSectionCategoryId,
                        out CabinetSectionCategoryInfo cabinetSectionCategory
                    );

                InstantiateButton(
                    m_ManufacturerButtonPrefab,
                    scrollContent,
                    buttonLabel: cabinetSectionCategory.name,
                    cabinetSectionCategory
                );
            }

            static void InstantiateButton(
                ToolbarGroupButton buttonPrefab,
                RectTransform scrollContent,
                string buttonLabel,
                CabinetSectionCategoryInfo cabinetSectionCategory)
            {
                ToolbarGroupButton button =
                    Instantiate(buttonPrefab, scrollContent);

                button.name = $"{nameof(ProductCategory.CabinetSection)}_{buttonLabel}_Button";
                button.phrase = buttonLabel;
                button.sprite = null;
                button.targetIdType = ToolbarGroupButton.IdType.CabinetSectionType;
                button.targetId = cabinetSectionCategory.id;
            }
        }

        /// <summary>
        ///     Called when the user clicks the Cabinet Type button the toolbar
        ///     (<see cref="KitchenBuildingMethod.Catalog"/> only).
        ///
        ///     Populates the toolbar with a button for each cabinet group
        ///     observed. E.g., Grill, Open Shelf, Insert, etc.
        /// </summary>
        protected void PopulateCabinetSectionGroupsToolbarForCatalog(ToolbarCategoryButton caller)
        {
            m_ManufacturerToolbar.DestroyScrollContent();

            RectTransform scrollContent =
                m_ManufacturerToolbar.GetScrollContent();

            IEnumerable<ProductInfo> availableCabinetsInCatalog =
                GetAvailableCabinetSections(m_ToolbarCabinetProductPredicate);

            IEnumerable<int> cabinetSectionGroupsInCatalog =
                availableCabinetsInCatalog
                    .Select(static x => x.cabinetSectionCategoryId)
                    .Distinct()
                    .OrderBy(
                        static cabinetSectionCategoryId =>
                        {
                            bool foundCategory =
                                CatalogData.CatalogDataStatus.FindCabinetSectionCategoryById(
                                    ProFitGlobals.cabinetSectionCategories,
                                    cabinetSectionCategoryId,
                                    out CabinetSectionCategoryInfo cabinetSectionCategory
                                );

                            return foundCategory ? cabinetSectionCategory.sortOrder : 1000;
                        }
                    );

            foreach (int cabinetSectionId in cabinetSectionGroupsInCatalog)
            {
                bool foundCategory =
                    CatalogData.CatalogDataStatus.FindCabinetSectionCategoryById(
                        ProFitGlobals.cabinetSectionCategories,
                        cabinetSectionId,
                        out CabinetSectionCategoryInfo cabinetSectionCategory
                    );

                string cabinetSectionType =
                    foundCategory ? cabinetSectionCategory.name : $"Unknown ({cabinetSectionId})";

                bool categoryHasValidCabinets =
                    availableCabinetsInCatalog.Any(
                        x =>
                        {
                            return x.category == ProductCategory.CabinetSection &&
                                x.cabinetSectionCategoryId == cabinetSectionId &&
                                m_ProductVisualDatabase.FindRuntimeVisualByID(x.visualId);
                        }
                    );

                if (!categoryHasValidCabinets)
                {
                    // Ignore empty categories
                    continue;
                }

                // Create a button for the cabinet section type.
                ToolbarGroupButton button =
                    Instantiate(m_ManufacturerButtonPrefab, scrollContent);

                button.name = $"{caller.category}_{cabinetSectionType}_Button";
                button.phrase = cabinetSectionType;
                button.sprite = null;
                button.targetIdType = ToolbarGroupButton.IdType.CabinetSectionType;
                button.targetId = cabinetSectionCategory.id;
            }
        }

        protected void PopulateCabinetSectionsToolbar(int cabinetSectionCategoryId)
        {
            MelcherLog.Log("Populating cabinet section toolbar");
            m_ProductToolbar.DestroyScrollContent();

            const ProductCategory Category = ProductCategory.CabinetSection;

            Kitchen kitchen = Kitchen.instance;

            RectTransform scrollContent =
                m_ProductToolbar.GetScrollContent();

            List<ProductInfo> productInfo = new();

            // Get the cabinet sections available in this catalog.
            ProductInfo[] cabinetsMatchingType =
                GetAvailableCabinetSections(m_ToolbarCabinetProductPredicate)
                    .Where(x => x.cabinetSectionCategoryId == cabinetSectionCategoryId)
                    .ToArray();

            // Sort the cabinets according to the order they were arranged in
            // the dashboard. If an item doesn't specify a sort order, sort it
            // to the back of the list.
            Array.Sort(
                cabinetsMatchingType,
                static (a, b) =>
                {
                    if (a.sortOrder >= 0 && b.sortOrder >= 0)
                    {
                        return a.sortOrder.CompareTo(b.sortOrder);
                    }
                    else if (a.sortOrder >= 0)
                    {
                        return -1;
                    }
                    else if (b.sortOrder >= 0)
                    {
                        return 1;
                    }

                    // As a fallback: sort the cabinets such that those with the
                    // standard depth appear first. Within each matching depth
                    // group, sort the cabinets from smallest to largest.
                    const float AlmostEqualTolerance = 0.001f;

                    Vector3 productSizeInMetersA = a.productSizeInMeters;
                    Vector3 productSizeInMetersB = b.productSizeInMeters;

                    bool isStandardDepthA = IsStandardDepth(productSizeInMetersA.z);
                    bool isStandardDepthB = IsStandardDepth(productSizeInMetersB.z);

                    if (isStandardDepthA ^ isStandardDepthB)
                    {
                        // Only one has the standard depth. Prefer it over the
                        // other.
                        return isStandardDepthA ? -1 : 1;
                    }

                    if (MelcherCommon.IsAlmostEqual(productSizeInMetersA.z, productSizeInMetersB.z, AlmostEqualTolerance))
                    {
                        // They have the same depths. Sort by their widths.
                        return productSizeInMetersA.x.CompareTo(productSizeInMetersB.x);
                    }

                    return productSizeInMetersA.z.CompareTo(productSizeInMetersB.z);

                    static bool IsStandardDepth(float cabinetDepthInMeters)
                    {
                        KitchenBuildingMethod buildingMethod =
                            Kitchen.instance.selectedBuildingMethod;

                        float standardCabinetDepthInMeters =
                            buildingMethod switch
                            {
                                KitchenBuildingMethod.Sections => Const.Stoll.DefaultCabinetDepth,
                                KitchenBuildingMethod.Catalog => Kitchen.instance.selectedBrandCatalog.standardCabinetDepthInMeters,
                                _ => throw new Exception("Unhandled building method.")
                            };

                        return
                            MelcherCommon.IsAlmostEqual(
                                cabinetDepthInMeters,
                                standardCabinetDepthInMeters,
                                AlmostEqualTolerance
                            );
                    }
                }
            );

            foreach (ProductInfo sectionProductInfo in cabinetsMatchingType)
            {
                ProductVisual sectionVisual =
                    m_ProductVisualDatabase.FindRuntimeVisualByID(
                        sectionProductInfo.visualId
                    );

                if (!sectionVisual)
                {
                    MelcherLog.LogError(
                        $"Could not find a product visual for {MelcherFormat.ConsoleString(sectionProductInfo.name)} with the visual ID {MelcherFormat.ConsoleString(sectionProductInfo.visualId)}"
                    );

                    if (!Application.isEditor && !ProFitGlobals.isVerbose)
                    {
                        continue;
                    }
                }

                // Create a button for the cabinet section type.
                ToolbarProductButton button =
                    Instantiate(m_ProductButtonPrefab, scrollContent);

                // Find the door style for this section that matches the
                // current door style.
                ProductVisual doorStyleVisual = null;
                if (sectionVisual)
                {
                    doorStyleVisual =
                        CabinetSectionDoorUtility.FindDoorStyleVisual(
                            kitchen.productVisualDatabase.runtimeEntries,
                            sectionVisual,
                            kitchen.selectedBrandCatalog,
                            kitchen.selectedDoorStyleName
                        );
                }

                button.name = $"{Category}_{sectionProductInfo.name}_Button";
                button.phrase = sectionProductInfo.name;
                button.sprite = sectionVisual?.info.thumbnail;
                button.productInfo = sectionProductInfo;
                button.overlaySprite = doorStyleVisual?.info.thumbnail;
                button.SetErrorHighlightVisible(
                    !sectionVisual || sectionProductInfo.hasErrors
                );
            }
        }

        protected void OnCabinetDoorButtonClicked(ToolbarCategoryButton caller)
        {
            Kitchen kitchen = Kitchen.instance;

            RectTransform scrollContent =
                m_ManufacturerToolbar.GetScrollContent();

            List<ProductInfo> productInfo = new();

            string selectedDoorStyleName = kitchen.selectedDoorStyleName;

            // Get the cabinet sections available in this catalog.
            var doorStyles = kitchen.selectedBrandCatalog.doorStyles;

            for (int i = 0; i < doorStyles.Count; ++i)
            {
                // Create a button for the cabinet section type.
                ToolbarGroupButton button =
                    Instantiate(m_ManufacturerButtonPrefab, scrollContent);

                BrandCatalog.DoorStyleInfo doorStyleInfo = doorStyles[i];

                button.name = caller.category + doorStyleInfo.name + "Button";
                button.phrase = doorStyleInfo.doorStyleDisplayName;
                button.sprite = doorStyleInfo.toolbarButtonSprite;
                button.targetIdType = ToolbarGroupButton.IdType.CabinetDoorStyle;
                button.targetId = i;
            }
        }

        protected void OnCabinetDoorHandleButtonClicked(ToolbarCategoryButton caller)
        {
            Kitchen kitchen = Kitchen.instance;

            RectTransform scrollContent =
                m_ManufacturerToolbar.GetScrollContent();

            // Get the cabinet sections available in this catalog.
            var doorHandleStyles = kitchen.selectedBrandCatalog.doorHandleStyles;

            for (int i = 0; i < doorHandleStyles.Count; ++i)
            {
                // Create a button for the cabinet section type.
                ToolbarGroupButton button =
                    Instantiate(m_ManufacturerButtonPrefab, scrollContent);

                BrandCatalog.DoorStyleInfo doorHandleStyleInfo = doorHandleStyles[i];

                button.name = caller.category + doorHandleStyleInfo.name + "Button";
                button.phrase = System.Text.RegularExpressions.Regex.Replace(doorHandleStyleInfo.name, "(?<!^)([A-Z])", " $1");
                button.sprite = doorHandleStyleInfo.toolbarButtonSprite;
                button.targetIdType = ToolbarGroupButton.IdType.CabinetDoorHandleStyle;
                button.targetId = i;
            }
        }

        protected void OnCabinetEndPanelsClicked(ToolbarCategoryButton caller)
        {
            Kitchen kitchen = Kitchen.instance;

            RectTransform scrollContent =
                m_ManufacturerToolbar.GetScrollContent();

            CreateToolbarButton(scrollContent, "leftEndPanel", "Left End Panel", 0, ToolbarGroupButton.IdType.CabinetEndPanels);
            CreateToolbarButton(scrollContent, "rightEndPanel", "Right End Panel", 1, ToolbarGroupButton.IdType.CabinetEndPanels);
        }

        protected void CreateToolbarButton(RectTransform scrollContent, string buttonName, string buttonPhrase, int targetId, ToolbarGroupButton.IdType idType)
        {
            ToolbarGroupButton button =
                    Instantiate(m_ManufacturerButtonPrefab, scrollContent);

                button.name = buttonName + "Button";
                button.phrase = buttonPhrase;
                button.sprite = null;
                button.targetIdType = idType;
                button.targetId = targetId;
        }

        /// <summary>
        ///     Called when the user clicks a manufacturer. This happens
        ///     sometime after the user clicks a product category. This
        ///     populates the designer toolbar with buttons for all products in
        ///     the chosen category.
        /// </summary>
        /// <param name="caller"></param>
        protected void OnManufacturerButtonClicked(ToolbarGroupButton caller)
        {
            Clicked(caller).Forget();

            async UniTaskVoid Clicked(ToolbarGroupButton caller)
            {
                if (caller == m_ManufacturerBreadcrumb)
                {
                    OnProductCategoryButtonClicked(null);
                    return;
                }

                if (caller)
                {
                    Kitchen kitchen = Kitchen.instance;

                    m_ProductToolbar.ResetScrollToStart();

                    m_ManufacturerBreadcrumb.SetIcon(caller.icon);
                    m_ManufacturerBreadcrumb.phrase = caller.phrase;
                    m_ManufacturerBreadcrumb.targetId = caller.targetId;
                    m_ManufacturerBreadcrumb.targetIdType = caller.targetIdType;
                    m_ManufacturerBreadcrumb.targetCabinetSpan = caller.targetCabinetSpan;

                    ProductCategory resetFinishCategory = ProductCategory.Unknown;

                    switch (caller.targetIdType)
                    {
                        case ToolbarGroupButton.IdType.ResetCabinet:
                            resetFinishCategory = ProductCategory.FinishCabinet;

                            kitchen.selectedExtraFeatures.Remove(KitchenExtraFeature.ChallengerPremiumCabinetFinish);

                            RectTransform scrollContent = m_ManufacturerToolbar.GetScrollContent();

                            foreach (Transform child in scrollContent)
                            {
                                if (child.TryGetComponent(out ToolbarGroupButton button) &&
                                    button.targetIdType == ToolbarGroupButton.IdType.ChallengerPremiumCabinetFinish)
                                {
                                    UpdateChallengerPremiumCabinetFinishPackageButtonLabel(button, hasPremiumFinish: false);
                                    break;
                                }
                            }

                            break;

                        case ToolbarGroupButton.IdType.ResetCountertop:
                            resetFinishCategory = ProductCategory.FinishCountertop;
                            break;

                        case ToolbarGroupButton.IdType.ResetFlooring:
                            resetFinishCategory = ProductCategory.FinishFloor;
                            break;

                        case ToolbarGroupButton.IdType.ChallengerPremiumCabinetFinish:
                            bool hasPremiumFinish = false;

                            if (!kitchen.selectedExtraFeatures.Remove(KitchenExtraFeature.ChallengerPremiumCabinetFinish))
                            {
                                // Couldn't remove so must not have existed. Let's
                                // add it as this fulfills the toggling behavior.
                                kitchen.selectedExtraFeatures.Add(KitchenExtraFeature.ChallengerPremiumCabinetFinish);
                                hasPremiumFinish = true;
                            }

                            EventBridge.Designer.showNotification?.Invoke(
                                title: "Premium Finish Package",
                                message: hasPremiumFinish ?
                                    "The premium finish package is now applied.\nThis means floors, shelves, and " +
                                    "brackets are powder coated texture black. All other interior faces are " +
                                    "powder coated to match the exterior color." :
                                    "The premium finish package is now removed.",
                                onDismiss: null
                            );

                            UpdateChallengerPremiumCabinetFinishPackageButtonLabel(caller, hasPremiumFinish);

                            // Intentional return. We don't need to open a sub
                            // menu for this option.
                            return;

                        default:
                            // Do nothing
                            break;
                    }

                    if (resetFinishCategory != ProductCategory.Unknown)
                    {
                        // This is a button to clear the product's finish
                        EventBridge.Designer.onFinishMaterialChanged?.Invoke(productInfo: null, resetFinishCategory);
                        return;
                    }

                    if (caller.targetIdType == ToolbarGroupButton.IdType.CabinetDoorStyle)
                    {
                        // This is a button to change the door style.

                        BrandCatalog selectedBrandCatalog =
                            kitchen.selectedBrandCatalog;

                        BrandCatalog.DoorStyleInfo doorStyleInfo =
                            selectedBrandCatalog.doorStyles[caller.targetId];

                        kitchen.selectedDoorStyleName = doorStyleInfo.name;

                        foreach (ElementIndex<Cabinet> i in kitchen.layout.EnumerateCabinets())
                        {
                            Cabinet cabinet = i.element;

                            if (cabinet.cabinetSectionProductInfo == null)
                            {
                                continue;
                            }

                            if (!cabinet.cabinetSectionInstance)
                            {
                                // Skip cabinets that don't have a custom cabinet
                                // model. These are procedural cabinets and they
                                // don't use cabinet doors.
                                continue;
                            }

                            // Find the model prefab that this cabinet section uses.
                            ProductVisual cabinetSectionVisualPrefab =
                                m_ProductVisualDatabase.FindRuntimeVisualByID(
                                    cabinet.cabinetSectionProductInfo.visualId
                                );

                            if (cabinetSectionVisualPrefab == null)
                            {
                                throw new InvalidOperationException(
                                    $"No visual found for cabinet section with visual ID: {cabinet.cabinetSectionProductInfo.visualId}"
                                );
                            }

                            bool expectingCabinetDoors =
                                cabinetSectionVisualPrefab.info.expectingCabinetDoors;

                            string usedDoorStyleVisualName = cabinet.cabinetSectionName;

                            string usedCustomDoorId = CabinetSectionDoorUtility.FindMappedDoorId(cabinet.cabinetSectionVisual.info.id);

                            if(usedCustomDoorId != string.Empty && kitchen.selectedBrandCatalog.cabinetSectionDoorStyle != null)
                            {
                                usedDoorStyleVisualName = $"ProductVisual_{usedCustomDoorId}";
                            }

                            string doorStyleVisualNameToFind =
                                usedDoorStyleVisualName + "@" + kitchen.selectedDoorStyleName;

                            ProductVisual replacementCabinetDoorVisual =
                                MelcherCommon.Find(
                                    doorStyleInfo.visuals,
                                    x => x.info.name == doorStyleVisualNameToFind
                                );

                            if (expectingCabinetDoors && !replacementCabinetDoorVisual)
                            {
                                MelcherLog.LogError($"Could not find a visual with the name {MelcherFormat.ConsoleString(doorStyleVisualNameToFind)}");
                            }
                            else if (!expectingCabinetDoors && replacementCabinetDoorVisual)
                            {
                                MelcherLog.LogError($"Found a visual with the name {MelcherFormat.ConsoleString(doorStyleVisualNameToFind)} but expected to find none.", cabinet);
                            }

                            if (replacementCabinetDoorVisual)
                            {
                                ProductVisualContent replacementCabinetDoorVisualPrefab =
                                    await ProductVisualContentLoader.FirstFromActiveScene()
                                        .ProductVisualToContent(replacementCabinetDoorVisual);

                                i.element.RemakeCabinetDoor(
                                    replacementCabinetDoorVisual.info,
                                    replacementCabinetDoorVisualPrefab
                                );
                            }
                        }

                        (Material cabinetMaterial, Material countertopMaterial) =
                            await kitchen.GetFinishMaterials(allowHighlightToReturn: true);

                        await kitchen.layout.SetCabinetMaterials(
                            cabinetMaterial,
                            countertopMaterial,
                            await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
                        );

                        return;
                    }

                    if(caller.targetIdType == ToolbarGroupButton.IdType.CabinetDoorHandleStyle)
                    {
                        // This is a button to change the door handle style.

                        BrandCatalog selectedBrandCatalog =
                            kitchen.selectedBrandCatalog;

                        BrandCatalog.DoorStyleInfo doorHandleStyleInfo =
                            selectedBrandCatalog.doorHandleStyles[caller.targetId];

                        kitchen.selectedDoorHandleStyleName = doorHandleStyleInfo.name;

                        EventBridge.Designer.onDoorHandleStyleNameUpdate?.Invoke(doorHandleStyleInfo.name);

                        return;
                    }

                    // MARK: END PANELS
                    if(caller.targetIdType == ToolbarGroupButton.IdType.CabinetEndPanels)
                    {
                        // This is a button to toggle end panels.
                        KitchenExtraFeature feature =
                            caller.targetId == 0 ?
                                KitchenExtraFeature.CabinetLeftEndPanels :
                                KitchenExtraFeature.CabinetRightEndPanels;

                        // Toggle whether the kitchen uses these panels.
                        bool didRemoveFeature = kitchen.selectedExtraFeatures.Remove(feature);
                        if (!didRemoveFeature)
                        {
                            kitchen.selectedExtraFeatures.Add(feature);
                        }

                        // Print a toast message to make it clear what changed.
                        string action = didRemoveFeature ? "Removed" : "Added";
                        string affectedSide =
                            feature == KitchenExtraFeature.CabinetLeftEndPanels ?
                                "left" :
                                "right";

                        EventBridge.Designer.showToast?.Invoke(
                            $"{action} {affectedSide} end panels."
                        );

                        // Store the selected end panel width. The stored value
                        // is otherwise unused. This block of code can
                        // potentially be removed.
                        {
                            float widthInMeters =
                                didRemoveFeature ?
                                    0.0f :
                                    kitchen.selectedBrandCatalog.cabinetRuntimeEndPanelInfo.specificPanelWidthInMeters;

                            if (feature == KitchenExtraFeature.CabinetLeftEndPanels)
                            {
                                kitchen.endPanelWidthInMetersLeft = widthInMeters;
                            }
                            else if (feature == KitchenExtraFeature.CabinetRightEndPanels)
                            {
                                kitchen.endPanelWidthInMetersRight = widthInMeters;
                            }
                        }

                        kitchen.UpdateLayoutCabinetMeshes();
                        kitchen.RefreshTickerLabels();

                        return;
                    }

                    // MARK: BACK PANELS
                    if(caller.targetIdType == ToolbarGroupButton.IdType.CabinetBackPanels)
                    {
                        float backPanelDepthInMeters =
                            kitchen.selectedBrandCatalog.cabinetRuntimeBackPanelInfo.panelDepthInMeters;

                        float deltaCabinetDepthInMeters = 0.0f;

                        // Toggle whether the kitchen uses back panels.
                        bool didRemoveFeature = kitchen.selectedExtraFeatures.Remove(KitchenExtraFeature.CabinetBackPanels);

                        if (didRemoveFeature)
                        {
                            kitchen.backPanelDepthInMeters = 0.0f;

                            deltaCabinetDepthInMeters = -backPanelDepthInMeters;
                        }
                        else
                        {
                            kitchen.selectedExtraFeatures.Add(KitchenExtraFeature.CabinetBackPanels);

                            kitchen.backPanelDepthInMeters = backPanelDepthInMeters;

                            deltaCabinetDepthInMeters = backPanelDepthInMeters;
                        }

                        string action = didRemoveFeature ? "Removed" : "Added";

                        EventBridge.Designer.showToast?.Invoke($"{action} back panels.");

                        // Back panels directly affect the depth of cabinets.
                        kitchen.ApplyDeltaToAllCabinetDepths(deltaCabinetDepthInMeters);

                        return;
                    }

                    CabinetBacking backingPrefab = null;

                    if (m_CategoryBreadcrumb.category == ProductCategory.Backsplash)
                    {
                        backingPrefab = m_BacksplashPrefab;
                    }
                    else if (m_CategoryBreadcrumb.category == ProductCategory.BarTopHigh)
                    {
                        backingPrefab = m_HighBarTopPrefab;

                        if (kitchen.selectedBrandCatalog &&
                            kitchen.selectedBrandCatalog.highBarTopOverridePrefab)
                        {
                            backingPrefab = kitchen.selectedBrandCatalog.highBarTopOverridePrefab;

                            if (kitchen.selectedCountertopFinish != null &&
                                kitchen.selectedBrandCatalog.whitelistedHighBarTopCountertopManufacturers.Count > 0)
                            {
                                List<string> allowedCountertopManufacturerNames = new();

                                bool isCurrentCountertopAllowed =
                                    kitchen.selectedBrandCatalog.whitelistedHighBarTopCountertopManufacturers.Contains(
                                        kitchen.selectedCountertopFinish.manufacturerId
                                    );

                                foreach (int manufacturerId in kitchen.selectedBrandCatalog.whitelistedHighBarTopCountertopManufacturers)
                                {
                                    bool foundManufacturer =
                                        ProFitGlobals.catalogData.FindManufacturerById(
                                            manufacturerId,
                                            out ManufacturerInfo manufacturerInfo
                                        );

                                    if (!foundManufacturer)
                                    {
                                        MelcherLog.LogError($"Manufacturer with ID {manufacturerId} is not found.");
                                        continue;
                                    }

                                    allowedCountertopManufacturerNames.Add(manufacturerInfo.name);
                                }

                                if (allowedCountertopManufacturerNames.Count > 0)
                                {
                                    if (!isCurrentCountertopAllowed)
                                    {
                                        // Clear the current incompatible countertop.
                                        EventBridge.Designer.onFinishMaterialChanged?.Invoke(
                                            productInfo: null,
                                            ProductCategory.FinishCountertop
                                        );
                                    }

                                    EventBridge.Designer.showConfirmPrompt?.Invoke(
                                        title: "Limited High Bar Top Finishes",
                                        message: "The selected countertop finish is not available on\n" +
                                            "high bar tops and therefore has been removed automatically.",
                                        confirmLabel: "OK",
                                        onConfirm: null
                                    );
                                }
                            }
                        }
                    }
                    else if (m_CategoryBreadcrumb.category == ProductCategory.BarTopLow)
                    {
                        backingPrefab = m_LowBarTopPrefab;
                    }

                    if (backingPrefab != null)
                    {
                        // Try to add the bartop/backsplash immediately without
                        // going into product toolbar

                        // TODO: Swap out existing backing when a new type is
                        //       selected.

                        ProductCategory backingCategory =
                            m_CategoryBreadcrumb.category;

                        ProductInfo backingInfo =
                            GetAvailableProductsInProductCategory(backingCategory).FirstOrDefault(
                                productInfo => productInfo.category == backingCategory
                            );

                        VolumeOccupantResult result =
                            await caller.targetCabinetSpan.TryInstantiateBacking(
                                backingInfo,
                                backingPrefab
                            );

                        if (result.status != OccupantCuboidStatus.Fits)
                        {
                            string toast =
                                result.status switch
                                {
                                    OccupantCuboidStatus.DataError => "The backing data is invalid.",
                                    OccupantCuboidStatus.OverlapsBacking => "A backing already exists.",
                                    OccupantCuboidStatus.OverlapsAnotherOccupant => "The backing would overlap a product.",
                                    OccupantCuboidStatus.CannotResizeCabinet => "The backing cannot resize its cabinets due to their existing products.",
                                    OccupantCuboidStatus.NotCompatible => "A backing cannot be added here because the cabinets are not the same height.",
                                    _ => "Cannot place backing."
                                };

                            EventBridge.Designer.showToast?.Invoke(toast);
                        }

                        MelcherLog.LogEditorOnly("Did add backing? " + (result.status == OccupantCuboidStatus.Fits));

                        return;
                    }

                    if (caller.targetIdType == ToolbarGroupButton.IdType.CabinetSectionType &&
                        Kitchen.instance.selectedBuildingMethod == KitchenBuildingMethod.Sections &&
                        caller.targetId == 0)
                    {
                        // Special case cabinet in Stoll. Try to add a procedural cabinet.

                        MelcherLog.Log("Adding a cab section", severity: MelcherLogSeverity.Verbose);

                        EventBridge.Designer.onTryAddSection.Invoke(
                            cabinetSectionProductInfo: null,
                            canReplaceCabinet: m_CabinetBeingSwapped
                        );

                        return;
                    }
                }

                m_CategoryToolbar.SetVisibility(ToolbarContent.Visibility.InBackground, ToolbarContent.Duration.Tween);
                m_ManufacturerToolbar.SetVisibility(ToolbarContent.Visibility.InBackground, ToolbarContent.Duration.Tween);
                m_ProductToolbar.SetVisibility(ToolbarContent.Visibility.InForeground, ToolbarContent.Duration.Tween);
                m_ProductActionToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Tween);

                // Early exit if caller is null. This means we're returning to the
                // toolbar from elsewhere and its current contents are good
                if (caller == null)
                {
                    return;
                }

                // Clear the product toolbar
                m_ProductToolbar.DestroyScrollContent();

                if (caller.targetIdType == ToolbarGroupButton.IdType.CabinetSectionType)
                {
                    // This is a button to choose a cabinet section category.
                    PopulateCabinetSectionsToolbar(caller.targetId);
                    return;
                }

                IEnumerable<ProductInfo> productsToDisplay =
                    GetAvailableProductsInProductCategory(m_CategoryBreadcrumb.category);

                switch (caller.targetIdType)
                {
                    case ToolbarGroupButton.IdType.Manufacturer:
                        productsToDisplay =
                            productsToDisplay.Where(
                                productInfo =>
                                    productInfo.manufacturerId == m_ManufacturerBreadcrumb.targetId &&
                                    productInfo.groupId == 0 &&
                                    productInfo.baseProductId == 0
                                );
                        break;

                    case ToolbarGroupButton.IdType.ProductGroup:
                        productsToDisplay =
                            productsToDisplay.Where(
                                productInfo =>
                                    productInfo.groupId == m_ManufacturerBreadcrumb.targetId
                                );
                        break;

                    default:
                        MelcherLog.LogError($"Unhandled target type: {caller.targetIdType}", caller);
                        return;
                }

                // Ensure the products are sorted according to the order that is
                // defined in the dashboard.
                ProductInfo[] sortedProductsToDisplay = productsToDisplay.ToArray();

                System.Array.Sort(
                    sortedProductsToDisplay,
                    static (a, b) =>
                    {
                        if (a.sortOrder >= 0 && b.sortOrder >= 0)
                        {
                            return a.sortOrder.CompareTo(b.sortOrder);
                        }
                        else if (a.sortOrder >= 0)
                        {
                            return -1;
                        }
                        else if (b.sortOrder >= 0)
                        {
                            return 1;
                        }
                        else
                        {
                            return a.id.CompareTo(b.id);
                        }
                    }
                );

                // Populate the product toolbar
                foreach (ProductInfo productInfo in sortedProductsToDisplay)
                {
                    ProductVisual visual =
                        m_ProductVisualDatabase.FindRuntimeVisualByID(
                            productInfo.visualId
                        );

                    if (!visual)
                    {
                        MelcherLog.LogError("Product ID not found in Product Visual Database: " + productInfo.visualId, m_ProductVisualDatabase);

                        if (!Application.isEditor && !ProFitGlobals.isVerbose)
                        {
                            continue;
                        }
                    }

                    ToolbarProductButton productButton =
                        Instantiate(
                            m_ProductButtonPrefab,
                            m_ProductToolbar.GetScrollContent()
                        );
                    productButton.name = productInfo.sku + "Button";
                    productButton.productInfo = productInfo;

                    if (visual && visual.info.thumbnail)
                    {
                        productButton.sprite = visual.info.thumbnail;
                    }

                    productButton.SetErrorHighlightVisible(
                        !visual || productInfo.hasErrors
                    );
                }
            }
        }

        /// <summary>
        ///     Called when the user clicks on a product in the toolbar.
        /// </summary>
        /// <param name="caller"></param>
        protected void OnProductClicked(ToolbarProductButton caller)
        {
            if (caller == m_ProductBreadcrumb)
            {
                OnManufacturerButtonClicked(null);
                return;
            }

            if (caller)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    m_ProductBreadcrumb.productInfo = caller.productInfo;
                    AddProduct(caller.GetComponent<MelcherButton>());
                    return;
                }

                SetProductBreadcrumbTarget(caller.productInfo);

                if (caller.productInfo.category.IsFinish())
                {
                    EventBridge.Designer.onFinishMaterialChanged.Invoke(caller.productInfo, caller.productInfo.category);
                    return;
                }
            }

            m_CategoryToolbar.SetVisibility(ToolbarContent.Visibility.InBackground, ToolbarContent.Duration.Tween);
            m_ManufacturerToolbar.SetVisibility(ToolbarContent.Visibility.InBackground, ToolbarContent.Duration.Tween);
            m_ProductToolbar.SetVisibility(ToolbarContent.Visibility.InBackground, ToolbarContent.Duration.Tween);
            m_ProductActionToolbar.SetVisibility(ToolbarContent.Visibility.InForeground, ToolbarContent.Duration.Tween);

            // TODO: Show valid placement locations on the cabinets
        }

        protected bool SetProductBreadcrumbTarget(ProductInfo targetProductInfo)
        {
            m_ProductBreadcrumb.productInfo = targetProductInfo;

            ProductVisual visual =
                m_ProductVisualDatabase.FindRuntimeVisualByID(
                    targetProductInfo.visualId
                );

            if (visual && visual.info.thumbnail)
            {
                m_ProductBreadcrumb.sprite = visual.info.thumbnail;
            }

            m_ProductBreadcrumb.SetErrorHighlightVisible(
                !visual || targetProductInfo.hasErrors
            );

            // Show the next/previous variant buttons if the chosen product
            // is a variant.
            m_ProductPreviousVariantButton.gameObject.SetActive(m_ProductBreadcrumb.belongsToProductVariantChain);
            m_ProductNextVariantButton.gameObject.SetActive(m_ProductBreadcrumb.belongsToProductVariantChain);

            // Start loading the product early to minimize perceived delay
            // when it is finally confirmed.

            if (visual)
            {
                ProductVisualContentLoader.FirstFromActiveScene()
                    .ProductVisualToContent(visual).Forget();
            }
            else
            {
                MelcherLog.LogError("Product ID not found in Product Visual Database: " + targetProductInfo.visualId, m_ProductVisualDatabase);

                if (!Application.isEditor && !ProFitGlobals.isVerbose)
                {
                    return false;
                }
            }

            return true;
        }

        protected void AddProduct(MelcherButton caller)
        {
            ProductInfo productInfo = m_ProductBreadcrumb.productInfo;
            MelcherLog.Log("Trying to add: " + productInfo.name);

            if (productInfo.category == ProductCategory.CabinetSection)
            {
                EventBridge.Designer.onTryAddSection.Invoke(productInfo, m_CabinetBeingSwapped);
            }
            else
            {
                // Minimize confusion for users: show a loading spinner when
                // adding a product and prevent the button from being triggered
                // multiple times while the asset is loading.

                m_ProductAddButton.interactable = false;
                m_ProductAddButtonSpinner.SetVisible(true);

                EventBridge.Designer.onTryAddProduct(
                    productInfo,
                    onComplete: _ =>
                    {
                        m_ProductAddButton.interactable = true;
                        m_ProductAddButtonSpinner.SetVisible(false);
                    }
                );
            }
        }

        protected void ShowProductInfo(MelcherButton caller)
        {
            ProductInfo productInfo = m_ProductBreadcrumb.productInfo;

            ProductVisual productVisual =
                m_ProductVisualDatabase.FindRuntimeVisualByID(
                    productInfo.visualId
                );

            MelcherLog.Log("Trying to show product info: " + productInfo.name);

            EventBridge.Designer.onAboutProduct?.Invoke(productInfo, productVisual);
        }

        protected void ShowPreviousProductVariant(MelcherButton caller)
        {
            ShowNextOrPreviousProductVariant(next: false);
        }

        protected void ShowNextProductVariant(MelcherButton caller)
        {
            ShowNextOrPreviousProductVariant(next: true);
        }

        protected void ShowNextOrPreviousProductVariant(bool next)
        {
            List<int> productVariantIds =
                ProFitGlobals.catalogData.GetProductVariantList(m_ProductBreadcrumb.productInfo);

            // We might encounter an error trying to display the next/previous
            // product variant. If this happens, we'll keep trying different
            // product variants until one succeeds or we run out of iterations.
            int maxIterations = productVariantIds.Count;

            int initialProductVaantIndex =
                m_ProductBreadcrumb.productVariantIndex;

            for (int i = 1; i <= maxIterations; ++i)
            {
                int variantIndex =
                    next ?
                        initialProductVaantIndex + i :
                        initialProductVaantIndex - i;

                variantIndex =
                    MelcherCommon.AbsMod(variantIndex, productVariantIds.Count);

                int productIdAtVariantIndex = productVariantIds[variantIndex];

                ProductInfo productInfoForVariant =
                    ProFitGlobals.catalogData.FindProductInfoById(
                        productIdAtVariantIndex,
                        findCabinetSection: false
                    );

                if (productInfoForVariant == null)
                {
                    MelcherLog.LogError($"Failed to find product variant with ID {productIdAtVariantIndex} (baseProductId: {productVariantIds[0]})");
                    continue;
                }

                if (!SetProductBreadcrumbTarget(productInfoForVariant))
                {
                    MelcherLog.LogError($"Failed to display variant: {productInfoForVariant.name} (id: {productInfoForVariant.id}, baseProductId: {productVariantIds[0]})");
                    continue;
                }

                // Succeeded.
                break;
            }
        }

        protected void ShowProductVariants(MelcherButton caller)
        {
        }

        protected void OnBreadcrumbButtonQuickHintManuallyClosed(QuickHint sender)
        {
            s_BreadcrumbButtonHintTaught = true;
        }

        protected void ResetToolbar()
        {
            SetToolbarVisibility(visible: false, ToolbarContent.Duration.Immediate);

            m_CategoryToolbar.SetVisibility(ToolbarContent.Visibility.InForeground, ToolbarContent.Duration.Immediate);
            m_ManufacturerToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Immediate);
            m_ProductToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Immediate);
            m_ProductSearchToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Immediate);
            m_ProductActionToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Immediate);

            m_SelectedProductToolbar.SetVisibility(ToolbarContent.Visibility.Collapsed, ToolbarContent.Duration.Immediate);

            // Reset/forget the toolbar button highlights
            ToolbarCategoryButton.ForgetHighlight();
            ToolbarGroupButton.ForgetHighlight();
            ToolbarProductButton.ForgetHighlight();
        }

        protected async UniTask OnHighlightChanged(GameObject highlightedObject)
        {
            m_ToolbarCabinetProductPredicate = null;

            Occupant occupant       = null;
            Cabinet cabinet         = null;
            CabinetBacking backing  = null;

            if (highlightedObject)
            {
                highlightedObject.TryGetComponent(out occupant);
                highlightedObject.TryGetComponent(out cabinet);
                highlightedObject.TryGetComponent(out backing);
            }

            bool isObjectWithDetails = occupant || cabinet || backing;

            bool isToolbarOpen = m_ToolbarAccordion.isExpanded;

            m_HighlightedProductContent.SetSelectedObject(highlightedObject);

            // Expand the toolbar if it's not open
            if (isObjectWithDetails && !isToolbarOpen)
            {
                SetToolbarVisibility(visible: true, ToolbarContent.Duration.Tween);
            }

            ToolbarContent.Visibility visibility =
                isObjectWithDetails ? ToolbarContent.Visibility.InForeground : ToolbarContent.Visibility.Collapsed;

            ToolbarContent.Duration duration =
                isToolbarOpen ? ToolbarContent.Duration.Tween : ToolbarContent.Duration.Immediate;

            // Show the selected product toolbar content
            m_SelectedProductToolbar.SetVisibility(visibility, duration);

            if (isObjectWithDetails)
            {
                // Set the toolbars up in background if visible
                if (m_ProductActionToolbar.visibility == ToolbarContent.Visibility.InForeground)
                {
                    m_ToolbarBeforeHighlight = m_ProductActionToolbar;
                }
                else if (m_ProductToolbar.visibility == ToolbarContent.Visibility.InForeground)
                {
                    m_ToolbarBeforeHighlight = m_ProductToolbar;
                }
                else if (m_ManufacturerToolbar.visibility == ToolbarContent.Visibility.InForeground)
                {
                    m_ToolbarBeforeHighlight = m_ManufacturerToolbar;
                }
                else if (m_CategoryToolbar.visibility == ToolbarContent.Visibility.InForeground)
                {
                    m_ToolbarBeforeHighlight = m_CategoryToolbar;
                }

                MelcherLog.LogEditorOnly("Toolbar before highlight: " + m_ToolbarBeforeHighlight, m_ToolbarBeforeHighlight);


                m_ToolbarBeforeHighlight?.SetVisibility(
                    ToolbarContent.Visibility.InBackground,
                    duration
                );
            }
            else
            {
                // Restore the last toolbar's visibility
                m_ToolbarBeforeHighlight?.SetVisibility(ToolbarContent.Visibility.InForeground, duration);
            }

            await UniTask.CompletedTask;
        }

        protected void OnBuildingMethodChanged(KitchenBuildingMethod newMethod)
        {
            int newCabinetFinishesButtonSiblingIndex =
                m_CabinetFinishesButtonDefaultSiblingIndex;

            if (newMethod == KitchenBuildingMethod.Sections)
            {
                // In Stoll, high bar tops are 14" deep.
                CuboidPadding overhangInMeters =
                    m_HighBarTopPrefab.countertopOverhangInMeters;

                // 10" here to account for overhang at the front and riser
                // thickness (1" and 3", at the moment).
                overhangInMeters.towardMaxZ = 10.0f * Units.Inches2Meters;

                m_HighBarTopPrefab.countertopOverhangInMeters = overhangInMeters;

                // TODO: This value is never restored if the user switches to
                //       a different building method. This is currently not
                //       possible, but still. -Ryan

                // TODO: This is also awkward that its happening in the toolbar
                //       UI script. It should ideally be in the design scene
                //       or kitchen script.
            }

            if (newMethod == KitchenBuildingMethod.Catalog)
            {
                // Move the cabinet finishes category button next to the door
                // styles category button.
                newCabinetFinishesButtonSiblingIndex =
                    m_DoorStylesButton.transform.GetSiblingIndex() + 1;
            }

            m_CabinetFinishesButton.transform.SetSiblingIndex(
                newCabinetFinishesButtonSiblingIndex
            );

            SetupCategoryToolbar();
        }

        /// <summary>
        ///     Cache the available products by their product categories for
        ///     this kitchen. This is intended to reduce the response delay when
        ///     the toolbar's category buttons ("Add Grill", "Add Cabinets",
        ///     etc) are pressed.
        /// </summary>
        protected void CacheAvailableProducts()
        {
            // Rebuild the product list caches entirely. Depending on which
            // mode the user is in (e.g., Catalog), the available products
            // can vary greatly between kitchens.
            m_AvailableProductsByProductCategory.Clear();

            Kitchen kitchen = Kitchen.instance;

            BrandCatalog selectedBrandCatalog = kitchen.selectedBrandCatalog;

            int defaultManufacturerId = ProFitGlobals.appProfile.defaultManufacturerId;

            // For dealers who have at least one manufacturer in their
            // whitelist, we will remove products from all manufacturers not in
            // the whitelist.
            bool useDealerSelectedWhitelist =
                ProFitGlobals.userProfile.dealerSelectedManufacturerIds != null &&
                ProFitGlobals.userProfile.dealerSelectedManufacturerIds.Count > 0;

            foreach (ProductInfo productInfo in ProFitGlobals.runtimeProducts)
            {
                if (productInfo == null)
                {
                    continue;
                }

                // Special products (like backings and plunges/shelves) are
                // accessible to every kitchen and therefore bypass all of the
                // whitelist checks imposed on other products.
                if (!productInfo.category.IsBacking() && productInfo.category is not ProductCategory.Shelf)
                {
                    if (!IsManufacturerIdAdminWhitelisted(productInfo.manufacturerId))
                    {
                        // This product is forbidden by the Pro-Fit admin/dashboard.
                        continue;
                    }

                    if (useDealerSelectedWhitelist &&
                        productInfo.manufacturerId != defaultManufacturerId &&
                        !ProFitGlobals.userProfile.dealerSelectedManufacturerIds.Contains(productInfo.manufacturerId))
                    {
                        // This product is forbidden by the user in their settings.
                        continue;
                    }

                    if (selectedBrandCatalog)
                    {
                        if (productInfo.category == ProductCategory.CabinetSection &&
                            selectedBrandCatalog.info.cabinetManufacturerId != productInfo.manufacturerId)
                        {
                            // The cabinet is not allowed by this brand catalog.
                            continue;
                        }
                        else
                        {
                            // Ensure this non-cabinet product is allowed by the
                            // brand catalog.
                            bool isAllowed = false;

                            if (productInfo.manufacturerId != defaultManufacturerId &&
                                kitchen.selectedBrandCatalog.info.availableManufacturerIds.Contains(productInfo.manufacturerId))
                            {
                                isAllowed |= true;
                            }

                            if (!isAllowed &&
                                productInfo.groupId != 0 &&
                                kitchen.selectedBrandCatalog.info.availableGroupIds.Contains(productInfo.groupId))
                            {
                                isAllowed |= true;
                            }

                            if (!isAllowed)
                            {
                                // The product is not allowed neither a selected
                                // manufacturer nor group.
                                continue;
                            }
                        }
                    }
                }

                // Retrieve (or make) the list that will hold the cached
                // available products for this product category.
                List<ProductInfo> allowedProductsInThisCategory = null;

                if (!m_AvailableProductsByProductCategory.TryGetValue(productInfo.category, out allowedProductsInThisCategory) ||
                    allowedProductsInThisCategory == null)
                {
                    // A list does not exist yet. Make one.
                    allowedProductsInThisCategory = new();
                    m_AvailableProductsByProductCategory[productInfo.category] = allowedProductsInThisCategory;
                }

                Debug.Assert(allowedProductsInThisCategory != null, $"A list for this product type ({productInfo.category}) was somehow not created. Bug in code.");

                allowedProductsInThisCategory.Add(productInfo);
            }
        }

        protected IEnumerable<ProductInfo> GetAvailableProductsInProductCategory(ProductCategory category)
        {
            IEnumerable<ProductInfo> productsInCategory = null;

            if (m_AvailableProductsByProductCategory.TryGetValue(category, out var productList))
            {
                productsInCategory = productList;
            }

            if (productsInCategory == null)
            {
                productsInCategory = Array.Empty<ProductInfo>();
            }

            return productsInCategory;
        }

        protected async UniTask OnBrandCatalogChanged(BrandCatalog brandCatalog)
        {
            SetupCategoryToolbar();

            // Brand catalogs can override low bar top's overhang.

            CuboidPadding lowBarTopCountertopOverhangInMeters =
                m_LowBarTopPrefab.countertopOverhangInMeters;

            lowBarTopCountertopOverhangInMeters.towardMaxZ =
                brandCatalog ?
                    brandCatalog.lowBarTopLengthInMeters :
                    15.0f * Units.Inches2Meters;

            m_LowBarTopPrefab.countertopOverhangInMeters =
                lowBarTopCountertopOverhangInMeters;

            // Brand catalogs can override high bar top's overhang.

            CuboidPadding highBarTopOverhangInMeters =
                m_HighBarTopPrefab.countertopOverhangInMeters;

            highBarTopOverhangInMeters.towardMaxZ =
                brandCatalog ?
                    brandCatalog.highBarTopOverhangInMeters :
                    10.0f * Units.Inches2Meters;

            m_HighBarTopPrefab.countertopOverhangInMeters =
                highBarTopOverhangInMeters;

            // Mistakenly added this thinking it was for Gemini but it was
            // actually a request for Stoll. Will leave this in incase we need
            // this later. -Ryan

            // Allow the catalog to override the material style of the cabinet
            // backings (high bar top and backsplash)
            //CabinetBacking.RiserMaterialMode riserMaterialMode =
            //    brandCatalog ?
            //        brandCatalog.backingRiserMaterialMode :
            //        CabinetBacking.RiserMaterialMode.UseCountertopMaterial;

            // We do not need to override the high bar top override prefab. We
            // assume that it is already configured correctly for its own
            // catalog. (`brandCatalog.highBarTopOverridePrefab`)

            //m_HighBarTopPrefab.riserMaterialMode = riserMaterialMode;
            //m_BacksplashPrefab.riserMaterialMode = riserMaterialMode;

            if (!brandCatalog)
            {
                return;
            }

            ComponentsInChildrenEnumerator<ToolbarCategoryButton> categoryButtons =
                new (
                    m_CategoryToolbar.GetScrollContent()
                );

            while (categoryButtons.MoveNext())
            {
                ToolbarCategoryButton categoryButton = categoryButtons.Current;
                ProductCategory category = categoryButton.category;

                bool allowed = categoryButton.gameObject.activeSelf;

                if (categoryButton == m_ToeKickButton)
                {
                    allowed &= brandCatalog.showToeKickButton;
                }
                else if (category == ProductCategory.Backsplash)
                {
                    allowed &= brandCatalog.hasBacksplashes;
                }
                else if (category == ProductCategory.BarTopHigh)
                {
                    allowed &= brandCatalog.hasHighBarTops;
                }
                else if (category == ProductCategory.BarTopLow)
                {
                    allowed &= brandCatalog.hasLowBarTops;
                }
                else if (category == ProductCategory.GrillWithInsulatedJacket)
                {
                    allowed &= brandCatalog.hasGrillsWithInsulatedJackets;
                }
                else if (category == ProductCategory.BurnerWithInsulatedJacket)
                {
                    allowed &= brandCatalog.hasBurnersWithInsulatedJackets;
                }
                else if (category == ProductCategory.Cookware)
                {
                    allowed &= brandCatalog.hasCookware;
                }
                else if (category == ProductCategory.Sink)
                {
                    allowed &= brandCatalog.hasSinksAndFridges;
                }
                else if (category == ProductCategory.Grill)
                {
                    allowed &= brandCatalog.hasGrills;
                }
                else if (category == ProductCategory.Burner)
                {
                    allowed &= brandCatalog.hasBurners;
                }

                categoryButton.gameObject.SetActive(allowed);
            }

            await UniTask.CompletedTask;
        }

        protected async UniTask OnKitchenReady()
        {
            // Rebuild the available products whenever the brand catalog
            // changes.
            CacheAvailableProducts();
        }

        protected void OnCabinetSwapStart(Cabinet cabinet)
        {
            m_CabinetBeingSwapped = cabinet;
        }

        protected void OnCabinetSwapCanceled()
        {
            m_CabinetBeingSwapped = null;
        }

        /// <summary>
        ///     Updates the label for the button to properly reflect whether it
        ///     will add or remove Challenger's premium cabinet finish package.
        ///     This will pulled out into its own method because the button's
        ///     label needs to update in more than one place.
        /// </summary>
        /// <param name="button">
        ///     The toolbar button that is used to add or remove the premium
        ///     cabinet finish package.
        /// </param>
        /// <param name="hasPremiumFinish">
        ///     Whether the kitchen has a premium finish at the moment.
        /// </param>
        protected void UpdateChallengerPremiumCabinetFinishPackageButtonLabel(ToolbarGroupButton button, bool hasPremiumFinish)
        {
            button.phrase =
                hasPremiumFinish ?
                    "Remove Premium Finish Package" :
                    "Add Premium Finish Package";
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DesignerToolbarUI))]

        [UnityEngine.Serialization.FormerlySerializedAs("m_ProductVisualCatalog")]
        [SerializeField, MelcherAssetField]
        protected ProductVisualDatabase m_ProductVisualDatabase = null;

        [Space]


        [SerializeField] protected CabinetBacking m_BacksplashPrefab = null;
        [UnityEngine.Serialization.FormerlySerializedAs("m_BarTopPrefab")]
        [SerializeField] protected CabinetBacking m_HighBarTopPrefab = null;
        [SerializeField] protected CabinetBacking m_LowBarTopPrefab = null;

        [Space]

        [SerializeField] protected MelcherToggle m_ToolbarToggleButton = null;

        [SerializeField] protected Accordion m_ToolbarAccordion = null;

        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_BackingIconColor = default;

        [Tooltip("List of masks to enabled on Awake. This lets the UI be " +
            "visible in the editor's scene view until the game starts.")]
        [SerializeField] protected Mask[] m_ToolbarMasks = null;

        [Space]
        [SerializeField] protected ToolbarContent m_CategoryToolbar = null;
        [SerializeField] protected ToolbarContent m_ManufacturerToolbar = null;
        [SerializeField] protected ToolbarContent m_ProductToolbar = null;
        [SerializeField] protected ToolbarContent m_ProductSearchToolbar = null;
        [SerializeField] protected ToolbarContent m_ProductActionToolbar = null;
        [SerializeField] protected ToolbarContent m_SelectedProductToolbar = null;

        [Space]
        // UI controls within m_CategoryToolbar
        [SerializeField] protected ToolbarCategoryButton m_StainlessSteelBurnerPlungeButton = default;
        [SerializeField] protected ToolbarCategoryButton m_ToeKickButton = default;
        [UnityEngine.Serialization.FormerlySerializedAs("m_AddSectionButton")]
        [SerializeField] protected ToolbarCategoryButton m_CabinetSectionButton = default;
        [UnityEngine.Serialization.FormerlySerializedAs("m_CabinetLayoutButtonSprite")]
        [SerializeField] protected Sprite m_CabinetSectionButtonSprite = default;
        [SerializeField] protected ToolbarCategoryButton m_DoorStylesButton = default;
        [SerializeField] protected ToolbarCategoryButton m_DoorHandleStylesButton = default;
        [SerializeField] protected ToolbarCategoryButton m_CabinetFinishesButton = default;
        [SerializeField] protected ToolbarCategoryButton m_EndPanelsButton = default;
        [SerializeField] protected ToolbarCategoryButton m_BackPanelsButton = default;

        [Space]
        // UI controls within m_SelectedProductToolbar - Should be moved out to a custom script
        [SerializeField] protected MelcherButton m_ProductAddButton = null;
        [SerializeField] protected Spinner m_ProductAddButtonSpinner = null;
        [SerializeField] protected MelcherButton m_ProductInfoButton = null;
        [SerializeField] protected MelcherButton m_ProductPreviousVariantButton = null;
        [SerializeField] protected MelcherButton m_ProductNextVariantButton = null;
        [SerializeField] protected MelcherButton m_ProductVariantsButton = null;

        [Space]
        [SerializeField] protected ToolbarCategoryButton m_CategoryBreadcrumb = null;
        [SerializeField] protected ToolbarGroupButton m_ManufacturerBreadcrumb = null;
        [SerializeField] protected ToolbarProductButton m_ProductBreadcrumb = null;

        [Space]
        [SerializeField] protected ToolbarGroupButton m_ManufacturerButtonPrefab = null;
        [SerializeField] protected ToolbarProductButton m_ProductButtonPrefab = null;

        [Space]
        [SerializeField] protected DesignerToolbarContentHighlightedProduct m_HighlightedProductContent = null;

        [Space]

        [SerializeField] protected QuickHint m_BreadcrumbButtonQuickHint = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        /// <summary>
        ///     Cached lists of available products for this kitchen, grouped by
        ///     their product category. This is intended to improve response
        ///     times when building the product toolbar. The lists are computed
        ///     when the kitchen is created/loaded.
        /// </summary>
        protected Dictionary<ProductCategory, List<ProductInfo>> m_AvailableProductsByProductCategory = new ();

        protected ToolbarContent m_ToolbarBeforeHighlight;

        protected int m_LastUsedDoorStyleId;
        protected int m_LastUsedDoorHandleStyleId;

        protected int m_CabinetFinishesButtonDefaultSiblingIndex;

        /// <summary>
        ///     This predicate is used to filter which cabinets can appear in
        ///     the cabinet product toolbar. When it is <see langword="null"/>,
        ///     all cabinets are allowed.
        /// </summary>
        protected Func<ProductInfo, bool> m_ToolbarCabinetProductPredicate;

        /// <summary>
        ///     The cabinet the user had selected in the designer scene when
        ///     they clicked the Swap Cabinet button on the toolbar. When a
        ///     new cabinet is being added and a valid location must be
        ///     located, this cabinet's location will be valid.
        /// </summary>
        protected Cabinet m_CabinetBeingSwapped;

        /// <summary>
        ///     Have this value be preserved over the life of the app for now.
        ///     Otherwise, it may get super annoying for experienced users or
        ///     in a kiosk environment.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.ResetToTypeDefault]
        protected static bool s_BreadcrumbButtonHintTaught;

    }   // class DesignerToolbarUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
