namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class HighlightTarget : MelcherMonoBehaviour
        , IPointerClickHandler
    {
        // =====================================================================
        // IPointerClickHandler
        // =====================================================================
        #region IPointerClickHandler

        public void OnPointerClick(PointerEventData eventData)
        {
            MelcherLog.Log("You clicked: " + name, this);
        }

        #endregion  // IPointerClickHandler
    }   // class HighlightTarget

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-31
/// ===========================================================================
