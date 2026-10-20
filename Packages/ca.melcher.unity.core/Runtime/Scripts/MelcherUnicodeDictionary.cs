namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEngine;

#if MELCHER_CORE_USE_TEXTCORE
    using FontAsset = UnityEngine.TextCore.Text.FontAsset;
#else
    using FontAsset = TMPro.TMP_FontAsset;
#endif

#if UNITY_EDITOR
    using UnityEditor;
#endif  // if UNITY_EDITOR

    using TextAsset = UnityEngine.TextAsset;

    /// <summary>
    /// A <see cref="MelcherUnicodeDictionary"/> is a scriptable object that
    /// maps names to a unicode encoding (e.g. "apple" => "\uf4d8"). The values
    /// are stored in a nested text asset which can be used by TextMesh Pro's
    /// font creator when generating an atlas. This is helpful for symbol fonts
    /// like FontAwesome.
    /// </summary>
    [CreateAssetMenu(fileName ="UnicodeDictionary", menuName ="Melcher Studios/Unicode Dictionary")]
    public class MelcherUnicodeDictionary : MelcherScriptableObject
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public ReadOnlyCollection<UnicodeEntry> entries
        {
            get
            {
                if (m_ReadonlyEntries == null)
                {
                    m_ReadonlyEntries = m_Entries.AsReadOnly();
                }

                return m_ReadonlyEntries;
            }
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Get the path to the font that can be used in TextMesh Pro's
        ///     <c>font</c> rich text tags.
        /// </summary>
        public string GetRichTextTagFontPath()
        {
            string path = null;

            if (m_Font != null)
            {
                // The path must be relative to a folder stored in the
                // TextMesh Pro settings

                string fontFolder = TMPro.TMP_Settings.defaultFontAssetPath;

                if (string.IsNullOrWhiteSpace(fontFolder))
                {
                    fontFolder = "Resources/";
                }

                int resourceFolderIndex = m_Font.dataRelativePath.IndexOf(fontFolder);

                if (resourceFolderIndex >= 0)
                {
                    int startIndex = resourceFolderIndex + fontFolder.Length;
                    path =
                        m_Font.dataRelativePath.Substring(
                            startIndex,
                            m_Font.dataRelativePath.Length - startIndex - ".asset".Length
                        );
                }
            }

            return path;
        }

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        /// <summary>
        ///     Get the path to the font that can be used in <see
        ///     cref="Resources.Load(string)"/> calls.
        /// </summary>
        public string GetSourceFontPath()
        {
            MelcherLog.Assert(
                m_Font != null,
                $"'{MelcherFormat.ConsoleFieldName(nameof(m_Font))}' must not be null; the actual field object, not its path.",
                context: this
            );

            if (string.IsNullOrEmpty(m_Font.dataRelativePath))
            {
                return null;
            }

            // m_Font points at the TextMesh Pro/TextCore font object. We want
            // to point at the original source font (TTF, OTF, etc).

            string projectRelativeFontAssetPath =
                "Assets/" + m_Font.dataRelativePath;

            FontAsset fontAsset =
                AssetDatabase.LoadAssetAtPath<FontAsset>(projectRelativeFontAssetPath);

            if (fontAsset == null)
            {
                MelcherLog.LogError("Failed to load font asset: " + projectRelativeFontAssetPath, this);

                return null;
            }

            string projectRelativeSourceFontPath;

            if (fontAsset.sourceFontFile != null)
            {
                projectRelativeSourceFontPath =
                    AssetDatabase.GetAssetPath(fontAsset.sourceFontFile);
            }
            else
            {
                // fontAsset.sourceFontFile is null so the font must be looked
                // up by the GUID stored in the font's creation settings.

                string sourceFontGuid;

#if MELCHER_CORE_USE_TEXTCORE

                sourceFontGuid =
                    fontAsset.fontAssetCreationEditorSettings.sourceFontFileGUID;
#else
                sourceFontGuid = fontAsset.creationSettings.sourceFontFileGUID;
#endif  // endif TEXTCORE_1_0_OR_NEWER else

                projectRelativeSourceFontPath =
                    AssetDatabase.GUIDToAssetPath(sourceFontGuid);
            }

            return projectRelativeSourceFontPath;
        }

        public void Rebuild()
        {
            hideFlags = HideFlags.None;
            // Remove the old text asset
            string assetPath = AssetDatabase.GetAssetPath(this);
            const int MaxIterations = 1000; // To prevent infinite loops
            for (int i = 0; i < MaxIterations; ++i)
            {
                TextAsset oldText = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                if (oldText == null)
                {
                    break;
                }
                AssetDatabase.RemoveObjectFromAsset(oldText);
            }

            // Write all the unicode characters to a buffer
            StringBuilder textContent = new StringBuilder();
            foreach (var entry in m_Entries)
            {
                textContent.Append(Regex.Unescape(entry.unicode));
            }

            string output = textContent.ToString();

            const char Underline = '\u005f';
            if (output.IndexOf(Underline) < 0)
            {
                output += Underline;
            }

            const char Ellipsis = '\u2026';
            if (output.IndexOf(Ellipsis) < 0)
            {
                output += Ellipsis;
            }

            // Create a new text asset from the buffer
            TextAsset newText = new TextAsset(output);
            newText.name = name + "_TextAsset";
            //newText.hideFlags = HideFlags.DontSaveInBuild;
            AssetDatabase.AddObjectToAsset(newText, this);

            // TODO: Automatically rebuild the font asset
            // string fontPath = GetFontResourcePath();
            // TMP_FontAsset font = Resources.Load<TMP_FontAsset>(fontPath);
            // TMPro.EditorUtilities.TMP_FontAssetEditor.

            m_LastBuildTime = DateTime.Now.ToString();

            AssetDatabase.SaveAssets();
        }
#endif  // if UNITY_EDITOR

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField]
        protected List<UnicodeEntry> m_Entries = new List<UnicodeEntry>();

        [SerializeField]
        [Tooltip("The font for the Unicode dictionary entries. This is " +
            "serialized as a weak reference (MelcherPathField) to prevent " +
            "automatically loading the font when the dictionary is accessed.")]
        protected MelcherPathField<FontAsset> m_Font = new MelcherPathField<FontAsset>();

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [SerializeField, MelcherReadOnlyInInspector]
        protected string m_LastBuildTime = DateTime.Now.ToString();

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected ReadOnlyCollection<UnicodeEntry> m_ReadonlyEntries;

        // ====================================================================
        // Nested classes
        // ====================================================================

        [Serializable]
        public struct UnicodeEntry
        {
            [Tooltip("The (un-escaped) localization phrase that will " +
                "translate to a unicode glyph.")]
            public string name;

            [Tooltip("The unicode text to substitute when the phrase is " +
                "translated.")]
            public string unicode;
        };  // struct UnicodeEntry

    }   // class MelcherUnicodeDictionary

}   // namespace Melcher
