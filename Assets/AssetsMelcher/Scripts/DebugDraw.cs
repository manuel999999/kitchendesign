namespace Melcher
{
    using UnityEngine;

    public static class DebugDraw
    {
        public static void Cross(Vector3 center, Color color, float scale = 0.1f, float duration = 0.0f)
        {
            float halfScale = scale * 0.5f;

            Vector3 left = center + (Vector3.left * halfScale);
            Vector3 bottom = center + (Vector3.down * halfScale);
            Vector3 back = center + (Vector3.back * halfScale);

            //Debug.DrawLine()
        }
    }   // class DebugDraw

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-06-20
/// ===========================================================================
