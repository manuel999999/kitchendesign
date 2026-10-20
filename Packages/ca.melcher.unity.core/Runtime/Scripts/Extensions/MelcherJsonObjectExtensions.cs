namespace Melcher
{
    using System.Globalization;

    using JsonObject = System.Collections.Generic.IDictionary<string, object>;

    /// <summary>
    /// A collection of extension methods to lookup data from JSON. These
    /// methods return an optional <see cref="MelcherJsonKeyResult"/> object
    /// which stores a full-picture of the request and any error that may have
    /// occurred.
    /// <br /><br />
    ///
    /// These are low-level functions not intended for direct use. Developers
    /// should prefer the high-level <see cref="MelcherJsonObjectParser"/>
    /// class. It's API is designed to extract structured JSON more succinctly
    /// and to simultaneously manage the error state bookkeeping.
    /// </summary>
    public static class MelcherJsonObjectExtensions
    {
        /// <summary>
        /// <inheritdoc cref="TryGetValueFromJson{ValueType}(JsonObject, string, out ValueType, out MelcherJsonKeyResult, MelcherJsonValueTypeOption, CultureInfo)"/>
        /// <br /><br />
        ///
        /// This overload drops the output <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public static bool TryGetValueFromJson<ValueType>(
            this    JsonObject                  jsonObject,
                    string                      key,
            out     ValueType                   value,
                    MelcherJsonValueTypeOption  options     = MelcherJsonValueTypeOption.Default,
                    CultureInfo                 cultureInfo = null)
            where ValueType : struct
        {
            return jsonObject.TryGetValueFromJson(key, out value, out MelcherJsonKeyResult _, options, cultureInfo);
        }

        /// <summary>
        /// Get a <c>bool</c>, <c>int</c>, <c>float</c>, or <c>double</c>, etc.
        /// </summary>
        public static bool TryGetValueFromJson<ValueType>(
            this    JsonObject                  jsonObject,
                    string                      key,
            out     ValueType                   value,
            out     MelcherJsonKeyResult        result,
                    MelcherJsonValueTypeOption  options     = MelcherJsonValueTypeOption.Default,
                    CultureInfo                 cultureInfo = null)
            where ValueType : struct
        {
            value = default;

            // Use invariant culture if unspecified. A custom culture may be
            // desired to interpret data more naturally for some users (e.g.,
            // using commas for decimal points)
            cultureInfo ??= CultureInfo.InvariantCulture;

            if (key == null || !jsonObject.TryGetValue(key, out object intermediateValue))
            {
                // Couldn't find the key in the dictionary
                result = MelcherJsonKeyResult.KeyNotFound<ValueType>(key);
                return false;
            }

            if (intermediateValue == null)
            {
                if (options.HasFlag(MelcherJsonValueTypeOption.AllowNullValueTypes))
                {
                    // When requested, we'll interpret null value types as their
                    // default value
                    intermediateValue = default(ValueType);
                }
                else
                {
                    // Otherwise, value types shouldn't be null
                    result =
                        new MelcherJsonKeyResult(
                            key: key,
                            keyExists: true,
                            requestedType: typeof(ValueType),
                            sourceValue: intermediateValue,
                            exception: new System.IO.InvalidDataException(GetExceptionMessage($"Null data for value-type and implicit default conversion is disabled."))
                        );
                    return false;
                }
            }

            result = MelcherJsonKeyResult.Success<ValueType>(key, intermediateValue);

            if (intermediateValue is ValueType castedValue)
            {
                // Found the key and it matches the requested type exactly
                value = castedValue;
                return true;
            }

            if (!options.HasFlag(MelcherJsonValueTypeOption.AllowTypeConversion))
            {
                // Conversion is disabled by the user
                result =
                    new MelcherJsonKeyResult(
                        key: key,
                        keyExists: true,
                        requestedType: typeof(ValueType),
                        sourceValue: intermediateValue,
                        exception: new System.IO.InvalidDataException(
                            GetExceptionMessage($"Type mismatch and conversion is disabled."),
                            innerException: s_InvalidCastException
                        )
                    );
                return false;
            }

            System.Exception exception = null;

            try
            {
                // Try to convert to the requested type
                value =
                    (ValueType)System.Convert.ChangeType(
                        value: intermediateValue,
                        conversionType: typeof(ValueType),
                        provider: cultureInfo
                    );

                return true;
            }
            catch (System.InvalidCastException e)
            {
                exception =
                    new System.IO.InvalidDataException(
                        GetExceptionMessage($"Unsupported conversion."),
                        innerException: e
                    );
            }
            catch (System.FormatException e)
            {
                exception =
                    new System.IO.InvalidDataException(
                        GetExceptionMessage($"Invalid value format."),
                        innerException: e
                    );
            }
            catch (System.OverflowException e)
            {
                exception =
                    new System.IO.InvalidDataException(
                        GetExceptionMessage($"Value overflow."),
                        innerException: e
                    );
            }

            // The success path has already early-exited
            MelcherLog.Assert(exception != null);

            result =
                new MelcherJsonKeyResult(
                    key: key,
                    keyExists: true,
                    requestedType: typeof(ValueType),
                    sourceValue: intermediateValue,
                    exception: exception
                );

            return false;

            /// <summary>
            /// Return a string to be used as an exception error message. The
            /// returned string is prefixed with <paramref name="message"/> and
            /// is followed by more text describing the requested JSON: the key
            /// name, expected type vs actual type, etc.
            /// </summary>
            string GetExceptionMessage(string message)
            {
                return GetExceptionMessage<ValueType>(key, intermediateValue, message);
            }
        }

        /// <summary>
        /// <inheritdoc cref="TryGetValueFromJson{ReferenceType}(JsonObject, string, out ReferenceType, out MelcherJsonKeyResult)"/>
        /// <br /><br />
        ///
        /// This overload drops the output <see cref="MelcherJsonKeyResult"/>.
        /// </summary>
        public static bool TryGetValueFromJson<ReferenceType>(
            this    JsonObject      jsonObject,
                    string          key,
            out     ReferenceType   reference)
            where ReferenceType : class
        {
            return jsonObject.TryGetValueFromJson(key, out reference, out MelcherJsonKeyResult _);
        }

        /// <summary>
        /// Get a <c>string</c>, <c>JsonObject</c>, or <c>JsonList</c> by key
        /// name.
        /// </summary>
        public static bool TryGetValueFromJson<ReferenceType>(
            this    JsonObject              jsonObject,
                    string                  key,
            out     ReferenceType           reference,
            out     MelcherJsonKeyResult    result)
            where ReferenceType : class
        {
            reference = default;

            if (key == null || !jsonObject.TryGetValue(key, out object intermediateValue))
            {
                result = MelcherJsonKeyResult.KeyNotFound<ReferenceType>(key);
                return false;
            }

            ReferenceType castedValue = intermediateValue as ReferenceType;

            if (intermediateValue != null && castedValue == null)
            {
                // Failed to cast
                result =
                    new MelcherJsonKeyResult(
                        key: key,
                        keyExists: true,
                        requestedType: typeof(ReferenceType),
                        sourceValue: intermediateValue,
                        exception: new System.IO.InvalidDataException(
                            GetExceptionMessage<ReferenceType>(key, intermediateValue, "Type mismatch."),
                            innerException: s_InvalidCastException
                        )
                    );

                return false;
            }

            result = MelcherJsonKeyResult.Success<ReferenceType>(key, castedValue);
            reference = castedValue;
            return true;
        }

        /// <summary>
        /// Create a verbose exception message. This is intended to provide a
        /// transparent picture of what JSON was requested and why it failed.
        /// </summary>
        private static string GetExceptionMessage<RequestedType>(string key, object intermediateValue, string message)
        {
            if (key == null)
            {
                throw new System.ArgumentNullException(nameof(key));
            }

            if (message == null)
            {
                throw new System.ArgumentNullException(nameof(message));
            }

            string actualTypeName = "null";
            string actualValueAsString = "null";

            if (intermediateValue != null)
            {
                actualTypeName = intermediateValue.GetType().FullName;

                try
                {
                    actualValueAsString = intermediateValue.ToString();
                }
                catch (System.Exception e)
                {
                    actualValueAsString = $"<error during value's ToString(): {e.Message}>";
                }
            }

            return $"{message} The data at key '{key}' is type {actualTypeName}. The type {typeof(RequestedType).FullName} was requested. Key's value is: {actualValueAsString}";
        }

        // =====================================================================
        // Internal static fields
        // =====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly System.InvalidCastException s_InvalidCastException =
            new System.InvalidCastException("Specified cast is not valid.");

    }   // class MelcherJsonObjectExtensions

    /// <summary>
    /// Options to control how C# value-type data is extracted from JSON.
    /// </summary>
    [System.Flags]
    public enum MelcherJsonValueTypeOption
    {
        Default = AllowTypeConversion,

        /// <summary>
        /// Allow conversions between types where possible. E.g., parse an <see
        /// cref="int"/> from a <see cref="string"/>; allow a <see
        /// cref="double"/> to be cast to a <see cref="float"/>; etc.
        /// </summary>
        AllowTypeConversion = 1 << 0,

        /// <summary>
        /// For value types, treat <see langword="null"/> in the JSON as the
        /// type's <see langword="default"/> value. E.g., for an <see
        /// cref="int"/>, <see langword="null"/> will become <c>0</c>.
        /// </summary>
        AllowNullValueTypes = 1 << 2,

        Lenient = AllowTypeConversion | AllowNullValueTypes

    }   // enum MelcherJsonValueTypeOption

}   // namespace Melcher

