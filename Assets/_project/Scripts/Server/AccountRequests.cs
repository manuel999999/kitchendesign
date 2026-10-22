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
    public sealed class AccountRequests
    {
        private readonly ServerConnection _connection;

        public AccountRequests(ServerConnection connection)
        {
            this._connection = connection;
        }

        public MelcherWebCoroutine SignUp(
            UserType    userType,
            UserFilter  userFilter,
            string      businessName,
            string      contactFirstName,
            string      contactLastName,
            string      contactEmail,
            string      contactUrl,
            string      contactPhoneNumber,
            string      referrer,
            string      address,
            string      city,
            string      state,
            string      zipCode,
            string      signInEmail,
            string      password,
            string      confirmPassword,
            bool        didAcceptTerms,
            bool        suppressEmail,
            Action      onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (userFilter == UserFilter.None)
            {
                throw new ArgumentException($"User filter must not be {userFilter}.", nameof(userFilter));
            }

            if (contactEmail == null)
            {
                throw new ArgumentNullException(nameof(contactEmail));
            }
            else
            {
                MelcherLog.Log("contactEmail is not null");
            }

            const string EndPoint = "/api/users/create";

            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                { "type",                   userFilter.ToString().ToLower() },

                { "address",                address },
                { "city",                   city },
                { "state",                  state },
                { "zip_code",               zipCode },

                { "business_name",          businessName },
                { "contact_phone_number",   contactPhoneNumber },
                { "contact_first_name",     contactFirstName },
                { "contact_last_name",      contactLastName },
                { "contact_email",          contactEmail },
                { "contact_url",            contactUrl },
                { "email",                  signInEmail },
                { "confirm_email",          signInEmail },
                { "password",               password },
                { "confirm_password",       confirmPassword },

                { "trial",                  (userType == UserType.Trial ? 1 : 0).ToString() },
                { "terms",                  (didAcceptTerms ? 1 : 0).ToString() },
                { "suppress_email",         (suppressEmail ? 1 : 0).ToString() },
            };

            if (!string.IsNullOrWhiteSpace(referrer))
            {
                postData.Add("referrer", referrer);
            }

            return
                MelcherWebRequestBuilder.Post(this._connection.Host + EndPoint)
                    .AddUrlParameters(postData)
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

        public MelcherWebCoroutine LogIn(
            string                          signInEmail,
            string                          password,
            bool                            isOffline,
            Action<User>                    onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (isOffline)
            {
                JsonDict token = new Dictionary<string, object>()
                {
                    { "type", "bearer" },
                    { "token", "" },
                    { "expires_at", DateTime.UtcNow.AddDays(1.0).ToLongDateString() }
                };

                JsonDict simulatedResponse = new Dictionary<string, object>()
                {
                    { "status", 200 },
                    { "id", 0 },
                    { "pending", 0 },
                    { "valid", 1 },
                    { "active", 1 },
                    { "token", token }
                };

                ParseResponse(builder: null, error: null, new MelcherJsonValue(simulatedResponse));

                return default;
            }

            const string EndPoint = "/api/users/login";

            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                { "email", signInEmail },
                { "password", password }
            };

            return
                MelcherWebRequestBuilder.Post(this._connection.Host + EndPoint)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                User user = default;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    AccountJson.TryParseLogInResponse(parser, out user);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                if (isOffline)
                {
                    user.userType = UserType.Offline;
                }

                onSuccess?.Invoke(user);
            }
        }

        public MelcherWebCoroutine CheckWhiteLabelAuthorization(
            string webDomainOfApp,
            string distributorUrlParameter,
            Action<User> onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                { "domain", webDomainOfApp },
                { "distributor", distributorUrlParameter }
            };

            const string EndPoint = "/api/authorization/check";

            return
                MelcherWebRequestBuilder.Post(this._connection.Host + EndPoint)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                User user = default;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    AccountJson.TryParseLogInResponse(parser, out user);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(user);
            }
        }

        public MelcherWebCoroutine ResetPassword(string email, Action onSuccess, MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/users/forget-password";

            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                { "email", email }
            };

            return
                MelcherWebRequestBuilder.Post(this._connection.Host + EndPoint)
                    .AddUrlParameters(postData)
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

        public MelcherWebCoroutine GetProfile(
            string                          apiToken,
            bool                            isOffline,
            Action<UserProfile>             onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (isOffline)
            {
                JsonDict profile = new Dictionary<string, object>()
                {
                    { "address", "123 Fake St." },
                    { "city", "New Nowhere" },
                    { "state", "ZZ" },
                    { "zip_code", "none" },

                    { "business_name", "Guest Co." },
                    { "contact_first_name", "Anonymous" },
                    { "contact_last_name", "Guest" },
                    { "contact_email", "n/a" },
                    { "contact_phone_number", "" },

                    { "authorized_domain", null },
                    { "currency", "USD" }
                };

                JsonDict user = new Dictionary<string, object>()
                {
                    { "id", 0 },
                    { "email", "n/a" },
                    { "pending", 0 },
                    { "valid", 1 },
                    { "active", 1 },
                    { "trial", 0 },
                    { "trial_expire", 6000.0f },
                    { "type", "profit" },
                    { "profile", profile }
                };

                JsonDict simulatedResponse = new Dictionary<string, object>()
                {
                    { "status", 200 },
                    { "user", user }
                };

                ParseResponse(builder: null, error: null, jsonValue: new MelcherJsonValue(simulatedResponse));

                return default;
            }

            const string EndPoint = "/api/users/profile";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(apiToken));

            return
                MelcherWebRequestBuilder.Post(this._connection.Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                UserProfile userProfile = default;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    AccountJson.TryParseUserProfile(parser, isOffline, out userProfile);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(userProfile);
            }
        }

        public MelcherWebCoroutine UpdateProfile(
            User                            user,
            UserProfile                     profile,
            string                          password,
            string                          confirmPassword,
            Action<UserProfile>             onSuccess,
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

            const string EndPoint = "/api/users/{user_id}/update";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            JsonDict jsonPayload = new Dictionary<string, object>()
            {
                { "address",                profile.address },
                { "city",                   profile.city },
                { "state",                  profile.state },
                { "zip_code",               profile.zipCode },

                { "business_name",          profile.businessName },
                { "contact_phone_number",   profile.contactPhoneNumber },
                { "contact_first_name",     profile.contactFirstName },
                { "contact_last_name",      profile.contactLastName },
                { "contact_email",          profile.contactEmail },
                { "contact_url",            profile.contactUrl },
                { "email",                  profile.signInEmail },
                { "confirm_email",          profile.signInEmail },

                { "measurement_units",      profile.preferredUnits },
            };

            if (password != null)
            {
                jsonPayload.Add("password", password);
            }

            if (confirmPassword != null)
            {
                jsonPayload.Add("confirm_password", confirmPassword);
            }

            if (user.userType == UserType.Dealer)
            {
                jsonPayload.Add("manufacturers", profile.dealerSelectedManufacturerIds);
            }

            if (profile.manufacturerIdToPriceMarkupPercentage != null)
            {
                JsonList dealerMarkup = new List<object>();

                foreach (var kv in profile.manufacturerIdToPriceMarkupPercentage)
                {
                    dealerMarkup.Add(
                        new Dictionary<string, object>()
                        {
                            { "manufacturerId", kv.Key },
                            { "markupPercentage", kv.Value }
                        }
                    );
                }

                jsonPayload.Add("dealers_markup", dealerMarkup);
            }

            string url = this._connection.Host + EndPoint.Replace("{user_id}", user.id.ToString());

            return
                MelcherWebRequestBuilder.Patch(url)
                    .SetHeaders(headers)
                    .SetJsonPayload(new MelcherJsonValue(jsonPayload))
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                UserProfile userProfile = default;

                error ??=
                    ServerJson.ValidateJsonResponse(jsonValue, out parser) ??
                    AccountJson.TryParseUserProfile(parser, isOffline: profile.userType == UserType.Offline, out userProfile);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(userProfile);
            }
        }

        public MelcherWebCoroutine DeleteAccount(
            User                            user,
            string                          password,
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

            const string EndPoint = "/api/users";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(ServerJson.GetAuthorizationHeader(user.apiToken));

            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                { "password", password }
            };

            return
                MelcherWebRequestBuilder.Delete(this._connection.Host + EndPoint)
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

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke();
            }
        }
    }
}
