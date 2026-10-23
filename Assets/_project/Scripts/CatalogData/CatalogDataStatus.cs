using KitchenDesign.Scriptables;
using KitchenDesign.Server;
using System;
using System.Collections.Generic;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;
using MEC;
using Melcher;
using UnityEngine;
using KitchenDesign.Shared.Common;
using KitchenDesign.Products;

using JsonDict = System.Collections.Generic.IDictionary<string, object>;

namespace KitchenDesign.CatalogData
{
    public sealed class CatalogDataStatus
    {
        private readonly AppProfileSO _appProfile;
        private readonly IAppRules _appRules;
        private readonly ServerClient _server;
        private readonly Dictionary<(int id, bool isCabinetSection), ProductInfo> _runtimeProductsById = new();
        private readonly Dictionary<(string sku, bool isCabinetSection), ProductInfo> _runtimeProductsBySku = new();
        private Action _queuedActions;

        public CatalogDataStatus(AppProfileSO appProfile, IAppRules appRules, ServerClient server)
        {
            this._appProfile = appProfile;
            this._appRules = appRules;
            this._server = server;
        }

        public bool IsLoaded { get; private set; }

        public List<CabinetSectionCategoryInfo> CabinetSectionCategories { get; set; } = new(capacity: 0);
        public List<BrandCatalogInfo> ServerBrandCatalogs { get; set; } = new(capacity: 0);
        public List<BrandCatalogSO> RuntimeBrandCatalogs { get; set; } = new();
        public DoorStyleLookup DoorStyles { get; } = new DoorStyleLookup();
        public List<ManufacturerInfo> Manufacturers { get; set; } = new(capacity: 0);
        public List<ProductGroupInfo> ProductGroups { get; set; } = new(capacity: 0);
        public List<ProductTagInfo> ProductTags { get; set; } = new(capacity: 0);
        public List<CabinetCompatibilityInfo> CabinetCompatibilities { get; set; } = new(capacity: 0);
        public List<ProductInfo> SourceProducts { get; set; } = new(capacity: 0);
        public List<ProductInfo> RuntimeProducts { get; } = new(capacity: 0);
        public Dictionary<int, List<int>> BaseProductIdToProductVariantIds { get; set; } = new();

        public void Initialize()
        {
            this.IsLoaded = false;

            this.CabinetSectionCategories.Clear();
            this.Manufacturers.Clear();
            this.ProductGroups.Clear();
            this.SourceProducts.Clear();
            this.ClearRuntimeProducts();
            this.BaseProductIdToProductVariantIds.Clear();

            Timing.RunCoroutine(this.LoadCoroutine());
        }

        public void ExecuteWhenLoaded(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(paramName: nameof(action));
            }

            if (this.IsLoaded)
            {
                action.Invoke();
                return;
            }

            this._queuedActions += action;
        }

        public void AddRuntimeProduct(ProductInfo productInfo)
        {
            this.RuntimeProducts.Add(productInfo);

            bool isCabinetSection = productInfo.category == ProductCategory.CabinetSection;

            this._runtimeProductsById.TryAdd((productInfo.id, isCabinetSection), productInfo);

            if (productInfo.sku != null)
            {
                this._runtimeProductsBySku.TryAdd((productInfo.sku, isCabinetSection), productInfo);
            }
        }

        public void ClearRuntimeProducts()
        {
            this.RuntimeProducts.Clear();
            this._runtimeProductsById.Clear();
            this._runtimeProductsBySku.Clear();
        }

        public ProductInfo FindProductInfoById(int productServerId, bool findCabinetSection)
        {
            this._runtimeProductsById.TryGetValue((productServerId, findCabinetSection), out ProductInfo result);
            return result;
        }

        public ProductInfo FindProductInfoBySku(string sku, bool findCabinetSection)
        {
            if (string.IsNullOrEmpty(sku))
            {
                return null;
            }

            this._runtimeProductsBySku.TryGetValue((sku, findCabinetSection), out ProductInfo result);
            return result;
        }

