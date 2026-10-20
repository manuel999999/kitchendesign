namespace Melcher
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using UnityEngine;

    /// <summary>
    ///     Handle localization and string translation.
    /// </summary>
    /// <verbose>
    ///     Translations operate on "phrases". Phrases are language-invariant
    ///     text that appear between <see cref="KeyPrefix"/> and <see
    ///     cref="KeyPostfix"/>. For example, "$(ABC)" is a phrase that would
    ///     appear in some app text label and, when passed to <see
    ///     cref="Localize(ref string)"/>, it would be replaced with the
    ///     translation corresponding to "ABC" in the specified language code.
    ///     <br /><br />
    ///
    ///     This class provides editor-time translation of unicode characters
    ///     defined in <see cref="MelcherUnicodeDictionary"/> assets in the
    ///     project. This is useful when designing UI with fonts like
    ///     FontAwesome.
    /// </verbose>
    [ExecuteInEditMode]
    [AddComponentMenu("")]  // Hide from the AddComponent menu
    public class MelcherLocalizer : MelcherSubsystem<MelcherLocalizer>
    {
        // =====================================================================
        // Consts
        // =====================================================================

        // Using parenthesis to be distinct from C# interpolated strings
        public const string KeyPrefix = "$(";
        public const string KeyPostfix = ")";

        // =====================================================================
        // Events
        // =====================================================================

        public delegate void LanguageChangedHandler(string languageCode);
        public static event LanguageChangedHandler onLanguageChanged;

        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        ///     Get or set the active language.
        /// </summary>
        public static string languageCode
        {
            get => s_Instance.m_LanguageCode;
            set
            {
                s_Instance.m_LanguageCode = value;
                onLanguageChanged?.Invoke(s_Instance.m_LanguageCode);
            }
        }

        // ====================================================================
        // Static functions
        // ====================================================================

        /// <summary>
        ///     Strip the <c>$()</c> symbols from a key.
        /// </summary>
        public static string UnescapeKey(string key)
        {
            if (key.StartsWith(KeyPrefix) && key.EndsWith(KeyPostfix))
            {
                key = key.Substring(KeyPrefix.Length, key.Length - KeyPrefix.Length - KeyPostfix.Length);
            }
            return key;
        }

        /// <summary>
        ///     Add the <c>$()</c> symbols to a key if it doesn't have them.
        /// </summary>
        public static string EscapeKey(string key)
        {
            if (!key.StartsWith(KeyPrefix) && !key.EndsWith(KeyPostfix))
            {
                key = KeyPrefix + key + KeyPostfix;
            }
            return key;
        }

        /// <summary>
        ///     Given a localization key (e.g., "$(MyKey)"), append some other
        ///     text to it: "$(MyKey)" + "Extra" => "$(MyKeyExtra)"
        /// </summary>
        public static string AppendToKey(string key, string toAppend)
        {
            return key.Insert(key.Length - 1, toAppend);
        }

        /// <summary>
        ///     Translate a phrase using the currently selected language.
        /// </summary>
        public static void Localize(ref string phrase)
        {
            s_Instance.DoLocalize(ref phrase, s_Instance.m_LanguageCode);
        }

        /// <summary>
        ///     Translate a phrase using an explicit language.
        /// </summary>
        public static void Localize(ref string phrase, string languageCode)
        {
            s_Instance.DoLocalize(ref phrase, languageCode);
        }

        /// <summary>
        ///     Low-level access to the dictionary used by a language code.
        ///     <br /><br />
        ///
        ///     The dictionary maps keys (the invariant text that appears
        ///     between <see cref="KeyPrefix"/> and <see cref="KeyPostfix"/>,
        ///     e.g., "$(Key)") to a translation in that language.
        /// </summary>
        /// <returns>
        ///     The dictionary used by <paramref name="languageCode"/>. If the
        ///     language code hasn't been added, the return value is <see
        ///     langword="null"/>.
        /// </returns>
        public static Dictionary<string, string> GetLanguagePhrases(string languageCode)
        {
            s_Instance.m_Languages.TryGetValue(
                languageCode,
                out Dictionary<string, string> languagePhrases
            );

            return languagePhrases;
        }

        /// <summary>
        ///     Low-level assignment of the translation dictionary used by a
        ///     language code.
        /// </summary>
        public static void SetLanguagePhrases(string languageCode, Dictionary<string, string> languagePhrases)
        {
            s_Instance.m_Languages[languageCode] = languagePhrases;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Load all <see cref="MelcherUnicodeDictionary">dictionaries</see>
        ///     and the fonts they reference so future localization requests can
        ///     translate to them.
        /// </summary>
        public void ReloadUnicodeDictionaries()
        {
            // Load all UnicodeDictionary assets in Resources. This ensures
            // the call to Resources.FindObjectsOfTypeAll() returns all of them.
            Resources.LoadAll("", typeof(MelcherUnicodeDictionary));

            m_UnicodeDictionaries.Clear();

            m_UnicodeDictionaries.AddRange(
                Resources.FindObjectsOfTypeAll<MelcherUnicodeDictionary>()
            );

            // Manually load each UnicodeDictionary's font. If they're not
            // preloaded like this, our Language.Localize() calls will fail
            // to render with the unicode glyphs
            foreach (MelcherUnicodeDictionary unicodeDict in m_UnicodeDictionaries)
            {
                string fontResourcePath = unicodeDict.GetRichTextTagFontPath();

                if (!string.IsNullOrEmpty(fontResourcePath))
                {
                    Resources.Load(fontResourcePath);
                }
            }
        }

        // public void ParseStreamingFile(string streamingPath, System.Action onComplete)
        // {
        //     // Try to load the previously downloaded translation first
        //     MelcherKeyValueParser.ParseStreamingFile(
        //         streamingPath,
        //         onComplete: (keyValueBlocks, error) =>
        //         {
        //             if (!string.IsNullOrEmpty(error))
        //             {
        //                 MelcherLog.LogError(
        //                     tag: nameof(Language),
        //                     message: error,
        //                     context: this
        //                 );
        //             }

        //             BuildDictionariesFromKeyValues(keyValueBlocks);

        //             onComplete?.Invoke();

        //             // Force localized text to refresh
        //             EventBridge.onLanguageChanged?.Invoke();
        //         }
        //     );
        // }

        /// <summary>
        ///     Ensure all <see cref="MelcherUnicodeDictionary">dictionaries
        ///     </see> are loaded when the subsystem is created.
        /// </summary>
        private void OnEnable()
        {
            ReloadUnicodeDictionaries();
        }

        private void DoLocalize(ref string phrase, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(phrase))
            {
                return;
            }

            string startingPhrase = phrase;

            Dictionary<string, string> selectedTranslations = null;
            Dictionary<string, string> anyTranslations = null;

            if (m_Languages != null)
            {
                if (languageCode != null)
                {
                    m_Languages.TryGetValue(languageCode, out selectedTranslations);
                }

                m_Languages.TryGetValue("any", out anyTranslations);
            }

            // Translate the text (limit the iterations to prevent an infinite
            // loop)
            MelcherSafeLoop safeLoop = MelcherSafeLoop.WithMaxIterations(maxIterations: 10);
            while (safeLoop.While(true))
            {
                bool didReplace = false;

                // Match the pattern "$(<text>)". We're wrapping the key using
                // parenthesis rather than curly braces to avoid confusion with
                // C#'s interpolated strings.
                string keyPattern =
                    "(" + Regex.Escape(KeyPrefix) + "([^" + KeyPostfix[0] + "]+)" + Regex.Escape(KeyPostfix) + ")";
                MatchCollection matches = Regex.Matches(phrase, keyPattern);
                foreach (Match match in matches)
                {
                    string key = match.Groups[2].Value;    // The inner match (between the prefix and postfix)

                    if ((selectedTranslations != null && selectedTranslations.TryGetValue(key, out string translation)) ||
                        (anyTranslations != null && anyTranslations.TryGetValue(key, out translation)))
                    {
                        phrase = phrase.Replace(match.Groups[1].Value, translation);
                        didReplace = true;
                        continue;
                    }

                    // Also compare key against UnicodeDictionaries
                    foreach (MelcherUnicodeDictionary unicodeDict in m_UnicodeDictionaries)
                    {
                        int unicodeIndex = MelcherCommon.FindIndex(unicodeDict.entries, x => x.name == key);
                        if (unicodeIndex >= 0)
                        {
                            string unicode = Regex.Unescape(unicodeDict.entries[unicodeIndex].unicode);

                            string fontPath = unicodeDict.GetRichTextTagFontPath();

                            if (!string.IsNullOrWhiteSpace(fontPath))
                            {
                                unicode = $"<font=\"{fontPath}\">{unicode}</font>";
                            }

                            phrase = phrase.Replace(match.Groups[1].Value, unicode);

                            didReplace = true;

                            break;
                        }
                    }
                }

                // Translation is finished when nothing is replaced
                if (!didReplace)
                {
                    break;
                }
            }

            if (safeLoop.isRunaway)
            {
                // Log an error when the input phrase appears cyclical

                MelcherLog.LogError(
                    message: $"Possible cyclical translation: {MelcherFormat.ConsoleString(startingPhrase)}\n" +
                        $"{MelcherFormat.ConsoleNumericValue(safeLoop.iteration)} recursions later, the phrase is: {MelcherFormat.ConsoleString(phrase)}",
                    context: this
                );
            }
        }

        // private void BuildDictionariesFromKeyValues(List<MelcherKeyValueBlock> blocks)
        // {
        //     // This dictionary maps language codes to that languages translations
        //     m_Languages = new Dictionary<string, Dictionary<string, string>>();

        //     foreach (var block in blocks)
        //     {
        //         var phraseKey = block.name;

        //         foreach (var keyValue in block.keyValues)
        //         {
        //             Dictionary<string, string> translations = null;

        //             var languageCode = keyValue.key;
        //             if (!m_Languages.TryGetValue(languageCode, out translations))
        //             {
        //                 translations = new Dictionary<string, string>();
        //                 m_Languages.Add(languageCode, translations);
        //             }

        //             translations.Add(phraseKey, keyValue.value);
        //         }
        //     }
        // }

        // ====================================================================
        // Non-serialized members
        // ====================================================================

        private string m_LanguageCode;
        private Dictionary<string, Dictionary<string, string>> m_Languages = new Dictionary<string, Dictionary<string, string>>();

        private List<MelcherUnicodeDictionary> m_UnicodeDictionaries = new List<MelcherUnicodeDictionary>();

    }   // class MelcherLocalizer

}   // namespace Melcher
