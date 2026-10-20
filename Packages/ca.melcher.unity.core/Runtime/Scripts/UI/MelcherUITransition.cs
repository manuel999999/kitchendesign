namespace Melcher
{
    /// <summary>
    /// A <see cref="MelcherUITransition"/> is a UI transition request. It does
    /// not itself define any animation logic, but simply contains the <see
    /// cref="MelcherUITransitionStyle">style</see> of animation (e.g.,
    /// progress, regress, etc.) and arguments to use within a <see
    /// cref="MelcherUI"/> (e.g., the animation's time scale, arbitrary user-
    /// data, etc.)
    /// <br /><br />
    ///
    /// <see cref="MelcherUI.Show">MelcherUI.Show</see> and <see
    /// cref="MelcherUI.Hide">MelcherUI.Hide</see> take an argument of this
    /// type.
    /// <br /><br />
    ///
    /// A <see cref="Default">default</see> UI transition is available and is
    /// convenient to use with the <see cref="WithStyle">WithStyle</see> method
    /// to tersely override the animation style.
    /// </summary>
    public readonly struct MelcherUITransition
    {
        // =====================================================================
        // Constructors
        // =====================================================================

        /// <summary>
        /// Construct a UI transition.
        /// </summary>
        public MelcherUITransition(MelcherUITransitionStyle style, float timeScale = 1.0f, object userData = null)
        {
            this.style = style;
            this.timeScale = timeScale;
            this.userData = userData;
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        /// <summary>
        /// The default UI transition. It represents forward-navigation using
        /// a non-scaled speed.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        public static readonly MelcherUITransition Default =
            new MelcherUITransition(
                style: MelcherUITransitionStyle.Progress,
                timeScale: 1.0f,
                userData: null
            );

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Clone the transition object but override its <see cref="style"/>.
        /// </summary>
        public MelcherUITransition WithStyle(MelcherUITransitionStyle newStyle)
        {
            return new MelcherUITransition(
                style: newStyle,
                timeScale: timeScale,
                userData: userData
            );
        }

        // =====================================================================
        // Members
        // =====================================================================

        /// <summary>
        /// A time factor to apply to an animation's timings. Classes that
        /// derive from <see cref="MelcherUI"/> should inspect this value and
        /// adjust their animation times accordingly.
        /// <br /><br />
        ///
        /// Example:<code>
        /// // Within a MelcherUI subclass's DoShow:
        /// const float SecondsToAnimate = 2.0f; // number of seconds at 1x time scale
        /// float scaledSecondsToAnimate = SecondsToAnimate / m_Transition.timeScale;
        /// </code>
        /// </summary>
        public readonly float timeScale;

        /// <summary>
        /// The type of transition to perform. The value is never intended to
        /// be <see langword="null"/>. There are some pre-built styles within
        /// <see cref="MelcherUITransitionStyle"/> (e.g., <see
        /// cref="MelcherUITransitionStyle.Progress"/>). Classes that derive
        /// from <see cref="MelcherUI"/> are expected to check this field to
        /// determine which animation implementation they will use.
        /// </summary>
        public readonly MelcherUITransitionStyle style;

        /// <summary>
        /// Optional, arbitrary data to pass to the UI. For example, this could
        /// be used to pass more animation arguments. It can be a class or a
        /// simple boxed value. The receiving <see cref="MelcherUI"/> is
        /// expected to cast this field back to the type it was passed in as
        /// to access it. The default value is <see langword="null"/>.
        /// </summary>
        public readonly object userData;

    }   // struct MelcherUITransition

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-01-22
/// ===========================================================================
