namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using UnityEngine.Networking;

    using JsonDict = System.Collections.Generic.IDictionary<string, object>;
    using JsonList = System.Collections.Generic.IList<object>;

    /// <summary>
    /// API endpoints for the Pro-Fit server.
    /// </summary>
    public static class ProFitWeb
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public static string domain => ProFitGlobals.appProfile.serverDomain;

        public static string Host => (UseHttps ? "https://" : "http://") + domain;
        public static bool UseHttps = true;

        public static bool IsTestServer =>
            ProFitGlobals.appProfile.targetServerType == ServerType.Test;

        /// <summary>
        /// Relative path (from Resources folder) where the "fallback JSON" is
        /// stored. The Fallback JSON is created at editor-time and is just a
        /// cached server response. They contain the manufacturer and product
        /// list. The fallback data is used when the server can't be reached or
        /// when the user signs in as a guest (offline user).
        /// </summary>
        public const string FallbackTagsFileName = "tags";
        public const string FallbackCompatibilitiesFileName = "compatibilities";
        public const string FallbackProductListFileName = "products";
        public const string FallbackCabinetListFileName = "cabinets";
        public const string FallbackCabinetSectionCategoriesFileName = "cabinetCategories";
        public const string FallbackManufacturersFileName = "manufacturers";
        public const string FallbackProductGroupsFileName = "productGroups";
        public const string FallbackBrandCatalogsFileName = "brandCatalogs";

        public const string ProductOverridesListFileName = "productOverrides";

        /// <summary>
        /// The list of always-allowed domains that can host the WebGL build.
        /// Other domains can be added in the backend on a per-user basis. This
        /// is intended to prevent unauthorized re-hosting of the app.
        /// </summary>
        public static readonly ReadOnlyCollection<string> WhitelistedEmbedDomains =
            new List<string>()
            {
                ProFitUtility.GetBaseDomain(ProFitGlobals.appOptions.GetServerDomain(ServerType.Production)),
                ProFitUtility.GetBaseDomain(ProFitGlobals.appOptions.GetServerDomain(ServerType.Test)),
                "profitoutdoorliving.com",
                "3doutdoorkitchendesign.com",
                "stollkitchendesign.com",
                "geminikitchendesigner.com",
            }.AsReadOnly();

        // ====================================================================
        // Static functions
        // ====================================================================

        public static MelcherWebCoroutine SignUp(
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
                // The email must not be null for backwards compatibility.
                throw new ArgumentNullException(nameof(contactEmail));
            }
            else
            {
                MelcherLog.Log("contactEmail is not null");
            }

            const string EndPoint = "/api/users/create";

            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                // Inform the server what app created the user.
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
                { "confirm_email",          signInEmail },  // just repeat the email
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
                MelcherWebRequestBuilder.Post(Host + EndPoint)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                error ??= ValidateJsonResponse(jsonValue, out MelcherJsonObjectParser parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke();
            }
        }

        /// <summary>
        ///     Log a dealer user into the app.
        /// </summary>
        /// <param name="signInEmail">
        ///     The user's sign-in email address.
        /// </param>
        /// <param name="password">
        ///     The user's password.
        /// </param>
        /// <param name="isOffline">
        ///     When this argument is <see cref="true"/>, any <paramref
        ///     name="signInEmail"/> and <paramref name="password"/> will work
        ///     to log in but the user will be in offline-mode. This means all
        ///     web requests will be simulated and do some default action.
        /// </param>
        /// <param name="onSuccess"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public static MelcherWebCoroutine LogIn(
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
                    // TODO: format should match something like this: 2022-06-29T03:42:54.843+01:00
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
                MelcherWebRequestBuilder.Post(Host + EndPoint)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                User user = default;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseLogInResponse(parser, out user);

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

        public static Exception TryParseLogInResponse(MelcherJsonObjectParser parser, out User user)
        {
            // {
            //     "status": 200,
            //     "id": 514,
            //     "pending": 0,
            //     "valid": 1,
            //     "active": 1,
            //     "type": "profit",
            //     "token": {
            //         "type": "bearer",
            //         "token": "MzI0MA.zINBobfS1QidY9RuNyEZLOMe1wnynXN_AQNFeWozEJ3WJ9PlhEtNHQ7Rv_Ly",
            //         "expires_at": "2022-06-29T03:42:54.843+01:00"
            //     }
            // }

            user = default;

            parser
                .GetValue("id", out user.id)
                .GetAndPushObject("token", out JsonDict tokenJson)
                    .GetValue("token", out user.apiToken)
                .PopObject();

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            return null;
        }

        /// <summary>
        ///     This endpoint is used to check if a WebGL build is enabled as a
        ///     White Label build.
        ///     <br /><br />
        ///
        ///     The end point returns the dealer's user info if the paramters
        ///     are valid.
        /// </summary>
        /// <param name="webDomainOfApp">
        ///     The domain of the site where the app is running. E.g.,
        ///     "mycoolsite.com"
        /// </param>
        /// <param name="distributorUrlParameter">
        ///     The "distributor" URL parameter value that is found in the
        ///     embed code.
        /// </param>
        /// <param name="onSuccess"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public static MelcherWebCoroutine CheckWhiteLabelAuthorization(
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
                MelcherWebRequestBuilder.Post(Host + EndPoint)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                // Success:
                // {
                //     "status": 200,
                //     "id": 532,
                //     "token": {
                //         "type": "bearer",
                //         "token": "MzkxMg.L0ISXjhmPNFesHcfF4pFzJMPe1t6tvfhFjE2tBM6fSPde0GiONL6L2eUb9jJ",
                //         "expires_at": "2023-03-26T16:49:48.273+01:00"
                //     }
                // }
                //
                // Error:
                // {
                //     "status": 403,
                //     "error": "Unauthorized."
                // }

                MelcherJsonObjectParser parser = null;
                User user = default;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    // Re-use TryParseLogInResponse because it fetches the same two
                    // fields we are interested about here: 'id' and 'token.token'
                    TryParseLogInResponse(parser, out user);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(user);
            }
        }

        public static MelcherWebCoroutine ResetPassword(string email, Action onSuccess, MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/users/forget-password";

            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                { "email", email }
            };

            return
                MelcherWebRequestBuilder.Post(Host + EndPoint)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                error ??=
                    ValidateJsonResponse(jsonValue, out MelcherJsonObjectParser parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke();
            }
        }

        public static MelcherWebCoroutine GetProfile(
            string                          apiToken,
            bool                            isOffline,
            Action<UserProfile>             onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (isOffline)
            {
                // Create the placeholder data to pass
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
            headers.Add(GetAuthorizationHeader(apiToken));

            return
                MelcherWebRequestBuilder.Post(Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                UserProfile userProfile = default;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseUserProfile(parser, isOffline, out userProfile);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(userProfile);
            }
        }

        public static Exception TryParseUserProfile(
            MelcherJsonObjectParser parser,
            bool isOffline,
            out UserProfile profile)
        {
            // {
            //     "status": 200,
            //     "user": {
            //         "id": 903,
            //         "email": "dealer@fake.com",
            //         "pending": 0,
            //         "valid": 1,
            //         "active": 1,
            //         "trial": 1,
            //         "type": "profit",                                // or "stoll"
            //         "trial_expire": 161849.431,
            //         "profile": {
            //             "business_name": "Dealer",
            //             "contact_email": "dealer@fake.com",
            //             "contact_url": null,
            //             "contact_phone_number": "123",
            //             "contact_first_name": "john",
            //             "contact_last_name": "smith",
            //             "address": "123",
            //             "city": "big city",
            //             "state": "ny",
            //             "zip_code": "123",
            //             "authorized_domain": null,
            //             "white_label": 1,
            //             "currency": "USD",
            //             "manufacturers": [                           // will not exist for non-dealers
            //                 {
            //                     "id": 1,
            //                     "name": "",
            //                     "authorized_domain": null
            //                 }
            //             ],
            //             "whitelisted_manufacturers": [
            //             ],
            //             "dealers_markup": [                          // may not exist and may be empty
            //                 {
            //                     "manufacturerId": 1,
            //                     "markupPercentage": 10
            //                 }
            //             ]
            //             "catalogs": [                                // will be empty if all catalogs are allowed
            //                 {
            //                     "id": 4,
            //                     "name": "Challenger"
            //                 }
            //             ]
            //         }
            //     }
            // }

            profile = default;

            parser.GetAndPushObject("user", out var _)
                .GetValue("id", out profile.userId)
                .GetValue("email", out profile.signInEmail)
                .GetValue("pending", out int isPending)
                .GetValue("active", out int isActive)
                .GetValue("valid", out int isValid)
                .GetValue("trial", out int isTrial)
                .GetValue("trial_expire", out float secondsBeforeTrialExpires)
                .GetValue("type", out string userFilterName, defaultValue: null);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            if (userFilterName == null)
            {
                // Fallback to Pro-Fit.
                // TODO: Treat this as an error.
                profile.userFilter = UserFilter.ProFit;
            }
            else
            {
                try
                {
                    profile.userFilter = Enum.Parse<UserFilter>(userFilterName, ignoreCase: true);
                }
                catch (Exception e)
                {
                    return e;
                }
            }

            profile.isPending = isPending != 0;
            profile.isApproved = isValid != 0;
            profile.isActive = isActive != 0;
            profile.isTrial = isTrial != 0;

            if (secondsBeforeTrialExpires > 0.0f)
            {
                profile.trialExpireUtc = DateTime.UtcNow.AddSeconds(secondsBeforeTrialExpires);
            }

            parser.GetAndPushObject("profile",      out var _)
                .GetValue("address",                out profile.address)
                .GetValue("city",                   out profile.city)
                .GetValue("state",                  out profile.state)
                .GetValue("zip_code",               out profile.zipCode)
                .GetValue("business_name",          out profile.businessName)
                .GetValue("contact_first_name",     out profile.contactFirstName)
                .GetValue("contact_last_name",      out profile.contactLastName)
                .GetValue("contact_email",          out profile.contactEmail)
                .GetValue("contact_url",            out profile.contactUrl, defaultValue: null)
                .GetValue("contact_phone_number",   out profile.contactPhoneNumber)
                .GetValue("authorized_domain",      out profile.authorizedDomain)
                .GetValue("redirect_url",           out profile.whiteLabelReturnUrl, defaultValue: "")
                .GetValue("white_label",            out int isWhiteLabeled, defaultValue: 0, options: MelcherJsonValueTypeOption.Lenient)
                .GetValue("measurement_units",      out string measurementUnitsName, defaultValue: null)
                .GetValue("inches_only",            out int inchesOnly, defaultValue: 0)
                .GetValue("currency",               out string currencyName, defaultValue: null);
            //MelcherLog.Log($"Got redirect URL={profile.whiteLabelReturnUrl}", severity: MelcherLogSeverity.Verbose);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            if (string.IsNullOrEmpty(measurementUnitsName) ||
                !Enum.TryParse(measurementUnitsName, ignoreCase: true, out profile.preferredUnits))
            {
                profile.preferredUnits = MeasurementUnit.FeetAndInches;
            }

            if (string.IsNullOrEmpty(currencyName) ||
                !Enum.TryParse(currencyName, ignoreCase: true, out profile.currency))
            {
                profile.currency = Currency.USD;
            }

            profile.isWhiteLabeled = isWhiteLabeled != 0;
            profile.isInchesOnly = inchesOnly != 0;

            profile.dealerSelectedManufacturerIds = new List<int>();

            parser.GetList("manufacturers", out JsonList dealerSelectedManufacturerIdsJson, errorIfNotFound: false);
            if (dealerSelectedManufacturerIdsJson != null)
            {
                // Read list of whitelisted manufacturer ids. These filter
                // which products will appear in the designer section
                for (int i = 0; i < dealerSelectedManufacturerIdsJson.Count; ++i)
                {
                    parser
                        .PushObject(i, (JsonDict)dealerSelectedManufacturerIdsJson[i])
                            .GetValue("id", out int dealerSelectedManufacturerId)
                        .PopObject();

                    if (parser.hasErrors)
                    {
                        return CombineParserErrorsIntoException(parser);
                    }

                    profile.dealerSelectedManufacturerIds.Add(dealerSelectedManufacturerId);
                }
            }

            profile.adminSelectedManufacturerIds = new List<int>();

            if (parser.currentObject.TryGetValueFromJson("whitelisted_manufacturers", out JsonList adminSelectedManufacturerIdsJson))
            {
                // Read list of whitelisted manufacturer ids. These filter
                // which products will appear in the designer section
                for (int i = 0; i < adminSelectedManufacturerIdsJson.Count; ++i)
                {
                    parser
                        .PushObject(i, (JsonDict)adminSelectedManufacturerIdsJson[i])
                            .GetValue("id", out int adminSelectedManufacturerId)
                        .PopObject();

                    if (parser.hasErrors)
                    {
                        return CombineParserErrorsIntoException(parser);
                    }

                    profile.adminSelectedManufacturerIds.Add(adminSelectedManufacturerId);
                }
            }

            profile.adminSelectedBrandCatalogIds = new List<int>();

            parser.GetList("catalogs", out JsonList allowedBrandCatalogs, errorIfNotFound: false);
            if (allowedBrandCatalogs != null)
            {
                for (int i = 0; i < allowedBrandCatalogs.Count; ++i)
                {
                    parser
                        .PushObject(i, (JsonDict)allowedBrandCatalogs[i])
                            .GetValue("id", out int allowedBrandCatalogId)
                        .PopObject();

                    if (parser.hasErrors)
                    {
                        return CombineParserErrorsIntoException(parser);
                    }

                    profile.adminSelectedBrandCatalogIds.Add(allowedBrandCatalogId);
                }
            }

            profile.manufacturerIdToPriceMarkupPercentage = new ();

            parser.GetList("dealers_markup", out JsonList dealersMarkup, errorIfNotFound: false);
            if (dealersMarkup != null)
            {
                for (int i = 0; i < dealersMarkup.Count; ++i)
                {
                    parser
                        .PushObject(i, (JsonDict)dealersMarkup[i])
                            .GetValue("manufacturerId", out int manufacturerId)
                            .GetValue("markupPercentage", out decimal markupPercentage)
                        .PopObject();

                    if (parser.hasErrors)
                    {
                        return CombineParserErrorsIntoException(parser);
                    }

                    profile.manufacturerIdToPriceMarkupPercentage[manufacturerId] = markupPercentage;
                }
            }

            if (isOffline)
            {
                profile.userType = UserType.Offline;
            }
            else if (profile.isTrial)
            {
                profile.userType = UserType.Trial;
            }
            else
            {
                profile.userType = UserType.Dealer;
            }


            return null;
        }

        public static MelcherWebCoroutine UpdateProfile(
            User                            user,
            UserProfile                     profile,
            string                          password,           // optional
            string                          confirmPassword,    // optional
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

            // Convert the user's current info to post data
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
                { "confirm_email",          profile.signInEmail },  // just repeat the email

                { "measurement_units",      profile.preferredUnits },
            };

            // Attach password fields (optional)
            if (password != null)
            {
                jsonPayload.Add("password", password);
            }

            if (confirmPassword != null)
            {
                jsonPayload.Add("confirm_password", confirmPassword);
            }

            // Dealers have an optional manufacturer whitelist
            if (user.userType == UserType.Dealer)
            {
                jsonPayload.Add("manufacturers", profile.dealerSelectedManufacturerIds);
            }

            // Update user-specified price markups
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

            string url = Host + EndPoint.Replace("{user_id}", user.id.ToString());

            return
                MelcherWebRequestBuilder.Patch(url)
                    .SetHeaders(headers)
                    .SetJsonPayload(new MelcherJsonValue(jsonPayload))
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            // Copied from GetProfile()
            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                UserProfile userProfile = default;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseUserProfile(parser, isOffline: profile.userType == UserType.Offline, out userProfile);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(userProfile);
            }
        }

        /// <summary>
        /// Delete the user's account. This is permanent. The app should
        /// immediately return to the main menu when this happens.
        /// </summary>
        public static MelcherWebCoroutine DeleteAccount(
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

            IDictionary<string, string> postData = new Dictionary<string, string>()
            {
                { "password", password }
            };

            return
                MelcherWebRequestBuilder.Delete(Host + EndPoint)
                    .SetHeaders(headers)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ValidateJsonResponse(jsonValue, out parser);

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

        public static MelcherWebCoroutine GetClientList(
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Client List");

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Client List");

                MelcherJsonObjectParser parser = null;

                error ??= ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                error = TryParseClientList(parser, out List<UserClient> clientList);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(clientList);
            }
        }

        public static Exception TryParseClientList(MelcherJsonObjectParser parser, out List<UserClient> clientList)
        {
            clientList = null;

            // {
            //     "status": 200,
            //     "clients": [
            //         {
            //         "id": 221,
            //         "first_name": "Bob",
            //         "last_name": "Smith",
            //         "email": "bobby@smith.com",
            //         "phone_number": "123",
            //         "address": "main st",
            //         "city": "new york",
            //         "state": "ny",
            //         "zip_code": "1234",
            //         "quotes": []
            //         },
            //         {
            //         "id": 222,
            //         "first_name": "New",
            //         "last_name": "Man",
            //         "email": "newman@fake.com",
            //         "phone_number": "123",
            //         "address": "poplar st",
            //         "city": "boston",
            //         "state": "ma",
            //         "zip_code": "12345",
            //         "quotes": []
            //         }
            //     ]
            // }

            parser.GetList("clients", out JsonList clientListJson);
            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            clientList = new List<UserClient>();

            List<Exception> clientIssues = new List<Exception>();

            foreach (JsonDict clientJson in clientListJson)
            {
                Exception error = TryParseUserClient(parser.Reset(clientJson), out UserClient client);

                if (error != null)
                {
                    clientIssues.Add(error);
                    continue;
                }

                clientList.Add(client);
            }

            if (clientIssues.Count > 0)
            {
                MelcherLog.LogError("Errors parsing client list: " + string.Join("\n", clientIssues));
            }

            return null;
        }

        public static MelcherWebCoroutine GetClient(
            User                            user,
            int                             clientId,
            Action<UserClient>              onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/clients/{client_id}/get";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(GetAuthorizationHeader(user.apiToken));

            string url = Host + EndPoint.Replace("{client_id}", clientId.ToString());

            return
                MelcherWebRequestBuilder.Get(url)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.GetAndPushObject("client", out JsonDict clientJson);
                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = TryParseUserClient(parser, out UserClient userClient);
                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(userClient);
            }
        }

        public static Exception TryParseUserClient(MelcherJsonObjectParser parser, out UserClient client)
        {
            // {
            //     "status": 200,
            //     "client": {
            //         "id": 221,
            //         "first_name": "Bob",
            //         "last_name": "Smith",
            //         "email": "bobby@smith.com",
            //         "phone_number": "123",
            //         "address": "main st",
            //         "city": "new york",
            //         "state": "ny",
            //         "zip_code": "1234",
            //          "quotes": []        <----- This will not be here when a
            //     }                               client is first created
            // }

            client = default;

            parser.GetValue("id",           out client.id)
                .GetValue("first_name",     out client.firstName)
                .GetValue("last_name",      out client.lastName)
                .GetValue("email",          out client.email)
                .GetValue("phone_number",   out client.phoneNumber, defaultValue: "")
                .GetValue("address",        out client.address,     defaultValue: "")
                .GetValue("city",           out client.city,        defaultValue: "")
                .GetValue("state",          out client.state,       defaultValue: "")
                .GetValue("zip_code",       out client.zipCode,     defaultValue: "");

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            // Prevent null values. Phone numbers were being returned as null.
            // Realistically, these values should never be null, just empty
            // strings if they're not specified.
            client.firstName    = client.firstName      ?? "";
            client.lastName     = client.lastName       ?? "";
            client.email        = client.email          ?? "";
            client.phoneNumber  = client.phoneNumber    ?? "";
            client.address      = client.address        ?? "";
            client.city         = client.city           ?? "";
            client.zipCode      = client.zipCode        ?? "";

            client.quotes = new List<UserClientQuote>();
            parser.GetList("quotes", out JsonList quotesListJson, errorIfNotFound : false);

            if (quotesListJson != null && quotesListJson.Count > 0)
            {
                List<Exception> quoteIssues = new List<Exception>();

                // Parse the quotes
                foreach (JsonDict quoteJson in quotesListJson)
                {
                    parser.Reset(quoteJson);

                    Exception error = TryParseQuoteResponse(parser, out UserClientQuote quote);

                    if (error != null)
                    {
                        quoteIssues.Add(error);

                        continue;
                    }

                    client.quotes.Add(quote);
                }

                // Print the quote issues in bulk
                if (quoteIssues.Count > 0)
                {
                    MelcherLog.LogError($"Errors parsing quote list for client ID {client.id}");
                }
            }

            return null;
        }


        public static MelcherWebCoroutine CreateClient(
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

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
                MelcherWebRequestBuilder.Post(Host + EndPoint)
                    .SetHeaders(headers)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.TryGetAndPushObject("client", out JsonDict _);

                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, CombineParserErrorsIntoException(parser), jsonValue);

                    return;
                }

                error = TryParseUserClient(parser, out UserClient client);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(client);
            }
        }

        public static MelcherWebCoroutine UpdateClient(
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

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

            string url = Host + EndPoint.Replace("{client_id}", userClient.id.ToString());

            return
                MelcherWebRequestBuilder.Patch(url)
                    .SetHeaders(headers)
                    .AddUrlParameters(postData)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.TryGetAndPushObject("client", out JsonDict _);

                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = TryParseUserClient(parser, out UserClient client);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(client);
            }
        }

        public static MelcherWebCoroutine DeleteClient(
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

            string url =
                Host + EndPoint.Replace("{client_id}", clientId.ToString());

            return
                MelcherWebRequestBuilder.Delete(url)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                error ??= ValidateJsonResponse(jsonValue, out MelcherJsonObjectParser parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke();
            }
        }

        // ====================================================================
        // Get Manufacturers
        // ====================================================================

        public static MelcherWebCoroutine GetManufacturers(
            Action<List<ManufacturerInfo>>  onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/manufacturers/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Manufacturer List");

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Manufacturer List");

                MelcherJsonObjectParser parser = null;
                List<ManufacturerInfo> manufacturers = null;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseManufacturers(parser, out manufacturers);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Writing Fallback Manufacturer List");
                SaveFallbackJsonResource(
                    FallbackManufacturersFileName,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif  // if UNITY_EDITOR

                onSuccess?.Invoke(manufacturers);
            }
        }

        public static Exception TryParseManufacturers(MelcherJsonObjectParser parser, out List<ManufacturerInfo> manufacturers)
        {
            // {
            //     "status": 200,
            //     "manufacturers": [
            //         {
            //             "id": 1,
            //             "name": "",
            //             "order": 0
            //         },
            //         {
            //             "id": 2,
            //             "name": "ABC Stone",
            //             "order": 2,
            //             "type_id": null OR integer,
            //             "type": {        // This will not exist if type_id is null.
            //                 "id": 5,     // Same value as `type_id`.
            //                 "name": "Finishes",
            //                 "value": "Finishes"
            //             }
            //         },
            //         {
            //             "id": 3,
            //             "name": "Alfa"
            //             "order": 1,
            //         }
            //     ]
            // }

            manufacturers = new List<ManufacturerInfo>();

            parser.GetList("manufacturers", out JsonList manufacturerListJson);
            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            List<Exception> manufacturerIssues = new List<Exception>();

            foreach (JsonDict manufacturerJson in manufacturerListJson)
            {
                Exception error =
                    TryParseManufacturer(
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
            // {
            //     "id": 1,
            //     "name": "",
            //     "order": 1,
            //     "type_id": null OR integer,
            //     "type": {        // This will not exist if type_id is null or 0.
            //         "id": 5,     // Same value as `type_id`.
            //         "name": "Finishes",
            //         "value": "Finishes"
            //     },
            //     "door_styles": [
            //         {
            //             "id": 3,
            //             "manufacturer_id": null,     // not used by Unity app
            //             "name": "Hampton",
            //         }
            //     ]
            // }

            manufacturer = default;
            manufacturer.doorStyles = new();

            parser
                .GetValue("id", out manufacturer.id)
                .GetValue("name", out manufacturer.name)
                .GetValue("order", out manufacturer.order, defaultValue: -1, MelcherJsonValueTypeOption.Lenient); // there is a bug on the server where newly added items do not have an order

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            if (parser.TryGetAndPushObject("type", out JsonDict typeObject))
            {
                // Ensure the pushed "type" object is popped regardless of how the
                // function exits.
                using var autoPopTypeObject =
                    MelcherDeferred.Defer(
                        toCapture: parser,
                        onDispose: static parser => parser.PopObject()
                    );

                parser.GetValue("value", out string categoryName, defaultValue: null);

                if (parser.hasErrors)
                {
                    return CombineParserErrorsIntoException(parser);
                }

                // Convert the server-returned name to a product category.

                if (TryParseProductCategory(categoryName, out ProductCategory productCategory))
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
                        return CombineParserErrorsIntoException(parser);
                    }

                    manufacturer.doorStyles.Add(doorStyleInfo);
                }
            }

            return null;
        }

        /// <summary>
        ///     Convert a product category name (as returned by the server) into
        ///     its corresponding <see cref="ProductCategory"/> value.
        /// </summary>
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

        // ====================================================================
        // Get cabinet section categories
        // ====================================================================

        public static MelcherWebCoroutine GetCabinetSectionCategories(
            Action<List<CabinetSectionCategoryInfo>> onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/cabinet-sections-categories/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting cabinet categories");

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading cabinet categories");

                MelcherJsonObjectParser parser = null;
                List<CabinetSectionCategoryInfo> categories = null;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseCabinetSectionCategoryList(parser, out categories);

                if (error != null)
                {
                    onError.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Writing fallback cabinet section categories");
                SaveFallbackJsonResource(
                    FallbackCabinetSectionCategoriesFileName,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif  // if UNITY_EDITOR

                onSuccess?.Invoke(categories);
            }
        }

        public static Exception TryParseCabinetSectionCategoryList(
            MelcherJsonObjectParser parser,
            out List<CabinetSectionCategoryInfo> categories)
        {
            // {
            //   "status": 200,
            //   "cabinet_section_category": [
            //     {
            //       "id": 17,
            //       "name": "Grill & Burner Base"
            //     },
            //     {
            //       "id": 35,
            //       "name": "Side Burner Base"
            //     },
            //     {
            //       "id": 27,
            //       "name": "Power Burner Base"
            //     }
            //   ]
            // }

            categories = new ();

            parser.GetList("cabinet_section_category", out JsonList categoryListJson);
            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            for (int i = 0; i < categoryListJson.Count; i++)
            {
                JsonDict categoryJson = (JsonDict)categoryListJson[i];
                Exception error =
                    TryParseCabinetSectionCategory(
                        parser.Reset(categoryJson),
                        out CabinetSectionCategoryInfo category
                    );

                if (error != null)
                {
                    Debug.LogException(error);
                    continue;
                }

                // The sort order is encoded in the order the sections are
                // returned.
                category.sortOrder = i;

                categories.Add(category);
            }

            return null;
        }

        public static Exception TryParseCabinetSectionCategory(
            MelcherJsonObjectParser parser,
            out CabinetSectionCategoryInfo category)
        {
            // {
            //   "id": 17,
            //   "name": "Grill & Burner Base"
            // }

            category = default;

            parser
                .GetValue("id", out category.id)
                .GetValue("name", out category.name);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            return null;
        }

        // ====================================================================
        // Get Product Groups
        // ====================================================================

        public static MelcherWebCoroutine GetProductGroups(
            Action<List<ProductGroupInfo>>  onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/groups/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Product Groups");

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Product Groups");

                MelcherJsonObjectParser parser = null;
                List<ProductGroupInfo> productGroups = null;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseProductGroups(parser, out productGroups);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Writing Fallback Product Groups");
                SaveFallbackJsonResource(
                    FallbackProductGroupsFileName,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif  // if UNITY_EDITOR

                onSuccess?.Invoke(productGroups);
            }
        }

        public static Exception TryParseProductGroups(MelcherJsonObjectParser parser, out List<ProductGroupInfo> productGroups)
        {
            // {
            //     "status": 200,
            //     "groups": [
            //         {
            //             "id": 1,
            //             "name": ""
            //         },
            //         {
            //             "id": 2,
            //             "name": "Brick"
            //         },
            //         {
            //             "id": 3,
            //             "name": "Stucco"
            //         }
            //     ]
            // }

            productGroups = new List<ProductGroupInfo>();

            parser.GetList("groups", out JsonList manufacturerListJson);
            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            List<Exception> productGroupIssues = new List<Exception>();

            foreach (JsonDict manufacturerJson in manufacturerListJson)
            {
                Exception error =
                    TryParseProductGroup(
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
            // {
            //     "id": 1,
            //     "name": ""
            // }

            productGroup = default;

            parser.GetValue("id", out productGroup.id)
                .GetValue("name", out productGroup.name);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            return null;
        }


        // ====================================================================
        // Get Product List
        // ====================================================================

        public static MelcherWebCoroutine GetProductList(
            User                            user,
            List<ManufacturerInfo>          manufacturers,
            Action<List<ProductInfo>>       onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                Exception error = null;

                // Read the fallback product list
                string fallbackProductListPath =
                    ProFitUtility.GetFallbackJsonPath(
                        ProFitGlobals.appProfile,
                        FallbackProductListFileName
                    );

                TextAsset fallbackProductList = Resources.Load<TextAsset>(fallbackProductListPath);

                JsonDict response = null;

                List<ProductInfo> productList = null;

                if (fallbackProductList)
                {
                    response = (JsonDict)MiniJSON.Json.Deserialize(fallbackProductList.text);

                    error =
                        TryParseProductList(
                            new MelcherJsonObjectParser(response),
                            ProFitGlobals.manufacturers,
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Product List");

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Product List");

                MelcherJsonObjectParser parser = null;
                List<ProductInfo> productList = null;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseProductList(parser, manufacturers, out productList);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage
                    ?.Invoke("Writing Fallback Product List");

                SaveFallbackJsonResource(
                    FallbackProductListFileName,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif  // if UNITY_EDITOR

                onSuccess?.Invoke(productList);

            }   // void ParseResponse

        }

        /// <summary>
        ///     Extract a list of <see cref="ProductInfo"/>'s from a JSON
        ///     string. This expects <paramref name="parser"/> to be at an
        ///     object that contains an array called "components".
        /// </summary>
        public static Exception TryParseProductList(
            MelcherJsonObjectParser parser,
            List<ManufacturerInfo>  manufacturers,
            out List<ProductInfo>   productList)
        {
            productList = null;

            parser.GetList("components", out JsonList productsJsonList);
            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            Dictionary<string, List<string>> productIssuesByManufacturer =
                new Dictionary<string, List<string>>();

            productList = new List<ProductInfo>();
            List<string> productIssues = new List<string>();

            List<(string modelId, string productName, string productId)> modelIdToProductName = new List<(string, string, string)>();

            // The server can return pricing info that is numbers or null. Treat
            // null prices as being 0.
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
                    MelcherLog.LogErrorEditorOnly($"Error parsing product. SKU: '{product.sku}', Product Name: {product.name}\n{CombineParserErrorsIntoException(parser)}");
                    continue;
                }

                if (productTags != null)
                {
                    Exception error = TryParseTags(parser, out List<ProductTagInfo> tags);
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
                    // Lookup the manufacturer ID from the name
                    if (ProFitUtility.FindManufacturerByName(manufacturers, manufacturerName, out ManufacturerInfo manufacturerInfo))
                    {
                        product.manufacturerId = manufacturerInfo.id;
                    }
                    else
                    {
                        // Implicitly create entries for unknown manufacturers.

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
                    // Lookup the group ID from the name (if given)
                    if (ProFitUtility.FindProductGroupByName(productGroupName, out ProductGroupInfo productGroup))
                    {
                        product.groupId = productGroup.id;
                    }
                    else
                    {
                        productIssues.Add("Unknown product group: " + productGroupName);
                    }
                }

                // Convert product category name to its enum value. Some
                // category names differ between the app and server so we
                // fallback to manually pairing categories in those cases
                if (!TryParseProductCategory(productType, out product.category))
                {
                    productIssues.Add("Unknown product category: " + productType);
                    productHasCriticalErrors = true;
                }

                // Validate measurements
                if (ProFitUtility.DoesCategoryUseSizes(product.category))
                {
                    if (!string.IsNullOrWhiteSpace(productWidth) &&
                        !ProFitUtility.TryParseImperialString(
                            productWidth,
                            out product.productSizeInMeters.x,
                            nameof(productWidth),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid product width: " + productWidth);
                    }

                    if (!string.IsNullOrWhiteSpace(productHeight) &&
                        !ProFitUtility.TryParseImperialString(
                            productHeight,
                            out product.productSizeInMeters.y,
                            nameof(productHeight),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid overlap height: " + productHeight);
                    }

                    if (!string.IsNullOrWhiteSpace(productDepth) &&
                        !ProFitUtility.TryParseImperialString(
                            productDepth,
                            out product.productSizeInMeters.z,
                            nameof(productDepth),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid overlap depth: " + productDepth);
                    }

                    if (string.IsNullOrWhiteSpace(cutOutWidth) ||
                        !ProFitUtility.TryParseImperialString(
                            cutOutWidth,
                            out product.cutOutSizeInMeters.x,
                            nameof(cutOutWidth),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid cut-out width: " + cutOutWidth);
                        //productHasCriticalErrors = true;
                    }

                    if (string.IsNullOrWhiteSpace(cutOutHeight) ||
                        !ProFitUtility.TryParseImperialString(
                            cutOutHeight,
                            out product.cutOutSizeInMeters.y,
                            nameof(cutOutHeight),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid cut-out height: " + cutOutHeight);
                        //productHasCriticalErrors = true;
                    }

                    if (string.IsNullOrWhiteSpace(cutOutDepth) ||
                        !ProFitUtility.TryParseImperialString(
                            cutOutDepth,
                            out product.cutOutSizeInMeters.z,
                            nameof(cutOutDepth),
                            productIssues)
                        )
                    {
                        productIssues.Add("Missing or invalid cut-out depth: " + cutOutDepth);
                        //productHasCriticalErrors = true;
                    }
                }

                // For special case models that don't appear in the dashboard's
                // model catalog, we must manually assign the product visual
                // ID. These models don't appear in the dashboard because it
                // doesn't make sense for the client to add a custom
                // "backsplash product", etc.
                // TODO: For backings, is `visualId` even used anymore? We may
                //       just be forcing a model from a prefab. -Ryan
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

                // TODO: FIXME: We are not yet storing the product padding in
                //              in the product database. Therefore, we forcibly
                //              assign the default values here.
                if (product.productPaddingInMeters.top      != 0.0f ||
                    product.productPaddingInMeters.bottom   != 0.0f ||
                    product.productPaddingInMeters.left     != 0.0f ||
                    product.productPaddingInMeters.right    != 0.0f)
                {
                    // Catch if someone is trying to add and load the product
                    // padding from the server because the code below will
                    // interfere.
                    MelcherLog.LogError(
                        $"Found non-zero product padding for product with " +
                        $"SKU {product.sku}. This looks like the product's " +
                        $"padding is being assigned but the code that sets " +
                        $"the default values will stomp over it."
                    );
                }

                // For cabinets in Catalog building method, some cabinets have
                // pre-set min and max sizes.
                if (!string.IsNullOrWhiteSpace(minWidth))
                {
                    bool didParse =
                        ProFitUtility.TryParseImperialString(
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
                        ProFitUtility.TryParseImperialString(
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

                if(ProFitGlobals.isAppOptionStoll)
                {
                    product.productPaddingInMeters.left     = Const.StollOccupantMinimumHorizontalPaddingInMeters * 0.5f;
                    product.productPaddingInMeters.right    = Const.StollOccupantMinimumHorizontalPaddingInMeters * 0.5f;
                }
                else
                {
                    product.productPaddingInMeters.left     = Const.OccupantMinimumHorizontalPaddingInMeters * 0.5f;
                    product.productPaddingInMeters.right    = Const.OccupantMinimumHorizontalPaddingInMeters * 0.5f;
                }

                if (productTags != null)
                {
                    foreach (object tag in productTags)
                    {
                        product.productTagIds.Add(tag.GetHashCode());
                    }
                }

                // Add entry to list so we can output a file of the product ID
                // and associated model ID
                modelIdToProductName.Add(
                    (
                        product.visualId,
                        product.name,
                        product.sku
                    )
                );

                // Log all errors with this product as one message.
                if (productIssues.Count > 0)
                {
                    // Encode error using MarkDown. Easiest solution
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
            // Write a file containing all the product issues. This is useful
            // for the client for fixing product issues
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
#endif  // if UNITY_EDITOR

            //#if UNITY_EDITOR
            //            string text = "Model ID|Product ID|Product Name\n";
            //            foreach (var tuple in modelIdToProductName)
            //            {
            //                text += tuple.modelId + "|" + tuple.productId + "|" + tuple.productName + "\n";
            //            }
            //            System.IO.File.WriteAllText(
            //                System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "model-ids.csv")),
            //                text
            //            );
            //#endif  // if UNITY_EDITOR

            if (productList.Count <= 0)
            {
                return new Exception("Failed to parse products");
            }

            return null;
        }

        // ====================================================================
        // Get catalogs
        // ====================================================================

        public static MelcherWebCoroutine GetBrandCatalogs(
            Action<List<BrandCatalogInfo>>              onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/catalogs/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke(
                "Requesting Brand Catalogs"
            );

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
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
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseBrandCatalogs(parser, out brandCatalogs);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke(
                    "Writing Fallback Brand Catalogs"
                );

                SaveFallbackJsonResource(
                    FallbackBrandCatalogsFileName,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif  // if UNITY_EDITOR

                onSuccess?.Invoke(brandCatalogs);
            }
        }

        public static Exception TryParseBrandCatalogs(
            MelcherJsonObjectParser parser,
            out List<BrandCatalogInfo> brandCatalogs)
        {
            //  {
            //    "status": 200,
            //    "catalogs": [
            //      {
            //        "id": 5,
            //        "name": "Danver",
            //        "manufacturer_id": 82,
            //        "available_manufacturer_ids": [
            //          1,
            //          2
            //        ],
            //        "available_groups_ids": [
            //          1,
            //          2
            //        ],
            //        "starting_cabinet_ids": [
            //          233,
            //          229,
            //          229
            //        ],
            //        "available_cabinet_ids":
            //        {
            //          236,
            //          237,
            //          238,
            //          ...
            //        }
            //      },
            //      {
            //        "id": 4,
            //        "name": "Challenger",
            //        "manufacturer_id": 71,
            //        "available_manufacturer_ids": [
            //          1,
            //          2
            //        ],
            //        "available_groups_ids": [
            //          1,
            //          2
            //        ],
            //        "starting_cabinet_ids": [
            //          241,
            //          298,
            //          298
            //        ],
            //        "available_cabinet_ids":
            //        {
            //          400,
            //          401,
            //          402,
            //          ...
            //        }
            //      }
            //    ]
            //  }

            brandCatalogs = new List<BrandCatalogInfo>();

            parser.GetList("catalogs", out JsonList brandCatalogListJson);
            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            List<Exception> brandCatalogIssues = new List<Exception>();

            foreach (JsonDict brandCatalogJson in brandCatalogListJson)
            {
                Exception error =
                    TryParseBrandCatalog(
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
            //  {
            //    "id": 5,
            //    "name": "Danver",
            //    "manufacturer_id": 82,
            //    "available_manufacturer_ids": [
            //      1,
            //      2
            //    ],
            //    "available_groups_ids": [
            //      1,
            //      2
            //    ],
            //    "starting_cabinet_ids": [
            //      233,
            //      229,
            //      229
            //    ],
            //    "available_cabinet_ids":
            //    {
            //      236,
            //      237,
            //      238,
            //      ...
            //    }
            //  }

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
                return CombineParserErrorsIntoException(parser);
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

        // ====================================================================
        // Cabinet List
        // ====================================================================

        public static MelcherWebCoroutine GetCabinetList(
            User                            user,
            List<ManufacturerInfo>          manufacturers,
            List<CabinetSectionCategoryInfo> cabinetSectionCategories,
            Action<List<ProductInfo>>       onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            if (user.userType == UserType.Offline)
            {
                Exception error = null;

                // Read the fallback cabinet list
                string fallbackCabinetListPath =
                    ProFitUtility.GetFallbackJsonPath(
                        ProFitGlobals.appProfile,
                        FallbackCabinetListFileName
                    );

                TextAsset fallbackCabinetList =
                    Resources.Load<TextAsset>(fallbackCabinetListPath);

                JsonDict response = null;

                List<ProductInfo> cabinetList = null;

                if (fallbackCabinetList)
                {
                    response = (JsonDict)MiniJSON.Json.Deserialize(fallbackCabinetList.text);

                    error =
                        TryParseCabinetList(
                            new MelcherJsonObjectParser(response),
                            ProFitGlobals.manufacturers,
                            ProFitGlobals.cabinetSectionCategories,
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting Cabinet Section List");

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Reading Cabinet Section List");

                MelcherJsonObjectParser parser = null;
                List<ProductInfo> cabinetList = null;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseCabinetList(
                        parser,
                        manufacturers,
                        cabinetSectionCategories,
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

                SaveFallbackJsonResource(
                    FallbackCabinetListFileName,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif  // if UNITY_EDITOR

                onSuccess?.Invoke(cabinetList);

            }   // void ParseResponse

        }

        /// <summary>
        ///     Extract a list of <see cref="ProductInfo"/>'s from a JSON
        ///     string. This expects <paramref name="parser"/> to be at an
        ///     object that contains an array called "components".
        /// </summary>
        public static Exception TryParseCabinetList(
            MelcherJsonObjectParser parser,
            List<ManufacturerInfo>  manufacturers,
            List<CabinetSectionCategoryInfo> cabinetCategories,
            out List<ProductInfo>   cabinetList)
        {
            cabinetList = null;

            parser.GetList("cabinet_sections", out JsonList cabinetsJsonList);
            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            Dictionary<string, List<string>> cabinetIssuesByManufacturer =
                new Dictionary<string, List<string>>();

            cabinetList = new List<ProductInfo>();
            List<string> cabinetIssues = new List<string>();

            List<(string modelId, string productName, string productId)> modelIdToCabinetName = new List<(string, string, string)>();

            // The server can return pricing info that is numbers or null. Treat
            // null prices as being 0.
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
                    //.GetValue("type", out string productType)
                    .GetValue("width", out string defaultCabinetWidth, defaultValue: null)
                    .GetValue("height", out string defaultCabinetHeight, defaultValue: null)
                    .GetValue("depth", out string defaultCabinetDepth, defaultValue: null)
                    //.GetValue("cutout_width", out string cutOutWidth)
                    //.GetValue("cutout_height", out string cutOutHeight)
                    //.GetValue("cutout_depth", out string cutOutDepth)
                    .GetValue("cabinet_section_type", out string cabinetSectionCategoryName)
                    .GetValue("compatibility_id", out cabinet.compatibilityId, defaultValue: 0, options: MelcherJsonValueTypeOption.Lenient);

                if (parser.hasErrors)
                {
                    MelcherLog.LogErrorEditorOnly($"Error parsing cabinet. Cabinet SKU: '{cabinet.sku}', Cabinet Name: {cabinet.name}\n{CombineParserErrorsIntoException(parser)}");
                    continue;
                }

                if (cabinet.id < 0)
                {
                    // Ensure cabinets with negative IDs are assigned a unique
                    // ID so they can still be loaded reliably. Products with
                    // no IDs are used by Stoll because its cabinets database
                    // is stored client-side.
                    int smallestId =
                        cabinetList
                            .Concat(ProFitGlobals.sourceProducts)
                            .Select(static productInfo => productInfo.id)
                            .Min();

                    cabinet.id = smallestId - 1;
                }

                {
                    // Lookup the manufacturer ID from the name
                    if (ProFitUtility.FindManufacturerByName(manufacturers, manufacturerName, out ManufacturerInfo manufacturerInfo))
                    {
                        cabinet.manufacturerId = manufacturerInfo.id;
                    }
                    else
                    {
                        // Implicitly create entries for unknown manufacturers.
                        // These will be ID'd starting at -1 and counting down.

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
                    // Lookup the group ID from the name (if given)
                    if (ProFitUtility.FindProductGroupByName(productGroupName, out ProductGroupInfo productGroup))
                    {
                        cabinet.groupId = productGroup.id;
                    }
                    else
                    {
                        cabinetIssues.Add("Unknown product group: " + productGroupName);
                    }
                }

                {
                    // Lookup the cabinet category ID from the name
                    if (ProFitUtility.FindCabinetSectionCategoryByName(cabinetCategories, cabinetSectionCategoryName, out CabinetSectionCategoryInfo cabinetSectionCategory))
                    {
                        cabinet.cabinetSectionCategoryId = cabinetSectionCategory.id;
                    }
                    else
                    {
                        // Implicitly create entries for unknown categories.
                        // These will be ID'd starting at -1 and counting down.

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

                // Validate measurements
                if (!string.IsNullOrWhiteSpace(defaultCabinetWidth) &&
                    !ProFitUtility.TryParseImperialString(
                        defaultCabinetWidth,
                        out cabinet.productSizeInMeters.x,
                        nameof(defaultCabinetWidth),
                        cabinetIssues)
                    )
                {
                    cabinetIssues.Add("Missing or invalid cabinet width: " + defaultCabinetWidth);
                }

                if (!string.IsNullOrWhiteSpace(defaultCabinetHeight) &&
                    !ProFitUtility.TryParseImperialString(
                        defaultCabinetHeight,
                        out cabinet.productSizeInMeters.y,
                        nameof(defaultCabinetHeight),
                        cabinetIssues)
                    )
                {
                    cabinetIssues.Add("Missing or invalid cabinet height: " + defaultCabinetHeight);
                }

                if (!string.IsNullOrWhiteSpace(defaultCabinetDepth) &&
                    !ProFitUtility.TryParseImperialString(
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

                // TODO: FIXME: We are not yet storing the product padding in
                //              in the product database. Therefore, we forcibly
                //              assign the default values here.
                if (cabinet.productPaddingInMeters.top      != 0.0f ||
                    cabinet.productPaddingInMeters.bottom   != 0.0f ||
                    cabinet.productPaddingInMeters.left     != 0.0f ||
                    cabinet.productPaddingInMeters.right    != 0.0f)
                {
                    // Catch if someone is trying to add and load the product
                    // padding from the server because the code below will
                    // interfere.
                    MelcherLog.LogError(
                        $"Found non-zero product padding for product with " +
                        $"SKU {cabinet.sku}. This looks like the product's " +
                        $"padding is being assigned but the code that sets " +
                        $"the default values will stomp over it."
                    );
                }

                // For cabinets in Catalog building method, some cabinets have
                // pre-set min and max sizes.
                if (!string.IsNullOrWhiteSpace(minWidth))
                {
                    bool didParse =
                        ProFitUtility.TryParseImperialString(
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
                        ProFitUtility.TryParseImperialString(
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

                cabinet.productPaddingInMeters.top      = 0.0f;// Const.OccupantMinimumVerticalPaddingInMeters * 0.5f;
                cabinet.productPaddingInMeters.bottom   = 0.0f;// Const.OccupantMinimumVerticalPaddingInMeters * 0.5f;

                if(ProFitGlobals.isAppOptionStoll)
                {
                    cabinet.productPaddingInMeters.left     = Const.StollOccupantMinimumHorizontalPaddingInMeters * 0.5f;
                    cabinet.productPaddingInMeters.right    = Const.StollOccupantMinimumHorizontalPaddingInMeters * 0.5f;
                }
                else
                {
                    cabinet.productPaddingInMeters.left     = Const.OccupantMinimumHorizontalPaddingInMeters * 0.5f;
                    cabinet.productPaddingInMeters.right    = Const.OccupantMinimumHorizontalPaddingInMeters * 0.5f;
                }

                // Add entry to list so we can output a file of the product ID
                // and associated model ID
                modelIdToCabinetName.Add(
                    (
                        cabinet.visualId,
                        cabinet.name,
                        cabinet.sku
                    )
                );

                // Log all errors with this cabinet as one message.
                if (cabinetIssues.Count > 0)
                {
                    // Encode error using MarkDown. Easiest solution
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
            // Write a file containing all the cabinet issues. This is useful
            // for the client for fixing cabinet issues
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
#endif  // if UNITY_EDITOR

            //#if UNITY_EDITOR
            //            string text = "Model ID|Product ID|Product Name\n";
            //            foreach (var tuple in modelIdToProductName)
            //            {
            //                text += tuple.modelId + "|" + tuple.productId + "|" + tuple.productName + "\n";
            //            }
            //            System.IO.File.WriteAllText(
            //                System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "model-ids.csv")),
            //                text
            //            );
            //#endif  // if UNITY_EDITOR

            if (cabinetList.Count <= 0)
            {
                return new Exception("Failed to parse cabinets");
            }

            return null;
        }


        // ====================================================================
        //
        // ====================================================================

        public static MelcherWebCoroutine CreateQuote(
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

        public static MelcherWebCoroutine UpdateQuote(
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

        public delegate void CreateOrUpdateQuoteHandler(UserClientQuote newUserClientQuote);

        private static MelcherWebCoroutine CreateOrUpdateQuote(
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

                            return MelcherWebRequestBuilder.Patch(Host + endPoint);
                        }
                        else
                        {
                            const string EndPoint = "/api/quotes/create";

                            return MelcherWebRequestBuilder.Post(Host + EndPoint);
                        }
                    },
                    arg1: quoteId
                );

            requestBuilder.SetHeader(GetAuthorizationHeader(user.apiToken));

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
                    ValidateJsonResponse(jsonValue, out  parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.TryGetAndPushObject("quote", out JsonDict quoteJson);
                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = TryParseQuoteResponse(parser, out UserClientQuote userClientQuote);
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

                // TODO: Coordinate with Chao to switch to this version. We
                //       don't need to pass a file name because there isn't a
                //       physical file
                //formData.Add(
                //    new MultipartFormDataSection(
                //        name: fileNameWithoutExt,
                //        data: screenshot.EncodeToJPG(),
                //        contentType: "image/jpeg"
                //    )
                //);
            }
        }

        public static MelcherWebCoroutine DeleteQuote(
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

            string url =
                Host + EndPoint.Replace("{quote_id}", quoteId.ToString());

            return
                MelcherWebRequestBuilder.Delete(url)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                error ??=
                    ValidateJsonResponse(jsonValue, out MelcherJsonObjectParser parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke();
            }
        }

        private static Exception TryParseQuoteResponse(MelcherJsonObjectParser parser, out UserClientQuote userClientQuote)
        {
            // "quote": {
            //     "name": "Joey Moes - Straight - 2022-06-05 7:28 PM",
            //     "notes": "",                                             // This may not exist because it was added late.
            //     "data": {
            //         "islandType": "Straight",
            //         "islandDimensions": "180\"L x 36\"H x 28\"D",
            //         "cabinetFinish": "",
            //         "counterTopFinish": "",
            //         "deckFinish": "",
            //         "paverFinish": "",
            //         "modelSaveData": [
            //             {
            //                 "productID": "AAEP-26",
            //                 "productPosition": {
            //                     "x": 0,
            //                     "y": 0,
            //                     "z": 0
            //                 },
            //                 "cabinetSide": 0
            //             }
            //         ],
            //         "counterTopHeight": "1\"",
            //         "top_measurements": [
            //             {
            //                 "section": 0,
            //                 "entries": [
            //                     {
            //                         "inches": 24.4999981,
            //                         "product_id": "AAEP-26"
            //                     },
            //                     {
            //                         "inches": 152.70372,
            //                         "product_id": ""
            //                     }
            //                 ]
            //             }
            //         ],
            //         "front_measurements": [
            //             [
            //                 {
            //                     "inches": 180,
            //                     "product_id": ""
            //                 }
            //             ]
            //         ],
            //         "kitchen_layout_style": "Straight",
            //         "cabinets": [
            //             {
            //                 "size_in_meters_x": 0,
            //                 "occupants": []
            //             },
            //             {
            //                 "size_in_meters_x": 4.572,
            //                 "occupants": [
            //                     {
            //                         "product_id": "AAEP-26",
            //                         "meters_from_cabinet_min_x": 0
            //                     }
            //                 ]
            //             },
            //             {
            //                 "size_in_meters_x": 0,
            //                 "occupants": []
            //             }
            //         ],
            //         "base_cabinet_depth_in_meters": 0.7112,
            //         "countertop_thickness_in_meters": 0.0254,
            //         "islandEnum": 0,
            //         "backplashLeftActive": false,
            //         "backsplashUpperActive": false,
            //         "backsplashRightActive": false,
            //         "barTopLeftActive": false,
            //         "barTopUpperActive": false,
            //         "barTopRightActive": false,
            //         "leftLength": false,
            //         "upperLength": false,
            //         "rightLength": false,
            //         "cabinetDepthLeft": 0,
            //         "cabinetDepthUpper": 0,
            //         "cabinetDepthRight": 0,
            //         "cabinetDepthInitial": 0,
            //         "fileName": "Joey Moes - Straight - 2022-06-05 7:28 PM",
            //         "timeStamp": "06/06/2022 01:28:10"
            //     },
            //     "id": 589,
            //     "image": "1654478890_quote_589.jpg",
            //     "top_down_image": "1654478890_quote_589_top_down.jpg",
            //     "front_images": "1654478890_quote_589_front_image_1.jpg",
            //     "analytics_key": "ab8y3", // or null
            //     "user": {
            //         "id": 525,
            //         "email": "dealer@fake.com",
            //         "remember_me_token": null,
            //         "pending": 0,
            //         "valid": 1,
            //         "trial": 0,
            //         "trial_expire": 0,
            //         "created_at": "2022-05-25T23:19:01.000+01:00",
            //         "updated_at": "2022-05-25T23:20:03.000+01:00",
            //         "profile": {
            //             "id": 103,
            //             "account_id": 525,
            //             "business_name": "Dealer",
            //             "contact_email": "dealer@fake.com",
            //             "contact_phone_number": "123",
            //             "contact_first_name": "Steve",
            //             "contact_last_name": "Smith",
            //             "address": "123",
            //             "city": "big city",
            //             "state": "ny",
            //             "zip_code": "123",
            //             "authorized_domain": null,
            //             "created_at": "2022-05-25T23:19:01.000+01:00",
            //             "updated_at": "2022-06-02T21:46:21.000+01:00"
            //         }
            //     }
            // }

            userClientQuote = default;


            parser.GetValue("pdf", out userClientQuote.pdfEndpoint);

            if (parser.hasErrors)
            {
                MelcherLog.LogWarning(CombineParserErrorsIntoException(parser));
            }

            parser.ClearErrors()
                .GetValue("id", out userClientQuote.id)
                .GetValue("name", out userClientQuote.saveName)
                .GetValue("notes", out userClientQuote.saveNotes, defaultValue: "")
                .GetAndPushObject("data", out userClientQuote.saveData);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            return null;
        }

        public static MelcherWebCoroutine GetQuote(
            User                            user,
            int                             quoteId,
            Action<UserClientQuote>         onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/quotes/{quote_id}/get";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(GetAuthorizationHeader(user.apiToken));

            string url =
                Host + EndPoint.Replace("{quote_id}", quoteId.ToString());

            return
                MelcherWebRequestBuilder.Get(url)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.TryGetAndPushObject("quote", out JsonDict quoteJson);

                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = TryParseQuoteResponse(parser, out UserClientQuote userClientQuote);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                onSuccess?.Invoke(userClientQuote);
            }
        }

        public static MelcherWebCoroutine GetAllQuotes(
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
            headers.Add(GetAuthorizationHeader(user.apiToken));

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

                parser.GetList("quotes", out JsonList quoteListJson);

                if (parser.hasErrors)
                {
                    onError?.Invoke(builder, CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                List<UserClientQuote> quotes = new List<UserClientQuote>();
                List<Exception> quoteIssues = new List<Exception>();

                foreach (JsonDict quoteJson in quoteListJson)
                {
                    parser.Reset(quoteJson);

                    error = TryParseQuoteResponse(parser, out UserClientQuote userClientQuote);

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

                    //onError?.Invoke(request, error, jsonResult, callbackData);
                }

                onSuccess?.Invoke(quotes);
            }
        }

        // ====================================================================
        // Tags
        // ====================================================================

        public static MelcherWebCoroutine GetTags(
            Action<List<ProductTagInfo>> onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/tags/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Getting Tags");

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                List<ProductTagInfo> tags = null;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseTags(parser, out tags);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage
                    ?.Invoke("Writing Fallback Tags");

                SaveFallbackJsonResource(
                    FallbackTagsFileName,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif  // if UNITY_EDITOR

                onSuccess?.Invoke(tags);
            }
        }

        public static Exception TryParseTags(MelcherJsonObjectParser parser, out List<ProductTagInfo> tags)
        {
            // {
            //     "status": 200,
            //     "tags": [
            //         {
            //             "id": 2,
            //             "name": "Smoker"
            //         },
            //         {
            //             "id": 1,
            //             "name": "Undermount Sink"
            //         }
            //     ]
            // }

            tags = new List<ProductTagInfo>();

            parser.GetList("tags", out JsonList tagsListJson);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            List<Exception> tagIssues = new List<Exception>();

            foreach (JsonDict tagJson in tagsListJson)
            {
                parser.Reset(tagJson);

                Exception error = TryParseTag(parser, out ProductTagInfo tag);

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

                //onError?.Invoke(request, error, jsonResult, callbackData);
            }

            return null;
        }

        public static Exception TryParseTag(MelcherJsonObjectParser parser, out ProductTagInfo tag)
        {
            //  {
            //    "id": 2,
            //    "name": "Smoker"
            //  }

            tag = default;

            parser
                .GetValue("id", out tag.id)
                .GetValue("name", out tag.name);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            return null;
        }

        // ====================================================================
        // Compatibilities
        // ====================================================================

        public static MelcherWebCoroutine GetCompatibilities(
            Action<List<CabinetCompatibilityInfo>> onSuccess,
            MelcherWebRequestBuilder.OnRequestErrorJson onError)
        {
            const string EndPoint = "/api/compatibilities/all";

            EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Getting Compatibilities");

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;
                List<CabinetCompatibilityInfo> compatibilities = null;

                error ??=
                    ValidateJsonResponse(jsonValue, out parser) ??
                    TryParseCompatibilities(parser, out compatibilities);

                if (error != null)
                {
                    onError?.Invoke(builder, error, jsonValue);
                    return;
                }

#if UNITY_EDITOR
                EventBridge.Menu.setSpinnerStatusMessage
                    ?.Invoke("Writing Fallback Compatibilities");

                SaveFallbackJsonResource(
                    FallbackCompatibilitiesFileName,
                    MiniJSON.Json.Serialize(jsonValue.jsonObject, prettyPrint: true)
                );
#endif  // if UNITY_EDITOR

                onSuccess?.Invoke(compatibilities);
            }
        }

        public static Exception TryParseCompatibilities(MelcherJsonObjectParser parser, out List<CabinetCompatibilityInfo> tags)
        {
            //  {
            //      "status": 200,
            //      "compatibilities": [
            //          {
            //              "id": 2,
            //              "name": "Grill Cabinet Without Smokers",
            //              "blacklisted": [
            //                  {
            //                      "id": 2,
            //                      "name": "Smoker"
            //                  }
            //              ],
            //              "whitelisted": []
            //          },
            //          {
            //              "id": 1,
            //              "name": "Challenger Apron (Farmhouse) Sink Base",
            //              "blacklisted": [],
            //              "whitelisted": [
            //                  {
            //                      "id": 1,
            //                      "name": "Undermount Sink"
            //                  }
            //              ]
            //          }
            //      ]
            //  }

            tags = new List<CabinetCompatibilityInfo>();

            parser.GetList("compatibilities", out JsonList compatibilitiesListJson);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            List<Exception> compatibilityIssues = new List<Exception>();

            foreach (JsonDict tagJson in compatibilitiesListJson)
            {
                parser.Reset(tagJson);

                Exception error =
                    TryParseCompatibility(
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

                //onError?.Invoke(request, error, jsonResult, callbackData);
            }

            return null;
        }

        public static Exception TryParseCompatibility(MelcherJsonObjectParser parser, out CabinetCompatibilityInfo compatibility)
        {
            //  {
            //      "id": 2,
            //      "name": "Grill Cabinet Without Smokers",
            //      "blacklisted": [
            //          {
            //              "id": 2,
            //              "name": "Smoker"
            //          }
            //      ],
            //      "whitelisted": [
            //          {
            //              "id": 1,
            //              "name": "Undermount Sink"
            //          }
            //      ]
            //  }

            compatibility = new CabinetCompatibilityInfo();

            parser
                .GetValue("id", out compatibility.id)
                .GetValue("name", out compatibility.name)
                .GetList("blacklisted", out JsonList denylist, errorIfNotFound: false)
                .GetList("whitelisted", out JsonList allowlist, errorIfNotFound: false);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
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

                    //  {
                    //      "id": 1,
                    //      "name": "Undermount Sink"
                    //  }

                    parser.Reset(entry)
                        .GetValue("id", out int productTagId);

                    if (parser.hasErrors)
                    {
                        return CombineParserErrorsIntoException(parser);
                    }

                    readInto.Add(productTagId);
                }

                return null;
            }
        }


        // ====================================================================
        //
        // ====================================================================

        public delegate void FindClientByEmailHandler(bool foundClient, UserClient client);

        public static MelcherWebCoroutine FindClientByEmail(User user, string clientEmail, FindClientByEmailHandler onComplete)
        {
            if (user.userType == UserType.Offline)
            {
                onComplete.Invoke(false, client: default);

                return default;
            }

            const string EndPoint = "/api/clients/find/{email}";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(GetAuthorizationHeader(user.apiToken));

            string url = Host + EndPoint.Replace("{email}", clientEmail);

            return
                MelcherWebRequestBuilder.Get(url)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    ForwardError(builder, error, jsonValue);
                    return;
                }

                parser.GetAndPushObject("client", out JsonDict clientJson);
                if (parser.hasErrors)
                {
                    ForwardError(builder, CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                error = TryParseUserClient(parser, out UserClient client);

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

        // ====================================================================
        // Analytics
        // ====================================================================

        public static MelcherWebCoroutine RecordAnalytics(
            User                        user,
            string                      analyticsKey,
            IEnumerable<int>            selectedProductIds,
            System.TimeSpan             sessionDesignTime,
            System.Action<bool>         onComplete)
        {
            if (user.userType == UserType.Offline)
            {
                // Offline users don't affect analytics.
                onComplete.Invoke(true);

                return default;
            }

            const string EndPoint = "/api/analytics/snapshot";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(GetAuthorizationHeader(user.apiToken));

            JsonDict jsonPayload = new Dictionary<string, object>()
            {
                { "analytics_key", analyticsKey },
                { "added_design_seconds", sessionDesignTime.TotalSeconds },
                { "product_ids", selectedProductIds },
            };

            return
                MelcherWebRequestBuilder.Post(Host + EndPoint)
                    .SetHeaders(headers)
                    .SetJsonPayload(new MelcherJsonValue(jsonPayload))
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ValidateJsonResponse(jsonValue, out parser);

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

        public static MelcherWebCoroutine GetIdempotencyToken(User user, System.Action<string> onComplete)
        {
            if (user.userType == UserType.Offline)
            {
                // No idempotency token.
                onComplete.Invoke(null);

                return default;
            }

            const string EndPoint = "/api/analytics/token";

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(GetAuthorizationHeader(user.apiToken));

            return
                MelcherWebRequestBuilder.Get(Host + EndPoint)
                    .SetHeaders(headers)
                    .OnJsonCompleted(ParseResponse)
                    .Send();

            void ParseResponse(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
            {
                MelcherJsonObjectParser parser = null;

                error ??= ValidateJsonResponse(jsonValue, out parser);

                if (error != null)
                {
                    ForwardError(builder, error, jsonValue);
                    return;
                }

                parser.GetValue("token", out string token, defaultValue: default);

                if (parser.hasErrors)
                {
                    ForwardError(builder, CombineParserErrorsIntoException(parser), jsonValue);
                    return;
                }

                onComplete?.Invoke(token);
            }

            void ForwardError(MelcherWebRequestBuilder builder, Exception error, MelcherJsonValue jsonValue)
            {
                // No idempotency token.
                onComplete?.Invoke(null);
            }
        }

        // ====================================================================
        // Shared utility functions
        // ====================================================================

        private static KeyValuePair<string, string> GetAuthorizationHeader(string apiToken)
        {
            return new KeyValuePair<string, string>("Authorization", "Bearer " + apiToken);
        }

        /// <summary>
        ///     Handle our common-case JSON validation. Ensures the response
        ///     is a JSON object and has a 200 status code.
        /// </summary>
        private static Exception ValidateJsonResponse(MelcherJsonValue jsonValue, out MelcherJsonObjectParser parser)
        {
            parser = new MelcherJsonObjectParser(jsonValue.jsonObject);

            if (!jsonValue.isObject)
            {
                return new InvalidOperationException("Expected JSON object.");
            }

            if (parser.currentObject.TryGetValueFromJson("error", out string errorMessage))
            {
                return new InvalidOperationException(errorMessage);
            }

            Exception error = null;

            if (parser.TryGetErrorMap("errors", out Dictionary<string, List<string>> errorMap))
            {
                // Make a human readable message out of the error map
                string combinedErrorMessage = "";
                foreach (var errorList in errorMap)
                {
                    if (Application.isEditor || MelcherWebRequestBuilder.runtimeDebug)
                    {
                        // We only log the error's key name in debug builds
                        // because they're less readable/more obtuse to a
                        // general app user
                        combinedErrorMessage += errorList.Key + "\n";
                    }

                    combinedErrorMessage += string.Join("\n", errorList.Value);
                }

                if (!string.IsNullOrEmpty(combinedErrorMessage))
                {
                    return new Exception(combinedErrorMessage);
                }

                // This is a pretty weak error message. We'll set it up as the
                // fallback in case nothing better shows up
                error = new Exception("Encountered a server response error.");
            }

            parser.GetValue("status", out int status);

            if (parser.hasErrors)
            {
                return CombineParserErrorsIntoException(parser);
            }

            if (status != ResponseCode.OK)
            {
                return new Exception("JSON status code " + status);
            }

            return error;
        }

        public static Exception CombineParserErrorsIntoException(MelcherJsonObjectParser parser)
        {
            Exception combinedError = null;

            if (parser.hasErrors)
            {
                s_CombinedErrorBuilder.Clear();

                foreach (MelcherJsonKeyResult error in parser.errors)
                {
                    s_CombinedErrorBuilder.AppendLine(error.exception.Message);
                }

                combinedError = new Exception(s_CombinedErrorBuilder.ToString());
            }

            return combinedError;
        }

#if UNITY_EDITOR
        private static void SaveFallbackJsonResource(string fileNameNoExt, string contents)
        {
            string resourcePath =
                ProFitUtility.GetFallbackJsonPath(
                    ProFitGlobals.appProfile,
                    fileNameNoExt
                );

            string assetPath =
                Path.Combine(
                    "Assets/AssetsMelcher/Resources/",
                    resourcePath + ".json"
                );

            UnityEditor.AssetDatabase.DeleteAsset(assetPath);

            // Ensure the destination folder exists.
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

            // Write the file using System classes. Unity's
            // AssetDatabase.CreateAsset doesn't do what I expect. (It makes a
            // YAML file with text data.)
            string projectRoot =
                Path.GetFullPath(
                    Path.Combine(Application.dataPath, "..")
                );
            string fullAssetPath = Path.Combine(projectRoot, assetPath);
            File.WriteAllText(fullAssetPath, contents);

            UnityEditor.AssetDatabase.ImportAsset(assetPath);
            UnityEditor.AssetDatabase.SaveAssets();

            MelcherLog.Log($"Wrote fallback JSON '{fileNameNoExt}': {fullAssetPath}");
        }
#endif  // if UNITY_EDITOR

        // =====================================================================
        // Static fields
        // =====================================================================

        private static readonly StringBuilder s_CombinedErrorBuilder = new StringBuilder();

    }   // class ProFitWeb

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-27
/// ===========================================================================
