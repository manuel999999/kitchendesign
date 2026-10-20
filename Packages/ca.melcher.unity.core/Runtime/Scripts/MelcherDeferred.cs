namespace Melcher
{
    using System;
    using System.ComponentModel;

    /// <summary>
    ///     Use this to schedule code to execute at the end of a function,
    ///     regardless of how the function exits. It is useful anywhere
    ///     variables are temporarily set and then restored.
    ///     <br /><br />
    ///
    ///     <code>
    ///     // To create the deferred object: capture it to a variable with the
    ///     // `using` keyword.
    ///     using var myDeferredAction =
    ///         MelcherDeferred.Defer(
    ///             () => MelcherLog.Log("This will print at the moment the function exits.")
    ///         );
    ///     </code>
    /// </summary>
    /// <verbose>
    /// <see cref="MelcherDeferred" /> provides factory functions to make
    /// deferred <see cref="Action">actions</see>. A deferred action refers to
    /// code that is executed when a source code block goes out of scope.
    /// Deferred actions execute in the reverse order that they were declared.
    /// <br /><br />
    ///
    /// The purpose of a defer is to guarantee the execution of some code,
    /// regardless of how the scope was exited. That is, the code is executed
    /// even if an exception is thrown, the containing function returns early,
    /// or its just a normal scope exit. This is similar to a <c>try-finally</c>
    /// block except that deferred actions are more succinct and convenient to
    /// use.
    /// <br /><br />
    ///
    /// Deferrals come in two types:
    /// <br /><br />
    ///
    /// 1. An "optimized" single-argument version. It captures an argument at
    ///    construction which will then be passed into the on-dispose callback
    ///    when it is invoked. This is more performant because it allows the
    ///    instantiated lambda to be reused by identical defers.
    /// <br /><br />
    ///
    /// 2. A zero-argument version. This is more likely to incur a GC alloc due
    ///    the the lambda capturing external references.
    /// <br /><br />
    ///
    /// A deferred action must be used within a <see langword="using"/>
    /// statement in order to guarantee its behaviour. The one exception to
    /// using a <see langword="using"/> statement is within a function that
    /// returns a deferred function. In such a case, the function caller will be
    /// the one to use a <see langword="using"/> statement, but the function
    /// itself must never capture its return value in one.
    /// <br /><br />
    ///
    /// The <see cref="Details.DeferredAction.Dismiss">dismiss</see> method is
    /// provided to cancel a pending defer. This is useful within functions that
    /// allocate a resource for the caller, but whose body may fail prior to the
    /// resource being fully initialized. Within these types of functions, the
    /// resource can be guarded with an internal deferred action that frees the
    /// resource if an error occurs, but which is dismissed when the function is
    /// successful and before the resource is returned to the caller.
    /// <br /><br />
    ///
    /// Deferred actions are implemented as <see langword="struct"/>s. This
    /// helps avoid some GC overhead but also means that users must be diligent
    /// about not copying it around.
    /// <br /><br />
    ///
    /// Example:
    /// <code>
    /// Texture2D newTexture = new Texture2D(128, 128);
    /// using var releaseTexture = MelcherDeferred.Defer(
    ///     toCapture: newTexture,
    ///     // Free the texture at the end of the guard's scope
    ///     onDispose: () => Destroy(newTexture)
    /// );
    /// </code>
    /// </verbose>
    public static class MelcherDeferred
    {
        // =====================================================================
        // Factory functions
        // =====================================================================

        /// <summary>
        /// Create a deferred action to trigger at the end of the scope.
        /// <br /><br />
        ///
        /// The <paramref name="toCapture"/> argument will be passed into the
        /// <paramref name="onDispose"/> function when it is executed.
        /// <br /><br />
        ///
        /// This overload is better optimized for recurring, similar deferred
        /// actions. The <paramref name="onDispose"/> callback can be re-used
        /// between instances. The user must ensure the callback does not
        /// capture any non-static variables.
        /// </summary>
        public static Details.DeferredActionWithArg<CapturedArgumentType> Defer<CapturedArgumentType>(CapturedArgumentType toCapture, Action<CapturedArgumentType> onDispose)
        {
            return new Details.DeferredActionWithArg<CapturedArgumentType>(toCapture, onDispose);
        }

        /// <summary>
        /// A factory function that creates a deferred action. The factory
        /// function has the benefit of being able to infer the generic type
        /// arguments, whereas using the direct constructor would not.
        /// </summary>
        /// <remarks>
        /// This overload may be less performant than <see
        /// cref="Defer{CapturedArgumentType}(CapturedArgumentType, Action{CapturedArgumentType})">
        /// because it is more likely to need to capture external data and
        /// therefore require a new lambda instantiation.
        /// </remarks>
        /// <summary>
        /// Create a deferred action to trigger at the end of the scope.
        /// <br /><br />
        ///
        /// The <paramref name="toCapture"/> argument will be passed into the
        /// <paramref name="onDispose"/> function when it is executed.
        /// </summary>
        public static Details.DeferredAction Defer(Action onDispose)
        {
            return new Details.DeferredAction(onDispose);
        }

        // =====================================================================
        // Implementation details
        // =====================================================================

        /// <summary>
        /// This class contains implementation details that are not meant to
        /// be accessed by users. To help towards this end, the class is hidden
        /// from code completion when it is referenced from an external
        /// assembly.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public static class Details
        {
            /// <summary>
            /// A deferred action that attempts to minimize GC allocations by
            /// capturing its data as part of the constructor. It relies on the
            /// user being diligent to not introduce any variable captures in
            /// the callback they provide. I.e., it should only access the
            /// single argument that is passed to it.
            /// </summary>
            public struct DeferredActionWithArg<CapturedArgumentType> : IDisposable
            {
                // =============================================================
                // Constructors
                // =============================================================

                /// <summary>
                /// Create a deferred function and pre-capture the argument that
                /// will be passed to the dispose callback.
                /// <br /><br />
                ///
                /// The user must ensure their <paramref name="onDispose"/>
                /// callback does not capture any non-static references.
                /// </summary>
                public DeferredActionWithArg(CapturedArgumentType toCapture, Action<CapturedArgumentType> onDispose)
                {
                    this.captured = toCapture;
                    this.onDispose = onDispose;
                }

                // =============================================================
                // Methods
                // =============================================================

                /// <summary>
                /// Prevent the deferred action from triggering when the scope
                /// exits.
                /// </summary>
                public void Dismiss()
                {
                    onDispose = null;
                    captured = default;
                }

                // =============================================================
                // IDisposable
                // =============================================================
                #region IDisposable

                /// <summary>
                ///     Execute the deferred action immediately. Once invoked,
                ///     the action will not trigger again when its scope exits.
                ///     The action cannot be invoked more than once; subsequent
                ///     calls are ignored. If the action was <see
                ///     cref="Dismiss">dismissed</see>, the call does nothing.
                /// </summary>
                public void Dispose()
                {
                    onDispose?.Invoke(captured);

                    Dismiss();
                }

                #endregion  // IDisposable

                // =============================================================
                // Internal fields
                // =============================================================

                public CapturedArgumentType captured;

                public Action<CapturedArgumentType> onDispose;

            }   // struct DeferredActionWithArg

            public struct DeferredAction : IDisposable
            {
                // =============================================================
                // Constructors
                // =============================================================

                /// <summary>
                /// Create a deferred function that is less GC optimized than
                /// <see cref="DeferredActionWithArg{CapturedArgumentType}"/>.
                /// </summary>
                public DeferredAction(Action onDispose)
                {
                    this.onDispose = onDispose;
                }

                // ====================================================================
                // Methods
                // ====================================================================

                /// <summary>
                /// Prevent the deferred action from triggering when the scope
                /// exits.
                /// </summary>
                public void Dismiss()
                {
                    onDispose = null;
                }

                // ====================================================================
                // IDisposable
                // ====================================================================
                #region IDisposable

                /// <summary>
                ///     Execute the deferred action immediately. Once invoked,
                ///     the action will not trigger again when its scope exits.
                ///     The action cannot be invoked more than once; subsequent
                ///     calls are ignored. If the action was <see
                ///     cref="Dismiss">dismissed</see>, the call does nothing.
                /// </summary>
                public void Dispose()
                {
                    onDispose?.Invoke();

                    Dismiss();
                }

                #endregion  // IDisposable

                // ====================================================================
                // Internal fields
                // ====================================================================

                /// <summary>
                /// The delegate invoked when disposed. This is exposed so users
                /// can append more actions if desired.
                /// </summary>
                public Action onDispose;

            }   // struct DeferredAction

        }   // static class Details

    }   // static class MelcherDefer

}   // namespace Melcher

/// ============================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-07-08
/// ===========================================================================
