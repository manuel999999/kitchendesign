namespace Melcher
{
    using System.Text;

    /// <summary>
    ///     Provides functions for converting unformatted phone numbers to
    ///     formatted and vice versa:
    ///     <br /><br />
    ///
    ///     <code>Format("1234567890") => "(123) 456 - 7890"</code>
    ///
    ///     <code>Unformat("(123) 456 - 7890") => "1234567890"</code>
    ///
    /// </summary>
    public static class PhoneNumber
    {
        const int MaxPhoneNumberLength = 10;

        /// <summary>
        ///     Returns an unformatted phone number that is <see
        ///     cref="MaxPhoneNumberLength"/> digits long.
        /// </summary>
        public static string Random()
        {
            s_UnformattedNumberBuilder.Clear();

            for (int i = 0; i < MaxPhoneNumberLength; ++i)
            {
                int minDigit = 0;

                if (i == 0)
                {
                    minDigit = 1;
                }

                int digit = UnityEngine.Random.Range(minDigit, 9);

                s_UnformattedNumberBuilder.Append(digit);
            }

            return s_UnformattedNumberBuilder.ToString();
        }

        public static string Format(string unformattedNumber)
        {
            return Format(unformattedNumber, unformattedCursorPos: 0, out int _);
        }

        public static string Format(string unformattedNumber, int unformattedCursorPos, out int formattedCursorPos)
        {
            // Ensure it is unformatted. It must be unformatted for the
            // cursor position stuff to make sense
            unformattedNumber = Unformat(unformattedNumber, unformattedCursorPos, out unformattedCursorPos);

            int unformattedLength = unformattedNumber.Length;

            formattedCursorPos = unformattedLength;

            if (unformattedLength > 6)
            {
                // "(###) ### - ####"

                if (unformattedCursorPos >= 0)
                {
                    ++formattedCursorPos;
                }
                if (unformattedCursorPos >= 3)
                {
                    formattedCursorPos += 2;
                }
                if (unformattedCursorPos >= 6)
                {
                    formattedCursorPos += 3;
                }

                return string.Format(
                    "({0}) {1} - {2}",
                    arg0: unformattedNumber.Substring(0, 3),
                    arg1: unformattedNumber.Substring(3, 3),
                    arg2: unformattedNumber.Substring(6)
                );
            }
            else if (unformattedLength > 3)
            {
                // "(###) ###"

                if (unformattedCursorPos >= 0)
                {
                    ++formattedCursorPos;
                }
                if (unformattedCursorPos >= 3)
                {
                    formattedCursorPos += 2;
                }

                return string.Format(
                    "({0}) {1}",
                    arg0: unformattedNumber.Substring(0, 3),
                    arg1: unformattedNumber.Substring(3)
                );
            }
            else if (unformattedLength > 0)
            {
                // "###"

                if (unformattedCursorPos >= 0)
                {
                    ++formattedCursorPos;
                }

                return string.Format(
                    "{0}",
                    arg0: unformattedNumber
                );
            }

            return "";
        }

        public static string Unformat(string formattedNumber)
        {
            return Unformat(formattedNumber, formattedCursorPos: 0, out int _);
        }

        public static string Unformat(string formattedNumber, int formattedCursorPos, out int unformattedCursorPos)
        {
            unformattedCursorPos = formattedCursorPos;

            s_UnformattedNumberBuilder.Clear();

            // Remove all non-digit characters. Adjust the cursor to account
            // for removed characters.

            int formattedNumberLength = formattedNumber.Length;

            for (int i = 0; i < formattedNumberLength; ++i)
            {
                if (char.IsDigit(formattedNumber[i]))
                {
                    s_UnformattedNumberBuilder.Append(formattedNumber[i]);
                }
                else if (i < formattedCursorPos)
                {
                    --unformattedCursorPos;
                }
            }

            if (s_UnformattedNumberBuilder.Length > MaxPhoneNumberLength)
            {
                s_UnformattedNumberBuilder.Length = MaxPhoneNumberLength;
            }

            return s_UnformattedNumberBuilder.ToString();
        }

        // ====================================================================
        // Static fields
        // ====================================================================

        private static StringBuilder s_UnformattedNumberBuilder = new StringBuilder();

    }   // static class PhoneNumber

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-17
/// ===========================================================================
