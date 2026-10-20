namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    ///     This editor adds a "Play Next" button to the inspector which allows
    ///     the <see cref="MelcherSound"/>'s settings to be tested by playing
    ///     them in edit-mode.
    /// </summary>
    [CustomEditor(typeof(MelcherSound))]
    public class MelcherSoundEditor : Editor
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public void OnEnable()
        {
            if (s_PlayNextButtonContent == null)
            {
                s_PlayNextButtonContent = new GUIContent("Play Next");
            }

            if (s_UnmuteAndPlayNextButtonContent == null)
            {
                s_UnmuteAndPlayNextButtonContent =
                    new GUIContent("Play Next (will Unmute Game View)");
            }

            StopSounds();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            bool isGameViewMuted = EditorUtility.audioMasterMute;

            GUIContent playNextButtonContent =
                isGameViewMuted ?
                    s_UnmuteAndPlayNextButtonContent :
                    s_PlayNextButtonContent;

            if (GUILayout.Button(playNextButtonContent))
            {
                if (isGameViewMuted)
                {
                    EditorUtility.audioMasterMute = false;
                }

                PlayNext();
            }

            if (GUILayout.Button("Stop All"))
            {
                StopSounds();
            }
        }

        private void PlayNext()
        {
            MelcherSound sound = (MelcherSound)target;
            if (sound == null)
            {
                return;
            }

            AudioSource audioSource = GetNextAudioSource();
            if (!audioSource)
            {
                return;
            }

            sound.PlayFrom(audioSource);
        }

        public static void StopSounds()
        {
            if (s_HiddenAudioSources == null)
            {
                return;
            }

            foreach (AudioSource audioSource in s_HiddenAudioSources)
            {
                if (audioSource)
                {
                    audioSource.Stop();
                }
            }
        }

        private static AudioSource GetNextAudioSource()
        {
            AudioSource[] audioSources = FindOrCreateHiddenAudioSourceList();

            if (s_NextAudioSourceIndex >= audioSources.Length)
            {
                s_NextAudioSourceIndex = 0;
            }

            int audioSourceIndex = s_NextAudioSourceIndex;

            s_NextAudioSourceIndex =
                (s_NextAudioSourceIndex + 1) % s_HiddenAudioSources.Length;

            return s_HiddenAudioSources[audioSourceIndex];
        }

        private static AudioSource[] FindOrCreateHiddenAudioSourceList()
        {
            const string AudioSourceHolderName = nameof(MelcherSoundEditor);

            if (!s_HiddenAudioSourceHolder)
            {
                // First, try to find an existing audio holder. Finding, rather
                // than always creating, prevents a gradual build-up of hidden
                // game objects between recompiles.

                s_HiddenAudioSourceHolder =
                    GameObject.Find("/" + AudioSourceHolderName);
            }

            if (!s_HiddenAudioSourceHolder)
            {
                // We didn't find an existing audio source holder. Make one.

                s_HiddenAudioSourceHolder =
                    new GameObject(AudioSourceHolderName);

                s_HiddenAudioSourceHolder.hideFlags = HideFlags.HideAndDontSave;

                s_HiddenAudioSources = null;
            }

            if (s_HiddenAudioSources == null || s_HiddenAudioSources.Length <= 0)
            {
                // Try to retrieve the existing audio sources.

                s_HiddenAudioSources =
                    s_HiddenAudioSourceHolder.GetComponents<AudioSource>();
            }

            if (s_HiddenAudioSources == null || s_HiddenAudioSources.Length <= 0)
            {
                // No existing audio sources. Create them.

                // Multiple audio sources are added to the audio source holder.
                // This allow rapid playback of clips and allows the sounds to
                // be stopped at request (i.e., by using `Play()` instead of
                // `PlayOneShot()`)

                s_HiddenAudioSources = new AudioSource[16];

                for (int i = 0; i < s_HiddenAudioSources.Length; ++i)
                {
                    s_HiddenAudioSources[i] =
                        s_HiddenAudioSourceHolder.AddComponent<AudioSource>();

                    s_HiddenAudioSources[i].playOnAwake = false;
                    s_HiddenAudioSources[i].spatialBlend = 0.0f;
                }
            }

            return s_HiddenAudioSources;
        }

        // ====================================================================
        // Static fields
        // ====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GameObject s_HiddenAudioSourceHolder;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static AudioSource[] s_HiddenAudioSources;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static int s_NextAudioSourceIndex;

        [MelcherStaticFieldEditorTreatment.ResetToTypeDefault]
        private static GUIContent s_PlayNextButtonContent;

        [MelcherStaticFieldEditorTreatment.ResetToTypeDefault]
        private static GUIContent s_UnmuteAndPlayNextButtonContent;

    }   // class MelcherSound

}   // namespace Melcher
