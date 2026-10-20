namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A state machine. This maintains a stack of states. The topmost state
    ///     (the one most recently pushed) is considered the one in focus. A
    ///     state with focus can request changes of the state machine; e.g., to
    ///     push a new state, pop its self, or to switch out the state for a
    ///     different one.
    /// </summary>
    public class MelcherStateMachine
    {
        // ====================================================================
        // Constructors
        // ====================================================================

        /// <summary>
        ///     Create a state machine.
        /// </summary>
        public MelcherStateMachine(string stateMachineName, bool logStateChanges)
        {
            m_StateStack = new Stack<IMelcherStateMachineState>();
            m_PopCallbacks = new Stack<Action>();
            name = stateMachineName;
            m_LogTag = MelcherLog.MakeLogTag(fromObjectHashCode: name, tag: name);
            this.logStateChanges = logStateChanges;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Return <see langword="true"/> if <paramref name="state"/> is the
        ///     topmost state; otherwise, return <see langword="false"/>.
        ///     <br /><br />
        ///
        ///     If the state stack is empty, only a <paramref name="state"/>
        ///     of <see langword="null"/> will return <see langword="true"/>.
        /// </summary>
        public bool IsTopmostState(IMelcherStateMachineState state)
        {
            if (m_StateStack.Count > 0)
            {
                return m_StateStack.Peek() == state;
            }

            return state == null;
        }

        /// <summary>
        ///     Send an event to the state stack. Each state (in the reverse
        ///     order they were added) gets a chance to process the event.
        ///     <br /><br />
        ///
        ///     If a state returns <see cref="MelcherEventChainResult.Stop"/>
        ///     from its <see cref="IMelcherStateMachineState.OnStateEvent(object)"/>
        ///     function, the event will stop being propagated down the state
        ///     stack.
        /// </summary>
        /// <param name="eventData">
        ///     The data to pass to the states. It is arbitrary and can be <see
        ///     langword="null"/> (although that may not be very useful).
        ///     Generally, it is one of the values from <see
        ///     cref="MelcherStateMachineBuiltInEvent"/> to forward a Unity
        ///     message to the state.
        /// </param>
        public void SendEvent(object eventData)
        {
            if (m_StateStack.Count > 0)
            {
                // Iterate the states in the reverse order they were added
                foreach (IMelcherStateMachineState state in m_StateStack)
                {
                    if (state.OnStateEvent(eventData) == MelcherEventChainResult.Stop)
                    {
                        // Stop propagating
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Clear all states. This allows the states to clean up (calls into
        ///     <see cref="IMelcherStateMachineState.OnStateDisabled"/> and <see
        ///     cref="IMelcherStateMachineState.OnStateRemoved"/> as necessary)
        ///     but cancels any pop callbacks.
        /// </summary>
        /// <remarks>
        ///     The pop callbacks are skipped because they are often used to
        ///     push new states onto the stack. This function is intended to
        ///     reset the state stack to be empty. Ignoring the callbacks
        ///     ensures this is true.
        /// </remarks>
        public void Purge()
        {
            SetStackDirty();

            PurgeTopmost(m_StateStack.Count);

            MelcherLog.Assert(m_StateStack.Count <= 0);
            MelcherLog.Assert(m_PopCallbacks.Count <= 0);
        }

        public void PurgeTopmost(int count)
        {
            SetStackDirty();

            if (count > 0)
            {
                IMelcherStateMachineState topmostState = m_StateStack.Peek();
                OnStateLoseFocus(topmostState);

                Stack<string> poppedStates = new Stack<string>();

                for (int i = 0; i < count; ++i)
                {
                    IMelcherStateMachineState popped = m_StateStack.Pop();
                    m_PopCallbacks.Pop();   // Keep synchronized with m_StateStack

                    OnStateRemoved(popped);

                    poppedStates.Push(GetStateName(popped));
                }

                poppedStates.Push(GetStateName(null));

                if (logStateChanges)
                {
                    MelcherLog.Log(
                        string.Join($" <b><color={s_StatePopColor}>{s_ArrowLeftGlyph}</color></b> ", poppedStates) + " (purge)",
                        tag: m_LogTag
                    );
                }
            }
        }

        /// <summary>
        ///     End the active state and resume the one that came before it (if
        ///     it exists).
        /// </summary>
        public void PopState()
        {
            MelcherLog.Assert(m_StateStack.Count > 0);

            int startingDirtiness = SetStackDirty();

            Action popCallback = m_PopCallbacks.Pop();
            IMelcherStateMachineState oldState = m_StateStack.Pop();
            oldState.onRequestStateChange -= ChangeState;
            OnStateLoseFocus(oldState);
            OnStateRemoved(oldState);

            IMelcherStateMachineState newState = m_StateStack.Count > 0 ? m_StateStack.Peek() : null;
            if (logStateChanges)
            {
                MelcherLog.Log(
                    $"<b>{GetStateName(newState)}</b> <b><color={s_StatePopColor}>{s_ArrowLeftGlyph}</color></b> {GetStateName(oldState)} (pop)\n{GetStateStackString()}",
                    tag: m_LogTag
                );
            }

            if (newState != null)
            {
                newState.onRequestStateChange += ChangeState;
                OnStateGainFocus(newState, oldState);
            }

            // Only execute the pop callback if the new state didn't modify the
            // state machine's stack
            if (m_CurrentDirtiness != startingDirtiness)
            {
                if (popCallback != null)
                {
                    MelcherLog.LogWarning($"A pop callback is being ignored because the state stack was modified before {nameof(PopState)} completed.");
                }
                return;
            }

            popCallback?.Invoke();
        }

        /// <summary>
        ///     Pause the previous state (if it exists) and begin a new state.
        /// </summary>
        public void PushState(IMelcherStateMachineState newState, Action popCallback)
        {
            MelcherLog.Assert(newState != null);

            int startingDirtiness = SetStackDirty();

            IMelcherStateMachineState oldState = null;
            if (m_StateStack.Count > 0)
            {
                oldState = m_StateStack.Peek();
                oldState.onRequestStateChange -= ChangeState;
                OnStateLoseFocus(oldState);
            }

            m_StateStack.Push(newState);
            m_PopCallbacks.Push(popCallback);

            if (logStateChanges)
            {
                MelcherLog.Log(
                    $"{GetStateName(oldState)} <b><color=green>{s_ArrowRightGlyph}</color></b> <b>{GetStateName(newState)}</b> (push)\n{GetStateStackString()}",
                    tag: m_LogTag
                );
            }

            newState.onRequestStateChange += ChangeState;
            OnStateAdded(newState, oldState);
            if (m_CurrentDirtiness != startingDirtiness)
            {
                return;
            }

            OnStateGainFocus(newState, oldState);
        }

        /// <summary>
        ///     Stop the current state and start a new one.
        /// </summary>
        public void SwitchState(IMelcherStateMachineState newState)
        {
            MelcherLog.Assert(m_StateStack.Count > 0);

            if (newState == null)
            {
                MelcherLog.LogError($"Cannot switch {name} to a null state. Use pop if you want to remove a state.");

                return;
            }

            int startingDirtiness = SetStackDirty();

            IMelcherStateMachineState oldState = m_StateStack.Pop();
            oldState.onRequestStateChange -= ChangeState;
            OnStateLoseFocus(oldState);
            OnStateRemoved(oldState);

            m_StateStack.Push(newState);

            if (logStateChanges)
            {
                MelcherLog.Log(
                    $"<b><color={s_StatePopColor}>--</color></b>{GetStateName(oldState)}   <b><color=green>++</color>{GetStateName(newState)}</b> (switch)\n{GetStateStackString()}",
                    tag: m_LogTag
                );
            }

            newState.onRequestStateChange += ChangeState;
            OnStateAdded(newState, oldState);
            if (m_CurrentDirtiness != startingDirtiness)
            {
                return;
            }

            OnStateGainFocus(newState, oldState);
        }

        /// <summary>
        ///     Allow the state machine's stack to be iterated.
        /// </summary>
        public IEnumerator<IMelcherStateMachineState> GetEnumerator()
        {
            return m_StateStack.GetEnumerator();
        }

        private static string GetStateName(IMelcherStateMachineState state)
        {
            return state == null ? "<NoState>" : state.GetType().Name;
        }

        protected virtual void OnStateAdded(IMelcherStateMachineState toState, IMelcherStateMachineState fromState)
        {
            toState.OnStateAdded(fromState);
        }

        protected virtual void OnStateGainFocus(IMelcherStateMachineState toState, IMelcherStateMachineState fromState)
        {
            toState.OnStateGainFocus(fromState);
        }

        protected virtual void OnStateLoseFocus(IMelcherStateMachineState state)
        {
            state.OnStateLoseFocus();
        }

        protected virtual void OnStateRemoved(IMelcherStateMachineState state)
        {
            state.OnStateRemoved();
        }

        private void ChangeState(MelcherStateMachineRequest stateChange)
        {
            switch (stateChange.changeType)
            {
                case MelcherStateMachineRequest.ChangeType.Pop:
                    PopState();
                    break;

                case MelcherStateMachineRequest.ChangeType.Push:
                    PushState(stateChange.state, stateChange.popCallback);
                    break;

                case MelcherStateMachineRequest.ChangeType.Switch:
                    SwitchState(stateChange.state);
                    break;

                default:
                    MelcherLog.LogError(
                        $"Unhandled state change type: {stateChange.changeType}",
                        tag: m_LogTag
                    );
                    break;
            }
        }

        /// <summary>
        ///     Mark the state stack as having changed and return a value that
        ///     represents the stack stack's new revision number.
        ///     <br /><br />
        ///
        ///     Elsewhere this class, the return value is be compared to <see
        ///     cref="m_CurrentDirtiness"/> to check if the stack's state
        ///     changed mid-update. Knowing if the state stack changed
        ///     mid-update is important to avoid sending update events to states
        ///     that are no longer valid.
        /// </summary>
        /// <returns>
        ///     Return a value that indicates the state stack's current
        ///     modification number.
        /// </returns>
        private int SetStackDirty()
        {
            // Increment and return a new value.
            return ++m_CurrentDirtiness;
        }

        /// <summary>
        ///     Return a string that contains the name of each state that is
        ///     currently in the stack separated by rightward arrows. The states
        ///     are listed left-to-right in the order they were added.
        /// </summary>
        private string GetStateStackString()
        {
            return
                "New state stack (in the order they were pushed): " +
                string.Join(
                    ", ",
                    m_StateStack.Reverse().Select(x => x.GetType().Name)
                );
        }

        // =====================================================================
        // Public fields
        // =====================================================================

        public readonly string name;

        public bool logStateChanges;

        private readonly string m_LogTag;

        // =====================================================================
        // Internal fields
        // =====================================================================

        /// <summary>
        ///     State stack. Only the most recently added state is updated.
        /// </summary>
        private Stack<IMelcherStateMachineState> m_StateStack;

        /// <summary>
        ///     Stack of callbacks; each entry in the stack is a function that
        ///     is called when the state stack is popped.
        /// </summary>MachineState
        private Stack<Action> m_PopCallbacks;

        /// <summary>
        ///     Incremented every time the state stack is modified. We can check
        ///     this value after calling a state's OnBeginState to ensure we
        ///     don't attempt to call OnStateActive on a state that has been
        ///     invalidated.
        /// </summary>
        private int m_CurrentDirtiness;

        // =====================================================================
        // Static fields
        // =====================================================================

        private const string s_ArrowLeftGlyph = "\u2190";   // <-
        private const string s_ArrowRightGlyph = "\u2192";  // ->

        /// <summary>
        ///     The colour used in the console when a state is popped. It is a
        ///     palish orange. This value was chosen to replace the original
        ///     red colour because red implies errors and some concern was
        ///     raised by developers unfamiliar with the state machine class.
        /// </summary>
        private const string s_StatePopColor = "#E5AC51";


    }   // class MelcherStateMachine

}   // namespace Melcher

