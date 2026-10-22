namespace KitchenDesign
{
    using Melcher;
    /// <summary>
    /// Represents an axis that a measurement was calculated across. We use
    /// this in <see cref="KitchenScreenshotManager.OccupantMeasurementPair"/>
    /// to know which direction the measurement happened in. This is basically
    /// an unsigned/absolute version of <see cref="CabinetLocalDirection"/>.
    /// </summary>
    public enum MeasurementAxis
    {
        X,
        Y,
        Z
    }   // enum MeasurementAxis

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-03
/// ===========================================================================
