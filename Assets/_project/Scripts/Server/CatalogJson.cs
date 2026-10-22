using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;
using Melcher;
using UnityEngine;

using JsonDict = System.Collections.Generic.IDictionary<string, object>;
using JsonList = System.Collections.Generic.IList<object>;

namespace KitchenDesign.Server
{
    public static class CatalogJson
    {
        public static Exception TryParseManufacturers(MelcherJsonObjectParser parser, out List<ManufacturerInfo> manufacturers)
        {
            manufacturers = new List<ManufacturerInfo>();

            parser.GetList("manufacturers", out JsonList manufacturerListJson);
            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            List<Exception> manufacturerIssues = new List<Exception>();

            foreach (JsonDict manufacturerJson in manufacturerListJson)
            {
                Exception error =
                    CatalogJson.TryParseManufacturer(
                        parser.Reset(manufacturerJson),
                        out ManufacturerInfo manufacturer
                    );

                if (error != null)
                {
                    manufacturerIssues.Add(error);
                    continue;
                }

                manufacturers.Add(manufacturer);
            }

            if (manufacturerIssues.Count > 0)
            {
                MelcherLog.LogError("Errors parsing manufacturer list: " + string.Join("\n", manufacturerIssues));
            }

            return null;
        }

        public static Exception TryParseManufacturer(MelcherJsonObjectParser parser, out ManufacturerInfo manufacturer)
        {
            manufacturer = default;
            manufacturer.doorStyles = new();

            parser
                .GetValue("id", out manufacturer.id)
                .GetValue("name", out manufacturer.name)
                .GetValue("order", out manufacturer.order, defaultValue: -1, MelcherJsonValueTypeOption.Lenient);

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            if (parser.TryGetAndPushObject("type", out JsonDict typeObject))
            {
                using var autoPopTypeObject =
                    MelcherDeferred.Defer(
                        toCapture: parser,
                        onDispose: static parser => parser.PopObject()
                    );

                parser.GetValue("value", out string categoryName, defaultValue: null);

                if (parser.hasErrors)
                {
                    return ServerJson.CombineParserErrorsIntoException(parser);
                }

                if (CatalogJson.TryParseProductCategory(categoryName, out ProductCategory productCategory))
                {
                    manufacturer.productCategory = productCategory;
                }
                else
                {
                    return new Exception($"Could not convert \"{categoryName}\" to a {typeof(ProductCategory).FullName}");
                }
            }

            parser.GetList("door_styles", out JsonList doorStylesList, errorIfNotFound: false);
            if (doorStylesList != null)
            {
                for (int i = 0; i < doorStylesList.Count; ++i)
                {
                    ManufacturerDoorStyleInfo doorStyleInfo = default;

                    parser
                        .PushObject(i, doorStylesList[i] as JsonDict)
                            .GetValue("id", out doorStyleInfo.id)
                            .GetValue("name", out doorStyleInfo.name)
                        .PopObject();

                    if (parser.hasErrors)
                    {
                        return ServerJson.CombineParserErrorsIntoException(parser);
                    }

                    manufacturer.doorStyles.Add(doorStyleInfo);
                }
            }

            return null;
        }

        public static bool TryParseProductCategory(string productCategoryName, out ProductCategory productCategory)
        {
            if (!Enum.TryParse(productCategoryName, ignoreCase: true, out productCategory))
            {
                productCategory =
                    productCategoryName switch
                    {
                        "Accessories" => ProductCategory.Accessory,
                        "Burners" => ProductCategory.Burner,
                        "Finishes" => ProductCategory.Finish,
                        "FinishesCabinets" => ProductCategory.FinishCabinet,
                        "FinishesCountertops" => ProductCategory.FinishCountertop,
                        "FinishesFlooring" => ProductCategory.FinishFloor,
                        "FridgeAndSinks" => ProductCategory.Sink,
                        "Grills" => ProductCategory.Grill,
                        "GrillswithInsulatedJackets" => ProductCategory.GrillWithInsulatedJacket,
                        "BurnerswithInsulatedJackets" => ProductCategory.BurnerWithInsulatedJacket,
                        "BarTop" => ProductCategory.BarTopHigh,
                        "LowBarTop" => ProductCategory.BarTopLow,
                        _ => ProductCategory.Unknown
                    };
            }

            return productCategory != ProductCategory.Unknown;
        }

