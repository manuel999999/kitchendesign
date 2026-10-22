namespace KitchenDesign.Products
{
    using Melcher;
    using KitchenDesign.Scriptables;
    using System.Collections.Generic;

    public static class ProductVisualLookups
    {
        public static ProductVisualSO FindById(IReadOnlyList<ProductVisualSO> productVisuals, string visualId)
        {
            if (string.IsNullOrEmpty(visualId))
            {
                return null;
            }

            return MelcherCommon.Find(
                productVisuals,
                currentVisual => currentVisual.info.id.Equals(visualId)
            );
        }
    }
}
