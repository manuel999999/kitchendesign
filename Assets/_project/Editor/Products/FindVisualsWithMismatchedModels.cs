namespace KitchenDesign.ProductsEditor
{
    using Melcher;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using KitchenDesign.Products;

    /// <summary>
    ///     This identifies <see cref="ProductVisualContent"/>s whose model prefab
    ///     does not match the model that is actually instantiated in its
    ///     hierarchy. There is likely problems when the model mismatches
    ///     (e.g., when restoring the prefabs materials) and this also eats
    ///     into our limited memory on iOS because we're loading two models
    ///     rather than one.
    /// </summary>
    public static class FindVisualsWithMismatchedModels
    {
        [MenuItem("Tools/Melcher Studios/Pro-Fit/Find visuals with mismatched models")]
        public static void FindMismatches()
        {
            string[] productVisualsGuids =
                AssetDatabase.FindAssets(
                    filter: "t:prefab",
                    searchInFolders: new[] { "Assets/AssetsMelcher/ProductVisuals" }
                );

            foreach (string guid in productVisualsGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (!prefab)
                {
                    continue;
                }

                ProductVisualContent productVisualContent =
                    prefab.GetComponent<ProductVisualContent>();

                if (!productVisualContent || !productVisualContent.productModelPrefab)
                {
                    continue;
                }

                // Check if we're using the same model.

                string proclaimedPrefab =
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        productVisualContent.productModelPrefab
                    );

                string actualPrefab =
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        productVisualContent.scaleOrigin.GetChild(0).gameObject
                    );

                if (proclaimedPrefab != actualPrefab)
                {
                    MelcherLog.Log($"ProductVisual model mismatch: {assetPath}\n\n{proclaimedPrefab} (proclaimed)\n\n{actualPrefab} (actual)", prefab);
                }
            }
        }
    }   // class AddGrillWithInsulatedJacketToCabinetVisuals

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2025-05-21
/// ===========================================================================
