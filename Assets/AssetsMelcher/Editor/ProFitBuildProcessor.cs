namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Scriptables;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditor;
    using System.IO;
    using System.Linq;
    using UnityEngine;
    using UnityEditor.iOS.Xcode;

    /// <summary>
    ///     Sets the app's background colour for the splash screen to match the
    ///     selected <see cref="AppSkin"/>. This is relevant in WebGL where the
    ///     splash screen background color is shown during the loading phase.
    /// </summary>
    public class ProFitBuildProcessor
        : IPreprocessBuildWithReport
        , IPostprocessBuildWithReport
    {
        public int callbackOrder => 10;

        public void OnPreprocessBuild(BuildReport report)
        {
            // App name
            PlayerSettings.productName =
                ProFitGlobals.appOptions.appDisplayName;

            {
                // WebGL background colour

                PlayerSettings.SplashScreen.backgroundColor =
                    ProFitGlobals.appOptions.appSkin.primaryColor;

                MelcherLog.Log("Applied app skin's primary color as background color. " +
                    MelcherFormat.ConsoleColorPreview(PlayerSettings.SplashScreen.backgroundColor)
                );
            }

            {
                // Application identifer (for mobile stores)

                MelcherEditorUtility.SetAllApplicationIdentifiers(
                    ProFitGlobals.appOptions.appIdentifier
                );

                MelcherLog.Log($"Applied app identifier: {ProFitGlobals.appOptions.appIdentifier}");
            }

            {
                // App icon (mainly for mobile stores)

                Texture2D appIcon = ProFitGlobals.appOptions.appIcon;

                // Set the default icon
                PlayerSettings.SetIconsForTargetGroup(
                    BuildTargetGroup.Unknown,
                    new[] { appIcon }
                );

#if UNITY_ANDROID
                // But for Android, we need to setup adapative icons. Without
                // this, Gemini's logo looks tiny and appears in a white box.
                PlatformIcon[] androidAdapativeIcons =
                    PlayerSettings.GetPlatformIcons(
                        BuildTargetGroup.Android,
                        UnityEditor.Android.AndroidPlatformIconKind.Adaptive
                    );

                // Only need to assign the first adaptive icon's foreground and
                // background. They are automatically scaled and re-used for
                // the smaller sizes.

                Texture2D androidIconBackground =
                    ProFitGlobals.appOptions.androidProjectSettings.adaptiveIconBackground;

                Texture2D androidIconForeground =
                    ProFitGlobals.appOptions.androidProjectSettings.adapativeIconForeground;

                if (((bool)androidIconBackground) != ((bool)androidIconForeground))
                {
                    throw new System.Exception($"AppOptions {ProFitGlobals.appOptions.name} must provide both or neither, foreground and background adaptive Android icons.");
                }

                androidAdapativeIcons[0].SetTexture(androidIconBackground, layer: 0);
                androidAdapativeIcons[0].SetTexture(androidIconForeground, layer: 1);

                PlayerSettings.SetPlatformIcons(
                    BuildTargetGroup.Android,
                    UnityEditor.Android.AndroidPlatformIconKind.Adaptive,
                    androidAdapativeIcons
                );

#endif

                MelcherLog.Log(
                    $"Applied app icon(s): {ProFitGlobals.appOptions.appIcon}",
                    appIcon
                );
            }

            MoveInactiveAppResources(
                from: s_ResourcesPath,
                to: s_InactiveResourcesPath
            );
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            MoveInactiveAppResources(
                from: s_InactiveResourcesPath,
                to: s_ResourcesPath
            );

            if (report.summary.platform == BuildTarget.iOS)
            {
                UpdatePlistFile(report.summary.outputPath);
            }
        }

        /// <summary>
        ///     Automatically add our usage descriptions for writing to the
        ///     user's photo library. The default descriptions do not pass
        ///     Apple review and this prevents us from having to manually
        ///     override the default description each build.
        /// </summary>
        /// <param name="buildOutputPath"></param>
        private void UpdatePlistFile(string buildOutputPath)
        {
            string plistFilePath = Path.Combine(buildOutputPath, "Info.plist");

            if (!File.Exists(plistFilePath))
            {
                MelcherLog.LogWarning($"Info.plist not found. Is the path correct? ({plistFilePath})");
                return;
            }

            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistFilePath);

            // Add a review-approved usage description for writing to the user's
            // photo library.
            const string PhotoAddUsageDescription =
                "Allows you to save screenshots of your 3D kitchen designs directly to your Photo Library.";

            plist.root.SetString("NSPhotoLibraryAddUsageDescription", PhotoAddUsageDescription);

            // Remove the default read+write access (we only need write access).
            plist.root.values.Remove("NSPhotoLibraryUsageDescription");

            plist.WriteToFile(plistFilePath);
        }

        //[MenuItem("Tools/Melcher Studios/Test Move Out")]
        //static void TestMoveOut()
        //{
        //    MoveInactiveAppResources(
        //        from: s_ResourcesPath,
        //        to: s_InactiveResourcesPath
        //    );
        //}

        //[MenuItem("Tools/Melcher Studios/Test Move In")]
        //static void TestMoveIn()
        //{
        //    MoveInactiveAppResources(
        //        from: s_InactiveResourcesPath,
        //        to: s_ResourcesPath
        //    );
        //}

        /// <summary>
        ///
        /// </summary>
        /// <param name="from">
        ///     Project-relative path where the assets currently reside.
        /// </param>
        /// <param name="to">
        ///     Project-relative path where the assets will be moved.
        /// </param>
        static protected void MoveInactiveAppResources(string from, string to)
        {
            Directory.CreateDirectory(to);

            // Discover the names of available app options. This will be used
            // to determine which folders need to be moved.
            string[] appOptionGuids =
                AssetDatabase.FindAssets($"t:{nameof(AppOptionsSO)}");

            string[] appOptionNames =
                appOptionGuids
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Select(
                        path =>
                            AssetDatabase.LoadAssetAtPath<AppOptionsSO>(path).fileSystemName
                    )
                    .ToArray();

            string activeAppResourcesName =
                ProFitGlobals.appProfile.GetAppResourcesName();

            DirectoryInfo fromDirectoryInfo = new (from);

            foreach (DirectoryInfo subdirectoryInfo in fromDirectoryInfo.EnumerateDirectories())
            {
                string subdirectoryName = subdirectoryInfo.Name;

                if (!appOptionNames.Any(appOptionName => subdirectoryName.StartsWith(appOptionName)))
                {
                    // Ignore folders that don't correlate with an app profile.
                    continue;
                }

                if (subdirectoryName == activeAppResourcesName)
                {
                    // Ignore the folder that represents the active profile.
                    continue;
                }

                string subdirectorySourcePath =
                    MelcherTransformFilePath.From(subdirectoryInfo.FullName)
                        .To(MelcherPredefinedFilePath.project);

                string subdirectoryDestinationPath =
                    Path.Combine(to, subdirectoryInfo.Name);

                AssetDatabase.MoveAsset(
                    subdirectorySourcePath,
                    subdirectoryDestinationPath
                );
            }
        }

        private const string s_ResourcesPath = "Assets/AssetsMelcher/Resources";
        private const string s_InactiveResourcesPath = "Assets/AssetsMelcher/InactiveResources";

    }   // class ProFitBuildProcess

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-11-07
/// ===========================================================================
