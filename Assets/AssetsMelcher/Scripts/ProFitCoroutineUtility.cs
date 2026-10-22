namespace KitchenDesign
{
    using Melcher;
    using MEC;
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Utility methods for coroutines.
    /// </summary>
    public static class ProFitCoroutineUtility
    {
        /// <summary>
        ///     Starts a coroutine that will invoke the action once the
        ///     requested number of frames have elapsed. The default number
        ///     of frames to wait is 1.
        /// </summary>
        /// <param name="action">
        ///     The action to invoke after the delayed frames.
        /// </param>
        /// <param name="framesToDelay">
        ///     The number of game frames to wait before the action is invoked.
        /// </param>
        public static CoroutineHandle CallDelayedByFrames(Action action, int framesToDelay = 1)
        {
            return Timing.RunCoroutine(InternalCoroutine(action, framesToDelay));

            static IEnumerator<float> InternalCoroutine(Action action, int framesToDelay)
            {
                for (; framesToDelay > 0; --framesToDelay)
                {
                    yield return Timing.WaitForOneFrame;
                }

                action.Invoke();
            }
        }

        /// <inheritdoc cref="CallDelayedByFrames(Action, int)"/>
        /// <param name="actionArgument">
        ///     Data that will be passed to the action once the delay has
        ///     elapsed.
        /// </param>
        // The parameter order (`actionArgument` before `action`) is intended
        // to reduce the likelihood of mixing `arg` and `framesToDelay`.
        public static CoroutineHandle CallDelayedByFrames<ArgType>(ArgType actionArgument, Action<ArgType> action, int framesToDelay = 1)
        {
            return Timing.RunCoroutine(InternalCoroutine(actionArgument, action, framesToDelay));

            static IEnumerator<float> InternalCoroutine(ArgType arg, Action<ArgType> action, int framesToDelay)
            {
                for (; framesToDelay > 0; --framesToDelay)
                {
                    yield return Timing.WaitForOneFrame;
                }

                action.Invoke(arg);
            }
        }

        public static CoroutineHandle CallWhenTrue(Action action, Func<bool> predicate)
        {
            return Timing.RunCoroutine(InternalRoutine(action, predicate));

            static IEnumerator<float> InternalRoutine(Action action, Func<bool> predicate)
            {
                if (predicate != null)
                {
                    yield return Timing.WaitUntilTrue(predicate);
                }

                action?.Invoke();
            }
        }

    }   // static class ProFitCoroutineUtility

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-10-03
/// ===========================================================================
