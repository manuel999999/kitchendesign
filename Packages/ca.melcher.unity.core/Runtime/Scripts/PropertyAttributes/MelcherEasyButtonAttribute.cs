namespace Melcher
{
    using System;
    using UnityEngine;
    using UnityEditor;

    public enum Align { Left, Center, Right };

    /// <summary>
    /// <br>
    /// Custom quick editor button attribute.
    /// </br>
    /// <para>
    /// Allows you to set any method that is in a class that inherits from MelcherMonoBehaviour
    /// or MelcherScriptableObject to have an inspector button to call said method.
    /// </para>
    /// <para>
    /// Passing no parameters labels the button by the class name, and sets a default
    /// size, alignment, and basic system colors. Label, height, width, text color, and
    /// button alignment are all optional parameters that can be passed.
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
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class MelcherEasyButtonAttribute : Attribute
    {
        public string label;
        public readonly int height;
        public readonly int width;
        public readonly int rowId;
        public readonly string color;
        public readonly Align alignment;
        public readonly GUIStyle style;

        /// <param name="label">Button Label. Defaults to Method name.</param>
        /// <param name="height">Button Height.</param>
        /// <param name="width">Button Width. 0 or no value = full width, expands with inspector.
        /// Negative value tightens button size to string size of label. Positive amount is
        /// fixed pixel size.</param>
        /// <param name="rowId">If we want buttons to appear on a row together, pass this an id
        /// of 0 or higher. All that match, go on a row together. Will be rendered after non-row
        /// EasyButtons, regardless of their order in their script, but in the order of the 
        /// row IDs. You can also use the rowIDs to simply reorder some of the buttons.</param>
        /// <param name="color">red, cyan, blue, darkblue, lightblue, purple, yellow, lime,
        /// fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive,
        /// navy, teal, aqua, magenta - Also accepts #RRGGBB values</param>
        /// <param name="alignment">Sets justification in inspector view for each button.
        /// To set the default value accross your entire project, set it in the constructor
        /// for this attribute class.</param>
        /// <param name="callerFilePath"></param>
        public MelcherEasyButtonAttribute(
            string label = null,
            int height = 0,
            int width = 0,
            int rowId = -1,
            string color = null,
            Align alignment = Align.Center
            )
        {
#if UNITY_EDITOR
            this.label = label;
            this.rowId = rowId;
            this.alignment = alignment;

            style = new GUIStyle(EditorStyles.miniButton);
            style.alignment = TextAnchor.MiddleCenter;


            if (height != 0)
            {
                style.fixedHeight = height;
                style.fontSize = Mathf.RoundToInt(height * .5f);
                this.height = height;
            }
            else
            {
                style.fixedHeight = 32;
                style.fontSize = EditorStyles.miniButton.fontSize;
            }
            if (width > 0)
            {
                style.fixedWidth = width;
                this.width = width;
            }
            else if (width < 0)
            {
                style.fixedWidth = EditorStyles.miniButton.fixedWidth;
                this.width = Mathf.RoundToInt(EditorStyles.miniButton.fixedWidth);
            }
            else
            {
                if (rowId < 0)
                {
                    if (alignment == Align.Center || alignment == Align.Right)
                    {
                        style.fixedWidth = Screen.width - 60f;
                    }
                    else
                    {
                        style.fixedWidth = EditorStyles.miniButton.fixedWidth;
                    }
                }
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
                    SetColors(EditorStyles.miniButton.normal.textColor);
                    LogColorWarning(color);
                }
            }
            else
            {
                style.font = EditorStyles.miniButton.font;
                SetColors(EditorStyles.miniButton.normal.textColor);
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

        public void SetButtonLabel(string label)
        {
            this.label = label;
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
