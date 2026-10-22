using KitchenDesign.Shared.Enums;

namespace KitchenDesign.Shared.Common
{
    public static class ProductCategorySpecificExtensions
    {
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
    }
}
