namespace Melcher
{
    using UnityEngine;
    using DG.Tweening;
    using UnityEngine.UI;

    public class ToolbarScrollRect : MelcherMonoBehaviour
    {
        // ====================================================================
        // Methods
        // ====================================================================

        protected void OnEnable()
        {
            if (m_ScrollBackwardsButton)
            {
                m_ScrollBackwardsButton.onClick += ScrollBackwards;
            }

            if (m_ScrollForwardsButton)
            {
                m_ScrollForwardsButton.onClick += ScrollForwards;
            }
        }

        protected void OnDisable()
        {
            if (m_ScrollBackwardsButton)
            {
                m_ScrollBackwardsButton.onClick -= ScrollBackwards;
            }

            if (m_ScrollForwardsButton)
            {
                m_ScrollForwardsButton.onClick -= ScrollForwards;
            }
        }

        protected void ScrollForwards(MelcherButton caller)
        {
            SmoothScroll(forward: true);
        }

        protected void ScrollBackwards(MelcherButton caller)
        {
            SmoothScroll(forward: false);
        }

        protected void SmoothScroll(bool forward)
        {
            Vector2 viewportSize = m_ScrollRect.viewport.rect.size;

            // Calculate the size of the content that extends beyond the
            // viewport. It's this size that matters when normalizing the
            // scroll rect's scroll position. The offscreen content size is
            // clamped to zero to handle cases where the content is smaller
            // than the viewport.

            Vector2 offscreenContentSize = m_ScrollRect.content.rect.size - viewportSize;
            offscreenContentSize = Vector2.Max(offscreenContentSize, Vector2.zero);

            // Apply the scroll direction to receive a negative scroll offset
            // when the scroll isn't in the forwards direction

            float directionedScrollRatio = m_ScrollRatio;
            if (!forward)
            {
                directionedScrollRatio = -directionedScrollRatio;
            }

            // Calculate the absolute distance the scroll will move
            Vector2 absoluteScrollChange = viewportSize * directionedScrollRatio;

            // Normalize scroll distance to ScrollRect position value (0-1).
            // This may result in NaN's if the content is zero width
            Vector2 normalizedScrollChange = absoluteScrollChange / offscreenContentSize;

            MelcherLog.Log($"viewport={viewportSize}\ncontent={m_ScrollRect.content.rect.size}\noffscreen={offscreenContentSize}\nabsolute={absoluteScrollChange}\nnormalized={normalizedScrollChange}");

            Tween tween = null;

            Vector2 newNormalizedScroll =
                m_ScrollRect.normalizedPosition + normalizedScrollChange;

            newNormalizedScroll.x = Mathf.Clamp01(newNormalizedScroll.x);
            newNormalizedScroll.y = Mathf.Clamp01(newNormalizedScroll.y);

            if (m_Vertical && float.IsNormal(normalizedScrollChange.y))
            {
                m_ScrollRect.DOVerticalNormalizedPos(
                    endValue: newNormalizedScroll.y,
                    duration: m_ScrollDuration
                );
            }
            else if (!m_Vertical && float.IsNormal(normalizedScrollChange.x))
            {
                m_ScrollRect.DOHorizontalNormalizedPos(
                    endValue: newNormalizedScroll.x,
                    duration: m_ScrollDuration
                );
            }

            if (tween != null)
            {
                m_ScrollTween.Reset(tween);
            }
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Tooltip("How far the scroll view should scroll (normalized to the " +
            "ScrollRect's size) when a button is pressed.")]
        [SerializeField]
        protected float m_ScrollRatio = 0.9f;

        [Tooltip("Number of seconds the scroll tween will use.")]
        [SerializeField]
        protected float m_ScrollDuration = 0.2f;

        [SerializeField]
        protected bool m_Vertical = false;

        [Space]

        [SerializeField]
        protected MelcherButton m_ScrollBackwardsButton = default;

        [SerializeField]
        protected MelcherButton m_ScrollForwardsButton = default;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected ScrollRect m_ScrollRect = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        private RecyclableTween<Tween> m_ScrollTween = new RecyclableTween<Tween>();

    }   // class ToolbarScrollRect

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-03-03
/// ===========================================================================
