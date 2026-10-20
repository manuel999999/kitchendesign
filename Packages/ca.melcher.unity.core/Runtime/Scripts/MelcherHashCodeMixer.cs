namespace Melcher
{
    /// <summary>
    ///     A helper API for overriding <see cref="object.GetHashCode"/> in
    ///     custom types.
    ///     <br /><br />
    ///
    ///     E.g., implementing <see cref="System.IEquatable{T}"/> when it
    ///     is necessary to combine multiple fields that affect the object's
    ///     measurable state.
    ///     <br /><br />
    ///
    ///     This struct uses function chaining.
    ///     <br /><br />
    ///
    ///     Example:
    ///     <code>
    ///         public override int GetHashCode()
    ///         {
    ///             // Suppose m_x, m_y and m_z are fields. Mix their hashes into one
    ///             return
    ///                 new MelcherHashCodeMixer()
    ///                     .Mix(m_x)
    ///                     .Mix(m_y)
    ///                     .Mix(m_z)
    ///                     .GetHashCode();
    ///             }
    ///     </code>
    /// </summary>
    public struct MelcherHashCodeMixer
    {
        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Mix the hash code from an object into a running total. Chain a
        ///     call to <see cref="GetHashCode"/> to complete the mixing and
        ///     retrieve the final hash code.
        /// </summary>
        /// <returns>
        ///     This returns a copy of the current mixer object so it can be
        ///     used for another mix via function chaining.
        /// </returns>
        public MelcherHashCodeMixer Mix<AnyType>(in AnyType typeToGetHashCodeFrom)
        {
            return MixExplicit(typeToGetHashCodeFrom.GetHashCode());
        }

        /// <summary>
        ///     Mix a hash code into a running total. Chain a call to <see
        ///     cref="GetHashCode"/> to complete the mixing and retrieve the final
        ///     hash code.
        /// </summary>
        public MelcherHashCodeMixer MixExplicit(int nextHashCode)
        {
            const int Prime = 357239; // Large prime number with distinct and non-boring bits
            unchecked
            {
                m_HashCode += nextHashCode;
                m_HashCode *= Prime;
                m_HashCode ^= (m_HashCode >> 9);
            }
            return this;
        }

        /// <summary>
        ///     Retrieve the hash code of all currently mixed values.
        /// </summary>
        /// <seealso cref="Mix"/>
        public override int GetHashCode()
        {
            return m_HashCode;
        }

        // =====================================================================
        // Internal fields
        // =====================================================================

        /// <summary>
        ///     The accumulating hash code.
        /// </summary>
        private int m_HashCode;

    }   // struct MelcherHashCodeMixer

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-12-03
/// ===========================================================================
