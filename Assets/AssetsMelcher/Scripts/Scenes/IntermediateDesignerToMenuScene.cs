namespace KitchenDesign
{
    using Melcher;
    using MEC;
    using System;
    using UnityEngine;

    /// <summary>
    ///     Free up as much memory as possible and then load the intended
    ///     menu scene. This was added because we were running out of memory
    ///     on iPhone SE's.
    /// </summary>
    public class IntermediateDesignerToMenuScene : MelcherStateMachineState
    {
        public override void OnStateAdded(IMelcherStateMachineState fromState)
        {
            base.OnStateAdded(fromState);

            Resources.UnloadUnusedAssets();

            GC.Collect();

            GC.WaitForPendingFinalizers();

            Timing.CallDelayed(
                delay: 0.25f,
                static () =>
                {
                    MelcherSceneManager.LoadSceneAsync(
                        MelcherSceneRequest.FromName("MenuScene"),
                        MelcherSceneLoadMode.Single
                    );
                }
            );
        }
    }   // class IntermediateDesignerToMenuScene

}   // namespace KitchenDesign

