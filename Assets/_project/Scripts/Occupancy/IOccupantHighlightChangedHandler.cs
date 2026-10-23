namespace KitchenDesign.Occupancy
{
    using Melcher;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using KitchenDesign.Cabinets;

    public interface IOccupantHighlightChangedHandler
    {
        UniTask OnOccupantHighlightChanged(ICabinet cabinet, Material highlight);

    }   // interface IOccupantHighlightChangedHandler

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-05-15
/// ===========================================================================
