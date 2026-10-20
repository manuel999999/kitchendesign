namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// A <see cref="MelcherAppValue{UnderlyingType}"/> represents a value that
    /// should remain constant throughout the app's lifetime. It wraps the value
    /// within an asset. This makes it convenient to implement the fly-weight
    /// pattern for reusing a value in many places.
    /// <br /><br />
    ///
    /// Some use cases for app values include: a reference to a scene, an
    /// animation curve, the max hit points for a monster, etc.
    /// <br /><br />
    ///
    /// The reason for suggesting the value remain constant throughout the app's
    /// lifetime is due to ScriptableObjects only being serialized at edit-time.
    /// In builds, the values are not saved and are subject to loss as soon as
    /// the object is unloaded. You may still choose to modify the value and be
    /// mindful of this caveat.
    /// </summary>
    public abstract class MelcherAppValue<UnderlyingType> : MelcherScriptableObject
    {
        /// <summary>
        /// Read-only access to value.
        /// </summary>
        public UnderlyingType value => m_Value;

        /// <summary>
        /// Explicit value mutator. Due to <see cref="ScriptableObject"/>s only
        /// being serialized within the editor, it is a helper reminder to
        /// requiring changing its value through a separate (this) function.
        /// </summary>
        public void SetSharedValue(UnderlyingType newValue)
        {
            m_Value = newValue;
        }

        /// <summary>
        /// Implicit conversion to underlying type.
        /// </summary>
        public static implicit operator UnderlyingType(MelcherAppValue<UnderlyingType> me) => me.m_Value;

        /// <summary>
        /// Return the underlying value's ToString().
        /// </summary>
        public override string ToString() => m_Value.ToString();

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        /// <summary>
        /// Data to be serialized to an asset file. Can be assigned in the
        /// Unity Inspector.
        /// </summary>
        [SerializeField]
        protected UnderlyingType m_Value = default;

        // ====================================================================
        // Editor-only
        // ====================================================================

#if UNITY_EDITOR
        /// <summary>
        ///     Paths to properties for use with <see
        ///     cref="UnityEditor.SerializedObject.FindProperty(string)"/>.
        /// </summary>
        public static class PropertyPath
        {
            public const string Value = nameof(m_Value);
        }   // static class PropertyPath

#endif  // if UNITY_EDITOR

    }   // class MelcherAppValue

}   // namespace Melcher

