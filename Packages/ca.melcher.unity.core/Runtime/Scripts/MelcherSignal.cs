namespace Melcher
{
    using System;
    using UnityEngine;

    /// <summary>
    ///     An implementation of the "ScriptableObject Events" pattern commonly
    ///     used in Unity game architecture. A <see cref="MelcherSignal"/> can
    ///     be used to loosely connect different systems.
    ///     <br /><br />
    ///
    ///     Signals are created from the Create Asset menu. These instances can
    ///     be referenced by script. Scripts that want to trigger the event
    ///     will call the Signal's <see cref="Signal(object)"/> method.
    ///     Listeners will subscribe to it's <see cref="onSignaled"/> event.
    ///     <br /><br />
    ///
    ///     The main difference with this implementation over canonical
    ///     scriptable-events is the use of <see cref="System.Object"/> to
    ///     agnostically pass event arguments. This eliminates the need for a
    ///     plethora of prototype classes that would otherwise be required to
    ///     declare Signals of unique signatures. It also means any Signal
    ///     asset can be slotted into any Signal field, making it easy to
    ///     respond to an event without caring about the details.
    ///     <br /><br />
    ///
    ///     Signal raisers implicitly cast their arguments to <see
    ///     cref="Object"/> (boxing occurs for value types). Signal handlers
    ///     manually cast the <see cref="Object"/> argument back to the correct
    ///     type.
    ///     <br /><br />
    ///
    ///     This design loses the type-safety of regular events. To mitigate
    ///     this, the <see cref="MelcherSignal"/> class contains the expected
    ///     argument type as a <see cref="string"/> and performs its own type
    ///     checks inside the <see cref="Signal(object)"/> function (using
    ///     reflection) but only when running the game in the editor or in a
    ///     development build.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSignal", menuName = "Melcher Studios/Signal")]
    public partial class MelcherSignal : MelcherScriptableObject
    {
        // ====================================================================
        // Events
        // ====================================================================

        /// <summary>
        ///     The event that is raised when <see cref="Signal(object)"/> is
        ///     called. Listeners must manually cast the argument to the
        ///     correct type.
        /// </summary>
        public event Action<MelcherSignal, object> onSignaled;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Raise the event to all listeners.
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Signal(object data)
        {
            if (data == null && !m_AllowNull)
            {
                throw new ArgumentException($"MelcherSignal '{name}' does not allow null data.");
            }

            AssertDataTypeSafety(data);

            // Notify all listeners
            onSignaled?.Invoke(this, data);
        }

        partial void AssertDataTypeSafety(object data);

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Tooltip("When disabled, the signal will raise an exception if the " +
            "signal's event data is null.")]
        [SerializeField]
        private bool m_AllowNull = true;

// Supposedly, serialized fields can be used for editor-only data starting in Unity 2021.2. Nice!
// https://forum.unity.com/threads/serialize-fields-only-in-editor.433422/#post-8067116
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("Editor-only")]

        [Tooltip("The full namespace-resolved type name of the intended " +
            "signal argument.")]
        [SerializeField]
        private string m_QualifiedArgumentTypeName = "";

#endif  // if UNITY_EDITOR

        // ====================================================================
        // Static fields
        // ====================================================================

        public const string AllowNullMemberName = nameof(m_AllowNull);

#if UNITY_EDITOR
        public const string QualifiedArgumentTypeNameMemberName = nameof(m_QualifiedArgumentTypeName);
#endif  // if UNITY_EDITOR

    }   // class MelcherSignal

}   // namespace Melcher
