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
    public class MemoryFlushControl : MelcherMonoBehaviour
    {
        public void Initialize()
        {
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
    }   // class MemoryFlushControl

}   // namespace KitchenDesign

