namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// Base class for safe-area conformers. Safe-area conformers wrap a
    /// RectTransform and resize it to be contained within the screen's safe
    /// area.
    /// </summary>
    public abstract class MelcherSafeAreaConformer : MelcherMonoBehaviour
    {
        public delegate void SafeAreaConformerHandler(MelcherSafeAreaConformer caller);
        public static event SafeAreaConformerHandler onEnable;
        public static event SafeAreaConformerHandler onDisable;
        public static event SafeAreaConformerHandler onParentChanged;

        // =====================================================================
        // Static functions
        // =====================================================================

        /// <summary>
        /// Return the safe area for the screen. <paramref name="minPadding"/>
        /// defines the minimum padding from each of the screen's edge to apply
        /// to the returned safe area.
        /// </summary>
        public static Rect GetSafeAreaWithMinimums(RectOffset minPadding)
        {
            return GetSafeAreaWithMinimums(
                minLeft:   minPadding.left,
                minRight:  minPadding.right,
                minTop:    minPadding.top,
                minBottom: minPadding.bottom
            );
        }

        public static Rect GetSafeAreaWithMinimums(int minLeft, int minRight, int minTop, int minBottom)
        {
            // Start with the safe area reported by Unity. This is in screen
            // space
            Rect safeArea = Screen.safeArea;

            // Calculate the whole screen resolution
            Vector2 screenResolution = new Vector2(Screen.width, Screen.height);

            if (safeArea.xMin < minLeft)
            {
                // Ensure minimum safe area on left
                safeArea.xMin = minLeft;
            }

            if (safeArea.yMin < minBottom)
            {
                // Ensure minimum safe area on bottom
                safeArea.yMin = minBottom;
            }

            if (screenResolution.x - safeArea.xMax < minRight)
            {
                // Ensure minimum safe area on right
                safeArea.xMax = safeArea.xMax - minRight;
            }

            if (screenResolution.y - safeArea.yMax < minTop)
            {
                // Ensure minimum safe area on top
                safeArea.yMax = safeArea.yMax - minTop;
            }

            return safeArea;
        }

        protected static int CalculateLayoutOrder(RectTransform caller)
        {
            int order = 1;
            for (Transform t = caller.parent; t; t = t.parent)
            {
                if (t.TryGetComponent(out MelcherSafeAreaConformer conformer))
                {
                    order += conformer.GetLayoutOrder();
                    break;
                }

                ++order;
            }

            return order;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public RectTransform GetRectTransform()
        {
            return m_RectTransform;
        }

        public int GetLayoutOrder()
        {
            return DoGetLayoutOrder();
        }

        public void ForceLayout()
        {
            DoLayout();
        }

        public virtual Rect CalculateScaledSafeArea(out float canvasScale)
        {
            canvasScale = m_RectTransform.GetCanvasScaleFactor();

            RectOffset minPadding =
                MelcherUnityUtility.isTargetMobilePlatform ? m_MinPaddingMobile : m_MinPaddingStandalone;

            Vector4 scaledMinPadding = minPadding.ToVector4() * canvasScale;

            // TODO: Is it safe to always apply this scaling? Do we want to
            //       assume that the minPadding is defined in the proper canvas
            //       scaling space (e.g., constant physical size)

            Rect safeArea =
                GetSafeAreaWithMinimums(
                    (int)scaledMinPadding.x,
                    (int)scaledMinPadding.y,
                    (int)scaledMinPadding.z,
                    (int)scaledMinPadding.w
                );

            safeArea.position /= canvasScale;
            safeArea.size /= canvasScale;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Ensure any safe area is ignored during edit mode. This
                // prevents UI being dirtied just from resizing the game view
                safeArea.Set(x: 0.0f, y: 0.0f, Screen.width / canvasScale, Screen.height / canvasScale);
            }
#endif  // if UNITY_EDITOR

            return safeArea;
        }

        protected virtual void Awake()
        {
            if (!Application.isMobilePlatform)
            {
                this.enabled = false;
            }
        }
        
        protected virtual void OnEnable()
        {
            onEnable?.Invoke(this);
        }

        protected virtual void OnDisable()
        {
            onDisable?.Invoke(this);
        }

        protected virtual void OnTransformParentChanged()
        {
            int order = CalculateLayoutOrder(m_RectTransform);

            if (order != m_LayoutOrder)
            {
                m_LayoutOrder = order;
                onParentChanged?.Invoke(this);
            }
        }

        protected virtual int DoGetLayoutOrder()
        {
            return m_LayoutOrder;
        }

        protected abstract void DoLayout();

        // =====================================================================
        // Inspector-serialized members
        // =====================================================================

        [SerializeField]
        [Tooltip("The minimum padding to ensure exists around the screen " +
            "edge for mobile platforms. If the device's reported safe area " +
            "has wider margins than this, this is ignored.")]
        protected MelcherVariable<RectOffset, MelcherRectOffsetAppValue> m_MinPaddingMobile = default;

        [SerializeField]
        [Tooltip("The minimum padding to ensure exists around the screen " +
            "edge for standalone platforms. If the reported safe area has " +
            "wider margins than this, this is ignored.")]
        protected MelcherVariable<RectOffset, MelcherRectOffsetAppValue> m_MinPaddingStandalone = default;

        // =====================================================================
        // OnValidate-serialized members
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected RectTransform m_RectTransform = null;

        [SerializeField, MelcherReadOnlyInInspector]
        protected int m_LayoutOrder;

#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        protected override void OnValidate()
        {
            base.OnValidate();

            m_LayoutOrder = CalculateLayoutOrder(m_RectTransform);
        }

#endif  // if UNITY_EDITOR

    }   // class MelcherSafeAreaConformer

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2021-12-15
/// ===========================================================================
