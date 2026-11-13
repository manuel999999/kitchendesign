namespace KitchenDesign.Photos
{
    using KitchenDesign.Utility;
    using Cysharp.Threading.Tasks;
    using KitchenDesign.Cabinets;
    using KitchenDesign.Geometry;
    using KitchenDesign.KitchenModel;
    using KitchenDesign.Measures;
    using KitchenDesign.Occupancy;
    using KitchenDesign.Products;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using Melcher;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;

    public class OccupantNearestNeighbours
    {
        public OccupantAndDistance GetNearestInDirection(ScreenshotAngle screenshotAngle, CabinetLocalDirection direction)
        {
            switch (screenshotAngle)
            {
                default:
                    throw new System.NotImplementedException($"Unhandled {nameof(ScreenshotAngle)}: {screenshotAngle}");

                case ScreenshotAngle.Top:
                    Debug.Assert(direction.ToAxis() != MeasurementAxis.Y, "For top screenshots, the direction should be along the XZ plane");
                    return topNearestNeighbours[(int)direction];

                case ScreenshotAngle.Front:
                    Debug.Assert(direction.ToAxis() != MeasurementAxis.Z, "For front screenshots, be direction should be along the XY plane");
                    return frontNearestNeighbours[(int)direction];
            }
        }

        public void SetNearestInDirection(ScreenshotAngle screenshotAngle, CabinetLocalDirection direction, OccupantAndDistance occupantAndDistance)
        {
            switch (screenshotAngle)
            {
                default:
                    throw new System.NotImplementedException($"Unhandled {nameof(ScreenshotAngle)}: {screenshotAngle}");

                case ScreenshotAngle.Top:
                    Debug.Assert(direction.ToAxis() != MeasurementAxis.Y, "For top screenshots, the direction should be along the XZ plane");
                    topNearestNeighbours[(int)direction] = occupantAndDistance;
                    break;

                case ScreenshotAngle.Front:
                    Debug.Assert(direction.ToAxis() != MeasurementAxis.Z, "For front screenshots, be direction should be along the XY plane");
                    frontNearestNeighbours[(int)direction] = occupantAndDistance;
                    break;
            }
        }

        public Occupant occupant;

        public OccupantAndDistance[] frontNearestNeighbours = new OccupantAndDistance[EnumInfo<CabinetLocalDirection>.count];
        public OccupantAndDistance[] topNearestNeighbours = new OccupantAndDistance[EnumInfo<CabinetLocalDirection>.count];

    }
}
