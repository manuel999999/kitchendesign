namespace KitchenDesign.UI.MenuScreens
{
    using KitchenDesign.UI.Views;
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using KitchenDesign.Events;

    public class HomeUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        protected override void Awake()
        {
            base.Awake();

            m_GridBackground.color =
                AppProfileStore.Skin.primaryColor;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_StartCustomizingButton.onClick += OnStartCustomizing;

            m_HeroImage.sprite = AppProfileStore.Options.homeScreenImage;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_StartCustomizingButton.onClick -= OnStartCustomizing;
        }

        protected void OnStartCustomizing(MelcherButton caller)
        {
            EventMenu.OnCreateClientRequested.Raise();

            //EventBridge.Menu.hideMainUi.Invoke();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(HomeUI))]

        [SerializeField] protected Image m_GridBackground = default;

        [SerializeField] protected Image m_HeroImage = default;

        [SerializeField] protected MelcherButton m_StartCustomizingButton = null;

    }   // class HomeUI

}   // namespace KitchenDesign.UI.MenuScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
