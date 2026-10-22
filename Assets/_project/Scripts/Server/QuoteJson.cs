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
    public static class QuoteJson
    {
        public static Exception TryParseQuoteResponse(MelcherJsonObjectParser parser, out UserClientQuote userClientQuote)
        {
            userClientQuote = default;

            parser.GetValue("pdf", out userClientQuote.pdfEndpoint);

            if (parser.hasErrors)
            {
                MelcherLog.LogWarning(ServerJson.CombineParserErrorsIntoException(parser));
            }

            parser.ClearErrors()
                .GetValue("id", out userClientQuote.id)
                .GetValue("name", out userClientQuote.saveName)
                .GetValue("notes", out userClientQuote.saveNotes, defaultValue: "")
                .GetAndPushObject("data", out userClientQuote.saveData);

            if (parser.hasErrors)
            {
                return ServerJson.CombineParserErrorsIntoException(parser);
            }

            return null;
        }
    }
}
