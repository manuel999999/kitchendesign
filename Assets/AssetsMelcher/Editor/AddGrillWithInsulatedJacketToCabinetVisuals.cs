namespace Melcher
{
    using System.Linq;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// </summary>
    public static class AddGrillWithInsulatedJacketToCabinetVisuals
    {
        [MenuItem("Tools/Melcher Studios/Pro-Fit/Add grill with insulated jacket to cabinet visuals...")]
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

                if (!prefab || !prefab.TryGetComponent(out ProductVisualContent productVisualContent))
                {
                    continue;
                }

                ProductVisualInfo productVisualInfo = productVisualContent.editorOnlyVisualInfo;

                if (!productVisualInfo ||
                    productVisualInfo.runtimePrefabCategory != ProductCategorySpecific.CabinetSection ||
                    !productVisualInfo.IsCategoryAllowedInSection(ProductCategorySpecific.Grill) ||
                    productVisualInfo.IsCategoryAllowedInSection(ProductCategorySpecific.GrillWithInsulatedJacket))
                {
                    continue;
                }

                productVisualInfo.productCategoriesAllowedInSection =
                    productVisualInfo.productCategoriesAllowedInSection.Append(new (ProductCategorySpecific.GrillWithInsulatedJacket)).ToArray();

                EditorUtility.SetDirty(productVisualContent);
            }

            AssetDatabase.SaveAssets();

        }
    }   // class AddGrillWithInsulatedJacketToCabinetVisuals

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2025-05-21
/// ===========================================================================
