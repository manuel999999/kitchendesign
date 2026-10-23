using Cysharp.Threading.Tasks;
using KitchenDesign.Geometry;
using KitchenDesign.KitchenModel;
using KitchenDesign.Products;
using KitchenDesign.Scriptables;
using KitchenDesign.Serializables;
using KitchenDesign.Server;
using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using Melcher;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using JsonDict = System.Collections.Generic.IDictionary<string, object>;
using JsonList = System.Collections.Generic.IList<object>;

namespace KitchenDesign.Save
{
    public sealed class KitchenSaveReader
    {
        private readonly Kitchen _kitchen;
        private readonly KitchenSelections _selections;
        private readonly ProductVisualDatabaseSO _productVisualDatabase;
        private readonly CabinetLayout[] _layoutPrefabs;
        private readonly Sprite[] _cabinetBackingSprites;
        private readonly CabinetBacking _backsplashPrefab;
        private readonly CabinetBacking _highBarTopPrefab;
        private readonly CabinetBacking _lowBarTopPrefab;

        public KitchenSaveReader(
            Kitchen kitchen,
            KitchenSelections selections,
            ProductVisualDatabaseSO productVisualDatabase,
            CabinetLayout[] layoutPrefabs,
            Sprite[] cabinetBackingSprites,
            CabinetBacking backsplashPrefab,
            CabinetBacking highBarTopPrefab,
            CabinetBacking lowBarTopPrefab)
        {
            this._kitchen = kitchen;
            this._selections = selections;
            this._productVisualDatabase = productVisualDatabase;
            this._layoutPrefabs = layoutPrefabs;
            this._cabinetBackingSprites = cabinetBackingSprites;
            this._backsplashPrefab = backsplashPrefab;
            this._highBarTopPrefab = highBarTopPrefab;
            this._lowBarTopPrefab = lowBarTopPrefab;
        }

