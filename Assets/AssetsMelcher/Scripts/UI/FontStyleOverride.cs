namespace Melcher
{
    using UnityEngine;

    //using TMPro.EditorUtilities;
    using UnityEditor;
    using TMPro;

    /// <summary>
    ///     This is intended to be used by <see cref="ButtonStyle"/> to allow
    ///     designers to tweak/override the apperance of fonts according to
    ///     a button's current style.
    ///     <br /><br />
    ///
    ///     This override object allows the user to say whether a style flag
    ///     should forced on, forced off, or kept however the text is normally
    ///     displayed.
    /// </summary>
    [System.Serializable]
    public struct FontStyleOverride
    {
        public FlagOverride bold;
        public FlagOverride italic;
        public FlagOverride underline;
        public FlagOverride strikethrough;
        public FlagOverride lowercase;
        public FlagOverride uppercase;
        public FlagOverride smallcaps;

        public FontStyles Apply(FontStyles style)
        {
            style = SetFlag(style, bold, FontStyles.Bold);
            style = SetFlag(style, italic, FontStyles.Italic);
            style = SetFlag(style, underline, FontStyles.Underline);
            style = SetFlag(style, strikethrough, FontStyles.Strikethrough);
            style = SetFlag(style, lowercase, FontStyles.LowerCase);
            style = SetFlag(style, uppercase, FontStyles.UpperCase);
            style = SetFlag(style, smallcaps, FontStyles.SmallCaps);

            return style;

            static FontStyles SetFlag(FontStyles current, FlagOverride overrideFlag, FontStyles bitToChange)
            {
                if (overrideFlag == FlagOverride.Enabled)
                {
                    current |= bitToChange;
                }
                else if (overrideFlag == FlagOverride.Disabled)
                {
                    current &= ~bitToChange;
                }

                return current;
            }
        }

        public enum FlagOverride : byte
        {
            Unchanged,
            Enabled,
            Disabled,
        }   // enum OverrideMode

    }   // class FontStyleOverride

    /*
    //[CustomPropertyDrawer(typeof(FontStyleOverride))]
    public class FontStyleOverrideDrawer : PropertyDrawer
    {

        static readonly GUIContent k_BoldLabel = new GUIContent("B", "Bold");
        static readonly GUIContent k_ItalicLabel = new GUIContent("I", "Italic");
        static readonly GUIContent k_UnderlineLabel = new GUIContent("U", "Underline");
        static readonly GUIContent k_StrikethroughLabel = new GUIContent("S", "Strikethrough");
        static readonly GUIContent k_LowercaseLabel = new GUIContent("ab", "Lowercase");
        static readonly GUIContent k_UppercaseLabel = new GUIContent("AB", "Uppercase");
        static readonly GUIContent k_SmallcapsLabel = new GUIContent("SC", "Smallcaps");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect row = position;
            row.height = EditorGUIUtility.singleLineHeight + 2.0f;

            EditorGUI.BeginProperty(row, k_FontStyleLabel, m_FontStyleProp);

            EditorGUI.PrefixLabel(row, k_FontStyleLabel);

            row.x += EditorGUIUtility.labelWidth;
            row.width -= EditorGUIUtility.labelWidth;

            row.width = Mathf.Max(25f, row.width / 7f);

            DrawFlag(row, property, nameof(FontStyleOverride.bold));
            row.xMin = position.xMin;
            row.xMax = position.xMax;
            DrawFlag(row, property, nameof(FontStyleOverride.italic));
            DrawFlag(row, property, nameof(FontStyleOverride.underline));
            DrawFlag(row, property, nameof(FontStyleOverride.strikethrough));
            DrawFlag(row, property, nameof(FontStyleOverride.lowercase));
            DrawFlag(row, property, nameof(FontStyleOverride.uppercase));
            DrawFlag(row, property, nameof(FontStyleOverride.smallcaps));

            static FontStyleOverride.FlagOverride DrawFlag(Rect row, SerializedProperty property, string propertyName)
            {
                using SerializedProperty relativeProperty =
                    property.FindPropertyRelative(propertyName);

                FontStyleOverride.FlagOverride styleFlag =
                    (FontStyleOverride.FlagOverride)relativeProperty.intValue;

                if (GUI.Toggle(row, styleFlag == FontStyleOverride.FlagOverride.Disabled, "Off", "ButtonLeft"))
                {
                    styleFlag = FontStyleOverride.FlagOverride.Disabled;
                }
                if (GUI.Toggle(row, styleFlag == FontStyleOverride.FlagOverride.Unchanged, "Unchanged", "ButtonMid"))
                {
                    styleFlag = FontStyleOverride.FlagOverride.Unchanged;
                }
                if (GUI.Toggle(row, styleFlag == FontStyleOverride.FlagOverride.Enabled, "On", "ButtonRight"))
                {
                    styleFlag = FontStyleOverride.FlagOverride.Enabled;
                }
            }


            v1 = TMP_EditorUtility.EditorToggle(row, (styleValue & 1) == 1, k_BoldLabel, TMP_UIStyleManager.alignmentButtonLeft) ? 1 : 0; // Bold
            row.x += row.width;
            v2 = TMP_EditorUtility.EditorToggle(row, (styleValue & 2) == 2, k_ItalicLabel, TMP_UIStyleManager.alignmentButtonMid) ? 2 : 0; // Italics
            row.x += row.width;
            v3 = TMP_EditorUtility.EditorToggle(row, (styleValue & 4) == 4, k_UnderlineLabel, TMP_UIStyleManager.alignmentButtonMid) ? 4 : 0; // Underline
            row.x += row.width;
            v7 = TMP_EditorUtility.EditorToggle(row, (styleValue & 64) == 64, k_StrikethroughLabel, TMP_UIStyleManager.alignmentButtonRight) ? 64 : 0; // Strikethrough
            row.x += row.width;

            int selected = 0;

            EditorGUI.BeginChangeCheck();
            v4 = TMP_EditorUtility.EditorToggle(row, (styleValue & 8) == 8, k_LowercaseLabel, TMP_UIStyleManager.alignmentButtonLeft) ? 8 : 0; // Lowercase
            if (EditorGUI.EndChangeCheck() && v4 > 0)
            {
                selected = v4;
            }
            row.x += row.width;
            EditorGUI.BeginChangeCheck();
            v5 = TMP_EditorUtility.EditorToggle(row, (styleValue & 16) == 16, k_UppercaseLabel, TMP_UIStyleManager.alignmentButtonMid) ? 16 : 0; // Uppercase
            if (EditorGUI.EndChangeCheck() && v5 > 0)
            {
                selected = v5;
            }
            row.x += row.width;
            EditorGUI.BeginChangeCheck();
            v6 = TMP_EditorUtility.EditorToggle(row, (styleValue & 32) == 32, k_SmallcapsLabel, TMP_UIStyleManager.alignmentButtonRight) ? 32 : 0; // Smallcaps
            if (EditorGUI.EndChangeCheck() && v6 > 0)
            {
                selected = v6;
            }

            if (selected > 0)
            {
                v4 = selected == 8 ? 8 : 0;
                v5 = selected == 16 ? 16 : 0;
                v6 = selected == 32 ? 32 : 0;
            }

            EditorGUI.EndProperty();

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + 2.0f;

            if (!EditorGUIUtility.wideMode)
            {
                // Drawer will span two lines when not in wide-mode.
                height *= 2.0f;
            }

            return height;
        }
    }
    */

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-06-04
/// ===========================================================================
