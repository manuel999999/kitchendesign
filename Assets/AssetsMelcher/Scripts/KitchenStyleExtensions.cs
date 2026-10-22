namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

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
    }   // class KitchenStyleExtensions

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-12-03
/// ===========================================================================
