namespace Melcher
{
    using UnityEngine;

#if UNITY_WEBGL
    using System.Runtime.InteropServices;
#endif  // if UNITY_WEBGL

    /// <summary>
    ///     Implements the Melcher WebGL JavaScript library.
    ///     <br /><br />
    ///
    ///     The library provides functions for accessing the browser's URL
    ///     and checking if the WebGL build is running within a mobile browser.
    /// </summary>
    public static class MelcherWebGL
    {
        // ====================================================================
        // Static functions
        // ====================================================================

        /// <summary>
        ///     Return <see langword="true"/> if this is a WebGL build or if
        ///     the Unity editor has WebGL as the active build target.
        ///     Otherwise, return <see langword="false"/>.
        /// </summary>
        public static bool IsActiveBuildTarget()
        {
            return MelcherUnityUtility.targetPlatform == RuntimePlatform.WebGLPlayer;
        }

        /// <summary>
        ///     If this is a WebGL instance, return <see langword="true"/> if
        ///     it is running on a mobile platform; otherwise, return <see
        ///     langword="false"/>.
        ///     <br /><br />
        ///
        ///     If this isn't a WebGL instance, return <see
        ///     cref="Application.isMobilePlatform"/>
        /// </summary>
        public static bool IsMobilePlatform()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return JS_IsMobilePlatform();
#else
            return Application.isMobilePlatform;
#endif  // if UNITY_WEBGL && !UNITY_EDITOR else
        }

        /// <summary>
        ///     Get the top-level URL of the browser. This is the URL of the
        ///     site in the user's browser which may be different from the URL
        ///     of the hosted content. E.g. a WebGL build may be hosted on one
        ///     site and referenced/embedded in another.
        ///     <br /><br />
        ///
        ///     This always returns <see langword="null"/> in non-WebGL
        ///     platforms (including the editor).
        /// </summary>
        public static string GetTopLevelUrl()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return JS_GetTopLevelUrl();
#else
            return null;
#endif  // if UNITY_WEBGL && !UNITY_EDITOR else
        }

        /// <summary>
        ///     Check if a URL parameter is defined. This only checks the
        ///     active iframe's URL because checking the parent's URL is
        ///     basically impossible with modern browser security (CORS).
        ///     <br /><br />
        ///
        ///     E.g., with the URL:
        ///     <br /><br />
        ///
        ///     <![CDATA[
        ///     https://www.vrts.ca/?arg1&arg2=value2
        ///     ]]>
        ///     <br /><br />
        ///
        ///     Passing in "arg1" would return <see langword="true"/> and
        ///     passing in "foo" would return <see langword="false"/>.
        ///     <br /><br />
        ///
        ///     This always returns <see langword="false"/> in non-WebGL
        ///     platforms (including the editor).
        /// </summary>
        public static bool HasUrlParameter(string paramKey)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return JS_HasUrlParameter(paramKey);
#else
            return false;
#endif  // if UNITY_WEBGL && !UNITY_EDITOR else
        }

        /// <summary>
        ///     Get the value of a URL parameter.  This only checks the active
        ///     iframe's URL because checking the parent's URL is basically
        ///     impossible with modern browser security (CORS).
        ///     <br /><br />
        ///
        ///     E.g., with the URL:
        ///     <br /><br />
        ///
        ///     <![CDATA[
        ///     https://www.vrts.ca/?arg1&arg2=value2
        ///     ]]>
        ///     <br /><br />
        ///
        ///     The key "arg1" would return "", the key "arg2" would return
        ///     "value2", and the key "foo" would return <see langword="null"/>.
        ///     <br /><br />
        ///
        ///     This always returns <see langword="null"/> in non-WebGL
        ///     platforms (including the editor).
        /// </summary>
        public static string GetUrlParameter(string paramKey)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return JS_GetUrlParameter(paramKey);
#else
            return null;
#endif  // if UNITY_WEBGL && !UNITY_EDITOR else
        }

        /// <summary>
        ///     Change the URL of the current browser tab.
        ///     <br /><br />
        ///
        ///     This does nothing in non-WebGL builds and the editor.
        /// </summary>
        public static void ChangeUrl(string newUrl)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_ChangeUrl(newUrl);
#endif  // if UNITY_WEBGL && !UNITY_EDITOR
        }

        /// <summary>
        ///     Open a new tab in the browser.
        /// </summary>
        public static void OpenNewTab(string tabUrl)
        {
            if (tabUrl != null)
            {
                Application.OpenURL(tabUrl);
            }
        }

        /// <summary>
        ///     Try to download a file from its raw bytes. The user may see a
        ///     "save as" prompt if their browser is configured to do so.
        ///     Either way, the download has the potential to be canceled by
        ///     the user, something we can't detect, and therefore this is a
        ///     "try" function with no return value.
        ///     <br /><br />
        ///
        ///     This does nothing in a non-WebGL build or the editor.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        public static void TryDownloadingFile(byte[] data, string fileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_TryDownloadingFile(data, data.Length, fileName);
#endif  // if UNITY_WEBGL && !UNITY_EDITOR
        }

        // ====================================================================
        // Imports
        // ====================================================================

#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern bool JS_IsMobilePlatform();

        [DllImport("__Internal")]
        private static extern string JS_GetTopLevelUrl();

        [DllImport("__Internal")]
        private static extern bool JS_HasUrlParameter(string paramKey);

        [DllImport("__Internal")]
        private static extern string JS_GetUrlParameter(string paramKey);

        [DllImport("__Internal")]
        private static extern string JS_ChangeUrl(string newUrl);

        [DllImport("__Internal")]
        private static extern void JS_TryDownloadingFile(byte[] data, int dataSizeInBytes, string fileName);

#endif  // if UNITY_WEBGL

    }   // class MelcherWebGL

}   // namespace Melcher
