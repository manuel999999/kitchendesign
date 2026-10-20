namespace Melcher
{
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// Defines common file locations. These are useful when used in conjunction
    /// with <see cref="MelcherTransformFilePath"/>.
    /// </summary>
    public static class MelcherPredefinedFilePath
    {
        /// <summary>
        /// The absolute path to the project's root.
        /// </summary>
        public static string project => Path.GetDirectoryName(Application.dataPath);

        /// <summary>
        /// The absolute path to the project's data folder. The name of the
        /// last folder in this path is "Assets" when running in the editor and
        /// "&lt;projectName&gt;_Data" in builds.
        /// </summary>
        public static string data => Application.dataPath;

        public static string streamingAssets => Application.streamingAssetsPath;
        public static string persistentData => Application.persistentDataPath;

        /// <summary>
        ///     Like <see cref="streamingAssets"/> but is guaranteed to have
        ///     a scheme prefixed. E.g., "file://", "https://", etc.
        /// </summary>
        public static string webSafeStreamingAssets
        {
            get
            {
                string path = streamingAssets;

                if (Application.platform != RuntimePlatform.Android &&
                    Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    path = "file://" + path;
                }

                return path;
            }
        }

    }   // class MelcherPredefinedFilePath

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-07-14
/// ===========================================================================
