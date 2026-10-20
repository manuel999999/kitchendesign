namespace Melcher
{
    /// <summary>
    /// Common constants used by Unity. It's nice to use the symbolic names
    /// provided by this class rather than use magic numbers.
    /// </summary>
    public static class MelcherConst
    {
        // ====================================================================
        // Unity Animator
        // ====================================================================

        public static class Animator
        {
            /// <summary>
            /// Special layer number for <see cref="UnityEngine.Animator.Play(int, int)"/>
            /// that selects the first state with the matching name or hash.
            /// </summary>
            public const int FirstMatchingState = -1;

            /// <summary>
            /// The default or base layer index of an animator.
            /// </summary>
            public const int BaseLayer = 0;

        }   // class Animator

        // ====================================================================
        // Unity Mixer
        // ====================================================================

        public static class Mixer
        {
            public const float DecibelMin = -80.0f;
            public const float DecibelMax = 0.0f;
            public const float DecibelMaxBoosted = 20.0f;

        }   // class Mixer

        // ====================================================================
        // Unity Audio Source
        // ====================================================================

        public static class AudioSource
        {
            public const float PitchMin = -3.0f;
            public const float PitchMax = 3.0f;

            public const float VolumeMin = 0.0f;
            public const float VolumeMax = 1.0f;

        }   // class AudioSource

        // ====================================================================
        // Unity Game Object Tag
        // ====================================================================

        /// <summary>
        /// Class to contain symbolic names for tags, which is nicer than using
        /// string literals. This nested class is also marked partial so it
        /// can be extended in a project-specific file.
        /// </summary>
        public static class Tag
        {
            public const string EditorOnly = "EditorOnly";
        }

    }   // class MelcherConst

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