        public async UniTask<bool> LoadFromJson(JsonDict saveJson)
        {
            MelcherJsonObjectParser parser = new MelcherJsonObjectParser(saveJson);

            parser
                .GetValue("cabinetFinish", out string cabinetFinishProductSku)
                .GetValue("counterTopFinish", out string countertopFinishProductSku)
                .GetValue("deckFinish", out string deckFinishProductSku)
                .GetValue("paverFinish", out string paverFinishProductSku)
                .GetValue("kitchen_layout_style", out string kitchenLayoutStyleName)
                .GetValue("kitchen_building_method", out string kitchenBuildingMethodName, defaultValue: "")
                .GetValue("brand_catalog", out string brandCatalogName, defaultValue: null)
                .GetValue("brand_catalog_id", out int brandCatalogId, defaultValue: -1)
                .GetValue("door_style_name", out this._selections.doorStyleName, defaultValue: null)
                .GetValue("door_handle_style_name", out this._selections.doorHandleStyleName, defaultValue: "")
                .GetValue("end_panel_width_left_in_meters", out this._selections.endPanelWidthInMetersLeft, defaultValue: 0f)
                .GetValue("end_panel_width_right_in_meters", out this._selections.endPanelWidthInMetersRight, defaultValue: 0f)
                .GetValue("back_panel_depth_in_meters", out this._selections.backPanelDepthInMeters, defaultValue: 0f)
                .GetValue("countertop_type", out string countertopTypeName, defaultValue: "")
                .GetValue("toe_kick_type", out string toeKickType, defaultValue: "")
                .GetValue("base_cabinet_depth_in_meters", out float baseCabinetDepthInMeters)
                .GetValue("countertop_thickness_in_meters", out float countertopThicknessInMeters)
                .GetValue("analytics_key", out this._selections.analyticsKey, defaultValue: null)
                .GetList("cabinets", out JsonList cabinetDataList)
                .GetList("cabinet_spans", out JsonList cabinetSpanList, errorIfNotFound: false)
                .GetList("extra_features", out JsonList extraFeaturesList, errorIfNotFound: false);

            if (parser.hasErrors)
            {
                MelcherLog.LogError(Server.ServerJson.CombineParserErrorsIntoException(parser).Message, this._kitchen);
                return false;
            }

            EnumInfo<KitchenBuildingMethod>.TryParse(
                kitchenBuildingMethodName,
                out this._selections.buildingMethod,
                defaultValue: KitchenBuildingMethod.Standard
            );

            if (this._selections.buildingMethod == 0)
            {
                this._selections.buildingMethod = KitchenBuildingMethod.Standard;
            }

            float countertopOverhangInMeters = this._kitchen.buildingRules.DefaultCountertopOverhangInMeters;

            this._selections.brandCatalog = null;

            bool isCatalogMode = this._kitchen.buildingRules.UsesBrandCatalog;
            bool hasCatalogName = !string.IsNullOrEmpty(brandCatalogName);
            if (isCatalogMode != hasCatalogName)
            {
                MelcherLog.LogError($"Invalid save data: isCatalogMode={isCatalogMode}, hasCatalogName={hasCatalogName}", this._kitchen);
                return false;
            }

            if (isCatalogMode)
            {
                if (brandCatalogId >= 0)
                {
                    foreach (BrandCatalogSO brandCatalog in ProFitGlobals.runtimeBrandCatalogs)
                    {
                        if (brandCatalog.catalogId == brandCatalogId)
                        {
                            this._selections.brandCatalog = brandCatalog;
                            EventBridge.Designer.refreshCabinetLoadingUiImage();
                            break;
                        }
                    }
                }

                if (!this._selections.brandCatalog)
                {
                    foreach (BrandCatalogSO brandCatalog in ProFitGlobals.runtimeBrandCatalogs)
                    {
                        if (brandCatalog.catalogName == brandCatalogName)
                        {
                            this._selections.brandCatalog = brandCatalog;
                            break;
                        }
                    }
                }

                if (!this._selections.brandCatalog)
                {
                    MelcherLog.LogError($"Could not find brand catalog with " +
                        $"name: {MelcherFormat.ConsoleString(brandCatalogName)} " +
                        $"and ID: {MelcherFormat.ConsoleNumericValue(brandCatalogId)}",
                        context: this._kitchen
                    );
                    return false;
                }
            }

            this._selections.countertopOverhangInMeters =
                new CuboidPadding()
                {
                    left = countertopOverhangInMeters,
                    right = countertopOverhangInMeters,
                    towardMinZ = countertopOverhangInMeters,
                    towardMaxZ = countertopOverhangInMeters,
                    top = 0.0f,
                    bottom = 0.0f
                };

            EventBridge.Designer.onBuildingMethodChanged?.Invoke(this._selections.buildingMethod);

            await EventBridge.Designer.onBrandCatalogChanged.InvokeIfNonEmpty(this._selections.brandCatalog);

            this._selections.cabinetFinish = ProFitGlobals.catalogData.FindProductInfoBySku(cabinetFinishProductSku, findCabinetSection: false);
            this._selections.countertopFinish = ProFitGlobals.catalogData.FindProductInfoBySku(countertopFinishProductSku, findCabinetSection: false);

            string flooringId = !string.IsNullOrEmpty(deckFinishProductSku) ? deckFinishProductSku : paverFinishProductSku;
            this._selections.flooringFinish = ProFitGlobals.catalogData.FindProductInfoBySku(flooringId, findCabinetSection: false);

            this._selections.baseCabinetDepthInMeters = baseCabinetDepthInMeters;
            this._selections.countertopThicknessInMeters = countertopThicknessInMeters;

            CabinetLayout cabinetLayoutPrefab =
                System.Array.Find(
                    this._layoutPrefabs,
                    layout => layout.style.ToString() == kitchenLayoutStyleName
                );

            if (cabinetLayoutPrefab == null)
            {
                throw new System.InvalidOperationException($"Couldn't find cabinet layout prefab for layout {kitchenLayoutStyleName}");
            }

            this._selections.layoutPrefab = cabinetLayoutPrefab;
            await this._kitchen.CreateCabinetLayoutFromShape(this._selections.layoutPrefab);

            EnumInfo<CountertopType>.TryParse(
                countertopTypeName,
                out this._selections.countertopType,
                defaultValue: CountertopType.Default
            );

            EnumInfo<CabinetToeKickType>.TryParse(
                toeKickType,
                out this._selections.toeKickType,
                defaultValue: CabinetToeKickType.None
            );

            bool remakeCabinets =
                this._kitchen.buildingRules.CabinetsAreProducts;

            if (string.IsNullOrEmpty(kitchenBuildingMethodName))
            {
                remakeCabinets = false;
            }

            if (remakeCabinets)
            {
                this._kitchen.layout.DestroyCabinets();
            }

            for (int i = 0; i < cabinetDataList.Count; ++i)
            {
                Utility.MemoryUtility.GarbageCollectOnLowMemoryDevices();

                JsonDict cabinetData = (JsonDict)cabinetDataList[i];
                parser.Reset(cabinetData)
                    .GetValue("size_in_meters_x", out float cabinetSizeInMetersX);

                if (parser.hasErrors)
                {
                    MelcherLog.LogError($"Error reading cabinet index {i}: " + Server.ServerJson.CombineParserErrorsIntoException(parser).Message);
                    continue;
                }

                Cabinet cabinet = null;
                ProductInfo cabinetProductInfo = null;

                Vector3? cabinetMinSizeInMeters = null;
                Vector3? cabinetMaxSizeInMeters = null;
                Vector3? cabinetLocalEulerAngles = null;

                if (remakeCabinets)
                {
                    Vector3 normalizedAnchorToPreviousSection = Vector3.zero;

                    Vector3 minSizeInMeters = Vector3.zero;
                    Vector3 maxSizeInMeters = Vector3.zero;
                    Vector3 localEulerAngles = Vector3.zero;

                    parser
                        .GetValue("cabinet_type", out string cabinetTypeName)
                        .GetAndPushObject("normalized_anchor_to_previous_section")
                            .GetValue("x", out normalizedAnchorToPreviousSection.x)
                            .GetValue("y", out normalizedAnchorToPreviousSection.y)
                            .GetValue("z", out normalizedAnchorToPreviousSection.z)
                        .PopObject()
                        .GetAndPushObject("local_euler_angles")
                            .GetValue("x", out localEulerAngles.x)
                            .GetValue("y", out localEulerAngles.y)
                            .GetValue("z", out localEulerAngles.z)
                        .PopObject()
                        .GetAndPushObject("min_size_in_meters")
                            .GetValue("x", out minSizeInMeters.x)
                            .GetValue("y", out minSizeInMeters.y)
                            .GetValue("z", out minSizeInMeters.z)
                        .PopObject()
                        .GetAndPushObject("max_size_in_meters")
                            .GetValue("x", out maxSizeInMeters.x)
                            .GetValue("y", out maxSizeInMeters.y)
                            .GetValue("z", out maxSizeInMeters.z)
                        .PopObject()
                        .GetValue("backing_sprite", out string cabinetBackingSpriteName)
                        .GetValue("cabinet_server_id", out int cabinetId, defaultValue: 0)
                        .GetValue("cabinet_id", out string cabinetSku, defaultValue: null);

                    if (parser.hasErrors)
                    {
                        MelcherLog.LogError($"Error remaking cabinet index {i}: " + Server.ServerJson.CombineParserErrorsIntoException(parser).Message);
                        continue;
                    }

                    CabinetModelType cabinetModelType = CabinetModelType.Procedural;

                    if (cabinetId != 0 || !string.IsNullOrEmpty(cabinetSku))
                    {
                        if (cabinetId > 0)
                        {
                            cabinetProductInfo =
                                ProFitGlobals.catalogData.FindProductInfoById(
                                    cabinetId,
                                    findCabinetSection: true
                                );
                        }

                        if (cabinetProductInfo == null)
                        {
                            cabinetProductInfo =
                                ProFitGlobals.catalogData.FindProductInfoBySku(
                                    cabinetSku,
                                    findCabinetSection: true
                                );
                        }

                        if (cabinetProductInfo == null)
                        {
                            int? remappedCabinetId =
                                ProFitGlobals.appOptions.remappedProductIdDatabase.Lookup(cabinetSku);

                            if (remappedCabinetId.HasValue)
                            {
                                cabinetProductInfo =
                                    ProFitGlobals.catalogData.FindProductInfoById(
                                        remappedCabinetId.Value,
                                        findCabinetSection: true
                                    );
                            }
                        }

                        if (cabinetProductInfo == null)
                        {
                            MelcherLog.LogError($"Failed to find cabinet info for cabinet with SKU: {cabinetSku}");
                            continue;
                        }

                        if (!string.IsNullOrEmpty(cabinetProductInfo.visualId))
                        {
                            ProductVisualSO cabinetVisual =
                                this._productVisualDatabase.FindRuntimeVisualByID(
                                    cabinetProductInfo.visualId
                                );

                            if (cabinetVisual == null)
                            {
                                MelcherLog.LogError($"Failed to find product visual with ID: {cabinetProductInfo.visualId}");
                                continue;
                            }

                            cabinetModelType = cabinetVisual.info.sectionModelType;
                        }
                    }

                    cabinetMinSizeInMeters = minSizeInMeters;
                    cabinetMaxSizeInMeters = maxSizeInMeters;
                    cabinetLocalEulerAngles = localEulerAngles;

                    if (!System.Enum.TryParse(cabinetTypeName, ignoreCase: true, out CabinetType cabinetType))
                    {
                        if (cabinetTypeName == "CornerColumnRight")
                        {
                            cabinetType = CabinetType.StraightOnRightOfCorner;
                        }
                        else if (cabinetTypeName == "CornerColumnLeft")
                        {
                            cabinetType = CabinetType.StraightOnLeftOfCorner;
                        }
                        else
                        {
                            MelcherLog.LogError($"Error remaking cabinet index {i}: Unknown cabinet type '{cabinetTypeName}'");
                            continue;
                        }
                    }

                    Cabinet cabinetPrefab =
                        this._kitchen.FindCabinetPrefab(
                            cabinetType,
                            cabinetModelType
                        );

                    if (!cabinetPrefab)
                    {
                        MelcherLog.LogError($"Error remaking cabinet index {i}: Cannot find prefab for cabinet type '{cabinetTypeName}'");
                        continue;
                    }

                    cabinet =
                        await this._kitchen.layout.InstantiateCabinet(
                            cabinetPrefab,
                            cabinetProductInfo,
                            kitchen: this._kitchen
                        );

                    cabinet.SetToeKickType(this._selections.toeKickType);

                    cabinet.normalizedAnchorToPreviousSection = normalizedAnchorToPreviousSection;

                    cabinet.backingSprite =
                        System.Array.Find(
                            this._cabinetBackingSprites,
                            sprite => sprite && sprite.name == cabinetBackingSpriteName
                        );
                }
                else
                {
                    cabinet = this._kitchen.layout.GetCabinetAtIndex(i);
                }

                parser.currentObject["exists"] = true;

                if (cabinet.cabinetType == CabinetType.Corner)
                {
                    cabinetSizeInMetersX = baseCabinetDepthInMeters;
                }

                if (cabinetLocalEulerAngles.HasValue)
                {
                    cabinet.GetTransform().localEulerAngles = cabinetLocalEulerAngles.Value;
                }

                Vector3 sizeInMeters = cabinet.sizeInMeters;
                sizeInMeters.x = cabinetSizeInMetersX;
                if (cabinetProductInfo != null)
                {
                    sizeInMeters.y = cabinetProductInfo.productSizeInMeters.y;
                }
                else if (this._kitchen.buildingRules.UsesBrandCatalog &&
                    cabinet.Is(CabinetType.Corner, CabinetModelType.Procedural))
                {
                    sizeInMeters.y = this._selections.brandCatalog.standardCabinetHeightInMeters;
                }

                sizeInMeters.z = this._selections.baseCabinetDepthInMeters;
                cabinet.sizeInMeters = sizeInMeters;

                if (cabinet.cabinetType.IsHeightTransition() &&
                    this._kitchen.buildingRules.UsesBrandCatalog &&
                    this._selections.brandCatalog != null &&
                    this._selections.brandCatalog.cabinetSectionLegStyle)
                {
                    cabinet.cabinet.transform.position +=
                        Vector3.up * this._selections.brandCatalog.cabinetSectionLegStyle.heightInMeters;
                }

                cabinet.SetCountertopThickness(countertopThicknessInMeters);
                cabinet.SetCountertopOverhang(this._selections.countertopOverhangInMeters);

                if (cabinetMinSizeInMeters.HasValue)
                {
                    cabinet.minSizeInMeters = cabinetMinSizeInMeters.Value;
                }

                if (cabinetMaxSizeInMeters.HasValue)
                {
                    cabinet.maxSizeInMeters = cabinetMaxSizeInMeters.Value;
                }
            }

            for (int i = 0; i < cabinetDataList.Count; ++i)
            {
                JsonDict cabinetData = (JsonDict)cabinetDataList[i];

                parser.Reset(cabinetData)
                    .GetValue("index_of_cabinet_being_wedged", out int indexOfCabinetBeingWedged, defaultValue: -1);

                if (indexOfCabinetBeingWedged < 0)
                {
                    continue;
                }

                Cabinet cabinetWedge = this._kitchen.layout.GetCabinetAtIndex(i);

                cabinetWedge.cornerCabinetBeingWedged =
                    this._kitchen.layout.GetCabinetAtIndex(indexOfCabinetBeingWedged);

                Vector3 sizeInMeters = cabinetWedge.sizeInMeters;
                sizeInMeters.z = sizeInMeters.x;
                cabinetWedge.sizeInMeters = sizeInMeters;
            }

            this._kitchen.SetBaseCabinetDepth(this._selections.baseCabinetDepthInMeters);

            if (cabinetSpanList != null)
            {
                for (int i = 0; i < cabinetSpanList.Count; ++i)
                {
                    JsonDict cabinetSpanData = (JsonDict)cabinetSpanList[i];

                    parser
                        .PushObject(i, cabinetSpanData)
                            .GetValue("index", out int cabinetSpanIndex)
                            .GetValue("backing_product_id", out string backingProductSku)
                            .GetValue("depth_in_meters", out float depthInMeters, defaultValue: 0.0f)
                        .PopObject();

                    if (depthInMeters > 0.0f)
                    {
                        CabinetSpan span = this._kitchen.layout.spans[cabinetSpanIndex];
                        span.SetBaseCabinetDepth(depthInMeters);
                    }

                    if (backingProductSku != null)
                    {
                        await LoadBackingFromProductSku(
                            cabinetSpanIndex,
                            backingProductSku
                        );
                    }
                }
            }

            this._selections.extraFeatures.Clear();
            if (extraFeaturesList != null)
            {
                for (int i = 0; i < extraFeaturesList.Count; ++i)
                {
                    if (!(extraFeaturesList[i] is string extraFeatureName))
                    {
                        continue;
                    }

                    if (!System.Enum.TryParse(extraFeatureName, out KitchenExtraFeature extraFeature))
                    {
                        MelcherLog.LogError(
                            $"Could not parse {MelcherFormat.ConsoleTypeName<KitchenExtraFeature>()} from " +
                            $"{MelcherFormat.ConsoleString(extraFeatureName)}"
                        );
                        continue;
                    }

                    this._selections.extraFeatures.Add(extraFeature);
                }
            }

            if (!MelcherCommon.IsAlmostEqual(this._selections.backPanelDepthInMeters, 0.0f, 0.001f))
            {
                this._kitchen.ApplyDeltaToAllCabinetDepths(this._selections.backPanelDepthInMeters);
            }

            this._kitchen.layout.UpdatePositions();
            this._kitchen.layout.UpdateMeshes();

            if (remakeCabinets)
            {
                this._kitchen.RecreateResizeWidgets();
                this._kitchen.UpdateCameraAndFramingGroups();
            }

            Utility.MemoryUtility.GarbageCollectOnLowMemoryDevices();

            for (int i = 0; i < cabinetDataList.Count; ++i)
            {
                JsonDict cabinetData = (JsonDict)cabinetDataList[i];

                Cabinet cabinet = this._kitchen.layout.GetCabinetAtIndex(i);

                await LoadOccupantsFromJson(parser, cabinetData, cabinet, requireOccupants: true);
            }

            Utility.MemoryUtility.GarbageCollectOnLowMemoryDevices();

            (Material cabinetMaterial, Material countertopMaterial) =
                await this._kitchen.GetFinishMaterials(allowHighlightToReturn: false);

            await this._kitchen.layout.SetCabinetMaterials(
                cabinetMaterial,
                countertopMaterial,
                await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
            );

            await this._kitchen.ChangeFinish(this._selections.flooringFinish, ProductCategory.FinishFloor);

            this._kitchen.CenterTheGround();

            if(ProFitGlobals.appRules.ReappliesCountertopThicknessAfterLoad)
            {
                this._kitchen.SetCountertopThickness(this._selections.countertopThicknessInMeters);
            }

            this._kitchen.layout.UpdateMeshes();
            this._kitchen.layout.UpdatePositions();

            this._kitchen.RefreshTickerLabels();

            await EventBridge.Designer.onKitchenReady.InvokeIfNonEmpty();

            return true;
        }

