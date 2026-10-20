namespace Melcher
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    using MEC;

    using JsonObject = System.Collections.Generic.IDictionary<string, object>;
    using JsonList = System.Collections.Generic.IList<object>;

    /// <summary>
    ///     <see cref="MelcherWebRequestBuilder"/> implements a builder pattern
    ///     for <see cref="UnityWebRequest"/>. It uses More Effective Coroutines
    ///     to kick off coroutines that monitor and handle web requests without
    ///     needing a Game Object instance. See the source file for examples.
    /// </summary>
    public partial class MelcherWebRequestBuilder
    {
        // ====================================================================
        // Documentation
        // ====================================================================

#if UNITY_EDITOR
        /// <summary>
        /// Example use:
        ///
        /// <code>
        /// public MelcherWebCoroutine LogIn(string username, string password)
        /// {
        ///     // Build a GET request for user log in
        ///
        ///     return
        ///         MelcherWebRequestBuilder.Get("https://www.server.com/api/login")
        ///             .AddFormData("username", username)
        ///             .AddFormData("password", password)
        ///             .OnResponseJson(
        ///                 onSuccess: (builder, jsonValue) =>
        ///                 {
        ///                     jsonValue.jsonObject.TryGetValueFromJson("api_token", out string apiToken);
        ///
        ///                     MelcherLog.Log("Log In success. API token is: " + apiToken);
        ///                 },
        ///                 onError: (builder, error) =>
        ///                 {
        ///                     // Error is an Exception type. This means it can
        ///                     // be type checked to provide localized error
        ///                     // messages.
        ///
        ///                     MelcherLog.LogError("Log In error: " + error.Message);
        ///                 }
        ///             )
        ///             .Send();
        /// }
        /// </code>
        ///
        ///     First, call one of the factory functions to begin constructing a
        ///     request:
        ///     <br /><br />
        ///
        ///     - <see cref="Get(string)"/>
        ///     <br /><br />
        ///
        ///     - <see cref="Post(string)"/>
        ///     <br /><br />
        ///
        ///     - etc.
        ///     <br /><br />
        ///
        ///     Next, configure the request. The following methods are all optional:
        ///     <br /><br />
        ///
        ///     1.  Headers: <see cref="SetHeader(string, string)"/>.
        ///         <br /><br />
        ///
        ///     2.  URL parameters: <see cref="AddUrlParameter(string, string)"/>.
        ///         <br /><br />
        ///
        ///     3.  Payload data: The payload must be one consistent format.
        ///         <br /><br />
        ///
        ///         <see cref="AddFormData(string, string)"/>
        ///         <br /><br />
        ///
        ///         <see cref="SetJsonPayload(MelcherJsonValue)"/>
        ///         <br /><br />
        ///
        ///         <see cref="SetRawPayload(byte[], string)"/>
        ///         <br /><br />
        ///
        ///     4.  Set a timeout (with millisecond precision) using <see
        ///         cref="SetTimeout(int)"/>.
        ///         <br /><br />
        ///
        ///         Vanilla <see cref="UnityWebRequest"/>s only allow timeouts to
        ///         be specified in seconds.
        ///         <br /><br />
        ///
        ///         Another improvement is that timeouts no longer print a curl
        ///         error message when a request expires. This keeps the console
        ///         clean.
        ///         <br /><br />
        ///
        ///         Timeouts must be set using this method. Any timeout set through
        ///         <see cref="UnityWebRequest.timeout"/> is ignored.
        ///         <br /><br />
        ///
        ///     5.  Add non-transmitted meta data: <see cref="SetUserData(object)"/>.
        ///         <br /><br />
        ///
        ///         This data can be accessed in any one of the many callback
        ///         functions as a means to pass arbitrary data into them. See
        ///         <see cref="userData"/>.
        ///         <br /><br />
        ///
        ///     6.  Configure extra options.
        ///         <br /><br />
        ///
        ///         <see cref="SetOptionVerbose(bool)"/>.
        ///         <br /><br />
        ///
        ///         <see cref="SetOptionDryRun(bool)"/>.
        ///         <br /><br />
        ///
        ///     7.  Set callbacks:
        ///         <br /><br />
        ///
        ///         A response callback is almost always set. These include
        ///         <see cref="OnResponse(OnRequestSuccess, OnRequestError)"/> and
        ///         <see cref="OnResponseJson(OnRequestSuccessJson, OnRequestErrorJson)"/>.
        ///         <br /><br />
        ///
        ///     Finally, once the request is fully built, call <see cref="Send"/>
        ///     to dispatch it.
        /// </summary>
        private static class Documentation
        {
            // This is an empty class. It simply exists to anchor more C-Sharp
            // documentation in a way that doesn't bloat the outer class's
            // summary. This approach allows us to continue the documentation
            // using XML tags and ensure code references survive renames.

        }   // static class Documentation

#endif  // if UNITY_EDITOR

        // =====================================================================
        // Factory functions
        // =====================================================================

        /// <summary>
        ///     Start building an HTTP GET request.
        /// </summary>
        public static MelcherWebRequestBuilder Get(string url)
            => GetNextRequestBuilder(url, UnityWebRequest.kHttpVerbGET);

        /// <summary>
        ///     Start building an HTTP POST request.
        /// </summary>
        public static MelcherWebRequestBuilder Post(string url)
            => GetNextRequestBuilder(url, UnityWebRequest.kHttpVerbPOST);

        /// <summary>
        ///     Start building an HTTP PUT request.
        /// </summary>
        public static MelcherWebRequestBuilder Put(string url)
            => GetNextRequestBuilder(url, UnityWebRequest.kHttpVerbPUT);

        /// <summary>
        ///     Start building an HTTP PATCH request.
        /// </summary>
        public static MelcherWebRequestBuilder Patch(string url)
            => GetNextRequestBuilder(url, "PATCH");

        /// <summary>
        ///     Start building an HTTP DELETE request.
        /// </summary>
        public static MelcherWebRequestBuilder Delete(string url)
            => GetNextRequestBuilder(url, UnityWebRequest.kHttpVerbDELETE);

        /// <summary>
        ///     Start building an HTTP CREATE request.
        /// </summary>
        public static MelcherWebRequestBuilder Create(string url)
            => GetNextRequestBuilder(url, UnityWebRequest.kHttpVerbCREATE);

        /// <summary>
        ///     Start building an HTTP HEAD request.
        /// </summary>
        public static MelcherWebRequestBuilder Head(string url)
            => GetNextRequestBuilder(url, UnityWebRequest.kHttpVerbHEAD);

        // =====================================================================
        // Static fields
        // =====================================================================

        /// <summary>
        ///     The error returned when a request's timeout elapses. A request's
        ///     timeout can be set using the <see cref="SetTimeout(int)"/>
        ///     method.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        public static readonly System.Exception TimedOutError = new System.Exception("[Timed out]");

        /// <summary>
        ///     The error returned when a request is configured with the <see
        ///     cref="RequestOption.DryRun"/> option. This error is always
        ///     raised before a dry-run request dispatches the request.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        public static readonly System.Exception DryRunError = new System.Exception("[Dry run]");


        // =====================================================================
        // Static functions
        // =====================================================================

        /// <summary>
        ///     Given a URL like "https://www.melcher.ca/" and a collection of
        ///     string-to-string key-values, return a URL that looks like:
        ///     "https://www.melcher.ca/?key1=value1&key2=value2"
        /// </summary>
        /// <param name="url"></param>
        /// <param name="urlParameters"></param>
        /// <returns></returns>
        public static string JoinUrlWithUrlParameters(string url, IEnumerable<KeyValuePair<string, string>> urlParameters)
        {
            if (urlParameters != null)
            {
                using var clear =
                    MelcherDeferred.Defer(
                        () => s_FormattedUrlArgs.Clear()
                    );

                foreach (KeyValuePair<string, string> urlParameter in urlParameters)
                {
                    if (string.IsNullOrWhiteSpace(urlParameter.Key))
                    {
                        MelcherLog.LogWarningEditorOnly($"Invalid URL query parameter. Key is blank.");
                    }
                    else if (string.IsNullOrWhiteSpace(urlParameter.Value))
                    {
                        s_FormattedUrlArgs.Add(urlParameter.Key);
                    }
                    else
                    {
                        s_FormattedUrlArgs.Add($"{urlParameter.Key}={urlParameter.Value}");
                    }
                }

                if (s_FormattedUrlArgs.Count > 0)
                {
                    // Ensure a forward slash is inserted between the URL and
                    // the URL parameters. This is how browsers behave, likely
                    // for aesthetics.

                    // Using string for backwards compatibility with Unity 2020
                    const string Questionmark = "?";
                    const char ForwardSlash = '/';

                    int indexOfQuestionmark = url.IndexOf(Questionmark);
                    if (indexOfQuestionmark < 0 && url.Length > 0 && url[url.Length - 1] != ForwardSlash)
                    {
                        url += ForwardSlash;
                    }
                    else if (indexOfQuestionmark == 0 || (indexOfQuestionmark > 0 && url[indexOfQuestionmark - 1] != ForwardSlash))
                    {
                        url = url.Insert(indexOfQuestionmark, ForwardSlash.ToString());
                    }

                    // Insert '&' between each subsequent key-value

                    string escapedUrlParameters = string.Join("&", s_FormattedUrlArgs);

                    url += indexOfQuestionmark >= 0 ? "&" : "?";

                    url += escapedUrlParameters;
                }
            }

            return url;
        }

        // =====================================================================
        // Properties
        // =====================================================================

        public IDictionary<string, string> headers => m_Headers;

        public IReadOnlyCollection<KeyValuePair<string, string>> urlParameters => m_UrlParameters;

        public IList<IMultipartFormSection> formData => m_FormData;

        public MelcherJsonValue? jsonPayload => m_JsonPayload;

        public object userData => m_UserData;

        public UnityWebRequest request => m_Request;

        public OnRequestSuccess onSuccess => m_OnSuccess;
        public OnRequestError onError => m_OnError;

        public OnRequestSuccessJson onSuccessJson => m_OnSuccessJson;
        public OnRequestErrorJson onErrorJson => m_OnErrorJson;

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Set a header value. Headers are internally modeled using a
        ///     dictionary which means every key can only have one value.
        /// </summary>
        /// <exception cref="System.ArgumentException">
        ///     Thrown if <paramref name="key"/> is <see langword="null"/> of
        ///     blank.
        ///     <br /><br />
        ///
        ///     Thrown if <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public MelcherWebRequestBuilder SetHeader(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new System.ArgumentException($"{nameof(key)} must not be null or white space.", nameof(key));
            }

            if (value == null)
            {
                throw new System.ArgumentException($"{nameof(value)} must not be null.", nameof(value));
            }

            m_Headers[key] = value;

            return this;
        }

        /// <inheritdoc cref="SetHeader(string, string)"/>
        public MelcherWebRequestBuilder SetHeader(KeyValuePair<string, string> header)
        {
            return SetHeader(header.Key, header.Value);
        }

        /// <summary>
        ///     Set multiple headers.
        /// </summary>
        /// <exception cref="System.ArgumentException">
        ///     Thrown if <paramref name="key"/> is <see langword="null"/> of
        ///     blank.
        ///     <br /><br />
        ///
        ///     Thrown if <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public MelcherWebRequestBuilder SetHeaders(IEnumerable<KeyValuePair<string, string>> newHeaders)
        {
            if (newHeaders == null)
            {
                throw new System.ArgumentNullException(nameof(newHeaders));
            }

            foreach (KeyValuePair<string, string> header in newHeaders)
            {
                SetHeader(header.Key, header.Value);
            }

            return this;
        }

        /// <summary>
        ///     Add a URL parameter. E.g., "?hello".
        /// </summary>
        public MelcherWebRequestBuilder AddUrlParameter(string key)
        {
            return AddUrlParameter(key, value: null);
        }

        /// <summary>
        ///     Add a URL parameter that has value. E.g., "?product_id=32".
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public MelcherWebRequestBuilder AddUrlParameter(string key, string value)
        {
            if (key == null)
            {
                throw new System.ArgumentNullException(nameof(key));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new System.ArgumentException($"{nameof(key)} must not be null or white space.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                value = null;
            }

            m_UrlParameters.Add(new KeyValuePair<string, string>(key, value));

            return this;
        }

        public MelcherWebRequestBuilder AddUrlParameters(IEnumerable<KeyValuePair<string, string>> newUrlParameters)
        {
            if (newUrlParameters == null)
            {
                throw new System.ArgumentNullException(nameof(newUrlParameters));
            }

            foreach (KeyValuePair<string, string> urlParameter in newUrlParameters)
            {
                AddUrlParameter(urlParameter.Key, urlParameter.Value);
            }

            return this;
        }

        /// <inheritdoc cref="AddFormData(IMultipartFormSection)"/>
        public MelcherWebRequestBuilder AddFormData(string key, string value)
        {
            if (key == null)
            {
                // Key cannot be null but empty is fine.

                throw new System.ArgumentNullException($"{nameof(key)} must not be null.", nameof(key));
            }

            if (string.IsNullOrEmpty(value))
            {
                // Key cannot be null or empty, but whitespace is fine.

                throw new System.ArgumentException($"{nameof(value)} must not be null or empty.", nameof(value));
            }

            return AddFormData(new MultipartFormDataSection(key, value));
        }

        /// <summary>
        ///     Attach a form section to the request. This offers more
        ///     flexibility than <see cref="AddPostData(string, string)"/>.
        ///     <br /><br />
        ///
        ///     E.g., a form section can include binary data to upload a file to
        ///     the server.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     Thrown if any other non form-data payload was already set.
        /// </exception>
        public MelcherWebRequestBuilder AddFormData(IMultipartFormSection formSection)
        {
            if (formSection == null)
            {
                throw new System.ArgumentNullException(nameof(formSection));
            }

            if (m_FormData.Count == 0)
            {
                ThrowIfAnyPayloadAlreadySet();
            }

            m_FormData.Add(formSection);

            return this;
        }

        public MelcherWebRequestBuilder AddFormData(IEnumerable<IMultipartFormSection> formData)
        {
            if (formData == null)
            {
                throw new System.ArgumentNullException(nameof(formData));
            }

            foreach (IMultipartFormSection formSection in formData)
            {
                AddFormData(formSection);
            }

            return this;
        }

        /// <summary>
        ///     Configure the web request as sending JSON data.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     Thrown if any type of payload was already set.
        /// </exception>
        public MelcherWebRequestBuilder SetJsonPayload(MelcherJsonValue jsonValue)
        {
            if (jsonValue.valueType == null)
            {
                throw new System.ArgumentException("JSON value must have a type.",  nameof(jsonValue));
            }

            ThrowIfAnyPayloadAlreadySet();

            m_JsonPayload = jsonValue;

            return this;
        }

        /// <summary>
        ///     For low-level control (e.g., uploading pre-formatted binary or
        ///     file data), this method can be used to deliver a payload of
        ///     bytes as-is.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     Thrown if any type of payload was already set.
        /// </exception>
        public MelcherWebRequestBuilder SetRawPayload(byte[] payloadData, string contentType)
        {
            if (payloadData == null)
            {
                throw new System.ArgumentNullException(nameof(payloadData));
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new System.ArgumentException(nameof(contentType));
            }

            ThrowIfAnyPayloadAlreadySet();

            m_RawPayload = payloadData;

            m_RawPayloadContentType = contentType;

            return this;
        }

        /// <summary>
        ///     Attach arbitrary meta data to the builder. This data isn't
        ///     transmitted in the request but is retrievable in the callback
        ///     functions.
        ///     <br /><br />
        ///
        ///     This overwrites any value that was set in a previous call to
        ///     this function.
        /// </summary>
        public MelcherWebRequestBuilder SetUserData(object userData)
        {
            m_UserData = userData;

            return this;
        }

        public MelcherWebRequestBuilder OnSetup(OnRequestSetup onSetup)
        {
            m_OnSetup = onSetup;
            return this;
        }

        public MelcherWebRequestBuilder OnStart(OnRequestStart onStart)
        {
            m_OnStart = onStart;
            return this;
        }

        /// <summary>
        ///     Set whether the request should be treated as a dry-run.
        ///     <br /><br />
        ///
        ///     Passing <see langword="true"/> will:
        ///     <inheritdoc cref="RequestOption.DryRun"/>
        ///     <br /><br />
        ///
        ///     The default value is <see langword="false"/> which means the
        ///     request will execute normally.
        /// </summary>
        public MelcherWebRequestBuilder SetOptionDryRun(bool isDryRun)
        {
            if (isDryRun)
            {
                m_RequestOptions |= RequestOption.DryRun;
            }
            else
            {
                m_RequestOptions &= ~RequestOption.DryRun;
            }

            return this;
        }

        /// <summary>
        ///     Set whether the request should be logged.
        ///     <br /><br />
        ///
        ///     Passing <see langword="true"/> will:
        ///     <inheritdoc cref="RequestOption.Verbose"/>
        ///     <br /><br />
        ///
        ///     The default value is <see langword="true"/> which means the
        ///     request will log to the console.
        /// </summary>
        public MelcherWebRequestBuilder SetOptionVerbose(bool isVerbose)
        {
            if (isVerbose)
            {
                m_RequestOptions |= RequestOption.Verbose;
            }
            else
            {
                m_RequestOptions &= ~RequestOption.Verbose;
            }

            return this;
        }

        /// <summary>
        ///     Set whether a download buffer will be created automatically for
        ///     the request.
        ///     <br /><br />
        ///
        ///     This is an optimization feature that is uncommonly needed. If a
        ///     server end point is known to not return a response, this can
        ///     be set to <see langword="false"/> to avoid allocating a buffer
        ///     to receive the response.
        ///     <br /><br />
        ///
        ///     This is <see langword="true"/> by default. If it is <see
        ///     langword="false"/>, <c>request.downloadHandler</c> will be <see
        ///     langword="null"/> unless one is manually created by the caller
        ///     in the <see cref="OnSetup(OnRequestSetup)"/> callback.
        /// </summary>
        public MelcherWebRequestBuilder SetAutoCreateDownloadBuffer(bool autoCreateDownloadBuffer)
        {
            m_AutoCreateDownloadBuffer = autoCreateDownloadBuffer;

            return this;
        }

        /// <summary>
        ///     Set the maximum number of milliseconds the request will wait
        ///     before automatically canceling and calling the request's
        ///     `onError` callback with the error <see cref="TimedOutError"/>.
        /// </summary>
        public MelcherWebRequestBuilder SetTimeout(int milliseconds)
        {
            m_TimeoutMilliseconds = milliseconds;
            return this;
        }

        /// <summary>
        ///     Set the callback to be invoked when the response completes
        ///     (whether successfully, by time out, exception, etc.). The user
        ///     has full discretion to determine is and isn't an an error.
        ///     <br /><br />
        ///
        ///     Example: a 404 response might be an expected, valid reply in
        ///     some APIs or an error in others.
        /// </summary>
        /// <param name="onCompleted"></param>
        public MelcherWebRequestBuilder OnCompleted(OnRequestCompleted onCompleted)
        {
            if (m_ResponseType != ResponseType.Unspecified)
            {
                throw new System.InvalidOperationException($"Request already configured for {m_ResponseType} response.");
            }

            m_ResponseType = ResponseType.Raw;

            m_OnCompleted = onCompleted;

            return this;
        }


        /// <summary>
        ///     Set the callbacks to be invoked when the response completes.
        ///     <br /><br />
        ///
        ///     Prefer <see cref="OnCompleted(OnRequestCompleted)"/>
        ///     instead of this function. In the future, this function will be
        ///     marked obsolete, and later, removed.
        /// </summary>
        /// <param name="onSuccess"></param>
        /// <param name="onError"></param>
        //[System.Obsolete("Replace with OnResponseCompleted")]
        public MelcherWebRequestBuilder OnResponse(
            OnRequestSuccess onSuccess  = null,
            OnRequestError   onError    = null)
        {
            if (m_ResponseType != ResponseType.Unspecified)
            {
                throw new System.InvalidOperationException($"Request already configured for {m_ResponseType} response.");
            }

            m_ResponseType = ResponseType.Raw;

            m_OnSuccess = onSuccess;
            m_OnError = onError;

            return this;
        }

        public MelcherWebRequestBuilder OnJsonCompleted(OnJsonRequestCompleted onCompleted)
        {
            if (m_ResponseType != ResponseType.Unspecified)
            {
                throw new System.InvalidOperationException($"Request already configured for {m_ResponseType} response.");
            }

            m_ResponseType = ResponseType.Json;

            m_OnJsonCompleted = onCompleted;

            return this;
        }

        //[System.Obsolete("Replace with OnJsonResponseCompleted")]
        public MelcherWebRequestBuilder OnResponseJson(
            OnRequestSuccessJson onSuccess   = null,
            OnRequestErrorJson   onError     = null)
        {
            if (m_ResponseType != ResponseType.Unspecified)
            {
                throw new System.InvalidOperationException($"Request already configured for {m_ResponseType} response.");
            }

            m_ResponseType = ResponseType.Json;

            m_OnSuccessJson = onSuccess;
            m_OnErrorJson = onError;

            return this;
        }

        public string GetUrlWithUrlParameters()
        {
            return JoinUrlWithUrlParameters(m_Url, m_UrlParameters);
        }

        public MelcherWebCoroutine Send()
        {
            OnRequestCompleted onRequestCompleted = null;

            if (m_ResponseType == ResponseType.Raw)
            {
                // TODO: Make these obsolete so that we are entirely using
                //       onRequestCompleted.
                if (m_OnSuccess != null || m_OnError != null)
                {
                    // Using legacy (non-unified) callbacks. These are less
                    // flexible for users of this class because they do not have
                    // full control over what is and isn't an error.

                    onRequestCompleted = HandleLegacyCallbacks;
                }
                else
                {
                    onRequestCompleted = m_OnCompleted;
                }
            }
            else if (m_ResponseType == ResponseType.Json)
            {
                // TODO: Make these obsolete so that we are entirely using
                //       onRequestCompleted.
                if (m_OnSuccessJson != null || m_OnErrorJson != null)
                {
                    // Using legacy (non-unified) callbacks. These are less
                    // flexible for users of this class because they do not have
                    // full control over what is and isn't an error.

                    onRequestCompleted = HandleLegacyJsonCallbacks;
                }
                else
                {
                    onRequestCompleted = HandleJsonCallback;
                }
            }

            return
                Timing.RunCoroutine(
                    coroutine: DoRequest(onRequestCompleted),
                    segment: Application.isPlaying ? Segment.Update : Segment.EditorUpdate
                );
        }

        /// <summary>
        ///     This acts as a shim to allow the "legacy" <c>onSuccess</c> and
        ///     <c>onError</c> callbacks to be used with the more recent
        ///     <see cref="OnCompleted(OnRequestCompleted)"/> function.
        /// </summary>
        protected void HandleLegacyCallbacks(MelcherWebRequestBuilder builder, System.Exception error)
        {
            if (error != null)
            {
                if (m_OnError != null)
                {
                    m_OnError.Invoke(builder, error);
                }
                else
                {
                    MelcherLog.LogError(error.Message);
                }
            }
            else
            {
                m_OnSuccess?.Invoke(builder);
            }
        }

        /// <summary>
        ///     This is a shim to allow the "legacy" <c>onSuccess</c> and
        ///     <c>onError</c> callbacks to be used with the more recent
        ///     <see cref="OnJsonCompleted(OnJsonRequestCompleted)"/> function.
        /// </summary>
        protected void HandleLegacyJsonCallbacks(MelcherWebRequestBuilder builder, System.Exception error)
        {
            if (error != null)
            {
                Error(builder, error);

                return;
            }

            error = ParseJsonResponse(builder, out MelcherJsonValue jsonResponse);

            if (error != null)
            {
                Error(builder, error);

                return;
            }

            builder.m_OnSuccessJson?.Invoke(builder, jsonResponse);

            static void Error(MelcherWebRequestBuilder builder, System.Exception error)
            {
                if (builder.m_OnErrorJson != null)
                {
                    builder.m_OnErrorJson.Invoke(builder, error, jsonResponse: default);
                }
                else
                {
                    MelcherLog.LogError(error.Message);
                }
            }
        }

        /// <summary>
        ///     A callback that is compatible with <see
        ///     cref="OnRequestCompleted"/> and that forwards to <see
        ///     cref="m_OnJsonCompleted"/>.
        /// </summary>
        protected void HandleJsonCallback(MelcherWebRequestBuilder builder, System.Exception error)
        {
            if (error != null)
            {
                m_OnJsonCompleted.Invoke(builder, error, jsonResponse: default);
                return;
            }

            error = ParseJsonResponse(builder, out MelcherJsonValue jsonResponse);

            m_OnJsonCompleted?.Invoke(builder, error, jsonResponse);
        }

        protected System.Exception ParseJsonResponse(MelcherWebRequestBuilder builder, out MelcherJsonValue jsonResponse)
        {
            jsonResponse = default;

            if (builder == null)
            {
                return new System.ArgumentException(nameof(builder));
            }

            if (builder.m_Request == null)
            {
                return new System.InvalidOperationException("UnityWebRequest is null.");
            }

            if (builder.m_Request.downloadHandler == null)
            {
                return new System.InvalidOperationException("UnityWebRequest.downloadHandler is null.");
            }

            string rawJsonResponse = builder.m_Request.downloadHandler.text;

            if (!string.IsNullOrWhiteSpace(rawJsonResponse))
            {
                object json = null;
                try
                {
                    // Convert text response to structured JSON

                    json = MiniJSON.Json.Deserialize(rawJsonResponse);
                }
                catch (System.Exception e)
                {
                    return e;
                }

                if (json is JsonList jsonList)
                {
                    jsonResponse = new MelcherJsonValue(jsonList);
                }
                else if (json is JsonObject jsonObject)
                {
                    jsonResponse = new MelcherJsonValue(jsonObject);
                }
                else
                {
                    return new System.InvalidOperationException("Unknown JSON type.");
                }
            }

            return null;
        }

        protected void ThrowIfAnyPayloadAlreadySet()
        {
            if (m_FormData.Count > 0)
            {
                throw new System.InvalidOperationException("Form data payload is already set.");
            }

            if (m_JsonPayload.HasValue)
            {
                throw new System.InvalidOperationException("JSON payload is already set.");
            }

            if (m_RawPayload != null)
            {
                throw new System.InvalidOperationException("Raw data payload is already set.");
            }
        }

        /// <summary>
        ///     Create, configure and dispatch a <see cref="UnityWebRequest"/>
        ///     to handle the request as constructed by the caller.
        /// </summary>
        protected IEnumerator<float> DoRequest(OnRequestCompleted onRequestCompleted)
        {
            MelcherLog.Assert(!string.IsNullOrWhiteSpace(m_Url));
            MelcherLog.Assert(m_Request == null);

            try
            {
                string url = GetUrlWithUrlParameters();

                // Make the web request. This will throw if the URL is
                // malformed.

                // Ideally, this try-catch would wrap the upcoming `using`
                // statement but it cannot because try-catch blocks cannot wrap
                // `yield` statements which the `using` block contains.

                m_Request = new UnityWebRequest(url);
            }
            catch (System.UriFormatException uriError)
            {
                onRequestCompleted?.Invoke(builder: this, error: uriError);

                yield break;
            }

            using (m_Request)
            {
                using var resetUnityWebRequest =
                    MelcherDeferred.Defer(
                        toCapture: this,
                        onDispose: melcherWebRequest => melcherWebRequest.m_Request = null
                    );

                m_Request.method = m_Method;

                if (m_AutoCreateDownloadBuffer)
                {
                    m_Request.downloadHandler = new DownloadHandlerBuffer();
                }

                if (m_Headers != null)
                {
                    foreach (KeyValuePair<string, string> header in m_Headers)
                    {
                        m_Request.SetRequestHeader(header.Key, header.Value);
                    }
                }

                if (m_FormData.Count > 0)
                {
                    // Configure form data upload

                    MelcherLog.Assert(m_Request.uploadHandler == null);

                    byte[] boundary = UnityWebRequest.GenerateBoundary();

                    byte[] uploadBytes = UnityWebRequest.SerializeFormSections(m_FormData, boundary);

                    m_Request.uploadHandler = new UploadHandlerRaw(uploadBytes);

                    m_Request.uploadHandler.contentType =
                        "multipart/form-data; boundary=" + System.Text.Encoding.UTF8.GetString(boundary, 0, boundary.Length);
                }

                if (m_JsonPayload.HasValue)
                {
                    // Configure JSON upload

                    MelcherLog.Assert(m_Request.uploadHandler == null);

                    string payload = MiniJSON.Json.Serialize(m_JsonPayload.Value.rawValue);

                    byte[] uploadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

                    m_Request.uploadHandler = new UploadHandlerRaw(uploadBytes);

                    m_Request.uploadHandler.contentType = "application/json";
                }

                if (m_RawPayload != null)
                {
                    // Configure raw byte upload

                    MelcherLog.Assert(!string.IsNullOrWhiteSpace(m_RawPayloadContentType));

                    MelcherLog.Assert(m_Request.uploadHandler == null);

                    m_Request.uploadHandler = new UploadHandlerRaw(m_RawPayload);

                    m_Request.uploadHandler.contentType = m_RawPayloadContentType;
                }


                if (m_OnSetup != null)
                {
                    // Allow the user to intercept the request before it starts
                    // and tune its values.

                    System.Exception setupError;

                    try
                    {
                        m_OnSetup.Invoke(this, out setupError);
                    }
                    catch (System.Exception e)
                    {
                        // Catch uncaught user exceptions so the request can
                        // recover via its callback.

                        setupError = e;
                    }

                    if (setupError != null)
                    {
                        if (onRequestCompleted != null)
                        {
                            onRequestCompleted.Invoke(this, setupError);
                        }
                        else
                        {
                            MelcherLog.LogError(setupError);
                        }

                        yield break;
                    }
                }

                // Verify callers are using the builder's timeout feature
                // because it offers more flexibility and avoids console spam
                // related to curl errors when a request times out.

                MelcherLog.Assert(m_TimeoutMilliseconds >= 0);

                MelcherLog.Assert(
                    m_Request.timeout == 0,
                    "Set a web request's timeout through the builder because it is more flexible and doesn't cause console spam."
                );

                m_Request.timeout = 0;

                // Log a start message if the user hasn't suppressed it.

                bool writeLogMessages = m_RequestOptions.HasFlag(RequestOption.Verbose);

                string requestId = null;

                if (writeLogMessages || runtimeDebug)
                {
                    try
                    {
                        LogRequestStart(this, out requestId);
                    }
                    catch (System.Exception e)
                    {
                        MelcherLog.LogError($"Exception thrown while logging request start:\n{e}");
                    }
                }

                if (m_RequestOptions.HasFlag(RequestOption.DryRun))
                {
                    // Dry-runs exit here. The request and its payload have been
                    // logged and verified. The next step would be to dispatch
                    // the request which dry-runs must not do.

                    // If callers want dry runs to be considered successful,
                    // they must handle it manually by matching the error to
                    // `DryRunError`.

                    onRequestCompleted?.Invoke(this, error: DryRunError);

                    yield break;
                }

                // Dispatch the request and immediately start measuring the
                // timeout.

                UnityWebRequestAsyncOperation process =
                    m_Request.SendWebRequest();

                double? timeoutRealtimeSinceStartup = null;

                if (m_TimeoutMilliseconds > 0)
                {
                    const double MillisecondsPerSecond = 1000.0;

                    double timeoutInSeconds =
                        m_TimeoutMilliseconds / MillisecondsPerSecond;

                    timeoutRealtimeSinceStartup =
                        Time.realtimeSinceStartupAsDouble + timeoutInSeconds;
                }

                try
                {
                    // Notify the user that the request was started and allow
                    // them to monitor the async operation handle.

                    m_OnStart?.Invoke(this, process);
                }
                catch (System.Exception e)
                {
                    // An exception in `onStart` does not stop the request.

                    Debug.LogException(e);
                }

                System.Exception errorFromRequest = null;

                while (!process.isDone)
                {
                    // Wait for the request to finish or timeout.

                    if (timeoutRealtimeSinceStartup.HasValue && Time.realtimeSinceStartupAsDouble >= timeoutRealtimeSinceStartup)
                    {
                        m_Request.Abort();

                        errorFromRequest = TimedOutError;

                        break;
                    }

                    yield return Timing.WaitForOneFrame;
                }

                if (errorFromRequest == null && !string.IsNullOrEmpty(m_Request.error))
                {
                    errorFromRequest = new System.Exception(m_Request.error);
                }

                if (writeLogMessages || runtimeDebug)
                {
                    try
                    {
                        LogRequestEnd(requestId, this, errorFromRequest);
                    }
                    catch (System.Exception e)
                    {
                        MelcherLog.LogError($"Exception thrown while logging request start:\n{e}");
                    }
                }

                if (errorFromRequest != null)
                {
                    if (onRequestCompleted != null)
                    {
                        onRequestCompleted.Invoke(this, error: errorFromRequest);
                    }
                    else
                    {
                        MelcherLog.LogError(errorFromRequest);
                    }

                    yield break;
                }

                onRequestCompleted?.Invoke(this, null);
            }
        }

        protected void Reset()
        {
            m_Method        = default;
            m_Url           = default;
            m_AutoCreateDownloadBuffer = true;
            m_ResponseType  = ResponseType.Unspecified;

            m_UrlParameters.Clear();
            m_Headers.Clear();
            m_FormData.Clear();

            m_JsonPayload   = default;
            m_RawPayload    = default;
            m_UserData      = default;
            m_OnSetup       = default;
            m_OnStart       = default;
            m_OnCompleted   = default;
            m_OnSuccess     = default;
            m_OnError       = default;
            m_OnSuccessJson = default;
            m_OnErrorJson   = default;

            m_RequestOptions        = RequestOption.Default;
            m_TimeoutMilliseconds   = 0;
            m_Request       = default;
        }

        // =====================================================================
        // Static internal functions
        // =====================================================================

        private static MelcherWebRequestBuilder GetNextRequestBuilder(string url, string method)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new System.ArgumentException("URL is null or empty", nameof(url));
            }

            MelcherWebRequestBuilder builder = s_BuilderPool.GetObject();
            builder.Reset();
            builder.m_Url = url;
            builder.m_Method = method;
            return builder;
        }

        /// <summary>
        ///     An internal function to try to interpret a server response as
        ///     JSON. This expects <paramref name="builder"/> to have a non-null
        ///     <see cref="m_Request"/>.
        /// </summary>
        protected static void OnInternalSuccessJson(MelcherWebRequestBuilder builder)
        {
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        /// <summary>
        ///     When <see langword="true"/>, the full contents of all web
        ///     requests are logged even if the app is a release build or the
        ///     request is not configured with the verbose flag. This feature
        ///     is intended to help debug issues in release builds and should
        ///     be used with extreme care: When this is enabled, it is possible
        ///     to leak sensitive user data to the log files.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.ResetToTypeDefault]
        public static bool runtimeDebug;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        protected static MelcherObjectPoolCyclic<MelcherWebRequestBuilder> s_BuilderPool =
            MelcherObjectPoolCyclic<MelcherWebRequestBuilder>.MakeDefault<MelcherWebRequestBuilder>(8);

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        protected static readonly List<string> s_FormattedUrlArgs = new List<string>();

        // =====================================================================
        // Internal member fields
        // =====================================================================

        protected string m_Method;

        protected string m_Url;

        protected bool m_AutoCreateDownloadBuffer;

        protected ResponseType m_ResponseType;

        protected List<KeyValuePair<string, string>> m_UrlParameters = new List<KeyValuePair<string, string>>();

        protected Dictionary<string, string> m_Headers = new Dictionary<string, string>();

        protected List<IMultipartFormSection> m_FormData = new List<IMultipartFormSection>();

        protected MelcherJsonValue? m_JsonPayload;

        protected byte[] m_RawPayload;

        protected string m_RawPayloadContentType;

        protected object m_UserData;

        protected OnRequestSetup m_OnSetup;
        protected OnRequestStart m_OnStart;

        protected OnRequestCompleted m_OnCompleted;
        protected OnRequestSuccess m_OnSuccess;
        protected OnRequestError m_OnError;

        protected OnJsonRequestCompleted m_OnJsonCompleted;
        protected OnRequestSuccessJson m_OnSuccessJson;
        protected OnRequestErrorJson m_OnErrorJson;

        protected RequestOption m_RequestOptions;

        protected int m_TimeoutMilliseconds;

        protected UnityWebRequest m_Request;

        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        /// Extra flags that modify how a request is performed.
        /// </summary>
        [System.Flags]
        protected enum RequestOption
        {
            Default = Verbose,

            /// <summary>
            ///     Simulate everything about the request except for actually
            ///     sending it. The request's <c>onError</c> callback will be
            ///     invoked (if non-null) with the error <see
            ///     cref="MelcherWebRequestBuilder.DryRunMessage"/>.
            /// </summary>
            DryRun = 1 << 0,

            /// <summary>
            ///     Write information about the request's start and end to the
            ///     log file.
            /// </summary>
            Verbose = 1 << 1

        }   // enum RequestOption


        /// <summary>
        ///     Type of server response expected.
        /// </summary>
        protected enum ResponseType
        {
            Unspecified,

            Raw,

            /// <summary>
            ///     The server will return a JSON response.
            /// </summary>
            Json,

        }   // enum ResponseMode

        public delegate void OnRequestSetup(MelcherWebRequestBuilder builder, out System.Exception setupError);
        public delegate void OnRequestStart(MelcherWebRequestBuilder builder, UnityWebRequestAsyncOperation process);

        /// <summary>
        ///     This is the new callback intended to replace <see
        ///     cref="OnRequestSuccess"/> and <see cref="OnRequestError"/>. It
        ///     allows users to better implement error checking specific to
        ///     their API.
        /// </summary>
        /// <param name="builder">
        ///     Contains the request data entered by the user as well as the
        ///     server's response.
        /// </param>
        /// <param name="error">
        ///     The type of error that occurred. This is <see langword="null"/>
        ///     if no low-level error occured. It is an exception to allow
        ///     users to use static type matching rather than error-prone
        ///     string matching.
        /// </param>
        public delegate void OnRequestCompleted(MelcherWebRequestBuilder builder, System.Exception error);
        public delegate void OnRequestSuccess(MelcherWebRequestBuilder builder);
        public delegate void OnRequestError(MelcherWebRequestBuilder builder, System.Exception error);

        /// <summary>
        ///     This is the new callback intended to replace <see
        ///     cref="OnRequestSuccessJson"/> and <see
        ///     cref="OnRequestErrorJson"/>. It allows users to better
        ///     implement error checking specific to their API.
        /// </summary>
        /// <inheritdoc cref="OnRequestCompleted"/>
        /// <param name="jsonResponse">
        ///     The parsed JSON data.
        /// </param>
        public delegate void OnJsonRequestCompleted(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonResponse);
        public delegate void OnRequestSuccessJson(MelcherWebRequestBuilder builder, MelcherJsonValue jsonResponse);
        public delegate void OnRequestErrorJson(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonResponse);

    }   // class MelcherWebRequestBuilder

}   // namespace Melcher
