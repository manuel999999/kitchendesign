namespace Melcher
{
    /// <summary>
    ///     A <see cref="MelcherMonoBehaviour"/>-based implementation of state
    ///     machine states.
    ///
    ///     It is more convenient to derive from this class than implement a
    ///     <see cref="IMelcherStateMachineState"/> directly because, by
    ///     extending this class, users do not need to provide explicit
    ///     implementations of every interface function. Rather, they can
    ///     simply override the interface methods they need and rely on the
    ///     empty defaults for the rest.
    ///     <br /><br />
    ///
    ///     To work around the fact that <see cref="onRequestStateChange"/> is
    ///     inaccessible to deriving classes, the protected function, <see
    ///     cref="InvokeStateChange"/>, serves as a bridge.
    /// </summary>
    public abstract class MelcherStateMachineState
        : MelcherMonoBehaviour
        , IMelcherStateMachineState
    {
        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Provides access to the <see cref="onRequestStateChange"/> event
        ///     so derived classes can invoke it.
        /// </summary>
        protected void InvokeStateChange(MelcherStateMachineRequest stateRequest)
        {
            onRequestStateChange.Invoke(stateRequest);
        }

        // =====================================================================
        // IMelcherStateMachineState
        // =====================================================================
        #region IMelcherStateMachineState

        public event IMelcherStateMachineState.RequestStateChangeHandler onRequestStateChange;

        public virtual void OnStateAdded(IMelcherStateMachineState fromState)
        {
            // Does nothing by default
        }

        public virtual void OnStateGainFocus(IMelcherStateMachineState fromState)
        {
            // Does nothing by default
        }

        public virtual MelcherEventChainResult OnStateEvent(object eventData)
        {
            // Does not propagate into any stacked states by default.
            return MelcherEventChainResult.Stop;
        }

        public virtual void OnStateLoseFocus()
        {
            // Does nothing by default
        }

        public virtual void OnStateRemoved()
        {
            // Does nothing by default
        }

        #endregion  // IMelcherStateMachineState

    }   // class MelcherStateMachineState

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-03-05
/// ===========================================================================
