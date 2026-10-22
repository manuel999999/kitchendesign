namespace KitchenDesign
{
    using Melcher;
    using System;
    using UnityEngine;

    /// <summary>
    ///     Renders a cuboid around an occupant to indicate the space it
    ///     occupies in its attached volume (e.g., a cabinet or cabinet shelf).
    ///     Meant for debugging purposes. Its visibility can be toggled at
    ///     runtime using a key-combination. Currently it is 1 + Backslash.
    /// </summary>
    public class OccupantOccupiedCuboidVisualization : MelcherMonoBehaviour
    {
        // =====================================================================
        // Static properties
        // =====================================================================

        /// <summary>
        /// Static visibility flag. When this is on, all product visualizations
        /// are visible.
        /// </summary>
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

        // =====================================================================
        // Internal events
        // =====================================================================

        protected static event Action onVisibilityChanged;

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetOccupant(Occupant product)
        {
            m_VisualizedOccupant = product;
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
            if (!m_Renderer.enabled || !m_VisualizedOccupant || m_VisualizedOccupant.cabinet == null)
            {
                return;
            }

            // Synchronize the rendered box to the occupant. Use the occupiable
            // volume's CalculateCabinetSpaceCuboidForProduct to ensure 1:1
            // visualization of the occupant's bounds.

            OccupiableVolume occupiableVolume = m_VisualizedOccupant.occupiedVolume;

            Cuboid volumeLocalCuboid =
                occupiableVolume.CalculateOccupancyCuboidForOccupant(m_VisualizedOccupant);

            Vector3 worldPosition =
                occupiableVolume.GetTransform().TransformPoint(volumeLocalCuboid.position);

            Vector3 productLocalPosition =
                m_VisualizedOccupant.productVisualContentInstance.GetTransform().InverseTransformPoint(worldPosition);

            m_TransformHelper.offsetInMeters = productLocalPosition;
            m_TransformHelper.sizeInMeters = volumeLocalCuboid.size;
        }

        protected void RefreshVisibility()
        {
            m_Renderer.enabled = s_IsVisible;
        }

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        private Renderer m_Renderer = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        private TransformHelper m_TransformHelper = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        /// <summary>
        ///     The cabinet occupant that this visual represents.
        /// </summary>
        protected Occupant m_VisualizedOccupant;

        // =====================================================================
        // Static fields
        // =====================================================================

        protected static bool s_IsVisible;

    }   // class OccupantOccupiedCuboidVisualization

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-06
/// ===========================================================================
