namespace Melcher
{
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif  // if UNITY_EDITOR

    /// <summary>
    /// Handle Melcher Core's startup initialization. This lets us control
    /// the order the subsystems initialize.
    /// </summary>
    internal static class MelcherMain
    {
#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        /// <summary>
        /// This is the first function called during a domain reload.
        /// Adds a listener for play-mode changes to manage the lifetime of
        /// subsystems.
        ///
        /// This function is also sometimes called before play mode is about
        /// to be entered. In this case, we skip calling <see
        /// cref="PlayModeInit"/> directly and wait for it to be invoked
        /// automatically.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void EditorInit()
        {
            EditorApplication.playModeStateChanged -= OnPlaymodeChanged;
            EditorApplication.playModeStateChanged += OnPlaymodeChanged;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            PlayModeInit();
            BeforeSceneInit();
        }

        /// <summary>
        /// Enforce lifecycle of editor-time subsystem. Called when the editor
        /// playmode changes.<br /><br />
        ///
        /// Entering edit-mode: make editor subsystems
        /// <br />
        /// Exiting edit-mode: destroy editor subsystems
        /// </summary>
        private static void OnPlaymodeChanged(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.ExitingEditMode)
            {
                MelcherSubsystemBase.DestroyAllEditorInstances();
            }
            else if (mode == PlayModeStateChange.EnteredEditMode)
            {
                PlayModeInit();
            }
        }
#endif  // if UNITY_EDITOR

        // =====================================================================
        // Runtime
        // =====================================================================

        /// <summary>
        /// This is the second function called during the domain reload
        /// sequence.
        ///
        /// - Manages subsystem initialization order
        /// - Creates other persistent game objects
        /// </summary>
        // TODO: Ideally, we could bake the editor check into
        //       MelcherAfterResetStaticFieldsMethod. -Ryan
#if UNITY_EDITOR
        [MelcherAfterResetStaticFieldsMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void PlayModeInit()
        {
            // Subsystems are always created (editor and runtime)
            MelcherLocalizer.CreateSubsystem();
            MelcherScreenEvents.CreateSubsystem();

            if (!Application.isPlaying)
            {
                return;
            }

            MelcherSceneManager.Init();
            MelcherSafeAreaManager.Init();

        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneInit()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            // Create the instance
            MelcherGlobalStateMachine.GetInstance();
        }

    }   // class MelcherMain

}   // namespace Melcher
