namespace Melcher
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// <see cref="MelcherKeyValues"/> represents an ad-hoc file format. The
    /// format is linear (not a tree), has a very simple syntax, and easily
    /// supports multi-line strings.
    /// 
    /// It is a useful format for storing translations.
    ///
    /// Example file syntax:
    /// <code>
    /// // This is a comment
    ///
    /// # LanguageName
    /// @en: English
    /// @fr: Francais
    ///
    /// # MyBlock       // A leading # followed by a space starts a block. The text after it defines the block's name
    /// @KeyOne: And it's value      // Key = "KeyOne", Value = "And it's value"
    /// @KeyTwo: A multi-line
    /// value. The newlines are preserved
    /// in the extracted value.
    /// 
    /// // "KeyTwo" = "A multi-line\nvalue. The newlines are preserved\nin the extracted value."
    /// 
    /// </code>
    /// </summary>
    public class MelcherKeyValues : MelcherMonoBehaviour
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public static List<KeyValueBlock> ParseFile(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            return ParseLines(lines);
        }

        public static List<KeyValueBlock> ParseText(string text)
        {
            string[] lines = MelcherCommon.SplitNewLines(text);
            return ParseLines(lines);
        }

        public static List<KeyValueBlock> ParseLines(IList<string> lines)
        {
            var blocks = new List<KeyValueBlock>();

            KeyValueBlock currentBlock = null;
            string currentKey = null;
            var currentValue = new StringBuilder();
            int trailingBlankLineCount = 0;

            void FinalizeKeyValue()
            {
                currentBlock.keyValues.Add(
                    new KeyValuePair<string, string>(currentKey, currentValue.ToString())
                );

                currentKey = null;
                currentValue.Clear();
                trailingBlankLineCount = 0;
            }

            void FinalizeBlock()
            {
                if (currentKey != null)
                {
                    FinalizeKeyValue();
                }

                blocks.Add(currentBlock);
                currentBlock = null;
            }

            // Read the file line by line
            int lineCount = lines.Count;
            for (int i = 0; i < lineCount; ++i)
            {
                var line = lines[i];
                int lineNumber = i + 1;

                if (line.StartsWith(CommentStart))
                {
                    // Skip comments. Comment must be at the start of the line
                    continue;
                }
                else if (line.StartsWith(BlockStart))
                {
                    if (currentBlock != null)
                    {
                        FinalizeBlock();
                    }

                    currentBlock = new KeyValueBlock();
                    currentBlock.name = line.Substring(BlockStart.Length).Trim();
                }
                else if (line.StartsWith(KeyStart))
                {
                    if (currentBlock == null)
                    {
                        Debug.LogError($"Key defined outside group: line {lineNumber}");
                        continue;
                    }

                    if (currentKey != null)
                    {
                        FinalizeKeyValue();
                    }

                    int endOfKey = line.IndexOf(KeyValueSeparator);
                    if (endOfKey > KeyStart.Length)
                    {
                        currentKey = line.Substring(KeyStart.Length, endOfKey - KeyStart.Length).Trim();

                        var value = line.Substring(endOfKey + KeyValueSeparator.Length).Trim();
                        currentValue.AppendLine(value);
                    }
                    else if (endOfKey == KeyStart.Length)
                    {
                        Debug.LogError($"Missing key name: line {lineNumber}");
                        continue;
                    }
                    else
                    {
                        // Use the whole line as the key
                        currentKey = line.Substring(KeyStart.Length).Trim();
                    }

                    // Remove leading and trailing whitespace around key
                    currentKey = currentKey.Trim();
                }
                else if (currentKey != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        // Count trailing blank lines. These won't be used
                        // unless more content for this key follows.
                        ++trailingBlankLineCount;
                    }
                    else
                    {
                        // Include previously skipped blank lines
                        while (trailingBlankLineCount > 0)
                        {
                            currentValue.AppendLine();
                            --trailingBlankLineCount;
                        }

                        currentValue.AppendLine(line);
                    }
                }
            }

            if (currentBlock != null)
            {
                FinalizeBlock();
            }

            return blocks;
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        /// <summary>
        /// Pattern to start a new block. A block has a name and a list of any
        /// number of key-value pairs.
        /// </summary>
        private const string BlockStart = "#";

        /// <summary>
        /// Pattern to start a new key value pair. Should appear after a block
        /// or after another key.
        /// </summary>
        private const string KeyStart = "@";

        /// <summary>
        /// Pattern to separate the value from the key.
        /// </summary>
        private const string KeyValueSeparator = ":";

        /// <summary>
        /// Pattern to mark a comment line.
        /// </summary>
        private const string CommentStart = "//";

        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        /// A block. It has a name and a list of key-value pairs. The key-values
        /// are string to string and so any non-string value type must be parsed
        /// by the user.
        /// </summary>
        public class KeyValueBlock
        {
            public string name;
            public List<KeyValuePair<string, string>> keyValues;

            public KeyValueBlock()
            {
                keyValues = new List<KeyValuePair<string, string>>();
            }

            public bool TryGetValue(string key, out string value)
            {
                value = null;

                var matchIndex = keyValues.FindIndex((x) => x.Key == key);
                if (matchIndex >= 0)
                {
                    value = keyValues[matchIndex].Value;
                }

                return matchIndex >= 0;
            }
        }   // class KeyValueBlock

    }   // class MelcherKeyValues

}   // namespace Melcher
