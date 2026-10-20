namespace Melcher
{
    using System.Collections.Generic;
    using UnityEngine;

#if UNITY_EDITOR
    using System.Linq;
    using UnityEditor.Animations;
#endif  // if UNITY_EDITOR

    /// <summary>
    /// The <see cref="MelcherAnimatorEventBridge"/> provides the ability to
    /// detect when ananimation starts or ends. It can also start an animation
    /// and trigger a callback when it finishes.
    /// <br /><br />
    ///
    /// This component manages the <see cref="Animator"/> on the same game
    /// object as itself. It allows users to subscribe to generic animation
    /// state events and to raise custom animation events of their own.
    /// <br /><br />
    ///
    /// Exposed generic animation events:
    /// <br /><br />
    ///
    /// 1. <see cref="onStateEnter">onStateEnter</see> - Called whenever an
    ///    animation state begins.
    /// <br /><br />
    ///
    /// 2. <see cref="onStateExit">onStateExit</see> - Called whenever an
    ///    animation state exits.
    /// <br /><br />
    ///
    /// 3. <see cref="onStateLoopPointReached">onStateLoopPointReached</see> -
    ///    Called whenever an animation state reaches the end of its clip. If
    ///    the clip is playing forwards, this will be on the last frame of the
    ///    clip. If the clip is playing backwards, this will be the 0th frame of
    ///    the clip.
    /// <br /><br />
    ///
    /// The complementing StateMachineBehaviour, <see
    /// cref="MelcherAnimatorStateEventRaiser"/>, forwards the animation states'
    /// start and end events to this object. It is necessary to attach a <see
    /// cref="AnimatorStateEventRaiser"/> state behaviour to all animation
    /// states. Fortunately, this is easily done with the inspector buttons.
    /// </summary>
    public class MelcherAnimatorEventBridge : MelcherMonoBehaviour
    {
        // ====================================================================
        // Events
        // ====================================================================

        public delegate void AnimatorStateHandler(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);

        /// <summary>
        /// This event triggers whenever an animation state (with an attached
        /// <see cref="AnimatorStateEventRaiser"/> behaviour) is entered.
        /// </summary>
        public event AnimatorStateHandler onStateEnter;

        /// <summary>
        /// This event triggers whenever an animation state (with an attached
        /// <see cref="AnimatorStateEventRaiser"/> behaviour) exits.
        /// </summary>
        public event AnimatorStateHandler onStateExit;

        public delegate void AnimatorEventHandler(Animator animator, AnimationEvent arg);

        /// <summary>
        /// This event triggers at the end of an animation clip and works for
        /// both forward and reverse playback.
        /// </summary>
        public event AnimatorEventHandler onStateLoopPointReached;

        /// <summary>
        /// This event triggers when the <see cref="RaiseEvent(AnimationEvent)"/>
        /// is called. <see cref="RaiseEvent(AnimationEvent)"/> may be used as
        /// an animation event callback function to minimize coupling between
        /// the animation clip and the scripts that respond to its events.
        /// </summary>
        public event AnimatorEventHandler onAnimationEvent;

        // ====================================================================
        // Properties
        // ====================================================================

        public Animator animator => m_Animator;

        // ====================================================================
        // Methods
        // ====================================================================

        public void Play(
            int stateNameHash,
            int layer = 0,
            float startNextStateAtNormalizedTime = 0.0f,
            AnimatorEventHandler onComplete = null,
            AnimatorStateHandler onKill = null)
        {
            m_Animator.Play(stateNameHash, layer, startNextStateAtNormalizedTime);
            if (onComplete != null || onKill != null)
            {
                m_LoopPointCallbacks.Add(new AnimatorLoopPointCallback(stateNameHash, layer, onComplete, onKill));
            }
        }

        public void PlayInFixedTime(
            int stateNameHash,
            int layer = 0,
            float startNextStateAtFixedTime = 0.0f,
            AnimatorEventHandler onComplete = null,
            AnimatorStateHandler onKill = null)
        {
            m_Animator.PlayInFixedTime(stateNameHash, layer, startNextStateAtFixedTime);
            if (onComplete != null || onKill != null)
            {
                m_LoopPointCallbacks.Add(new AnimatorLoopPointCallback(stateNameHash, layer, onComplete, onKill));
            }
        }

        public void CrossFade(
            int stateNameHash,
            float normalizedTransitionDuration,
            int layer = 0,
            float normalizedTimeOffset = 0.0f,
            float normalizedTransitionTime = 0.0f,
            AnimatorEventHandler onComplete = null,
            AnimatorStateHandler onKill = null)
        {
            m_Animator.CrossFade(
                stateNameHash,
                normalizedTransitionDuration,
                layer,
                normalizedTimeOffset,
                normalizedTransitionTime
            );
            if (onComplete != null || onKill != null)
            {
                m_LoopPointCallbacks.Add(new AnimatorLoopPointCallback(stateNameHash, layer, onComplete, onKill));
            }
        }

        public void CrossFadeInFixedTime(
            int stateNameHash,
            float fixedTransitionDuration,
            int layer = 0,
            float fixedTimeOffset = 0.0f,
            float normalizedTransitionTime = 0.0f,
            AnimatorEventHandler onComplete = null,
            AnimatorStateHandler onKill = null)
        {
            m_Animator.CrossFadeInFixedTime(
                stateNameHash,
                fixedTransitionDuration,
                layer,
                fixedTimeOffset,
                normalizedTransitionTime
            );
            if (onComplete != null || onKill != null)
            {
                m_LoopPointCallbacks.Add(new AnimatorLoopPointCallback(stateNameHash, layer, onComplete, onKill));
            }
        }

        // =====================================================================
        // Methods for AnimationStateEventRaiser
        // =====================================================================

        /// <summary>
        /// Called by <see cref="AnimatorStateEventRaiser"/> to notify us when
        /// a new state has started.
        /// </summary>
        public void RaiseStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            onStateEnter?.Invoke(animator, stateInfo, layerIndex);
        }

        /// <summary>
        /// Called by <see cref="AnimatorStateEventRaiser"/> to notify us when
        /// a state has ended.
        /// </summary>
        public void RaiseStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Clear any pending callbacks that are directly associated
            // with this event / state.
            for (int i = m_LoopPointCallbacks.Count - 1; i >= 0; --i)
            {
                bool isMatch = m_LoopPointCallbacks[i].IsMatch(stateInfo.shortNameHash, layerIndex);

                if (!isMatch && animator.IsInTransition(layerIndex))
                {
                    // Check if the state that would have been started was us
                    stateInfo = animator.GetNextAnimatorStateInfo(layerIndex);
                    isMatch = m_LoopPointCallbacks[i].IsMatch(stateInfo.shortNameHash, layerIndex);
                }

                if (isMatch)
                {
                    m_LoopPointCallbacks[i].onKill?.Invoke(m_Animator, stateInfo, layerIndex);
                    m_LoopPointCallbacks.RemoveAt(i);
                }
            }

            onStateExit?.Invoke(animator, stateInfo, layerIndex);
        }

        /// <summary>
        /// Called by animation clips. Serves as a generic animation event
        /// callback function; i.e., Animator SendMessage()'s to RaiseEvent,
        /// which in turn invokes <see cref="onAnimationEvent"/> for any
        /// listeners (listener doesn't have to be a script attached to the
        /// animator).
        /// </summary>
        public void RaiseEvent(AnimationEvent e)
        {
            onAnimationEvent?.Invoke(animator, e);
        }

        // =====================================================================
        // Internal methods
        // =====================================================================

        private void Start()
        {
            // Add runtime animation events to the start and end of each
            // animation clip. These events detect when a state loops or
            // completes. Depending on the state's playback speed (positive or
            // negative), one or the other animation event will emit the
            // onStateLoopPointReached event

            AnimationClip[] clips =
                m_Animator.runtimeAnimatorController.animationClips;

            // AnimationClip.AddEvent() clones the event passed to it. This
            // allows us to get away with allocating one AnimationEvent and
            // reusing it to populate all the animation clips.
            var loopPointEvent = new AnimationEvent();

            // Loop point event handlers are optional. Ensure that errors are
            // logged to the console if no one handles the event.
            loopPointEvent.messageOptions = SendMessageOptions.DontRequireReceiver;

            foreach (AnimationClip clip in clips)
            {
                AnimationEvent[] clipEvents = clip.events;

                // Add loop point event for backwards play (negative clip speed)
                loopPointEvent.functionName = nameof(OnLoopPointReached_Backward);
                loopPointEvent.time = 0.0f;
                AddAnimationEventIfUnique(clip, clipEvents, loopPointEvent);

                // Add loop point event for normal play (positive clip speed)
                loopPointEvent.functionName = nameof(OnLoopPointReached_Forward);
                loopPointEvent.time = clip.length;
                AddAnimationEventIfUnique(clip, clipEvents, loopPointEvent);
            }

            void AddAnimationEventIfUnique(AnimationClip animationClip, AnimationEvent[] existingAnimationEvents, AnimationEvent newAnimationEvent)
            {
                // Skip adding the event if it was already added
                if (!MelcherCommon.CollectionContainsMatchingField(existingAnimationEvents, newAnimationEvent.functionName, evt => evt.functionName))
                {
                    // Use AnimationClip.AddEvent() for better performance. The
                    // event collection type used inside AnimationClip may be a
                    // type that is faster to append to than our event array.
                    animationClip.AddEvent(newAnimationEvent);
                }
            }
        }

        /// <summary>
        /// This is called when an animation clip is at absolute normalized time
        /// 0. It triggers the loop-point event if the animation is playing
        /// backwards (speed < 0).
        /// </summary>
        private void OnLoopPointReached_Backward(AnimationEvent e)
        {
            if (e.animatorStateInfo.speed < 0.0f)
            {
                EmitLoopPointEvent(e);
            }
        }

        /// <summary>
        /// This is called when an animation clip is at absolute normalized time
        /// 1. It triggers the loop-point event if the animation is playing
        /// forwards (speed > 0).
        /// </summary>
        private void OnLoopPointReached_Forward(AnimationEvent e)
        {
            if (e.animatorStateInfo.speed > 0.0f)
            {
                EmitLoopPointEvent(e);
            }
        }

        /// <summary>
        /// Trigger any user-pending loop-point callbacks (which there may be
        /// none) and then trigger the generic <see cref="onStateLoopPointReached"/>
        /// event.
        /// </summary>
        /// <param name="e"></param>
        private void EmitLoopPointEvent(AnimationEvent e)
        {
            int layerIndex = FindLayerForAnimatorState(m_Animator, e.animatorStateInfo.shortNameHash);

            // Check for any pending callbacks that are directly associated
            // with this event / state
            for (int i = m_LoopPointCallbacks.Count - 1; i >= 0; --i)
            {
                if (m_LoopPointCallbacks[i].IsMatch(e.animatorStateInfo.shortNameHash, layerIndex))
                {
                    // Invoke the callbacks and remove it
                    m_LoopPointCallbacks[i].onComplete?.Invoke(m_Animator, e);
                    m_LoopPointCallbacks[i].onKill?.Invoke(m_Animator, e.animatorStateInfo, layerIndex);
                    m_LoopPointCallbacks.RemoveAt(i);
                }
            }

            // Generic loop-point reached trigger
            onStateLoopPointReached?.Invoke(m_Animator, e);
        }

        // =====================================================================
        // Editor-only
        // =====================================================================
