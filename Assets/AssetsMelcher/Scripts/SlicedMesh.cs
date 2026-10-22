namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    ///     Implements 27-slice scaling of a cuboid mesh.
    /// </summary>
    public class SlicedMesh : MelcherMonoBehaviour, ISerializationCallbackReceiver
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public IReadOnlyList<Slice> slices => m_Slices;
        public Cuboid cuboid => m_Cuboid;

        public CuboidPadding skinScale
        {
            get => m_SkinScale;
            set
            {
                m_SkinScale = value;
                UpdateSlices(cuboid);
            }
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public void UpdateSlices(Cuboid cuboid)
        {
            m_Cuboid = cuboid;

            if (!m_AllowUpdateInPrefabs && ProFitUtility.IsPrefab(gameObject))
            {
                // Only modify the visibility prefab instances. This
                // prevents prefab assets being modified and dirtying git
                // changes.

                return;
            }

            foreach (Slice slice in m_Slices)
            {
                Vector3 sizeInMeters =
                    slice.sizeInInches * Const.Inches2Meters;

                Vector3 localPosition =
                    Cuboid.NormalizedToPoint(cuboid, slice.normalizedAnchor);

                const float Tolerance = 0.0001f;

                if (!MelcherCommon.IsAlmostEqual(sizeInMeters, Vector3.zero, Tolerance))
                {
                    // Quite a hack. This is to accommodate sliced meshes that
                    // are not 1m cubes. E.g., some authored countertop models
                    // for Napoleon and Challenger.
                    localPosition.x = slice.normalizedAnchor.x;
                    localPosition.z = slice.normalizedAnchor.z;
                }

                if (MelcherCommon.IsAlmostEqual(sizeInMeters.x, 0.0f, Tolerance))
                {
                    sizeInMeters.x = 1.0f;
                }

                if (MelcherCommon.IsAlmostEqual(sizeInMeters.y, 0.0f, Tolerance))
                {
                    sizeInMeters.y = 1.0f;
                }

                if (MelcherCommon.IsAlmostEqual(sizeInMeters.z, 0.0f, Tolerance))
                {
                    sizeInMeters.z = 1.0f;
                }

                Vector3 localScale =
                    MelcherCommon.ComponentWiseDivide(cuboid.size, sizeInMeters);

                if (slice.scalingX == AxisScaling.PreserveOriginalScale)
                {
                    localScale.x = 1.0f;
                }

                if (slice.scalingY == AxisScaling.PreserveOriginalScale)
                {
                    localScale.y = 1.0f;
                }

                if (slice.scalingZ == AxisScaling.PreserveOriginalScale)
                {
                    localScale.z = 1.0f;
                }

                if (slice.scalingX == AxisScaling.ScaleToSkin)
                {
                    float skinScaleX =
                        slice.normalizedAnchor.x < 0.5f ?
                            m_SkinScale.left :
                            m_SkinScale.right;

                    localScale.x =
                        ApplySkinScale(localScale.x, skinScaleX, m_SkinScaleMode.x);
                }

                if (slice.scalingY == AxisScaling.ScaleToSkin)
                {
                    float skinScaleY =
                        slice.normalizedAnchor.y < 0.5f ?
                            m_SkinScale.bottom :
                            m_SkinScale.top;

                    localScale.y =
                        ApplySkinScale(localScale.y, skinScaleY, m_SkinScaleMode.y);
                }

                if (slice.scalingZ == AxisScaling.ScaleToSkin)
                {
                    float skinScaleZ =
                        slice.normalizedAnchor.z < 0.5f ?
                            m_SkinScale.towardMinZ :
                            m_SkinScale.towardMaxZ;

                    localScale.z =
                        ApplySkinScale(localScale.z, skinScaleZ, m_SkinScaleMode.z);
                }

                // Determine whether the mesh slice is visible if enough of its
                // axises have non-zero scale. This was added to handle end
                // cabinets: In Pro-Fit, end cabinets have zero width and are
                // only rendered for their countertop; In Stoll, end cabinets
                // have width and must render fully. But importantly, the
                // number of axises that must be non-zero depends on whether
                // the sliced mesh is allowed to be flat.

                int axisesWithNonZeroScale = 0;

                if (!IsAlmostZero(localScale.x))
                {
                    ++axisesWithNonZeroScale;
                }

                if (!IsAlmostZero(localScale.y))
                {
                    ++axisesWithNonZeroScale;
                }

                if (!IsAlmostZero(localScale.z))
                {
                    ++axisesWithNonZeroScale;
                }

                // If we're allowing flat slices to render (e.g.,
                // the countertop of a backsplash), then only 2 axises
                // need non-zero scaling. Otherwise, all 3 axises must
                // be non-zero.
                bool isSliceVisible =
                    axisesWithNonZeroScale >= (m_ShowFlatSlices ? 2 : 3);

                foreach (MeshRenderer mesh in slice.meshRenderers)
                {
                    if (!mesh)
                    {
                        continue;
                    }

                    Transform t = mesh.transform;
                    t.localPosition = localPosition;
                    t.localScale = localScale;

                    mesh.gameObject.SetActive(isSliceVisible);
                }
            }

            static bool IsAlmostZero(float value)
            {
                const float Threshold = 0.001f;

                return MelcherCommon.IsAlmostEqual(value, 0.0f, Threshold);
            }

            static float ApplySkinScale(float currentValue, float scale, SkinScaleMode skinScaleMode)
            {
                return
                    skinScaleMode switch
                    {
                        SkinScaleMode.Absolute => scale,
                        SkinScaleMode.Relative => currentValue * scale,
                        _ => throw new System.ArgumentException($"Unhandled {nameof(SkinScaleMode)} value: {skinScaleMode}", nameof(skinScaleMode))
                    };
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField]
        protected CuboidPadding m_SkinScale = CuboidPadding.one;

        [SerializeField]
        // TODO: Tried to use MelcherStableEnum here but it doesn't work!
        //       The stable enum's property drawer throws an exception when
        //       trying to access the X value of the Vector3T. Tried using
        //       serializedProperty.boxedValue but this also errored. -Ryan
        protected Vector3T<SkinScaleMode> m_SkinScaleMode = new (SkinScaleMode.Absolute);

        [SerializeField]
        protected Vector3 m_SizeInInches = Vector3.one * Const.Meters2Inches;

        [Tooltip("When enabled, mesh slices that are flat (i.e., one of " +
            "their three axises is scaled to zero) are still rendered. " +
            "Otherwise, any slice that has an axis whose scale is zero will " +
            "not render at all.")]
        [SerializeField]
        protected bool m_ShowFlatSlices = false;

        [SerializeField]
        protected List<Slice> m_Slices = null;

        [SerializeField]
        protected bool m_AllowUpdateInPrefabs = default;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        private Cuboid m_Cuboid;

        // =====================================================================
        // Nested types
        // =====================================================================

        [System.Serializable]
        public struct Slice
        {
            public string name => m_Name;

            /// <summary>
            /// For the Inspector
            /// </summary>
            [SerializeField] private string m_Name;

            public Vector3 normalizedAnchor;

            [Tooltip("The length of mesh. Used to properly scale meshes " +
                "that are not a uniform 1 meter box. E.g., some cabinet " +
                "countertop meshes have a specific size. When any one of " +
                "these axises is 0, it is interpreted as 1 meter long.")]
            public Vector3 sizeInInches;

            // These values have been superseded by the `AxisScaling` values
            // but will temporarily remain in code to help the transition.
            public bool scaleAlongX;
            public bool scaleAlongY;
            public bool scaleAlongZ;

            public AxisScaling scalingX;
            public AxisScaling scalingY;
            public AxisScaling scalingZ;

            public List<MeshRenderer> meshRenderers;
        }

        public enum SkinScaleMode
        {
            /// <summary>
            ///     The skin scale is used as the absolute scale.
            /// </summary>
            Absolute,

            /// <summary>
            ///     The skin scale is multiplied by the current cuboid size.
            /// </summary>
            Relative,
        }

        public enum AxisScaling
        {
            /// <summary>
            ///     This axis's scale will be kept at 1.0. This is useful for
            ///     some 45-degree corner cabinets whose inner corner would
            ///     otherwise be skewed/distorted if trying to match the
            ///     sliced mesh's scale or skin size.
            /// </summary>
            PreserveOriginalScale,
            ScaleToSize,
            ScaleToSkin,
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += LateValidate;
#endif  // if UNITY_EDITOR
        }

#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        /// <summary>
        ///     Migrate the data from <see cref="Slice.scaleAlongX"/>-etc. to
        ///     <see cref="Slice.scalingX"/>-etc.
        /// </summary>
        protected void LateValidate()
        {
            for (int i = 0; i < m_Slices.Count; ++i)
            {
                Slice slice = m_Slices[i];

                if (slice.scalingX != AxisScaling.PreserveOriginalScale ||
                    slice.scalingY != AxisScaling.PreserveOriginalScale ||
                    slice.scalingZ != AxisScaling.PreserveOriginalScale)
                {
                    // Already treated. We won't try to migrate the old scaling
                    // values.
                    continue;
                }

                slice.scalingX = slice.scaleAlongX ? AxisScaling.ScaleToSize : AxisScaling.ScaleToSkin;
                slice.scalingY = slice.scaleAlongY ? AxisScaling.ScaleToSize : AxisScaling.ScaleToSkin;
                slice.scalingZ = slice.scaleAlongZ ? AxisScaling.ScaleToSize : AxisScaling.ScaleToSkin;

                m_Slices[i] = slice;

                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

#endif  // if UNITY_EDITOR

    }   // class SlicedMesh

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-07
/// ===========================================================================
