namespace KitchenDesign
{
    using Melcher;
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    ///     A generic button that anchors to a cabinet and appears when the
    ///     cabinet is selected. It currently has one use case which is to
    ///     open a textbox for the user to type in the cabinet's size.
    /// </summary>
    public class ButtonWidget : KitchenWidget
        , IPointerClickHandler
    {
        public delegate void OnPointerClickedHandler(ButtonWidget caller);
        public event OnPointerClickedHandler onClicked;

        // =====================================================================
        // IPointerClickHandler
        // =====================================================================
        #region IPointerClickHandler

        public void OnPointerClick(PointerEventData eventData)
        {
            onClicked?.Invoke(this);
        }

        #endregion  // IPointerClickHandler

    }   // class ButtonWidget

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-04-16
/// ===========================================================================
