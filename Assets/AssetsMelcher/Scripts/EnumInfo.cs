namespace Melcher
{
    using System;
    using System.Collections.ObjectModel;

    // TODO: Replace all references of this with Melcher.EnumInfo

    /// <summary>
    /// Provides a Generics-friendly interface for introspecting an
    /// <see langword="enum"/> type.
    /// </summary>
    public static class EnumInfo<EnumType>
        where EnumType : struct, Enum
    {
        /// <summary>
        /// Return the number of names defined in an enum.
        /// </summary>
        /// <remarks>
        /// This calls <see cref="Enum.GetNames(Type)"/> the first time, then
        /// returns a cached result on subsequent calls.
        /// </remarks>
        public static int count => names.Count;

        /// <summary>
        /// Return the names defined in an enum. The return value is a
        /// <see cref="ReadOnlyCollection{T}"/> to prevent the caller from
        /// modifying the values.
        /// </summary>
        /// <remarks>
        /// This calls <see cref="Enum.GetNames(Type)"/> the first time, then
        /// returns a cached result on subsequent calls.
        /// </remarks>
        public static ReadOnlyCollection<string> names
        {
            get
            {
                if (s_Names == null)
                {
                    // Memorize the names the first time they are requested
                    string[] names = Enum.GetNames(typeof(EnumType));
                    s_Names = Array.AsReadOnly(names);
                }

                return s_Names;
            }
        }

        /// <summary>
        /// Return the values defined in an enum. The return value is a
        /// <see cref="ReadOnlyCollection{T}"/> to prevent the caller from
        /// modifying the values.
        /// </summary>
        /// <remarks>
        /// This calls <see cref="Enum.GetValues(Type)"/> the first time, then
        /// returns a cached result on subsequent calls.
        /// </remarks>
        public static ReadOnlyCollection<EnumType> values
        {
            get
            {
                if (s_Values == null)
                {
                    // Memorize the values the first time they are requested
                    Array array = Enum.GetValues(typeof(EnumType));
                    s_Values = Array.AsReadOnly((EnumType[])array);
                }

                return s_Values;
            }
        }

        /// <summary>
        /// Get the underlying type of the enum. E.g., typeof(int),
        /// typeof(byte), etc.
        /// </summary>
        public static Type underlyingType
            => Enum.GetUnderlyingType(typeof(EnumType));

        public static bool TryParse(string value, out EnumType result, EnumType defaultValue = default)
        {
            return TryParse(value, ignoreCase: true, out result, defaultValue);
        }

        public static bool TryParse(string value, bool ignoreCase, out EnumType result, EnumType defaultValue = default)
        {
            bool didParse = Enum.TryParse(value, ignoreCase, out result);

            if (!didParse)
            {
                result = defaultValue;
            }

            return didParse;
        }

        /// <summary>
        /// Enum names.
        /// </summary>
        private static ReadOnlyCollection<string> s_Names = default;

        /// <summary>
        /// Enum values.
        /// </summary>
        private static ReadOnlyCollection<EnumType> s_Values = default;

    }   // class EnumInfo


}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-12-01
/// ===========================================================================