        private async UniTask LoadOccupantsFromJson(MelcherJsonObjectParser parser, JsonDict volumeData, OccupiableVolume volume, bool requireOccupants)
        {
            parser.Reset(volumeData)
                .GetValue("exists", out bool cabinetExists, defaultValue: false)
                .GetList("occupants", out JsonList volumeOccupants);

            if (parser.hasErrors)
            {
                if (requireOccupants)
                {
                    MelcherLog.LogError($"Error reading occupants in volume: " + Server.ServerJson.CombineParserErrorsIntoException(parser).Message);
                }

                return;
            }

            if (!cabinetExists)
            {
                return;
            }

            foreach (JsonDict occupantEntry in volumeOccupants)
            {
                parser.Reset(occupantEntry)
                    .GetValue("product_server_id", out int productId, defaultValue: 0)
                    .GetValue("product_id", out string productSku, defaultValue: null)
                    .GetValue("is_locked_to_cabinet", out bool isLockedToCabinet, defaultValue: false);

                if (string.IsNullOrEmpty(productSku) && productId == 0)
                {
                    MelcherLog.LogError($"Error reading occupant: " + Server.ServerJson.CombineParserErrorsIntoException(parser).Message);
                    continue;
                }

                float occupantLocalPositionX = default;
                float occupantLocalPositionY = float.NaN;
                float occupantLocalPositionZ = float.NaN;

                if (parser.TryGetAndPushObject("local_position", out JsonDict localPosition))
                {
                    parser
                        .GetValue("x", out occupantLocalPositionX)
                        .GetValue("y", out occupantLocalPositionY)
                        .GetValue("z", out occupantLocalPositionZ);

                    parser.PopObject();
                }
                else
                {
                    parser.GetValue("meters_from_cabinet_min_x", out occupantLocalPositionX);
                }

                Vector3 occupantCutOutSize = default;

                if (parser.TryGetAndPushObject("cut_out_size_in_meters", out JsonDict cutOutSize))
                {
                    parser
                        .GetValue("x", out occupantCutOutSize.x)
                        .GetValue("y", out occupantCutOutSize.y)
                        .GetValue("z", out occupantCutOutSize.z);

                    parser.PopObject();
                }

                if (parser.hasErrors)
                {
                    MelcherLog.LogError($"Failed to parse occupant in volume: " + Server.ServerJson.CombineParserErrorsIntoException(parser).Message);
                    continue;
                }

                ProductInfo productInfo = null;

                if (productId > 0)
                {
                    productInfo =
                        ProFitGlobals.catalogData.FindProductInfoById(
                            productId,
                            findCabinetSection: false
                        );
                }

                if (productInfo == null)
                {
                    productInfo =
                        ProFitGlobals.catalogData.FindProductInfoBySku(
                            productSku,
                            findCabinetSection: false
                        );

                    if (productInfo == null && ProFitGlobals.appOptions.remappedProductIdDatabase)
                    {
                        int? remappedProductId =
                            ProFitGlobals.appOptions.remappedProductIdDatabase.Lookup(productSku);

                        if (remappedProductId.HasValue)
                        {
                            ProductInfo remappedProductInfo =
                                ProFitGlobals.catalogData.FindProductInfoById(
                                    remappedProductId.Value,
                                    findCabinetSection: false
                                );

                            if (remappedProductInfo != null)
                            {
                                productInfo = remappedProductInfo;
                            }
                        }
                    }
                }

                if (productInfo == null)
                {
                    MelcherLog.LogError($"Unknown product SKU: {productSku}");
                    continue;
                }

                if (productInfo.category == ProductCategory.BarTopHigh ||
                    productInfo.category == ProductCategory.Backsplash)
                {
                    Cabinet cabinet = volume as Cabinet;

                    if (!cabinet)
                    {
                        MelcherLog.LogWarning(
                            $"Found a cabinet backing saved within a cabinet " +
                            $"shelf. This should never happen. Backings are " +
                            $"saved to the cabinet span or, in older builds, " +
                            $"to the root volume (cabinet), never a shelf."
                        );
                    }
                    else
                    {
                        int cabinetSpanIndex =
                            this._kitchen.layout.FindCabinetSpanIndex(cabinet);

                        await LoadBackingFromProductSku(cabinetSpanIndex, productSku);
                    }

                    continue;
                }

                ProductVisualSO productVisual =
                    this._productVisualDatabase.FindRuntimeVisualByID(
                        productInfo.visualId
                    );

                if (productVisual == null)
                {
                    MelcherLog.LogError($"Unknown product visual. SKU: {productSku}, visual ID: {productInfo.visualId}");
                    continue;
                }

                ProductVisualContent productVisualPrefab =
                    await ProFitGlobals.productContent
                        .LoadContent(productVisual);

                Occupant occupant =
                    await volume.InstantiateAndInsertOccupantAt(
                        userInitiated: false,
                        productInfo,
                        productVisual,
                        productVisualPrefab,
                        this._kitchen.InstantiateOccupant,
                        occupantLocalPositionX,
                        float.IsNaN(occupantLocalPositionY) ? null : occupantLocalPositionY,
                        float.IsNaN(occupantLocalPositionZ) ? null : occupantLocalPositionZ
                    );

                occupant.isLockedToCabinet = isLockedToCabinet;

                if (occupantCutOutSize != Vector3.zero)
                {
                    occupant.SetCutOutSizeInMeters(occupantCutOutSize);
                }

                if (occupant.TryGetComponent(out CabinetShelf shelf))
                {
                    parser.currentObject["exists"] = true;
                    await LoadOccupantsFromJson(parser, occupantEntry, shelf, requireOccupants: false);
                }
            }
        }



