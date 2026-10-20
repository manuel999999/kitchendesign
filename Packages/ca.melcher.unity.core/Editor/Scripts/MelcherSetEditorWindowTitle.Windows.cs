#if UNITY_EDITOR_WIN
namespace Melcher
{
    using System;
    using System.Runtime.InteropServices;

    // Windows implementation.
    // Based on the source code by element_wsc provided here:
    // https://forum.unity.com/threads/unity-project-name-on-windows-bar.618316/#post-4147537
    public static partial class MelcherSetUnityEditorTitle
    {
        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Recreate the default window title except prepend it with the
        ///     project's product name.
        /// </summary>
        static partial void ChangeEditorWindowTitle(string newTitle, ref bool isImplemented)
        {
            IntPtr windowHandle = GetActiveWindow();

            SetWindowTextA(windowHandle, newTitle);

            isImplemented = true;
        }

        // ====================================================================
        // Imports
        // ====================================================================

        [DllImport("User32")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("User32")]
        private static extern bool SetWindowTextA(IntPtr hWnd, string lpString);

    }   // class MelcherSetUnityEditorTitle

}   // namespace Melcher

#endif  // if UNITY_EDITOR_WIN

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-03-02
/// ===========================================================================