        public List<int> GetProductVariantList(ProductInfo productInfo)
        {
            int baseProductId =
                productInfo.baseProductId != 0 ?
                    productInfo.baseProductId :
                    productInfo.id;

            return this.BaseProductIdToProductVariantIds.GetValueOrDefault(baseProductId);
        }

        public bool FindManufacturerByName(string manufacturerNameToFind, out ManufacturerInfo manufacturer)
        {
            IReadOnlyList<ManufacturerInfo> manufacturerList =
                (IReadOnlyList<ManufacturerInfo>)this.Manufacturers ?? Array.Empty<ManufacturerInfo>();

            return CatalogLookups.FindManufacturerByName(manufacturerList, manufacturerNameToFind, out manufacturer);
        }

        public bool FindManufacturerById(int manufacturerIdToFind, out ManufacturerInfo manufacturer)
        {
            IReadOnlyList<ManufacturerInfo> manufacturerList =
                (IReadOnlyList<ManufacturerInfo>)this.Manufacturers ?? Array.Empty<ManufacturerInfo>();

            int manufacturerCount = manufacturerList.Count;
            for (int i = 0; i < manufacturerCount; ++i)
            {
                if (manufacturerList[i].id == manufacturerIdToFind)
                {
                    manufacturer = manufacturerList[i];
                    return true;
                }
            }

            manufacturer = default;
            return false;
        }

        public bool FindProductGroupByName(string groupNameToFind, out ProductGroupInfo group)
        {
            return CatalogLookups.FindProductGroupByName(this.ProductGroups, groupNameToFind, out group);
        }

        public bool FindProductGroupById(int groupIdToFind, out ProductGroupInfo group)
        {
            IReadOnlyList<ProductGroupInfo> productGroupList =
                (IReadOnlyList<ProductGroupInfo>)this.ProductGroups ?? Array.Empty<ProductGroupInfo>();

            int productGroupCount = productGroupList.Count;
            for (int i = 0; i < productGroupCount; ++i)
            {
                if (productGroupList[i].id == groupIdToFind)
                {
                    group = productGroupList[i];
                    return true;
                }
            }

            group = default;
            return false;
        }

        public bool FindBrandCatalogInfoById(int catalogId, out BrandCatalogInfo brandCatalogInfo)
        {
            brandCatalogInfo = default;

            int catalogIndex =
                this.ServerBrandCatalogs.FindIndex(
                    productInfo => productInfo.id == catalogId
                );

            if (catalogIndex >= 0)
            {
                brandCatalogInfo = this.ServerBrandCatalogs[catalogIndex];
            }

            return catalogIndex >= 0;
        }

        public CabinetCompatibilityInfo FindCompatibilityById(int id)
        {
            if (id == 0)
            {
                return null;
            }

            foreach (CabinetCompatibilityInfo compatibility in this.CabinetCompatibilities)
            {
                if (compatibility.id == id)
                {
                    return compatibility;
                }
            }

            return null;
        }

        public JsonDict LoadFallbackWebResponse(string fileName)
        {
            string fallbackResourcePath = this._appProfile.GetFallbackJsonPath(fileName);

            TextAsset fallbackResource = Resources.Load<TextAsset>(fallbackResourcePath);

            using var unloadFallbackResource =
                MelcherDeferred.Defer(
                    toCapture: fallbackResource,
                    onDispose: static resource => Resources.UnloadAsset(resource)
                );

            if (fallbackResource == null)
            {
                MelcherLog.LogError($"Error loading fallback {fileName}");
                return null;
            }

            if (MiniJSON.Json.Deserialize(fallbackResource.text) is not JsonDict jsonDict)
            {
                MelcherLog.LogError($"Error deserializing fallback {fileName}");
                return null;
            }

            return jsonDict;
        }

