namespace Melcher
{
    /// <summary>
    ///     Default event arguments used by <see
    ///     cref="MelcherGlobalStateMachine"/> to notify the state machine of a
    ///     Unity update message.
    ///     <br /><br />
    ///
    ///     This setup of using an empty class and creating static instances
    ///     is known as the enum class pattern. It allows users to implement
    ///     their own values.
    /// </summary>
    public class MelcherStateMachineBuiltInEvent
    {
        /// <summary>
        ///     Indicates a <see cref="UnityEngine.MonoBehaviour"/>
        ///     <c>Update</c> event occurred.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        public static readonly MelcherStateMachineBuiltInEvent UnityUpdate =
            new MelcherStateMachineBuiltInEvent();

        /// <summary>
        ///     Indicates a <see cref="UnityEngine.MonoBehaviour"/>
        ///     <c>LateUpdate</c> event occurred.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        public static readonly MelcherStateMachineBuiltInEvent UnityLateUpdate =
            new MelcherStateMachineBuiltInEvent();

        /// <summary>
        ///     Indicates a <see cref="UnityEngine.MonoBehaviour"/>
        ///     <c>FixedUpdate</c> event occurred.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        public static readonly MelcherStateMachineBuiltInEvent UnityFixedUpdate =
            new MelcherStateMachineBuiltInEvent();

    }   // class MelcherStateMachineBuiltInEvent

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-19
/// ===========================================================================
