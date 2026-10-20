namespace Melcher
{
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;


    public class MelcherButton : MelcherSelectable
    {
        // =====================================================================
        // Events
        // =====================================================================

        public Action<MelcherButton> onClick;

        // =====================================================================
        // Methods
        // =====================================================================

        protected override void DoClick(PointerEventData eventData)
        {
            onClick?.Invoke(this);
        }

    }   // class MelcherButton


    /*
    public class MelcherButton : MelcherMonoBehaviour
        , IPointerEnterHandler
        , IPointerExitHandler
        , IPointerDownHandler
        , IPointerUpHandler
        , IPointerClickHandler
    {
        // ====================================================================
        // Events
        // ====================================================================

        public event Action<MelcherButton> onClick;
        public event Action<ButtonAppearance, PointerEventData> onAppearanceChange;

        // ====================================================================
        // Properties
        // ====================================================================

        public bool interactable
        {
            get => m_Interactable;
            set
            {
                if (m_Interactable != value)
                {
                    SetInteractable(value);
                }
            }
        }

        // ====================================================================
        // Methods
        // ====================================================================

        public RectTransform GetRectTransform()
        {
            return m_RectTransform;
        }

        public ButtonAppearance GetAppearance()
        {
            var appearance = ButtonAppearance.Normal;

            if (!m_Interactable)
            {
                appearance = ButtonAppearance.NonInteractable;
            }
            else if (m_PointerFlags.HasFlag(PointerFlag.Entered | PointerFlag.Down))
            {
                appearance = ButtonAppearance.Pressed;
            }
            else if (m_PointerFlags.HasFlag(PointerFlag.Entered))
            {
                appearance = ButtonAppearance.Hovered;
            }

            return appearance;
        }

        protected virtual void SetInteractable(bool isInteractable)
        {
            if (m_Interactable != isInteractable)
            {
                m_Interactable = isInteractable;

                onAppearanceChange?.Invoke(GetAppearance(), null);
            }
        }


        // ====================================================================
        // IPointerDownHandler
        // ====================================================================
        #region IPointerDownHandler
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            eventData.Use();

            m_PointerFlags |= PointerFlag.Down;

            if (m_Interactable)
            {
                onAppearanceChange?.Invoke(ButtonAppearance.Pressed, eventData);
            }
        }

        #endregion  // IPointerDownHandler

        // ====================================================================
        // IPointerUpHandler
        // ====================================================================
        #region IPointerUpHandler
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            eventData.Use();

            m_PointerFlags &= ~PointerFlag.Down;

            if (m_Interactable)
            {
                onAppearanceChange?.Invoke(
                    m_PointerFlags.HasFlag(PointerFlag.Entered) ? ButtonAppearance.Hovered : ButtonAppearance.Normal,
                    eventData
                );
            }
        }

        #endregion  // IPointerUpHandler

        // ====================================================================
        // IPointerClickHandler
        // ====================================================================
        #region IPointerClickHandler
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();

            if (m_Interactable)
            {
                onClick?.Invoke(this);
            }
        }

        #endregion  // IPointerClickHandler

        // ====================================================================
        // IPointerEnterHandler
        // ====================================================================
        #region IPointerEnterHandler
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            eventData.Use();

            m_PointerFlags |= PointerFlag.Entered;

            if (m_Interactable)
            {
                ButtonAppearance appearance = ButtonAppearance.Hovered;
                if (m_PointerFlags.HasFlag(PointerFlag.Down))
                {
                    appearance = ButtonAppearance.Pressed;
                }

                onAppearanceChange?.Invoke(appearance, eventData);
            }
        }

        #endregion  // IPointerEnterHandler

        // ====================================================================
        // IPointerExitHandler
        // ====================================================================
        #region IPointerExitHandler
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            eventData.Use();

            m_PointerFlags &= ~PointerFlag.Entered;

            if (m_Interactable)
            {
                onAppearanceChange?.Invoke(ButtonAppearance.Normal, eventData);
            }
        }

        #endregion  // IPointerExitHandler

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [SerializeField] protected bool m_Interactable = true;

        // ====================================================================
        // OnValidate-serialized members
        // ====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected RectTransform m_RectTransform = null;

        // ====================================================================
        // Non-serialized members
        // ====================================================================

        protected PointerFlag m_PointerFlags;

        // ====================================================================
        // Nested types
        // ====================================================================

        public enum ButtonAppearance
        {
            Normal,
            Hovered,
            Pressed,
            NonInteractable
        }   // enum ButtonAppearance

        [Flags]
        public enum PointerFlag
        {
            None = 0,

            /// <summary>
            /// The pointer is hovered over the button's RectTransform
            /// </summary>
            Entered = 1 << 0,

            /// <summary>
            /// The pointer is pressed or held down
            /// </summary>
            Down    = 1 << 1,
        }   // enum ButtonFlag

    }   // class MelcherButton
    */

}   // namespace Melcher
