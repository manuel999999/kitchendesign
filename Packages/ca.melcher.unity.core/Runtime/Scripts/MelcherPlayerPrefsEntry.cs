namespace Melcher
{
    using System;
    using UnityEngine;

    /// <summary>
    ///     This wraps a PlayerPref type and key. It is meant to be used in a
    ///     MonoBehaviour or ScriptableObject where the key and default value can
    ///     be set in the inspector.
    ///
    ///     In scripts where this is used, <see cref="FromPrefs"/> can be called to
    ///     initialize the value from <see cref="PlayerPrefs"/> and then
    ///     <see cref="ToPrefs"/> is used to save out the value. It is important
    ///     to call <see cref="PlayerPrefs.Save"/> to actually commit the values to
    ///     disk.
    ///
    ///     The <see cref="onValueChanged"/> event is called whenever the value
    ///     changes.
    ///
    ///     <code>
    ///         [SerializeField]
    ///         protected MelcherPlayerPrefsEntry<float> m_GameVolume = default;
    ///
    ///         // ...
    ///
    ///         // Load from player prefs
    ///         m_GameVolume.FromPrefs(defaultValue: 1.0f);
    ///
    ///         // Reading
    ///         AudioSource gunSound = ...;
    ///         gunSound.volume = m_GameVolume.volume;
    ///
    ///         // Writing
    ///         m_GameVolume.value = 0.5f;
    ///         PlayerPrefs.Save();
    ///     </code>
    /// </summary>
    /// <typeparam name="ValueType">
    ///     One of: bool, int, float, string
    /// </typeparam>
    [Serializable]
    public class MelcherPlayerPrefsEntry<ValueType>
    {
        public delegate void OnValueChangedHandler(ValueType oldValue, ValueType newValue);
        public event OnValueChangedHandler onValueChanged;

        // ====================================================================
        // Properties
        // ====================================================================

        public ValueType value
        {
            get => m_Value;
            set
            {
                if (!object.Equals(m_Value, value))
                {
                    ValueType oldValue = m_Value;
                    m_Value = value;
                    ToPrefs();
                    onValueChanged?.Invoke(oldValue: oldValue, newValue: m_Value);
                }
            }
        }

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        /// Read the value from PlayerPrefs.
        /// </summary>
        public void FromPrefs(ValueType defaultValue = default)
        {
            object newValue = default;

            // Handle correct type
            switch (defaultValue)
            {
                case bool b:
                    newValue = PlayerPrefs.GetInt(m_PlayerPrefsKey, b ? 1 : 0) != 0;
                    break;

                case int i:
                    newValue = PlayerPrefs.GetInt(m_PlayerPrefsKey, i);
                    break;

                case float f:
                    newValue = PlayerPrefs.GetFloat(m_PlayerPrefsKey, f);
                    break;

                case string s:
                    newValue = PlayerPrefs.GetString(m_PlayerPrefsKey, s);
                    break;

                default:
                    MelcherLog.LogError(
                        $"{m_PlayerPrefsKey} uses unsupported value type: {m_Value.GetType().FullName}"
                    );
                    break;
            }

            // Set via the `value` property to reuse the onValueChanged event
            value = (ValueType)newValue;
        }

        /// <summary>
        /// Write the value to PlayerPrefs. Does not call PlayerPrefs.Save().
        /// </summary>
        public void ToPrefs()
        {
            // Handle correct type
            switch (m_Value)
            {
                case bool b:
                    PlayerPrefs.SetInt(m_PlayerPrefsKey, b ? 1 : 0);
                    break;

                case int i:
                    PlayerPrefs.SetInt(m_PlayerPrefsKey, i);
                    break;

                case float f:
                    PlayerPrefs.SetFloat(m_PlayerPrefsKey, f);
                    break;

                case string s:
                    PlayerPrefs.SetString(m_PlayerPrefsKey, s);
                    break;

                default:
                    MelcherLog.LogError(
                        message: $"{m_PlayerPrefsKey} uses unsupported value type: {m_Value.GetType().FullName}"
                    );
                    break;
            }
        }

        /// <summary>
        /// Delete a value from PlayerPrefs. Does not call PlayerPrefs.Save().
        /// </summary>
        public void DeletePref()
        {
            PlayerPrefs.DeleteKey(m_PlayerPrefsKey);
        }

        /// <summary>
        /// Get Key if needed.
        /// </summary>

        public string GetKey()
        {
            return m_PlayerPrefsKey;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField]
        private ValueType m_Value = default;

        [SerializeField]
        private string m_PlayerPrefsKey = null;

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================
        public const string ValueMemberName = nameof(m_Value);
        public const string PlayerPrefsKeyMemberName = nameof(m_PlayerPrefsKey);
#endif  // if UNITY_EDITOR

    }   // class MelcherPlayerPrefsEntry

}   // namespace Melcher