#if UNITY_EDITOR

        // TODO: Add two inspector buttons:
        //          1.) Add Event Raisers to all States
        //          2.) Remove Event Raisers from all States
        //       Call this function with true for 1 and false for 2
        public void AddOrRemoveStateRaisers(bool willAddState)
        {
            if (animator && animator.runtimeAnimatorController)
            {
                string controllerAssetPath =
                    UnityEditor.AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);

                // Ensure the user intends to modify *all* states in the animator
                // controller.

                string promptMessage =
                    $"Do you want to add/remove {UnityEditor.ObjectNames.NicifyVariableName(nameof(MelcherAnimatorStateEventRaiser))}s " +
                    $"to/from *all* states in the active animator controller?\n" +
                    $"\n" +
                    $"Active animator controller: {controllerAssetPath}";

                bool hasUserPermission =
                    UnityEditor.EditorUtility.DisplayDialog(
                        title: "Bulk Animator Controller State Change",
                        message: promptMessage,
                        ok: (willAddState ? "Add To All States" : "Remove From All States")
                    );

                if (!hasUserPermission)
                {
                    return;
                }

                AnimatorController animatorController =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerAssetPath);

                int numberOfBehavioursAddedOrRemoved = 0;

                // Iterate over animation layers to gain access to the states in those layers
                for (int layerIndex = 0; layerIndex < animatorController.layers.Length; ++layerIndex)
                {
                    AnimatorControllerLayer layer = animatorController.layers[layerIndex];

                    // Iterate over the states of the layer
                    for (int stateIndex = 0; stateIndex < layer.stateMachine.states.Length; ++stateIndex)
                    {
                        ChildAnimatorState state = layer.stateMachine.states[stateIndex];

                        // Check if the state already has the state machine component
                        MelcherAnimatorStateEventRaiser existingState =
                            state.state.behaviours.OfType<MelcherAnimatorStateEventRaiser>().FirstOrDefault();
                        bool hasState = existingState != null;

                        if (hasState != willAddState)
                        {
                            if (willAddState)
                            {
                                // Doesn't have the state. Let's add it
                                state.state.AddStateMachineBehaviour<MelcherAnimatorStateEventRaiser>();
                                layer.stateMachine.states[stateIndex] = state;
                            }
                            else
                            {
                                Destroy(existingState);
                                existingState = null;
                            }

                            ++numberOfBehavioursAddedOrRemoved;
                        }
                    }
                }

                if (numberOfBehavioursAddedOrRemoved > 0)
                {
                    UnityEditor.EditorUtility.SetDirty(animatorController);
                }

                MelcherLog.Log(
                    $"{(willAddState ? "Added" : "Removed")} {numberOfBehavioursAddedOrRemoved} state behaviour(s).",
                    context: animator.runtimeAnimatorController
                );
            }
        }

