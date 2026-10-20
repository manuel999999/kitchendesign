namespace Melcher
{
    public enum ProductCategory
    {
        Unknown,
        Grill,
        GrillWithInsulatedJacket,
        Burner,
        Cookware,
        Storage,
        /// <summary>
        /// Sinks and fridges.
        /// </summary>
        Sink,
        Accessory,
        Backsplash,

        /// <summary>
        ///     The traditional bar top option. It has a riser whose
        ///     countertop extends out from the cabinet.
        /// </summary>
        BarTopHigh,

        /// <summary>
        ///     Extends the main countertop out from the back of the cabinet
        ///     but does not add any additional height. I.e., it does not
        ///     have a riser.
        /// </summary>
        BarTopLow,

        Finish, // TODO: Check if this is necessary anymore. I believe it was replaced with the other 3 ____Finish values. -Ryan
        FinishFloor,
        FinishCabinet,
        FinishCountertop,

        /// <summary>
        ///     These are the dropped areas of countertop where power burners
        ///     are generally placed. They were initially called shelves.
        /// </summary>
        Shelf,

        CabinetSection,
        CabinetDoor,
        CabinetDoorHandle,
        BurnerWithInsulatedJacket,
        CabinetEndPanels,
        CabinetBackPanels

    }   // enum ProductCategory

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
