namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    ///     This enum allows flag-like data to be attached to a kitchen save.
    ///     For example, this is used to store whether Challenger's premium
    ///     finishes are active or not.
    /// </summary>
    public enum KitchenExtraFeature
    {
        /// <summary>
        ///     "The finish reflects powder coat interiors. Floors, shelves and
        ///     brackets are powder coated texture black. All other interior
        ///     panels are powder coated to match the exterior color
        ///     selection."
        /// </summary>
        ChallengerPremiumCabinetFinish,

        /// <summary>
        ///     This kitchen is using back panels. This adds extra depth to each
        ///     cabinet.
        /// </summary>
        CabinetBackPanels,

        CabinetLeftEndPanels,

        CabinetRightEndPanels,

    }   // enum KitchenExtraFeature

}   // namespace KitchenDesign

