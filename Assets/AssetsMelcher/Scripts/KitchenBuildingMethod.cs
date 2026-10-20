namespace Melcher
{
    /// <summary>
    ///     Determines the types of options the player has during kitchen
    ///     design. The intent is to allow the same project to be reused with
    ///     slight alterations.
    /// </summary>
    // TODO: Consider something like a capabilities list instead. I.e., the
    //       ability to check if the app supports section-based building, or
    //       white-label mode, etc. This will allow build with different
    //       permutations of features. -Ryan
    [System.Flags]
    public enum KitchenBuildingMethod
    {
        /// <summary>
        ///     The original kitchen mode: the player chooses a cabinet layout
        ///     style (straight, L, U, etc.) and can only change each cabinet
        ///     section's size.
        /// </summary>
        Standard = 1 << 0,

        /// <summary>
        ///     The block mode: the player chooses a cabinet layout as before
        ///     but is also able to add more cabinet sections after the fact.
        /// </summary>
        Sections = 1 << 1,

        /// <summary>
        ///     The catalog system as used in Universal Kitchen Designer. The
        ///     user chooses a catalog at the start and then is able to build a
        ///     kitchen in a manner similar to <see cref="Sections"/> except
        ///     with the user's choice of unique cabinet models and without
        ///     cabinet mullions/columns.
        /// </summary>
        Catalog = 1 << 2,

    }   // enum KitchenBuildingMethod

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-08-02
/// ===========================================================================
