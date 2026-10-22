namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    /// A component to manage the <see cref="Transform"/>'s size and position
    /// with either metric and imperial values.
    /// </summary>
    public class TransformHelper : MelcherMonoBehaviour
    {
        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        /// Local space offset
        /// </summary>
        public Vector3 offsetInInches
        {
            get => m_OffsetInInches;
            set
            {
                m_OffsetInInches = value;
                ApplyPositionAndScale();
            }
        }

        /// <summary>
        /// Local space offset
        /// </summary>
        public Vector3 offsetInMeters
        {
            get => m_OffsetInInches * Const.Inches2Meters;
            set
            {
                m_OffsetInInches = value * Const.Meters2Inches;
                ApplyPositionAndScale();
            }
        }

        public Vector3 sizeInInches
        {
            get => m_SizeInInches;
            set
            {
                m_SizeInInches = value;
                ApplyPositionAndScale();
            }
        }

        public Vector3 sizeInMeters
        {
            get => m_SizeInInches * Const.Inches2Meters;
            set
            {
                m_SizeInInches = value * Const.Meters2Inches;
                ApplyPositionAndScale();
            }
        }

        // ====================================================================
        // Methods
        // ====================================================================

        public Transform GetTransform()
        {
            return m_Transform;
        }

        public void ApplyPositionAndScale()
        {
            if (m_ApplyOffset)
            {
                m_Transform.localPosition = m_OffsetInInches * Const.Inches2Meters;
            }

            m_Transform.localScale =
                Vector3.Scale(m_SizeInInches * Const.Inches2Meters, m_Scale);
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [UnityEngine.Serialization.FormerlySerializedAs("m_Offset")]
        [SerializeField] protected Vector3 m_OffsetInInches = default;
        [UnityEngine.Serialization.FormerlySerializedAs("m_Size")]
        [SerializeField] protected Vector3 m_SizeInInches = default;
        [Tooltip("Allows a constant scale to be applied to the transform. " +
            "Useful for inverting one or more axis.")]
        [SerializeField] protected Vector3 m_Scale = Vector3.one;

        [Tooltip("When enabled, the transform's local position is changed " +
            "to match the Offset In Inches value. This is generally " +
            "desired. However, sometimes it is convenient to manually " +
            "position the transform (e.g. lining up a known-size cube with " +
            "a model when the cube's offset isn't important).")]
        [SerializeField] protected bool m_ApplyOffset = true;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Transform m_Transform = default;

#if UNITY_EDITOR
        // ====================================================================
        // Editor
        // ====================================================================
        #region Editor

        protected override void OnValidate()
        {
            base.OnValidate();

            ApplyPositionAndScale();
        }

        #endregion  // Editor
#endif  // if UNITY_EDITOR

    }   // class TransformHelper

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-18
/// ===========================================================================
