namespace Melcher
{
    /// <summary>
    ///     Used by authored cabinets in Catalogs building mode.
    /// </summary>
    public enum CabinetSectionFootingType
    {
        /// <summary>
        ///     No gap under the cabinet. The cabinet rests on the floor.
        /// </summary>
        OnGround,

        /// <summary>
        ///     The cabinet is raised off the ground by the height of its
        ///     catalog's cabinet leg. The legs are visible.
        /// </summary>
        OnLegs,

        /// <summary>
        ///     The cabinet is raised off the ground by the height of its
        ///     catalog's cabinet leg. No legs are visualized for this cabinet.
        /// </summary>
        Floating,

        /// <summary>
        ///     The cabinet is raised off the ground by the height of its
        ///     catalog's cabinet leg (no legs visualized) and the whole height
        ///     is occupiable (including the empty air below the cabinet).
        /// </summary>
        FloatingOccupiableToGround,

    }   // enum CabinetSectionFootingType

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-11-28
/// ===========================================================================
