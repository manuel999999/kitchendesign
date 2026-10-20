namespace Melcher
{
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
        public ProductVisual productVisual;

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

    /// <summary>
    ///     Summarizes the result of an attempt to insert an occupant into a
    ///     cabinet volume.
    /// </summary>
    public enum OccupantCuboidStatus
    {
        /// <summary>
        ///     The product fits in the volume. None of the product's cuboid
        ///     expands beyond the volume it was compared to.
        /// </summary>
        Fits,

        /// <summary>
        ///     The volume doesn't allow this product to be added. Some cabinet
        ///     sections disallow all products (e.g., corners, ends and spacers)
        ///     and <see cref="CabinetShelf"/> is restricted to burners.
        /// </summary>
        CannotContainProduct,

        /// <summary>
        ///     The product would overlap another product.
        /// </summary>
        OverlapsAnotherOccupant,

        /// <summary>
        ///     The product would overlap the cabinet span's backing.
        /// </summary>
        OverlapsBacking,

        /// <summary>
        ///     The product is too far left in the volume.
        /// </summary>
        TooFarLeft,

        /// <summary>
        ///     The product is too far right in the volume.
        /// </summary>
        TooFarRight,

        /// <summary>
        ///     The product is too far down in the volume.
        /// </summary>
        TooFarDown,

        /// <summary>
        ///     The product is too far up in the volume.
        /// </summary>
        TooFarUp,

        /// <summary>
        ///     The product is too far back in the volume.
        /// </summary>
        TooFarBack,

        /// <summary>
        ///     The product is too far forward in the volume.
        /// </summary>
        TooFarForward,

        /// <summary>
        ///     The product needs to resize the cabinet but doing so would
        ///     cause issues. E.g., the cabinet might shrink such that existing
        ///     products would be invalid.
        /// </summary>
        CannotResizeCabinet,

        /// <summary>
        ///     Used when trying to add a cabinet backing to a cabinet span
        ///     that has non-standard cabinet heights. That is, the two
        ///     features are mutually exclusive.
        /// </summary>
        NotCompatible,

        /// <summary>
        ///     The product could not be instantiated a reason relating to
        ///     the data. For example, the <see cref="ProductInfo"/> for the
        ///     product may not have been found.
        /// </summary>
        DataError,

        /// <summary>
        ///     Unity's instantiate function returned <see langword="null"/>.
        /// </summary>
        InstantiateError,

    }   // enum OccupantCuboidStatus

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-05
/// ===========================================================================
