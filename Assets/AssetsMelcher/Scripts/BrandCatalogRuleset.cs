namespace Melcher
{
    /// <summary>
    ///     A ruleset indicates special behaviours for a catalog to follow.
    ///     For example, WeatherStrong does not add a back panel to a fridge.
    ///     We were previously handling these rules by checking the catalog's
    ///     name (using magic strings) but since adding generic catalogs, we
    ///     added this safer, more robust way of checking.
    /// </summary>
    public enum BrandCatalogRuleset
    {
        Unknown,

        Challenger,

        Danver,

        Napoleon,

        WeatherStrong,

    }   // enum BrandCatalogRuleset

}   // namespace Melcher

