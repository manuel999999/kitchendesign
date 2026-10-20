namespace Melcher
{
    using System.IO;

    /// <summary>
    ///     Save and load EditorPrefs-like values on a per-project, per-user
    ///     basis. Values are written into the Unity project's
    ///     "UserSettings/Melcher" folder.
    /// </summary>
    public static class MelcherEditorUserSettings
    {
        public const string DefaultExtension = ".txt";

        /// <summary>
        ///     Store a value to disk.
        /// </summary>
        public static void Set(string key, string value)
        {
            string pathToFileInUserData = KeyNameToFilePathInUserData(key);

            string directory = Path.GetDirectoryName(pathToFileInUserData);
            Directory.CreateDirectory(directory);

            File.WriteAllText(pathToFileInUserData, value);
        }

        /// <summary>
        ///     Attempt to read a previously stored value from disk.
        /// </summary>
        public static bool TryGet(string key, out string value)
        {
            string pathToFileInUserData = KeyNameToFilePathInUserData(key);

            try
            {
                value = File.ReadAllText(pathToFileInUserData);
            }
            catch
            {
                value = default;
                return false;
            }

            return true;
        }

        public static string KeyNameToFilePathInUserData(string key)
        {
            if (!Path.HasExtension(key))
            {
                key += DefaultExtension;
            }

            return
                Path.Combine(
                    MelcherPredefinedFilePath.project,
                    "UserSettings",
                    "Melcher",
                    key
                );
        }

    }   // class MelcherEditorUserSettings

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-10-23
/// ===========================================================================
