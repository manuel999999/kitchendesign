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
    public sealed class CatalogRequests
    {
        private readonly ServerConnection _connection;
        private readonly IAppRules _appRules;

        public CatalogRequests(ServerConnection connection, IAppRules appRules)
        {
            this._connection = connection;
            this._appRules = appRules;
        }

        public MelcherWebCoroutine GetManufacturers(
            Action<List<ManufacturerInfo>>  onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/manufacturers/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Manufacturer List");

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Manufacturer List");

                MelcherJsonObjectParser parser = null;
                List<ManufacturerInfo> manufacturers = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    CatalogJson.TryParseManufacturers(parser, out manufacturers);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Writing Fallback Manufacturer List");
                this._connection.SaveFallbackJsonResource(
                    FallbackJsonFiles.Manufacturers,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif

                onSuccess?.Invoke(manufacturers);
            }
        }

        public MelcherWebCoroutine GetCabinetSectionCategories(
            Action<List<CabinetSectionCategoryInfo>> onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/cabinet-sections-categories/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting cabinet categories");

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading cabinet categories");

                MelcherJsonObjectParser parser = null;
                List<CabinetSectionCategoryInfo> categories = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    CatalogJson.TryParseCabinetSectionCategoryList(parser, out categories);

                if (error != null)
                {
                    onError.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Writing fallback cabinet section categories");
                this._connection.SaveFallbackJsonResource(
                    FallbackJsonFiles.CabinetSectionCategories,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif

                onSuccess?.Invoke(categories);
            }
        }

        public MelcherWebCoroutine GetProductGroups(
            Action<List<ProductGroupInfo>>  onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/groups/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Product Groups");

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Product Groups");

                MelcherJsonObjectParser parser = null;
                List<ProductGroupInfo> productGroups = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    CatalogJson.TryParseProductGroups(parser, out productGroups);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Writing Fallback Product Groups");
                this._connection.SaveFallbackJsonResource(
                    FallbackJsonFiles.ProductGroups,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif

                onSuccess?.Invoke(productGroups);
            }
        }

        public MelcherWebCoroutine GetProductList(
            User                            user,
            List<ManufacturerInfo>          manufacturers,
            Func<IReadOnlyList<ProductGroupInfo>> productGroups,
            Action<List<ProductInfo>>       onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                Exception error = null;

                string fallbackProductListPath =
                    this._connection.AppProfile.GetFallbackJsonPath(
                        FallbackJsonFiles.Products
                    );

                TextAsset fallbackProductList = Resources.Load<TextAsset>(fallbackProductListPath);

                JsonDict response = null;

                List<ProductInfo> productList = null;

                if (fallbackProductList)
                {
                    response = (JsonDict)MiniJSON.Json.Deserialize(fallbackProductList.text);

                    error =
                        CatalogJson.TryParseProductList(
                            new MelcherJsonObjectParser(response),
                            manufacturers,
                            productGroups(),
                            this._appRules,
                            out productList
                        );
                }
                else
                {
                    error = new Exception("Failed to load fallback product list.");
                }

                if (error != null)
                {
                    onError?.Invoke(builder: null, error: error, jsonResponse: new MelcherJsonValue(response));
                }
                else
                {
                    onSuccess?.Invoke(productList);
                }

                return default;
            }

            const string EndPoint = "/api/components/all";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Product List");

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Product List");

                MelcherJsonObjectParser parser = null;
                List<ProductInfo> productList = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    CatalogJson.TryParseProductList(parser, manufacturers, productGroups(), this._appRules, out productList);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage
                    ?.Invoke("Writing Fallback Product List");

                this._connection.SaveFallbackJsonResource(
                    FallbackJsonFiles.Products,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif

                onSuccess?.Invoke(productList);
            }
        }

        public MelcherWebCoroutine GetBrandCatalogs(
            Action<List<BrandCatalogInfo>>              onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/catalogs/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke(
                "Requesting Brand Catalogs"
            );

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke(
                    "Reading Brand Catalogs"
                );

                MelcherJsonObjectParser parser = null;
                List<BrandCatalogInfo> brandCatalogs = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    CatalogJson.TryParseBrandCatalogs(parser, out brandCatalogs);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke(
                    "Writing Fallback Brand Catalogs"
                );

                this._connection.SaveFallbackJsonResource(
                    FallbackJsonFiles.BrandCatalogs,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif

                onSuccess?.Invoke(brandCatalogs);
            }
        }

        public MelcherWebCoroutine GetCabinetList(
            User                            user,
            List<ManufacturerInfo>          manufacturers,
            List<CabinetSectionCategoryInfo> cabinetSectionCategories,
            Func<IReadOnlyList<ProductGroupInfo>> productGroups,
            Func<IEnumerable<ProductInfo>> existingProducts,
            Action<List<ProductInfo>>       onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                Exception error = null;

                string fallbackCabinetListPath =
                    this._connection.AppProfile.GetFallbackJsonPath(
                        FallbackJsonFiles.Cabinets
                    );

                TextAsset fallbackCabinetList =
                    Resources.Load<TextAsset>(fallbackCabinetListPath);

                JsonDict response = null;

                List<ProductInfo> cabinetList = null;

                if (fallbackCabinetList)
                {
                    response = (JsonDict)MiniJSON.Json.Deserialize(fallbackCabinetList.text);

                    error =
                        CatalogJson.TryParseCabinetList(
                            new MelcherJsonObjectParser(response),
                            manufacturers,
                            cabinetSectionCategories,
                            productGroups(),
                            existingProducts(),
                            this._appRules,
                            out cabinetList
                        );
                }
                else
                {
                    error = new Exception("Failed to load fallback cabinet list.");
                }

                if (error != null)
                {
                    onError?.Invoke(builder: null, error: error, jsonResponse: new MelcherJsonValue(response));
                }
                else
                {
                    onSuccess?.Invoke(cabinetList);
                }

                return default;
            }

            const string EndPoint = "/api/cabinet-sections/all";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Cabinet Section List");

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Cabinet Section List");

                MelcherJsonObjectParser parser = null;
                List<ProductInfo> cabinetList = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    CatalogJson.TryParseCabinetList(
                        parser,
                        manufacturers,
                        cabinetSectionCategories,
                        productGroups(),
                        existingProducts(),
                        this._appRules,
                        out cabinetList
                    );

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage
                    ?.Invoke("Writing Fallback Cabinet List");

                this._connection.SaveFallbackJsonResource(
                    FallbackJsonFiles.Cabinets,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif

                onSuccess?.Invoke(cabinetList);
            }
        }

        public MelcherWebCoroutine GetTags(
            Action<List<ProductTagInfo>> onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/tags/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Getting Tags");

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                List<ProductTagInfo> tags = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    CatalogJson.TryParseTags(parser, out tags);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage
                    ?.Invoke("Writing Fallback Tags");

                this._connection.SaveFallbackJsonResource(
                    FallbackJsonFiles.Tags,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif

                onSuccess?.Invoke(tags);
            }
        }

        public MelcherWebCoroutine GetCompatibilities(
            Action<List<CabinetCompatibilityInfo>> onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/compatibilities/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Getting Compatibilities");

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                List<CabinetCompatibilityInfo> compatibilities = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    CatalogJson.TryParseCompatibilities(parser, out compatibilities);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage
                    ?.Invoke("Writing Fallback Compatibilities");

                this._connection.SaveFallbackJsonResource(
                    FallbackJsonFiles.Compatibilities,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif

                onSuccess?.Invoke(compatibilities);
            }
        }
    }
}
