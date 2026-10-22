using KitchenDesign.Shared.Enums;

namespace KitchenDesign.Shared.Common
{
    public static class ProductCategoryExtensions
    {
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
    }
}
