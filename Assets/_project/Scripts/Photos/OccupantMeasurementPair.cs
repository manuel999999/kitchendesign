namespace KitchenDesign.Photos
{
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

    public struct OccupantMeasurementPair
    {
        public OccupantMeasurementPair(Occupant a, Occupant b, float distanceInMeters, float extraDistanceInMetersForLabelOnly, CabinetLocalDirection directionAToB, ScreenshotAngle screenshotAngle)
        {
            occupantA = a;
            occupantB = b;
            this.distanceInMeters = distanceInMeters;
            this.extraDistanceInMetersForLabelOnly = extraDistanceInMetersForLabelOnly;
            this.directionAtoB = directionAToB;
            this.screenshotAngle = screenshotAngle;
        }

        public Occupant occupantA;
        public Occupant occupantB;
        public float distanceInMeters;
        public float extraDistanceInMetersForLabelOnly;
        public CabinetLocalDirection directionAtoB;
        public ScreenshotAngle screenshotAngle;
    }
}