        private async UniTask LoadBackingFromProductSku(int cabinetSpanIndex, string backingProductSku)
        {
            CabinetSpan span = this._kitchen.layout.spans[cabinetSpanIndex];

            if (span.backing)
            {
                MelcherLog.LogError(
                    $"Attempted to load a backing for a cabinet span that " +
                    $"already has one.\n" +
                    $"\n" +
                    $"The current backing is: {span.backing}\n" +
                    $"The extra backing backing is: {backingProductSku}",
                    span.backing
                );
                return;
            }

            ProductInfo backingProductInfo =
                ProFitGlobals.catalogData.FindProductInfoBySku(
                    backingProductSku,
                    findCabinetSection: false
                );

            if (backingProductSku == null)
            {
                MelcherLog.LogError($"Unknown backing product SKU: {backingProductSku}");
                return;
            }

            CabinetBacking backingPrefab = null;

            switch (backingProductInfo.category)
            {
                case ProductCategory.Backsplash:
                    backingPrefab = this._backsplashPrefab;
                    break;

                case ProductCategory.BarTopHigh:
                    backingPrefab = this._highBarTopPrefab;

                    if (this._selections.brandCatalog &&
                        this._selections.brandCatalog.highBarTopOverridePrefab)
                    {
                        backingPrefab = this._selections.brandCatalog.highBarTopOverridePrefab;
                    }
                    break;

                case ProductCategory.BarTopLow:
                    backingPrefab = this._lowBarTopPrefab;
                    break;
            }

            if (!backingPrefab)
            {
                MelcherLog.LogError(
                    $"No backing prefab associated with product category " +
                    $"{backingProductInfo.category}. Backing product SKU: " +
                    $"{backingProductSku}"
                );

                return;
            }

            VolumeOccupantResult result =
                await span.TryInstantiateBacking(
                    backingProductInfo,
                    backingPrefab
                );

            if (result.status != OccupantCuboidStatus.Fits)
            {
                MelcherLog.LogError($"Could not create backing using SKU: {backingProductSku}");
            }
        }

        public async UniTask<bool> TryLoadRequestedSave()
        {
            if (!ProFitGlobals.activeClientId.HasValue ||
                !ProFitGlobals.activeQuoteId.HasValue)
            {
                return false;
            }

            int clientId = ProFitGlobals.activeClientId.Value;
            int quoteId = ProFitGlobals.activeQuoteId.Value;
            if (!ProFitGlobals.TryGetClientAndQuoteIndex(clientId, quoteId, out int clientIndex, out int quoteIndex))
            {
                return false;
            }

            UserClientQuote quote = ProFitGlobals.userClients[clientIndex].quotes[quoteIndex];

            using var clearLoadingSpinner =
                MelcherDeferred.Defer(
                    onDispose: static () => EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Hide)
                );

            EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Load);

            EventBridge.Designer.setSpinnerStatusMessage?.Invoke($"Preparing {quote.saveName}");

            return await LoadFromJson(quote.saveData);
        }
    }
}
