namespace Melcher
{
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;

    public class MainUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ShowBackButton(bool active)
        {
            m_BackButton.gameObject.SetActive(active);
        }

        public void ShowFooter(bool active)
        {
            m_Footer.gameObject.SetActive(active);

            if (active)
            {
                // Force an immediate re-layout. Without this, the tab buttons
                // won't have the proper measurements and the indicator tween
                // will not work as intended
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Footer);
            }
        }

        public void SelectHomeTab()
        {
            TweenSelectionIndicatorTo(m_HomeTab.transform as RectTransform);
        }

        public void SelectClientsTab()
        {
            TweenSelectionIndicatorTo(m_ClientsTab.transform as RectTransform);
        }

        public void SelectUserProfileTab()
        {
            TweenSelectionIndicatorTo(m_ProfileTab.transform as RectTransform);
        }

        protected override void Awake()
        {
            base.Awake();

            AppSkin currentSkin = ProFitGlobals.appOptions.appSkin;

            m_HeaderBackground.color = currentSkin.primaryColor;

            m_HeaderLogo.sprite = currentSkin.menuHeaderLogo;

            m_HeaderLogo.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                currentSkin.menuHeaderLogoHeight
            );

            m_BackButton.gameObject.SetActive(false);

            m_ScreenOrientationButton.gameObject.SetActive(MelcherUnityUtility.isTargetMobilePlatform);

            // The footer is off by default
            m_Footer.gameObject.SetActive(false);

            m_FooterIndicator.GetComponent<Graphic>().color = currentSkin.toggleOnColor;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_QuitButton.gameObject.SetActive(
                MelcherUnityUtility.isTargetStandalonePlatform
            );

            m_BackButton.onClick += NavigateBackward;
            m_ScreenOrientationButton.onClick += ChangeScreenOrientation;
            m_QuitButton.onClick += ShowQuitPrompt;

            m_HomeTab.onValueChanged += OnHomeToggleChanged;
            m_ClientsTab.onValueChanged += OnClientsToggleChanged;
            m_ProfileTab.onValueChanged += OnProfileToggleChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_BackButton.onClick -= NavigateBackward;
            m_ScreenOrientationButton.onClick -= ChangeScreenOrientation;
            m_QuitButton.onClick -= ShowQuitPrompt;

            m_HomeTab.onValueChanged -= OnHomeToggleChanged;
            m_ClientsTab.onValueChanged -= OnClientsToggleChanged;
            m_ProfileTab.onValueChanged -= OnProfileToggleChanged;
        }

        protected void NavigateBackward(MelcherButton caller)
        {
            EventBridge.Menu.navigateBackward.Invoke();
        }

        protected void ChangeScreenOrientation(MelcherButton caller)
        {
            EventBridge.Menu.toggleScreenOrientation.Invoke();
        }

        protected void ShowQuitPrompt(MelcherButton caller)
        {
            EventBridge.Menu.showQuitPrompt.Invoke();
        }

        private void OnHomeToggleChanged(IMelcherToggle toggle)
        {
            if (toggle.isOn && m_Footer.gameObject.activeSelf)
            {
                EventBridge.Menu.showHomeUi?.Invoke();
            }
        }

        private void OnClientsToggleChanged(IMelcherToggle toggle)
        {
            if (toggle.isOn && m_Footer.gameObject.activeSelf)
            {
                EventBridge.Menu.showClientListUi?.Invoke();
            }
        }

        private void OnProfileToggleChanged(IMelcherToggle toggle)
        {
            if (toggle.isOn && m_Footer.gameObject.activeSelf)
            {
                EventBridge.Menu.showUserProfileUi?.Invoke();
            }
        }

        private void TweenSelectionIndicatorTo(RectTransform transformWidthToMatch)
        {
            Rect targetRect = transformWidthToMatch.GetRectInParentSpace();
            targetRect.xMin += 60.0f;
            targetRect.xMax -= 60.0f;

            Rect indicatorRect = m_FooterIndicator.GetRectInParentSpace();

            Vector2 offsetMin = m_FooterIndicator.offsetMin;
            Vector2 offsetMax = m_FooterIndicator.offsetMax;

            offsetMin.x += targetRect.xMin - indicatorRect.xMin;
            offsetMax.x += targetRect.xMax - indicatorRect.xMax;

            // Leftwards easing
            Ease minEase = Ease.OutExpo;
            Ease maxEase = Ease.InOutExpo;

            if (targetRect.xMin >= indicatorRect.xMax)
            {
                // Swap. Rightwards easing
                (minEase, maxEase) = (maxEase, minEase);
            }

            const float TweenDuration = 0.2f;

            // Animate the closer edge first and then the farther edge to match
            // the target transform's width
            m_FooterIndicatorTween.Reset(
                DOTween.Sequence()
                    .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                    .Join(
                        m_FooterIndicator.DOOffsetMin(offsetMin, TweenDuration)
                            .SetEase(minEase)
                    )
                    .Join(
                        m_FooterIndicator.DOOffsetMax(offsetMax, TweenDuration)
                            .SetEase(maxEase)
                    )
            );
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(MainUI))]

        [SerializeField] protected MelcherButton m_BackButton = null;
        [SerializeField] protected MelcherButton m_ScreenOrientationButton = null;
        [SerializeField] protected MelcherButton m_QuitButton = null;

        [Space]
        [SerializeField] protected Graphic m_HeaderBackground = default;
        [SerializeField] protected Image m_HeaderLogo = default;

        [Space]
        [SerializeField] protected RectTransform m_Footer = null;
        [SerializeField] protected MelcherToggle m_HomeTab = null;
        [SerializeField] protected MelcherToggle m_ClientsTab = null;
        [SerializeField] protected MelcherToggle m_ProfileTab = null;
        [SerializeField] protected RectTransform m_FooterIndicator = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected RecyclableTween<Sequence> m_FooterIndicatorTween = new RecyclableTween<Sequence>();

    }   // class MainUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
