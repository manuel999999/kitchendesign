namespace KitchenDesign.Cabinets
{
    using Melcher;
    using UnityEngine;
    using KitchenDesign.Geometry;
    using KitchenDesign.KitchenModel;

    /// <summary>
    /// This interface is used by <see cref="Occupant"/> to request information
    /// up the class hierarchy dependency. (Typically, dependencies are fed
    /// down and the nested nodes don't know anything about the objects above
    /// them):
    /// <br /><br />
    ///
    /// - A <see cref="Kitchen"/> has one <see cref="CabinetLayout"/>.<br />
    /// - A <see cref="CabinetLayout"/> has many <see cref="Cabinet"/>s.<br />
    /// - A <see cref="Cabinet"/> has many <see cref="Occupant"/>s.<br />
    /// </summary>
    public interface ICabinet
    {
        public Vector3 sizeInMeters { get; }

        public IKitchen kitchen { get; }

        public Cuboid resizerCuboid { get; }

        public Cuboid occupiableCuboid { get; }

        public Transform GetTransform();

        public Transform GetLocalOrientationForOccupants();

    }   // interface ICabinet

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
