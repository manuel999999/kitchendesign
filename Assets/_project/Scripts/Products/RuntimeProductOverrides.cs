namespace KitchenDesign.Products
{
    using Melcher;
    using KitchenDesign.Brands;
    using KitchenDesign.BuildingRules;
    using KitchenDesign.CatalogData;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Serializables;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Interfaces;
    using UnityEngine;

    public static class RuntimeProductOverrides
    {
        public static void RebuildRuntimeProducts(
            CatalogDataStatus       catalogData,
            ProductVisualDatabaseSO productVisualDatabase,
            IAppRules               appRules,
            KitchenBuildingMethod   newBuildingMethod)
        {
            catalogData.ClearRuntimeProducts();

            foreach (ProductInfo sourceProductInfo in catalogData.SourceProducts)
            {
                ProductInfo runtimeProductInfo = new (sourceProductInfo);

                catalogData.AddRuntimeProduct(runtimeProductInfo);

                ProductVisualSO sourceProductVisual =
                    ProductVisualLookups.FindById(
                        productVisualDatabase.sourceEntries,
                        runtimeProductInfo.visualId
                    );

                if (!sourceProductVisual)
                {
                    continue;
                }

                ProductVisualSO runtimeProductVisual =
                    productVisualDatabase.FindRuntimeVisualByID(
                        runtimeProductInfo.visualId
                    );

                if (!runtimeProductVisual)
                {
                    MelcherLog.LogError("Did not find runtime visual for " +  runtimeProductInfo.visualId);
                    continue;
                }

                if (appRules.AppliesRuntimeProductOverrides)
                {
                    ModifyProductInfoForAppRules(
                        appRules,
                        runtimeProductInfo,
                        runtimeProductVisual.info
                    );
                }
                else if (!new BuildingMethodRules(newBuildingMethod).UsesBrandCatalog)
                {
                    runtimeProductVisual.info.initialInsetFromEdgeInMeters =
                        sourceProductVisual.info.initialInsetFromEdgeInMeters;
                }
            }

            MelcherLog.Assert(
                catalogData.SourceProducts.Count == catalogData.RuntimeProducts.Count,
                $"Failed to create an equal number of runtime product info as there are source product info. (Source={catalogData.SourceProducts.Count} vs Runtime={catalogData.RuntimeProducts.Count}). " +
                $"These must be exactly one runtime entry for each source entry."
            );
        }

        public static void ApplySmokerCutOutHeight(
            CatalogDataStatus                   catalogData,
            ProductVisualDatabaseSO             productVisualDatabase,
            IAppRules                           appRules,
            KitchenBuildingMethod               selectedBuildingMethod,
            AssetAddress<ProductVisualContent>  address,
            LoadedProductContent                newlyLoadedProductVisualContent)
        {
            if (!new BuildingMethodRules(selectedBuildingMethod).UsesBrandCatalog && !appRules.AdjustsSmokerCutOutHeight)
            {
                return;
            }

            ProductVisualSO runtimeProductVisual = null;

            foreach (ProductVisualSO i in productVisualDatabase.runtimeEntries)
            {
                if (i.contentAddress == address)
                {
                    runtimeProductVisual = i;
                    break;
                }
            }

            foreach (ProductInfo runtimeProductInfo in catalogData.RuntimeProducts)
            {
                if (runtimeProductInfo.visualId == runtimeProductVisual.info.id)
                {
                    ModifySmokerContentCutOutHeight(
                        runtimeProductInfo.sourceProductInfo,
                        runtimeProductVisual.sourceProductVisual.info,
                        newlyLoadedProductVisualContent.SourceContentPrefab,
                        runtimeProductInfo,
                        runtimeProductVisual.info,
                        newlyLoadedProductVisualContent.RuntimeContentPrefab,
                        14.0f * Units.Inches2Meters
                    );
                }
            }
        }

        public static void ApplyBrandCatalogPadding(
            CatalogDataStatus       catalogData,
            ProductVisualDatabaseSO productVisualDatabase,
            BrandCatalogSO            selectedBrandCatalog)
        {
            foreach (ProductInfo runtimeProductInfo in catalogData.RuntimeProducts)
            {
                if (string.IsNullOrEmpty(runtimeProductInfo.visualId))
                {
                    continue;
                }

                ProductVisualSO runtimeProductVisual =
                    productVisualDatabase.FindRuntimeVisualByID(
                        runtimeProductInfo.visualId
                    );

                if (!runtimeProductVisual)
                {
                    MelcherLog.LogWarningEditorOnly($"Visual ID not found in the product visual database: {runtimeProductInfo.visualId}");
                    continue;
                }

                float paddingInMeters = runtimeProductInfo.sourceProductInfo.productPaddingInMeters.left;

                bool isHeatedProduct =
                    runtimeProductVisual.info.runtimePrefabCategory is
                        ProductCategorySpecific.Grill or
                        ProductCategorySpecific.GrillWithInsulatedJacket or
                        ProductCategorySpecific.Cookware or
                        ProductCategorySpecific.Smoker;

                if (selectedBrandCatalog)
                {
                    if (isHeatedProduct ||
                        runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Fridge)
                    {
                        paddingInMeters = 0.0f;
                    }

                    if (selectedBrandCatalog.ruleset == BrandCatalogRuleset.Challenger)
                    {
                        if (runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Fridge)
                        {
                            paddingInMeters = 0.25f * Units.Inches2Meters;
                        }
                        else if (runtimeProductVisual.info.runtimePrefabCategory != ProductCategorySpecific.Smoker)
                        {
                            paddingInMeters = BrandRules.ChallengerOccupantMinimumHorizontalPaddingInMeters * 0.5f;
                        }
                    }

                    if (runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Fridge)
                    {
                        const float MinimumPaddingInMeters = (1 / 128.0f) * Units.Inches2Meters;
                        paddingInMeters = MinimumPaddingInMeters;
                    }

                    if (runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Shelf)
                    {
                        paddingInMeters = 0.0f;
                    }

                    bool isHeatedOrBurnerProduct =
                        runtimeProductVisual.info.runtimePrefabCategory is
                            ProductCategorySpecific.Grill or
                            ProductCategorySpecific.GrillWithInsulatedJacket or
                            ProductCategorySpecific.Cookware or
                            ProductCategorySpecific.Burner or
                            ProductCategorySpecific.BurnerWithInsulatedJacket or
                            ProductCategorySpecific.Smoker;

                    if (isHeatedOrBurnerProduct)
                    {
                        float? brandPaddingInMeters =
                            selectedBrandCatalog.rules.HeatedOrBurnerProductSidePaddingInMeters(
                                isSmoker: runtimeProductVisual.info.runtimePrefabCategory == ProductCategorySpecific.Smoker
                            );

                        if (brandPaddingInMeters.HasValue)
                        {
                            paddingInMeters = brandPaddingInMeters.Value;
                        }
                    }
                }

                runtimeProductInfo.productPaddingInMeters.left = paddingInMeters;
                runtimeProductInfo.productPaddingInMeters.right = paddingInMeters;
            }
        }

        private static void ModifyProductInfoForAppRules(
            IAppRules           appRules,
            ProductInfo         runtimeProductInfo,
            ProductVisualInfoSO runtimeVisualInfo)
        {
            float? highBarTopRiserDepthInMeters = appRules.HighBarTopRiserDepthInMeters;

            if (highBarTopRiserDepthInMeters.HasValue &&
                runtimeProductInfo.sku == "Backing:BarTop")
            {
                Vector3 productSizeInMeters =
                    runtimeProductInfo.productSizeInMeters;

                productSizeInMeters.z = highBarTopRiserDepthInMeters.Value;

                runtimeProductInfo.productSizeInMeters = productSizeInMeters;
            }

            if (!runtimeVisualInfo)
            {
                return;
            }

            float? fridgeHorizontalPaddingInMeters = appRules.FridgeHorizontalPaddingInMeters;

            if (fridgeHorizontalPaddingInMeters.HasValue &&
                runtimeVisualInfo.runtimePrefabCategory == ProductCategorySpecific.Fridge)
            {
                runtimeProductInfo.productPaddingInMeters.left = fridgeHorizontalPaddingInMeters.Value;
                runtimeProductInfo.productPaddingInMeters.right = fridgeHorizontalPaddingInMeters.Value;
            }

            float? bottomAnchoredProductInsetInMeters = appRules.BottomAnchoredProductInsetInMeters;

            if (bottomAnchoredProductInsetInMeters.HasValue &&
                runtimeVisualInfo.anchor.IsAtBottom() &&
                runtimeVisualInfo.initialInsetFromEdgeInMeters > 0.0f)
            {
                runtimeVisualInfo.initialInsetFromEdgeInMeters = bottomAnchoredProductInsetInMeters.Value;
            }
        }

        private static void ModifySmokerContentCutOutHeight(
            ProductInfo         sourceProductInfo,
            ProductVisualInfoSO sourceVisualInfo,
            ProductVisualContent sourceVisualContent,
            ProductInfo         runtimeProductInfo,
            ProductVisualInfoSO runtimeVisualInfo,
            ProductVisualContent runtimeVisualContent,
            float               desiredCutOutHeightInMeters)
        {
            if (runtimeVisualInfo.runtimePrefabCategory != ProductCategorySpecific.Smoker)
            {
                return;
            }

            Vector3 adjustmentOffsetInMeters = Vector3.zero;

            float cutOutHeightInMeters =
                sourceVisualContent.modelCutOutExtentMax.localPosition.y -
                sourceVisualContent.modelCutOutExtentMin.localPosition.y;

            if (runtimeVisualInfo.anchor == ProductAnchor.OnFrontAtTop &&
                runtimeVisualInfo.initialInsetFromEdgeInMeters <= 0.0f)
            {
                adjustmentOffsetInMeters =
                    Vector3.up * (cutOutHeightInMeters - desiredCutOutHeightInMeters);

                cutOutHeightInMeters = desiredCutOutHeightInMeters;
            }

            runtimeProductInfo.cutOutSizeInMeters.y = cutOutHeightInMeters;

            runtimeVisualContent.modelCutOutExtentMin.localPosition =
                sourceVisualContent.modelCutOutExtentMin.localPosition + adjustmentOffsetInMeters;

            int modelCount = sourceVisualContent.scaleOrigin.childCount;

            for (int i = 0; i < modelCount; ++i)
            {
                Transform sourceModel = sourceVisualContent.scaleOrigin.GetChild(i);
                Transform runtimeModel = runtimeVisualContent.scaleOrigin.GetChild(i);

                runtimeModel.localPosition = sourceModel.localPosition + adjustmentOffsetInMeters;
            }
        }
    }
}
