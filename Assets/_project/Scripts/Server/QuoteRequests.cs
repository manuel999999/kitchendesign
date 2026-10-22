using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;
using Melcher;
using UnityEngine;
using UnityEngine.Networking;

using JsonDict = System.Collections.Generic.IDictionary<string, object>;
using JsonList = System.Collections.Generic.IList<object>;

namespace KitchenDesign.Server
{
    public sealed class QuoteRequests
    {
        private readonly ServerConnection _connection;

        public QuoteRequests(ServerConnection connection)
        {
            this._connection = connection;
        }

        public MelcherWebCoroutine CreateQuote(
            User                    user,
            int                     clientId,
            string                  saveName,
            string                  saveNotes,
            JsonDict                saveData,
            byte[]                  coverPhoto,
            byte[]                  topDownPhoto,
            List<byte[]>            frontPhotos,
            byte[]                  topDownPlanPhoto,
            List<byte[]>            frontPlanPhotos,
            CreateOrUpdateQuoteHandler onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            return CreateOrUpdateQuote(
                user,
                clientId,
                quoteId: null,
                saveName,
                saveNotes,
                saveData,
                coverPhoto,
                topDownPhoto,
                frontPhotos,
                topDownPlanPhoto,
                frontPlanPhotos,
                onSuccess,
                onError
            );
        }

        public MelcherWebCoroutine UpdateQuote(
            User                    user,
            int                     clientId,
            int                     quoteId,
            string                  saveName,
            string                  saveNotes,
            JsonDict                saveData,
            byte[]                  coverPhoto,
            byte[]                  topDownPhoto,
            List<byte[]>            frontPhotos,
            byte[]                  topDownPlanPhoto,
            List<byte[]>            frontPlanPhotos,
            CreateOrUpdateQuoteHandler onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            return CreateOrUpdateQuote(
                user,
                clientId,
                quoteId,
                saveName,
                saveNotes,
                saveData,
                coverPhoto,
                topDownPhoto,
                frontPhotos,
                topDownPlanPhoto,
                frontPlanPhotos,
                onSuccess,
                onError
            );
        }

        private MelcherWebCoroutine CreateOrUpdateQuote(
            User                    user,
            int                     clientId,
            int?                    quoteId,
            string                  saveName,
            string                  saveNotes,
            JsonDict                saveData,
            byte[]                  coverPhoto,
            byte[]                  topDownPhoto,
            List<byte[]>            frontPhotos,
            byte[]                  topDownPlanPhoto,
            List<byte[]>            frontPlanPhotos,
            CreateOrUpdateQuoteHandler onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                onError?.Invoke(builder: null, error: new Exception("Offline users can't create quotes."), jsonResponse: default);

                return default;
            }

            MelcherWebRequestBuilder requestBuilder =
                MelcherCommon.Invoke(
                    quoteId =>
                    {
                        if (quoteId.HasValue)
                        {
                            string endPoint =
                                "/api/quotes/{quote_id}/update"
                                    .Replace(
                                        "{quote_id}",
                                        quoteId.Value.ToString()
                                    );

                            return MelcherWebRequestBuilder.Patch(this._connection.Host + endPoint);
                        }
                        else
                        {
                            const string EndPoint = "/api/quotes/create";

                            return MelcherWebRequestBuilder.Post(this._connection.Host + EndPoint);
                        }
                    },
                    arg1: quoteId
                );

            requestBuilder.SetHeader(ServerJson.GetAuthorizationHeader(user.apiToken));

            string data = MiniJSON.Json.Serialize(saveData);

            requestBuilder
                .AddFormData("name", saveName)
                .AddFormData("data", data)
                .AddFormData("client_id", clientId.ToString());

            if (!string.IsNullOrWhiteSpace(saveNotes))
            {
                requestBuilder.AddFormData("notes", saveNotes);
            }

            if (Application.isEditor || Debug.isDebugBuild)
            {
                requestBuilder
                    .AddFormData("suppress_email", "1");
            }

            AddScreenshotIfExists(requestBuilder, coverPhoto, fieldName: "image", fileNameWithoutExt: "screenshot");
            AddScreenshotIfExists(requestBuilder, topDownPhoto, fieldName: "top_down_image", fileNameWithoutExt: "top_down");

