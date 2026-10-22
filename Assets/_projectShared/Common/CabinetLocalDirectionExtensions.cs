using KitchenDesign.Shared.Enums;
using UnityEngine;

namespace KitchenDesign.Shared.Common
{
    public static class CabinetLocalDirectionExtensions
    {
        public static Vector3 ToVector3(this CabinetLocalDirection direction)
        {
            switch (direction)
            {
                default:
                    throw new System.InvalidOperationException($"Unhandled {nameof(CabinetLocalDirection)} value: {direction}");

                case CabinetLocalDirection.Left:
                    return Vector3.left;

                case CabinetLocalDirection.Right:
                    return Vector3.right;

                case CabinetLocalDirection.Up:
                    return Vector3.up;

                case CabinetLocalDirection.Down:
                    return Vector3.down;

                case CabinetLocalDirection.Front:
                    return Vector3.back;

                case CabinetLocalDirection.Back:
                    return Vector3.forward;
            }
        }

        public static MeasurementAxis ToAxis(this CabinetLocalDirection direction)
        {
            switch (direction)
            {
                default:
                    throw new System.InvalidOperationException($"Unhandled {nameof(CabinetLocalDirection)} value: {direction}");

                case CabinetLocalDirection.Left:
                case CabinetLocalDirection.Right:
                    return MeasurementAxis.X;

                case CabinetLocalDirection.Up:
                case CabinetLocalDirection.Down:
                    return MeasurementAxis.Y;

                case CabinetLocalDirection.Front:
                case CabinetLocalDirection.Back:
                    return MeasurementAxis.Z;
            }
        }

        public static CabinetLocalDirection Reverse(this CabinetLocalDirection direction)
        {
            switch (direction)
            {
                default:
                    throw new System.InvalidOperationException($"Unhandled {nameof(CabinetLocalDirection)} value: {direction}");

                case CabinetLocalDirection.Left:
                    return CabinetLocalDirection.Right;

                case CabinetLocalDirection.Right:
                    return CabinetLocalDirection.Left;

                case CabinetLocalDirection.Up:
                    return CabinetLocalDirection.Down;

                case CabinetLocalDirection.Down:
                    return CabinetLocalDirection.Up;

                case CabinetLocalDirection.Front:
                    return CabinetLocalDirection.Back;

                case CabinetLocalDirection.Back:
                    return CabinetLocalDirection.Front;
            }
        }
    }
}
