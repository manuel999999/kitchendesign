namespace Melcher
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    ///     Adds a menu command that forces Unity to re-serialize all selected
    ///     assets. This is a useful for a few use cases such as fixing assets
    ///     that did not serialize correctly or to clean up old <see
    ///     cref="UnityEngine.Serialization.FormerlySerializedAsAttribute"/>
    ///     uses.
    /// </summary>
    public static class MelcherReserializeSelectedAssetsMenu
    {
        // ====================================================================
        // Static functions
        // ====================================================================

        public static void ReserializeAssets(IEnumerable<string> assetGuids)
        {
            foreach (string guid in assetGuids)
            {
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem(MelcherEditorConst.Menu.ToolsAssetsPrefix + "Reserialize selected assets...")]
        private static void ReserializeSelectedAssets()
        {
            string[] selectedAssetGuids = Selection.assetGUIDs;

            const string DialogTitle = "Melcher - Reserialize selected assets";

            const string DialogHelp =
                "This command helps fix assets with serialization issues or " +
                "clean up old FormerlySerializedAs attribute uses in " +
                "scripts. If executed correctly (i.e., with every asset " +
                "that uses the script selected), it is then safe to remove " +
                "the FormerlySerializedAs attributes from the script " +
                "without data loss.\n" +
                "\n" +
                "Note: This feature does not attempt to reserialize " +
                "files in folders. Select the assets explicitly.";

            if (selectedAssetGuids.Length <= 0)
            {
                EditorUtility.DisplayDialog(
                    title: DialogTitle,
                    message: "No assets selected. Select one or more assets " +
                    "in the project window and try again.\n" +
                    "\n" +
                    DialogHelp,
                    ok: "Close"
                );

                return;
            }

            bool willReserialize =
                EditorUtility.DisplayDialog(
                    title: DialogTitle,
                    message: "Force Unity to reserialize the selected " +
                    "assets?\n" +
                    "\n" +
                    DialogHelp,
                    ok: $"Reserialize {selectedAssetGuids.Length} Assets",
                    cancel: "Cancel"
                );

            if (!willReserialize)
            {
                return;
            }

            ReserializeAssets(selectedAssetGuids);

            MelcherLog.Log($"Reserialized {selectedAssetGuids.Length} assets.");
        }

    }   // static class MelcherReserializeSelectedAssetsMenu

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-02-16
/// ===========================================================================
