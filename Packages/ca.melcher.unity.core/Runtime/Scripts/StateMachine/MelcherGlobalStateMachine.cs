namespace Melcher
{
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    ///     The main purpose of <see cref="MelcherGlobalStateMachine"/> is to
    ///     implement the app's heartbeat. Rather than define <c>Update</c>
    ///     methods in every <see cref="MonoBehaviour"/>, the global state
    ///     machine can be used to direct update messages only to those
    ///     objects that need it.
    ///     <br /><br />
    ///
    ///     The process is is for users to implement custom <see
    ///     cref="IMelcherStateMachineState"/>'s that are aware of any objects
    ///     that will be updated.
    ///     <br /><br />
    ///
    ///     The global state machine searches for the first <see
    ///     cref="IMelcherStateMachineState"/> it can find in a newly loaded
    ///     (non-additive) scene. It propagates Unity's Update messages to this
    ///     state.
    /// </summary>
    [AddComponentMenu("")]  // Hide from Add Component menu
    [DefaultExecutionOrder(-10)] // Update earlier than a normal MonoBehaviour. This ensures the state receives its Update before scene objects.
    public class MelcherGlobalStateMachine
        : MelcherPersistentMonoBehaviour<MelcherGlobalStateMachine>
    {
        public static bool logStateChanges
        {
            get => GetInstance().m_StateMachine.logStateChanges;
            set => GetInstance().m_StateMachine.logStateChanges = value;
        }

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Try to find the first <see cref="IMelcherStateMachineState"/>
        ///     in a scene and push it to the state stack. This is useful for
        ///     using states in scenes that were loaded additively.
        /// </summary>
        /// <param name="scene">
        ///     The scene to search for a <see
        ///     cref="IMelcherStateMachineState"/>.
        /// </param>
        /// <returns>
        ///     <see cref="true"/> if a <see cref="IMelcherStateMachineState"/>
        ///     was found in <paramref name="scene"/> and was added to the
        ///     state stack.
        /// </returns>
        public bool PushFirstStateFromScene(Scene scene)
        {
            IMelcherStateMachineState firstStateInScene =
                MelcherSearch.FirstFromScene<IMelcherStateMachineState>(scene, includeInactiveGameObjects: false);

            if (firstStateInScene != null)
            {
                m_StateMachine.PushState(firstStateInScene, popCallback: null);
            }

            return firstStateInScene != null;
        }

        private void Awake()
        {
            if (Time.frameCount > 0)
            {
                FindFirstStateOnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }
        }

        private void OnEnable()
        {
            MelcherSceneManager.sceneLoaded += FindFirstStateOnSceneLoaded;
            MelcherSceneManager.sceneUnloading += RemoveStatesOnSceneUnloading;
        }

        private void OnDisable()
        {
            MelcherSceneManager.sceneLoaded -= FindFirstStateOnSceneLoaded;
            MelcherSceneManager.sceneUnloading -= RemoveStatesOnSceneUnloading;
        }

        /// <summary>
        /// Propagate Unity's Update event to the state machine.
        /// </summary>
        private void Update()
        {
            m_StateMachine.SendEvent(MelcherStateMachineBuiltInEvent.UnityUpdate);
        }

        /// <summary>
        /// Forward Unity's FixedUpdate event to the state machine.
        /// </summary>
        private void FixedUpdate()
        {
            m_StateMachine.SendEvent(MelcherStateMachineBuiltInEvent.UnityFixedUpdate);
        }

        /// <summary>
        /// Forward Unity's LateUpdate event to the state machine.
        /// </summary>
        private void LateUpdate()
        {
            m_StateMachine.SendEvent(MelcherStateMachineBuiltInEvent.UnityLateUpdate);
        }

        /// <summary>
        /// Cleanup global state machine chain before the app exits.
        /// </summary>
        private void OnApplicationQuit()
        {
            m_StateMachine.Purge();
        }

        /// <summary>
        /// Find and push the first <see cref="IMelcherStateMachineState"/>
        /// found in the newly loaded (non-additive) scene.
        /// </summary>
        private void FindFirstStateOnSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
        {
            if (loadMode == LoadSceneMode.Additive)
            {
                // Ignore scenes that are loaded additively because they do not
                // have chain of command to modify the state machine.
                //
                // Any scene that loads in Single scene mode is scanned for an
                // IMelcherStateMachineState. This state (if found) has chain
                // of command.
                //
                // In order to change the global state machine to a new state,
                // the active state should invoke its onRequestStateChange. If
                // this is too restrictive, the PushFirstStateFromScene() can
                // be used to push a state from anywhere.

                return;
            }

            if (!m_StateMachine.IsTopmostState(null))
            {
                MelcherLog.LogError(
                    $"State machine not empty after full scene change. Purging. " +
                    $"Was {MelcherFormat.ConsoleMethodName("UnityEngine.SceneManagement.SceneManager.LoadScene")} called instead of {MelcherFormat.ConsoleMethodName("Melcher.MelcherSceneManager.LoadScene")}?",
                    tag: s_Tag
                );

                m_StateMachine.Purge();
            }

            PushFirstStateFromScene(loadedScene);
        }

        /// <summary>
        ///     This function is called just before any scene begins unloading.
        ///     If any states in the state machine belongs to the unloading
        ///     scene, those states will be popped. If those states can only
        ///     be popped by pop
        /// </summary>
        private void RemoveStatesOnSceneUnloading(Scene unloadingScene)
        {
            int numberOfStatesToPop = 0;

            bool canWarnAboutMixedStateScenes = false;

            bool didWarnAboutMixedStateScenes = false;

            // Check if any of the active states in the state machine are part
            // of the unloading scene
            int stateNumber = 0;
            foreach (IMelcherStateMachineState state in m_StateMachine)
            {
                // This should be 1 on the first iteration and increase by 1
                // every time after.
                ++stateNumber;

                // Try to cast the state to a MonoBehaviour. (We expect users to
                // make their IMelcherStateMachineState's MonoBehaviours.)
                if (!(state is MonoBehaviour stateAsMonoBehaviour))
                {
                    // Not a MonoBehaviour. This doesn't guarantee the scene
                    // isn't owned by a scene, but it means we have no way to
                    // check that!

                    MelcherLog.LogWarningEditorOnly(
                        $"Cannot determine if {state.GetType().Name} is part of a scene. This is because it " +
                        $"isn't a {nameof(MonoBehaviour)}.",
                        context: state as Object,
                        tag: s_Tag
                    );

                    continue;
                }

                SceneHandle stateSceneHandle =
                    stateAsMonoBehaviour.gameObject.scene.handle;

                bool stateBelongsToUnloadingScene =
                    stateSceneHandle == unloadingScene.handle;

                if (stateBelongsToUnloadingScene)
                {
                    numberOfStatesToPop = stateNumber;

                    if (canWarnAboutMixedStateScenes && !didWarnAboutMixedStateScenes)
                    {
                        MelcherLog.LogWarning(
                            $"The state stack will pop states from scenes that " +
                            $"are still loaded because the scene, " +
                            $"{unloadingScene.name}, is unloading and:\n" +
                            $"    1. One or more of its states are in use, AND\n" +
                            $"    2. One or more of those states pushed a state " +
                            $"from a different scene.\n" +
                            $"In this case, the scene {unloadingScene.name} " +
                            $"pushed {stateAsMonoBehaviour.gameObject.scene.name}\n" +
                            $"This means the unloading scene's states cannot be " +
                            $"popped without also popping states from a " +
                            $"different scene. This breaks the intended " +
                            $"global state machine behaviour. To avoid this " +
                            $"automatic popping of states from other scenes, " +
                            $"ensure that states only push other states that " +
                            $"live in scenes that will be unloaded before the " +
                            $"scene that pushed it is unloaded; or, pop the " +
                            $"states manually before unloading the scene."
                        );

                        didWarnAboutMixedStateScenes = true;
                    }
                }
                else
                {
                    canWarnAboutMixedStateScenes = true;
                }
            }

            if (numberOfStatesToPop >= 0)
            {
                // The scene that is unloading contains at least one state that
                // the state machine is using.
                m_StateMachine.PurgeTopmost(numberOfStatesToPop);
            }
        }

        // TODO Does this break encapsulation?
        public void Purge()
        {
            m_StateMachine.Purge();
        }

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        private MelcherStateMachine m_StateMachine =
            new MelcherStateMachine(nameof(MelcherGlobalStateMachine), logStateChanges: true);

        // =====================================================================
        // Static fields
        // =====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly string s_Tag = MelcherLog.MakeLogTag(Color.yellow);

    }   // class MelcherGlobalStateMachine

}   // namespace Melcher
