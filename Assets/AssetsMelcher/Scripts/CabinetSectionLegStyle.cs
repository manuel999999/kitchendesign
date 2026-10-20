namespace Melcher
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "CabinetSectionLegStyle", menuName = "Melcher Studios/Pro-Fit/Cabinet Section Leg Style")]
    public class CabinetSectionLegStyle : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public Transform legModelPrefab => m_LegModelPrefab;

        public Vector2 horizontalInsetFromCabinetEdgesInMeters =>
            m_HorizontalInsetFromCabinetEdgesInInches * Const.Inches2Meters;

        public float heightInMeters => m_HeightInInches * Const.Inches2Meters;

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField, MelcherAssetField]
        protected Transform m_LegModelPrefab;

        [SerializeField]
        protected Vector2 m_HorizontalInsetFromCabinetEdgesInInches;

        [SerializeField]
        [Tooltip("How far the vertical center line of the leg model will be " +
            "placed into cabinet from the cabinets edges.")]
        protected float m_HeightInInches;

    }   // class CabinetSectionLegStyle

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-06-13
/// ===========================================================================
