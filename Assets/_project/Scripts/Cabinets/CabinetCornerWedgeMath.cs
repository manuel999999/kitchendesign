namespace KitchenDesign.Cabinets
{
    using UnityEngine;

    public static class CabinetCornerWedgeMath
    {
        public static float CalculateOrthogonalLength(Cabinet cornerWedge)
        {
            return Mathf.Sqrt(cornerWedge.sizeInMeters.x * cornerWedge.sizeInMeters.x / 2.0f);
        }
    }
}
