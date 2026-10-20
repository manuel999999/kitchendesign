namespace Melcher
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using System.Linq;
    using UnityEngine;

    public class CabinetShapeUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ClearForm()
        {
            m_ShapeToggleGroup.allowSwitchOff = true;
            m_ShapeToggleGroup.SetAllTogglesOff(sendCallback: false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_StraightToggle.onValueChanged += OnStraightShapeToggleChanged;
            m_LToggle.onValueChanged        += OnLShapeToggleChanged;
            m_DiagonalLToggle.onValueChanged += OnDiagonalLToggleChanged;
            m_UToggle.onValueChanged        += OnUShapeToggleChanged;
            m_GalleyToggle.onValueChanged   += OnGalleyShapeToggleChanged;

            m_BackButton.onClick += OnBackButtonClicked;
            m_NextButton.onClick += OnNextButtonClicked;

            // Allow app profiles to customize which kitchen styles are
            // available.
            UpdateToggleVisibility(m_StraightToggle, KitchenStyle.Straight);
            UpdateToggleVisibility(m_LToggle, KitchenStyle.L);
            UpdateToggleVisibility(m_DiagonalLToggle, KitchenStyle.DiagonalL);
            UpdateToggleVisibility(m_UToggle, KitchenStyle.U);
            UpdateToggleVisibility(m_GalleyToggle, KitchenStyle.Galley);

            static void UpdateToggleVisibility(MelcherToggle toggle, KitchenStyle style)
            {
                bool isAllowed = ProFitGlobals.appOptions.IsKitchenStyleAllowed(style);
                toggle.gameObject.SetActive(isAllowed);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_StraightToggle.onValueChanged -= OnStraightShapeToggleChanged;
            m_LToggle.onValueChanged        -= OnLShapeToggleChanged;
            m_DiagonalLToggle.onValueChanged -= OnDiagonalLToggleChanged;
            m_UToggle.onValueChanged        -= OnUShapeToggleChanged;
            m_GalleyToggle.onValueChanged   -= OnGalleyShapeToggleChanged;

            m_BackButton.onClick -= OnBackButtonClicked;
            m_NextButton.onClick -= OnNextButtonClicked;
        }

        protected void OnStraightShapeToggleChanged(IMelcherToggle toggle)
        {
            SelectLayout(m_StraightLayoutPrefab, toggle);
        }

        protected void OnLShapeToggleChanged(IMelcherToggle toggle)
        {
            SelectLayout(m_LLayoutPrefab, toggle);
        }

        protected void OnDiagonalLToggleChanged(IMelcherToggle toggle)
        {
            SelectLayout(m_DiagonalLLayoutPrefab, toggle);
        }

        protected void OnUShapeToggleChanged(IMelcherToggle toggle)
        {
            SelectLayout(m_ULayoutPrefab, toggle);
        }

        protected void OnGalleyShapeToggleChanged(IMelcherToggle toggle)
        {
            SelectLayout(m_GalleyLayoutPrefab, toggle);
        }

        protected void SelectLayout(CabinetLayout layoutPrefab, IMelcherToggle toggle)
        {
            if (toggle.isOn)
            {
                m_ShapeToggleGroup.allowSwitchOff = false;
                EventBridge.Designer.setCabinetLayout.Invoke(layoutPrefab);

                if (m_LastSelectedKitchenStyle != layoutPrefab.style)
                {
                    // Ensure the kitchen template UI (which may be shown next)
                    // doesn't retain a template that is no longer compatible
                    // with the kitchen style.
                    EventBridge.Designer.setKitchenTemplate(null);

                    m_LastSelectedKitchenStyle = layoutPrefab.style;
                }
            }
        }

        protected void OnBackButtonClicked(MelcherButton button)
        {
            m_CanvasGroup.interactable = false;

            if (Kitchen.instance.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
            {
                Hide();

                EventBridge.Designer.showBrandCatalogUi.Invoke();
            }
            else if (ProFitGlobals.appOptions.allowBuildingMethods)
            {
                Hide();

                EventBridge.Designer.showBuildingMethodsUi.Invoke();
            }
            else if (ProFitUtility.ShouldUseWhiteLabelMode())
            {
                Hide();

                EventBridge.Designer.showWhiteLabelClientInfoUi.Invoke();
            }
            else
            {
                EventBridge.Designer.loadMenuScene.Invoke();
            }
        }

        protected void OnNextButtonClicked(MelcherButton caller)
        {
            Kitchen kitchen = Kitchen.instance;
            BrandCatalog brandCatalog = kitchen.selectedBrandCatalog;

            if (m_ShapeToggleGroup.IsAnyToggleOn())
            {
                Hide();

                if (brandCatalog != null)
                {
                    IEnumerable<KitchenTemplate> templatesForStyle =
                        brandCatalog.kitchenTemplates.Where(
                            x => x.style == kitchen.selectedCabinetLayout.style
                        );

                    int templateCount = templatesForStyle.Count();

                    if (templateCount > 1)
                    {
                        // Prompt the user to pick a template.
                        EventBridge.Designer.showKitchenTemplateUi.Invoke();
                        return;
                    }
                    else if (templateCount == 1)
                    {
                        // Only one template to choose. Automatically use it.
                        EventBridge.Designer.setKitchenTemplate?.Invoke(templatesForStyle.ElementAt(0));
                    }

                    if(brandCatalog.cabinetCountertopThicknessInInches > 0)
                    {
                        CreateKitchenWithCustomCountertopThickness(brandCatalog.cabinetCountertopThicknessInInches);
                        return;
                    }
                }

                EventBridge.Designer.showCabinetDetailsUi.Invoke(CabinetOtherSizesUI.Mode.InitialCabinetSetup);
            }
        }

        protected void CreateKitchenWithCustomCountertopThickness(float thicknessInInches)
        {
            CabinetOtherSizeControl.Option countertopThickness = new CabinetOtherSizeControl.Option(thicknessInInches);

            float countertopThicknessInMeters = countertopThickness.sizeInMeters;
            CountertopType countertopType = CountertopType.Default;
            CuboidPadding countertopOverhangInInches = CuboidPadding.one;
            float baseCabinetDepthInMeters = Kitchen.instance.selectedBrandCatalog.standardCabinetDepthInMeters;

            EventBridge.Designer.setCountertopType?.Invoke(countertopType);
            EventBridge.Designer.setBaseCabinetDepth.Invoke(baseCabinetDepthInMeters);
            EventBridge.Designer.setCountertopThickness.Invoke(countertopThicknessInMeters);
            EventBridge.Designer.setCountertopOverhang.Invoke(countertopOverhangInInches * Const.Inches2Meters);
            EventBridge.Designer.createKitchen.Invoke().Forget();
    }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(CabinetShapeUI))]

        [SerializeField] protected MelcherToggleGroup m_ShapeToggleGroup = null;

        [Space]
        [SerializeField] protected MelcherToggle m_StraightToggle = null;
        [SerializeField] protected MelcherToggle m_LToggle = null;
        [SerializeField] protected MelcherToggle m_DiagonalLToggle = null;
        [SerializeField] protected MelcherToggle m_UToggle = null;
        [SerializeField] protected MelcherToggle m_GalleyToggle = null;

        [Space]
        [SerializeField, MelcherAssetField] protected CabinetLayout m_StraightLayoutPrefab = null;
        [SerializeField, MelcherAssetField] protected CabinetLayout m_LLayoutPrefab = null;
        [SerializeField, MelcherAssetField] protected CabinetLayout m_DiagonalLLayoutPrefab = null;
        [SerializeField, MelcherAssetField] protected CabinetLayout m_ULayoutPrefab = null;
        [SerializeField, MelcherAssetField] protected CabinetLayout m_GalleyLayoutPrefab = null;

        [Space]
        [SerializeField] protected MelcherButton m_BackButton = null;
        [SerializeField] protected MelcherButton m_NextButton = null;

        protected KitchenStyle m_LastSelectedKitchenStyle;

    }   // class CabinetShapeUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-30
/// ===========================================================================
