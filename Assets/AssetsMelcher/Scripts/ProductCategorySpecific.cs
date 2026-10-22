namespace KitchenDesign
{
    using Melcher;
    /// <summary>
    ///     Similar to <see cref="ProductCategory"/> except it provides more
    ///     granular values. It is intended to help categorize product visuals
    ///     for the purpose of altering the runtime prefabs' properties before
    ///     their associated <see cref="ProductInfo"/>s are known. See <see
    ///     cref="ProductVisualContent.runtimePrefabCategory"/>.
    /// </summary>
    public enum ProductCategorySpecific
    {
        Unknown,
        Accessory,
        Burner,
        /// <summary>
        ///     A cabinet finish.
        /// </summary>
        Cabinet,
        Cookware,
        Countertop,
        Flooring,
        Fridge,
        Grill,
        GrillWithInsulatedJacket,
        Sink,
        IceBox,
        Smoker,
        Storage,
        Shelf,
        /// <summary>
        ///     A cabinet with empty space for pre-arranged doors and drawers.
        /// </summary>
        CabinetSection,
        /// <summary>
        ///     A door or doors that fit into a <see cref="CabinetSection"/>.
        /// </summary>
        CabinetDoor,

        Backsplash,
        BarTopHigh,
        BarTopLow,
        BurnerWithInsulatedJacket
    }   // enum ProductCategorySpecific

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-10-31
/// ===========================================================================
