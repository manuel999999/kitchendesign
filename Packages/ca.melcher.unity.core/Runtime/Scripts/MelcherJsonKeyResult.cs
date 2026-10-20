using System.Collections.Generic;

namespace Melcher
{
    /// <summary>
    ///     The result of a key-value lookup from a <c>JsonObject</c>.
    ///     <br /><br />
    ///
    ///     This type is used throughout Melcher's <c>JsonObject</c> extension
    ///     methods and <see cref="MelcherJsonObjectParser"/>.
    ///     <br /><br />
    /// 
    ///     The purpose of this type is to represent the details of a request
    ///     and any possible error that occurred. With this information, a
    ///     localizable error message can be built by user. Here are some common
    ///     causes of lookup errors:
    ///     <br /><br />
    ///
    ///     -   A key does not exist<br />
    ///     -   A value is <see langword="null"/> for a value type (long,
    ///         double, etc)<br />
    ///     -   The requested type doesn't match the type in JSON<br />
    ///     -   The requested type may overflow trying to represent the value
    ///         stored in JSON<br />
    ///     <br /><br />
    ///
    ///     It is by-design that this object doesn't hold error strings. It is
    ///     intended that the user checks <see cref="exception"/> for the type
    ///     of error that occurred, and for the user to compose their own error
    ///     message using the whatever details are necessary (e.g., <see
    ///     cref="requestedType"/>, <see cref="sourceValue"/>, etc.).
    ///     <br /><br />
    ///
    ///     If <see cref="exception"/> is <see langword="null"/>, no error has
    ///     occurred.
    ///     <br /><br />
    ///
    ///     Possible exception types:
    ///     <br /><br />
    ///
    ///     -   <see cref="System.Collections.Generic.KeyNotFoundException"/>
    ///         <br /><br />
    ///
    ///         JSON key not found.
    ///         <br /><br />
    ///
    ///     -   <see cref="System.ArgumentNullException"/>
    ///         <br /><br />
    ///
    ///         The JSON value is <see langword="null"/> when a value-type is
    ///         requested and <see cref="MelcherJsonValueTypeOption.AllowNullValueTypes"/>
    ///         isn't enabled.
    ///         <br /><br />
    ///
    ///     -   <see cref="System.ArgumentException"/>
    ///         <br /><br />
    ///
    ///         JSON value doesn't match requested type and <see
    ///         cref="MelcherJsonValueTypeOption.AllowTypeConversion"/>
    ///         isn't enabled.
    ///         <br /><br />
    ///
    ///     -   <see cref="System.InvalidCastException"/>
    ///         <br /><br />
    ///
    ///         JSON value doesn't match requested type and there is no
    ///         conversion operator defined.
    ///         <br /><br />
    ///
    ///     -   <see cref="System.FormatException"/>
    ///
    ///         Error parsing JSON value into requested type.
    ///         <br /><br />
    ///
    ///     -   <see cref="System.OverflowException"/>
    ///         <br /><br />
    ///
    ///         Overflow when JSON value was converted to requested
    ///         type.
    ///         <br /><br />
    ///
    /// </summary>
    public readonly struct MelcherJsonKeyResult
    {
        // =====================================================================
        // Constructors
        // =====================================================================

        /// <summary>
        /// Return an object that indicates the key exists and has no error.
        /// </summary>
        public static MelcherJsonKeyResult Success<RequestedType>(string key, object value)
        {
            return new MelcherJsonKeyResult(
                key: key,
                keyExists: true,
                requestedType: typeof(RequestedType),
                sourceValue: value,
                exception: null
            );
        }

        /// <summary>
        /// Return an object that indicates a key does not exist.
        /// </summary>
        public static MelcherJsonKeyResult KeyNotFound<RequestedType>(string key)
        {
            return new MelcherJsonKeyResult(
                key: key,
                keyExists: false,
                requestedType: typeof(RequestedType),
                sourceValue: null,
                exception: s_SharedKeyNotFoundException
            );
        }

        public MelcherJsonKeyResult(
            string              key,
            bool                keyExists,
            System.Type         requestedType,
            object              sourceValue     = null,
            System.Exception    exception       = null)
        {
            this.key = key;
            this.keyExists = keyExists;
            this.requestedType = requestedType;
            this.sourceValue = sourceValue;
            this.exception = exception;
        }

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        /// Get the "actual" data type that is stored in the JSON.
        /// </summary>
        public System.Type sourceValueType => sourceValue?.GetType();

        // =====================================================================
        // Fields
        // =====================================================================

        /// <summary>
        /// The JSON key name.
        /// </summary>
        public readonly string key;

        /// <summary>
        /// Indicates whether the <see cref="key"/> was defined in the JSON.
        /// </summary>
        public readonly bool keyExists;

        /// <summary>
        /// The type of data that was requested by the user. This may differ
        /// from the actual data used within the JSON.
        /// </summary>
        public readonly System.Type requestedType;

        /// <summary>
        /// The value that was found in the JSON. This will be <see
        /// langword="null"/> when the data is literally <see langword="null"/>
        /// or when the key isn't defined and nothing is found.
        /// </summary>
        public readonly object sourceValue;

        /// <summary>
        /// The error that occurred when a value was attempted to be extracted.
        /// This will be <see langword="null"/> if no error occurred.
        ///
        /// The reason this is an exception type rather than string is to allow
        /// more reliable classification of the error. I.e., preferring C# type-
        /// checking to string searching.
        /// </summary>
        public readonly System.Exception exception;

        // ====================================================================
        // Static fields
        // ====================================================================

        /// <summary>
        /// This exception is intended to be reused by multiple key results to
        /// keep garbage allocation to a minimum.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly KeyNotFoundException s_SharedKeyNotFoundException = new KeyNotFoundException();

    }   // struct MelcherJsonKeyResult

}   // namespace Melcher

