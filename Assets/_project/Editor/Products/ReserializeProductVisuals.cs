namespace KitchenDesign.ProductsEditor
{
    using Melcher;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    ///     This class adds a menu item to dirty and re-save all <see
    ///     cref="ProductVisualContent"/> assets in the project.
    ///     <br /><br />
    ///
    ///     The result of this is all product visuals will be updated to their
    ///     latest serialization layout. This then allows old <see
    ///     cref="UnityEngine.Serialization.FormerlySerializedAsAttribute"/>
    ///     to be removed.
    /// </summary>
    public static class ReserializeProductVisuals
    {
        [MenuItem("Tools/Melcher Studios/Pro-Fit/Reserialize Product Visuals...")]
        public static void ReserializeAll()
        {
            bool willReserialize =
                EditorUtility.DisplayDialog(
                    title: "Reserialize All Product Visuals?",
                    message: "This is intended to refresh assets so they can " +
                    "have any FormerlySerializedAs attribute removed.\n" +
                    "\n" +
                    "It is probably best to do this as a separate commit to " +
                    "ensure no important data is lost.",
                    ok: "Reserialize Product Visuals",
                    cancel: "Cancel"
                );

            if (!willReserialize)
            {
                return;
            }

            string[] productVisualsGuids =
                AssetDatabase.FindAssets("t:prefab", new[] { "Assets/AssetsMelcher/ProductVisuals" } );

            // Force all product visuals to be dirty so they re-serialize.

            foreach (string guid in productVisualsGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                ProductVisualContent productVisual =
                    prefab.GetComponent<ProductVisualContent>();

                EditorUtility.SetDirty(productVisual);
            }

            AssetDatabase.SaveAssets();

        }
    }   // class ReserializeProductVisuals

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-17
/// ===========================================================================
