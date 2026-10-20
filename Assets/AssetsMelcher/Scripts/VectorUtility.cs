namespace Melcher
{
    using UnityEngine;

    public static class VectorUtility
    {
        public static Vector3 RotateAroundPivot(Vector3 current, Vector3 pivot, Quaternion rotation)
        {
            return rotation * (current - pivot) + pivot;
        }
    }   // class VectorUtility

}   // namespace Melcher

