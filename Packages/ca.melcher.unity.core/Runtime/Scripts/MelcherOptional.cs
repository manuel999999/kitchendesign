namespace Melcher
{
    /// <summary>
    ///     Allows any type to act similar to <see cref="System.Nullable{T}"/>,
    ///     including reference types. Importantly, it differentiates between
    ///     <see langword="null"/> and "not specified" (almost like a
    ///     null-<see langword="null"/>).
    ///     <br /><br />
    ///
    ///     <code>
    ///         void PrintHasValue(MelcherOptional<string> optional)
    ///         {
    ///             MelcherLog.Log(optional.hasValue);
    ///         }
    ///
    ///         PrintHasValue(default); // outputs: "false"
    ///         PrintHasValue(null);    // outputs: "true"
    ///         PrintHasValue("hello"); // outputs: "true"
    ///     </code>
    ///
    ///     The purpose of this type is to allow function arguments to be
    ///     optional but still allow <see langword="null"/> to be a valid
    ///     (non-empty) value.
    ///     <br /><br />
    ///
    ///     The perk of <see cref="MelcherOptional{ElementType}"/> is needing
    ///     fewer function overloads when dealing with optional parameters.
    ///     <br /><br />
    ///
    ///     For example, <see cref="MelcherJsonObjectParser"/> uses <see
    ///     cref="MelcherOptional{ElementType}"/> as the type for `defaultValue`
    ///     in <see cref="MelcherJsonObjectParser.GetValue(string, out string,
    ///     MelcherOptional{string})"/> to allow <see langword="null"/> as a
    ///     valid default value.
    /// </summary>
    public struct MelcherOptional<ElementType>
    {
        // =====================================================================
        // Static properties
        // =====================================================================

        /// <summary>
        ///     Return an empty (<see cref="hasValue"/> = <see langword="false"/>)
        ///     optional.
        /// </summary>
        public static MelcherOptional<ElementType> empty => default;

        // =====================================================================
        // Constructors
        // =====================================================================

        /// <summary>
        ///     Create an optional instance. The optional is valid even if
        ///     <paramref name="initialValue"/> is <see langword="null"/> or
        ///     <see langword="default"/>.
        /// </summary>
        /// <remarks>
        ///     To construct an empty (non-valid) optional, call its default
        ///     constructor, preferably using the keyword <see
        ///     langword="default"/> shorthand or the static <see cref="empty"/>
        ///     property.
        /// </remarks>
        public MelcherOptional(ElementType initialValue)
        {
            m_HasValue = true;
            m_Value = initialValue;
        }

        /// <summary>
        ///     Implicitly cast an <typeparamref name="ElementType"/> value into
        ///     <see cref="MelcherOptional{ElementType}"/> instance. Any
        ///     instance constructed like this will always return <see
        ///     langword="true"/> from its <see cref="hasValue"/> property.
        ///     <br /><br />
        ///
        ///     The implicit conversion operator makes it convenient to call
        ///     functions using <see cref="MelcherOptional{ElementType}"/>
        ///     without manually handling types.
        /// </summary>
        public static implicit operator MelcherOptional<ElementType>(ElementType initialValue)
        {
            return new MelcherOptional<ElementType>(initialValue);
        }

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        ///     Return <see langword="true"/> if <see cref="value"/> can be
        ///     called to retrieve a valid value. Otherwise, return <see
        ///     langword="false"/> to indicate that calling <see cref="value"/>
        ///     will throw an exception.
        /// </summary>
        public bool hasValue => m_HasValue;

        /// <summary>
        ///     Set or retrieve the valid value. The getter throws an exception
        ///     if a valid value isn't assigned (see <see cref="hasValue"/>).
        ///     <br /><br />
        ///
        ///     To clear a valid value, call <see cref="Reset"/> with no
        ///     arguments.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     Thrown if <see cref="hasValue"/> is <see langword="false"/>.
        /// </exception>
        public ElementType value
        {
            get
            {
                if (!m_HasValue)
                {
                    throw new System.InvalidOperationException($"Called {nameof(this.value)} when {nameof(hasValue)} is false.");
                }

                return m_Value;
            }

            set
            {
                m_HasValue = true;
                m_Value = value;
            }
        }

        /// <summary>
        ///     Return the type of the generic argument <typeparamref
        ///     name="ElementType"/>.
        /// </summary>
        public System.Type GetUnderlyingType()
        {
            return typeof(ElementType);
        }

        /// <summary>
        ///     Fetch the underlying value into <paramref name="outValue"/>.
        /// </summary>
        /// <returns>
        ///     <see cref="hasValue"/>.
        /// </returns>
        public bool TryGetValue(out ElementType outValue)
        {
            outValue = m_Value;
            return m_HasValue;
        }

        /// <summary>
        ///     Clear the valid value. This is the only way to make <see
        ///     cref="hasValue"/> return <see langword="false"/>.
        /// </summary>
        public void Reset()
        {
            m_HasValue = false;
            m_Value = default;
        }

        /// <summary>
        ///     Change the valid value. This is cause <see cref="hasValue"/>
        ///     to return <see langword="true"/>.
        /// </summary>
        public void Reset(ElementType newValue)
        {
            m_HasValue = true;
            m_Value = newValue;
        }

        // =====================================================================
        // Internal fields
        // =====================================================================

        /// <summary>
        ///     Whether <see cref="m_Value"/> has had an explicit value set.
        /// </summary>
        private bool m_HasValue;

        /// <summary>
        ///     The underlying value. Meant to be accessed by calling <see
        ///     cref="value"/> or <see cref="TryGetValue(out ElementType)"/>.
        /// </summary>
        private ElementType m_Value;

    }   // struct MelcherOptional

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-25
/// ===========================================================================
