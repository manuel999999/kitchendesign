namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.SceneManagement;
    using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
    using UnityEditor;
#endif  // if UNITY_EDITOR

    /// <summary>
    /// Wraps Unity's default <see cref="SceneManager"/> to provide extra
    /// functionality.
    ///
    /// One new feature is the <see cref="sceneUnloading"/> event. This event
    /// is called immediately when a scene unload is requested and before the
    /// scene begins unloading.
    ///
    /// This class also uses a flexible <see cref="MelcherSceneRequest"/> type
    /// to describe scenes in a unified way (build index, name, path, handle,
    /// etc.).
    /// </summary>
    public static class MelcherSceneManager
    {
        // ====================================================================
        // Events
        // ====================================================================

        /// <summary>
        /// Called immediately before a scene begins to unload.
        /// </summary>
        public static event UnityAction<Scene> sceneUnloading;

        /// <summary>
        /// Called immediately when a scene begins to load.
        /// </summary>
        public static event UnityAction<MelcherSceneRequest, LoadSceneParameters> sceneLoading;

        /// <summary>
        /// Called when a scene has finished loading. This event is meant to
        /// replace <see cref="SceneManager.sceneLoaded"/>
        /// </summary>
        public static event UnityAction<Scene, LoadSceneMode> sceneLoaded;

        /// <summary>
        /// Called when a scene has finished unloading. This event is meant to
        /// replace <see cref="SceneManager.sceneUnloaded"/>
        /// </summary>
        public static event UnityAction<Scene> sceneUnloaded;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Count the number of scenes that have finished loading. This
        ///     differs from <see cref="SceneManager.sceneCount"/> because
        ///     it excludes scenes that are still loading.
        /// </summary>
        public static int GetLoadedSceneCount()
        {
            int loaded = 0;
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; ++i)
            {
                if (SceneManager.GetSceneAt(i).isLoaded)
                {
                    ++loaded;
                }
            }
            return loaded;
        }

        /// <summary>
        ///     Return <see langword="true"/> if any scene is currently
        ///     loading.
        /// </summary>
        public static bool IsAnySceneLoading()
        {
            MelcherSceneEnumerator enumerator =
                new MelcherSceneEnumerator(
                    includeNonLoaded: true,
                    order: MelcherSceneEnumerator.Ordering.NewestToOldest
                );

            while (enumerator.MoveNext())
            {
                if (!enumerator.Current.isLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsAnySceneUnloading()
        {
            return s_UnloadingSceneCount > 0 || s_ScenesQueuedForUnloading.Count > 0;
        }

        /// <summary>
        /// Try to lookup a scene that is loading or has finished loading.
        /// </summary>
        /// <returns>
        /// Returns whether the found scene is valid (<see cref="Scene.IsValid"/>).
        /// </returns>
        public static bool TryGetScene(MelcherSceneRequest request, out Scene scene, MelcherSceneEnumerator.Ordering sceneOrder = MelcherSceneEnumerator.Ordering.OldestToNewest)
        {
            Func<MelcherSceneRequest, Scene, bool> sceneComparer;

            switch (request.mode)
            {
                case MelcherSceneRequest.Mode.BuildIndex:
                    sceneComparer =
                        (request, otherScene) =>
                            request.intValue == otherScene.buildIndex;
                    break;

                case MelcherSceneRequest.Mode.Name:
                    sceneComparer =
                        (request, otherScene) =>
                            request.stringValue == otherScene.name;
                    break;

                case MelcherSceneRequest.Mode.Path:

                    string qualifiedScenePath = request.stringValue;

                    if (!qualifiedScenePath.EndsWith(".unity"))
                    {
                        // Make a path scene request that is prefixed with "Assets/"
                        // and contains a ".unity" file extension. This is necessary
                        // because `SceneManager.GetSceneAt` returns Scenes whose
                        // path matches this format.
                        qualifiedScenePath = System.IO.Path.Combine("Assets", request.stringValue + ".unity");
                    }

                    request = MelcherSceneRequest.FromPath(qualifiedScenePath);

                    sceneComparer =
                        (request, otherScene) =>
                            request.stringValue == otherScene.path;

                    break;

                case MelcherSceneRequest.Mode.Handle:
                    sceneComparer =
                        (request, otherScene) =>
                            request.sceneHandle == otherScene.handle;
                    break;

                default:
                    throw new ArgumentException($"Unsupported {nameof(MelcherSceneRequest)}.{nameof(MelcherSceneRequest.Mode)}: {request.mode}", nameof(request));
            }

            // Default the output argument to an invalid scene
            scene = default;

            // Iterate over all scenes (loaded and otherwise) to find the
            // requested scene
            MelcherSceneEnumerator sceneEnumerator =
                new MelcherSceneEnumerator(includeNonLoaded: true, sceneOrder);

            while (sceneEnumerator.MoveNext())
            {
                if (sceneComparer.Invoke(request, sceneEnumerator.Current))
                {
                    scene = sceneEnumerator.Current;
                    break;
                }
            }

            return scene.IsValid();
        }

        public static Scene CreateScene(string sceneName, LocalPhysicsMode physicsMode = LocalPhysicsMode.None)
        {
            MelcherSceneRequest request = MelcherSceneRequest.FromName(sceneName);

            LoadSceneParameters loadSceneParameters =
                new LoadSceneParameters(
                    LoadSceneMode.Additive,
                    physicsMode
                );

            // Notify pre-load listeners before the scene is created
            sceneLoading?.InvokeAndIgnoreExceptions(request, loadSceneParameters);

            Scene createdScene =
                SceneManager.CreateScene(
                    sceneName,
                    new CreateSceneParameters(physicsMode)
                );

            if (!createdScene.isLoaded)
            {
                MelcherLog.LogError($"Failed to create scene: {sceneName}", tag: s_Tag);
                return default;
            }

            MelcherLog.Log($"Created scene: {sceneName}", tag: s_Tag);

            s_MelcherLoadingSceneHandles.Add(createdScene.handle);

            // Notify post-load listeners after the scene is created
            sceneLoaded?.InvokeAndIgnoreExceptions(createdScene, loadSceneParameters.loadSceneMode);

            return createdScene;
        }

        /// <summary>
        /// Load a scene.
        /// </summary>
        public static AsyncOperation LoadSceneAsync(
            MelcherSceneRequest                 request,
            MelcherSceneLoadMode                loadMode,
            LocalPhysicsMode                    localPhysicsMode = LocalPhysicsMode.None,
            UnityAction<Scene, LoadSceneMode>   onLoaded = null)
        {
            AsyncOperation loadOperation = null;

            if (s_CurrentSingleSceneRequest.HasValue)
            {
                throw new System.InvalidOperationException(
                    $"A scene is already loading using the Single load mode " +
                    $"({s_CurrentSingleSceneRequest}). Please wait for it to " +
                    $"finish before loading another scene ({request})"
                );
            }

            if (loadMode == MelcherSceneLoadMode.Include && TryGetScene(request, out Scene existingScene))
            {
                if (onLoaded != null)
                {
                    if (existingScene.isLoaded)
                    {
                        // The scene is already loaded. Execute the callback
                        // immediately
                        onLoaded.Invoke(existingScene, LoadSceneMode.Additive);
                    }
                    else
                    {
                        // The scene is still loading. Just register our intent
                        // to respond when it finishes loading
                        CreateTemporarySceneLoadedListener(existingScene.handle, onLoaded);
                    }
                }

                // TODO: Should we return an AsyncOperation whose isDone is
                // always true?
                return loadOperation;
            }

            LoadSceneParameters loadSceneParameters =
                new LoadSceneParameters(
                    mode: LoadSceneMode.Additive,
                    physicsMode: localPhysicsMode
                );

            if (loadMode == MelcherSceneLoadMode.Single)
            {
                loadSceneParameters.loadSceneMode = LoadSceneMode.Single;

                // This value acts like a mutex. This prevents a secondary
                // Single-load scene happening while the scenes are still
                // setting up to unload from the first call
                s_CurrentSingleSceneRequest = request;

                try
                {
                    // Send unload messages to all loaded scenes in the reverse
                    // order that they were loaded
                    MelcherSceneEnumerator loadedSceneEnumerator =
                        new MelcherSceneEnumerator(order: MelcherSceneEnumerator.Ordering.NewestToOldest);

                    while (loadedSceneEnumerator.MoveNext())
                    {
                        Scene unloadingScene = loadedSceneEnumerator.Current;
                        s_MelcherUnloadingSceneHandles.Add(unloadingScene.handle);
                        sceneUnloading?.InvokeAndIgnoreExceptions(unloadingScene);
                    }
                }
                finally
                {
                    s_CurrentSingleSceneRequest = null;
                }
            }

            sceneLoading?.InvokeAndIgnoreExceptions(request, loadSceneParameters);

            switch (request.mode)
            {
                case MelcherSceneRequest.Mode.BuildIndex:
                    loadOperation =
                        SceneManager.LoadSceneAsync(
                            sceneBuildIndex: request.intValue,
                            loadSceneParameters
                        );
                    break;

                case MelcherSceneRequest.Mode.Name:
                case MelcherSceneRequest.Mode.Path:
                    loadOperation =
                        SceneManager.LoadSceneAsync(
                            sceneName: request.stringValue,
                            loadSceneParameters
                        );
                    break;

                default:
                    throw new System.ArgumentException($"Unsupported scene request mode: {request.mode}", nameof(request));
            }

            if (loadOperation != null)
            {
                MelcherLog.Log("Loading scene: " + request, tag: s_Tag);

                if (TryGetScene(request, out Scene loadingScene, sceneOrder: MelcherSceneEnumerator.Ordering.NewestToOldest))
                {
                    s_MelcherLoadingSceneHandles.Add(loadingScene.handle);

                    // If a callback is provided, add a temporary sceneLoaded
                    // listener to execute the callback and automatically remove
                    // itself.
                    if (onLoaded != null)
                    {
                        CreateTemporarySceneLoadedListener(loadingScene.handle, onLoaded);
                    }
                }
                else
                {
                    MelcherLog.LogError("Failed to find scene for loading scene", tag: s_Tag);
                }
            }
            else
            {
                MelcherLog.LogError("Failed to load scene " + request, tag: s_Tag);
            }

            return loadOperation;
        }

        public static AsyncOperation UnloadSceneAsync(MelcherSceneRequest request, UnityAction<Scene> onUnloaded = null)
        {
            if (!TryGetScene(request, out Scene sceneToUnload))
            {
                MelcherLog.LogError(
                    $"Can't unload the scene because it's invalid: {request}",
                    tag: s_Tag
                );
                return null;
            }

            if (!sceneToUnload.isLoaded)
            {
                MelcherLog.LogError(
                    $"Can't unload the scene because it hasn't finished loading or is already unloading: {request}",
                    tag: s_Tag
                );
                return null;
            }

            ++s_UnloadingSceneCount;

            MelcherLog.Log($"Unloading scene: {sceneToUnload.name}", tag: s_Tag);

            s_MelcherUnloadingSceneHandles.Add(sceneToUnload.handle);

            sceneUnloading?.InvokeAndIgnoreExceptions(sceneToUnload);

            if (onUnloaded != null)
            {
                CreateTemporarySceneUnloadedListener(sceneToUnload.handle, onUnloaded);
            }

            return SceneManager.UnloadSceneAsync(sceneToUnload);
        }

        public static void EnqueueUnloadScene(MelcherSceneRequest request, UnityAction<Scene> onUnloaded = null)
        {
            if (TryGetScene(request, out Scene sceneToUnload) && sceneToUnload.isLoaded)
            {
                bool startTask = s_ScenesQueuedForUnloading.Count == 0;

                MelcherLog.Log($"Unloading scene (queued): {sceneToUnload.name}", tag: s_Tag);
                s_ScenesQueuedForUnloading.Enqueue(sceneToUnload);

                if (onUnloaded != null)
                {
                    CreateTemporarySceneUnloadedListener(sceneToUnload.handle, onUnloaded);
                }

                if (startTask)
                {
                    // A task is used to controllably unload each prefab
                    // pool's scene in sequential fashion. This is intended to
                    // fix an issue caused by unloading too many scenes at once
                    // on mobile platforms.

                    UnloadScenesSequentially().Forget();
                }
            }

            static async UniTask UnloadScenesSequentially()
            {
                // Wait one frame before unloading to allow any other scenes to
                // register as unloading within the same frame.
                await UniTask.DelayFrame(2);

                await
                    UniTask.WaitWhile(
                        () => IsAnySceneLoading(),
                        timing: PlayerLoopTiming.LastUpdate
                    );

                while (s_ScenesQueuedForUnloading.Count > 0)
                {
                    Scene sceneToUnload =
                        s_ScenesQueuedForUnloading.Peek();

                    await
                        UnloadSceneAsync(
                            MelcherSceneRequest.FromScene(sceneToUnload)
                        );

                    s_ScenesQueuedForUnloading.Dequeue();
                }
            }
        }

        private static void CreateTemporarySceneLoadedListener(SceneHandle sceneHandle, UnityAction<Scene, LoadSceneMode> onLoadedCallback)
        {
            UnityAction<Scene, LoadSceneMode> callbackHandler = null;
            callbackHandler = (loadedScene, loadMode) =>
            {
                if (sceneHandle == loadedScene.handle)
                {
                    sceneLoaded -= callbackHandler;
                    onLoadedCallback.Invoke(loadedScene, loadMode);
                }
            };

            sceneLoaded += callbackHandler;
        }

        private static void CreateTemporarySceneUnloadedListener(SceneHandle sceneHandle, UnityAction<Scene> onUnloadedCallback)
        {
            UnityAction<Scene> callbackHandler = null;
            callbackHandler = (unloadedScene) =>
            {
                if (sceneHandle == unloadedScene.handle)
                {
                    sceneUnloaded -= callbackHandler;
                    onUnloadedCallback.Invoke(unloadedScene);
                }
            };

            sceneUnloaded += callbackHandler;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (loadSceneMode == LoadSceneMode.Additive)
            {
                MelcherLog.Log($"Added scene: {scene.name}", tag: s_Tag);
            }
            else
            {
                MelcherLog.Log($"Loaded scene: {scene.name}", tag: s_Tag);
            }

            if (!s_MelcherLoadingSceneHandles.Remove(scene.handle) &&
                IsShowingNonMelcherSceneLoadingErrors())
            {
                MelcherLog.LogError(
                    $"The scene {scene.name} appears to have been loaded " +
                    $"from outside of {MelcherFormat.ConsoleTypeName(typeof(MelcherSceneManager))}.",
                    tag: s_Tag
                );
            }

            sceneLoaded?.InvokeAndIgnoreExceptions(scene, loadSceneMode);
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            // TODO: Warn if s_UnloadingSceneCount is not > 0. How to do this
            //       but avoid printing errors when Unity unloads scenes in
            //       the editor (stopping play mode)? -Ryan
            if (s_UnloadingSceneCount > 0)
            {
                --s_UnloadingSceneCount;
            }

            MelcherLog.Log($"Unloaded scene: {scene.name}", tag: s_Tag);

            if (!s_MelcherUnloadingSceneHandles.Remove(scene.handle) &&
                IsShowingNonMelcherSceneLoadingErrors())
            {
                MelcherLog.LogError(
                    $"The scene {scene.name} appears to have been unloaded " +
                    $"from outside of {MelcherFormat.ConsoleTypeName(typeof(MelcherSceneManager))}.",
                    tag: s_Tag
                );
            }

            // Partition the editor's console by logging a horizontal line
            // between LoadSceneMode.Single calls. We log this inside
            // OnSceneUnloaded() instead of OnSceneLoaded() because the latter
            // function is called after the new scene's objects Awake() and
            // those object's initialization messages would appear on the wrong
            // side of the line.
            if (IsFinalSceneUnloadBeforeSingleSceneLoad(scene))
            {
                MelcherLog.LogEditorOnly(MelcherLog.DashedLine, tag: s_Tag);
            }

            sceneUnloaded?.Invoke(scene);
        }

        /// <summary>
        /// Check if this is the last unloading scene before the new scene is
        /// loaded during a <see cref="LoadSceneMode.Single"/> load-mode call.
        /// </summary>
        private static bool IsFinalSceneUnloadBeforeSingleSceneLoad(Scene scene)
        {
            // The final scene will be the second-to-last scene in the loaded
            // scene list. The last scene is the one requested to load by
            // `LoadSceneMode.Single`. Despite being in the loaded scene list,
            // all of the scenes will have `isLoaded` false, indicating that a
            // single scene load is in progress
            int sceneCount = SceneManager.sceneCount;
            return sceneCount > 1 &&
                SceneManager.GetSceneAt(sceneCount - 2).handle == scene.handle &&
                GetLoadedSceneCount() <= 0;
        }

        // =====================================================================
        // Static functions
        // =====================================================================

        public static void Init()
        {
            MelcherLog.Assert(Application.isPlaying);

            Application.quitting -= OnAppQuit;
            Application.quitting += OnAppQuit;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            s_CurrentSingleSceneRequest = null;

            static void OnAppQuit()
            {
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

#if UNITY_EDITOR
            int editorLoadedSceneCount =
                UnityEditor.SceneManagement.EditorSceneManager.sceneCount;

            // When the Play Mode Option "disable scene reload" is enabled,
            // `sceneLoaded` events are not sent for scenes that are already
            // loaded in the editor when play-mode starts. We want these
            // events to trigger in the editor because they will happen in
            // build and it is sensible to keep things consistent.
            if (UnityEditor.EditorSettings.enterPlayModeOptionsEnabled &&
                UnityEditor.EditorSettings.enterPlayModeOptions.HasFlag(UnityEditor.EnterPlayModeOptions.DisableSceneReload))
            {
                // Notify listeners that the active scene was loaded first (single)
                Scene activeEditorScene =
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

                UnityEditor.EditorApplication.delayCall +=
                    () => sceneLoaded?.Invoke(activeEditorScene, LoadSceneMode.Single);

                // Notify remaining scenes as loaded (additive)
                for (int i = 0; i < editorLoadedSceneCount; ++i)
                {
                    Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                    if (scene.handle == activeEditorScene.handle)
                    {
                        // Skip active scene. It's already handled
                        continue;
                    }

                    UnityEditor.EditorApplication.delayCall +=
                        () => sceneLoaded?.Invoke(scene, LoadSceneMode.Additive);
                }
            }
            else
            {
                // Mark the scenes that are already loaded in the editor as
                // having been loaded via MelcherSceneManager. This prevents
                // editor-time errors about scene that are already open as
                // having been loaded improperly.

                for (int i = 0; i < editorLoadedSceneCount; ++i)
                {
                    Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                    s_MelcherLoadingSceneHandles.Add(scene.handle);
                }
            }
#endif
        }

        /// <summary>
        ///     Return <see langword="true"/> if the editor is currently
        ///     configured to display errors when a scene is loaded without
        ///     using <see cref="MelcherSceneManager"/>.
        ///     <br /><br />
        ///
        ///     This always returns <see langword="true"/> in builds because
        ///     we don't have access to editor prefs at runtime.
        /// </summary>
        private static bool IsShowingNonMelcherSceneLoadingErrors()
        {
#if UNITY_EDITOR
            return EditorPrefs.GetBool(EditorPrefsKey.ShowNonMelcherSceneLoadingErrors, defaultValue: true);
#else
            return true;
#endif
        }


        // ====================================================================
        // Members
        // ====================================================================

        private static MelcherSceneRequest? s_CurrentSingleSceneRequest;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly string s_Tag = MelcherLog.MakeLogTag("52825D"); // Middle-green

        [MelcherStaticFieldEditorTreatment.ClearCollection]
        private static Queue<Scene> s_ScenesQueuedForUnloading = new Queue<Scene>();

        [MelcherStaticFieldEditorTreatment.ResetToTypeDefault]
        private static int s_UnloadingSceneCount;

        [MelcherStaticFieldEditorTreatment.ClearCollection]
        private static List<SceneHandle> s_MelcherLoadingSceneHandles = new List<SceneHandle>();

        [MelcherStaticFieldEditorTreatment.ClearCollection]
        private static List<SceneHandle> s_MelcherUnloadingSceneHandles = new List<SceneHandle>();


#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            // Synchronize the menu item's checkbox to its actual state.
            Menu.SetChecked(s_ToggleNonMelcherSceneLoadingErrorsMenuItemPath, IsShowingNonMelcherSceneLoadingErrors());
        }

        [MelcherResetStaticFieldsMethod]
        private static int OnResetStaticFields()
        {
            int fieldsReset = 0;

            ++fieldsReset;  s_CurrentSingleSceneRequest = null;

            return fieldsReset;
        }

        /// <summary>
        ///     A toggleable menu item to enable or disable the warnings this
        ///     feature emits.
        ///     <br /><br />
        ///
        ///     This is intended as a temporary feature to help migrate existing
        ///     projects over to Melcher Core.
        /// </summary>
        [MenuItem(itemName: s_ToggleNonMelcherSceneLoadingErrorsMenuItemPath)]
        private static void ToggleNonMelcherSceneLoadingErrors()
        {
            bool showErrors = IsShowingNonMelcherSceneLoadingErrors();

            showErrors = !showErrors;

            Menu.SetChecked(s_ToggleNonMelcherSceneLoadingErrorsMenuItemPath, showErrors);

            EditorPrefs.SetBool(key: EditorPrefsKey.ShowNonMelcherSceneLoadingErrors, value: showErrors);
        }

        public static class EditorPrefsKey
        {
            /// <summary>
            ///     An EditorPrefs key that saves whether the user wants to see
            ///     errors when a scene appears to have been loaded outside of
            ///     <see cref="MelcherSceneManager"/>.
            /// </summary>
            [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
            public static readonly string ShowNonMelcherSceneLoadingErrors = typeof(MelcherSceneManager).FullName + "." + nameof(ShowNonMelcherSceneLoadingErrors);

        }   // static class EditorPrefsKey

        /// <summary>
        ///     The menu item path to toggle warnings on or off.
        /// </summary>
        private const string s_ToggleNonMelcherSceneLoadingErrorsMenuItemPath =
            "Tools/Melcher Studios/Editor Settings/Error when scenes are loaded without using MelcherSceneManager";

#endif  // if UNITY_EDITOR

    }   // class MelcherSceneManager

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-01-19
/// ===========================================================================
