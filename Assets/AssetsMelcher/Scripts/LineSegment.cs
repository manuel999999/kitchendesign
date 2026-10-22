namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    ///     Used by <see cref="KitchenScreenshotManager"/> to cache the start
    ///     and end positions of measurement lines.
    /// </summary>
    public struct LineSegment
    {
        // ====================================================================
        // Constructors
        // ====================================================================

        public LineSegment(Vector3 begin, Vector3 end)
        {
            this.begin = begin;
            this.end = end;
        }

        // ====================================================================
        // Properties
        // ====================================================================

        public float distance => Vector3.Distance(end, begin);

        // ====================================================================
        // Fields
        // ====================================================================

        public Vector3 begin;
        public Vector3 end;

        // ====================================================================
        // Methods
        // ====================================================================

        public void RotateAroundPivot(Vector3 pivot, Quaternion rotation)
        {

            begin = VectorUtility.RotateAroundPivot(begin, pivot, rotation);
            end = VectorUtility.RotateAroundPivot(end, pivot, rotation);
        }

        public void Transform(Transform transform)
        {
            begin = transform.TransformPoint(begin);
            end = transform.TransformPoint(end);
        }

        public void InverseTransform(Transform transform)
        {
            begin = transform.InverseTransformPoint(begin);
            end = transform.InverseTransformPoint(end);
        }

    }   // class LineSegment

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-24
/// ===========================================================================
