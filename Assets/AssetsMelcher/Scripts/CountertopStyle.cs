namespace KitchenDesign
{
    using Melcher;
    /// <summary>
    ///     Represents the different types of countertops in the app.
    ///     <br /><br />
    ///
    ///     Currently, the only special case the <see cref="Stoll"/> countertop
    ///     which clamps the thickness of countertops on the kitchen's backings.
    /// </summary>
    public enum CountertopType
    {
        Default,

        /// <summary>
        ///     A Stoll countertop. These are special in that they are clamped
        ///     to be 2" thick on backings (bar tops and backsplashes). The
        ///     Stoll version of the app can use both <see cref="Stoll"/> and
        ///     <see cref="Default"/> countertops.
        /// </summary>
        Stoll,

    }   // enum CountertopType

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic
/// Initial creation date: 2023-10-04
/// ===========================================================================
