namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using DG.Tweening;

    /// <summary>
    /// A Toast is a short message that displays on the screen for a short
    /// amount of time and then disappears automatically without disrupting
    /// user interaction with the app.
    /// </summary>
    public class ToastUI : MelcherUI
    {
        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        /// Show the message and then fade out.
        /// </summary>
        public void ShowToast(string toast, float holdDuration)
        {
            m_ToastCanvasGroup.alpha = 0.0f;
            m_ToastLabel.phrase = toast;

            if (holdDuration <= 0.0f)
            {
                m_ToastSequence.KillIfActive();
            }
            else
            {
                m_ToastSequence.Reset(
                    DOTween.Sequence()
                        .SetLink(m_ToastCanvasGroup.gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                        .Join(
                            m_ToastCanvasGroup.DOFade(endValue: 1.0f, duration: 0.1f)
                                .SetEase(Ease.Linear)
                        )
                        .AppendInterval(holdDuration)
                        .Append(
                            m_ToastCanvasGroup.DOFade(endValue: 0.0f, duration: 0.5f)
                                .SetEase(Ease.Linear)
                        )
                );
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // Hide the toast until it is needed
            m_ToastCanvasGroup.alpha = 0.0f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EventBridge.Designer.showToast += ShowToast;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EventBridge.Designer.showToast -= ShowToast;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Header(nameof(ToastUI))]

        [SerializeField] protected CanvasGroup m_ToastCanvasGroup = default;
        [SerializeField] protected MelcherLocalizedText m_ToastLabel = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected RecyclableTween<Sequence> m_ToastSequence = new RecyclableTween<Sequence>();

    }   // class ToastUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-15
/// ===========================================================================