        public static Exception TryParseCabinetSectionCategoryList(
            MelcherJsonObjectParser parser,
            out List<CabinetSectionCategoryInfo> categories)
        {
            categories = new ();

            parser.GetList("cabinet_section_category", out JsonList categoryListJson);
            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            for (int i = 0; i < categoryListJson.Count; i++)
            {
                JsonDict categoryJson = (JsonDict)categoryListJson[i];
                Exception error =
                    CatalogJson.TryParseCabinetSectionCategory(
                        parser.Reset(categoryJson),
                        out CabinetSectionCategoryInfo category
                    );

                if (error != null)
                {
                    Debug.LogException(error);
                    continue;
                }

                category.sortOrder = i;

                categories.Add(category);
            }

            return null;
        }

        public static Exception TryParseCabinetSectionCategory(
            MelcherJsonObjectParser parser,
            out CabinetSectionCategoryInfo category)
        {
            category = default;

            parser
                .GetValue("id", out category.id)
                .GetValue("name", out category.name);

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            return null;
        }

        public static Exception TryParseProductGroups(MelcherJsonObjectParser parser, out List<ProductGroupInfo> productGroups)
        {
            productGroups = new List<ProductGroupInfo>();

            parser.GetList("groups", out JsonList manufacturerListJson);
            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            List<Exception> productGroupIssues = new List<Exception>();

            foreach (JsonDict manufacturerJson in manufacturerListJson)
            {
                Exception error =
                    CatalogJson.TryParseProductGroup(
                        parser.Reset(manufacturerJson),
                        out ProductGroupInfo productGroup
                    );

                if (error != null)
                {
                    productGroupIssues.Add(error);
                    continue;
                }

                productGroups.Add(productGroup);
            }

            if (productGroupIssues.Count > 0)
            {
                MelcherLog.LogError("Errors parsing product group list: " + string.Join("\n", productGroupIssues.Select(x => x .Message)));
            }

            return null;
        }

        public static Exception TryParseProductGroup(MelcherJsonObjectParser parser, out ProductGroupInfo productGroup)
        {
            productGroup = default;

            parser.GetValue("id", out productGroup.id)
                .GetValue("name", out productGroup.name);

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            return null;
        }

