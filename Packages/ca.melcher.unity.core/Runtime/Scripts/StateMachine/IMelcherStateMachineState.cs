namespace Melcher
{
    /// <summary>
    ///     The interface for states used by <see cref="MelcherStateMachine"/>.
    ///     <br /><br />
    ///
    ///     States are a useful mechanism to compartmentalize and manage app
    ///     flow. Using a state machine, transitions can will trigger on-enter
    ///     and on-exit logic, which is useful for handling setup and tear
    ///     down.
    ///     <br /><br />
    ///
    ///     A unique feature of <see cref="IMelcherStateMachineState"/> is its
    ///     <see cref="onRequestStateChange"/> event. This event allows the
    ///     state to communicate with its state machine. For example, a state
    ///     can control its own lifetime and "pop" when it is finished or jump
    ///     to other states.
    ///     <br /><br />
    ///
    ///     It is convenient to make a <see cref="MelcherMonoBehaviour"/>
    ///     implement this class. <see cref="MelcherStateMachineState"/> is a
    ///     pre-rolled implementation that does nothing by default, allowing
    ///     users to implement only the functions they deem necessary. The
    ///     MonoBehaviour can implement its one-time, possibly expensive,
    ///     loading logic in its Awake/Start methods, and use the state
    ///     callbacks for actions that need to be repeated.
    ///     <br /><br />
    ///
    ///     The first <see cref="IMelcherStateMachineState"/> found in a
    ///     single-loaded scene will be made the active state in the <see
    ///     cref="MelcherGlobalStateMachine"/>.
    /// </summary>
    public interface IMelcherStateMachineState
    {
        // =====================================================================
        // Events
        // =====================================================================

        delegate void RequestStateChangeHandler(MelcherStateMachineRequest request);

        /// <summary>
        /// This event allows the state to request its state machine change in
        /// some way. Use a factory function from <see cref="MelcherStateMachineRequest"/>
        /// to create a request. E.g., <see cref="MelcherStateMachineRequest.Pop"/>.
        ///
        /// The request must be made from a state that has focus, otherwise it
        /// is ignored. A state has focus when it is the topmost state in the
        /// state machine's stack.
        ///
        /// This event allows states to control their own lifespan and to "pop"
        /// to a previous state without knowing which state that was.
        ///
        /// See <see cref="MelcherStateMachineRequest"/> for more info.
        /// </summary>
        event RequestStateChangeHandler onRequestStateChange;

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Initialize the state. A <see cref="MelcherStateMachine"/> invokes
        /// this function when the state is added to the stack.
        ///
        /// The state machine ensures this callback is paired with an eventual
        /// <see cref="OnStateRemoved"/> call.
        /// </summary>
        void OnStateAdded(IMelcherStateMachineState from);

        /// <summary>
        /// The state became the top of its state machine's stack. This means it
        /// will be the first state to receive events passed to the state
        /// machine.
        ///
        /// This is also a formal notification that the state can make change
        /// requests to the state machine. It is possible to make requests
        /// earlier than this, i.e., in <see cref="OnStateAdded"/>, but this
        /// method is prone to being called more often and thus may appear more
        /// reliable.) See <see cref="onRequestStateChange"/> for more info.
        ///
        /// This callback is useful to implement pausing behaviour. When a state
        /// gains focus, it can re-enable features that were halted in a
        /// previous <see cref="OnStateLoseFocus"/> call.
        ///
        /// The state machine ensures this callback is paired with an eventual
        /// <see cref="OnStateLoseFocus"/> call.
        /// </summary>
        void OnStateGainFocus(IMelcherStateMachineState from);

        /// <summary>
        ///     Inform the state that some event occurred which the state may
        ///     optionally handle. The most common purpose is to handle a
        ///     <see cref="MelcherStateMachineBuiltInEvent.UnityUpdate"/> event
        ///     (or one of its variants: LateUpdate, FixedUpdate).
        ///     <br /><br />
        ///
        ///     The state can choose whether to propagate the event to
        ///     previously pushed states (by returning <see
        ///     cref="MelcherEventChainResult.Continue"/>) or end propagation
        ///     of the event here (by returning <see
        ///     cref="MelcherEventChainResult.Stop"/>). For example, a "pause"
        ///     state may return <see cref="MelcherEventChainResult.Stop"/> to
        ///     prevent its previous state from continuing to tick.
        ///     <br /><br />
        ///
        ///     A <see cref="MelcherGlobalStateMachine"/> will automatically
        ///     make calls to this function for the built-in event types
        ///     (Update, LateUpdate, FixedUpdate) for whichever state it has
        ///     active. When a state is not part of the global state machine,
        ///     (e.g., it is part of a custom state machine in another state or
        ///     some other custom setup), this function must be called
        ///     manually.
        ///     <br /><br />
        ///
        ///     An opaque <see langword="object"/> is used for this function's
        ///     argument because it allows arbitrary types of events to be
        ///     handled without requiring downstream changes for those
        ///     implementing the interface. E.g., adding a new event doesn't
        ///     necessitate all implementers must also add a function to handle
        ///     or ignore it.
        /// </summary>
        /// <returns>
        ///     Return <see cref="MelcherEventChainResult.Continue"/> to allow
        ///     the event to bubble into the previously pushed states;
        ///     otherwise, return <see cref="MelcherEventChainResult.Stop"/> to
        ///     stop the event from propagating.
        /// </returns>
        MelcherEventChainResult OnStateEvent(object eventData);

        /// <summary>
        /// The state is no longer the top of its state machine's stack. This
        /// may be due to another state being pushed or this state being popped.
        ///
        /// The state machine ensures this callback is paired with a previous
        /// <see cref="OnStateGainFocus"/> call.
        /// </summary>
        void OnStateLoseFocus();

        /// <summary>
        /// Perform clean-up in this method. The state is being removed from a
        /// state machine.
        ///
        /// The state machine ensures this callback is paired with a previous
        /// <see cref="OnStateAdded"/> call.
        /// </summary>
        void OnStateRemoved();

    }   // interface IMelcherStateMachineState


    /// <summary>
    ///     An instruction to a state's <see cref="MelcherStateMachine"/>. This
    ///     determines whether the event being handled will propagate into
    ///     previously pushed states (<see cref="Continue"/>) or if the
    ///     event will end here (<see cref="Stop"/>).
    /// </summary>
    public enum MelcherEventChainResult
    {
        /// <summary>
        ///     Stop the event from propagating to the remaining states on the
        ///     stack. E.g., may be used by a "pause" state to prevent the
        ///     previous state from continuing to tick.
        /// </summary>
        Stop,

        /// <summary>
        ///     Propagate the event to the previously pushed state, if it
        ///     exists.
        /// </summary>
        Continue,

    }   // enum MelcherEventChain

}   // namespace Melcher
