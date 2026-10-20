namespace Melcher
{
    using System;
    using UnityEngine;

    /// <summary>
    ///     This class allows <see cref="Application.wantsToQuit"/> to be tested
    ///     within the editor's Play Mode. Normally, it is not possible to test
    ///     this event in the editor because the function that calls it, <see
    ///     cref="Application.Quit"/>, is stubbed out during Play Mode.
    ///     <br /><br />
    ///
    ///     To use this class, add a listener to its <see cref="wantsToQuit"/>
    ///     event.  Then call this class's <see cref="Quit"/> function. This
    ///     will attempt to quit the app (or end Play Mode in the editor) but
    ///     will first notify the <see cref="wantsToQuit"/> listeners and give
    ///     them a chance to cancel the request.
    ///     <br /><br />
    ///
    ///     <see cref="wantsToQuit"/> forwards to <see
    ///     cref="Application.wantsToQuit"/> in builds.
    /// </summary>
    public class MelcherQuitWrapper
    {
        /// <summary>
        ///     This event is triggered when <see cref="Quit"/> is called.
        ///     <br /><br />
        ///
        ///     In builds, this event forwards to <see
        ///     cref="Application.wantsToQuit"/>.
        /// </summary>
        public static event Func<bool> wantsToQuit
        {
            add
            {
                if (Application.isEditor)
                {
                    m_WantsToQuitEditorPlaymode += value;
                }
                else
                {
                    Application.wantsToQuit += value;
                }
            }

            remove
            {
                if (Application.isEditor)
                {
                    m_WantsToQuitEditorPlaymode -= value;
                }
                else
                {
                    Application.wantsToQuit -= value;
                }
            }
        }

        /// <summary>
        ///     Try to quit the app. The request can be caught and canceled by
        ///     listeners to <see cref="wantsToQuit"/>. If it canceled, further
        ///     listeners will not be notified and the app will not quit.
        ///     Otherwise, if the request is not canceled, the app will quit or
        ///     or Play Mode will end (in the editor).
        ///     <br /><br />
        ///
        ///     This function works in both the editor and in builds.
        /// </summary>
        public static void Quit()
        {
            // Same functionality as Application.wantsToQuit. Iterate the list
            // and allow the first non-true return value to cancel the quit
            if (m_WantsToQuitEditorPlaymode != null)
            {
                foreach (Func<bool> continueQuit in m_WantsToQuitEditorPlaymode.GetInvocationList())
                {
                    try
                    {
                        if (!continueQuit())
                        {
                            // Cancel the quit attempt
                            return;
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }

#if UNITY_EDITOR
            // End play-mode
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // Exit the build
            Application.Quit();
#endif  // if UNITY_EDITOR else
        }

        /// <summary>
        ///     A replacement for <see cref="Application.wantsToQuit"/> that we
        ///     can invoke.
        /// </summary>
        private static event Func<bool> m_WantsToQuitEditorPlaymode;

    }   // class MelcherQuitWrapper

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-15
/// ===========================================================================
