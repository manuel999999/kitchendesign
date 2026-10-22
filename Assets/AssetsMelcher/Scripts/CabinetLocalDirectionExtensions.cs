namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

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
                    // Intentionally Vector3.back. The front of the cabinet
                    // points towards -Z
                    return Vector3.back;

                case CabinetLocalDirection.Back:
                    // Intentional Vector3.forward.
                    return Vector3.forward;
            }
        }

        /// <summary>
        /// Convert the direction to an axis; i.e., a direction without a + or
        /// -.
        /// </summary>
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

        /// <summary>
        /// Get the reverse direction.
        /// </summary>
        public static CabinetLocalDirection Reverse(this CabinetLocalDirection direction)
        {
            // We could do something clever, like add 1 if the number is even
            // or subtract 1 otherwise, but that adds a extra dependency on the
            // implementation of CabinetLocalDirection. It would be prone to
            // break if the enum was ever reordered or expanded.

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

    }   // class CabinetLocalDirectionExtensions

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-07
/// ===========================================================================
