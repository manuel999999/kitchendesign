namespace Melcher
{
    /// <summary>
    ///     The bridge that <see cref="MelcherToggleGroup"/> uses to interface
    ///     with <see cref="MelcherToggle"/>. This avoids a cyclical dependency
    ///     between the two types, which makes for a cleaner design.
    /// </summary>
    public interface IMelcherToggle
    {
        delegate void OnValueChangedHandler(IMelcherToggle caller);

        /// <summary>
        ///     This event is invoked whenever the toggle's <see
        ///     cref="isOn"/> property is changed. This event can be bypassed
        ///     by calling <see cref="SetIsOnWithoutNotify(bool)"/>.
        /// </summary>
        event OnValueChangedHandler onValueChanged;

        bool isOn { get; set; }

        // TODO: Replace this public function with an event in ToggleGroup that
        //       MelcherToggle can listen to. /This will better encapsulate the
        //       class and prevent developers from calling it when it is meant
        //       to only be called from ToggleGroup. -Ryan
        void SetIsOnIgnoringToggleGroup(bool newIsOn, bool instant = false, bool sendCallback = true);

        void SetIsOnWithoutNotify(bool newIsOn, bool instant);

    }   // interface IMelcherToggle

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-20
/// ===========================================================================