        public Exception LoadAndApplyProductOverrides()
        {
            string productOverridesListPath =
                this._appProfile.GetFallbackJsonPath(FallbackJsonFiles.ProductOverrides);

            TextAsset productOverridesListJson = Resources.Load<TextAsset>(productOverridesListPath);

            if (!productOverridesListJson)
            {
                return new Exception("Failed to load product overrides list.");
            }

            JsonDict response = (JsonDict)MiniJSON.Json.Deserialize(productOverridesListJson.text);

            if (response == null)
            {
                return new Exception("Failed to deserialize product override JSON.");
            }

            Exception error =
                CatalogJson.TryParseProductList(
                    new MelcherJsonObjectParser(response),
                    this.Manufacturers,
                    this.ProductGroups,
                    this._appRules,
                    out List<ProductInfo> overrideProductList
                );

            if (error != null)
            {
                MelcherLog.LogError(error.Message);
                return error;
            }

            foreach (ProductInfo overrideProductInfo in overrideProductList)
            {
                int existingProductIndex =
                    this.SourceProducts.FindIndex(
                        x => x.sku == overrideProductInfo.sku
                    );

                if (existingProductIndex < 0)
                {
                    MelcherLog.Log(
                        $"Adding a new product info for {MelcherFormat.ConsoleString(overrideProductInfo.name)} ({overrideProductInfo.manufacturerId}).",
                        severity: MelcherLogSeverity.Verbose
                    );

                    this.SourceProducts.Add(overrideProductInfo);

                    continue;
                }

                MelcherLog.Log(
                    $"Overriding product info for {MelcherFormat.ConsoleString(this.SourceProducts[existingProductIndex].name)}.",
                    severity: MelcherLogSeverity.Verbose
                );

                ProductInfo originalProduct = this.SourceProducts[existingProductIndex];

                if (!string.IsNullOrEmpty(overrideProductInfo.name))
                {
                    originalProduct.name = overrideProductInfo.name;
                }

                if (!string.IsNullOrEmpty(overrideProductInfo.description))
                {
                    originalProduct.description = overrideProductInfo.description;
                }

                if (!string.IsNullOrEmpty(overrideProductInfo.visualId))
                {
                    originalProduct.visualId = overrideProductInfo.visualId;
                }

                if (!MelcherCommon.IsAlmostEqual(overrideProductInfo.productSizeInMeters, Vector3.zero, tolerance: 0.001f))
                {
                    originalProduct.productSizeInMeters = overrideProductInfo.productSizeInMeters;
                }

                if (overrideProductInfo.minWidthInMeters.HasValue)
                {
                    originalProduct.minWidthInMeters = overrideProductInfo.minWidthInMeters.Value;
                }

                if (overrideProductInfo.maxWidthInMeters.HasValue)
                {
                    originalProduct.maxWidthInMeters = overrideProductInfo.maxWidthInMeters.Value;
                }

                if (overrideProductInfo.productTagIds.Count > 0)
                {
                    originalProduct.productTagIds = overrideProductInfo.productTagIds;
                }

                if (overrideProductInfo.compatibilityId != 0)
                {
                    originalProduct.compatibilityId = overrideProductInfo.compatibilityId;
                }
            }

            return null;
        }

        public void BuildProductVariantLists()
        {
            this.BaseProductIdToProductVariantIds.Clear();

            foreach (ProductInfo productInfo in this.SourceProducts)
            {
                int baseProductId = productInfo.baseProductId;

                if (baseProductId == 0)
                {
                    continue;
                }

                List<int> productVariantIds =
                    this.BaseProductIdToProductVariantIds.GetValueOrDefault(baseProductId);

                if (productVariantIds == null)
                {
                    productVariantIds = new();
                    productVariantIds.Add(baseProductId);
                    this.BaseProductIdToProductVariantIds[baseProductId] = productVariantIds;
                }

                productVariantIds.Add(productInfo.id);
            }
        }

