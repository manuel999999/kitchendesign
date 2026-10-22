namespace KitchenDesign
{
    using Melcher;
    public static class ScreenshotAngleExtensions
    {
        /// <summary>
        ///     Return <see langword="true"/> if the screenshot angle
        ///     represents one that is us used for the CAD/plan images.
        ///     I.e., it is <see cref="ScreenshotAngle.Top"/>, <see
        ///     cref="ScreenshotAngle.Front"/> or <see
        ///     cref="ScreenshotAngle.Back"/>.
        /// </summary>
        public static bool IsPlanAngle(this ScreenshotAngle angle)
        {
            return
                angle == ScreenshotAngle.Top ||
                angle == ScreenshotAngle.Front ||
                angle == ScreenshotAngle.Back;
        }

    }   // static class ScreenshotAngleExtensions

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-07-20
/// ===========================================================================
