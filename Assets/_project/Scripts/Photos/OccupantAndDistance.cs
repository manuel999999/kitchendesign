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

    public struct OccupantAndDistance
    {
        public Occupant occupant;
        public float distanceInMeters;

    }
}