        public void PopulateRuntimeBrandCatalogs()
        {
            this.RuntimeBrandCatalogs.Clear();

            foreach (BrandCatalogInfo serverBrandCatalog in this.ServerBrandCatalogs)
            {
                foreach (BrandCatalogSO presetBrandCatalog in this._appProfile.presetBrandCatalogs)
                {
                    if (serverBrandCatalog.id == presetBrandCatalog.catalogId)
                    {
                        this.RuntimeBrandCatalogs.Add(presetBrandCatalog);
                        break;
                    }

                    if (serverBrandCatalog.cabinetManufacturerId == presetBrandCatalog.cabinetManufacturerId)
                    {
                        BrandCatalogSO serverDefinedCatalog = UnityEngine.Object.Instantiate(presetBrandCatalog);

                        serverDefinedCatalog.catalogName = serverBrandCatalog.name;
                        serverDefinedCatalog.catalogId = serverBrandCatalog.id;

                        this.RuntimeBrandCatalogs.Add(serverDefinedCatalog);

                        break;
                    }
                }
            }
        }

        private IEnumerator<float> LoadCoroutine()
        {
            AppOptionsSO appOptions = this._appProfile.appOptions;

            if (this.LoadFallbackWebResponse(FallbackJsonFiles.Manufacturers) is JsonDict manufacturersResponse)
            {
                Exception error =
                    CatalogJson.TryParseManufacturers(
                        new MelcherJsonObjectParser(manufacturersResponse),
                        out List<ManufacturerInfo> manufacturers
                    );

                this.Manufacturers = manufacturers;

                if (error != null)
                {
                    MelcherLog.LogError("Error parsing fallback manufacturers JSON: " + error.Message);
                }
            }

            if (this.LoadFallbackWebResponse(FallbackJsonFiles.ProductGroups) is JsonDict productGroupsResponse)
            {
                Exception error =
                    CatalogJson.TryParseProductGroups(
                        new MelcherJsonObjectParser(productGroupsResponse),
                        out List<ProductGroupInfo> productGroups
                    );

                this.ProductGroups = productGroups;

                if (error != null)
                {
                    MelcherLog.LogError("Error parsing fallback product groups JSON: " + error.Message);
                }
            }

            if (this.LoadFallbackWebResponse(FallbackJsonFiles.CabinetSectionCategories) is JsonDict categoriesResponse)
            {
                Exception error =
                    CatalogJson.TryParseCabinetSectionCategoryList(
                        new MelcherJsonObjectParser(categoriesResponse),
                        out List<CabinetSectionCategoryInfo> cabinetSectionCategories
                    );

                this.CabinetSectionCategories = cabinetSectionCategories;

                if (error != null)
                {
                    MelcherLog.LogError("Error parsing fallback cabinet section categories JSON: " + error.Message);
                }
            }

            if (appOptions.cabinetDataAvailability >= CabinetDataAvailability.ClientOnly &&
                this.LoadFallbackWebResponse(FallbackJsonFiles.BrandCatalogs) is JsonDict brandCatalogsResponse)
            {
                Exception error =
                    CatalogJson.TryParseBrandCatalogs(
                        new MelcherJsonObjectParser(brandCatalogsResponse),
                        out List<BrandCatalogInfo> serverBrandCatalogs
                    );

                this.ServerBrandCatalogs = serverBrandCatalogs;

                if (error != null)
                {
                    MelcherLog.LogError("Error parsing fallback brand catalogs JSON: " + error.Message);
                }
            }

            if (appOptions.cabinetDataAvailability >= CabinetDataAvailability.ClientOnly &&
                this.LoadFallbackWebResponse(FallbackJsonFiles.Tags) is JsonDict tagsResponse)
            {
                Exception error =
                    CatalogJson.TryParseTags(
                        new MelcherJsonObjectParser(tagsResponse),
                        out List<ProductTagInfo> productTags
                    );

                this.ProductTags = productTags;

                if (error != null)
                {
                    MelcherLog.LogError("Error parsing fallback product tags JSON: " + error.Message);
                }
            }

            if (this.LoadFallbackWebResponse(FallbackJsonFiles.Compatibilities) is JsonDict compatibilitiesResponse)
            {
                Exception error =
                    CatalogJson.TryParseCompatibilities(
                        new MelcherJsonObjectParser(compatibilitiesResponse),
                        out List<CabinetCompatibilityInfo> cabinetCompatibilities
                    );

                this.CabinetCompatibilities = cabinetCompatibilities;

                if (error != null)
                {
                    MelcherLog.LogError("Error parsing fallback compatibilities JSON: " + error.Message);
                }
            }

            if (this.LoadFallbackWebResponse(FallbackJsonFiles.Products) is JsonDict productsResponse)
            {
                Exception error =
                    CatalogJson.TryParseProductList(
                        new MelcherJsonObjectParser(productsResponse),
                        this.Manufacturers,
                        this.ProductGroups,
                        this._appRules,
                        out List<ProductInfo> sourceProducts
                    );

                this.SourceProducts = sourceProducts;

                if (error != null)
                {
                    MelcherLog.LogError("Error parsing fallback product JSON: " + error.Message);
                }
            }

            if (appOptions.cabinetDataAvailability >= CabinetDataAvailability.ClientOnly &&
                this.LoadFallbackWebResponse(FallbackJsonFiles.Cabinets) is JsonDict cabinetsResponse)
            {
                Exception error =
                    CatalogJson.TryParseCabinetList(
                        new MelcherJsonObjectParser(cabinetsResponse),
                        this.Manufacturers,
                        this.CabinetSectionCategories,
                        this.ProductGroups,
                        this.SourceProducts,
                        this._appRules,
                        out List<ProductInfo> cabinetList
                    );

                if (error != null)
                {
                    MelcherLog.LogError("Error parsing fallback cabinet list JSON: " + error.Message);
                }

                if (cabinetList != null)
                {
                    this.SourceProducts.AddRange(cabinetList);
                }
            }

            this.LoadAndApplyProductOverrides();

            this.BuildProductVariantLists();

            IEnumerator<float> probe = this._server.Connection.ProbeCoroutine();
            while (probe.MoveNext())
            {
                yield return probe.Current;
            }

            bool hostIsReachable = this._server.Connection.IsHostReachable;

            if (hostIsReachable)
            {
                MelcherLog.Log("Found server at: " + this._server.Connection.Host);

                if (appOptions.cabinetDataAvailability >= CabinetDataAvailability.Server)
                {
                    var request =
                        this._server.Catalog.GetBrandCatalogs(
                            onSuccess: newBrandCatalogs => this.ServerBrandCatalogs = newBrandCatalogs,
                            onError: (builder, error, jsonValue) =>
                            {
                                MelcherLog.LogError(error.Message);
                            }
                        );

                    yield return Timing.WaitUntilDone(request);

                    this.PopulateRuntimeBrandCatalogs();
                }

                {
                    var request =
                        this._server.Catalog.GetManufacturers(
                            onSuccess: newManufacturers => this.Manufacturers = newManufacturers,
                            onError: (builder, error, jsonValue) =>
                            {
                                MelcherLog.LogError(error.Message);
                            }
                        );

                    yield return Timing.WaitUntilDone(request);
                }

                {
                    var request =
                        this._server.Catalog.GetProductGroups(
                            onSuccess: newProductGroupsList => this.ProductGroups = newProductGroupsList,
                            onError: (builder, error, jsonValue) =>
                            {
                                MelcherLog.LogError(error.Message);
                            }
                        );

                    yield return Timing.WaitUntilDone(request);
                }

                if (appOptions.cabinetDataAvailability >= CabinetDataAvailability.Server)
                {
                    var request =
                        this._server.Catalog.GetTags(
                            onSuccess: newProductTagsList => this.ProductTags = newProductTagsList,
                            onError: (builder, error, jsonValue) =>
                            {
                                MelcherLog.LogError(error.Message);
                            }
                        );

                    yield return Timing.WaitUntilDone(request);
                }

                if (appOptions.cabinetDataAvailability >= CabinetDataAvailability.Server)
                {
                    var request =
                        this._server.Catalog.GetCompatibilities(
                            onSuccess: newCabinetCompatibilities => this.CabinetCompatibilities = newCabinetCompatibilities,
                            onError: (builder, error, jsonValue) =>
                            {
                                MelcherLog.LogError(error.Message);
                            }
                        );

                    yield return Timing.WaitUntilDone(request);
                }
            }

#if UNITY_EDITOR
            this.AddEditorProductsForUnusedVisuals();
#endif

            this.IsLoaded = true;

            MelcherLog.LogEditorOnly("Executing queued actions...");
            this._queuedActions?.Invoke();
            this._queuedActions = null;
            MelcherLog.LogEditorOnly("Finished queued actions");
        }

#if UNITY_EDITOR
        private void AddEditorProductsForUnusedVisuals()
        {
            int fakeProductNumber = 0;

            string[] productVisualDatabaseGuids =
                UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ProductVisualDatabaseSO)}");

            MelcherLog.Assert(productVisualDatabaseGuids.Length == 1,
                "This bit of code assumes there is only one product visual database. Please update the code to select the correct catalog."
            );

            string productVisualDatabasePath =
                UnityEditor.AssetDatabase.GUIDToAssetPath(productVisualDatabaseGuids[0]);

            ProductVisualDatabaseSO productVisualDatabase =
                UnityEditor.AssetDatabase.LoadAssetAtPath<ProductVisualDatabaseSO>(productVisualDatabasePath);

            int defaultManufacturerId = this._appProfile.defaultManufacturerId;

            int defaultManufacturerIndex =
                this.Manufacturers.FindIndex(
                    x => x.id == defaultManufacturerId
                );

            if (defaultManufacturerIndex < 0)
            {
                throw new Exception("Couldn't find default manufacturer index.");
            }

            defaultManufacturerId = this.Manufacturers[defaultManufacturerIndex].id;

            MelcherLog.LogVerbose($"Using manufacturer ID {defaultManufacturerId} as the default. It's name is: {this.Manufacturers[defaultManufacturerIndex].name}");

            System.Text.StringBuilder stringBuilder = new();

            stringBuilder.AppendLine("<Visual Name> :: <Visual ID> :: <Category>");

            foreach (ProductVisualSO visual in productVisualDatabase.runtimeEntries)
            {
                ProductVisualContent visualContent =
                    visual.contentAddress.editorOnlyCachedAsset;

                if (!visualContent || !(visualContent.productModelPrefab || visualContent.finishMaterial))
                {
                    continue;
                }

                int indexOfFirstProductUsingVisual =
                    this.SourceProducts.FindIndex(
                        x => x.visualId == visual.info.id
                    );

                if (indexOfFirstProductUsingVisual >= 0)
                {
                    continue;
                }

                Vector3 modelCutOutSizeInMeters =
                    visualContent.modelCutOutExtentMax.localPosition - visualContent.modelCutOutExtentMin.localPosition;

                modelCutOutSizeInMeters *= 0.8f;

                this.SourceProducts.Add(
                    new ProductInfo()
                    {
                        id = -1 - fakeProductNumber,
                        sku = "UnusedVisual_" + fakeProductNumber,
                        manufacturerId = defaultManufacturerId,
                        groupId = 0,
                        name = visualContent.name,
                        category = visual.info.runtimePrefabCategory.ToProductCategory(),
                        cutOutSizeInMeters = modelCutOutSizeInMeters,
                        visualId = visual.info.id,
                        doesExistInServerDashboard = false
                    }
                );

                ++fakeProductNumber;

                stringBuilder.AppendLine(
                    $"{visual.name} :: {visual.info.id} :: {this.SourceProducts[^1].category}"
                );
            }

            string outPath =
                Utility.LogFilesForEditor.Overwrite(
                    "issues-unused-product-visuals.txt",
                    stringBuilder.ToString()
                );

            MelcherLog.LogErrorEditorOnly("Wrote unused product visuals to: " + outPath);
        }
#endif
    }
}
