namespace KitchenDesign.Cabinets
{
    using Melcher;
    using UnityEngine;
    using KitchenDesign.Geometry;
    using KitchenDesign.Scriptables;

    /// <summary>
    ///     Represents an instantiated instance of a cabinet leg.
    /// </summary>
    public class CabinetSectionLeg
    {
        // ====================================================================
        // Static methods
        // ====================================================================

        public static CabinetSectionLeg InstantiateLeg(
            CabinetSectionLegStyleSO  legStyle,
            ICabinet                cabinet,
            Vector3                 normalizedCabinetAnchor,
            Vector3                 offsetFromCabinetAnchorInMeters)
        {
            Transform legInstance =
                GameObject.Instantiate(
                    legStyle.legModelPrefab,
                    parent: cabinet.GetTransform()
                );

            return
                new CabinetSectionLeg()
                {
                    legStyle = legStyle,
                    legInstance = legInstance,
                    cabinet = cabinet,
                    normalizedCabinetAnchor = normalizedCabinetAnchor,
                    offsetFromCabinetAnchorInMeters = offsetFromCabinetAnchorInMeters
                };
        }

        public void RefreshPosition()
        {
            Vector3 localSpaceLegPosition =
                Cuboid.NormalizedToPoint(cabinet.resizerCuboid, normalizedCabinetAnchor);

            localSpaceLegPosition += offsetFromCabinetAnchorInMeters;

            legInstance.localPosition = localSpaceLegPosition;
        }

        // ====================================================================
        // Properties
        // ====================================================================

        public CabinetSectionLegStyleSO legStyle { get; protected set; }

        public Transform legInstance { get; protected set; }

        public ICabinet cabinet { get; protected set; }

        public Vector3 normalizedCabinetAnchor { get; protected set; }

        public Vector3 offsetFromCabinetAnchorInMeters { get; protected set; }

    }   // class CabinetSectionLeg

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-09-25
/// ===========================================================================
