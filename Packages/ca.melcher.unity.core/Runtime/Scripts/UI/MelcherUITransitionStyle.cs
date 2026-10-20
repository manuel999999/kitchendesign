namespace Melcher
{
    /// <summary>
    ///     <see cref="MelcherUITransitionStyle"/> is an "enumeration class"; it
    ///     is just an empty class that is used to simply declare the styles of
    ///     UI transitions that are available but without defining their
    ///     specific implementation.
    ///     <br /><br />
    ///
    ///     The idea is users will create static, read-only instances of this
    ///     class to extend the enumeration. (Some built-in styles are included
    ///     within the class itself.) It is up to the various <see
    ///     cref="MelcherUI"/> subclasses to actually implement the transition
    ///     logic for these names.
    /// </summary>
    public class MelcherUITransitionStyle
    {
        /// <summary>
        ///     Default transition. This should be treated as a forward
        ///     navigation transition; i.e. moving towards a "more nested" UI
        ///     state.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        public static readonly MelcherUITransitionStyle Progress =
            new MelcherUITransitionStyle();

        /// <summary>
        ///     Backwards navigation. It can be thought of moving towards a
        ///     "less nested" UI state.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        public static readonly MelcherUITransitionStyle Regress =
            new MelcherUITransitionStyle();

    }   // class MelcherUITransitionStyle

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-01-22
/// ===========================================================================
