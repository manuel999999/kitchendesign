namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Server;
    /// <summary>
    ///     This contains the results of an occupant placement attempt. I.e.,
    ///     whether a product could be added to a volume. The information
    ///     contained can be useful to show feedback messages when an occupant
    ///     fails to be added to a volume.
    /// </summary>
    public struct VolumeOccupantResult
    {
        /// <summary>
        ///     The cabinet volume the placement check was made against.
        /// </summary>
        public OccupiableVolume volume;

        /// <summary>
        ///     The space in the cabinet that the product would like to occupy
        ///     (if placement failed) or the space the product is now occupying
        ///     (if placement succeeded). The cuboid is in the volume's local
        ///     space.
        /// </summary>
        public Cuboid occupantCuboidWithinVolume;

        /// <summary>
        ///     The info of the product that was attempting to be inserted.
        /// </summary>
        public ProductInfo productInfo;

        /// <summary>
        /// </summary>
        public ProductVisualSO productVisual;

        /// <summary>
        ///     The product that was trying to be inserted. It will be <see
        ///     langword="null" /> if the product did not exist and could not be
        ///     added.
        /// </summary>
        public Occupant sourceOccupant;

        /// <summary>
        ///     The first occupant found that blocks the insert attempt. If this
        ///     has a value, it will be a product different from the product
        ///     being inserted/moved (i.e., <see cref="sourceOccupant"/>).
        /// </summary>
        public Occupant blockingOccupant;

        /// <summary>
        ///     The state of the placement attempt. If the occupant was added,
        ///     the state will be <see cref="OccupantCuboidStatus.Fits"/>;
        ///     otherwise, the value will indicate which test failed.
        /// </summary>
        public OccupantCuboidStatus status;

    }   // struct VolumeOccupantResult


}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-05
/// ===========================================================================
