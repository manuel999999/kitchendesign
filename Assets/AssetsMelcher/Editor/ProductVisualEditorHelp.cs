namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using UnityEditor;
    using UnityEngine;

    public static class ProductVisualEditorHelp
    {
        [MenuItem("Tools/Melcher Studios/Pro-Fit/Apply product visual cabinet categories &v")]
        public static void Execute()
        {
            if (!Selection.activeObject ||
                !(Selection.activeObject is GameObject prefab) ||
                !(prefab.TryGetComponent(out ProductVisualContent content)))
            {
                return;
            }

            content.editorOnlyVisualInfo.productCategoriesAllowedInSection =
                new MelcherStableEnum<ProductCategorySpecific>[]
                {
                    new (ProductCategorySpecific.Grill),
                    new (ProductCategorySpecific.GrillWithInsulatedJacket),
                    new (ProductCategorySpecific.Burner),
                    new (ProductCategorySpecific.BurnerWithInsulatedJacket),
                    new (ProductCategorySpecific.Cookware)
                };

            EditorUtility.SetDirty(content);
        }
    }   // class ProductVisualEditorHelp

}   // namespace KitchenDesign

