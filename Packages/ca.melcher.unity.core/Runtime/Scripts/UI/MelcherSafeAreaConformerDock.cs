namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// This script docks a RectTransform along one edge of the screen such
    /// that its content is shifted to fit within the safe area but the outer
    /// boundaries are extended to meet the screen edges.
    /// <br /><br />
    ///
    /// This is useful for panels with backgrounds that should extend to the
    /// screen's boundaries, but whose panel's content should still be contained
    /// within the safe area.
    /// <br /><br />
    ///
    /// In other words, this script is a good candidate for you if you have
    /// something like a toolbar that you want at the bottom of the screen,
    /// but you want to shift the toolbar's content up to be within the safe
    /// area. What you would do is have the toolbar use a <see
    /// cref="MelcherSafeAreaConformerDock"/> and a child container with a
    /// <see cref="MelcherSafeAreaConformerShrink"/>. The toolbar buttons would
    /// be placed on the child container and the toolbar root would remain
    /// at the bottom of the screen.
    /// </summary>
    [AddComponentMenu("UI/Dock To Safe Area")]
    [RequireComponent(typeof(RectTransform))]
    public class MelcherSafeAreaConformerDock : MelcherSafeAreaConformer
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public bool isCenterDocked => m_DockMode == DockMode.Center;

        // ====================================================================
        // Methods
        // ====================================================================

        protected override int DoGetLayoutOrder()
        {
            int order = m_LayoutOrder;

            if (m_DockMode == DockMode.Center)
            {
                if (m_LeftDock)
                {
                    order = Mathf.Max(m_LeftDock.GetLayoutOrder() + 1, order);
                }
                if (m_RightDock)
                {
                    order = Mathf.Max(m_RightDock.GetLayoutOrder() + 1, order);
                }
                if (m_TopDock)
                {
                    order = Mathf.Max(m_TopDock.GetLayoutOrder() + 1, order);
                }
                if (m_BottomDock)
                {
                    order = Mathf.Max(m_BottomDock.GetLayoutOrder() + 1, order);
                }
            }

            return order;
        }

        /// <summary>
        /// Resize this RectTransform to match the screen's safe area.
        /// </summary>
        protected override void DoLayout()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Rect safeArea = CalculateScaledSafeArea(out float canvasScale);

            Rect rectInCanvasSpace = m_RectTransform.GetRectInCanvasSpace();

            switch (m_DockMode)
            {
                default:
                    MelcherLog.LogError($"Unknown dock mode {m_DockMode}", this);
                    return;

                case DockMode.Left:
                    if (rectInCanvasSpace.xMax < safeArea.xMin + m_AnchoredSize)
                    {
                        // Shift right edge rightward to maintain the anchored
                        // width within the safe area
                        rectInCanvasSpace.xMax = safeArea.xMin + m_AnchoredSize;
                    }
                    break;

                case DockMode.Top:
                    if (rectInCanvasSpace.yMin > safeArea.yMax - m_AnchoredSize)
                    {
                        // Shift the bottom edge down to maintain the anchored
                        // height within the safe area
                        rectInCanvasSpace.yMin = safeArea.yMax - m_AnchoredSize;
                    }
                    break;

                case DockMode.Right:
                    if (rectInCanvasSpace.xMin > safeArea.xMax - m_AnchoredSize)
                    {
                        // Shift the left edge leftward to maintain the anchored
                        // width within the safe area
                        rectInCanvasSpace.xMin = safeArea.xMax - m_AnchoredSize;
                    }
                    break;

                case DockMode.Bottom:
                    if (rectInCanvasSpace.yMax < safeArea.yMin + m_AnchoredSize)
                    {
                        // Shift the top edge upward to maintain the anchored
                        // height within the safe area
                        rectInCanvasSpace.yMax = safeArea.yMin + m_AnchoredSize;
                    }
                    break;

                case DockMode.Center:
                    // Temporarily reset / remove offset from edges
                    rectInCanvasSpace.xMin -= m_RectTransform.offsetMin.x;
                    rectInCanvasSpace.yMin -= m_RectTransform.offsetMin.y;
                    rectInCanvasSpace.xMax -= m_RectTransform.offsetMax.x;
                    rectInCanvasSpace.yMax -= m_RectTransform.offsetMax.y;

                    if (IsDockValidAndActive(m_LeftDock))
                    {
                        rectInCanvasSpace.xMin = m_LeftDock.m_RectTransform.GetRectInCanvasSpace().xMax;
                    }
                    rectInCanvasSpace.xMin += m_CenterDockPadding.x;

                    if (IsDockValidAndActive(m_RightDock))
                    {
                        rectInCanvasSpace.xMax = m_RightDock.m_RectTransform.GetRectInCanvasSpace().xMin;
                    }
                    rectInCanvasSpace.xMax -= m_CenterDockPadding.y;

                    if (IsDockValidAndActive(m_BottomDock))
                    {
                        rectInCanvasSpace.yMin = m_BottomDock.m_RectTransform.GetRectInCanvasSpace().yMax;
                    }
                    rectInCanvasSpace.yMin += m_CenterDockPadding.w;

                    if (IsDockValidAndActive(m_TopDock))
                    {
                        rectInCanvasSpace.yMax = m_TopDock.m_RectTransform.GetRectInCanvasSpace().yMin;
                    }
                    rectInCanvasSpace.yMax -= m_CenterDockPadding.z;
                    break;
            }

            m_RectTransform.SetRectFromCanvasSpaceRect(rectInCanvasSpace);
        }

        private static bool IsDockValidAndActive(MelcherSafeAreaConformerDock dock)
        {
            return dock && dock.enabled && dock.gameObject.activeInHierarchy;
        }

        // =====================================================================
        // Inspector-serialized members
        // =====================================================================

        [Header(nameof(MelcherSafeAreaConformerDock))]

        [SerializeField]
        protected DockMode m_DockMode = DockMode.Top;

        [SerializeField]
        protected Vector4 m_CenterDockPadding = default;

        [SerializeField]
        protected MelcherSafeAreaConformerDock m_LeftDock = default;

        [SerializeField]
        protected MelcherSafeAreaConformerDock m_RightDock = default;

        [SerializeField]
        protected MelcherSafeAreaConformerDock m_TopDock = default;

        [SerializeField]
        protected MelcherSafeAreaConformerDock m_BottomDock = default;

        // ====================================================================
        // OnValidate-serialized members
        // ====================================================================

        [SerializeField, MelcherReadOnlyInInspector]
        protected float m_AnchoredSize;


