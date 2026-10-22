namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    ///     Manages a UI canvas that is used to prompt the player for what
    ///     type of kitchen designing method they want to use: Standard or
    ///     Sections.
    ///     <br /><br />
    ///
    ///     This is one of several pop-ups that may appear at the start of the
    ///     game. This UI is intended to be skipped if the <see
    ///     cref="AppOptionsSO"/> in Resources disallows building methods.
    /// </summary>
    public class BuildingMethodUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ClearForm()
        {
            m_MethodToggleGroup.allowSwitchOff = true;
            m_MethodToggleGroup.SetAllTogglesOff(sendCallback: false);
            m_NextButton.interactable = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_StandardToggle.onValueChanged += OnStandardBuildingMethodToggleChanged;
            m_SectionsToggle.onValueChanged += OnSectionsBuildingMethodToggleChanged;
            m_CatalogToggle.onValueChanged += OnCatalogBuildingMethodToggleChanged;

            m_BackButton.onClick += OnBackButtonClicked;
            m_NextButton.onClick += OnNextButtonClicked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_StandardToggle.onValueChanged -= OnStandardBuildingMethodToggleChanged;
            m_SectionsToggle.onValueChanged -= OnSectionsBuildingMethodToggleChanged;
            m_CatalogToggle.onValueChanged -= OnCatalogBuildingMethodToggleChanged;

            m_BackButton.onClick -= OnBackButtonClicked;
            m_NextButton.onClick -= OnNextButtonClicked;
        }

        protected void OnStandardBuildingMethodToggleChanged(IMelcherToggle toggle)
        {
            SelectBuildingMethod(toggle, KitchenBuildingMethod.Standard);
        }

        protected void OnSectionsBuildingMethodToggleChanged(IMelcherToggle toggle)
        {
            SelectBuildingMethod(toggle, KitchenBuildingMethod.Sections);
        }

        protected void OnCatalogBuildingMethodToggleChanged(IMelcherToggle toggle)
        {
            SelectBuildingMethod(toggle, KitchenBuildingMethod.Catalog);
        }

        protected void SelectBuildingMethod(IMelcherToggle toggle, KitchenBuildingMethod buildingMethod)
        {
            if (toggle.isOn)
            {
                m_MethodToggleGroup.allowSwitchOff = false;
                EventBridge.Designer.setBuildingMethod.Invoke(buildingMethod);
                m_NextButton.interactable = true;
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
            else
            {
                EventBridge.Designer.loadMenuScene.Invoke();
            }
        }

        protected void OnNextButtonClicked(MelcherButton caller)
        {
            if (m_MethodToggleGroup.IsAnyToggleOn())
            {
                Hide();

                if (m_CatalogToggle.isOn)
                {
                    EventBridge.Designer.showBrandCatalogUi.Invoke();
                }
                else
                {
                    EventBridge.Designer.showCabinetShapeUi.Invoke();
                }
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(CabinetShapeUI))]

        [SerializeField] protected MelcherToggleGroup m_MethodToggleGroup = null;

        [Space]
        [SerializeField] protected MelcherToggle m_StandardToggle = null;
        [SerializeField] protected MelcherToggle m_SectionsToggle = null;
        [SerializeField] protected MelcherToggle m_CatalogToggle = null;

        [Space]
        [SerializeField] protected MelcherButton m_BackButton = null;
        [SerializeField] protected MelcherButton m_NextButton = null;

    }   // class BuildingMethodUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-08-01
/// ===========================================================================
