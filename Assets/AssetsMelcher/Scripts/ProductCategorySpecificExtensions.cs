namespace KitchenDesign
{
    using Melcher;
    public static class ProductCategorySpecificExtensions
    {
        /// <summary>
        ///     Convert a <see cref="ProductCategorySpecific"/> enum to a
        ///     <see cref="ProductCategory"/>. The latter is used more widely
        ///     through the app and therefore it is convenient to support this
        ///     conversion.
        /// </summary>
        public static ProductCategory ToProductCategory(this ProductCategorySpecific specificCategory)
        {
            return specificCategory switch
            {
                ProductCategorySpecific.Accessory   => ProductCategory.Accessory,
                ProductCategorySpecific.Burner      => ProductCategory.Burner,
                ProductCategorySpecific.Cabinet     => ProductCategory.FinishCabinet,
                ProductCategorySpecific.Cookware    => ProductCategory.Cookware,
                ProductCategorySpecific.Countertop  => ProductCategory.FinishCountertop,
                ProductCategorySpecific.Flooring    => ProductCategory.FinishFloor,
                ProductCategorySpecific.Fridge      => ProductCategory.Sink,
                ProductCategorySpecific.Grill       => ProductCategory.Grill,
                ProductCategorySpecific.GrillWithInsulatedJacket => ProductCategory.GrillWithInsulatedJacket,
                ProductCategorySpecific.BurnerWithInsulatedJacket => ProductCategory.BurnerWithInsulatedJacket,
                ProductCategorySpecific.IceBox      => ProductCategory.Sink,
                ProductCategorySpecific.Sink        => ProductCategory.Sink,
                ProductCategorySpecific.Smoker      => ProductCategory.Cookware,
                ProductCategorySpecific.Storage     => ProductCategory.Storage,
                _                                   => ProductCategory.Unknown
            };
        }

    }   // class ProductCategorySpecificExtensions

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-05-17
/// ===========================================================================
