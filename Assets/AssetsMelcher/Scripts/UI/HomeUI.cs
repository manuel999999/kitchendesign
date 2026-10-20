namespace Melcher
{
    using UnityEngine;
    using UnityEngine.UI;

    public class HomeUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        protected override void Awake()
        {
            base.Awake();

            m_GridBackground.color =
                ProFitGlobals.appOptions.appSkin.primaryColor;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_StartCustomizingButton.onClick += OnStartCustomizing;

            m_HeroImage.sprite = ProFitGlobals.appOptions.homeScreenImage;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_StartCustomizingButton.onClick -= OnStartCustomizing;
        }

        protected void OnStartCustomizing(MelcherButton caller)
        {
            EventBridge.Menu.pushCreateClientUi();

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

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
