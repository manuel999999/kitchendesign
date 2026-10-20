namespace Melcher
{
    // TODO: Check that this is always serialized as a MelcherStableEnum.
    public enum CabinetType
    {
        Unknown,
        EndLeft,
        EndRight,
        Straight,
        // TODO: Rename this. It should disambiguate that this is a real cabinet
        //       section. Some cabinet layouts, like galley, have stealth
        //       corners: some other cabinet type whose rotation forces a
        //       turn. -Ryan
        Corner,
        Space,

        /// <summary>
        ///     A section of cabinet that may only appear immediately to the
        ///     left of a corner cabinet. This type of cabinet can be reordered
        ///     in the cabinet layout (only during Catalog building method)
        ///     where it can be placed between a straight cabinet and a corner.
        ///     <br /><br />
        ///
        ///     When a kitchen's cabinets have columns (e.g., <see
        ///     cref="KitchenBuildingMethod.Sections"/> building method), an
        ///     extra cabinet section is added before and after corner
        ///     sections. This ensures the columns on the inside corners have
        ///     the correct width (when combined with the half-column in the
        ///     other cabinet sections) without complicating the anchoring
        ///     behaviour of the corner itself.
        /// </summary>
        StraightOnLeftOfCorner,

        /// <summary>
        ///     An extra bit of cabinet added after a corner section when the
        ///     kitchen is in <see cref="KitchenBuildingMethod.Sections"/> mode.
        ///     This cabinet section appears to the right of a corner.
        /// </summary>
        StraightOnRightOfCorner,

        /// <summary>
        ///     A transition between two cabinets where the left cabinet is
        ///     taller than the cabinet on the right.
        /// </summary>
        HighLeftToLowRight,

        /// <summary>
        ///     The reverse of <see cref="HighLeftToLowRight"/>.
        /// </summary>
        LowLeftToHighRight,

        /// <summary>
        ///     A special type of cabinet that anchors itself to a corner to
        ///     create a resizeable 45-degree wedge. Each wedge represents its
        ///     own cabinet span.
        /// </summary>
        CornerWedge,

    }   // enum CabinetType

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-14
/// ===========================================================================
