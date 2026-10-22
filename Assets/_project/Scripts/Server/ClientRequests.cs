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
    public sealed class ClientRequests
    {
        private readonly ServerConnection _connection;

        public ClientRequests(ServerConnection connection)
        {
            this._connection = connection;
        }

        public MelcherWebCoroutine GetClientList(
            User                            user,
            Action<List<UserClient>>        onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/clients/all";

            if (user.userType == UserType.Offline)
            {
                JsonList clients = new List<object>();

                JsonDict simulatedResponse = new Dictionary<string, object>()
                {
                    { "status", 200 },
                    { "clients", clients }
                };

                ParseResponse(builder: null, error: null, new MelcherJsonValue(simulatedResponse));

                return default;
            }

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Client List");

            return
                MelcherWebRequestBuilder.Get(this._connection.Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Client List");

                MelcherJsonObjectParser parser = null;

                error ??= ServerJson.ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                error = ClientJson.TryParseClientList(parser, out List<UserClient> clientList);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(clientList);
            }
        }

        public MelcherWebCoroutine GetClient(
            User                            user,
            int                             clientId,
            Action<UserClient>              onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/clients/{client_id}/get";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            string url = this._connection.Host + EndPoint.Replace("{client_id}", clientId.ToString());

            return
                MelcherWebRequestBuilder.Get(url)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ServerJson.ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.GetAndPushObject("client", out JsonDict clientJson);
                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, ServerJson.CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = ClientJson.TryParseUserClient(parser, out UserClient userClient);
                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(userClient);
            }
        }

        public MelcherWebCoroutine CreateClient(
            User                user,
            string              firstName,
            string              lastName,
            string              email,
            string              unformattedPhoneNumber,
            string              address,
            string              city,
            string              state,
            string              zipCode,
            bool                suppressEmail,
            Action<UserClient>  onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                JsonList quotes = new List<object>();

                JsonDict client = new Dictionary<string, object>()
                {
                    { "id",             0 },
                    { "first_name",     firstName },
                    { "last_name",      lastName },
                    { "email",          email },
                    { "phone_number",   unformattedPhoneNumber },
                    { "address",        address },
                    { "city",           city},
                    { "state",          state },
                    { "zip_code",       zipCode },
                    { "quotes",         quotes }
                };

                JsonDict simulatedResponse = new Dictionary<string, object>()
                {
                    { "status", 200 },
                    { "client", client }
                };

                ParseResponse(builder: null, error: null, new MelcherJsonValue(simulatedResponse));

                return default;
            }

            const string EndPoint = "/api/clients/create";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                { "first_name",     firstName },
                { "last_name",      lastName },
                { "email",          email},
                { "phone_number",   unformattedPhoneNumber },
                { "address",        address },
                { "city",           city },
                { "state",          state },
                { "zip_code",       zipCode }
            };

            return
                MelcherWebRequestBuilder.Post(this._connection.Host + EndPoint)
                    .SetHeaders(headers)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ServerJson.ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.TryGetAndPushObject("client", out JsonDict _);

                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, ServerJson.CombineParserErrorsIntoException(parser), jsonValue);

                    return;
                }

                error = ClientJson.TryParseUserClient(parser, out UserClient client);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(client);
            }
        }

        public MelcherWebCoroutine UpdateClient(
            User                            user,
            UserClient                      userClient,
            Action<UserClient>              onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                JsonDict client = new Dictionary<string, object>()
                {
                    { "id",             userClient.id },
                    { "first_name",     userClient.firstName },
                    { "last_name",      userClient.lastName },
                    { "email",          userClient.email },
                    { "phone_number",   userClient.phoneNumber },
                    { "address",        userClient.address },
                    { "city",           userClient.city},
                    { "state",          userClient.state },
                    { "zip_code",       userClient.zipCode },
                    { "quotes",         userClient.quotes }
                };

                JsonDict simulatedResponse = new Dictionary<string, object>()
                {
                    { "status", 200 },
                    { "client", client }
                };

                ParseResponse(builder: null, error: null, new MelcherJsonValue(simulatedResponse));

                return default;
            }

            const string EndPoint = "/api/clients/{client_id}/update";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                { "first_name",     userClient.firstName },
                { "last_name",      userClient.lastName },
                { "email",          userClient.email },
                { "phone_number",   userClient.phoneNumber },
                { "address",        userClient.address },
                { "city",           userClient.city },
                { "state",          userClient.state },
                { "zip_code",       userClient.zipCode }
            };

            string url = this._connection.Host + EndPoint.Replace("{client_id}", userClient.id.ToString());

            return
                MelcherWebRequestBuilder.Patch(url)
                    .SetHeaders(headers)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ServerJson.ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.TryGetAndPushObject("client", out JsonDict _);

                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, ServerJson.CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = ClientJson.TryParseUserClient(parser, out UserClient client);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(client);
            }
        }

        public MelcherWebCoroutine DeleteClient(
            User                            user,
            int                             clientId,
            Action                          onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                JsonDict simulatedResponse = new Dictionary<string, object>()
                {
                    { "status", 200 }
                };

                ParseResponse(builder: null, error: null, new MelcherJsonValue(simulatedResponse));

                return default;
            }

            const string EndPoint = "/api/clients/{client_id}";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            string url =
                this._connection.Host + EndPoint.Replace("{client_id}", clientId.ToString());

            return
                MelcherWebRequestBuilder.Delete(url)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                error ??= ServerJson.ValidateJsonResponse(jsonValue, out MelcherJsonObjectParser parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke();
            }
        }

        public MelcherWebCoroutine FindClientByEmail(User user, string clientEmail, FindClientByEmailHandler onComplete)
        {
            if (user.userType == UserType.Offline)
            {
                onComplete.Invoke(false, client: default);

                return default;
            }

            const string EndPoint = "/api/clients/find/{email}";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            string url = this._connection.Host + EndPoint.Replace("{email}", clientEmail);

            return
                MelcherWebRequestBuilder.Get(url)
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

                parser.GetAndPushObject("client", out JsonDict clientJson);
                if (parser.hasErrors)
                {
                    ForwardError(builder, ServerJson.CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = ClientJson.TryParseUserClient(parser, out UserClient client);

                if (error != null)
                {
                    ForwardError(builder, error, jsonValue);
                    return;
                }

                onComplete?.Invoke(foundClient: true, client);
            }

            void ForwardError(MelcherWebRequestBuilder builder, Exception error, MelcherJsonValue jsonValue)
            {
                onComplete?.Invoke(foundClient: false, client: default);
            }
        }
    }
}
