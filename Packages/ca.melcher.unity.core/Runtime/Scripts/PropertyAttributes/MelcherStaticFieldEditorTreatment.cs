namespace Melcher
{
    using System;
    using System.Diagnostics;

    /// <summary>
    ///     Contains attributes for marking how static fields are handled by
    ///     <see cref="MelcherResetStaticFieldsFeature"/> when Play Mode is
    ///     starting.
    ///     <br /><br />
    ///
    ///     Use examples:
    ///     <code>
    ///         // Default behaviour. This field's value will persist between
    ///         // play sessions and MelcherResetStaticFieldsFeature will warn
    ///         // that this behaviour should be explicitly labeled with
    ///         // `MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions`
    ///         private static int s_CountedInStaticFieldsToResetCount;
    ///
    ///         // This field's value will persist between play sessions but
    ///         // MelcherResetStaticFieldsFeature will *not* warn about it.
    ///         [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
    ///         private static int s_NotCountedInStaticFieldsToResetCount;
    ///
    ///         // This field's value will be reset to its type default.
    ///         // For an int, this would be 0.
    ///         [MelcherStaticFieldEditorTreatment.ResetToTypeDefault]
    ///         private static int s_ResetToZeroAutomatically;
    ///     </code>
    /// </summary>
    public class MelcherStaticFieldEditorTreatment
    {
        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        ///     Marks a static field as ignored so it is not counted as
        ///     a field needing to be reset in functions using the <see
        ///     cref="MelcherResetStaticFieldsMethodAttribute"/>.
        /// </summary>
        public class PersistBetweenPlaymodeSessionsAttribute : BaseStaticFieldAttribute
        {
            public PersistBetweenPlaymodeSessionsAttribute()
            {
            }
        }   // class PersistBetweenPlaymodeSessionsAttribute

        /// <summary>
        ///     Marks a static field to be automatically reset whenever script
        ///     reloading takes place. The field will be assigned the default
        ///     value for its type. Importantly, this can be different from the
        ///     field's initial value! In most cases, this mode obviates the
        ///     need to add a <see
        ///     cref="MelcherResetStaticFieldsMethodAttribute"/> static
        ///     function to a class.
        /// </summary>
        public class ResetToTypeDefaultAttribute : BaseStaticFieldAttribute
        {
            public ResetToTypeDefaultAttribute()
            {
            }
        }   // class ResetToTypeDefaultAttribute

        /// <summary>
        ///     Marks a static collection-based field to be cleared whenever
        ///     script reloading takes place. This attempts to support many
        ///     of C#'s built-in collection types.
        /// </summary>
        public class ClearCollectionAttribute : BaseStaticFieldAttribute
        {
            public ClearCollectionAttribute()
            {
            }
        }   // class ClearCollectionAttribute

        [Conditional("UNITY_EDITOR")]
        [AttributeUsage(AttributeTargets.Field)]
        // Hide this type from code completion. It's not intended to be used by
        // external assemblies.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public abstract class BaseStaticFieldAttribute : Attribute
        {
            protected BaseStaticFieldAttribute()
            {
            }
        }   // class MelcherResetStaticFieldInfoAttribute

    }   // class MelcherStaticFieldEditorTreatment

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-10
/// ===========================================================================
