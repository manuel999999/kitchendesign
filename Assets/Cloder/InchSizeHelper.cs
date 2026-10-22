namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    /// Editor helper for sizing a box in inches. Either type a size and apply
    /// it to the box, or drag the start and end markers and fill the space
    /// between them, which writes the measured size back in inches.
    /// Sizes run along this object's own axes.
    /// </summary>
    public class InchSizeHelper : MonoBehaviour
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public Vector3 sizeInInches
        {
            get => m_SizeInInches;
            set => m_SizeInInches = value;
        }

        public Transform start => m_Start;

        public Transform end => m_End;

        public Transform hook => m_Hook;

        public Vector3 hookPositionInInches
        {
            get => m_HookPositionInInches;
            set => m_HookPositionInInches = value;
        }

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        /// Moves the box so the hook lands on <see cref="hookPositionInInches"/>,
        /// a world position. The box and its markers keep their offset from the
        /// hook, so the hook works as the handle the box is placed by.
        /// </summary>
        public void MoveHookToPosition()
        {
            if (!m_Hook)
            {
                Debug.LogWarning("Assign the hook first.", this);
                return;
            }

            transform.position +=
                m_HookPositionInInches * Const.Inches2Meters - m_Hook.position;
        }

        /// <summary>
        /// Resizes the box to <see cref="sizeInInches"/>. The corner the start
        /// marker is on stays where it is and the box grows away from it. Both
        /// markers end up on the corners they were nearest to.
        /// </summary>
        public void ApplySize()
        {
            if (!m_Start || !m_End)
            {
                Debug.LogWarning("Assign the start and end markers first.", this);
                return;
            }

            Quaternion rotation = transform.rotation;
            Vector3 center = transform.TransformPoint(meshBounds.center);
            Vector3 currentSize = Vector3.Scale(Abs(transform.lossyScale), meshBounds.size);
            Vector3 newSize = Abs(m_SizeInInches) * Const.Inches2Meters;

            Vector3 startSides = SidesOf(m_Start, center, rotation, sideWhenCentered: -1.0f);
            Vector3 endSides = SidesOf(m_End, center, rotation, sideWhenCentered: 1.0f);

            Vector3 anchor = center + rotation * Vector3.Scale(startSides, currentSize * 0.5f);
            Vector3 newCenter = anchor - rotation * Vector3.Scale(startSides, newSize * 0.5f);

            SetBox(newCenter, newSize);

            m_Start.position = anchor;
            m_End.position = newCenter + rotation * Vector3.Scale(endSides, newSize * 0.5f);
        }

        /// <summary>
        /// Moves and resizes the box to span the start and end markers, then
        /// writes the measured size into <see cref="sizeInInches"/>. The
        /// markers do not move.
        /// </summary>
        public void FillBetweenStartAndEnd()
        {
            if (!m_Start || !m_End)
            {
                Debug.LogWarning("Assign the start and end markers first.", this);
                return;
            }

            // The markers may be children of this object, so remember where
            // they are before the box changes underneath them.
            Vector3 startPosition = m_Start.position;
            Vector3 endPosition = m_End.position;

            Vector3 size =
                Abs(Quaternion.Inverse(transform.rotation) * (endPosition - startPosition));

            SetBox((startPosition + endPosition) * 0.5f, size);

            m_Start.position = startPosition;
            m_End.position = endPosition;

            m_SizeInInches = RoundToThousandths(size * Const.Meters2Inches);
        }

        protected void Reset()
        {
            m_Start = transform.Find("Start");
            m_End = transform.Find("End");
            m_Hook = transform.Find("Hook");

            m_SizeInInches = RoundToThousandths(
                Vector3.Scale(Abs(transform.lossyScale), meshBounds.size) * Const.Meters2Inches
            );
        }

        /// <summary>
        /// Scales and moves this object so its mesh occupies a box of the given
        /// size, in meters along this object's axes, centered on a world point.
        /// </summary>
        private void SetBox(Vector3 centerInWorld, Vector3 sizeInMeters)
        {
            Bounds bounds = meshBounds;

            Vector3 parentScale =
                transform.parent ? Abs(transform.parent.lossyScale) : Vector3.one;

            Vector3 localScale = transform.localScale;

            for (int axis = 0; axis < 3; ++axis)
            {
                float metersPerUnitOfScale = parentScale[axis] * bounds.size[axis];

                if (metersPerUnitOfScale > Mathf.Epsilon)
                {
                    localScale[axis] = sizeInMeters[axis] / metersPerUnitOfScale;
                }
            }

            transform.localScale = localScale;
            transform.position += centerInWorld - transform.TransformPoint(bounds.center);
        }

        /// <summary>
        /// For each axis of the box, -1 if the marker is on the negative side
        /// of the box's center and +1 if it is on the positive side.
        /// </summary>
        private static Vector3 SidesOf(
            Transform marker,
            Vector3 center,
            Quaternion rotation,
            float sideWhenCentered
        )
        {
            const float Tolerance = 1e-5f;

            Vector3 offset = Quaternion.Inverse(rotation) * (marker.position - center);
            Vector3 sides = default;

            for (int axis = 0; axis < 3; ++axis)
            {
                sides[axis] =
                    Mathf.Abs(offset[axis]) <= Tolerance
                        ? sideWhenCentered
                        : Mathf.Sign(offset[axis]);
            }

            return sides;
        }

        private static Vector3 Abs(Vector3 v)
            => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static Vector3 RoundToThousandths(Vector3 v)
            => new Vector3(
                Mathf.Round(v.x * 1000.0f) / 1000.0f,
                Mathf.Round(v.y * 1000.0f) / 1000.0f,
                Mathf.Round(v.z * 1000.0f) / 1000.0f
            );

        private Bounds meshBounds
        {
            get
            {
                MeshFilter meshFilter = GetComponent<MeshFilter>();

                return meshFilter && meshFilter.sharedMesh
                    ? meshFilter.sharedMesh.bounds
                    : new Bounds(Vector3.zero, Vector3.one);
            }
        }

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [SerializeField]
        private Vector3 m_SizeInInches = Vector3.one * Const.Meters2Inches;

        [SerializeField]
        private Transform m_Start;

        [SerializeField]
        private Transform m_End;

        [SerializeField]
        private Transform m_Hook;

        [SerializeField]
        private Vector3 m_HookPositionInInches;
    }
}
