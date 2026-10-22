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
    public sealed class AnalyticsRequests
    {
        private readonly ServerConnection _connection;

        public AnalyticsRequests(ServerConnection connection)
        {
            this._connection = connection;
        }

        public MelcherWebCoroutine RecordAnalytics(
            User                        user,
            string                      analyticsKey,
            IEnumerable<int>            selectedProductIds,
            System.TimeSpan             sessionDesignTime,
            System.Action<bool>         onComplete)
        {
            if (user.userType == UserType.Offline)
            {
                onComplete.Invoke(true);

                return default;
            }

            const string EndPoint = "/api/analytics/snapshot";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            JsonDict jsonPayload = new Dictionary<string, object>()
            {
                { "analytics_key", analyticsKey },
                { "added_design_seconds", sessionDesignTime.TotalSeconds },
                { "product_ids", selectedProductIds },
            };

            return
                MelcherWebRequestBuilder.Post(this._connection.Host + EndPoint)
                    .SetHeaders(headers)
                    .SetJsonPayload(new MelcherJsonValue(jsonPayload))
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ServerJson.ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    ForwardError(builder, error, jsonValue);
                    return;
                }

                onComplete?.Invoke(true);
            }

            void ForwardError(MelcherWebRequestBuilder builder, Exception error, MelcherJsonValue jsonValue)
            {
                onComplete?.Invoke(false);
            }
        }

        public MelcherWebCoroutine GetIdempotencyToken(User user, System.Action<string> onComplete)
        {
            if (user.userType == UserType.Offline)
            {
                onComplete.Invoke(null);

                return default;
            }

            const string EndPoint = "/api/analytics/token";

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

                error ??= ServerJson.ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    ForwardError(builder, error, jsonValue);
                    return;
                }

                parser.GetValue("token", out string token, defaultValue: default);

                if (parser.hasErrors)
                {
                    ForwardError(builder, ServerJson.CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                onComplete?.Invoke(token);
            }

            void ForwardError(MelcherWebRequestBuilder builder, Exception error, MelcherJsonValue jsonValue)
            {
                onComplete?.Invoke(null);
            }
        }
    }
}
