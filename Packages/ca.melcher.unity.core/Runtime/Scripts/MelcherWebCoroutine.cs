namespace Melcher
{
    using System.Collections.Generic;
    using MEC;

    // REVIEW:
    //      The rationale of using this type to "handle mock web-requests" is
    //      misguided. It should be outside the scope of the endpoint to handle
    //      special state such as guest mode.
    //
    //      A better architecture is to define a server API interface and
    //      implement a guest version, thus separating the guest mode logic
    //      from the real world logic.
    //
    //      -Ryan

    /// <summary>
    ///     Prefer this as the return type for any web-based API. It achieves
    ///     two goals:
    ///     <br /><br />
    ///
    ///     1. Always converts to a valid coroutine handle so it can be
    ///        reliably waited upon (<see cref="Timing.WaitUntilDone"/>) even
    ///        if the endpoint doesn't reach the point of actually kicking off
    ///        a coroutine.<br />
    ///     2. Provides easy interoperability with coroutines by implicitly
    ///        converting to or from a More Effective Coroutines coroutine
    ///        handle.<br />
    ///
    ///     It is common to have a web API perform mock web-requests in some
    ///     situations. E.g., to allow a sign in to be bypassed. Normally,
    ///     returning a <see langword="default"/> <see cref="CoroutineHandle"/>
    ///     results in an invalid handle that will error if passed to <see
    ///     cref="Timing.WaitUntilDone"/>. To fix this, either all call sites
    ///     must be check for this or the endpoint author must ensure the
    ///     function never returns an invalid coroutine.
    ///     <see cref="MelcherWebCoroutine"/> automates the latter.
    /// </summary>
    /// <remarks>
    ///     <code>
    ///         // Setup
    ///         public static MelcherWebCoroutine LogIn(string username)
    ///         {
    ///             if (username == "guest")
    ///             {
    ///                 return default;
    ///             }
    ///
    ///             return MelcherWebRequestBuilder.Get(/* ... */).Send();
    ///         }
    ///
    ///         // Use
    ///         Timing.WaitUntilDone(LogIn("guest"));
    ///   </code>
    /// </remarks>
    public readonly struct MelcherWebCoroutine
    {
        // ====================================================================
        // Constructors
        // ====================================================================

        public MelcherWebCoroutine(CoroutineHandle coroutine)
        {
            m_CoroutineHandle = coroutine;
        }

        // ====================================================================
        // Implicit casts
        // ====================================================================

        /// <summary>
        ///     Make a <see cref="MelcherWebCoroutine"/> from a <see
        ///     cref="CoroutineHandle"/>.
        /// </summary>
        public static implicit operator MelcherWebCoroutine(CoroutineHandle coroutine)
        {
            return new MelcherWebCoroutine(coroutine);
        }

        /// <summary>
        ///     Return a valid <see cref="CoroutineHandle"/>.
        /// </summary>
        public static implicit operator CoroutineHandle(MelcherWebCoroutine wrapper)
        {
            if (wrapper.m_CoroutineHandle.IsValid)
            {
                return wrapper.m_CoroutineHandle;
            }

            return Timing.RunCoroutine(DoNothing());

            static IEnumerator<float> DoNothing()
            {
                yield break;
            }
        }

        // ====================================================================
        // Internal fields
        // ====================================================================

        private readonly CoroutineHandle m_CoroutineHandle;

    }   // class MelcherWebCoroutine

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-01-04
/// ===========================================================================
