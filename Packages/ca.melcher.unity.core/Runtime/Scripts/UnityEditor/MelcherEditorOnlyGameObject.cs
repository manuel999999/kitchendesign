namespace Melcher
{
    using UnityEngine;

#if UNITY_EDITOR
    using System.Collections.Generic;
#endif

    /// <summary>
    ///     Game object with this component attached will automatically be
    ///     marked for removal from builds.
    ///     <br /><br />
    ///
    ///     In Play Mode, the game object is not deleted but simply disabled.
    ///     It can be configured to stay enabled if there is only one scene
    ///     active.
    /// </summary>
    public class MelcherEditorOnlyGameObject : MelcherMonoBehaviour
    {
        // ====================================================================
        // Methods
        // ====================================================================

        private void Awake()
        {
#if UNITY_EDITOR
            // TODO: Should we check the number of scenes that are loading vs number that are already loaded?
            int scenesLoaded = UnityEngine.SceneManagement.SceneManager.sceneCount;
            if (m_EnableInSingleScene && scenesLoaded == 1)
            {
                return;
            }
#endif  // if UNITY_EDITOR

            // Disable the game object the first time it wakes. This is done
            // in Awake() instead of OnEnable() to allow developers to forcibly
            // re-enable the game object at editor run-time if desired.
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        // ====================================================================
        // Editor
        // ====================================================================

        /// <summary>
        /// Ensure the game object is using the EditorOnly tag.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!UnityEditor.EditorApplication.isPlaying)
            {
                // The tag can't be changed immediately from OnValidate:
                //     "SendMessage cannot be called..."
                // Therefore, queue the object and validate when possible

                s_QueuedForValidate.Add(this);

                if (s_QueuedForValidate.Count == 1)
                {
                    UnityEditor.EditorApplication.delayCall += SetTags;
                }
            }
        }

        private static void SetTags()
        {
            try
            {
                foreach (MelcherEditorOnlyGameObject queued in s_QueuedForValidate)
                {
                    if (queued && !queued.CompareTag(EditorOnlyTag))
                    {
                        if (UnityEditor.PrefabUtility.IsPartOfAnyPrefab(queued) &&
                            !UnityEditor.PrefabUtility.IsAddedComponentOverride(queued))
                        {
                            MelcherLog.LogWarning(
                                $"Skipping prefab instance {MelcherUnityUtility.GetHierarchyPath(queued.transform)} with tag {queued.tag}",
                                context: queued
                            );
                            continue;
                        }

                        // Assign editor tag. This will ensure the object is removed in builds
                        MelcherLog.Log(
                            $"Changing tag of {MelcherUnityUtility.GetHierarchyPath(queued.transform)}: {queued.tag} -> {EditorOnlyTag}",
                            context: queued
                        );
                        queued.tag = EditorOnlyTag;

                        // Record prefab instance changes
                        if (MelcherEditorUtility.IsPartOfCurrentPrefabStage(queued.gameObject))
                        {
                            UnityEditor.EditorUtility.SetDirty(queued);
                        }
                        else if (UnityEditor.PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(queued))
                        {
                            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(queued);
                            UnityEditor.EditorUtility.SetDirty(queued);
                        }
                    }
                }
            }
            finally
            {
                s_QueuedForValidate.Clear();
            }
        }

        public const string EditorOnlyTag = "EditorOnly";

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static List<MelcherEditorOnlyGameObject> s_QueuedForValidate =
            new List<MelcherEditorOnlyGameObject>();

#endif

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

#pragma warning disable 0414    // The field '<varname>' is assigned but its value is never used
        [Tooltip("When enabled, the script will not disable the game object " +
            "in the Awake function if there is only one scene loaded. This " +
            "is only relevant inside the editor because the object's tag " +
            "(EditorOnly) force the game object to be stripped from builds.\n" +
            "\n" +
            "One example use-case is for editor-only cameras. E.g. for " +
            "designing UI.")]
        [SerializeField]
        private bool m_EnableInSingleScene = false;
#pragma warning restore 0414

    }   // class MelcherEditorOnlyGameObject

}   // namespace Melcher
