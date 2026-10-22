namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using DG.Tweening;

    public static class MenuTransition
    {
        /// <summary>
        ///     Pixel distance to slide. This is in pixels rather than a
        ///     normalized screen distance because these transitions are used
        ///     for both portrait and landscape screen ratios, and normalized
        ///     coordinates would result in drastically different movement
        ///     speeds.
        /// </summary>
        const float SlideDistance = 500.0f;

        public static void HalfSlideOut(MelcherUI ui, MelcherUITransition transition, RecyclableTween<Sequence> tween, TweenCallback onComplete)
        {
            RectTransform toSlide = ui.GetRectTransform();
            float x = SlideDistance;//slideDistance * SlideRatio;
            Vector2 targetOffsetMin = new Vector2(x, toSlide.offsetMin.y);
            Vector2 targetOffsetMax = new Vector2(x, toSlide.offsetMax.y);

            // When progressing, slide content out to the left
            if (transition.style == MelcherUITransitionStyle.Progress)
            {
                targetOffsetMin.x = -targetOffsetMin.x;
                targetOffsetMax.x = -targetOffsetMax.x;
            }

            float tweenDuration = 0.1f / transition.timeScale;

            tween.Reset(
                DOTween.Sequence()
                    .SetLink(toSlide.gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                    .Join(
                        toSlide.DOOffsetMin(targetOffsetMin, tweenDuration)
                            .SetEase(Ease.InOutSine)
                    )
                    .Join(
                        toSlide.DOOffsetMax(targetOffsetMax, tweenDuration)
                            .SetEase(Ease.InOutSine)
                    )
                    .Join(
                        ui.canvasGroup.DOFade(0.0f, tweenDuration)
                            .SetEase(Ease.Linear)

                    )
                    .OnComplete(onComplete)
            );
        }

        public static void HalfSlideIn(MelcherUI ui, MelcherUITransition transition, RecyclableTween<Sequence> tween, TweenCallback onComplete)
        {
            // onComplete?.Invoke();
            // return;

            RectTransform toSlide = ui.GetRectTransform();
            //float slideDistance = Mathf.Min(toSlide.rect.width, toSlide.rect.height);
            float x = SlideDistance;//slideDistance * SlideRatio;
            Vector2 startOffsetMin = new Vector2(x, toSlide.offsetMin.y);
            Vector2 startOffsetMax = new Vector2(x, toSlide.offsetMax.y);

            // When regressing, slide content in from the left
            if (transition.style != MelcherUITransitionStyle.Progress)
            {
                startOffsetMin.x = -startOffsetMin.x;
                startOffsetMax.x = -startOffsetMax.x;
            }

            toSlide.offsetMin = startOffsetMin;
            toSlide.offsetMax = startOffsetMax;
            ui.canvasGroup.alpha = 0.0f;

            float tweenDuration = 0.18f / transition.timeScale;

            tween.Reset(
                DOTween.Sequence()
                    .SetLink(toSlide.gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                    .Join(
                        toSlide.DOOffsetMin(new Vector2(0.0f, startOffsetMin.y), tweenDuration)
                            .SetEase(Ease.InOutSine)
                    )
                    .Join(
                        toSlide.DOOffsetMax(new Vector2(0.0f, startOffsetMax.y), tweenDuration)
                            .SetEase(Ease.InOutSine)
                    )
                    .Join(
                        ui.canvasGroup.DOFade(1.0f, tweenDuration)
                            .SetEase(Ease.Linear)
                    )
                    .OnComplete(onComplete)
            );
        }
    }   // class MenuTransition

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
