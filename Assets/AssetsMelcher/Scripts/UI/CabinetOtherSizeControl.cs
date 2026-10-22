namespace KitchenDesign
{
    using Melcher;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// This script helps manage a slider value for setting up the cabinet
    /// details. Each slider value step indicates a measurement and has a
    /// name.
    /// </summary>
    public class CabinetOtherSizeControl : MelcherMonoBehaviour
    {
        // =====================================================================
        // Events
        // =====================================================================

        public event Action<Option> onValueChanged;

        // =====================================================================
        // Properties
        // =====================================================================

        public Option value => m_Options[(int)m_ValueSlider.value];

        public IEnumerable<Option> options => m_Options;

        public bool useIsEnabledToggle
        {
            get => m_UseIsEnabledToggle;
            set
            {
                m_UseIsEnabledToggle = value;

                UpdateIsEnabledToggleVisibility();

                // Force the control to update its appearance to the new state.
                OnIsEnabledChanged(m_IsEnabledToggle);
            }
        }

        public MelcherToggle isEnabledToggle => m_IsEnabledToggle;

        // =====================================================================
        // Methods
        // =====================================================================

        public void SelectOption(int index, bool notify)
        {
            m_ValueSlider.SetValueWithoutNotify(index);

            if (notify)
            {
                OnValueChanged(index);
            }
            else
            {
                Option selectedOption = m_Options[index];

                UpdateLabel(selectedOption);
            }
        }

        public int SetOptions(int defaultIndex, params Option[] options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Length <= 0)
            {
                throw new System.ArgumentException("Must provide at least one option.", nameof(options));
            }

            if (defaultIndex < 0 || defaultIndex >= options.Length)
            {
                throw new ArgumentOutOfRangeException("Must be within 0 and options.Length", nameof(defaultIndex));
            }

            m_Options = new Option[options.Length];
            Array.Copy(options, m_Options, options.Length);
            m_ValueSlider.maxValue = m_Options.Length - 1;

            m_ValueSlider.SetValueWithoutNotify(defaultIndex);
            UpdateLabel(m_Options[defaultIndex]);

            return defaultIndex;
        }

        protected void OnEnable()
        {
            m_IsEnabledToggle.onValueChanged += OnIsEnabledChanged;
            m_ValueSlider.onValueChanged.AddListener(OnValueChanged);

            UpdateIsEnabledToggleVisibility();
        }

        protected void OnDisable()
        {
            m_IsEnabledToggle.onValueChanged -= OnIsEnabledChanged;
            m_ValueSlider.onValueChanged.RemoveListener(OnValueChanged);
        }

        protected void OnValueChanged(float sliderValue)
        {
            Option selectedOption = m_Options[(int)sliderValue];

            UpdateLabel(selectedOption);

            onValueChanged?.Invoke(selectedOption);
        }

        protected void UpdateLabel(Option selectedOption)
        {
            if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
            {
                m_ValueLabel.phrase =
                    ProFitUtility.FormatMetersToCentimeters(
                        selectedOption.sizeInMeters
                    );

                return;
            }

            m_ValueLabel.phrase = selectedOption.displayLabel;
        }

        /// <summary>
        ///     Called when the "Is Enabled" toggle value changes. This enables
        ///     the canvas group so the slider control can be changed when the
        ///     toggle is turned on. When the toggle is off, the control is
        ///     disabled and greyed out.
        /// </summary>
        /// <param name="toggle"></param>
        protected void OnIsEnabledChanged(IMelcherToggle toggle)
        {
            bool isToggleOn = toggle.isOn || !m_UseIsEnabledToggle;

            m_IsEnabledCanvasGroup.interactable = isToggleOn;
            m_IsEnabledCanvasGroup.alpha = isToggleOn ? 1.0f : 0.65f;
        }

        protected void UpdateIsEnabledToggleVisibility()
        {
            m_IsEnabledToggle.gameObject.SetActive(m_UseIsEnabledToggle);
        }

        // ====================================================================
        // Static functions
        // ====================================================================

        public static bool IsSizeAlmostEqualToSizeOption(float meters, Option option)
        {
            return MelcherCommon.IsAlmostEqual(meters, option.sizeInMeters, tolerance: 0.001f);
        }

        /// <summary>
        ///     Assign the supported cabinet depth options.
        ///     <br /><br />
        ///
        ///     This is a standalone function because the cabinet depth options
        ///     are displayed in two different scripts and this ensures we only
        ///     have one spot to update if the depths ever change.
        /// </summary>
        public static int SetupCabinetDepthSizes(CabinetOtherSizeControl control, KitchenBuildingMethod buildingMethod)
        {
            if (buildingMethod == KitchenBuildingMethod.Sections)
            {
                // Only one cabinet depth in Sections method.
                return
                    control.SetOptions(
                        defaultIndex: 0,
                        new Option(29.0f)
                    );
            }
            else
            {
                return
                    control.SetOptions(
                        defaultIndex: 2,
                        new Option(24.0f),
                        new Option(26.0f),
                        new Option(28.0f),
                        new Option(30.0f),
                        new Option(32.0f),
                        new Option(34.0f),
                        new Option(36.0f)
                    );
            }
        }

        /// <summary>
        ///     Finds the control's option that is within a (hard-coded)
        ///     threshold of <paramref name="currentSizeInMeters"/> and marks
        ///     that option as selected.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if an option within the threshold was
        ///     found; otherwise, return <see langword="false"/>.
        /// </returns>
        public static bool SelectOptionMatchingCurrentSize(CabinetOtherSizeControl control, float currentSizeInMeters)
        {
            int optionIndex = -1;
            foreach (Option sizeOption in control.options)
            {
                ++optionIndex;

                if (IsSizeAlmostEqualToSizeOption(currentSizeInMeters, sizeOption))
                {
                    control.SelectOption(optionIndex, notify: false);
                    return true;
                }
            }

            return false;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Tooltip("Whether this control should show and use the 'Is Enabled' " +
            "toggle. If it is used, the toggle will render at the top and " +
            "control the interactability of the slider. Otherwise, the " +
            "toggle is hidden and the slider remains interactable.")]
        [SerializeField] protected bool m_UseIsEnabledToggle;

        [Tooltip("A checkbox to enable this control. When the control is " +
            "disabled, a CanvasGroup is used to disable the slider controls.")]
        [SerializeField] protected MelcherToggle m_IsEnabledToggle;

        [Tooltip("The canvas group that is enabled/disabled when the Is " +
            "Enabled Toggle's value changes.")]
        [SerializeField] protected CanvasGroup m_IsEnabledCanvasGroup;

        [Space]

        [Tooltip("The UI label to display the current slider value.")]
        [SerializeField] protected MelcherLocalizedText m_ValueLabel = null;

        [Tooltip("A slider to choose from different measurements.")]
        [SerializeField] protected Slider m_ValueSlider = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected Option[] m_Options;

        // =====================================================================
        // Nested types
        // =====================================================================

        public readonly struct Option
        {
            public Option(float sizeInInches)
            {
                ImperialUnit unit = ImperialUnit.FromTotalInches(sizeInInches);
                displayLabel = unit.ToString();
                sizeInMeters = (float)unit.totalMeters;
            }

            public readonly string displayLabel;
            public readonly float sizeInMeters;
        }   // struct Option

    }   // class CabinetOtherSizeControl

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-31
/// ===========================================================================
