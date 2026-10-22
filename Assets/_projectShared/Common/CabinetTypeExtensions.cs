using KitchenDesign.Shared.Enums;

namespace KitchenDesign.Shared.Common
{
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

        public static bool IsEnd(this CabinetType cabinetType, bool includeTransitions = false)
        {
            bool isTransition = includeTransitions && cabinetType.IsHeightTransition();

            return isTransition || cabinetType == CabinetType.EndLeft || cabinetType == CabinetType.EndRight;
        }

        public static bool IsHeightTransition(this CabinetType cabinetType)
        {
            return
                cabinetType == CabinetType.HighLeftToLowRight ||
                cabinetType == CabinetType.LowLeftToHighRight;
        }

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

        public static bool IsMidSpanCabinetType(this CabinetType cabinetType, KitchenBuildingMethod buildingMethod)
        {
            return cabinetType == CabinetType.Straight ||
                cabinetType.IsBlindCorner(buildingMethod);
        }
    }
}
