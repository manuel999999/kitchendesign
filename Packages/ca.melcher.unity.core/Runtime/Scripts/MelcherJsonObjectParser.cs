namespace Melcher
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;

    using JsonObject = System.Collections.Generic.IDictionary<string, object>;
    using JsonList = System.Collections.Generic.IList<object>;

    /// <summary>
    ///     A utility object for extracting structured JSON fields and logging
    ///     missing key names. It is useful for having terse, yet robust, JSON
    ///     parsing.
    ///     <br /><br />
    ///
    ///     Key values can be requested succinctly using function-chaining. Any
    ///     keys that are not found, or cannot be extracted, are added to an
    ///     internal error list. This list is viewable from the <see
    ///     cref="errors"/> property and can be cleared using <see
    ///     cref="ClearErrors"/>
    ///     <br /><br />
    ///
    ///     A common implementation is to request all the desired keys blindly
    ///     and then check for errors afterwards with <see cref="hasErrors"/>.
    /// </summary>
    public class MelcherJsonObjectParser
    {
        // ====================================================================
        // Documentation
        // ====================================================================

#if UNITY_EDITOR
        /// <summary>
        ///     Example use:
        ///
        ///     <code>
        ///     JsonObject jsonObject = /* {
        ///         myInt: 4,
        ///         aString: "Hello there"
        ///     } */
        ///
        ///     MelcherJsonObjectParser jsonObjectParser = new MelcherJsonObjectParser(jsonObject)
        ///         .GetValue("myInt", out int myInt)
        ///         .GetValue("aString", out string aString)
        ///         .GetValue("foobar", out float foobar);
        ///
        ///     // The above will add an error because "foobar" doesn't exist in the
        ///     // object
        ///
        ///     if (jsonObjectParser.hasError)
        ///     {
        ///         foreach (var error in jsonObjectParser.errors)
        ///         {
        ///             // Compose an error message using errors
        ///         }
        ///     }
        ///     </code>
        /// </summary>
        private static class Documentation
        {
            // This is an empty class. It simply exists to anchor more C-Sharp
            // documentation in a way that doesn't bloat the outer class's
            // summary. This approach allows us to continue the documentation
            // using XML tags and ensure code references survive renames.

        }   // static class Documentation

#endif  // if UNITY_EDITOR

        // ====================================================================
        // Constructor
        // ====================================================================

        public MelcherJsonObjectParser(JsonObject jsonObject)
        {
            Reset(jsonObject);
        }

        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        /// Return true if any key-name since the last <see cref="Reset"/> call
        /// was not found.
        /// </summary>
        public bool hasErrors => m_Errors.Count > 0;

        public ReadOnlyCollection<MelcherJsonKeyResult> errors
            => m_ErrorsReadOnly ??= m_Errors.AsReadOnly();

        /// <summary>
        /// Return the current object. This can be <see langword="null"/> if an
        /// invalid object was pushed or non-existent key was used.
        /// </summary>
        public JsonObject currentObject => m_JsonStack.Peek().jsonObject;

        // ====================================================================
        // GetValue
        // ====================================================================

        /// <summary>
        /// <inheritdoc cref="GetValue{ValueType}(string, out ValueType, out MelcherJsonKeyResult)"/>
        /// <br /><br />
        ///
        /// This overload omits the output <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public MelcherJsonObjectParser GetValue<ValueType>(string key, out ValueType value, MelcherJsonValueTypeOption options = MelcherJsonValueTypeOption.Default)
            where ValueType : struct
        {
            return GetValue(key, out value, out MelcherJsonKeyResult _, options);
        }

        /// <summary>
        /// <inheritdoc cref="GetValue_Boilerplate"/>
        /// <br /><br />
        ///
        /// When the topmost JSON object is <see langword="null"/>, or the key
        /// name isn't found, or when the value cannot be assigned/converted
        /// from the type in the JSON, the <paramref name="value"/> is set to
        /// <see langword="default"/>. If this happens and the topmost JSON
        /// object is valid (non-null), an explanatory error object is added
        /// to the error list, <see cref="errors"/>. Use <see cref="hasErrors"/>
        /// to check if an error has occurred since the last call to <see
        /// cref="Reset"/>.
        /// </summary>
        public MelcherJsonObjectParser GetValue<ValueType>(string key, out ValueType value, out MelcherJsonKeyResult result, MelcherJsonValueTypeOption options = MelcherJsonValueTypeOption.Default)
            where ValueType : struct
        {
            value = default;
            result = MelcherJsonKeyResult.KeyNotFound<ValueType>(GetFullPathForKey(key));

            StackNode topNode = m_JsonStack.Peek();
            if (topNode.jsonObject == null)
            {
                // Return if the key's parent doesn't exist. We don't bother
                // reporting this as an error because it's parent has already
                // been reported as missing.
                return this;
            }

            if (!topNode.jsonObject.TryGetValueFromJson(key, out value, out result, options))
            {
                LogAndCaptureJsonKeyError(key, result);
            }

            return this;
        }

        /// <summary>
        /// <inheritdoc cref="GetValue{ValueType}(string, out ValueType, ValueType, out MelcherJsonKeyResult)"/>
        /// <br /><br />
        ///
        /// This overload omits the output <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public MelcherJsonObjectParser GetValue<ValueType>(
            string                      key,
            out ValueType               value,
            ValueType                   defaultValue,
            MelcherJsonValueTypeOption  options = MelcherJsonValueTypeOption.Default,
            CultureInfo                 cultureInfo = null)
            where ValueType : struct
        {
            return GetValue(key, out value, defaultValue, out MelcherJsonKeyResult _, options, cultureInfo);
        }

        /// <summary>
        /// <inheritdoc cref="GetValue_Boilerplate"/>
        /// <br /><br />
        ///
        /// A value is one of the plain data types:
        /// <c>bool</c>, <c>int</c>, <c>long</c>, <c>float</c>, etc.
        /// <br /><br />
        ///
        /// This overload will not log an error if the key is missing. If the
        /// value cannot be read for any reason (e.g., not defined or
        /// incompatible types, etc.), <paramref name="value"/> will be
        /// assigned <paramref name="defaultValue"/>.
        /// </summary>
        public MelcherJsonObjectParser GetValue<ValueType>(
            string                      key,
            out ValueType               value,
            ValueType                   defaultValue,
            out MelcherJsonKeyResult    result,
            MelcherJsonValueTypeOption  options = MelcherJsonValueTypeOption.Default,
            CultureInfo                 cultureInfo = null)
            where ValueType : struct
        {
            result = MelcherJsonKeyResult.KeyNotFound<ValueType>(GetFullPathForKey(key));

            StackNode topNode = m_JsonStack.Peek();
            if (topNode.jsonObject == null || !topNode.jsonObject.TryGetValueFromJson(key, out value, out result, options, cultureInfo))
            {
                if (result.exception != null && result.keyExists)
                {
                    LogAndCaptureJsonKeyError(key, result);
                }

                value = defaultValue;
            }

            return this;
        }

        /// <summary>
        /// <inheritdoc cref="GetValue(string, out string, string, out MelcherJsonKeyResult)"/>
        /// <br /><br />
        ///
        /// This overload omits the output <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public MelcherJsonObjectParser GetValue(string key, out string value, MelcherOptional<string> defaultValue = default)
        {
            return GetValue(key, out value, out MelcherJsonKeyResult _, defaultValue);
        }

        /// <summary>
        /// <inheritdoc cref="GetValue_Boilerplate"/>
        /// <br /><br />
        ///
        /// This overload handles <see cref="string"/> values.
        /// </summary>
        public MelcherJsonObjectParser GetValue(string key, out string value, out MelcherJsonKeyResult result, MelcherOptional<string> defaultValue = default)
        {
            value = default;
            result = MelcherJsonKeyResult.KeyNotFound<string>(GetFullPathForKey(key));

            StackNode topNode = m_JsonStack.Peek();

            if (!defaultValue.hasValue)
            {
                // No default value was provided. This means we will error if
                // a value cannot be retrieved.

                if (topNode.jsonObject == null)
                {
                    // Return if the key's parent doesn't exist. We don't bother
                    // reporting this as an error because it's parent has already
                    // been reported as missing.
                    return this;
                }

                if (!topNode.jsonObject.TryGetValueFromJson(key, out value, out result))
                {
                    LogAndCaptureJsonKeyError(key, result);
                }
            }
            else if (topNode.jsonObject == null || !topNode.jsonObject.TryGetValueFromJson(key, out value, out result))
            {
                // There was an error reading the value but because the user
                // provided a default value, it will be used as the resulting
                // value.

                // If there is an error and it's not a just a key-does-not-exist
                // error, it will be appended to the internal error list because
                // this likely indicates some type of type issue.
                if (result.exception != null && result.keyExists)
                {
                    LogAndCaptureJsonKeyError(key, result);
                }

                value = defaultValue.value;
            }

            return this;
        }

        // =====================================================================
        // GetList
        // =====================================================================

        /// <summary>
        /// <inheritdoc cref="GetList(string, out JsonList, out MelcherJsonKeyResult)"/>
        /// <br /><br />
        ///
        /// This overload omits the output <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public MelcherJsonObjectParser GetList(string key, out JsonList list, bool errorIfNotFound = true)
        {
            return GetList(key, out list, out MelcherJsonKeyResult _, errorIfNotFound);
        }


        /// <summary>
        /// Get a list by name. The list is not added to the internal parser
        /// state. If the list is a list of JSON objects, you may wish to push
        /// and then pop each element from this <see code="MelcherJsonObjectParser"/>
        /// to parse as needed.
        /// </summary>
        public MelcherJsonObjectParser GetList(string key, out JsonList list, out MelcherJsonKeyResult error, bool errorIfNotFound = true)
        {
            list = null;
            error = MelcherJsonKeyResult.KeyNotFound<JsonList>(GetFullPathForKey(key));

            StackNode topNode = m_JsonStack.Peek();
            if (topNode.jsonObject == null)
            {
                // Return if the key's parent doesn't exist. We don't bother
                // reporting this as an error because it's parent has already
                // been reported as missing.
                return this;
            }

            if (!topNode.jsonObject.TryGetValueFromJson(key, out list, out error) &&
                errorIfNotFound)
            {
                LogAndCaptureJsonKeyError(key, error);
            }

            return this;
        }

        // =====================================================================
        // GetAndPushObject
        // =====================================================================

        /// <summary>
        /// <inheritdoc cref="GetAndPushObject(string, out JsonObject, out MelcherJsonKeyResult)"/>
        /// <br /><br />
        ///
        /// This overload omits the output <see cref="JsonObject"/> and <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public MelcherJsonObjectParser GetAndPushObject(string key)
        {
            return GetAndPushObject(key, out JsonObject _, out MelcherJsonKeyResult _);
        }

        /// <summary>
        /// <inheritdoc cref="GetAndPushObject(string, out JsonObject, out MelcherJsonKeyResult)"/>
        /// <br /><br />
        ///
        /// This overload omits the output <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public MelcherJsonObjectParser GetAndPushObject(string key, out JsonObject jsonObject)
        {
            return GetAndPushObject(key, out jsonObject, out MelcherJsonKeyResult _);
        }

        /// <summary>
        /// Get a nested JSON object by name and make it the active JSON object
        /// in the parser. Future get-requests will query the new object until
        /// <see cref="PopObject"/> is called.
        /// </summary>
        /// <remarks>
        /// If the named object doesn't exist, the state of the parser is still
        /// modified to include a null object. Future get-requests will silently
        /// fail (i.e. not append an error message), relying on the fact that
        /// the error message from this failed call will represent any errors
        /// with its sub fields.
        /// </remarks>
        public MelcherJsonObjectParser GetAndPushObject(string key, out JsonObject jsonObject, out MelcherJsonKeyResult error)
        {
            if (!TryGetAndPushObject(key, out jsonObject, out error, errorIfMissing: true))
            {
                // An error is already logged by the previous function because
                // the object could not be retrieved

                // Add the JSON object, even if it is null. The other methods
                // know to silently ignore a null top node. This lets the parser
                // continue logging errors using the full path to the JSON key,
                // even when it's non-existent
                m_JsonStack.Push(new StackNode(jsonObject, key));
                RebuildPath();
            }

            return this;
        }

        // =====================================================================
        // Push/Pop
        // =====================================================================

        /// <summary>
        /// Push a JSON object so it becomes the most recent JSON object. I.e.,
        /// future get-requests will query this object until <see
        /// cref="PopObject"/> is called.
        /// <br /><br />
        ///
        /// This function is intended to be passed values from a JSON list and
        /// therefore takes the list element's index as the first argument.
        /// This is convenient for reusing the same parser to handle
        /// sub-objects.
        /// <br /><br />
        ///
        /// It is not necessary to call <see cref="PopObject"/> if there is no
        /// further use for the parser.
        /// </summary>
        public MelcherJsonObjectParser PushObject(int listIndex, JsonObject jsonObject)
        {
            m_JsonStack.Push(new StackNode(jsonObject, $"[{listIndex}]"));
            RebuildPath();
            return this;
        }

        /// <summary>
        /// Pop the last object. This will except if the node is the JSON root.
        /// </summary>
        public MelcherJsonObjectParser PopObject()
        {
            if (m_JsonStack.Count <= 1)
            {
                throw new System.InvalidOperationException("Cannot pop root object");
            }

            m_JsonStack.Pop();
            RebuildPath();
            return this;
        }

        // =====================================================================
        // TryGetAndPushObject
        // =====================================================================

        /// <summary>
        ///     Lookup a nested JSON object within the current topmost JSON
        ///     object. If a JSON object is found, it is pushed to the internal
        ///     stack.
        ///     <br /><br />
        ///
        ///     This overload omits the output <see cref="JsonObject"/>.
        /// </summary>
        public bool TryGetAndPushObject(string key, bool errorIfMissing = false)
        {
            return TryGetAndPushObject(key, out JsonObject _, errorIfMissing);
        }

        /// <summary>
        /// <inheritdoc cref="TryGetAndPushObject(string, out JsonObject, out MelcherJsonKeyResult)/>
        /// <br /><br />
        ///
        /// This overload omits the output <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public bool TryGetAndPushObject(string key, out JsonObject jsonObject, bool errorIfMissing = false)
        {
            return TryGetAndPushObject(key, out jsonObject, out MelcherJsonKeyResult _, errorIfMissing);
        }

        /// <summary>
        /// Lookup a nested JSON object within the topmost JSON object. If a
        /// JSON object is found, it is pushed to the internal stack and
        /// returned via <paramref name="jsonObject"/>.
        /// </summary>
        public bool TryGetAndPushObject(string key, out JsonObject jsonObject, out MelcherJsonKeyResult error, bool errorIfMissing = false)
        {
            jsonObject = null;
            error = MelcherJsonKeyResult.KeyNotFound<JsonObject>(GetFullPathForKey(key));

            StackNode topNode = m_JsonStack.Peek();
            if (topNode.jsonObject == null)
            {
                // Return if the key's parent doesn't exist. We don't bother
                // reporting this as an error because it's parent has already
                // been reported as missing.
                return false;
            }

            if (!topNode.jsonObject.TryGetValueFromJson(key, out jsonObject, out error))
            {
                if (error.keyExists || errorIfMissing)
                {
                    LogAndCaptureJsonKeyError(key, error);
                }

                return false;
            }

            m_JsonStack.Push(new StackNode(jsonObject, key));
            RebuildPath();
            return true;
        }

        // =====================================================================
        // TryGetErrorMap
        // =====================================================================

        /// <summary>
        /// <inheritdoc cref="TryGetErrorMap(string, out Dictionary{string, List{string}}, out MelcherJsonKeyResult)"/>
        /// <br /><br />
        ///
        /// This overload omits the output <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public bool TryGetErrorMap(string key, out Dictionary<string, List<string>> errorMap)
        {
            return TryGetErrorMap(key, out errorMap, out MelcherJsonKeyResult _);
        }

        /// <summary>
        /// Try to parse an error map.
        ///
        /// For example, a server's sign up API may return errors for multiple
        /// input fields.
        ///
        /// <code>
        /// {
        ///     "errors": {
        ///         "last_name": [
        ///             "Required field."
        ///         ],
        ///         "password": [
        ///             "Must be 8 characters or more.",
        ///             "Must have at least 1 symbol.",
        ///             "Must have at least 1 number."
        ///         ],
        ///         "email": [
        ///             "Invalid email"
        ///         ]
        ///     }
        /// }
        /// </code>
        ///
        /// An error map is defined as a dictionary of strings to string lists.
        /// Each key in the dictionary corresponds to a category (e.g., "email",
        /// or "password"). The string-lists for each category contains the zero
        /// or more errors associated with the group.
        /// </summary>
        /// <param name="key">
        /// The key name that the error map is defined under.
        /// </param>
        /// <param name="errorMap">
        /// The output variable to contain the parsed error map.
        /// </param>
        /// <returns>
        /// Return <see langword="true"/> if an error map was found and the
        /// output argument <paramref name="errorMap"/> contains entries;
        /// Otherwise, return <see langword="false"/> with <paramref name="errorMap"/>
        /// set to <see langword="null"/>.
        /// </returns>
        public bool TryGetErrorMap(string key, out Dictionary<string, List<string>> errorMap, out MelcherJsonKeyResult error)
        {
            errorMap = null;
            error = MelcherJsonKeyResult.KeyNotFound<JsonObject>(GetFullPathForKey(key));

            if (currentObject == null)
            {
                // Return if the key's parent doesn't exist. We don't bother
                // reporting this as an error because it's parent has already
                // been reported as missing.
                return false;
            }

            if (!currentObject.TryGetValueFromJson(key, out JsonObject errorMapRoot, out error))
            {
                if (error.keyExists)
                {
                    // Only worth considering an error if the key exists but the
                    // types do not match. If the key doesn't exist, we assume
                    // it's optional
                    LogAndCaptureJsonKeyError(key, error);
                }

                // Nothing to do if key doesn't exist / isn't a JsonObject
                return false;
            }

            if (errorMapRoot.Count <= 0)
            {
                // Ignore empty error maps
                return false;
            }

            errorMap = new Dictionary<string, List<string>>();

            // Extract the strings for every category. Even if the categories
            // are all empty, we'll still treat this as a valid error map

            foreach (KeyValuePair<string, object> errorMapEntry in errorMapRoot)
            {
                string category = errorMapEntry.Key;

                // Get the list of strings for this error category
                // TODO: Should this be using our TryGetJsonValue methods?
                if (!errorMap.TryGetValue(category, out List<string> errorsInCategory))
                {
                    // List for this category doesn't exist yet; create it.
                    errorsInCategory = new List<string>();
                    errorMap.Add(category, errorsInCategory);
                }

                // Append all the errors for this category to the list
                foreach (object errorLine in (JsonList)errorMapEntry.Value)
                {
                    errorsInCategory.Add(errorLine.ToString());
                }
            }

            return true;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Clear the error list so <see cref="hasErrors"/> returns false.
        /// </summary>
        public MelcherJsonObjectParser ClearErrors()
        {
            m_Errors.Clear();
            return this;
        }

        /// <summary>
        /// Clear the parser to reuse it with a new JSON object. This will
        /// clear the internal error list and cause <see cref="hasErrors"/> to
        /// return <see langkeyword="false"/>.
        /// </summary>
        public MelcherJsonObjectParser Reset(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                throw new System.ArgumentNullException(nameof(jsonObject));
            }

            m_JsonStack.Clear();
            m_JsonStack.Push(new StackNode(jsonObject, name: null));
            m_Errors.Clear();
            RebuildPath();

            return this;
        }

        // =====================================================================
        // Internal methods
        // =====================================================================

        /// <summary>
        ///     Compute the path to the active JSON object. The path is used to
        ///     build the full path for any key fetched.
        ///     <br /><br />
        ///
        ///     The root object has no name/key so it is omitted. The path
        ///     has the format: "key1 > key2 > key3 > ". The trailing ">"
        ///     ensures the path can easily be appended with any other key
        ///     to report an error with the direct JSON object path.
        /// </summary>
        private void RebuildPath()
        {
            if (m_JsonStack.Count <= 1)
            {
                // Root JSON objects have no name/key.
                m_CurrentPath = "";
                return;
            }

            const string Separator = " > ";

            // Make a path using each node's name (skipping the root)
            IEnumerable<string> pushedKeys =
                m_JsonStack.Skip(1).Select(node => node.name ?? "");

            m_CurrentPath = string.Join(Separator, pushedKeys);

            m_CurrentPath += Separator;
        }

        /// <summary>
        /// Return a path for a key at the current location.
        /// </summary>
        private string GetFullPathForKey(string key)
        {
            return $"{m_CurrentPath}{key}";
        }

        private void LogAndCaptureJsonKeyError(string key, in MelcherJsonKeyResult result)
        {
            string keyWithCurrentPath = $"{m_CurrentPath}{key}";

            // Clone the error but include the path in the key
            m_Errors.Add(
                new MelcherJsonKeyResult(
                    key: keyWithCurrentPath,
                    keyExists: result.keyExists,
                    requestedType: result.requestedType,
                    sourceValue: result.sourceValue,
                    exception: result.exception
                )
            );

            MelcherLog.LogErrorEditorOnly($"Error with JSON key \"{keyWithCurrentPath}\": {result.exception}");
        }

        // ====================================================================
        // Internal fields
        // ====================================================================

        /// <summary>
        /// The stack/history of JSON objects. Whenever an object is get'd, it
        /// is added to the stack. Future get's will attempt to query the
        /// topmost JSON object. The topmost JSON object can be null, in which
        /// case get-requests will silently fail (relying on the error message
        /// for the missing JSON object itself to convey the error).
        /// </summary>
        private Stack<StackNode> m_JsonStack = new Stack<StackNode>();

        /// <summary>
        /// List of all error results encountered during parsing.
        /// </summary>
        private List<MelcherJsonKeyResult> m_Errors = new List<MelcherJsonKeyResult>();

        /// <summary>
        /// A read-only view of <see cref="m_Errors"/>. Accessed via <see
        /// cref="errors"/>.
        /// </summary>
        private ReadOnlyCollection<MelcherJsonKeyResult> m_ErrorsReadOnly;

        /// <summary>
        /// The JSON path to the current object. The root node is unnamed.
        /// "sub_object > sub_object_2 > name > "
        /// </summary>
        private string m_CurrentPath = string.Empty;

        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        /// Names a JSON object.
        /// </summary>
        public struct StackNode
        {
            public StackNode(JsonObject jsonObject, string name)
            {
                this.jsonObject = jsonObject;
                this.name = name;
            }

            /// <summary>
            /// JSON object. Can be null when the object is not found. See
            /// <see cref="GetAndPushObject"/>
            /// </summary>
            public JsonObject jsonObject;

            /// <summary>
            /// Name of the JSON object. Null if this node is the root.
            /// </summary>
            public string name;
        }

#if UNITY_EDITOR
        // ====================================================================
        // CSharp Documentation
        // ====================================================================

        /// <summary>
        /// Attempt to lookup a JSON value by key-name from the most recent
        /// JSON object in the stack.
        /// <br /><br />
        ///
        /// The most recent JSON object in the stack is the newest of: <br />
        ///  - the last object that was pushed and yet to be popped<br />
        ///  - the object used to reset the parser<br />
        ///  - the object used to construct the parser<br />
        /// <br /><br />
        ///
        /// The most recent JSON object will be <see langword="null"/> if the
        /// user attempts to push a key-name that isn't defined at the current
        /// object.
        /// </summary>
        // This is only here so it can be <includedoc>'d for many different
        // functions.
        private static void GetValue_Boilerplate() { }
#endif  // if UNITY_EDITOR

    }   // struct MelcherJsonObjectParser

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
