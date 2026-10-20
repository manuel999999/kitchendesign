namespace Melcher
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.SceneManagement;
    using Cysharp.Threading.Tasks;

    /// <summary>
    ///     Handles scene transitions - swapping one scene out for another using
    ///     additive scene loading. Uses Tasks for cleaner code.
    /// </summary>
    public static class MelcherSceneSwitcher
    {
        /// <summary>
        ///     Load a new scene and then unload the previous. This works even
        ///     if only a single scene is currently loaded.
        /// </summary>
        /// <param name="toLoad">
        ///     Requested scene to load.
        /// </param>
        /// <param name="toUnload">
        ///     The currently loaded scene that will be unloaded.
        /// </param>
        /// <returns>
        ///     A task that can be awaited to know when the new scene is loaded
        ///     and the previous scene is unloaded.
        /// </returns>
        public static UniTask<MelcherSceneSwitcherResult> LoadThenUnload(MelcherSceneRequest toLoad, Scene toUnload)
        {
            return LoadThenUnload(toLoad, DisableUnloadingAudioListener, toUnload);
        }

        public static async UniTask<MelcherSceneSwitcherResult> LoadThenUnload(MelcherSceneRequest toLoad, OnSceneLoadedEventHandler onLoaded, Scene toUnload)
        {
            EventSystem currentEventSystem = EventSystem.current;
            if (currentEventSystem != null && currentEventSystem.gameObject.scene.handle == toUnload.handle)
            {
                currentEventSystem.gameObject.SetActive(false);
            }

            await
                MelcherSceneManager.LoadSceneAsync(
                    toLoad,
                    MelcherSceneLoadMode.Additive
                );

            if (!MelcherSceneManager.TryGetScene(toLoad, out Scene loadedScene))
            {
                throw new System.Exception("Failed to load scene: " + toLoad);
            }

            if (onLoaded != null)
            {
                // Provide an opportunity to transfer data from the scene soon
                // to be unloaded to the newly loaded scene.
                await onLoaded.Invoke(loadedScene, toUnload);
            }

            await
                MelcherSceneManager.UnloadSceneAsync(
                    MelcherSceneRequest.FromScene(toUnload)
                );

            return
                new MelcherSceneSwitcherResult(
                    loadedScene: loadedScene,
                    unloadedScene: toUnload
                );
        }

        public static UniTask DisableUnloadingAudioListener(Scene loadedScene, Scene toUnload)
        {
            // Turn off audio listeners in original scene
            List<AudioListener> audioListeners = new List<AudioListener>();
            MelcherSearch.AllFromScene(audioListeners, toUnload);
            foreach (AudioListener audioListener in audioListeners)
            {
                audioListener.enabled = false;
            }

            return UniTask.CompletedTask;
        }

        public static UniTask<MelcherSceneSwitcherResult> UnloadThenLoad(Scene toUnload, MelcherSceneRequest toLoad)
        {
            return UnloadThenLoad(toUnload, onUnloaded: null, toLoad);
        }

        public static async UniTask<MelcherSceneSwitcherResult> UnloadThenLoad(Scene toUnload, OnSceneUnloadedEventHandler onUnloaded, MelcherSceneRequest toLoad)
        {
            await
                MelcherSceneManager.UnloadSceneAsync(
                    MelcherSceneRequest.FromScene(toUnload)
                );

            if (onUnloaded != null)
            {
                await onUnloaded.Invoke(toUnload);
            }

            await
                MelcherSceneManager.LoadSceneAsync(
                    toLoad,
                    loadMode: MelcherSceneLoadMode.Additive
                );

            if (!MelcherSceneManager.TryGetScene(toLoad, out Scene loadedScene))
            {
                throw new System.Exception("Failed to load scene: " + toLoad);
            }

            return
                new MelcherSceneSwitcherResult(
                    loadedScene: loadedScene,
                    unloadedScene: toUnload
                );
        }

        // =====================================================================
        // Nested types
        // =====================================================================

        public delegate UniTask OnSceneLoadedEventHandler(Scene loadedScene, Scene toUnload);

        public delegate UniTask OnSceneUnloadedEventHandler(Scene unloadedScene);


    }   // static class MelcherSceneSwitcher

    public struct MelcherSceneSwitcherResult
    {
        public MelcherSceneSwitcherResult(Scene loadedScene, Scene unloadedScene)
        {
            this.loadedScene = loadedScene;
            this.unloadedScene = unloadedScene;
        }

        public readonly Scene unloadedScene;
        public readonly Scene loadedScene;

    }   // struct MelcherSceneSwitcherResult

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-05-06
/// ===========================================================================