#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        protected override void OnValidate()
        {
            base.OnValidate();

            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // Ensure references to docked safe areas are consistent
            if (m_LeftDock && m_LeftDock.m_DockMode != DockMode.Left)
            {
                m_LeftDock = null;
            }
            if (m_RightDock && m_RightDock.m_DockMode != DockMode.Right)
            {
                m_RightDock = null;
            }
            if (m_TopDock && m_TopDock.m_DockMode != DockMode.Top)
            {
                m_TopDock = null;
            }
            if (m_BottomDock && m_BottomDock.m_DockMode != DockMode.Bottom)
            {
                m_BottomDock = null;
            }

            Vector2 rectSize = m_RectTransform.rect.size;
            switch (m_DockMode)
            {
                default:
                    MelcherLog.LogError(
                        $"Unknown {nameof(DockMode)} '{m_DockMode}'",
                        context: this
                    );
                    break;

                case DockMode.Left:
                case DockMode.Right:
                    // Cache the current width of the RectTransform
                    m_AnchoredSize = rectSize.x;
                    break;

                case DockMode.Top:
                case DockMode.Bottom:
                    // Cache the current height of the RectTransform
                    m_AnchoredSize = rectSize.y;
                    break;

                case DockMode.Center:
                    // Resize to fit between docked neighbours
                    UnityEditor.EditorApplication.delayCall += DoLayout;
                    break;
            }
        }

        public const string CenterDockPaddingMemberName = nameof(m_CenterDockPadding);
        public const string LeftDockMemberName      = nameof(m_LeftDock);
        public const string RightDockMemberName     = nameof(m_RightDock);
        public const string TopDockMemberName       = nameof(m_TopDock);
        public const string BottomDockMemberName    = nameof(m_BottomDock);
        public const string AnchoredSizeMemberName  = nameof(m_AnchoredSize);
        public const string LayoutOrderMemberName   = nameof(m_LayoutOrder);

#endif  // if UNITY_EDITOR

        // =====================================================================
        // Nested types
        // =====================================================================

        public enum DockMode
        {
            Left,
            Top,
            Right,
            Bottom,
            Center
        }   // enum DockMode

    }   // class MelcherSafeAreaConformerDock

}   // namespace Melcher
