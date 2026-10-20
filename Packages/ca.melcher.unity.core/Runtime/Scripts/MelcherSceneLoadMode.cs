namespace Melcher
{
    public enum MelcherSceneLoadMode
    {
        Single,

        Additive,

        /// <summary>
        /// Like <see cref="Additive"/> but skip if the scene is already
        /// loaded.
        /// </summary>
        Include

    }   // enum MelcherSceneLoadMode

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-01-19
/// ===========================================================================
