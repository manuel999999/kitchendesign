namespace Melcher
{
    using BaseType =
#if UNITY_EDITOR
        System.Attribute;
#else   // if UNITY_EDITOR
        UnityEngine.RuntimeInitializeOnLoadMethodAttribute;
#endif  // if UNITY_EDITOR else

    /// <summary>
    ///     A function attribute to mark static functions to execute after
    ///     <see cref="MelcherResetStaticFieldsFeature"/> resets static
    ///     fields (i.e., when entering play mode).
    ///     <br /><br />
    ///
    ///     The intent is to avoid race conditions with other functions that
    ///     wish to execute during <see
    ///     cref="UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration"/>
    ///     but that also need the static fields to have been reset first.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class MelcherAfterResetStaticFieldsMethodAttribute : BaseType
    {
#if !UNITY_EDITOR
        [UnityEngine.Scripting.Preserve]
        public MelcherAfterResetStaticFieldsMethodAttribute()
            : base(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)
        {
        }
#endif  // if !UNITY_EDITOR

    }   // class MelcherAfterResetStaticFieldsMethodAttribute

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-02-07
/// ===========================================================================
