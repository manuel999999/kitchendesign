namespace Melcher
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.Linq;

    [CustomEditor(typeof(ProductVisualDatabase))]
    public class ProductVisualDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open In Product Visual Database Manager"))
            {
                ProductVisualDatabaseManager.ShowDatabase(target as ProductVisualDatabase);
            }

            if (GUILayout.Button("Open In Product Visual Database States Manager"))
            {
                ProductVisualDatabaseStatesManager.ShowDatabase(target as ProductVisualDatabase);
            }

            if (GUILayout.Button("Open In Product Visual Database Sets Manager"))
            {
                ProductVisualDatabaseSetsManager.ShowDatabase(target as ProductVisualDatabase);
            }

            if (GUILayout.Button("Find and Fix Errors..."))
            {
                int dialogResult =
                    EditorUtility.DisplayDialogComplex(
                        title: "Error Check Product Visual Database",
                        message: "Find errors in the product visual database" +
                            "that can interfere with app functionality.\n" +
                            "\n" +
                            "Optionally, you may choose to automatically " +
                            "fix the found errors. (By default, the errors " +
                            "are only logged to the console.)",
                        ok: "Find Errors",
                        alt: "Find and Fix Errors",
                        cancel: "Cancel"
                    );

                if (dialogResult != MelcherEditorConst.DialogResult.Cancel)
                {
                    (target as ProductVisualDatabase).ErrorCheck(allowFixUp: dialogResult == MelcherEditorConst.DialogResult.Alt);
                }
            }

            if (GUILayout.Button("Find Unused Product Visuals..."))
            {
                bool willFindUnusedVisuals =
                    EditorUtility.DisplayDialog(
                        title: "Find Unused Product Visuals",
                        message: "Find unused Product Visuals (prefab " +
                            "assets) in the project. This is intended to " +
                            "ensure products were not lost during a merge " +
                            "conflict or anything that causes null entries " +
                            "in the database.\n" +
                            "\n" +
                            "This does not modify the database. All unused " +
                            "visuals are logged to the console. The search " +
                            "is limited to the ProductVisuals folder.",
                        ok: "Find Unused"
                    );

                if (!willFindUnusedVisuals)
                {
                    return;
                }

                ProductVisualDatabase productVisualDatabase =
                    target as ProductVisualDatabase;

                string[] prefabGuids =
                    AssetDatabase.FindAssets(
                        filter: "t:Prefab",
                        searchInFolders: new [] {"Assets/AssetsMelcher/ProductVisuals"}
                    );

                string[] prefabAssetPaths =
                    prefabGuids.Select(x => AssetDatabase.GUIDToAssetPath(x))
                        .ToArray();

                List<string> unusedPrefabAssetPaths = new();

                foreach (string prefabAssetPath in prefabAssetPaths)
                {
                    bool isAssetInProductVisualDatabase =
                        productVisualDatabase.sourceEntries.Any(
                            x => AssetDatabase.GetAssetPath(x) == prefabAssetPath
                        );

                    if (isAssetInProductVisualDatabase)
                    {
                        continue;
                    }

                    unusedPrefabAssetPaths.Add(prefabAssetPath);
                }

                MelcherLog.Log($"Found {unusedPrefabAssetPaths.Count} unused product visuals:", productVisualDatabase);
                foreach (string assetPath in unusedPrefabAssetPaths)
                {
                    MelcherLog.Log($"  unused visual: {assetPath}", AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
                }
            }

            base.OnInspectorGUI();
        }

    }   // class ProductVisualDatabaseEditor

}   // namespace Melcher

/// ============================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-05-19
/// ===========================================================================