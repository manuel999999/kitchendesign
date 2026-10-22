using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KitchenDesign.Shared.Common;
using Melcher;
using UnityEngine;

namespace KitchenDesign.Utility
{
    public static class MeasurementFormat
    {
        private static readonly string[] DoubleQuoteCharacters = new[]
        {
            "\u201C",
            "\u201D",
            "\u201F",
            "\u275D",
            "\u275E",
            "\u301D",
            "\u301E",
            "\U0001F676",
            "\U0001F677",
            "\U0001F678"
        };

        private static readonly string[] SingleQuoteCharacters = new[]
        {
            "\u2018",
            "\u2019",
            "\u201B",
            "\u275B",
            "\u275C",
            "\uFF02",
        };

        public static bool TryParseImperialString(
            string          imperialText,
            out float       lengthInMeters,
            string          name = null,
            List<string>    errors = null)
        {
            lengthInMeters = 0.0f;

            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.Empty;
            }
            else
            {
                name += ": ";
            }

            System.Text.StringBuilder buffer = new System.Text.StringBuilder(imperialText);
            foreach (string singleQuote in SingleQuoteCharacters)
            {
                buffer.Replace(singleQuote, "'");
            }
            foreach (string doubleQuote in DoubleQuoteCharacters)
            {
                buffer.Replace(doubleQuote, "\"");
            }
            imperialText = buffer.ToString();

            const char Feet = '\'';
            const char Inch = '"';
            const char Divide = '/';

            MatchCollection regexMatches = Regex.Matches(imperialText, @$"(?:(\d+(?:\.\d*)?)\s*[{Inch}{Feet}{Divide}]?)");

            int matchCount = regexMatches.Count;
            float lengthInInches = 0.0f;
            for (int i = 0; i < matchCount; ++i)
            {
                string numberMatchText = regexMatches[i].Groups[1].Value;
                if (!float.TryParse(numberMatchText, out float numberMatchAsFloat))
                {
                    errors?.Add(name + "Failed to parse float from: " + numberMatchText);
                    return false;
                }

                string wholeMatchText = regexMatches[i].Value;
                if (wholeMatchText.EndsWith(Feet))
                {
                    numberMatchAsFloat *= Units.InchesPerFoot;

                }
                else if (i == 0 && wholeMatchText.EndsWith(" ") &&
                    ((i + 1 >= matchCount) || !regexMatches[i + 1].Value.EndsWith(Divide)))
                {
                    numberMatchAsFloat *= Units.InchesPerFoot;
                }
                else if (wholeMatchText.EndsWith(Divide))
                {
                    if (i + 1 >= matchCount)
                    {
                        errors?.Add(name + "Invalid imperial format: Division symbol must have a number after it");
                        break;
                    }

                    string nextNumberMatchText = regexMatches[i + 1].Groups[1].Value;
                    if (!float.TryParse(nextNumberMatchText, out float divisor))
                    {
                        errors?.Add(name + "Failed to parse float from: " + nextNumberMatchText);
                        break;
                    }

                    int numeratorAsInt = Mathf.RoundToInt(numberMatchAsFloat);
                    int divisorAsInt = Mathf.RoundToInt(divisor);

                    if (divisor == 0.0f)
                    {
                        errors?.Add(name + $"Would-be division by zero: {wholeMatchText}/{regexMatches[i + 1].Value}");
                        return false;
                    }

                    if (!Mathf.Approximately(numberMatchAsFloat - numeratorAsInt, 0.0f))
                    {
                        errors?.Add(name + $"Numerator is a decimal: {wholeMatchText}/{regexMatches[i + 1].Value}");
                    }

                    if (!Mathf.Approximately(divisor - divisorAsInt, 0.0f))
                    {
                        errors?.Add(name + $"Denominator is a decimal: {wholeMatchText}/{regexMatches[i + 1].Value}");
                    }

                    if (divisorAsInt != 3 && divisorAsInt != 5 && Mathf.NextPowerOfTwo(divisorAsInt) != divisorAsInt)
                    {
                        errors?.Add(name + $"Non-standard imperial denominator: {wholeMatchText}/{regexMatches[i + 1].Value}");
                    }

                    numberMatchAsFloat /= divisor;

                    if (numberMatchAsFloat >= 1.0f)
                    {
                        errors?.Add(name + $"Improper fraction: {wholeMatchText}/{regexMatches[i + 1].Value}");
                    }

                    ++i;
                }

                lengthInInches += numberMatchAsFloat;
            }

            lengthInMeters = lengthInInches * Units.Inches2Meters;

            return true;
        }

        public static string FormatMetersToCentimeters(float sizeInMeters, int? rightDecimalPlaces = 2, int? leftDecimalPlaces = null)
        {
            float sizeInCentimeters = sizeInMeters * Units.Meters2Centimeters;

            string result =
                rightDecimalPlaces.HasValue ?
                    sizeInCentimeters.ToString("N" + rightDecimalPlaces.Value) :
                    sizeInCentimeters.ToString();

            if (leftDecimalPlaces.HasValue)
            {
                int numberOfDigitsBeforeDecimal =
                    result.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

                if (numberOfDigitsBeforeDecimal < 0)
                {
                    numberOfDigitsBeforeDecimal = 0;
                }

                int unrestrictedLength =
                    result.Length - numberOfDigitsBeforeDecimal;

                result = result.PadLeft(unrestrictedLength + leftDecimalPlaces.Value);
            }

            return $"{result}cm";
        }

        public static string FormatMetersToImperial(float sizeInMeters, bool showFeet = true, bool showFraction = true, int minimumInchDivisions = ImperialUnit.DefaultMaximumInchDivision)
        {
            return ImperialUnit.FromTotalMeters(sizeInMeters).ToString(showFeet, showFraction, minimumInchDivisions);
        }

        public static string FormatMetersToImperialMonospaced(
            StringBuilder stringBuilder,
            int monoSize,
            float sizeInMeters,
            bool showFeet = true,
            bool showFraction = true,
            int minimumInchDivisions = ImperialUnit.DefaultMaximumInchDivision,
            bool showInchesWhenZero = true)
        {
            stringBuilder.Clear();

            ImperialUnit.FromTotalMeters(sizeInMeters)
                .GetValuesUsingMaximumInchDivisions(minimumInchDivisions, out int feet, out int inches, out int numerator, out int denominator);

            stringBuilder.AppendFormat("<mspace={0}>", monoSize);
            {
                if (feet > 0)
                {
                    stringBuilder.AppendFormat("{0,2}'", feet);
                }
                else
                {
                    stringBuilder.Append(' ', 3);
                }

                if (inches > 0 || feet > 0 || showInchesWhenZero)
                {
                    stringBuilder.AppendFormat("{0,2}\"", inches);
                }
                else
                {
                    stringBuilder.Append(' ', 3);
                }
            }
            stringBuilder.Append("</mspace>");

            stringBuilder.AppendFormat("<size=50%><mspace={0}>", monoSize / 2);
            {
                if (numerator > 0 && denominator > 0)
                {
                    stringBuilder.AppendFormat("{0,1}/{1,1}", numerator, denominator);
                }
                else
                {
                    stringBuilder.Append(' ', 3);
                }
            }
            stringBuilder.Append("</mspace></size>");

            return stringBuilder.ToString();
        }
    }
}
