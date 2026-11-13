namespace KitchenDesign.Serializables
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

    [System.Serializable]
    public class ScreenshotGallery
    {
        public byte[] cover;

        public byte[] topDown;

        public byte[] topDownPlan;

        public List<byte[]> front = new List<byte[]>();

        public List<byte[]> frontPlan = new List<byte[]>();

    }
}
