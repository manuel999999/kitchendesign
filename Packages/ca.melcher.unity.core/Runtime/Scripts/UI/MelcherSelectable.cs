namespace Melcher
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class MelcherSelectable : Selectable
        , IPointerClickHandler
    {
        // =====================================================================
        // Events
        // =====================================================================

        public delegate void AppearanceChangeHandler(MelcherSelectable selectable, Appearance appearance, bool instant);
        public AppearanceChangeHandler onAppearanceChanged;

        // =====================================================================
        // Methods
        // =====================================================================

        public RectTransform GetRectTransform()
        {
            return m_RectTransform;
        }

        /// <summary>
        ///     Get the control's current appearance.
        /// </summary>
        public Appearance GetAppearance()
        {
            if (!m_LastAppearance.HasValue)
            {
                m_LastAppearance = interactable ? Appearance.Normal : Appearance.NonInteractable;
            }

            return m_LastAppearance.Value;
        }

        public void RefreshAppearance(bool instant = false)
        {
            // Signal to interested parties (using our public enum) that
            // the objects appearance has changed.
            onAppearanceChanged?.Invoke(this, GetAppearance(), instant);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            // Call into the base class's function to allow Unity's built-in
            // transitions to continue to work.
            base.DoStateTransition(state, instant);

            Appearance appearance;

            // Translate Unity's (protected) SelectionState into our custom
            // (and public) Appearance enum. This way, outside classes can
            // query the appearance state and react to it.
            switch (state)
            {
                default:
                case SelectionState.Normal:
                    appearance = Appearance.Normal;
                    break;

                case SelectionState.Highlighted:
                case SelectionState.Selected:
                    appearance = Appearance.Hovered;
                    break;

                case SelectionState.Pressed:
                    appearance = Appearance.Pressed;
                    break;

                case SelectionState.Disabled:
                    appearance = Appearance.NonInteractable;
                    break;
            }

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                // Early return during edit time to avoid invoking appearance
                // change callbacks outside of play mode: We can't guarantee
                // child classes have `onAppearanceChanged` implementations
                // that work during edit time and therefore the most stable
                // solution is to just skip them.
                //
                // The base class, Selectable, is marked [ExecuteAlways] by
                // Unity which helps visualize the control's appearance during
                // edit time. However, it also causes this function to be
                // invoked at edit time which we need to early exit from.

                return;
            }
#endif  // if UNITY_EDITOR

            NotifyAppearanceChanged(appearance, instant);
        }

        protected virtual void NotifyAppearanceChanged(Appearance appearance, bool instant)
        {
            m_LastAppearance = appearance;
            RefreshAppearance(instant);
        }

        protected virtual void DoClick(PointerEventData eventData)
        {
            // Do nothing by default
        }

        // =====================================================================
        // IPointerClickHandler
        // =====================================================================
        #region IPointerClickHandler
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (IsInteractable() &&
                IsActive() &&
                eventData.button == PointerEventData.InputButton.Left)
            {
                DoClick(eventData);
            }
        }
        #endregion  // IPointerClickHandler

        // ====================================================================
        // ISelectHandler
        // ====================================================================
        #region ISelectHandler

        // TODO: Allow buttons to be highlighted on hover after they are
        //       selected. Unity's Selectable class prevents this. -Ryan
        //public override void OnSelect(BaseEventData eventData)
        //{
        //    base.OnSelect(eventData);
        //}

        #endregion  // ISelectHandler

        // =====================================================================
        // OnValidate-serialized members
        // =====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected RectTransform m_RectTransform = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        /// <summary>
        /// The last appearance state this selectable entered.
        /// </summary>
        protected Appearance? m_LastAppearance;

        // =====================================================================
        // Nested types
        // =====================================================================

        // TODO: It turns out `Appearance` is a bad name for this type. After
        //       having some time to use it, it is clear that this should just
        //       be a public version of SelectableState. A robust "appearance"
        //       field is just too nebulous and state-dependent for a base class
        //       to define. Better, is to manage a real "appearance" in child
        //       classes. We can still implement something of a generic
        //       appearance field using an enum object pattern, but it is
        //       important that we also ensure *this* type is just used just as
        //       a public mirror of UnityEngine.UI.SelectableState. -Ryan
        // TODO: Rename to `MelcherSelectableState`?
        public enum Appearance
        {
            Normal,
            Hovered,
            Pressed,
            Selected,
            NonInteractable,

            // Ryan: Removed `ToggleIsOn` and `ToggleIsOff` for the following
            //       reasons:
            //
            //       1. They overwrite other important appearance info, like
            //          whether a control is interactable.
            //
            //       2. ToggleIsOn and IsOff are special cases exclusive to
            //          MelcherToggle. As an analogy, it's almost as if we had
            //          a MelcherTextBox and were trying to encode the content
            //          of the text in the appearance flag. It's cleaner to
            //          handle state-dependent appearance by checking the isOn
            //          field directly in onAppearanceChanged. We can do this
            //          now that MelcherSelectable has a RefreshAppearance()
            //          function.

        }   // enum Appearance

#if UNITY_EDITOR
        // ====================================================================
        // Editor
        // ====================================================================
        #region Editor
        protected override void OnValidate()
        {
            base.OnValidate();

            // This class must derive from Selectable and therefore it must
            // manually call ValidateFields to recreate the functionality of
            // MelcherMonoBehaviour.
            MelcherOnValidateAttribute.ValidateFields(this);
        }

        protected override void Reset()
        {
            base.Reset();

            // Disable transitions by default. This is done because the
            // built-in Selectable transitions are far too limiting. By
            // disabling transitions by default, we hope to encourage users to
            // choose something better. (E.g., build their own "SelectableView"
            // script to manage its transitions).
            transition = Transition.None;

            // Default the selectable's navigation to None. Automatic is almost
            // always a mess and can cause performance issues when Unity tries
            // to render the connections in the editor.
            Navigation newNavigation = navigation;
            newNavigation.mode = Navigation.Mode.None;
            navigation = newNavigation;
        }
        #endregion  // Editor

#endif  // if UNITY_EDITOR

    }   // class MelcherSelectable

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
