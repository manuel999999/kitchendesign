namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using DG.Tweening;

    public static class DOTweenExtensions
    {
        // =====================================================================
        // RectTransform
        // =====================================================================

        public static Tween DOOffsetMin(this RectTransform target, Vector2 endValue, float duration)
        {
            return DOTween.To(
                getter: () => target.offsetMin,
                setter: v2 => target.offsetMin = v2,
                endValue,
                duration
            ).SetTarget(target);
        }

        public static Tween DOOffsetMax(this RectTransform target, Vector2 endValue, float duration)
        {
            return DOTween.To(
                getter: () => target.offsetMax,
                setter: v2 => target.offsetMax = v2,
                endValue,
                duration
            ).SetTarget(target);
        }

        public static Tween DOOffsetUniform(this RectTransform target, Vector2 endValue, float duration)
        {
            return DOTween.To(
                getter: () => target.offsetMin,
                setter: v2 =>
                {
                    target.offsetMin = v2;
                    target.offsetMax = v2;
                },
                endValue,
                duration
            );
        }


        // =====================================================================
        // CanvasRenderer
        // =====================================================================

        /// <summary>
        /// Tween the alpha of a UI canvas renderer. This is discrete from the
        /// UI's color and so it is useful for fading objects that are slightly
        /// transparent by default.
        /// </summary>
        public static Tween DOAlpha(this CanvasRenderer target, float endValue, float duration)
        {
            return DOTween.To(
                getter: () => target.GetAlpha(),
                setter: f => target.SetAlpha(f),
                endValue,
                duration
            ).SetTarget(target);
        }
    }   // class DOTweenExtensions

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
