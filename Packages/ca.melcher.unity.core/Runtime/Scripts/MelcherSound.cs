namespace Melcher
{
    using UnityEngine;
    using UnityEngine.Audio;

    /// <summary>
    ///     A <see cref="MelcherSound"/> is a list of audio clips and their
    ///     collective shared properties (starting volume, pitch, output mixer
    ///     group, etc.). The audio clips in the list are intended to be played
    ///     back one at a time, and each time a different clip will be
    ///     selected.
    ///     <br /><br />
    ///
    ///     This class provides methods to play the internally selected clip
    ///     from an <see cref="AudioSource"/> and the audio source's properties
    ///     will be adjusted to match the <see cref="MelcherSound"/>'s
    ///     configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSound", menuName = "Melcher Studios/Sound")]
    public class MelcherSound : MelcherScriptableObject
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public AudioMixerGroup mixerGroup => m_MixerGroup;

        // =====================================================================
        // Methods
        // =====================================================================

        public void PlayFrom(AudioSource audioSource)
        {
            AudioClip clip = GetNextClip();
            if (clip)
            {
                audioSource.clip = clip;

                ApplySoundProperties(audioSource);

                audioSource.Play();
            }
        }

        public void PlayOneShotFrom(AudioSource audioSource, float volumeScale = 1.0f)
        {
            AudioClip clip = GetNextClip();
            if (clip)
            {
                ApplySoundProperties(audioSource);

                audioSource.PlayOneShot(clip, volumeScale);
            }
        }

        public AudioClip GetNextClip()
        {
            AudioClip clip = null;

            if (m_Clips.Length > 0)
            {
                switch (m_SequenceMode)
                {
                case SequenceMode.Shuffled:
                    if (m_ShuffledClipIndices == null || m_ShuffledClipIndices.Length != m_Clips.Length)
                    {
                        m_ShuffledClipIndices = new byte[m_Clips.Length];

                        for (byte i = 0; i < m_ShuffledClipIndices.Length; ++i)
                        {
                            m_ShuffledClipIndices[i] = i;
                        }

                        MelcherCommon.Shuffle(m_ShuffledClipIndices);
                    }

                    if (m_NextClipIndex >= m_ShuffledClipIndices.Length)
                    {
                        // Grab the last clip index. We will ensure this index
                        // does not repeat first after the re-shuffle.
                        byte lastClipIndex =
                            m_ShuffledClipIndices[m_ShuffledClipIndices.Length - 1];

                        // Re-shuffle
                        MelcherCommon.Shuffle(m_ShuffledClipIndices);
                        m_NextClipIndex = 0;

                        // Ensure the first clip isn't the same as the last clip
                        if (m_Clips.Length > 2 && m_ShuffledClipIndices[0] == lastClipIndex)
                        {
                            int r = Random.Range(1, m_ShuffledClipIndices.Length);
                            byte toSwap = m_ShuffledClipIndices[0];
                            m_ShuffledClipIndices[0] = m_ShuffledClipIndices[r];
                            m_ShuffledClipIndices[r] = toSwap;
                        }
                    }

                    clip = m_Clips[m_NextClipIndex];
                    ++m_NextClipIndex;

                    break;

                case SequenceMode.Random:
                    clip = m_Clips[Random.Range(0, m_Clips.Length)];
                    break;

                case SequenceMode.Ordered:
                    clip = m_Clips[m_NextClipIndex];
                    m_NextClipIndex = (m_NextClipIndex + 1) % m_Clips.Length;
                    break;
                }
            }

            return clip;
        }

        /// <summary>
        ///     Set the <paramref name="audioSource"/>'s mixer group, volume,
        ///     and pitch such that it conforms to the setup of the <see
        ///     cref="MelcherSound"/> object. This does not assign a clip.
        /// </summary>
        public void ApplySoundProperties(AudioSource audioSource)
        {
            float time = Time.time;
            audioSource.outputAudioMixerGroup = mixerGroup;
            audioSource.volume = m_StartVolume.Evaluate(time, lerpFactor: Random.value);
            audioSource.pitch = m_StartPitch.Evaluate(time, lerpFactor: Random.value);
        }

        // =====================================================================
        // Inspector-serialized members
        // =====================================================================

        [SerializeField]
        protected AudioMixerGroup m_MixerGroup = null;

        [Space]

        [SerializeField]
        protected ParticleSystem.MinMaxCurve m_StartVolume = 1.0f;

        [SerializeField]
        protected ParticleSystem.MinMaxCurve m_StartPitch = 1.0f;

        [Space]

        // The idea behind these (VolumeModifier & PitchModifier) was to have
        // the sounds' volume & pitch be adjusted over time during play. For
        // example, having a sound's pitch change during playback. This
        // functionality isn't implemented here but it may exist in a project
        // like Kareero. --Ryan

        [SerializeField]
        [Tooltip("Not used in Melcher Core. May be used in Kareero?")]
        protected ModifierMode m_VolumeModifierMode = ModifierMode.None;

        [SerializeField]
        [Tooltip("Not used in Melcher Core. May be used in Kareero?")]
        protected ParticleSystem.MinMaxCurve m_VolumeModifier = default;

        [Space]

        [SerializeField]
        [Tooltip("Not used in Melcher Core. May be used in Kareero?")]
        protected ModifierMode m_PitchModifierMode = ModifierMode.None;

        [SerializeField]
        [Tooltip("Not used in Melcher Core. May be used in Kareero?")]
        protected ParticleSystem.MinMaxCurve m_PitchModifier = default;

        [Space]

        [SerializeField]
        protected SequenceMode m_SequenceMode = SequenceMode.Random;

        [SerializeField]
        protected AudioClip[] m_Clips = null;

        // =====================================================================
        // Internal members
        // =====================================================================

        private int m_NextClipIndex;
        private byte[] m_ShuffledClipIndices;

        // =====================================================================
        // Nested types
        // =====================================================================

        public enum ModifierMode
        {
            None,
            Add,
            Multiply
        }   // enum ModifierMode

        public enum SequenceMode
        {
            Shuffled,
            Random,
            Ordered
        }   // enum SequenceMode

    }   // class MelcherSound

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-01-19
/// ===========================================================================
