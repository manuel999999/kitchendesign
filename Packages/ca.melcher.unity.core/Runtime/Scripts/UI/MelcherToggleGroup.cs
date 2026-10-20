namespace Melcher
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    /// <summary>
    ///     This is an alternate implementation of Unity's <see
    ///     cref="UnityEngine.UI.ToggleGroup"/>. It knows all of the toggles
    ///     that are associated with the group and exposes an event that will
    ///     be invoked whenever any of the linked toggles changes state.
    /// </summary>
    public class MelcherToggleGroup : MelcherMonoBehaviour
    {
        // =====================================================================
        // Events
        // =====================================================================

        public delegate void OnToggleChangedHandler(MelcherToggleGroup group, IMelcherToggle toggle);
        public event OnToggleChangedHandler onToggleChanged;

        // =====================================================================
        // Properties
        // =====================================================================

        public bool allowSwitchOff
        {
            get => m_AllowSwitchOff;
            set => m_AllowSwitchOff = value;
        }

        public ReadOnlyCollection<IMelcherToggle> toggles => m_Toggles.AsReadOnly();

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Return <see langword="true"/> if any toggle that is currently
        ///     registered is on.
        /// </summary>
        // REVIEW: Should we rename this to match Unity's ToggleGroup API?
        //         I.e., `AnyTogglesOn` -Ryan

        public bool IsAnyToggleOn()
        {
            return GetFirstActiveToggle() != null;
        }

        public IMelcherToggle GetFirstActiveToggle()
        {
            foreach (IMelcherToggle toggle in m_Toggles)
            {
                if (toggle.isOn)
                {
                    return toggle;
                }
            }

            return null;
        }

        /// <summary>
        ///     Switch all toggles off, regardless of whether the <see
        ///     cref="allowSwitchOff"/> property is enabled or not.
        /// </summary>
        public void SetAllTogglesOff(bool instant = false, bool sendCallback = true)
        {
            foreach (IMelcherToggle toggle in m_Toggles)
            {
                if (toggle != null && toggle.isOn)
                {
                    toggle.SetIsOnIgnoringToggleGroup(newIsOn: false, instant, sendCallback);
                }
            }
        }

        public void RegisterToggle(IMelcherToggle toggle)
        {
            m_Toggles.Add(toggle);

            // TODO:
            // EnsureValidState();
        }

        public void UnregisterToggle(IMelcherToggle toggle)
        {
            m_Toggles.Remove(toggle);
        }

        public void ChangeToggleIsOn(IMelcherToggle instigatingToggle, bool newValue, bool instant = false)
        {
            IMelcherToggle firstActiveToggle = GetFirstActiveToggle();

            if (ReferenceEquals(instigatingToggle, firstActiveToggle) && !m_AllowSwitchOff)
            {
                // Can't turn off the active toggle
                return;
            }

            instigatingToggle.SetIsOnIgnoringToggleGroup(newValue, instant);

            // If turning a value on, make sure all the others get turned off
            if (newValue)
            {
                for (int i = m_Toggles.Count - 1; i >= 0; --i)
                {
                    if (!ReferenceEquals(m_Toggles[i], instigatingToggle))
                    {
                        m_Toggles[i].SetIsOnIgnoringToggleGroup(newIsOn: false, instant, sendCallback: true);
                    }
                }
            }

            onToggleChanged?.Invoke(this, instigatingToggle);
        }

        public void Refresh()
        {
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField]
        protected bool m_AllowSwitchOff = default;

        // =====================================================================
        // Internal fields
        // =====================================================================

        protected List<IMelcherToggle> m_Toggles = new List<IMelcherToggle>();

    }   // class MelcherToggleGroup

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-20
/// ===========================================================================
