namespace Melcher
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEngine.Networking;

    // Logging behaviours
    public partial class MelcherWebRequestBuilder
    {
        // =====================================================================
        // Static functions
        // =====================================================================

        /// <summary>
        ///     Log a message about the start of a web request. The exact output
        ///     depends on whether the app is running the editor or not.
        ///     <br /><br />
        ///
        ///     This is intended to be called just before the web request is
        ///     dispatched to make a record of what was requested.
        ///     <br /><br />
        ///
        ///     An ID is generated for the request and returned via the
        ///     <paramref name="requestId"/> argument. This ID is later reused
        ///     in the corresponding <see cref="LogRequestEnd(string,
        ///     MelcherWebRequestBuilder, System.Exception)"/> function to help pair
        ///     the two messages.
        ///     <br /><br />
        ///
        ///     The logged message will always includes the request's ID, the
        ///     request method and the URL.
        ///     <br /><br />
        ///
        ///     If the app is running in the editor, all of the headers and data
        ///     will be logged too. Otherwise, no header or data is logged. This
        ///     is to ensure sensitive user data is not leaked. If runtime
        ///     logging is needed by the user, it must be implement manually.
        /// </summary>
        private static void LogRequestStart(
            MelcherWebRequestBuilder    builder,
            out string                  requestId)
        {
            MelcherLog.Assert(builder != null);
            MelcherLog.Assert(builder.request != null);

            requestId = s_NextRequestId.ToString();

            // Colorize the request ID. This is done to further help pair the
            // start and end log messages. The colours are reused cyclically.
            int colorIndex = s_NextRequestId % s_RequestColorHexCodes.Count;
            requestId = $"<color={s_RequestColorHexCodes[colorIndex]}>[#{requestId}]</color>";

            ++s_NextRequestId;

            string cleanUrl = builder.request.url;

#if !UNITY_EDITOR
            if (!runtimeDebug)
            {
                // Strip URL parameters in release builds to protect the user's
                // privacy. URL parameters could contain sensitive information like
                // a password.
                int urlParameterIndex = cleanUrl.IndexOf("?");
                if (urlParameterIndex >= 0)
                {
                    cleanUrl = cleanUrl.Substring(0, urlParameterIndex) + $"?<URL parameters hidden>";
                }
            }
#endif  // if !UNITY_EDITOR

            s_VerboseMessageBuilder.Clear();
            s_VerboseMessageBuilder.AppendLine($"{requestId} Sent {builder.request.method} to {cleanUrl}");

            if (UnityEngine.Application.isEditor || runtimeDebug)
            {
                // Within the editor, more information is logged to help with
                // debugging during development. This extra information logging is
                // limited to the editor to ensure no secrets are leaked.

                if (builder.headers.Count > 0)
                {
                    // Include request headers in the logged output

                    s_VerboseMessageBuilder.AppendLine();
                    s_VerboseMessageBuilder.AppendLine(FormatStringAsLogTitle("Request headers:"));
                    foreach (KeyValuePair<string, string> header in builder.headers)
                    {
                        s_VerboseMessageBuilder.Append(s_DictionaryKeyGlyph);
                        s_VerboseMessageBuilder.AppendLine(header.Key);

                        s_VerboseMessageBuilder.Append(s_DictionaryValueGlyph);
                        s_VerboseMessageBuilder.AppendLine(header.Value);

                        s_VerboseMessageBuilder.AppendLine();
                    }
                }

                if (builder.m_Request.uploadHandler != null)
                {
                    // Log the type of request that was sent

                    s_VerboseMessageBuilder.AppendLine(FormatStringAsLogTitle("Content type:"));
                    s_VerboseMessageBuilder.AppendLine(builder.m_Request.uploadHandler.contentType);
                }

                s_VerboseMessageBuilder.AppendLine();

                if (builder.formData.Count > 0)
                {
                    // Include the request's form/post data in the logged output
                    s_VerboseMessageBuilder.AppendLine(FormatStringAsLogTitle("Form data:"));

                    foreach (IMultipartFormSection section in builder.formData)
                    {
                        // Field name goes first
                        {
                            string fieldName = section.sectionName;

                            if (string.IsNullOrEmpty(fieldName))
                            {
                                fieldName = "<i>[anonymous]</i>";
                            }

                            s_VerboseMessageBuilder.Append(s_DictionaryKeyGlyph);
                            s_VerboseMessageBuilder.AppendLine(fieldName);
                        }

                        // Meta data comes next. Meta data is only printed if the
                        // field is a file or a non-loggable content type
                        byte[] sectionData = section.sectionData;
                        if (section.fileName != null || !IsLoggableContentType(section.contentType))
                        {
                            long sectionDataSize = sectionData != null ? sectionData.LongLength : 0;
                            s_VerboseMessageBuilder.Append("\u2026");  // horizontal ellipsis
                            s_VerboseMessageBuilder.Append($" <b>fileName</b>={section.fileName ?? "<i>[blank]</i>"}");
                            s_VerboseMessageBuilder.Append($", <b>byteCount</b>={sectionDataSize}");
                            s_VerboseMessageBuilder.Append($", <b>contentType</b>={section.contentType ?? "<i>[blank]</i>"}");
                            s_VerboseMessageBuilder.AppendLine();
                        }

                        // Finally, print the field's content.
                        s_VerboseMessageBuilder.Append(s_DictionaryValueGlyph);
                        string content = GetContentStringFromData(sectionData);
                        s_VerboseMessageBuilder.AppendLine(content);

                        s_VerboseMessageBuilder.AppendLine();
                    }
                }
                else if (builder.jsonPayload.HasValue)
                {
                    // Include the request's form/post data in the logged output
                    s_VerboseMessageBuilder.AppendLine(FormatStringAsLogTitle("JSON data:"));

                    string prettyJson = MiniJSON.Json.Serialize(builder.jsonPayload.Value.rawValue, prettyPrint: true);

                    s_VerboseMessageBuilder.AppendLine(prettyJson);

                    s_VerboseMessageBuilder.AppendLine();
                }
                else if (builder.request.uploadHandler != null)
                {
                    s_VerboseMessageBuilder.AppendLine(FormatStringAsLogTitle("Raw data:"));

                    string content = GetContentStringFromData(builder.request.uploadHandler.data);
                    s_VerboseMessageBuilder.AppendLine(content);
                }
            }

            MelcherLog.Log(s_VerboseMessageBuilder.ToString(), tag: s_LogTag);
        }

        /// <summary>
        ///     Log a message about the result of a web request.
        /// </summary>
        /// <param name="requestId">
        ///     An ID previously generated by <see
        ///     cref="LogRequestStart(MelcherWebRequestBuilder, out string)"/>.
        /// </param>
        /// <param name="builder"></param>
        /// <param name="error"></param>
        private static void LogRequestEnd(
            string                      requestId,
            MelcherWebRequestBuilder    builder,
            System.Exception            error)
        {
            MelcherLog.Assert(requestId != null);
            MelcherLog.Assert(builder != null);
            MelcherLog.Assert(builder.m_Request != null);

            s_VerboseMessageBuilder.Clear();
            s_VerboseMessageBuilder.Append($"{requestId} Received response code {builder.m_Request.responseCode}");

            if (error != null)
            {
                s_VerboseMessageBuilder.Append($" with error: {error.Message}");

                MelcherLog.LogError(s_VerboseMessageBuilder.ToString(), tag: s_LogTag);

                return;
            }

            if (UnityEngine.Application.isEditor || runtimeDebug)
            {
                // For security and privacy reasons, the response is only logged
                // within the editor.

                s_VerboseMessageBuilder.AppendLine();
                s_VerboseMessageBuilder.AppendLine(FormatStringAsLogTitle("Content type:"));
                string contentType = builder.request.GetResponseHeader("Content-Type");
                if (string.IsNullOrEmpty(contentType))
                {
                    contentType = "<i>[blank]</i>";
                }
                s_VerboseMessageBuilder.AppendLine(contentType);

                s_VerboseMessageBuilder.AppendLine();
                s_VerboseMessageBuilder.AppendLine(FormatStringAsLogTitle("Content:"));

                if (builder.request.downloadHandler == null)
                {
                    // downloadHandler may be null if the user doesn't intend the
                    // server's response to be read/downloaded.

                    s_VerboseMessageBuilder.AppendLine("<i>[No download handler]</i>");
                }
                else
                {
                    string content =
                        GetContentStringFromData(builder.request.downloadHandler.data);

                    if (contentType.StartsWith(s_ContentTypeJson, System.StringComparison.OrdinalIgnoreCase))
                    {
                        // Pretty-print JSON for better readability

                        try
                        {
                            object json = MiniJSON.Json.Deserialize(content);
                            content = MiniJSON.Json.Serialize(json, prettyPrint: true);
                        }
                        catch
                        {
                            // Ignore (de)serialization errors
                        }
                    }

                    s_VerboseMessageBuilder.AppendLine(content);
                }
            }

            MelcherLog.Log(s_VerboseMessageBuilder.ToString(), tag: s_LogTag);
        }

        /// <summary>
        ///     Decode a UTF8 string from its binary data. If the data is null,
        ///     "[blank]" is returned. If the decoded string contains
        ///     binary-looking data, "[binary content]" is returned.
        ///     <br /><br />
        ///
        ///     A string looks like it is binary if the first 100 or so
        ///     characters contain a control character.
        /// </summary>
        private static string GetContentStringFromData(byte[] data)
        {
            if (!TryDecodeDataAsText(data, out string content))
            {
                content = "<i>[blank]</i>";
            }
            else if (IsBinaryData(content))
            {
                content = "<i>[binary content]</i>";
            }

            return content;
        }

        /// <summary>
        ///     Try to decode a byte array (<paramref name="data"/>) into a UTF8
        ///     string and return it via <paramref name="decodedString"/>.
        /// </summary>
        /// <returns>
        ///     Return <see langword="true"/> if <paramref name="data"/> was
        ///     decoded successfully. Otherwise, return <see langword="false"/>
        ///     and set <paramref name="decodedString"/> to <see
        ///     langword="null"/>.
        /// </returns>
        private static bool TryDecodeDataAsText(byte[] data, out string decodedString)
        {
            bool didDecode = false;
            decodedString = null;

            if (data != null && data.Length > 0)
            {
                try
                {
                    decodedString = System.Text.Encoding.UTF8.GetString(data);
                    didDecode = true;
                }
                catch (System.ArgumentException)
                {
                    // Data contains invalid Unicode code points
                    // Do nothing.
                    decodedString = "[binary content]";
                }
            }

            return didDecode;
        }

        /// <summary>
        ///     Return <see langword="true"/> if <paramref name="contentType"/>
        ///     is a known printable text content type. E.g., "text/plain" or
        ///     "application/json". Otherwise, return <see langword="false"/>.
        /// </summary>
        private static bool IsLoggableContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            foreach (string loggableContentType in s_LoggableContentType)
            {
                if (contentType.StartsWith(loggableContentType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Return true if <paramref name="data"/> appears to contain binary
        ///     data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool IsBinaryData(string data)
        {
            // Limit the number of bytes checked. We're relying on binary data
            // having binary characters at the start to avoid analyzing the
            // whole string.
            int bytesToCheck = 128;
            if (bytesToCheck > data.Length)
            {
                bytesToCheck = data.Length;
            }

            Match match =
                s_BinaryAndNonPrintableCharacterPattern.Match(data, beginning: 0, length: bytesToCheck);

            return match != null && match.Success;
        }

        private static string FormatStringAsLogTitle(string title)
        {
            return $"<size=16><b>{title}</b></size>";
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        private const string s_ContentTypeJson = "application/json";

        /// <summary>
        ///     <see cref="MelcherWebRequestBuilder"/>'s log messages are colourized to better
        ///     stand out in the console.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        protected static readonly string s_LogTag = MelcherLog.MakeLogTag("41729f");  // "Queen blue"

        /// <summary>
        ///     Straight right arrow. Used to format a readable log message and
        ///     accent a Key in a key-value pair.
        /// </summary>
        protected const string s_DictionaryKeyGlyph = "\u2192";

        /// <summary>
        ///     Right arrow with hook end. Used to format a readable log message
        ///     and accent a Value in a key-value pair.
        /// </summary>
        protected const string s_DictionaryValueGlyph = "\u21AA";

        /// <summary>
        ///     List of available colors used to distinguish web requests in the
        ///     log output.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly ReadOnlyCollection<string> s_RequestColorHexCodes =
            new List<string>()
            {
                "#3CB44B",  // Green
                "#FFFF19",  // Yellow
                "#0082C8",  // Blue
                "#911EB4",  // Purple
                "#46F0F0",  // Cyan
                "#D2F53C",  // Lime
                "#FABEBE",  // Pink
                "#008080",  // Teal
                "#E6BEFF",  // Lavender
                "#AA6E28",  // Brown
                "#FFFAC8",  // Beige
                "#AAFFC3",  // Mint
                "#808000",  // Olive
                "#FFD7B4",  // Coral
                "#000080",  // Navy
            }.AsReadOnly();

        /// <summary>
        ///     Content-type prefixes that are assumed to be printable as
        ///     plaintext.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        protected static readonly ReadOnlyCollection<string> s_LoggableContentType =
            new List<string>()
            {
                "text/",
                "application/json",
                "application/xml",
                "multipart/form-data"
            }.AsReadOnly();

        /// <summary>
        ///     A regex pattern used to match a string containing binary data.
        ///     <br /><br />
        ///
        ///     The pattern matches any character from the group of control
        ///     characters, but exclude whitespace characters.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        protected static readonly Regex s_BinaryAndNonPrintableCharacterPattern =
            new Regex(pattern: @"(?!\s)\p{C}");

        /// <summary>
        ///     A StringBuilder used to construct log messages at the start and
        ///     end of a web request.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        protected static readonly StringBuilder s_VerboseMessageBuilder = new StringBuilder();

        /// <summary>
        ///     The next web request ID to use. This ID is also used as a
        ///     (wrapping) index into the <see cref="s_RequestColorHexCodes"/>
        ///     array to colourize a request's log messages for easier visual
        ///     matching.
        /// </summary>
        private static int s_NextRequestId = 0;

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        [MelcherResetStaticFieldsMethod]
        protected static int ResetStaticFields_Logging()
        {
            int fieldsReset = 0;

            ++fieldsReset;  s_NextRequestId = 0;

            return fieldsReset;
        }

#endif  // if UNITY_EDITOR

    }   // class MelcherWebRequest

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-20
/// ===========================================================================
