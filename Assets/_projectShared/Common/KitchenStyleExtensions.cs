using KitchenDesign.Shared.Enums;

namespace KitchenDesign.Shared.Common
{
    public static class KitchenStyleExtensions
    {
        public static string ToDisplayName(this KitchenStyle kitchenStyle)
        {
            return kitchenStyle switch
            {
                KitchenStyle.Straight => "Straight",
                KitchenStyle.L => "L-Shaped",
                KitchenStyle.U => "U-Shaped",
                KitchenStyle.Galley => "Galley",
                _ => throw new System.ArgumentException(nameof(kitchenStyle))
            };
        }
    }
}
