namespace KitchenDesign.Geometry
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using UnityEngine;

    public static class CabinetLocalDirectionUtil
    {
        public static CabinetLocalDirection FromVector3(Vector3 vec)
        {
            Vector3 absVec = MelcherCommon.Abs(vec);

            if (absVec.x >= absVec.y && absVec.x >= absVec.z)
            {
                return
                    vec.x > 0.0f ?
                        CabinetLocalDirection.Right :
                        CabinetLocalDirection.Left;
            }
            else if (absVec.y >= absVec.x && absVec.y >= absVec.z)
            {
                return
                    vec.y > 0.0f ?
                        CabinetLocalDirection.Up :
                        CabinetLocalDirection.Down;
            }
            else
            {
                return
                    vec.z > 0.0f ?
                        CabinetLocalDirection.Back :
                        CabinetLocalDirection.Front;
            }
        }

    }   // class CabinetLocalDirectionUtil

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-03-26
/// ===========================================================================
