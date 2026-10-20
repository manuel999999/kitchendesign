namespace Melcher
{
    /// <summary>
    ///     Utility functions for quickly checking if an enum is one of many
    ///     values. E.g., checking if a cabinet is an end.
    /// </summary>
    public static class CabinetTypeExtensions
    {
        public static CabinetType Reverse(this CabinetType cabinetType)
        {
            return
                cabinetType switch
                {
                    CabinetType.EndLeft => CabinetType.EndRight,
                    CabinetType.EndRight => CabinetType.EndLeft,

                    CabinetType.StraightOnLeftOfCorner => CabinetType.StraightOnRightOfCorner,
                    CabinetType.StraightOnRightOfCorner => CabinetType.StraightOnLeftOfCorner,

                    _ => cabinetType
                };
        }

        /// <summary>
        ///     Return <see langword="true"/> if the cabinet section is an end.
        ///     Optionally, the function can include height transitions as ends
        ///     because they are visually similar to ends.
        /// </summary>
        public static bool IsEnd(this CabinetType cabinetType, bool includeTransitions = false)
        {
            bool isTransition = includeTransitions && cabinetType.IsHeightTransition();

            return isTransition || cabinetType == CabinetType.EndLeft || cabinetType == CabinetType.EndRight;
        }

        /// <summary>
        ///     Return <see langword="true"/> if the cabinet section represents
        ///     a height change from a shorter cabinet to a taller one (or vice
        ///     versa). Otherwise, return <see langword="false"/>.
        /// </summary>
        public static bool IsHeightTransition(this CabinetType cabinetType)
        {
            return
                cabinetType == CabinetType.HighLeftToLowRight ||
                cabinetType == CabinetType.LowLeftToHighRight;
        }

        // TODO: Warning: The value of this method is only valid if the kitchen
        //                is in catalog method. In Sections method, the same
        //                enum values are used for the mullions that appear
        //                on either side of a corner.
        //
        //                We should really have separate enum names for blind
        //                corners and mullions. -Ryan
        public static bool IsBlindCorner(this CabinetType cabinetType, KitchenBuildingMethod buildingMethod)
        {
            if (buildingMethod != KitchenBuildingMethod.Catalog)
            {
                return false;
            }

            return
                cabinetType == CabinetType.StraightOnLeftOfCorner ||
                cabinetType == CabinetType.StraightOnRightOfCorner;
        }

        /// <summary>
        ///     Return <see langword="true"/> if the cabinet type is one that
        ///     appears in the middle of a cabinet span. I.e., one that counts
        ///     as having at least one cabinet in the span.
        /// </summary>
        /// <param name="cabinetType"></param>
        /// <returns></returns>
        public static bool IsMidSpanCabinetType(this CabinetType cabinetType, KitchenBuildingMethod buildingMethod)
        {
            return cabinetType == CabinetType.Straight ||
                cabinetType.IsBlindCorner(buildingMethod);
        }


    }   // class CabinetTypeExtensions

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-10-03
/// ===========================================================================