            if (frontPhotos != null)
            {
                int frontPhotoCount = frontPhotos.Count;
                if (frontPhotoCount == 1)
                {
                    AddScreenshotIfExists(requestBuilder, frontPhotos[0], fieldName: "front_image", fileNameWithoutExt: "front");
                }
                else
                {
                    for (int i = 0; i < frontPhotoCount; ++i)
                    {
                        AddScreenshotIfExists(requestBuilder, frontPhotos[i], fieldName: "front_images", fileNameWithoutExt: $"front_{i}");
                    }
                }
            }

            AddScreenshotIfExists(requestBuilder, topDownPlanPhoto, fieldName: "top_down_image_plan", fileNameWithoutExt: "top_down_plan");

            if (frontPlanPhotos != null)
            {
                int frontPlanPhotoCount = frontPlanPhotos.Count;
                if (frontPlanPhotoCount == 1)
                {
                    AddScreenshotIfExists(requestBuilder, frontPlanPhotos[0], fieldName: "front_image_plan", fileNameWithoutExt: "front_plan");
                }
                else
                {
                    for (int i = 0; i < frontPlanPhotoCount; ++i)
                    {
                        AddScreenshotIfExists(requestBuilder, frontPlanPhotos[i], fieldName: "front_images_plan", fileNameWithoutExt: $"front_plan_{i}");
                    }
                }
            }

            return requestBuilder
                .OnJsonCompleted(ParseResponse)
                .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out  parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.TryGetAndPushObject("quote", out JsonDict quoteJson);
                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, ServerJson.CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = QuoteJson.TryParseQuoteResponse(parser, out UserClientQuote userClientQuote);
                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(userClientQuote);
            }

            void AddScreenshotIfExists(MelcherWebRequestBuilder builder, byte[] screenshotData, string fieldName, string fileNameWithoutExt)
            {
                if (screenshotData == null || screenshotData.Length <= 0)
                {
                    return;
                }

                builder.AddFormData(
                    new MultipartFormFileSection(
                        name: fieldName,
                        data: screenshotData,
                        fileName: fileNameWithoutExt + ".jpg",
                        contentType: "image/jpeg"
                    )
                );
            }
        }

        public MelcherWebCoroutine DeleteQuote(
            User                            user,
            int                             quoteId,
            Action                          onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                JsonDict simulatedResponse = new Dictionary<string, object>()
                {
                    { "status", 200 }
                };

                ParseResponse(builder: null, error: null, jsonValue: new MelcherJsonValue(simulatedResponse));

                return default;
            }

            const string EndPoint = "/api/quotes/{quote_id}";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            string url =
                this._connection.Host + EndPoint.Replace("{quote_id}", quoteId.ToString());

            return
                MelcherWebRequestBuilder.Delete(url)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out MelcherJsonObjectParser parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke();
            }
        }

        public MelcherWebCoroutine GetQuote(
            User                            user,
            int                             quoteId,
            Action<UserClientQuote>         onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/quotes/{quote_id}/get";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            string url =
                this._connection.Host + EndPoint.Replace("{quote_id}", quoteId.ToString());

            return
                MelcherWebRequestBuilder.Get(url)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.TryGetAndPushObject("quote", out JsonDict quoteJson);

                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, ServerJson.CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = QuoteJson.TryParseQuoteResponse(parser, out UserClientQuote userClientQuote);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(userClientQuote);
            }
        }

        public MelcherWebCoroutine GetAllQuotes(
            User user,
            Action<List<UserClientQuote>> onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                onSuccess?.Invoke(new List<UserClientQuote>());

                return default;
            }

            const string EndPoint = "/api/quotes/all";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.GetList("quotes", out JsonList quoteListJson);

                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, ServerJson.CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                List<UserClientQuote> quotes = new List<UserClientQuote>();
                List<Exception> quoteIssues = new List<Exception>();

                foreach (JsonDict quoteJson in quoteListJson)
                {
                    parser.Reset(quoteJson);

                    error = QuoteJson.TryParseQuoteResponse(parser, out UserClientQuote userClientQuote);

                    if (error != null)
                    {
                        quoteIssues.Add(error);
                        continue;
                    }

                    quotes.Add(userClientQuote);
                }

                if (quoteIssues.Count > 0)
                {
                    MelcherLog.LogError(
                        "Issues while parsing quotes:\n" +
                            string.Join("\n", quoteIssues.Select(issue => issue.Message))
                    );
                }

                onSuccess?.Invoke(quotes);
            }
        }
    }
}
