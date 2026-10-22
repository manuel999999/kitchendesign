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
    public static class AccountJson
    {
        public static Exception TryParseLogInResponse(MelcherJsonObjectParser parser, out User user)
        {
            user = default;

            parser
                .GetValue("id", out user.id)
                .GetAndPushObject("token", out JsonDict tokenJson)
                    .GetValue("token", out user.apiToken)
                .PopObject();

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            return null;
        }

        public static Exception TryParseUserProfile(
            MelcherJsonObjectParser parser,
            bool isOffline,
            out UserProfile profile)
        {
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
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            if (userFilterName == null)
            {
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

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
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
                for (int i = 0; i < dealerSelectedManufacturerIdsJson.Count; ++i)
                {
                    parser
                        .PushObject(i, (JsonDict)dealerSelectedManufacturerIdsJson[i])
                            .GetValue("id", out int dealerSelectedManufacturerId)
                        .PopObject();

                    if (parser.hasErrors)
                    {
                        return ServerJson.CombineParserErrorsIntoException(parser);
                    }

                    profile.dealerSelectedManufacturerIds.Add(dealerSelectedManufacturerId);
                }
            }

            profile.adminSelectedManufacturerIds = new List<int>();

            if (parser.currentObject.TryGetValueFromJson("whitelisted_manufacturers", out JsonList adminSelectedManufacturerIdsJson))
            {
                for (int i = 0; i < adminSelectedManufacturerIdsJson.Count; ++i)
                {
                    parser
                        .PushObject(i, (JsonDict)adminSelectedManufacturerIdsJson[i])
                            .GetValue("id", out int adminSelectedManufacturerId)
                        .PopObject();

                    if (parser.hasErrors)
                    {
                        return ServerJson.CombineParserErrorsIntoException(parser);
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
                        return ServerJson.CombineParserErrorsIntoException(parser);
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
                        return ServerJson.CombineParserErrorsIntoException(parser);
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
    }
}
