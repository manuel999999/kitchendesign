namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// Property drawer for a UnicodeEntry. This just renders both
    /// fields on one line.
    /// </summary>
    [CustomPropertyDrawer(typeof(MelcherUnicodeDictionary.UnicodeEntry))]
    public class MelcherUnicodeDictionaryEntryDrawer : PropertyDrawer
    {
        /// <summary>
        ///     Override the default drawer height to fit larger glyph icons.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return MelcherUnicodeDictionaryEditor.IconSize + 4;
        }

        /// <summary>
        ///     Draw the row of fields for the Unicode Dictionary entry.
        ///     <br /><br />
        ///
        ///     The row has a button that displays the unicode glyph. Clicking
        ///     the button copies the localization phrase used to translate to
        ///     the glyph.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                const float NameRatio = 0.6f;

                float glyphKeySeparatorWidth = 2.0f;
                float keyValueSeparatorWidth = EditorStyles.label.CalcSize(MelcherUnicodeDictionaryEditor.entryKeyValueSeparator).x;

                const float GlyphWidth = 30f;
                float flexibleTextRegionWidth = position.width - (GlyphWidth + glyphKeySeparatorWidth + keyValueSeparatorWidth);

                // Use the current font style of the selected unicode dictionary
                GUIStyle fontStyle =
                    MelcherUnicodeDictionaryEditor.currentGlyphStyle;

                using SerializedProperty keyProperty =
                    property.FindPropertyRelative(nameof(MelcherUnicodeDictionary.UnicodeEntry.name));

                using SerializedProperty valueProperty =
                    property.FindPropertyRelative(nameof(MelcherUnicodeDictionary.UnicodeEntry.unicode));

                {
                    // Draw the unicode glyph

                    Rect glyphRect = position;
                    glyphRect.width = GlyphWidth;

                    string text = valueProperty.stringValue;
                    try
                    {
                        text = System.Text.RegularExpressions.Regex.Unescape(text);
                    }
                    catch (System.ArgumentException)
                    {
                        // Silently ignore errors caused by incomplete escaped
                        // text
                        text = "";
                    }

                    GUIContent glyphContent =
                        new GUIContent(
                            text,
                            tooltip: "Copy the escaped localization phrase to the clipboard."
                        );

                    if (GUI.Button(glyphRect, glyphContent, fontStyle))
                    {
                        // Copy the localization alias to the clipboard

                        GUIUtility.systemCopyBuffer =
                            MelcherLocalizer.EscapeKey(keyProperty.stringValue);

                        EditorWindow.focusedWindow.ShowNotification(
                            new GUIContent($"Copied \"{GUIUtility.systemCopyBuffer}\" to clipboard")
                        );
                    }

                    position.xMin += glyphRect.width;
                }

                {
                    // Spacer between button and localization alias

                    position.xMin += glyphKeySeparatorWidth;
                }

                {
                    // Draw the localization alias

                    Rect keyRect = position;
                    keyRect.width = flexibleTextRegionWidth * NameRatio;

                    keyRect.height =
                        GUI.skin.textField.CalcHeight(
                            content: new GUIContent(valueProperty.stringValue),
                            width: keyRect.width
                        );

                    keyRect =
                        CenterRectVertically(
                            availableArea: position,
                            rectToCenter: keyRect
                        );

                    DrawPropertyFieldWithTooltip(
                        keyRect,
                        keyProperty,
                        label: GUIContent.none
                    );

                    position.xMin += keyRect.width;
                }

                {
                    // Draw an arrow

                    Rect postfixRect = position;
                    postfixRect.width = keyValueSeparatorWidth;

                    EditorGUI.LabelField(postfixRect, MelcherUnicodeDictionaryEditor.entryKeyValueSeparator);

                    position.xMin += postfixRect.width;
                }

                {
                    Rect unicodeRect = position;
                    unicodeRect.width = (flexibleTextRegionWidth * (1.0f - NameRatio));

                    unicodeRect.height =
                        GUI.skin.textField.CalcHeight(
                            content: new GUIContent(valueProperty.stringValue),
                            width: unicodeRect.width
                        );

                    unicodeRect =
                        CenterRectVertically(
                            availableArea: position,
                            rectToCenter: unicodeRect
                        );

                    DrawPropertyFieldWithTooltip(
                        unicodeRect,
                        valueProperty,
                        label: GUIContent.none
                    );

                    position.xMin += unicodeRect.width;
                }
            }
        }

        private static Rect CenterRectVertically(Rect availableArea, Rect rectToCenter)
        {
            float availableAreaHeight = availableArea.yMax - availableArea.yMin;

            float verticalCenterOffset = (availableAreaHeight - rectToCenter.height) * 0.5f;

            rectToCenter.position += Vector2.up * verticalCenterOffset;

            return rectToCenter;
        }

        private static void DrawPropertyFieldWithTooltip(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label);

            // Draw a label over the property field that has the tooltip

            GUIContent tooltipContent =
                new GUIContent(GUIContent.none);

            tooltipContent.tooltip = property.tooltip;

            EditorGUI.LabelField(position, tooltipContent);
        }
    }   // class MelcherUnicodeDictionaryEntryDrawer

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-03-13
/// ===========================================================================
