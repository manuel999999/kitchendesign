namespace Melcher
{
    using UnityEditor;

    /// <summary>
    ///     Adds a context menu to try invoking any <see
    ///     cref="UnityEngine.Object"/>'s OnValidate method via
    ///     reflection.
    /// </summary>
    internal static class MelcherValidateContextMenu
    {
        /// <summary>
        ///     Use reflection to call the object's OnValidate method.
        /// </summary>
        [MenuItem("CONTEXT/Object/Validate")]
        private static void Validate(MenuCommand command)
        {
            MelcherEditorUtility.Validate(command.context);
        }

    }   // class MelcherValidateContextMenu

}   // namespace Melcher
