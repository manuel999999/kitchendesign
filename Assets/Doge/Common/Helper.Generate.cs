using System;

namespace Doge.Common
{
    public static partial class Helper
    {
        private static readonly string GENERATE_LETTERS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string GENERATE_NUMBERS = "1234567890";
        private static readonly string GENERATE_SPECIAL = "!@#%^&*_+/\\;";

        private static string GenerateString(int len, string entries) {
            var stringChars = new char[len];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++) {
                stringChars[i] = entries[random.Next(entries.Length)];
            }

            return new string(stringChars);
        }

        /// <summary>
        /// Generate string with letters, numbers, and special characters.
        /// </summary>
        /// <param name="len">String length.</param>
        /// <returns>Randomize string.</returns>
        public static string GenerateString(int len) {
            return GenerateString(len, $"{GENERATE_LETTERS}{GENERATE_NUMBERS}{GENERATE_SPECIAL}");
        }

        /// <summary>
        /// Generate string with letters only.
        /// </summary>
        /// <param name="len">String length.</param>
        /// <returns>Randomize string.</returns>

        public static string GenerateLettersString(int len) {
            return GenerateString(len, $"{GENERATE_LETTERS}");
        }

        /// <summary>
        /// Generate string with letters and numbers.
        /// </summary>
        /// <param name="len">String length.</param>
        /// <returns>Randomize string.</returns>
        public static string GenerateAlphaNumericString(int len) {
            return GenerateString(len, $"{GENERATE_LETTERS}{GENERATE_NUMBERS}");
        }
    }
}
