namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// The <see cref="MelcherAnimatorStateEventRaiser"/> can be attached to
    /// animator states in an AnimationController to perform the duty of
    /// forwarding that state's enter and exit events to the animator's attached
    /// <see cref="MelcherAnimatorEventBridge"/>.
    /// </summary>
    // Because this class has no internal state, we can share the instance
    // between all animators.
    [SharedBetweenAnimators]
    public class MelcherAnimatorStateEventRaiser : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent(out MelcherAnimatorEventBridge bridge))
            {
                bridge.RaiseStateEnter(animator, stateInfo, layerIndex);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent(out MelcherAnimatorEventBridge bridge))
            {
                bridge.RaiseStateExit(animator, stateInfo, layerIndex);
            }
        }

    }   // class MelcherAnimatorStateEventRaiser

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2021-11-30
/// ===========================================================================
