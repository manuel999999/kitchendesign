namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEditor;

    public static class MelcherBuildNameOptions
    {
        /// <summary>
        /// Re-load the target platform's build name options from EditorPrefs.
        /// </summary>
        public static void LoadFromEditorPrefs()
        {
            // Load settings from editor prefs
            LoadBoolFromEditorPrefs(nameof(s_UseAutomaticBuildNaming), ref s_UseAutomaticBuildNaming);
            LoadBoolFromEditorPrefs(nameof(s_UseAppIdentifier), ref s_UseAppIdentifier);
            LoadBoolFromEditorPrefs(nameof(s_AppendBuildDate), ref s_AppendBuildDate);
            LoadBoolFromEditorPrefs(nameof(s_AppendBuildVersion), ref s_AppendBuildVersion);
            LoadBoolFromEditorPrefs(nameof(s_AppendBuildOptionTags), ref s_AppendBuildOptionTags);
            LoadBoolFromEditorPrefs(nameof(s_ForceUniqueName), ref s_ForceUniqueName);
        }

        public static void DrawEmbeddedGUI()
        {
            // Draw a preview
            string exampleName = "<user controlled naming>";
            if (s_UseAutomaticBuildNaming)
            {
                BuildPlayerOptions exampleBuildOptions = new BuildPlayerOptions()
                {
                    locationPathName = $"/{Application.productName}",
                    options = BuildOptions.Development
                };
                RenameOutput(ref exampleBuildOptions, pretendNotUnique: true);
                exampleName = exampleBuildOptions.locationPathName;
            }

            GUILayout.Label(exampleName, GUI.skin.box, GUILayout.ExpandWidth(true));

            void DrawToggle(ref bool toggleState, GUIContent guiContent)
            {
                bool newToggleState = GUILayout.Toggle(toggleState, guiContent, Array.Empty<GUILayoutOption>());

                if (newToggleState != toggleState)
                {
                    toggleState = newToggleState;

                    SaveToEditorPrefs();
                }
            }

            DrawToggle(ref s_UseAutomaticBuildNaming, s_UseAutomaticBuildNamingContent);

            using (new EditorGUI.DisabledGroupScope(disabled: !s_UseAutomaticBuildNaming))
            {
                using (var indentScope = new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(20.0f);

                    using (var indentedVerticalScope = new GUILayout.VerticalScope())
                    {
                        DrawToggle(ref s_UseAppIdentifier, s_UseAppIdentifierContent);
                        DrawToggle(ref s_AppendBuildDate, s_AppendBuildDateContent);
                        DrawToggle(ref s_AppendBuildVersion, s_AppendBuildVersionContent);
                        DrawToggle(ref s_AppendBuildOptionTags, s_AppendBuildOptionTagsContent);
                        DrawToggle(ref s_ForceUniqueName, s_ForceUniqueNameContent);
                        DrawToggle(ref s_OnlyNameContainingFolder, s_OnlyNameContainingFolderContent);
                    }
                }
            }
        }

        public static void RenameOutput(ref BuildPlayerOptions options)
        {
            if (s_UseAutomaticBuildNaming)
            {
                LoadFromEditorPrefs();

                RenameOutput(ref options, pretendNotUnique: false);
            }
        }

        private static void RenameOutput(ref BuildPlayerOptions options, bool pretendNotUnique)
        {
            // Normalize path slashes
            string originalOutputPath = Path.GetFullPath(options.locationPathName);

            List<string> nameParts = new List<string>();

            if (s_UseAppIdentifier)
            {
                // Use the app's identifier/bundle name
                string appIdentifier = MelcherEditorUtility.GetApplicationIdentifier();
                nameParts.Add(appIdentifier);
            }
            else
            {
                // Use the app name with any spaces removed
                string appName = Application.productName;
                appName = appName.Replace(" ", "");
                nameParts.Add(appName);
            }

            if (s_AppendBuildDate)
            {
                // ISO date format (local time)
                string date = System.DateTime.Now.ToString("yyyy-MM-dd");
                nameParts.Add(date);
            }

            if (s_AppendBuildVersion)
            {
                // App version (dots to dashes)
                string version = MelcherVersion.version;
                version = version.Replace(".", "-");
                nameParts.Add(version);
            }

            if (s_AppendBuildOptionTags)
            {
                // Extra tag information based on build options
                if (options.options.HasFlag(BuildOptions.Development))
                {
                    nameParts.Add("dev");
                }
            }

            string baseRenamedOutput = string.Join("_", nameParts);

            // The parent folder that the build is placed into
            string outputDir = Path.GetDirectoryName(originalOutputPath);
            // The build's file extension (".exe", ".apk", "", etc.)
            string outputExt = Path.GetExtension(originalOutputPath);
            // If forcing a unique name, this is suffix that makes it unique
            string unique = "";

            string GetNewOutputPath()
            {
                return Path.Combine(outputDir, $"{baseRenamedOutput}{unique}{outputExt}");
            }

            // Append numbers to the output name if a build with the same name
            // already exists
            if (s_ForceUniqueName)
            {
                // Accumulator to ensure unique file names
                int uniqueAttempt = 0;

                bool DoesOutputExist()
                {
                    string output = GetNewOutputPath();
                    return File.Exists(output) || Directory.Exists(output);
                }

                while (DoesOutputExist() || pretendNotUnique)
                {
                    pretendNotUnique = false;

                    ++uniqueAttempt;
                    unique = $"_{uniqueAttempt:00}";
                }
            }

            string newOutputPath = GetNewOutputPath();

            // For platforms that output a folder (e.g. WebGL, iOS), we have to
            // ensure the folder exists.
            bool isOutputADirectory = Directory.Exists(originalOutputPath);
            if (isOutputADirectory)
            {
                // Attempt to handle case-sensitive file systems
                bool isOsCaseSensitive =
                    !System.Environment.OSVersion.ToString().Contains("Windows");

                bool isNewOutputPathDifferentFromOriginal =
                    0 != string.Compare(originalOutputPath, newOutputPath, ignoreCase: isOsCaseSensitive);

                if (isNewOutputPathDifferentFromOriginal)
                {
                    // Unity will already have created an output folder for the
                    // original output path before the pre-build handlers are
                    // called. This means a folder for the wrong output name
                    // will persist unless we remove it.

                    // We test if it's safe to remove the original output
                    // directory. It must be empty and just recently created.

                    System.IO.DirectoryInfo i = new DirectoryInfo(originalOutputPath);
                    bool isRecent = (System.DateTime.UtcNow - i.CreationTimeUtc) < System.TimeSpan.FromMinutes(1.0);
                    bool hasFiles = i.EnumerateFiles().GetEnumerator().MoveNext();

                    if (isRecent && !hasFiles)
                    {
                        Directory.Delete(originalOutputPath);
                    }
                }

                // Ensure a directory exists for the new output path
                if (!Directory.Exists(newOutputPath))
                {
                    Directory.CreateDirectory(newOutputPath);
                }
            }

            options.locationPathName = newOutputPath;
        }

        private static void SaveToEditorPrefs()
        {
            SaveBoolToEditorPrefs(nameof(s_UseAutomaticBuildNaming), s_UseAutomaticBuildNaming);
            SaveBoolToEditorPrefs(nameof(s_UseAppIdentifier), s_UseAppIdentifier);
            SaveBoolToEditorPrefs(nameof(s_AppendBuildDate), s_AppendBuildDate);
            SaveBoolToEditorPrefs(nameof(s_AppendBuildVersion), s_AppendBuildVersion);
            SaveBoolToEditorPrefs(nameof(s_AppendBuildOptionTags), s_AppendBuildOptionTags);
            SaveBoolToEditorPrefs(nameof(s_ForceUniqueName), s_ForceUniqueName);
        }

        private static void LoadBoolFromEditorPrefs(string fieldName, ref bool toLoad)
        {
            // Default to whatever the value was. This means values will copy 
            // over to other platforms when they're not defined yet
            toLoad = EditorPrefs.GetBool(GetEditorPrefsKey(fieldName), defaultValue: toLoad);
        }

        private static void SaveBoolToEditorPrefs(string fieldName, bool toSave)
        {
            EditorPrefs.SetBool(GetEditorPrefsKey(fieldName), toSave);
        }

        private static string GetEditorPrefsKey(string fieldName)
        {
            string platform = EditorUserBuildSettings.activeBuildTarget.ToString();
            string simpleName = UnityEditor.ObjectNames.NicifyVariableName(fieldName).Replace(" ", "");
            return $"{typeof(MelcherBuildNameOptions).FullName}.{platform}_{simpleName}";
        }

        static MelcherBuildNameOptions()
        {
            // TODO: Should this be an Init() function like is done in MelcherVariableDrawer?
            LoadFromEditorPrefs();

            s_UseAutomaticBuildNamingContent    = new GUIContent("Automatic build naming", tooltip: "If enabled, the output build name will be automatically chosen and the user's input is ignored.");
            s_UseAppIdentifierContent           = new GUIContent("Use app identifier", tooltip: "Use the application identifier as the base output name. When this is off, the product name will be used.");
            s_AppendBuildDateContent            = new GUIContent("Append build date", tooltip: "Append the local build time in ISO format (YYYY-MM-DD)");
            s_AppendBuildVersionContent         = new GUIContent("Append build version", tooltip: "Append the app's version (using MelcherVersion.version)");
            s_AppendBuildOptionTagsContent      = new GUIContent("Append build option tags", tooltip: "Append significant build options to the output name. (E.g. Development Mode => \"dev\")");
            s_ForceUniqueNameContent            = new GUIContent("Force unique name", tooltip: "This appends a number to the build name if the generated name would conflict with an existing build.");
            s_OnlyNameContainingFolderContent   = new GUIContent("Only name containing folder", tooltip: "");
        }

        // TODO: Setup a GUIContent for each checkbox. These will have a tooltip
        //       that better explains the function of each option
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_UseAutomaticBuildNamingContent;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_UseAppIdentifierContent;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_AppendBuildDateContent;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_AppendBuildVersionContent;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_AppendBuildOptionTagsContent;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_ForceUniqueNameContent;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_OnlyNameContainingFolderContent;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_ReplaceSpacesWithUnderscoresContent;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static bool s_UseAutomaticBuildNaming;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static bool s_UseAppIdentifier;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static bool s_AppendBuildDate;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static bool s_AppendBuildVersion;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static bool s_AppendBuildOptionTags;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static bool s_ForceUniqueName;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static bool s_OnlyNameContainingFolder;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static bool s_ReplaceSpacesWithUnderscores;

    }   // class MelcherBuildNameOptions

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-18
/// ===========================================================================
