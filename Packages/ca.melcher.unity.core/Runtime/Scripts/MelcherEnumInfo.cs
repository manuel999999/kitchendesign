namespace Melcher
{
    using OD;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    ///     Provides a Generics-friendly interface for introspecting an
    ///     <see langword="enum"/> type.
    /// </summary>
    public static class MelcherEnumInfo<EnumType>
        where EnumType : Enum
    {
        // =====================================================================
        // Static properties
        // =====================================================================

        /// <summary>
        ///     Get the underlying type of the enum. E.g., <c>typeof(int)</c>,
        ///     <c>typeof(byte)</c>, etc.
        /// </summary>
        public static Type underlyingType
            => Enum.GetUnderlyingType(typeof(EnumType));

        /// <summary>
        ///     Return the number of names defined in an enum.
        /// </summary>
        /// <remarks>
        ///     This calls <see cref="Enum.GetNames(Type)"/> the first time,
        ///     then returns a cached result on subsequent calls.
        /// </remarks>
        public static int count => names.Count;

        /// <summary>
        ///     Return the names defined in an enum. The order of names is the
        ///     order they were declared in the enum.
        /// </summary>
        /// <remarks>
        ///     This calls <see cref="Enum.GetNames(Type)"/> the first time,
        ///     then returns a cached result on subsequent calls.
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
        ///     Return the values defined in an enum.
        /// </summary>
        /// <remarks>
        ///     This calls <see cref="Enum.GetValues(Type)"/> the first time,
        ///     then returns a cached result on subsequent calls.
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
        ///     Get a dictionary that maps the names defined by an enum to
        ///     their value.
        /// </summary>
        public static ReadOnlyDictionary<string, EnumType> namesToValues
        {
            get
            {
                if (s_NamesToValues == null)
                {
                    OrderedDictionary<string, EnumType> writableNameToValues =
                        new OrderedDictionary<string, EnumType>();

                    foreach (string enumName in names)
                    {
                        EnumType enumValue = (EnumType)Enum.Parse(typeof(EnumType), enumName);
                        writableNameToValues.Add(enumName, enumValue);
                    }

                    // Cache the dictionary for reuse.
                    s_NamesToValues = new ReadOnlyDictionary<string, EnumType>(writableNameToValues);
                }

                return s_NamesToValues;
            }
        }

        public static string[] GetNamesFromMask(EnumType mask)
        {
            List<string> names = new List<string>();

            foreach (KeyValuePair<string, EnumType> kv in namesToValues)
            {
                if (mask.HasFlag(kv.Value))
                {
                    names.Add(kv.Key);
                }
            }

            return names.ToArray();
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        private static ReadOnlyDictionary<string, EnumType> s_NamesToValues = default;

        /// <summary>
        /// Enum names.
        /// </summary>
        private static ReadOnlyCollection<string> s_Names = default;

        /// <summary>
        /// Enum values.
        /// </summary>
        private static ReadOnlyCollection<EnumType> s_Values = default;

    }   // class MelcherEnumInfo

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-12-01
/// ===========================================================================
