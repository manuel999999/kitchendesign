namespace KitchenDesign.UI.Views
{
    using KitchenDesign.Utility;
    using Melcher;
    using DG.Tweening;

    /// <summary>
    /// Base class for Pro-Fit menus. Menus fit within the header/footer and
    /// have a small slide transition effect.
    /// </summary>
    public class Menu : MelcherUI
    {
        protected override void DoShow()
        {
            base.DoShow();
            MenuTransition.HalfSlideIn(this, m_Transition, _transitionTween, onComplete: null);
        }

        protected override void DoHide()
        {
            MenuTransition.HalfSlideOut(this, m_Transition, _transitionTween, () => SetVisible(false));
        }

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected RecyclableTween<Sequence> _transitionTween = new RecyclableTween<Sequence>();
    }   // class Menu

}   // namespace KitchenDesign.UI.Views

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
