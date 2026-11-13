namespace KitchenDesign.UI.DesignerScreens
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;
    using KitchenDesign.Shared.Enums;

    public class ToolbarContent : MelcherMonoBehaviour
    {
        public const float ExtraVerticalOffsetWhenHidden = 64.0f;

        // =====================================================================
        // Properties
        // =====================================================================

        public ToolbarContentVisibility visibility => m_LastVisibility;

        // =====================================================================
        // Methods
        // =====================================================================

        public RectTransform GetRectTransform()
        {
            return m_RectTransform;
        }

        public RectTransform GetScrollContent()
        {
            return m_ScrollView.content;
        }

        public void ResetScrollToStart()
        {
            m_ScrollView.horizontalNormalizedPosition = 0.0f;
        }

        public void SetVisibility(ToolbarContentVisibility visibility, ToolbarContentDuration duration)
        {
            m_LastVisibility = visibility;

            Vector2 offsetMin = Vector2.zero;
            Vector2 offsetMax = Vector2.zero;
            bool rootInteractable = true;
            bool listInteractable = false;
            float tintAlpha = 0.0f;
            Ease offsetEase = Ease.InQuad;
            Ease alphaEase = Ease.OutQuad;

            offsetMin.x = m_RectTransform.offsetMin.x;
            offsetMax.x = m_RectTransform.offsetMax.x;

            switch (visibility)
            {
            case ToolbarContentVisibility.Collapsed:
                rootInteractable = false;
                offsetMin.y = -m_RectTransform.rect.height - ExtraVerticalOffsetWhenHidden;
                offsetMax.y = -m_RectTransform.rect.height - ExtraVerticalOffsetWhenHidden;
                break;

            case ToolbarContentVisibility.InForeground:
                listInteractable = true;
                offsetEase = Ease.OutQuad;
                alphaEase = Ease.InQuad;
                break;

            case ToolbarContentVisibility.InBackground:
                tintAlpha = 1.0f;
                break;
            }

            m_CanvasGroup.interactable = rootInteractable;
            m_ListCanvasGroup.interactable = listInteractable;

            if (duration == ToolbarContentDuration.Immediate)
            {
                m_VisibilityTween.KillIfActive();
                m_RectTransform.offsetMin = offsetMin;
                m_RectTransform.offsetMax = offsetMax;
                m_InactiveTint.canvasRenderer.SetAlpha(tintAlpha);
            }
            else
            {
                const float TweenSeconds = 0.15f;

                m_VisibilityTween.Reset(
                    DOTween.Sequence()
                        .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                        .Join(
                            m_RectTransform.DOOffsetMin(offsetMin, TweenSeconds)
                                .SetEase(offsetEase)
                        )
                        .Join(
                            m_RectTransform.DOOffsetMax(offsetMax, TweenSeconds)
                                .SetEase(offsetEase)
                        )
                        .Join(
                            m_InactiveTint.canvasRenderer.DOAlpha(tintAlpha, TweenSeconds * 0.9f)
                                .SetEase(alphaEase)
                        )
                );
            }
        }

        /// <summary>
        ///     Destroy all elements in the scroll view's content. This may
        ///     not be an intended action for all toolbars. E.g., the
        ///     Highlighted Object Actions toolbar has a static list of
        ///     controls.
        /// </summary>
        public void DestroyScrollContent()
        {
            RectTransform scrollContent = m_ScrollView.content;

            int buttonCount = scrollContent.childCount;
            for (int i = buttonCount - 1; i >= 0; --i)
            {
                Destroy(scrollContent.GetChild(i).gameObject);
            }

        }

        protected void Awake()
        {
            m_UiExplode.SetExploded(false);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected Graphic m_InactiveTint = null;

        [SerializeField] protected CanvasGroup m_ListCanvasGroup = null;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected RectTransform m_RectTransform = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected CanvasGroup m_CanvasGroup = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected MelcherUIExplode m_UiExplode = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down)]
        protected ScrollRect m_ScrollView = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected RecyclableTween<Sequence> m_VisibilityTween = new RecyclableTween<Sequence>();

        protected ToolbarContentVisibility m_LastVisibility;

    }   // class ToolbarContent

}   // namespace KitchenDesign.UI.DesignerScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
