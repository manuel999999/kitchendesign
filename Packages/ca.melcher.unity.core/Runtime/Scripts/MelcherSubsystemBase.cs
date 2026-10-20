namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// The non-generic base class for all <see cref="MelcherSubsystem
    /// {SubsystemType}"/>'s. Refer to the generic version for better
    /// documentation on the purpose of subsystems.
    ///
    /// The intent of this class is to provide a type that can be used to
    /// easily filter subsystems of any type and to provide a location for
    /// shared functionality that is best not duplicated into each subsystem
    /// (e.g. events).
    /// </summary>
    public abstract partial class MelcherSubsystemBase : MelcherMonoBehaviour
    {
        /// <summary>
        /// Find and destroy all editor-time instances of /any/ <see
        /// cref="MelcherSubsystemBase"/>. This function ignores non-editor
        /// instances. The intent of this function was to offer a convenient
        /// way to clean up all editor subsystems between playmode state
        /// changes rather than needing to explicitly destroy each subsystem
        /// type.
        /// </summary>
        public static void DestroyAllEditorInstances()
        {
            DestroyEditorInstances<MelcherSubsystemBase>();
        }

        /// <summary>
        /// Find and destroy all subsystems by type.
        /// </summary>
        public static void DestroyEditorInstances<SubsystemType>()
            where SubsystemType : MonoBehaviour
        {
            InternalDestroyEditorInstances<SubsystemType>();
        }

        static partial void InternalDestroyEditorInstances<SubsystemType>()
            where SubsystemType : MonoBehaviour;

    }   // class MelcherSubsystemBase

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-07-14
/// ===========================================================================
