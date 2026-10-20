namespace Melcher
{
    using System;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// <br>
    /// Custom version of the base Header Attribute.
    /// </br>
    /// <para>
    /// If no header is entered, the default value is the name of current script. Font size
    /// value can be passed, as well as color as a string. If neither is passed, default
    /// values come from EditorStyle.boldLabel style. For passing a color, but no header,
    /// remember to prefix with 'color:'
    /// </para>
    /// <para>
    /// Word wrap is enabled, as well as rich text. Rich text will override the entered
    /// size and color, which are mainly there for convenience.
    /// </para>
    /// <para>
    /// Accepted html colors: red, cyan, blue, darkblue, lightblue, purple, yellow, lime,
    /// fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive,
    /// navy, teal, aqua, magenta"
    /// </para>
    /// Strings that begin with '#' will be parsed as hexadecimal in the following way:
    /// <list type = "bullet">
    /// <item><description>#RGB (becomes RRGGBB)</description></item>
    /// <item><description>#RRGGBB</description></item>
    /// <item><description>#RGBA (becomes RRGGBBAA)</description></item>
    /// <item><description>#RRGGBBAA</description></item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class MelcherHeaderAttribute : PropertyAttribute
    {
        public readonly string header;
        public readonly GUIStyle style;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="size"></param>
        /// <param name="color">red, cyan, blue, darkblue, lightblue, purple, yellow, lime,
        /// fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive,
        /// navy, teal, aqua, magenta</param>
        /// <param name="wordWrap"></param>
        /// <param name="callerFilePath"></param>
        public MelcherHeaderAttribute(string header = null,
                int size = 0,
                string color = null,
                bool wordWrap = true,
                [CallerFilePath] string callerFilePath = null)
        {
#if UNITY_EDITOR
            style = new GUIStyle(EditorStyles.boldLabel);
            style.richText = true;
            style.wordWrap = wordWrap;

            if (size != 0)
            {
                style.fontSize = size;
            }
            else
            {
                style.fontSize = EditorStyles.boldLabel.fontSize;
            }

            if (!string.IsNullOrWhiteSpace(color))
            {
                Color newColor = new Color();
                if (ColorUtility.TryParseHtmlString(color, out newColor))
                {
                    SetColors(newColor);
                }
                else
                {
                    SetColors(EditorStyles.boldLabel.normal.textColor);
                    LogColorWarning(color);
                }
            }
            else
            {
                style.font = EditorStyles.boldLabel.font;
                SetColors(EditorStyles.boldLabel.normal.textColor);
            }

            if (string.IsNullOrWhiteSpace(header))
            {
                header = System.IO.Path.GetFileNameWithoutExtension(callerFilePath);
                string methodName = UnityEditor.ObjectNames.NicifyVariableName(header);
                this.header = methodName;

            }
            else
            {
                this.header = header;
            }

            void SetColors(Color colorValue)
            {
                style.normal.textColor = colorValue;
                style.focused.textColor = colorValue;
                style.active.textColor = colorValue;
                style.hover.textColor = colorValue;
            }
#endif
        }

        private static void LogColorWarning(string color)
        {
            MelcherLog.LogWarning($"Color value {color} isn't a proper html color\n" +
            "Please use red, cyan, blue, darkblue, lightblue, purple, yellow, lime,\n" +
            "fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive,\n" +
            "navy, teal, aqua, magenta \n\n" +
            "Strings that begin with '#' will be parsed as hexadecimal in the following way:\n" +
            "#RGB (becomes RRGGBB)\n" +
            "#RRGGBB\n" +
            "#RGBA (becomes RRGGBBAA)\n" +
            "#RRGGBBAA\n");
        }
    }   // class MelcherHeaderAttribute

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Justin Bender
/// Initial creation date: 2022-08-16
/// ===========================================================================
