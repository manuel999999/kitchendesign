namespace Melcher
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// <see cref="MelcherVariable{ValueType, AssetType}"/> is a generic
    /// implementation of Ryan Hipple's <c>FloatVariable</c>. It is intended
    /// for inspector-serialized variables where you want the flexibility to
    /// use either a unique (per-component) value or a shared value stored
    /// within an asset. For this implementation, the asset must be a subtype
    /// of <see cref="MelcherAppValue{UnderlyingType}"/>.
    /// <br /><br />
    ///
    /// The two generic template types, <typeparamref name="ValueType"/> and
    /// <typeparamref name="AppValueType"/>, control what the variable stores
    /// and how it behaves in the inspector. <typeparamref name="ValueType"/>
    /// is the fundamental, underlying type: <c>int</c>, <c>float</c>,
    /// <c>string</c>, <c>Vector3</c>, etc. <typeparamref name="AppValueType"/>
    /// is the <see cref="MelcherAppValue{UnderlyingType}"/> type that
    /// implicitly converts to a <typeparamref name="ValueType"/>.
    /// <br /><br />
    ///
    /// Refer to the following resources:<br />
    /// <see href="https://unity.com/how-to/architect-game-code-scriptable-objects#architect-variables"/><br />
    /// <see href="https://www.youtube.com/watch?v=raQ3iHhE_Kk"/>
    /// </summary>
    [System.Serializable]
    public class MelcherVariable<ValueType, AppValueType> : MelcherVariableBase
        where AppValueType : MelcherAppValue<ValueType>
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public ValueType value
        {
            get
            {
                if (hasAsset)
                {
                    return m_AppValue;
                }

                return m_InlineValue;
            }

            set
            {
                if (hasAsset)
                {
                    m_AppValue.SetSharedValue(value);
                }
                else
                {
                    m_InlineValue = value;
                }
            }
        }

        public override System.Type appValueType => typeof(AppValueType);

        public override bool hasAsset => !EqualityComparer<AppValueType>.Default.Equals(m_AppValue, default(AppValueType));

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Implicit conversion to underlying type.
        /// </summary>
        public static implicit operator ValueType(MelcherVariable<ValueType, AppValueType> me) => me.value;
        
        public override string ToString()
        {
            if (EqualityComparer<ValueType>.Default.Equals(value, default(ValueType)))
            {
                return "";
            }

            return value.ToString();
        }

        // =====================================================================
        // Inspector-serialized members
        // =====================================================================

        [Tooltip("A value defined inline with this component. Saves the need " +
            "of creating an app value (asset).")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_Unique")]
        protected ValueType m_InlineValue;

        [Tooltip("An asset. Useful when a value should be similar in many " +
            "places. May be null.")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_Shared")]
        protected AppValueType m_AppValue;

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        public const string InlineMemberName = nameof(m_InlineValue);
        public const string AppValueMemberName = nameof(m_AppValue);

#endif  // if UNITY_EDITOR

    }   // class MelcherVariable

}   // namespace Melcher
