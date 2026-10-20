namespace Melcher
{
    using UnityEngine;

    public static class ProFitUnityExtensions
    {
        public static void SetLocalPositionAndRotation(this Transform target, Vector3 localPosition, Quaternion localRotation)
        {
            target.localPosition = localPosition;
            target.localRotation = localRotation;
        }
    }   // class ProFitUnityExtensions

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-05-02
/// ===========================================================================
