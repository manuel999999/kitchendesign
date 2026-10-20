namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     Renders a cuboid within an <see cref="OccupiableVolume"/> to
    ///     visualize the region where products can be attached. Meant for
    ///     debugging purposes.
    /// </summary>
    public class OccupiableVolumeCuboidVisualization : MelcherMonoBehaviour
    {
        // ====================================================================
        // Static properties
        // ====================================================================

        public static bool isVisible
        {
            get => s_IsVisible;
            set
            {
                if (value != s_IsVisible)
                {
                    s_IsVisible = value;
                    onVisibilityChanged?.Invoke();
                }
            }
        }

        // ====================================================================
        // Events
        // ====================================================================

        public static event System.Action onVisibilityChanged;

        // ====================================================================
        // Methods
        // ====================================================================

        public void SetOccupiableVolume(OccupiableVolume volume)
        {
            // Switch the renderer off in case the volume is null. It will be
            // switched back on in RefreshVisibility if it should be visible.
            m_Renderer.enabled = false;
            m_VisualizedVolume = volume;
            RefreshVisibility();
        }

        protected void Awake()
        {
            RefreshVisibility();
        }

        protected void OnEnable()
        {
            onVisibilityChanged += RefreshVisibility;
        }

        protected void OnDisable()
        {
            onVisibilityChanged -= RefreshVisibility;
        }

        protected void Update()
        {
            if (!m_Renderer.enabled || !m_VisualizedVolume)
            {
                return;
            }

            Cuboid occupiableCuboid = m_VisualizedVolume.occupiableCuboid;

            m_TransformHelper.offsetInMeters = occupiableCuboid.position;
            m_TransformHelper.sizeInMeters = occupiableCuboid.size;
        }

        /// <summary>
        ///     Set the visibility of the cuboid renderer depending on the
        ///     value of <see cref="isVisible"/> and some volume-specific rules
        ///     to avoid drawing a cuboid for volumes that are not occupiable.
        /// </summary>
        protected void RefreshVisibility()
        {
            bool isVisibleUsingRules = s_IsVisible;

            if (m_VisualizedVolume is Cabinet cabinet)
            {
                if (!cabinet.canHaveProducts)
                {
                    // Force the visualization mesh to hide for cabinets that
                    // disallow products to be added to them.
                    isVisibleUsingRules = false;
                }
            }

            m_Renderer.enabled = isVisibleUsingRules;
        }

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Tooltip("The volume that this visualization represents.")]
        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Up)]
        protected OccupiableVolume m_VisualizedVolume = default;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        private Renderer m_Renderer = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        private TransformHelper m_TransformHelper = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        // ====================================================================
        // Static fields
        // ====================================================================

        protected static bool s_IsVisible;

    }   // class OccupiableVolumeCuboidVisualization

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-06-21
/// ===========================================================================
