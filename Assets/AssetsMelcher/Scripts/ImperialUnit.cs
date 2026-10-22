namespace KitchenDesign
{
    using Melcher;
    using System;

    /// <summary>
    /// Handles unit conversion between imperial and metric.
    /// </summary>
    public struct ImperialUnit
    {
        public const int DefaultMaximumInchDivision = 8;

        // ====================================================================
        // Static functions
        // ====================================================================

        public static ImperialUnit FromTotalMeters(float totalMeters)
        {
            return new ImperialUnit()
            {
                m_Inches = totalMeters * Const.Meters2Inches
            };
        }

        public static ImperialUnit FromTotalInches(float totalInches)
        {
            return new ImperialUnit()
            {
                m_Inches = totalInches
            };
        }

        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        /// The total measurement in meters.
        /// </summary>
        public double totalMeters => m_Inches * Const.Inches2Meters;

        /// <summary>
        /// The number of whole feet that the measurement is divisible by.
        /// </summary>
        public int feet => (int)Math.Floor(m_Inches / Const.InchesPerFoot);

        /// <summary>
        /// The total measurement in feet.
        /// </summary>
        public double totalFeet => m_Inches / Const.InchesPerFoot;

        /// <summary>
        /// The number of whole inches the measurement is divisible by that
        /// extend beyond whole divisions of feet. That is, the leftover inches
        /// after <see cref="feet"/> is removed.
        /// </summary>
        public int inches => (int)Math.Floor(m_Inches - (feet * Const.InchesPerFoot));

        /// <summary>
        /// The total measurement in inches.
        /// </summary>
        public double totalInches => m_Inches;

        /// <summary>
        /// The fraction part of the measurement.
        /// </summary>
        public (int numerator, int denominator) fraction => GetFraction();

        /// <summary>
        /// The fractional part of the measurement as a decimal number.
        /// </summary>
        public double fractionAsDecimal => Math.Ceiling(m_Inches) - m_Inches;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        /// Gets the feet, inches and fraction, but ensures any rounding at the
        /// fractional level, caused by the resolution of <paramref
        /// name="maximumInchDivisions"/>, bubbles up to the inches and feet
        /// measurements. E.g., if a fraction appears to be 1/1, the inches
        /// will be increased by 1, and if inches goes above 11, the feet will
        /// be increased by one.
        /// </summary>
        /// <param name="maximumInchDivisions"></param>
        /// <param name="feet"></param>
        /// <param name="inches"></param>
        /// <param name="numerator"></param>
        /// <param name="denominator"></param>
        public void GetValuesUsingMaximumInchDivisions(int maximumInchDivisions, out int feet, out int inches, out int numerator, out int denominator)
{
            (numerator, denominator) = GetFraction(maximumInchDivisions);

            inches = this.inches;

            if (numerator == 1 && denominator == 1)
            {
                numerator = 0;
                denominator = 0;
                ++inches;
            }

            feet = this.feet;

            while (inches >= Const.InchesPerFoot)
            {
                inches -= (int)Const.InchesPerFoot;
                ++feet;
            }
        }

        public (int numerator, int denominator) GetFraction(int maximumInchDivisions = DefaultMaximumInchDivision)
        {
            // The number of non-whole inches left
            double partialInches = m_Inches - Math.Floor(m_Inches);

            /// Determine if the leftover inches are within some threshold of
            /// a denominator. This is used to check for non-power-of-two
            /// denominators.
            static bool IsMultiple(double partialInches, double denominator, ref int numerator)
            {
                if (Math.Abs(Math.Round(partialInches) - partialInches) < 0.0001)
                {
                    // Partial inches is effectively 0 or 1, which we don't
                    // want display as a fraction.
                    return false;
                }

                double increment = 1.0 / denominator;

                double numberOfIncrements = partialInches / increment;
                numerator = (int)Math.Round(numberOfIncrements);

                const double Threshold = 0.0001;

                return Math.Abs(numberOfIncrements - numerator) <= Threshold;
            }

            int numerator = 0;

            // Check for some non-power of two denominators.

            if (IsMultiple(partialInches, 3, ref numerator))
            {
                return (numerator, 3);
            }

            if (IsMultiple(partialInches, 5, ref numerator))
            {
                return (numerator, 5);
            }

            // Determine the best power-of-two ratio.

            numerator =  (int)Math.Round((m_Inches - Math.Floor(m_Inches)) * maximumInchDivisions);
            int denominator = maximumInchDivisions;

            // Simplify the fraction by iteratively halving it
            while (numerator > 0 && (numerator % 2) == 0)
            {
                numerator /= 2;
                denominator /= 2;
            }

            return (numerator, denominator);
        }

        public void SetFromTotalMeters(float newTotalMeters)
        {
            m_Inches = newTotalMeters * Const.Meters2Inches;
        }

        public void SetFromTotalInches(double totalInches)
        {
            m_Inches = totalInches;
        }

        public void Set(int feet, int inches)
        {
            Set(feet, inches, numerator: 0, denominator: 0);
        }

        public void Set(int feet, int inches, int numerator, int denominator)
        {
            m_Inches = (feet * Const.InchesPerFoot) + inches;
            if (denominator > 0)
            {
                m_Inches += (double)numerator / denominator;
            }
        }

        public ImperialUnit RoundToNearestDivision(int maximumInchDivision = DefaultMaximumInchDivision)
        {
            GetValuesUsingMaximumInchDivisions(
                maximumInchDivisions: 8,
                out int feet,
                out int inches,
                out int numerator,
                out int denominator
            );

            Set(feet, inches, numerator, denominator);

            return this;
        }

        /// <summary>
        /// Format a string to contain the feet, inches and fraction of the
        /// imperial measurement. If any of those components is zero, it is
        /// omitted from the result.
        /// </summary>
        public override string ToString()
        {
            return ToString(showFeet: true, showFraction: true, DefaultMaximumInchDivision);
        }

        public string ToString(bool showFeet, bool showFraction = true, int maximumInchDivisions = DefaultMaximumInchDivision)
        {
            string s = "";

            GetValuesUsingMaximumInchDivisions(maximumInchDivisions, out int feet, out int inches, out int numerator, out int denominator);

            if (!showFeet)
            {
                inches += (int)(feet * Const.InchesPerFoot);
                feet = 0;
            }

            // Create the output text

            bool needsInchMark = false;
            bool nextAppendNeedsSpace = false;

            void AppendText(string toAppend)
            {
                if (nextAppendNeedsSpace)
                {
                    s += ' ';
                }

                s += toAppend;
                nextAppendNeedsSpace = true;
            }

            if (feet > 0)
            {
                AppendText($"{feet}'");
            }

            if (inches > 0)
            {
                AppendText($"{inches}");
                needsInchMark = true;
            }

            // Add the fractional part
            if (showFraction)
            {
                if (numerator > 0 && denominator > 0)
                {
                    AppendText($"{numerator}/{denominator}");
                    needsInchMark = true;
                }
            }

            if (needsInchMark)
            {
                nextAppendNeedsSpace = false;
                AppendText("\"");
            }

            if (string.IsNullOrEmpty(s))
            {
                s = "0\"";
            }

            return s;
        }

        // ====================================================================
        // Internal fields
        // ====================================================================

        private double m_Inches;

    }   // class ImperialUnit

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-10-19
/// ===========================================================================
