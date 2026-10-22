namespace KitchenDesign
{
    using Melcher;
    /// <summary>
    /// A direction that is in the local-space of a cabinet/volume.
    /// </summary>
    // TODO: Rename to VolumeLocalDirection
    public enum CabinetLocalDirection
    {
        /// <summary>
        /// A direction pointing at the left side of the cabinet from the
        /// right side.
        /// </summary>
        Left,

        /// <summary>
        /// A direction pointing at the right side of the cabinet from the
        /// left side.
        /// </summary>
        Right,

        /// <summary>
        /// A direction pointing at the top of the cabinet from the bottom
        /// side.
        /// </summary>
        Up,

        /// <summary>
        /// A direction pointing at the bottom of the cabinet from the top
        /// side.
        /// </summary>
        Down,

        /// <summary>
        /// A direction pointing at the back of the cabinet from the front
        /// side. The back of the cabinet is where vents are attached.
        /// </summary>
        Back,

        /// <summary>
        /// A direction pointing at the front of the cabinet from the back
        /// side. The front of the cabinet is where the drawers are attached.
        /// </summary>
        Front

    }   // class CabinetLocalDirection

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-02
/// ===========================================================================
