namespace KitchenDesign
{
    using Melcher;
    using System;
    using UnityEngine;

    /// <summary>
    /// A pop-up to select a list sort mode (avoids a drop down control
    /// which is usually a bad experience on mobile)
    /// </summary>
    public class SortModeUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void SetupAndShow(Action<ListControllerSortMode> onChoose)
        {
            m_Callback = onChoose;

            Show();
        }

        protected override void Start()
        {
            m_NameAscendingButton.GetComponentInChildren<MelcherLocalizedText>().phrase = ListControllerSortMode.NameAscending.phrase;
            m_NameDescendingButton.GetComponentInChildren<MelcherLocalizedText>().phrase = ListControllerSortMode.NameDescending.phrase;
            m_DateAscendingButton.GetComponentInChildren<MelcherLocalizedText>().phrase = ListControllerSortMode.DateAscending.phrase;
            m_DateDescendingButton.GetComponentInChildren<MelcherLocalizedText>().phrase = ListControllerSortMode.DateDescending.phrase;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EventBridge.Menu.showSortModeUi += SetupAndShow;

            m_NameAscendingButton.onClick   += SortNameAscending;
            m_NameDescendingButton.onClick  += SortNameDescending;
            m_DateAscendingButton.onClick   += SortDateAscending;
            m_DateDescendingButton.onClick  += SortDateDescending;

            m_CancelButton.onClick += OnClickCancel;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EventBridge.Menu.showSortModeUi -= SetupAndShow;

            m_NameAscendingButton.onClick   -= SortNameAscending;
            m_NameDescendingButton.onClick  -= SortNameDescending;
            m_DateAscendingButton.onClick   -= SortDateAscending;
            m_DateDescendingButton.onClick  -= SortDateDescending;

            m_CancelButton.onClick -= OnClickCancel;
        }

        protected void SortNameAscending(MelcherButton caller)
        {
            DoCallback(caller, ListControllerSortMode.NameAscending);
        }

        protected void SortNameDescending(MelcherButton caller)
        {
            DoCallback(caller, ListControllerSortMode.NameDescending);
        }

        protected void SortDateAscending(MelcherButton caller)
        {
            DoCallback(caller, ListControllerSortMode.DateAscending);
        }

        protected void SortDateDescending(MelcherButton caller)
        {
            DoCallback(caller, ListControllerSortMode.DateDescending);
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            Hide();
        }

        protected void DoCallback(MelcherButton caller, ListControllerSortMode sortMode)
        {
            m_Callback?.Invoke(sortMode);
            m_Callback = null;

            Hide();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [MelcherHeader]

        [SerializeField] protected MelcherButton m_NameAscendingButton = null;
        [SerializeField] protected MelcherButton m_NameDescendingButton = null;
        [SerializeField] protected MelcherButton m_DateAscendingButton = null;
        [SerializeField] protected MelcherButton m_DateDescendingButton = null;

        [SerializeField] protected MelcherButton m_CancelButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected Action<ListControllerSortMode> m_Callback;

    }   // class SortModeUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
