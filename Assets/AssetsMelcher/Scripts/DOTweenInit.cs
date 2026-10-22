namespace KitchenDesign
{
    using Melcher;
    using DG.Tweening;

    /// <summary>
    /// Provides a static function to initialize DOTween with an initial
    /// tween and sequence capacity.
    /// </summary>
    public static class DOTweenInit
    {
        public static void Init(bool isDebugMode)
        {
            MelcherLog.Log($"Initializing DOTween for {s_MaxTweenCount} tweens and {s_MaxSequenceCount} sequences.", severity: MelcherLogSeverity.Verbose);

            DOTween.debugMode = isDebugMode;
            DOTween.showUnityEditorReport = isDebugMode;

            DOTween.Init(
                recycleAllByDefault: true,
                useSafeMode: isDebugMode,
                logBehaviour: isDebugMode ? LogBehaviour.Verbose : LogBehaviour.ErrorsOnly
            ).SetCapacity(s_MaxTweenCount, s_MaxSequenceCount);
        }

        // These values were chosen based on what DOTween warned was needed as
        // it expanded beyond its default capacities
        private const int s_MaxTweenCount = 1024;
        private const int s_MaxSequenceCount = 256;

    }   // class DOTweenInit

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-24
/// ===========================================================================
