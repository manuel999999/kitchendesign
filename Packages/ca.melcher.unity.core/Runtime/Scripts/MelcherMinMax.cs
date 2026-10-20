namespace Melcher
{
    /// <summary>
    ///     <see cref="MelcherMinMax"/> is essentially just a Vector2 with more
    ///     meaningful field names.
    /// </summary>
    [System.Serializable]
    public struct MelcherMinMax
    {
        public static MelcherMinMax zeroOne => new MelcherMinMax(0.0f, 1.0f);

        // =====================================================================
        // Factory functions
        // =====================================================================

        public static MelcherMinMax FromMinAndSpan(float min, float span)
        {
            return new MelcherMinMax(min, min + span);
        }

        public static MelcherMinMax FromMaxAndSpan(float max, float span)
        {
            return new MelcherMinMax(max - span, max);
        }

        // =====================================================================
        // Constructor
        // =====================================================================

        public MelcherMinMax(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        // =====================================================================
        // Properties
        // =====================================================================

        public float span => max - min;

        // =====================================================================
        // Fields
        // =====================================================================

        public float min;
        public float max;

    }   // class MelcherMinMax

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-02-20
/// ===========================================================================
