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
    public static class ServerJson
    {
        private static readonly StringBuilder s_CombinedErrorBuilder = new StringBuilder();

        public static KeyValuePair<string, string> GetAuthorizationHeader(string apiToken)
        {
            return new KeyValuePair<string, string>("Authorization", "Bearer " + apiToken);
        }

        public static Exception ValidateJsonResponse(MelcherJsonValue jsonValue, out MelcherJsonObjectParser parser)
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
                string combinedErrorMessage = "";
                foreach (var errorList in errorMap)
                {
                    if (Application.isEditor || MelcherWebRequestBuilder.runtimeDebug)
                    {
                        combinedErrorMessage += errorList.Key + "\n";
                    }

                    combinedErrorMessage += string.Join("\n", errorList.Value);
                }

                if (!string.IsNullOrEmpty(combinedErrorMessage))
                {
                    return new Exception(combinedErrorMessage);
                }

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
    }
}
