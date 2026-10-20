namespace Melcher
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    ///     This interface provides a number of callback methods to allow
    ///     custom <see cref="Occupant"/> behaviours. The <see
    ///     cref="Occupant"/> script attempts to find components on the same
    ///     game object as its self that implement this interface. For each
    ///     component it finds, it will invoke the relevant callback method.
    ///     E.g., <see cref="CabinetBacking"/>s implements this interface to
    ///     add its ability to resize the <see cref="Occupant"/> to fit its
    ///     cabinet.
    /// </summary>
    // TODO: Should these be split into separate interfaces?
    public interface IOccupantListener
    {
        void OnAddedToCabinet(ICabinet cabinet);

        void OnRemovedFromCabinet(ICabinet cabinet);

        UniTask OnCabinetMaterialsChanged(Material cabinetMaterial, Material countertopMaterial);

    }   // interface IOccupantListener

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-11
/// ===========================================================================
