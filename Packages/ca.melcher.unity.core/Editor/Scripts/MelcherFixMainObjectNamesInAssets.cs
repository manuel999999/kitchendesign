namespace Melcher
{
    using System.IO;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.PackageManager;

    using PackageInfo = UnityEditor.PackageManager.PackageInfo;

    /// <summary>
    ///     Adds a menu command to find and fix all assets that have mismatched
    ///     main object names versus file names. I.e.:
    ///     <br /><br />
    ///
    ///         "Main Object Name '{0}' does not match filename '{1}'".
    ///         <br /><br />
    ///
    ///     This warning can appear when assets are renamed outside of Unity.
    ///     <br /><br />
    ///
    ///     Read-only assets are ignored. Supports Undo. Has a dry-run mode to
    ///     list the assets it would have modified in a full run.
    ///     <br /><br />
    ///
    ///     This is essentially a way to do what Unity already does <see
    ///     href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/Editor.cs#L1176">
    ///     here</see> but in batch.
    /// </summary>
    public static class MelcherFixMainObjectNamesInAssets
    {
        [MenuItem(MelcherEditorConst.Menu.ToolsAssetsPrefix + "Fix main object names in assets...")]
        public static void FindAndFixAssets()
        {
            string dialogTitle =
                ObjectNames.NicifyVariableName(nameof(MelcherFixMainObjectNamesInAssets));

            // Prompt the user to do a dry run or the real thing. A dry run
            // allows the user to verify nothing unwanted is affected.
            int dialogResult =
                EditorUtility.DisplayDialogComplex(
                    dialogTitle,
                    message: "Find and fix assets with incorrect main object " +
                        "names?\n\nThis loads every asset in the project and " +
                        "can be very slow in large projects.",
                    ok: "Dry Run",
                    cancel: "Cancel",
                    alt: "Fix All"
                );

            const int DialogResultOK = 0;
            const int DialogResultCancel = 1;

            if (dialogResult == DialogResultCancel)
            {
                return;
            }

            bool isDryRun = dialogResult == DialogResultOK;
            if (isDryRun)
            {
                dialogTitle += " (Dry Run)";
            }

            // Find all assets
            string[] allGuidsInProject = AssetDatabase.FindAssets("");

            // List of types that don't complain in the inspector when their
            // names are mismatched.
            System.Type[] assetTypeBlacklist = new[]
            {
                typeof(Shader),         // Shaders appear with mismatched names regularly. Even in official packages.
                typeof(DefaultAsset),   // Folders with periods in them will have different names. This is OK.
            };

            bool didCancel = false;

            try
            {
                int assetCount = allGuidsInProject.Length;
                MelcherLog.Log($"Starting scan of {assetCount} assets...");
                for (int i = 0; i < assetCount; i++)
                {
                    string assetGuid = allGuidsInProject[i];
                    string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        MelcherLog.LogWarning($"Skipping asset with GUID {assetGuid} because it has no asset path.");
                        continue;
                    }

                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    if (!asset)
                    {
                        MelcherLog.LogWarning($"Skipping asset because it could not be loaded: {assetPath}");
                        continue;
                    }

                    string mainObjectName = asset.name;
                    string expectedMainObjectName = Path.GetFileNameWithoutExtension(assetPath);

                    didCancel =
                        EditorUtility.DisplayCancelableProgressBar(
                            dialogTitle + $" - {i + 1} of {assetCount}",
                            info: assetPath,
                            progress: (float)(i + 1) / assetCount
                        );

                    if (didCancel)
                    {
                        // The user requested an early exit
                        break;
                    }

                    int blacklistedTypeIndex =
                        System.Array.IndexOf(assetTypeBlacklist, asset.GetType());

                    if (blacklistedTypeIndex >= 0)
                    {
                        // Skip assets of blacklisted types. See array definition
                        // above.
                        continue;
                    }

                    if (mainObjectName == expectedMainObjectName)
                    {
                        // Asset is already correct
                        continue;
                    }

                    PackageInfo packageInfo = PackageInfo.FindForAssetPath(assetPath);
                    if (packageInfo != null &&
                        packageInfo.source != PackageSource.Embedded &&
                        packageInfo.source != PackageSource.Local)
                    {
                        // Asset is read-only
                        continue;
                    }

                    if (isDryRun)
                    {
                        MelcherLog.Log("Would fix: " + assetPath, asset);
                    }
                    else
                    {
                        Undo.RecordObject(asset, dialogTitle);
                        using SerializedObject serializedAsset = new SerializedObject(asset);
                        asset.name = expectedMainObjectName;
                        MelcherLog.Log("Fixed: " + assetPath, asset);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                MelcherLog.Log(didCancel ? "Canceled." : "Finished.");
            }
        }

    }   // class FixAssetMainObjectNames

}   // namespace Melcher


/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-01-24
/// ===========================================================================
