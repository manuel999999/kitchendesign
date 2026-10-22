namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    ///     A non-axis-aligned bounds. This works by representing the bounds as
    ///     a local-space (axis-aligned) region and storing a world-to-local
    ///     matrix to make calculations relative to this representation.
    ///     <br /><br />
    ///
    ///     This was needed to handle raycasts against 45-degree corner wedges.
    /// </summary>
    public struct OrientedBounds
    {
        // ====================================================================
        // Constructors
        // ====================================================================
        public OrientedBounds(Bounds localBounds, Matrix4x4 worldToLocal)
        {
            this.localBounds = localBounds;
            this.worldToLocal = worldToLocal;
        }

        // ====================================================================
        // Fields
        // ====================================================================

        public Bounds localBounds;

        public Matrix4x4 worldToLocal;

        // ====================================================================
        // Methods
        // ====================================================================

        public bool IntersectRay(Ray worldRay, out float distance)
        {
            distance = float.MaxValue;

            Ray localRay =
                new Ray(
                    worldToLocal.MultiplyPoint3x4(worldRay.origin),
                    worldToLocal.MultiplyVector(worldRay.direction)
                );

            bool didIntersect =
                localBounds.IntersectRay(localRay, out float localDistance);

            if (didIntersect)
            {
                distance = localDistance * worldRay.direction.magnitude;
            }

            return didIntersect;
        }

        public void Draw(Color color, float duration, bool drawX = false)
        {
            Matrix4x4 localToWorld = worldToLocal.inverse;

            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;

            System.Span<Vector3> corners = stackalloc Vector3[8];

            MelcherUnityUtility.CalculateAABBCorners(min, max, corners);

            for (int i = 0; i < corners.Length; ++i)
            {
                corners[i] = localToWorld.MultiplyPoint3x4(corners[i]);
            }

            // Draw lines between the bottom ring of vertices.
            Debug.DrawLine(corners[0], corners[1], color, duration);
            Debug.DrawLine(corners[1], corners[2], color, duration);
            Debug.DrawLine(corners[2], corners[3], color, duration);
            Debug.DrawLine(corners[3], corners[0], color, duration);

            // Draw vertical lines between bottom and top ring of vertices.
            for (int i = 0; i < 4; ++i)
            {
                Debug.DrawLine(corners[i], corners[i + 4], color, duration);
            }

            // Draw lines between the top ring of vertices.
            Debug.DrawLine(corners[4], corners[5], color, duration);
            Debug.DrawLine(corners[5], corners[6], color, duration);
            Debug.DrawLine(corners[6], corners[7], color, duration);
            Debug.DrawLine(corners[7], corners[4], color, duration);

            if (drawX)
            {
                Debug.DrawLine(corners[0], corners[6], color, duration);
                Debug.DrawLine(corners[1], corners[7], color, duration);
                Debug.DrawLine(corners[2], corners[4], color, duration);
                Debug.DrawLine(corners[3], corners[5], color, duration);
            }
        }
    }   // class OrientedBounds

}   // namespace KitchenDesign

