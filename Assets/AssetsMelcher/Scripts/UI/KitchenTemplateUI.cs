namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    ///     Allows the user to select a default kitchen design. This is used by
    ///     Gemini for select brand catalogs.
    /// </summary>
    public class KitchenTemplateUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ClearForm()
        {
            m_TemplateToggleGroup.allowSwitchOff = true;
            m_TemplateToggleGroup.SetAllTogglesOff(sendCallback: false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_TemplateToggleGroup.onToggleChanged += OnToggleChanged;

            m_BackButton.onClick += OnBackButtonClicked;
            m_NextButton.onClick += OnNextButtonClicked;

        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_TemplateToggleGroup.onToggleChanged -= OnToggleChanged;

            m_BackButton.onClick -= OnBackButtonClicked;
            m_NextButton.onClick -= OnNextButtonClicked;
        }

        protected override void DoShow()
        {
            base.DoShow();

            Kitchen kitchen = Kitchen.instance;
            KitchenStyle kitchenStyle = kitchen.selectedCabinetLayout.style;

            m_Title.phrase = $"{kitchenStyle}-Shape Templates";


            List<MelcherToggle> toggles = new();
            MelcherSearch.AllFromTransformDownward(toggles, m_TemplateToggleGroup.transform, minDistance: 1, count: 1, includeInactiveGameObjects: true);

            foreach (MelcherToggle toggle in toggles)
            {
                toggle.gameObject.SetActive(false);
            }

            int nextToggleIndex = 0;
            int? selectedToggleIndex = null;
            BrandCatalog brandCatalog = kitchen.selectedBrandCatalog;
            for (int i = 0; i < brandCatalog.kitchenTemplates.Count; ++i)
            {
                KitchenTemplate template = brandCatalog.kitchenTemplates[i];
                if (template.style != kitchenStyle)
                {
                    continue;
                }

                if (template == kitchen.selectedKitchenTemplate)
                {
                    selectedToggleIndex = nextToggleIndex;
                }

                MelcherToggle toggle = toggles[nextToggleIndex];

                ++nextToggleIndex;

                MelcherLocalizedText label =
                    toggle.GetComponentInChildren<MelcherLocalizedText>(includeInactive: true);

                label.phrase = template.displayName;

                Image image =
                    toggle.GetComponentInChildren<Image>(includeInactive: true);

                image.sprite = template.previewSprite;

                toggle.gameObject.SetActive(true);
            }

            m_TemplateToggleGroup.SetAllTogglesOff(instant: true, sendCallback: false);

            if (selectedToggleIndex.HasValue)
            {
                toggles[selectedToggleIndex.Value].SetIsOnWithoutNotify(newIsOn: true, instant: true);
            }
        }

        protected void OnToggleChanged(MelcherToggleGroup group, IMelcherToggle toggle)
        {
            if (!toggle.isOn)
            {
                return;
            }

            // Ensure one toggle is always active.
            m_TemplateToggleGroup.allowSwitchOff = false;

            MelcherToggle toggleComponent = (toggle as MelcherToggle);
            int toggleIndex = toggleComponent.transform.GetSiblingIndex();

            Kitchen kitchen = Kitchen.instance;

            KitchenStyle selectedStyle = kitchen.selectedCabinetLayout.style;

            KitchenTemplate template =
                kitchen.selectedBrandCatalog.kitchenTemplates
                    .Where(x => x.style == selectedStyle)
                    .Skip(toggleIndex)
                    .First();

            EventBridge.Designer.setKitchenTemplate.Invoke(template);
        }

        protected void OnBackButtonClicked(MelcherButton button)
        {
            m_CanvasGroup.interactable = false;

            Hide();

            EventBridge.Designer.showCabinetShapeUi.Invoke();
        }

        protected void OnNextButtonClicked(MelcherButton caller)
        {
            if (m_TemplateToggleGroup.IsAnyToggleOn())
            {
                Hide();
                EventBridge.Designer.showCabinetDetailsUi.Invoke(CabinetOtherSizesUI.Mode.InitialCabinetSetup);
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [MelcherHeader]

        [SerializeField] protected MelcherLocalizedText m_Title = null;

        [Space]
        [SerializeField] protected MelcherToggleGroup m_TemplateToggleGroup = null;

        [Space]
        [SerializeField] protected MelcherButton m_BackButton = null;
        [SerializeField] protected MelcherButton m_NextButton = null;

    }   // class KitchenTemplateUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-30
/// ===========================================================================
