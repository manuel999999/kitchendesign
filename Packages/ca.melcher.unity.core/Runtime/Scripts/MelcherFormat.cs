namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// String formatting functions. These may be used to create consistent
    /// styling in the console log.
    /// </summary>
    public static class MelcherFormat
    {
        // =====================================================================
        // ConsoleTypeName
        // =====================================================================

        /// <summary>
        /// Infer a type name from a variable's type. This throws an exception
        /// when <paramref name="fromVariableType"/> is <see langword="null"/>.
        /// </summary>
        public static string ConsoleTypeName(object fromVariableType, bool useFullName = false)
        {
            return ConsoleTypeName(fromVariableType.GetType());
        }

        /// <summary>
        /// Type name from generic type argument.
        /// </summary>
        public static string ConsoleTypeName<Type>(bool useFullName = false)
        {
            return ConsoleTypeName(typeof(Type), useFullName);
        }

        /// <summary>
        /// Type name from type object. This throws an exception when <paramref
        /// name="type"/> is <see langword="null"/>.
        /// </summary>
        public static string ConsoleTypeName(System.Type type, bool useFullName = false)
        {
            return ConsoleTypeName(MelcherCommon.PrettyTypeName(type, useFullName));
        }

        /// <summary>
        /// Syntax highlight a type name. The name can include a namespace
        /// qualifier. Any dots contained within the type name will be excluded
        /// from colorizing.
        /// </summary>
        public static string ConsoleTypeName(string typeName)
        {
            if (typeName == null)
            {
                return ConsoleKeyword("null");
            }

            // Don't colorize the generic argument angle brackets. This reads
            // better
            // Use () to avoid replacing characters in the rich text tags
            typeName = typeName.Replace("<", "(color)<(/color)");
            typeName = typeName.Replace(">", "(color)>(/color)");
            typeName = typeName.Replace("(", "<");
            typeName = typeName.Replace(")", ">");

            // Don't colorize the namespace dots. This reads better
            typeName = typeName.Replace(".", "<color>.</color>");
            typeName = typeName.Replace(",", "<color>,</color>");

            ConsoleColor typeNameColor =
                new ConsoleColor(
                    lightThemeColor: "#2B91AF", // "Cyan Cornflower Blue", type name in VS light theme
                    darkThemeColor:  "#4DC7AE"  // Teal-green, type name in VS dark theme
                );

            return $"<color={typeNameColor}><b>{typeName}</b></color>";
        }

        // =====================================================================
        // ConsoleNumericValue
        // =====================================================================

        /// <summary>
        /// Return a string colorized to highlight the value is numeric.
        /// </summary>
        public static string ConsoleNumericValue(object numericValue)
        {
            ConsoleColor numericValueColor =
                new ConsoleColor(
                    lightThemeColor: "#098658", // "Spanish Viridian", a mid green for numeric literals in VS light theme
                    darkThemeColor:  "#A7CEA8"  // "Cambridge Blue", but its literally a light green. Used for numeric literals in VS
                );

            return $"<color={numericValueColor}>{numericValue}</color>";
        }

        // =====================================================================
        // ConsoleSubdued
        // =====================================================================

        public static readonly ConsoleColor SubduedColor =
            new ConsoleColor(
                lightThemeColor: "#C0C0C0", // Light grey
                darkThemeColor:  "#808080"  // Mid-grey
            );

        /// <summary>
        /// Return a string colorized with a mid-gray color. This helps
        /// de-emphasizes the text.
        /// </summary>
        public static string ConsoleSubdued(object message)
        {
            return $"<color={SubduedColor}>{message}</color>";
        }

        // =====================================================================
        // ConsoleFieldName
        // =====================================================================

        /// <summary>
        ///     Syntax highlight a field name. The field can be prefixed with
        ///     the type and/or namespace. Anything before the last dot is
        ///     formatted using <see cref="ConsoleTypeName"/> so it stands out
        ///     from the actual field name.
        /// </summary>
        /// <param name="fieldName">
        ///     String to format. E.g., "Melcher.MelcherLog.logLevel"
        /// </param>
        public static string ConsoleFieldName(string fieldName)
        {
            ConsoleColor fieldNameColor =
                new ConsoleColor(
                    lightThemeColor: "#1F3794", // "Dark Cornflower Blue", variable names in VS light theme
                    darkThemeColor:  "#D9DBDC"  // "Gainsboro" (off-white color) used for field names in VS
                );

            string typePortion = null;

            int dot = fieldName.LastIndexOf(".");
            if (dot >= 0)
            {
                typePortion = ConsoleTypeName(fieldName.Substring(0, dot + 1));
                fieldName = fieldName.Substring(dot + 1);
            }

            return $"{typePortion}<color={fieldNameColor}><b>{fieldName}</b></color>";
        }


        // =====================================================================
        // ConsoleMethodName
        // =====================================================================

        /// <summary>
        /// Syntax highlight a method name. The method can be prefixed with the
        /// type and/or namespace. Anything before the last dot is formatted
        /// using <see cref="ConsoleTypeName"/> so it stands apart from the
        /// actual method name.
        /// </summary>
        /// <param name="fieldName">
        /// String to format. E.g., "Melcher.MelcherFormat.ConsoleMethodName"
        /// </param>
        public static string ConsoleMethodName(string methodName)
        {
            ConsoleColor methodNameColor =
                new ConsoleColor(
                    lightThemeColor: "#74531F", // Dark-yellow, method names in VS light theme
                    darkThemeColor:  "#DCDCAA"  // Pale-yellow, method names in VS dark theme
                );

            string typePortion = null;

            int dot = methodName.LastIndexOf(".");
            if (dot >= 0)
            {
                typePortion = ConsoleTypeName(methodName.Substring(0, dot + 1));
                methodName = methodName.Substring(dot + 1);
            }

            return $"{typePortion}<color={methodNameColor}><b>{methodName}</b></color>";
        }

        // =====================================================================
        // ConsoleSymbolName
        // =====================================================================

        /// <summary>
        /// Syntax highlight an enum name. Enums are backed by symbolic
        /// constants but may still contain a value that isn't specifically
        /// defined. Enums formatted with this will always be colorized, even if
        /// they don't match a defined value exactly.
        /// </summary>
        public static string ConsoleSymbolName<EnumType>(EnumType enumValue)
            where EnumType : System.Enum
        {
            return ConsoleSymbolName(enumValue.ToString());
        }

        /// <summary>
        /// Color a symbolic name. E.g., an enum entry, a constant, etc.
        /// </summary>
        public static string ConsoleSymbolName(string text)
        {
            ConsoleColor symbolNameColor =
                new ConsoleColor(
                    lightThemeColor: "#8F08C4", // Purple, keywords in VS light theme.
                    darkThemeColor:  "#A6A7EC"  // Purple used for macros in VS
                );

            return $"<color={symbolNameColor}>{text}</color>";
        }

        // =====================================================================
        // ConsoleKeyword
        // =====================================================================

        /// <summary>
        /// Syntax highlight a keyword. E.g., "null", "static", etc.
        /// </summary>
        public static string ConsoleKeyword(string keyword)
        {
            ConsoleColor keywordColor =
                new ConsoleColor(
                    lightThemeColor: "#0000FF", // Solid blue, keywords in VS light theme
                    darkThemeColor: "#569CD6"   // Mid-blue used for keywords in VS dark theme
                );

            return $"<color={keywordColor}>{keyword}</color>";
        }

        // =====================================================================
        // ConsoleString
        // =====================================================================

        /// <summary>
        /// Syntax highlight a string. If <paramref name="text"/> is <see
        /// langword="null"/>, the returned string will be the output of
        /// <c>ConsoleKeyword("null")</c>. Otherwise, the text is wrapped in
        /// double quotes and returned with a color.
        /// </summary>
        public static string ConsoleString(string text)
        {
            if (text == null)
            {
                return ConsoleKeyword("null");
            }

            ConsoleColor embeddedQuoteTextColor =
                new ConsoleColor(
                    lightThemeColor: "#B776FB", // "Lavender", escaped string characters in VS light theme
                    darkThemeColor:  "#D7BA7D"  // Darkish-yellow, escaped characters in VS dark theme
                );

            // Color embedded quotes slightly different.
            text = text.Replace("\"", $"<color={embeddedQuoteTextColor}>\"</color>");

            ConsoleColor quotedTextColor =
                new ConsoleColor(
                    lightThemeColor: "#A31515", // "Blood (Animal)", strings in VS light theme
                    darkThemeColor:  "#CE9178"  // Pale-brown, strings in VS dark theme
                );

            return $"<color={quotedTextColor}>\"{text}\"</color>";
        }

        // ====================================================================
        // ConsoleColorPreview
        // ====================================================================

        /// <summary>
        ///     Create a richtext-colourized string of Unicode block characters
        ///     of the requested length. This is useful for displaying a colour
        ///     in the console because the block characters provide a dense
        ///     region of the screen to display the colour.
        /// </summary>
        /// <param name="color">
        ///     The colour to capture in the returned richtext.
        /// </param>
        /// <param name="length">
        ///     The number of colourized glyphs to return.
        /// </param>
        /// <returns></returns>
        public static string ConsoleColorPreview(Color color, int length = 1)
        {
            string colorHexCode =
                UnityEngine.ColorUtility.ToHtmlStringRGB(color);

            const char BlockCharacter = '\u2588';

            string previewText = new string(BlockCharacter, length);

            return $"<color=#{colorHexCode}>{previewText}</color>";
        }

        // ====================================================================
        // ConsoleProTempFilter
        // ====================================================================

        public static string ConsoleProTempFilterTag(string filterName)
        {
            return $"@{filterName}";
        }

        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        ///     Holds a pair of colours: one that looks best with the Unity
        ///     Editor's light theme and the other looks best with the dark
        ///     theme.
        /// </summary>
        public readonly struct ConsoleColor
        {
            // =====================================================================
            // Constructors
            // =====================================================================

            /// <summary>
            ///     Create a <see cref="ConsoleColor"/> from its light and dark
            ///     theme colours.
            /// </summary>
            /// <param name="lightThemeColor">
            ///     The rich-text color format to use when the editor is using
            ///     the light theme. This is also the default colour returned
            ///     when inside a build. E.g., "#FFFFFF" or "white".
            /// </param>
            /// <param name="darkThemeColor">
            ///     The rich-text color format to use when the editor is using
            ///     the dark theme.
            /// </param>
            public ConsoleColor(string lightThemeColor, string darkThemeColor)
            {
                this.lightThemeColor = lightThemeColor;
                this.darkThemeColor = darkThemeColor;
            }

            // =====================================================================
            // Properties
            // =====================================================================

            /// <summary>
            ///     Get the name or hex code of the color that best matches the
            ///     current editor skin. In a build, this always returns
            ///     <see cref="lightThemeColor"/>.
            /// </summary>
            public string currentThemeColor
            {
                get
                {
#if UNITY_EDITOR
                    if (UnityEditor.EditorGUIUtility.isProSkin)
                    {
                        return darkThemeColor;
                    }
#endif  // if UNITY_EDITOR

                    return lightThemeColor;
                }
            }

            // =====================================================================
            // Methods
            // =====================================================================

            /// <summary>
            ///     Alias for <see cref="currentThemeColor"/>.
            /// </summary>
            public override string ToString()
            {
                return currentThemeColor;
            }

            // =====================================================================
            // Public fields
            // =====================================================================

            public readonly string lightThemeColor;
            public readonly string darkThemeColor;

        }   // struct ConsoleColor

    }   // class MelcherFormat
}

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-12-19
/// ===========================================================================