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
    public static class ClientJson
    {
        public static Exception TryParseClientList(MelcherJsonObjectParser parser, out List<UserClient> clientList)
        {
            clientList = null;

            parser.GetList("clients", out JsonList clientListJson);
            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            clientList = new List<UserClient>();

            List<Exception> clientIssues = new List<Exception>();

            foreach (JsonDict clientJson in clientListJson)
            {
                Exception error = ClientJson.TryParseUserClient(parser.Reset(clientJson), out UserClient client);

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

        public static Exception TryParseUserClient(MelcherJsonObjectParser parser, out UserClient client)
        {
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
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

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

                foreach (JsonDict quoteJson in quotesListJson)
                {
                    parser.Reset(quoteJson);

                    Exception error = QuoteJson.TryParseQuoteResponse(parser, out UserClientQuote quote);

                    if (error != null)
                    {
                        quoteIssues.Add(error);

                        continue;
                    }

                    client.quotes.Add(quote);
                }

                if (quoteIssues.Count > 0)
                {
                    MelcherLog.LogError($"Errors parsing quote list for client ID {client.id}");
                }
            }

            return null;
        }
    }
}
