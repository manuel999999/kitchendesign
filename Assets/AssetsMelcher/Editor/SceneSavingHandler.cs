namespace Melcher
{
    using MEC;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    /// <summary>
    ///     Intercepts requests to save the scene to perform some cleanup before
    ///     the scene is written to disk.
    ///     <br /><br />
    ///
    ///     Currently, this just ensures that any rogue More Effective Coroutine
    ///     TimingController objects are removed before the scene is saved.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSavingHandler
    {
        static SceneSavingHandler()
        {
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            RemoveMoreEffectiveCoroutineTimingControllers(scene);
        }

        /// <summary>
        ///     Clean up unintended/rogue Timing components in order to prevent
        ///     runtime issues.
        ///     <br /><br />
        ///
        ///     These rogue Timing components are unintentionally created when
        ///     More Effective Coroutine is used at edit-time. These lingering
        ///     objects get saved as part of the scene and then cause issues
        ///     during gameplay, especially when a scene that contains one is
        ///     reloaded.
        /// </summary>
        /// <param name="scene"></param>
        private static void RemoveMoreEffectiveCoroutineTimingControllers(UnityEngine.SceneManagement.Scene scene)
        {
            List<Timing> rogueTimingControllers = new ();

            MelcherSearch.AllFromScene(
                rogueTimingControllers,
                scene,
                includeInactiveComponents: true,
                includeInactiveGameObjects: true
            );

            // Iterate the objects in reverse order. This ensures the system
            // continues working even if the objects are somehow nested
            // within each other.
            for (int i = rogueTimingControllers.Count - 1; i >= 0; --i)
            {
                Timing timing = rogueTimingControllers[i];

                // Does this game object have more components than just a Timing
                // component? (Game objects always have at least 1 Transform
                // component.)
                bool hasNonTimingComponents =
                    timing.gameObject.GetComponentCount() > 2;

                bool hasChildren = timing.gameObject.transform.childCount > 0;

                if (hasNonTimingComponents || hasChildren)
                {
                    Object.DestroyImmediate(timing);
                }
                else
                {
                    Object.DestroyImmediate(timing.gameObject);
                }
            }

            if (rogueTimingControllers.Count > 0)
            {
                MelcherLog.Log($"Removed {rogueTimingControllers.Count} More Effective Coroutine TimingController(s).");
            }
        }
    }   // class SceneSavingHandler

}   // namespace Melcher

