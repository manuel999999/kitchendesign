namespace Melcher
{
    using TMPro;
    using UnityEngine;

    /// <summary>
    ///     A validator for a <see cref="TMP_InputField"/> that ensures the
    ///     user only enters digits and ensures the display is formatted
    ///     nicely.
    /// </summary>
    [CreateAssetMenu(fileName = "PhoneNumberInputFieldValidator", menuName = "Melcher Studios/Phone Input Field Validator")]
    public class PhoneNumberInputFieldValidator : TMP_InputValidator
    {
        public override char Validate(ref string text, ref int pos, char ch)
        {
            if (char.IsDigit(ch))
            {
                // Phone numbers can only contain digits. Ignore bad input.

                string unformattedNumber = PhoneNumber.Unformat(text, pos, out int unformattedInsertPos);

                unformattedNumber = unformattedNumber.Insert(unformattedInsertPos, ch.ToString());
                ++unformattedInsertPos;

                text = PhoneNumber.Format(unformattedNumber, unformattedInsertPos, out pos);
            }

            // Always return '\0' because we are formatting the entire string.
            // If we were return a normal character here, it would be appended
            // after our formatted text and cause characters to be duplicated:
            // "1" is formatted to "(1)" which would then be concatenated with
            // 1 to become "(1)1"
            return '\0';
        }

    }   // class PhoneNumberInputFieldValidator

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-16
/// ===========================================================================
