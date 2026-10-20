namespace Melcher
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class MelcherToggle : MelcherSelectable
        , IMelcherToggle
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public MelcherToggleGroup toggleGroup
        {
            get => m_ToggleGroup;

            set
            {
                if (value != m_ToggleGroup)
                {
                    m_ToggleGroup?.UnregisterToggle(this);

                    m_ToggleGroup = value;

                    m_ToggleGroup?.RegisterToggle(this);
                }
            }
        }

        // =====================================================================
        // Methods
        // =====================================================================

        protected override void Awake()
        {
            base.Awake();

            m_ToggleGroup?.RegisterToggle(this);
        }

        protected override void OnDestroy()
        {
            m_ToggleGroup?.UnregisterToggle(this);

            base.OnDestroy();
        }

        /// <summary>
        ///     This is protected because it is intended to be invoked through
        ///     the <see cref="IMelcherToggle.isOn"/> property.
        /// </summary>
        protected void SetIsOn(bool newIsOn)
        {
            if (m_ToggleGroup)
            {
                m_ToggleGroup.ChangeToggleIsOn(this, newValue: newIsOn);
            }
            else
            {
                SetIsOnIgnoringToggleGroup(newIsOn);
            }
        }

        protected override void DoClick(PointerEventData eventData)
        {
            SetIsOn(!isOn);
        }


        // =====================================================================
        // IMelcherToggle
        // =====================================================================
        #region IMelcherToggle

        public event IMelcherToggle.OnValueChangedHandler onValueChanged;

        public bool isOn
        {
            get => m_IsOn;
            set => SetIsOn(value);
        }

        public void SetIsOnIgnoringToggleGroup(bool newIsOn, bool instant = false, bool sendCallback = true)
        {
            // TODO: This needs to handle if it belongs to a toggle group. -Ryan
            if (newIsOn != m_IsOn)
            {
                m_IsOn = newIsOn;

                RefreshAppearance(instant);

                if (sendCallback)
                {
                    onValueChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        ///     Set the toggle's <see cref="isOn"/> value *without* invoking
        ///     its <see cref="onValueChanged"/> event. Note that this still
        ///     invokes the <see cref="MelcherSelectable.onAppearanceChanged"/>
        ///     event so listeners can update to reflect the new value visually.
        /// </summary>
        /// <param name="newIsOn">
        ///     <see langword="true"/> to make the toggle checked; <see
        ///     langword="false"/> to make the toggle unchecked.
        /// </param>
        /// <param name="instant">
        ///     Whether the selectable's transition should be instant. This is
        ///     <see langword="false"/> by default. This value is passed to
        ///     the <see cref="MelcherSelectable.onAppearanceChanged"/> handler
        ///     so it is up the implementation to handle it appropriately.
        /// </param>
        public void SetIsOnWithoutNotify(bool newIsOn, bool instant = false)
        {
            if (newIsOn != m_IsOn)
            {
                m_IsOn = newIsOn;
                RefreshAppearance(instant);
            }
        }

        #endregion  // IMelcherToggle

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField]
        protected MelcherToggleGroup m_ToggleGroup = default;

        [Tooltip("Whether this toggle is checked (true) or unchecked (false).")]
        [SerializeField]
        protected bool m_IsOn = default;

    }   // class MelcherToggle

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-20
/// ===========================================================================
