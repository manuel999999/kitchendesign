namespace KitchenDesign
{
    using Melcher;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    ///     A pop-up for the user to select one option out of many for the
    ///     user's profile. This avoids the need for a drop down control
    ///     which is usually a bad experience on mobile.
    /// </summary>
    public class EditUserProfileMultipleChoiceUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void SetupAndShow(IEnumerable<string> choices, EventBridge.Menu.OnMultipleChoiceSelected onChoose, string title)
        {
            m_Title.phrase = title;

            ComponentsInChildrenEnumerator<MelcherButton> buttonEnumerator =
                new (m_MultipleChoiceListRoot);

            int buttonCount = 0;

            while (buttonEnumerator.MoveNext())
            {
                // Hide all the options (any buttons that are used will be
                // enabled later)
                buttonEnumerator.Current.gameObject.SetActive(false);

                ++buttonCount;
            }

            buttonEnumerator.Reset();

            foreach (string choice in choices)
            {
                if (!buttonEnumerator.MoveNext())
                {
                    MelcherLog.LogError($"More choices provided than buttons available. Provided {choices.Count()} choices but only {buttonCount} buttons exist.", this);

                    break;
                }

                MelcherLocalizedText buttonLabel =
                    buttonEnumerator.Current.GetComponentInChildren<MelcherLocalizedText>();

                buttonLabel.phrase = choice;

                // Make this button visible because it is in use.
                buttonEnumerator.Current.gameObject.SetActive(true);
            }

            m_Callback = onChoose;

            Show();
        }

        protected override void Start()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EventBridge.Menu.showMultipleChoiceUi += SetupAndShow;

            ComponentsInChildrenEnumerator<MelcherButton> choices =
                new (m_MultipleChoiceListRoot);

            while (choices.MoveNext())
            {
                choices.Current.onClick += OnChoiceSelected;
            }

            m_CancelButton.onClick += OnClickCancel;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EventBridge.Menu.showMultipleChoiceUi -= SetupAndShow;

            ComponentsInChildrenEnumerator<MelcherButton> choices =
                new (m_MultipleChoiceListRoot);

            while (choices.MoveNext())
            {
                choices.Current.onClick -= OnChoiceSelected;
            }

            m_CancelButton.onClick -= OnClickCancel;
        }

        protected void OnChoiceSelected(MelcherButton caller)
        {
            int choiceIndex = caller.GetRectTransform().GetSiblingIndex();

            DoCallback(choiceIndex);
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            DoCallback(null);
        }

        protected void DoCallback(int? choiceIndex)
        {
            m_Callback?.Invoke(choiceIndex);
            m_Callback = null;

            Hide();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [MelcherHeader]

        [SerializeField]
        protected MelcherLocalizedText m_Title = default;

        [SerializeField]
        protected RectTransform m_MultipleChoiceListRoot = default;

        [SerializeField]
        protected MelcherButton m_CancelButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected EventBridge.Menu.OnMultipleChoiceSelected m_Callback;

    }   // class EditUserProfileMultipleChoiceUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
