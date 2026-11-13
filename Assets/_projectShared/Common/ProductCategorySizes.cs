using KitchenDesign.Shared.Enums;

namespace KitchenDesign.Shared.Common
{
    public static class ProductCategorySizes
    {
        public static bool DoesCategoryUseSizes(ProductCategory category)
        {
            return
                category != ProductCategory.Finish &&
                category != ProductCategory.FinishFloor &&
                category != ProductCategory.FinishCabinet &&
                category != ProductCategory.FinishCountertop;
        }
    }
}
