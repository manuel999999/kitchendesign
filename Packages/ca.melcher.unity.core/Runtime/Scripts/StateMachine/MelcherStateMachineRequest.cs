namespace Melcher
{
    using System;

    /// <summary>
    /// Represents a request to modify a <see cref="MelcherStateMachine"/>.
    /// E.g., this may be a request to push a new state, pop the current state,
    /// or switch the current state out for another.
    /// <br /><br />
    ///
    /// Requests are intended to be created within a <see
    /// cref="IMelcherStateMachineState"/> and passed as an event argument to
    /// <see cref="IMelcherStateMachineState.onRequestStateChange"/>. The
    /// request will be processed if the state has focus in its state machine.
    /// (A state has focus when it is the topmost state in the state machine's
    /// stack.)
    /// <br /><br />
    ///
    /// This is a struct to keep GC allocation down.
    /// </summary>
    public readonly struct MelcherStateMachineRequest
    {
        // =====================================================================
        // Factory functions
        // =====================================================================

        /// <summary>
        /// Revert to the state machine's previous state.
        /// </summary>
        public static MelcherStateMachineRequest Pop()
        {
            return new MelcherStateMachineRequest(ChangeType.Pop);
        }

        /// <summary>
        /// Push a new state onto the stack. When this new state is eventually
        /// popped, the state that was topmost before it was pushed will become
        /// the topmost state again.
        /// </summary>
        /// <param name="newState">
        /// The new/next state to begin.
        /// </param>
        /// <param name="popCallback">
        /// When this state is eventually popped, this callback will be invoked.
        /// It is optional.
        /// </param>
        public static MelcherStateMachineRequest Push(IMelcherStateMachineState newState, Action popCallback = null)
        {
            return new MelcherStateMachineRequest(ChangeType.Push, newState, popCallback);
        }

        /// <summary>
        /// Switch to a new state without reactivating to the parent state. This
        /// skips delays invoking any pending pop callback when switching to the
        /// new state. Any previous pop callback will be invoked when the the
        /// next state is eventually popped.
        /// </summary>
        /// <param name="newState">
        /// The new/next state to begin.
        /// </param>
        public static MelcherStateMachineRequest Switch(IMelcherStateMachineState newState)
        {
            return new MelcherStateMachineRequest(ChangeType.Switch, newState);
        }

        // =====================================================================
        // Properties
        // =====================================================================

        public ChangeType changeType { get; }
        public IMelcherStateMachineState state { get; }
        public Action popCallback { get; }

        // =====================================================================
        // Constructors
        // =====================================================================

        private MelcherStateMachineRequest(ChangeType changeType, IMelcherStateMachineState state = null, Action popCallback = null)
        {
            this.changeType = changeType;
            this.state = state;
            this.popCallback = popCallback;
        }

        // =====================================================================
        // Nested types
        // =====================================================================

        public enum ChangeType
        {
            /// <summary>
            /// 
            /// </summary>
            Pop,
            Push,
            Switch
        }   // enum Type


    }   // struct MelcherStateMachineRequest

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-19
/// ===========================================================================
