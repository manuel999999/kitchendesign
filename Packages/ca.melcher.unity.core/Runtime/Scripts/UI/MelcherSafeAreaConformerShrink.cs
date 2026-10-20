namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// This script resizes its RectTransform to the size of the screen's
    /// safe area (as reported by Unity). The RectTransform must be resizable
    /// so this script will check that it is not on the topmost Canvas.
    /// </summary>
    [AddComponentMenu("UI/Shrink To Safe Area")]
    [RequireComponent(typeof(RectTransform))]
    public class MelcherSafeAreaConformerShrink : MelcherSafeAreaConformer
    {
        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Resize this RectTransform to match the screen's safe area.
        /// </summary>
        protected override void DoLayout()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (!m_InitialRectInCanvasSpace.HasValue)
            {
                m_InitialRectInCanvasSpace = m_RectTransform.GetRectInCanvasSpace();
            }

            Rect safeArea = CalculateScaledSafeArea(out float canvasScale);

            Rect rectInCanvasSpace = m_InitialRectInCanvasSpace.Value;

            if (rectInCanvasSpace.xMin < safeArea.xMin)
            {
                rectInCanvasSpace.xMin = safeArea.xMin;
            }

            if (rectInCanvasSpace.yMin < safeArea.yMin)
            {
                rectInCanvasSpace.yMin = safeArea.yMin;
            }

            if (rectInCanvasSpace.xMax > safeArea.xMax)
            {
                rectInCanvasSpace.xMax = safeArea.xMax;
            }

            if (rectInCanvasSpace.yMax > safeArea.yMax)
            {
                rectInCanvasSpace.yMax = safeArea.yMax;
            }

            m_RectTransform.SetRectFromCanvasSpaceRect(rectInCanvasSpace);
        }

        // =====================================================================
        // Internal members
        // =====================================================================

        protected Rect? m_InitialRectInCanvasSpace;

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        protected override void OnValidate()
        {
            base.OnValidate();
        }

#endif  // if UNITY_EDITOR

    }   // class MelcherSafeAreaConformerShrink

}   // namespace Melcher
