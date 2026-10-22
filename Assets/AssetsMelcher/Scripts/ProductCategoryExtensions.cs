namespace KitchenDesign
{
    using Melcher;
    public static class ProductCategoryExtensions
    {
        /// <summary>
        /// Return <see cref="true"/> if the category is a finish. I.e., it
        /// represents a countertop, cabinet or flooring material.
        /// </summary>
        public static bool IsFinish(this ProductCategory category)
        {
            return
                category == ProductCategory.Finish ||
                category == ProductCategory.FinishCountertop ||
                category == ProductCategory.FinishCabinet ||
                category == ProductCategory.FinishFloor;
        }

        public static bool IsBacking(this ProductCategory category)
        {
            return
                category == ProductCategory.Backsplash ||
                category == ProductCategory.BarTopHigh ||
                category == ProductCategory.BarTopLow;
        }

    }   // class ProductCategoryExtensions

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-12-06
/// ===========================================================================
