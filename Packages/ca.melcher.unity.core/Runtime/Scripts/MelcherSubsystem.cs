namespace Melcher
{
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif  // if UNITY_EDITOR

    /// <summary>
    /// This is the intended base class for any Melcher subsystem. Subsystems
    /// are persistent game objects: they live independent of any scene and
    /// survive for the whole play session. They are meant to be created via
    /// <see cref="CreateSubsystem"/> within a <see cref="InitializeOnLoadAttribute"/>
    /// or <see cref="RuntimeInitializeLoadType.SubsystemRegistration"/>
    /// function. In the editor, subsystems need to be created and destroyed
    /// between play mode changes.
    ///
    /// An example of one of these subsystems is the <see cref="MelcherLocalizer"/>
    /// class: it allows translation of phrases and for lookup of unicode glyphs
    /// at editor time which is greatly useful when designing the UI.
    /// </summary>
    public abstract class MelcherSubsystem<SubsystemType> : MelcherSubsystemBase
        where SubsystemType : MonoBehaviour
    {
        // =====================================================================
        // Static methods
        // =====================================================================

        /// <summary>
        /// Create a persistent game object with an instance of this script.
        /// </summary>
        public static SubsystemType CreateSubsystem()
        {
            DestroyEditorInstances();

            MelcherLog.Assert(s_Instance == null, "Subsystem instance is not null before create.");

            GameObject subsystemGameObject = null;

            if (MelcherUnityUtility.IsApplicationPlaying())
            {
                // Run-time subsystems use DontDestroyOnLoad to survive scene changes
                subsystemGameObject = new GameObject(typeof(SubsystemType).Name);
                DontDestroyOnLoad(subsystemGameObject);
            }
#if UNITY_EDITOR
            else
            {
                // Editor-time subsystem use HideFlags to survive scene changes
                subsystemGameObject = EditorUtility.CreateGameObjectWithHideFlags(
                    "[EditorInstance]" + typeof(SubsystemType).Name,
                    EditorSubsystemHideFlags,
                    components: System.Array.Empty<System.Type>()
                );
            }
#endif  // if UNITY_EDITOR

            s_Instance = subsystemGameObject.AddComponent<SubsystemType>();

            return s_Instance;
        }

        public static void DestroyEditorInstances()
        {
            MelcherSubsystemBase.DestroyEditorInstances<SubsystemType>();

            s_Instance = null;
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        // This field is not reset here because its state is managed elsewhere
        // by calling DestroyEditorInstances and CreateSubsystem
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        protected static SubsystemType s_Instance;

    }   // class MelcherSubsystem

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-03-13
/// ===========================================================================
