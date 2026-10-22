namespace KitchenDesign
{
    using Melcher;
    /// <summary>
    /// Types of screenshot camera angles. This is used to determine what
    /// product measurements should be made visible when taking screenshots. It
    /// is also used to describe which screenshot angles a inter-product
    /// measurement should be rendered in.
    /// </summary>
    public enum ScreenshotAngle
    {
        /// <summary>
        /// Default/none state.
        /// </summary>
        None,

        /// <summary>
        /// A cover photo. This screenshot has perspective and is used as the
        /// "beauty shot" for the PDF document.
        /// </summary>
        Cover,

        /// <summary>
        /// A top-down perspective. The top-down photo uses a square aspect
        /// ratio to better capture the full kitchen.
        /// </summary>
        Top,

        /// <summary>
        /// A front-facing perspective.
        /// </summary>
        Front,

        /// <summary>
        /// The back of the cabinet.
        /// </summary>
        Back,

    }   // enum ScreenshotAngle

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-10-20
/// ===========================================================================
