namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Geometry;
    /// <summary>
    ///     This interface provides a way for <see cref="ProductVisualContent"/>s
    ///     to modify the cuboid they occupy in an <see
    ///     cref="OccupiableVolume"/>. For example, a <see cref="CabinetShelf"/>
    ///     will extend its occupied cuboid downward to encompass its
    ///     countertop thickness.
    /// </summary>
    public interface IProductVisualModifyOccupiedCuboidHandler
    {
        /// <summary>
        ///     This method is called by an <see cref="OccupiableVolume"/>
        ///     when the volume calculates how much space the product will
        ///     occupy in the volume.
        ///     <br /><br />
        ///
        ///     This function is intended to allow products such as the <see
        ///     cref="CabinetShelf"/>s to inflate its occupied size to include
        ///     the thickness of the countertop, without altering the cut-out
        ///     size.
        /// </summary>
        /// <param name="occupiedCuboid">
        ///     The cabinet-local cuboid that the product occupies. The
        ///     cuboid is already anchored and clipped to the volume in
        ///     question.
        /// </param>
        void OnModifyOccupiedCuboid(ref Cuboid occupiedCuboid, OccupiableVolume volumeDoingTesting);

    }   // interface IProductVisualModifyOccupiedCuboidHandler

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic
/// Initial creation date: 2023-10-05
/// ===========================================================================