        public static Exception TryParseProductList(
            MelcherJsonObjectParser parser,
            List<ManufacturerInfo>  manufacturers,
            IReadOnlyList<ProductGroupInfo> productGroups,
            IAppRules               appRules,
            out List<ProductInfo>   productList)
        {
            productList = null;

            parser.GetList("components", out JsonList productsJsonList);
            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            Dictionary<string, List<string>> productIssuesByManufacturer =
                new Dictionary<string, List<string>>();

            productList = new List<ProductInfo>();
            List<string> productIssues = new List<string>();

            List<(string modelId, string productName, string productId)> modelIdToProductName = new List<(string, string, string)>();

            const MelcherJsonValueTypeOption ForgivingJsonOptions =
                MelcherJsonValueTypeOption.AllowTypeConversion |
                MelcherJsonValueTypeOption.AllowNullValueTypes;

            foreach (JsonDict productJson in productsJsonList)
            {
                ProductInfo product =
                    new ProductInfo()
                    {
                        doesExistInServerDashboard = true
                    };

                productIssues.Clear();
                bool productHasCriticalErrors = false;

                parser.Reset(productJson)
                    .GetValue("id", out product.id)
                    .GetValue("order", out product.sortOrder, defaultValue: -1, options: MelcherJsonValueTypeOption.Lenient)
                    .GetValue("product_id", out product.sku)
                    .GetValue("manufacturer", out string manufacturerName)
                    .GetValue("group", out string productGroupName, defaultValue: null)
                    .GetValue("base_component_id", out product.baseProductId, defaultValue: 0, options: MelcherJsonValueTypeOption.Lenient)
                    .GetValue("name", out product.name)
                    .GetValue("description", out product.description)
                    .GetValue("link", out product.storeLink)
                    .GetValue("model_id", out product.visualId)
                    .GetValue("distributor_price", out product.priceDealer, ForgivingJsonOptions)
                    .GetValue("customer_price", out product.priceCustomer, ForgivingJsonOptions)
                    .GetValue("type", out string productType)
                    .GetValue("width", out string productWidth)
                    .GetValue("height", out string productHeight)
                    .GetValue("depth", out string productDepth)
                    .GetValue("cutout_width", out string cutOutWidth)
                    .GetValue("cutout_height", out string cutOutHeight)
                    .GetValue("cutout_depth", out string cutOutDepth)
                    .GetValue("min_width", out string minWidth, defaultValue: null)
                    .GetValue("max_width", out string maxWidth, defaultValue: null)
                    .GetValue("compatibility_id", out product.compatibilityId, defaultValue: 0, options: MelcherJsonValueTypeOption.Lenient)
                    .GetValue("do_layout_using_product_size", out product.doLayoutUsingProductSize, defaultValue: false)
                    .GetList("tags", out JsonList productTags, errorIfNotFound: false);

                if (parser.hasErrors)
                {
                    MelcherLog.LogErrorEditorOnly($"Error parsing product. SKU: '{product.sku}', Product Name: {product.name}\n{ServerJson.CombineParserErrorsIntoException(parser)}");
                    continue;
                }

                if (productTags != null)
                {
                    Exception error = CatalogJson.TryParseTags(parser, out List<ProductTagInfo> tags);
                    if (error != null)
                    {
                        MelcherLog.LogErrorEditorOnly($"Error parsing product. SKU: '{product.sku}', Product Name: {product.name}\n{error}");
                        continue;
                    }

                    foreach (ProductTagInfo tag in tags)
                    {
                        product.productTagIds.Add(tag.id);
                    }
                }

                {
                    if (CatalogLookups.FindManufacturerByName(manufacturers, manufacturerName, out ManufacturerInfo manufacturerInfo))
                    {
                        product.manufacturerId = manufacturerInfo.id;
                    }
                    else
                    {
                        int smallestManufacturerId = 0;

                        if (manufacturers.Any())
                        {
                            smallestManufacturerId =
                                Math.Min(
                                    smallestManufacturerId,
                                    manufacturers.Min(x => x.id)
                                );
                        }

                        int newManufacturerId = smallestManufacturerId - 1;

                        manufacturers.Add(
                            new ManufacturerInfo()
                            {
                                id = newManufacturerId,
                                name = manufacturerName,
                            }
                        );

                        MelcherLog.LogWarning(
                            $"Adding unknown manufacturer '{manufacturerName}' ({newManufacturerId}) first seen on '{product.name}' ({product.id})"
                        );

                        product.manufacturerId = newManufacturerId;
                    }

                    if (product.manufacturerId < 0)
                    {
                        productIssues.Add("Unknown manufacturer: " + manufacturerName);
                    }
                }

                if (!string.IsNullOrEmpty(productGroupName))
                {
                    if (CatalogLookups.FindProductGroupByName(productGroups, productGroupName, out ProductGroupInfo productGroup))
                    {
                        product.groupId = productGroup.id;
                    }
                    else
                    {
                        productIssues.Add("Unknown product group: " + productGroupName);
                    }
                }

                if (!CatalogJson.TryParseProductCategory(productType, out product.category))
                {
                    productIssues.Add("Unknown product category: " + productType);
                    productHasCriticalErrors = true;
                }

                if (ProFitUtility.DoesCategoryUseSizes(product.category))
                {
                    if (!string.IsNullOrWhiteSpace(productWidth) &&
                        !Utility.MeasurementFormat.TryParseImperialString(
                            productWidth,
                            out product.productSizeInMeters.x,
                            nameof(productWidth),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid product width: " + productWidth);
                    }

                    if (!string.IsNullOrWhiteSpace(productHeight) &&
                        !Utility.MeasurementFormat.TryParseImperialString(
                            productHeight,
                            out product.productSizeInMeters.y,
                            nameof(productHeight),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid overlap height: " + productHeight);
                    }

                    if (!string.IsNullOrWhiteSpace(productDepth) &&
                        !Utility.MeasurementFormat.TryParseImperialString(
                            productDepth,
                            out product.productSizeInMeters.z,
                            nameof(productDepth),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid overlap depth: " + productDepth);
                    }

                    if (string.IsNullOrWhiteSpace(cutOutWidth) ||
                        !Utility.MeasurementFormat.TryParseImperialString(
                            cutOutWidth,
                            out product.cutOutSizeInMeters.x,
                            nameof(cutOutWidth),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid cut-out width: " + cutOutWidth);
                    }

                    if (string.IsNullOrWhiteSpace(cutOutHeight) ||
                        !Utility.MeasurementFormat.TryParseImperialString(
                            cutOutHeight,
                            out product.cutOutSizeInMeters.y,
                            nameof(cutOutHeight),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid cut-out height: " + cutOutHeight);
                    }

                    if (string.IsNullOrWhiteSpace(cutOutDepth) ||
                        !Utility.MeasurementFormat.TryParseImperialString(
                            cutOutDepth,
                            out product.cutOutSizeInMeters.z,
                            nameof(cutOutDepth),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid cut-out depth: " + cutOutDepth);
                    }
                }

                if (product.sku == "Backing:Backsplash")
                {
                    product.visualId = "backsplash";
                }
                else if (product.sku == "Backing:BarTop")
                {
                    product.visualId = "bartop";
                }
                else if (product.sku == "Backing:LowBarTop")
                {
                    product.visualId = "Backing:LowBarTop";
                }
                else if (product.sku == "Shelf:Top")
                {
                    product.visualId = "Shelf:Top";
                }
                else if (product.sku == "SARBKT")
                {
                    product.visualId = "Accessory_Stoll_StainlessSteelBurnerPlungeKit";
                }

                if (string.IsNullOrEmpty(product.visualId))
                {
                    productIssues.Add("Missing visualId.");
                }

                if (product.productPaddingInMeters.top      != 0.0f ||
                    product.productPaddingInMeters.bottom   != 0.0f ||
                    product.productPaddingInMeters.left     != 0.0f ||
                    product.productPaddingInMeters.right    != 0.0f)
                {
                    MelcherLog.LogError(
                        $"Found non-zero product padding for product with " +
                        $"SKU {product.sku}. This looks like the product's " +
                        $"padding is being assigned but the code that sets " +
                        $"the default values will stomp over it."
                    );
                }

                if (!string.IsNullOrWhiteSpace(minWidth))
                {
                    bool didParse =
                        Utility.MeasurementFormat.TryParseImperialString(
                            minWidth,
                            out float minWidthInMeters,
                            nameof(minWidth),
                            productIssues
                        );

                    if (didParse)
                    {
                        product.minWidthInMeters = minWidthInMeters;
                    }
                    else
                    {
                        productIssues.Add($"Bad {nameof(minWidth)} format: " + minWidth);
                    }
                }

                if (!string.IsNullOrWhiteSpace(maxWidth))
                {
                    bool didParse =
                        Utility.MeasurementFormat.TryParseImperialString(
                            maxWidth,
                            out float maxWidthInMeters,
                            nameof(maxWidth),
                            productIssues
                        );

                    if (didParse)
                    {
                        product.maxWidthInMeters = maxWidthInMeters;
                    }
                    else
                    {
                        productIssues.Add($"Bad {nameof(minWidth)} format: " + minWidth);
                    }
                }

                product.productPaddingInMeters.top      = Const.OccupantMinimumVerticalPaddingInMeters;
                product.productPaddingInMeters.bottom   = 0.0f;

                float occupantMinimumHorizontalPaddingInMeters = appRules.OccupantMinimumHorizontalPaddingInMeters;

                product.productPaddingInMeters.left     = occupantMinimumHorizontalPaddingInMeters * 0.5f;
                product.productPaddingInMeters.right    = occupantMinimumHorizontalPaddingInMeters * 0.5f;

                if (productTags != null)
                {
                    foreach (object tag in productTags)
                    {
                        product.productTagIds.Add(tag.GetHashCode());
                    }
                }

                modelIdToProductName.Add(
                    (
                        product.visualId,
                        product.name,
                        product.sku
                    )
                );

                if (productIssues.Count > 0)
                {
                    string error = $"**[{product.sku}]** {product.name}\n\n * " + string.Join("\n * ", productIssues) + "\n\n";

                    if (!productIssuesByManufacturer.TryGetValue(manufacturerName, out List<string> manufacturerProductList))
                    {
                        manufacturerProductList = new List<string>();
                        productIssuesByManufacturer.Add(manufacturerName, manufacturerProductList);
                    }

                    manufacturerProductList.Add(error);
                }

                product.hasErrors = productHasCriticalErrors;
                productList.Add(product);
            }

#if UNITY_EDITOR
            if (productIssuesByManufacturer.Count > 0)
            {
                var alphabetizedManufacturers = productIssuesByManufacturer.Keys.OrderBy(x => x).ToList();

                StringBuilder stringBuilder = new StringBuilder();

                foreach (var manufacturer in alphabetizedManufacturers)
                {
                    stringBuilder.AppendLine("## " + manufacturer);

                    var issues = productIssuesByManufacturer[manufacturer];
                    foreach (string issue in issues)
                    {
                        stringBuilder.AppendLine(issue);
                    }
                }

                string outPath =
                    ProFitUtility.Editor.OverwriteSpecificLogFile(
                        "issues-products.md",
                        stringBuilder.ToString()
                    );

                MelcherLog.LogErrorEditorOnly("Wrote product issues to: " + outPath);
            }
#endif

            if (productList.Count <= 0)
            {
                return new Exception("Failed to parse products");
            }

            return null;
        }

        public static Exception TryParseBrandCatalogs(
            MelcherJsonObjectParser parser,
            out List<BrandCatalogInfo> brandCatalogs)
        {
            brandCatalogs = new List<BrandCatalogInfo>();

            parser.GetList("catalogs", out JsonList brandCatalogListJson);
            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            List<Exception> brandCatalogIssues = new List<Exception>();

            foreach (JsonDict brandCatalogJson in brandCatalogListJson)
            {
                Exception error =
                    CatalogJson.TryParseBrandCatalog(
                        parser.Reset(brandCatalogJson),
                        out BrandCatalogInfo brandCatalog
                    );

                if (error != null)
                {
                    brandCatalogIssues.Add(error);
                    continue;
                }

                brandCatalogs.Add(brandCatalog);
            }

            if (brandCatalogIssues.Count > 0)
            {
                MelcherLog.LogError("Errors parsing brand catalog list: " + string.Join("\n", brandCatalogIssues));
            }

            return null;
        }

        public static Exception TryParseBrandCatalog(MelcherJsonObjectParser parser, out BrandCatalogInfo brandCatalog)
        {
            brandCatalog = default;

            parser
                .GetValue("id", out brandCatalog.id)
                .GetValue("name", out brandCatalog.name)
                .GetValue("manufacturer_id", out brandCatalog.cabinetManufacturerId, defaultValue: 0)
                .GetList("available_manufacturer_ids", out JsonList availableManufacturerIds)
                .GetList("available_groups_ids", out JsonList availableGroupIds)
                .GetList("starting_cabinet_ids", out JsonList startingCabinetSectionIds)
                .GetList("available_cabinet_ids", out JsonList allowedCabinetSectionIds, errorIfNotFound: false);

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            brandCatalog.availableManufacturerIds = new List<int>();
            if (availableManufacturerIds != null)
            {
                foreach (object id in availableManufacturerIds)
                {
                    brandCatalog.availableManufacturerIds.Add((int)(long)id);
                }
            }

            brandCatalog.availableGroupIds = new List<int>();
            if (availableGroupIds != null)
            {
                foreach (object id in availableGroupIds)
                {
                    brandCatalog.availableGroupIds.Add((int)(long)id);
                }
            }

            brandCatalog.startingCabinetSectionServerIds = new List<int>();
            if (startingCabinetSectionIds != null)
            {
                foreach (object id in startingCabinetSectionIds)
                {
                    brandCatalog.startingCabinetSectionServerIds.Add((int)(long)id);
                }
            }

            brandCatalog.allowedCabinetSectionServerIds = new List<int>();
            if (allowedCabinetSectionIds != null)
            {
                foreach (object id in allowedCabinetSectionIds)
                {
                    brandCatalog.allowedCabinetSectionServerIds.Add((int)(long)id);
                }
            }

            return null;
        }

        public static Exception TryParseCabinetList(
            MelcherJsonObjectParser parser,
            List<ManufacturerInfo>  manufacturers,
            List<CabinetSectionCategoryInfo> cabinetCategories,
            IReadOnlyList<ProductGroupInfo> productGroups,
            IEnumerable<ProductInfo> existingProducts,
            IAppRules               appRules,
            out List<ProductInfo>   cabinetList)
        {
            cabinetList = null;

            parser.GetList("cabinet_sections", out JsonList cabinetsJsonList);
            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            Dictionary<string, List<string>> cabinetIssuesByManufacturer =
                new Dictionary<string, List<string>>();

            cabinetList = new List<ProductInfo>();
            List<string> cabinetIssues = new List<string>();

            List<(string modelId, string productName, string productId)> modelIdToCabinetName = new List<(string, string, string)>();

            const MelcherJsonValueTypeOption ForgivingJsonOptions =
                MelcherJsonValueTypeOption.AllowTypeConversion |
                MelcherJsonValueTypeOption.AllowNullValueTypes;

            foreach (JsonDict cabinetJson in cabinetsJsonList)
            {
                ProductInfo cabinet =
                    new ProductInfo()
                    {
                        doesExistInServerDashboard = true,
                        category = ProductCategory.CabinetSection
                    };

                cabinetIssues.Clear();
                bool productHasCriticalErrors = false;

                parser.Reset(cabinetJson)
                    .GetValue("id", out cabinet.id)
                    .GetValue("order", out cabinet.sortOrder, defaultValue: -1, options: MelcherJsonValueTypeOption.Lenient)
                    .GetValue("name", out cabinet.name)
                    .GetValue("description", out cabinet.description)
                    .GetValue("product_id", out cabinet.sku)
                    .GetValue("cabinet_model_id", out cabinet.visualId)
                    .GetValue("minimum_width", out string minWidth, defaultValue: null)
                    .GetValue("maximum_width", out string maxWidth, defaultValue: null)
                    .GetValue("customer_pricing", out cabinet.priceCustomer, ForgivingJsonOptions)
                    .GetValue("dealer_pricing", out cabinet.priceDealer, ForgivingJsonOptions)
                    .GetValue("manufacturer", out string manufacturerName)
                    .GetValue("group", out string productGroupName, defaultValue: null)
                    .GetValue("link", out cabinet.storeLink)
                    .GetValue("width", out string defaultCabinetWidth, defaultValue: null)
                    .GetValue("height", out string defaultCabinetHeight, defaultValue: null)
                    .GetValue("depth", out string defaultCabinetDepth, defaultValue: null)
                    .GetValue("cabinet_section_type", out string cabinetSectionCategoryName)
                    .GetValue("compatibility_id", out cabinet.compatibilityId, defaultValue: 0, options: MelcherJsonValueTypeOption.Lenient);

                if (parser.hasErrors)
                {
                    MelcherLog.LogErrorEditorOnly($"Error parsing cabinet. Cabinet SKU: '{cabinet.sku}', Cabinet Name: {cabinet.name}\n{ServerJson.CombineParserErrorsIntoException(parser)}");
                    continue;
                }

                if (cabinet.id < 0)
                {
                    int smallestId =
                        cabinetList
                            .Concat(existingProducts)
                            .Select(static productInfo => productInfo.id)
                            .Min();

                    cabinet.id = smallestId - 1;
                }

                {
                    if (CatalogLookups.FindManufacturerByName(manufacturers, manufacturerName, out ManufacturerInfo manufacturerInfo))
                    {
                        cabinet.manufacturerId = manufacturerInfo.id;
                    }
                    else
                    {
                        int smallestManufacturerId = 0;

                        if (manufacturers.Any())
                        {
                            smallestManufacturerId =
                                Math.Min(
                                    smallestManufacturerId,
                                    manufacturers.Min(x => x.id)
                                );
                        }

                        int newManufacturerId = smallestManufacturerId - 1;

                        manufacturers.Add(
                            new ManufacturerInfo()
                            {
                                id = newManufacturerId,
                                name = manufacturerName,
                            }
                        );

                        MelcherLog.LogWarning(
                            $"Adding unknown manufacturer '{manufacturerName}' ({newManufacturerId}) first seen on '{cabinet.name}' ({cabinet.id})"
                        );

                        cabinet.manufacturerId = newManufacturerId;
                    }

                    if (cabinet.manufacturerId < 0)
                    {
                        cabinetIssues.Add("Unknown manufacturer: " + manufacturerName);
                    }
                }

                if (!string.IsNullOrEmpty(productGroupName))
                {
                    if (CatalogLookups.FindProductGroupByName(productGroups, productGroupName, out ProductGroupInfo productGroup))
                    {
                        cabinet.groupId = productGroup.id;
                    }
                    else
                    {
                        cabinetIssues.Add("Unknown product group: " + productGroupName);
                    }
                }

                {
                    if (CatalogLookups.FindCabinetSectionCategoryByName(cabinetCategories, cabinetSectionCategoryName, out CabinetSectionCategoryInfo cabinetSectionCategory))
                    {
                        cabinet.cabinetSectionCategoryId = cabinetSectionCategory.id;
                    }
                    else
                    {
                        int smallestCabinetSectionCategoryId = 0;

                        if (cabinetCategories.Any())
                        {
                            smallestCabinetSectionCategoryId =
                                Math.Min(
                                    smallestCabinetSectionCategoryId,
                                    cabinetCategories.Min(x => x.id)
                                );
                        }

                        int newCabinetSectionCategoryId = smallestCabinetSectionCategoryId - 1;

                        cabinetCategories.Add(
                            new CabinetSectionCategoryInfo()
                            {
                                id = newCabinetSectionCategoryId,
                                name = cabinetSectionCategoryName,
                                sortOrder = 1000
                            }
                        );

                        MelcherLog.LogWarning(
                            $"Adding unknown cabinet section category '{cabinetSectionCategoryName}' ({newCabinetSectionCategoryId}) first seen on '{cabinet.name}' ({cabinet.id})"
                        );

                        cabinet.cabinetSectionCategoryId = newCabinetSectionCategoryId;
                    }

                    if (cabinet.cabinetSectionCategoryId < 0)
                    {
                        cabinetIssues.Add("Unknown cabinet section ID: " + cabinetSectionCategoryName);
                    }
                }

                if (!string.IsNullOrWhiteSpace(defaultCabinetWidth) &&
                    !Utility.MeasurementFormat.TryParseImperialString(
                        defaultCabinetWidth,
                        out cabinet.productSizeInMeters.x,
                        nameof(defaultCabinetWidth),
                        cabinetIssues)
                    )
                {
                    cabinetIssues.Add("Missing or invalid cabinet width: " + defaultCabinetWidth);
                }

                if (!string.IsNullOrWhiteSpace(defaultCabinetHeight) &&
                    !Utility.MeasurementFormat.TryParseImperialString(
                        defaultCabinetHeight,
                        out cabinet.productSizeInMeters.y,
                        nameof(defaultCabinetHeight),
                        cabinetIssues)
                    )
                {
                    cabinetIssues.Add("Missing or invalid cabinet height: " + defaultCabinetHeight);
                }

                if (!string.IsNullOrWhiteSpace(defaultCabinetDepth) &&
                    !Utility.MeasurementFormat.TryParseImperialString(
                        defaultCabinetDepth,
                        out cabinet.productSizeInMeters.z,
                        nameof(defaultCabinetDepth),
                        cabinetIssues)
                    )
                {
                    cabinetIssues.Add("Missing or invalid cabinet depth: " + defaultCabinetDepth);
                }

                if (string.IsNullOrEmpty(cabinet.visualId))
                {
                    cabinetIssues.Add("Missing visualId.");
                }

                if (cabinet.productPaddingInMeters.top      != 0.0f ||
                    cabinet.productPaddingInMeters.bottom   != 0.0f ||
                    cabinet.productPaddingInMeters.left     != 0.0f ||
                    cabinet.productPaddingInMeters.right    != 0.0f)
                {
                    MelcherLog.LogError(
                        $"Found non-zero product padding for product with " +
                        $"SKU {cabinet.sku}. This looks like the product's " +
                        $"padding is being assigned but the code that sets " +
                        $"the default values will stomp over it."
                    );
                }

                if (!string.IsNullOrWhiteSpace(minWidth))
                {
                    bool didParse =
                        Utility.MeasurementFormat.TryParseImperialString(
                            minWidth,
                            out float minWidthInMeters,
                            nameof(minWidth),
                            cabinetIssues
                        );

                    if (didParse)
                    {
                        cabinet.minWidthInMeters = minWidthInMeters;
                    }
                    else
                    {
                        cabinetIssues.Add($"Bad {nameof(minWidth)} format: " + minWidth);
                    }
                }

                if (!string.IsNullOrWhiteSpace(maxWidth))
                {
                    bool didParse =
                        Utility.MeasurementFormat.TryParseImperialString(
                            maxWidth,
                            out float maxWidthInMeters,
                            nameof(maxWidth),
                            cabinetIssues
                        );

                    if (didParse)
                    {
                        cabinet.maxWidthInMeters = maxWidthInMeters;
                    }
                    else
                    {
                        cabinetIssues.Add($"Bad {nameof(minWidth)} format: " + minWidth);
                    }
                }

                cabinet.productPaddingInMeters.top      = 0.0f;
                cabinet.productPaddingInMeters.bottom   = 0.0f;

                float occupantMinimumHorizontalPaddingInMeters = appRules.OccupantMinimumHorizontalPaddingInMeters;

                cabinet.productPaddingInMeters.left     = occupantMinimumHorizontalPaddingInMeters * 0.5f;
                cabinet.productPaddingInMeters.right    = occupantMinimumHorizontalPaddingInMeters * 0.5f;

                modelIdToCabinetName.Add(
                    (
                        cabinet.visualId,
                        cabinet.name,
                        cabinet.sku
                    )
                );

                if (cabinetIssues.Count > 0)
                {
                    string error = $"**[{cabinet.sku}]** {cabinet.name}\n\n * " + string.Join("\n * ", cabinetIssues) + "\n\n";

                    if (!cabinetIssuesByManufacturer.TryGetValue(manufacturerName, out List<string> manufacturerProductList))
                    {
                        manufacturerProductList = new List<string>();
                        cabinetIssuesByManufacturer.Add(manufacturerName, manufacturerProductList);
                    }

                    manufacturerProductList.Add(error);
                }

                cabinet.hasErrors = productHasCriticalErrors;
                cabinetList.Add(cabinet);
            }

#if UNITY_EDITOR
            if (cabinetIssuesByManufacturer.Count > 0)
            {
                var alphabetizedManufacturers = cabinetIssuesByManufacturer.Keys.OrderBy(x => x).ToList();

                StringBuilder stringBuilder = new StringBuilder();

                foreach (var manufacturer in alphabetizedManufacturers)
                {
                    stringBuilder.AppendLine("## " + manufacturer);

                    var issues = cabinetIssuesByManufacturer[manufacturer];
                    foreach (string issue in issues)
                    {
                        stringBuilder.AppendLine(issue);
                    }
                }

                string outPath =
                    ProFitUtility.Editor.OverwriteSpecificLogFile(
                        "issues-cabinets.md",
                        stringBuilder.ToString()
                    );

                MelcherLog.LogErrorEditorOnly("Wrote cabinet issues to: " + outPath);
            }
#endif

            if (cabinetList.Count <= 0)
            {
                return new Exception("Failed to parse cabinets");
            }

            return null;
        }

        public static Exception TryParseTags(MelcherJsonObjectParser parser, out List<ProductTagInfo> tags)
        {
            tags = new List<ProductTagInfo>();

            parser.GetList("tags", out JsonList tagsListJson);

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            List<Exception> tagIssues = new List<Exception>();

            foreach (JsonDict tagJson in tagsListJson)
            {
                parser.Reset(tagJson);

                Exception error = CatalogJson.TryParseTag(parser, out ProductTagInfo tag);

                if (error != null)
                {
                    tagIssues.Add(error);
                    continue;
                }

                tags.Add(tag);
            }

            if (tagIssues.Count > 0)
            {
                MelcherLog.LogError(
                    "Issues while parsing tags:\n" +
                        string.Join("\n", tagIssues.Select(issue => issue.Message))
                );
            }

            return null;
        }

        public static Exception TryParseTag(MelcherJsonObjectParser parser, out ProductTagInfo tag)
        {
            tag = default;

            parser
                .GetValue("id", out tag.id)
                .GetValue("name", out tag.name);

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            return null;
        }

        public static Exception TryParseCompatibilities(MelcherJsonObjectParser parser, out List<CabinetCompatibilityInfo> tags)
        {
            tags = new List<CabinetCompatibilityInfo>();

            parser.GetList("compatibilities", out JsonList compatibilitiesListJson);

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            List<Exception> compatibilityIssues = new List<Exception>();

            foreach (JsonDict tagJson in compatibilitiesListJson)
            {
                parser.Reset(tagJson);

                Exception error =
                    CatalogJson.TryParseCompatibility(
                        parser,
                        out CabinetCompatibilityInfo compatibility
                    );

                if (error != null)
                {
                    compatibilityIssues.Add(error);
                    continue;
                }

                tags.Add(compatibility);
            }

            if (compatibilityIssues.Count > 0)
            {
                MelcherLog.LogError(
                    "Issues while parsing compatibilities:\n" +
                        string.Join("\n", compatibilityIssues.Select(issue => issue.Message))
                );
            }

            return null;
        }

        public static Exception TryParseCompatibility(MelcherJsonObjectParser parser, out CabinetCompatibilityInfo compatibility)
        {
            compatibility = new CabinetCompatibilityInfo();

            parser
                .GetValue("id", out compatibility.id)
                .GetValue("name", out compatibility.name)
                .GetList("blacklisted", out JsonList denylist, errorIfNotFound: false)
                .GetList("whitelisted", out JsonList allowlist, errorIfNotFound: false);

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            if (denylist != null)
            {
                Exception error =
                    ReadElementsFromArray(parser, denylist, compatibility.productTagIdDenylist);

                if (error != null)
                {
                    return error;
                }
            }

            if (allowlist != null)
            {
                Exception error =
                    ReadElementsFromArray(parser, allowlist, compatibility.productTagIdAllowlist);

                if (error != null)
                {
                    return error;
                }
            }

            return null;

            static Exception ReadElementsFromArray(MelcherJsonObjectParser parser, JsonList jsonEntries, HashSet<int> readInto)
            {
                foreach (object arrayElement in jsonEntries)
                {
                    JsonDict entry = arrayElement as JsonDict;
                    if (entry == null)
                    {
                        continue;
                    }

                    parser.Reset(entry)
                        .GetValue("id", out int productTagId);

                    if (parser.hasErrors)
                    {
                        return ServerJson.CombineParserErrorsIntoException(parser);
                    }

                    readInto.Add(productTagId);
                }

                return null;
            }
        }
    }
}
