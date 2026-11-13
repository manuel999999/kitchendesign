namespace KitchenDesign.Utility
{
    using Melcher;
    using UnityEngine;

    public static class UnityExtensions
    {
        public static void SetLocalPositionAndRotation(this Transform target, Vector3 localPosition, Quaternion localRotation)
        {
            target.localPosition = localPosition;
            target.localRotation = localRotation;
        }
    }   // class UnityExtensions

}   // namespace KitchenDesign.Utility

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-05-02
/// ===========================================================================
