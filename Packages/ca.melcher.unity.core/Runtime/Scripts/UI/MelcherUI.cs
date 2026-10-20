namespace Melcher
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Base class for canvases that will be enabled/disabled (e.g. main menu,
    /// log in screen, options menu, etc.). This class is designed to disable
    /// the Canvas component rather than disable the Canvas' game object
    /// because this avoids the layout calculation if the game object is
    /// re-enabled.
    ///
    /// The basic usage of this class is to call <see cref="Show"/> and pass in
    /// an optional transition and callback. The transition is accessible from
    /// <see cref="DoShow"/> and <see cref="DoHide"/>. Child classes can
    /// override these functions to build custom transition effects. The
    /// callback is invoked when the menu has finished hiding.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MelcherUI : MelcherMonoBehaviour
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public CanvasGroup canvasGroup => m_CanvasGroup;

        // =====================================================================
        // Methods
        // =====================================================================

        public RectTransform GetRectTransform()
        {
            return m_RectTransform;
        }

        /// <summary>
        /// If the UI is shown and hasn't been hid yet, return true; otherwise,
        /// return false.
        /// </summary>
        public bool IsVisible()
        {
            if (m_Visibility == Visibility.NotSet)
            {
                return m_ShowOnStart;
            }

            return m_Visibility == Visibility.Shown;
        }

        /// <summary>
        /// If the UI is visible and interactable, return true; otherwise,
        /// return false.
        /// </summary>
        public bool IsInteractable()
        {
            return m_Canvas.enabled && m_CanvasGroup.interactable;
        }

        /// <summary>
        /// Request this UI to appear.
        /// </summary>
        /// <param name="transition">
        /// Transition parameters.
        /// </param>
        /// <param name="onHidden">
        /// Callback to fire when the UI is eventually hidden.
        /// </param>
        public void Show(MelcherUITransition? transition = null, Action onHidden = null)
        {
            m_Transition = transition.GetValueOrDefault(MelcherUITransition.Default);

            if (onHidden != null)
            {
                m_OnHidden += onHidden;
            }

            // Immediately make the UI interactable so its Selectables state
            // doesn't cause a visual pop later
            m_CanvasGroup.interactable = true;

            DoShow();
        }

        /// <summary>
        /// Request this UI to hide.
        /// </summary>
        /// <param name="onHidden">Callback to fire when the UI is completely
        ///     hidden.
        /// </param>
        /// <param name="transition">Transition parameters.</param>
        public void Hide(MelcherUITransition? transition = null, Action onHidden = null)
        {
            m_Transition = transition.GetValueOrDefault(MelcherUITransition.Default);

            if (onHidden != null)
            {
                m_OnHidden += onHidden;
            }

            m_CanvasGroup.interactable = false;

            DoHide();
        }

        /// <summary>
        /// Request this UI show if hidden, or hide if shown.
        /// 
        /// </summary>
        /// <param name="transition">Transition parameters.</param>
        public void Toggle(MelcherUITransition? transition = null, Action onHidden = null)
        {
            Toggle(
                doShow: (IsVisible() && m_LiveTransitionDirection is Visibility.Hidden) ||
                        (!IsVisible() && m_LiveTransitionDirection is Visibility.NotSet),
                transition: transition,
                onHidden: onHidden
            );
        }

        /// <summary>
        /// Request this UI to show or hide explicitly.
        /// </summary>
        /// <param name="doShow"> Show = true, Hide = false.</param>
        /// <param name="transition">Transition parameters.</param>
        public void Toggle(bool doShow, MelcherUITransition? transition = null, Action onHidden = null)
        {
            if (doShow)
            {
                Show(transition, onHidden);
            }
            else
            {
                Hide(transition, onHidden);
            }
        }

        /// <summary>
        /// Does nothing. Marked as virtual to reserve for future use and make
        /// its implementation consistent (easier to remember) with Start.
        /// </summary>
        protected virtual void Awake()
        {
            // Does nothing
        }

        /// <summary>
        /// Apply m_ShowOnStart.
        /// </summary>
        protected virtual void Start()
        {
            // Initialize the menu's visibility. This happens in Start rather
            // than Awake to ensure the UI has a chance to initialize itself
            // before it is forced into refreshing its rendering.
            if (m_Visibility == Visibility.NotSet)
            {
                SetVisible(m_ShowOnStart);
            }
        }

        /// <summary>
        /// Does nothing. Marked as virtual to reserve for future use and make
        /// its implementation consistent (easier to remember) with OnDisable.
        /// </summary>
        protected virtual void OnEnable()
        {
            // Does nothing
        }

        /// <summary>
        /// Free up delegate memory.
        /// </summary>
        protected virtual void OnDisable()
        {
            m_OnHidden = null;
        }

        /// <summary>
        /// User-defined implementation of UI appear effect. By default, this
        /// just makes the canvas visible.
        /// </summary>
        protected virtual void DoShow()
        {
            SetVisible(true);
        }

        /// <summary>
        /// User-defined implementation of UI hide effect. By default, this
        /// just makes the canvas invisible.
        /// </summary>
        protected virtual void DoHide()
        {
            SetVisible(false);
        }

        /// <summary>
        /// Called whenever the canvas' visibility truly changes. E.g.
        /// consecutive calls to <see cref="Show"/> will only trigger this
        /// function once.
        /// </summary>
        protected virtual void OnVisibilityChanged()
        {
            // Does nothing by default
        }

        /// <summary>
        /// Enable/disable the canvas and raycaster (if it exists). If the
        /// canvas was made invisible, notify all onUiHidden listeners and
        /// clear the listener list.
        /// </summary>
        protected void SetVisible(bool isVisible)
        {
            bool wasShown = IsVisible();

            m_Visibility = isVisible ? Visibility.Shown : Visibility.Hidden;

            m_Canvas.enabled = isVisible;

            m_CanvasGroup.interactable = isVisible;

            if (m_Raycaster)
            {
                m_Raycaster.enabled = isVisible;
            }

            if (wasShown && !isVisible)
            {
                m_OnHidden?.Invoke();
                m_OnHidden = null;
            }

            if (TryGetComponent(out MelcherUIExplode explode))
            {
                // Unexplode if shown. Explode if hidden.
                explode.SetExploded(!isVisible);

                // In the Unity editor, the UI is kept visible permanently. This
                // means it is always ready to be selected in the scene view
                if (Application.isEditor)
                {
                    if (m_ShowInSceneViewInEditor)
                    {
                        m_Canvas.enabled = true;
                    }
                }
            }
        }

        // =====================================================================
        // Inspector-serialized members
        // =====================================================================

        [Header(nameof(MelcherUI))]
        [UnityEngine.Serialization.FormerlySerializedAs("m_ShowOnAwake")]
        [SerializeField]
        protected bool m_ShowOnStart = false;

        [SerializeField]
        protected bool m_ShowInSceneViewInEditor = false;

        // =====================================================================
        // OnValidate-serialized members
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected RectTransform m_RectTransform = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Canvas m_Canvas = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected CanvasGroup m_CanvasGroup = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(errorIfFewerThan: 0)]
        protected GraphicRaycaster m_Raycaster = null;

        // =====================================================================
        // Non-serialized members
        // =====================================================================

        /// <summary>
        ///     The UI's visibility state.
        ///     <br /><br />
        ///
        ///     This is set to <see cref="Visibility.Shown"/> after <see
        ///     cref="Show"/> is called.
        ///     <br /><br />
        ///
        ///     This is set to <see cref="Visibility.Hidden"/> sometime after
        ///     <see cref="Hide"/> is called.
        /// </summary>
        /// <remarks>
        ///     The UI can be "visible" when it is hidden. This happens in the
        ///     editor when UI is hidden; it explodes and remains literally
        ///     visible to retain high accessibility for editing. In this state
        ///     the UI is still internally marked as Hidden.
        /// </remarks>
        protected Visibility m_Visibility;

        /// <summary>
        /// If child classes are doing timed transitions, we can check this
        /// state, like if we want to <see cref="Toggle"/> while a transition
        /// is currently happening.
        /// </summary>
        protected Visibility m_LiveTransitionDirection;

        /// <summary>
        /// Actions to trigger when the UI has completely hidden.
        /// </summary>
        protected Action m_OnHidden;

        protected MelcherUITransition m_Transition;

        // =====================================================================
        // Nested types
        // =====================================================================

        protected enum Visibility
        {
            NotSet,

            Hidden,

            Shown
        }   // enum Visibility

    }   // class MelcherUI

}   // namespace Melcher