#endif  // if UNITY_EDITOR

        // =====================================================================
        // Static functions
        // =====================================================================

        public int FindLayerForAnimatorState(Animator animator, int stateShortNameHash)
        {
            int layerCount = animator.layerCount;

            for (int layerIndex = 0; layerIndex < layerCount; ++layerIndex)
            {
                if (stateShortNameHash == animator.GetCurrentAnimatorStateInfo(layerIndex).shortNameHash)
                {
                    return layerIndex;
                }
                else if (animator.IsInTransition(layerIndex) &&
                    stateShortNameHash == animator.GetNextAnimatorStateInfo(layerIndex).shortNameHash)
                {
                    return layerIndex;
                }
            }

            return -1;
        }
 

        // ====================================================================
        // OnValidate-serialized members
        // ====================================================================

        /// <summary>
        /// The animator this event bridge manages. This must not be <see
        /// langword="null"/>.
        /// </summary>
        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        private Animator m_Animator = null;

        // ====================================================================
        // Internal members
        // ====================================================================

        private List<AnimatorLoopPointCallback> m_LoopPointCallbacks =
            new List<AnimatorLoopPointCallback>();

        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        /// Callback info for an animator state that is currently playing.
        /// </summary>
        private readonly struct AnimatorLoopPointCallback
        {
            public readonly int stateShortNameHash;
            public readonly int layer;

            /// <summary>
            /// This event is triggered when the animator state reaches its
            /// loop-point.
            /// </summary>
            public readonly AnimatorEventHandler onComplete;

            /// <summary>
            /// This event is triggered immediately after <see
            /// cref="onComplete"/> or if the animator state is canceled
            /// prematurely.
            /// </summary>
            public readonly AnimatorStateHandler onKill;

            public AnimatorLoopPointCallback(int stateNameHash, int layer, AnimatorEventHandler onComplete, AnimatorStateHandler onKill)
            {
                this.stateShortNameHash = stateNameHash;
                this.layer = layer;
                this.onComplete = onComplete;
                this.onKill = onKill;
            }

            /// <summary>
            /// Check if this callback is configured for the state identified
            /// by <paramref name="stateShortNameHash"/> and <paramref name="layerIndex"/>.
            /// </summary>
            /// <param name="stateShortNameHash"></param>
            /// <param name="layerIndex"></param>
            /// <returns></returns>
            public bool IsMatch(int stateShortNameHash, int layerIndex)
            {
                return this.stateShortNameHash == stateShortNameHash &&
                    this.layer == layerIndex;
            }
        }

    }   // class MelcherAnimatorEventBridge

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2021-11-30
/// ===========================================================================
