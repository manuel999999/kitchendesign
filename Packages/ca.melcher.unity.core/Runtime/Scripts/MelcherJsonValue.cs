namespace Melcher
{
    using JsonObject = System.Collections.Generic.IDictionary<string, object>;
    using JsonList = System.Collections.Generic.IList<object>;

    /// <summary>
    ///     Represents any JSON value: object, list, string, number or bool.
    ///     Its purpose is to allow functions to declare parameters that accept
    ///     any JSON value, while still maintaining strong type checking.
    ///     <br /><br />
    ///
    ///     The exact type of the underlying JSON value can be inspected using
    ///     <see cref="valueType"/>. The type can also be checked more tersely
    ///     using one of the "is" properties: <see cref="isList"/>, <see
    ///     cref="isInteger"/>, etc.
    ///     <br /><br />
    ///
    ///     A use case example is <see cref="MelcherWebRequestBuilder.OnJsonRequestSuccess"/>.
    ///     It receives an argument of type <see cref="MelcherJsonValue"/> which
    ///     is the result of parsing a server's JSON response. To support any
    ///     valid JSON response, the callback uses <see cref="MelcherJsonValue"/>
    ///     to accept any JSON that the server might return.
    ///     <br /><br />
    ///
    ///     A note on performance: This boxes value types (e.g., long, double,
    ///     bool, etc.). Therefore, there is some GC overhead to using this
    ///     type, despite it being a <see langword="struct"/>.
    ///     <br /><br />
    ///
    ///     Integer types are stored as <see cref="long"/> to provide support
    ///     for the largest numbers.
    ///     <br /><br />
    ///
    ///     Floating point types are stored as <see cref="double"/> for the
    ///     maximum precision.
    ///     <br /><br />
    /// </summary>
    public readonly struct MelcherJsonValue
    {
        // ================================================================
        // Constructors
        // ================================================================

        public MelcherJsonValue(JsonList list)
        {
            m_ValueType = typeof(JsonList);
            m_Value     = list;
        }

        public MelcherJsonValue(JsonObject jsonObject)
        {
            m_ValueType = typeof(JsonObject);
            m_Value     = jsonObject;
        }

        public MelcherJsonValue(string text)
        {
            m_ValueType = typeof(string);
            m_Value     = text;
        }

        public MelcherJsonValue(double? floating)
        {
            m_ValueType = typeof(double);
            m_Value     = floating;
        }

        public MelcherJsonValue(long? integer)
        {
            m_ValueType = typeof(long);
            m_Value     = integer;
        }

        public MelcherJsonValue(bool? boolean)
        {
            m_ValueType = typeof(bool);
            m_Value     = boolean;
        }

        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        ///     Get the type that this value was constructed with. This may be
        ///     <see langword="null"/> if this object was default constructed.
        /// </summary>
        public System.Type valueType => m_ValueType;

        /// <summary>
        ///     Untyped access to the underlying value. Useful when needing to
        ///     serialize the underlying value without otherwise understanding
        ///     it.
        /// </summary>
        public object rawValue => m_Value;

        // =====================================================================
        // Properties - Quick type checks
        // =====================================================================

        /// <summary>
        ///     Return <see langword="true"/> if the underlying data has not
        ///     been initialized. For example, this will happen with a default
        ///     constructed <see cref="MelcherJsonValue"/> but may also be
        ///     useful in other places, like a server API that returns a blank
        ///     JSON message.
        /// </summary>
        public bool isEmpty => m_ValueType == null;

        /// <summary>
        ///     Return <see langword="true"/> if the underlying data is a JSON
        ///     object. Otherwise, return <see langword="false"/>.
        /// </summary>
        public bool isObject => m_ValueType == typeof(JsonObject);

        /// <summary>
        ///     Return <see langword="true"/> if the underlying data is a list.
        ///     Otherwise, return <see langword="false"/>.
        /// </summary>
        public bool isList => m_ValueType == typeof(JsonList);

        /// <summary>
        ///     Return <see langword="true"/> if the underlying data is a
        ///     <see cref="string"/>, including if the value is <see
        ///     langword="null"/>. Otherwise, return <see langword="false"/>.
        /// </summary>
        public bool isText => m_ValueType == typeof(string);

        /// <summary>
        ///     Return <see langword="true"/> if the underlying data is a
        ///     <see cref="double"/>, including if the value is <see
        ///     langword="null"/>. Otherwise, return <see langword="false"/>.
        /// </summary>
        public bool isFloating => m_ValueType == typeof(double);

        /// <summary>
        ///     Return <see langword="true"/> if the underlying data is a
        ///     <see cref="long"/>, including if the value is <see
        ///     langword="null"/>. Otherwise, return <see langword="false"/>.
        /// </summary>
        public bool isInteger => m_ValueType == typeof(long);

        /// <summary>
        ///     Return <see langword="true"/> if the underlying data is a
        ///     <see cref="bool"/>, including if the value is <see
        ///     langword="null"/>. Otherwise, return <see langword="false"/>.
        /// </summary>
        public bool isBoolean => m_ValueType == typeof(bool);

        // =====================================================================
        // Properties - Get value by type
        // =====================================================================

        /// <summary>
        ///     Return the wrapped JSON object. This will be <see
        ///     langword="null"/> if the value is not a JSON object.
        /// </summary>
        public JsonObject jsonObject => m_Value as JsonObject;

        /// <summary>
        ///     Return the wrapped JSON list. This will be <see
        ///     langword="null"/> if the value is not a JSON list.
        /// </summary>
        public JsonList list => m_Value as JsonList;

        /// <summary>
        ///     Return the wrapped <see cref="string"/>. This will also be <see
        ///     langword="null"/> if the type does not match.
        /// </summary>
        public string text => m_Value as string;

        /// <summary>
        ///     Return the wrapped <see cref="double"/>. This will also be <see
        ///     langword="null"/> if the type does not match.
        /// </summary>
        public double? floating => (isFloating && m_Value != null) ? (double?)m_Value : null;

        /// <summary>
        ///     Return the wrapped <see cref="long"/>. This will also be <see
        ///     langword="null"/> if the type does not match.
        /// </summary>
        public long? integer => (isInteger && m_Value != null) ? (long?)m_Value : null;

        /// <summary>
        ///     Return the wrapped <see cref="bool"/>. This will also be <see
        ///     langword="null"/> if the type does not match.
        /// </summary>
        public bool? boolean => (isBoolean && m_Value != null) ? (bool?)m_Value : null;

        // ================================================================
        // Internal fields
        // ================================================================

        private readonly System.Type m_ValueType;
        private readonly object m_Value;

    }   // struct MelcherJsonValue


}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-18
/// ===========================================================================
