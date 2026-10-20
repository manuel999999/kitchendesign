namespace Melcher
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    ///     Attach this component to UI controls to have them ignore drag
    ///     events. This is useful within sign up screens on mobile where the
    ///     user may attempt to drag the screen but find the drag action is
    ///     stolen by a text input box or other control.
    /// </summary>
    public class DragPassthrough : MelcherMonoBehaviour
        , IBeginDragHandler
        , IDragHandler
        , IEndDragHandler
    {
        // ====================================================================
        // IBeginDragHandler
        // ====================================================================
        #region IBeginDragHandler
        /// <summary>
        /// Forward drag events to drag recipient
        /// </summary>
        public void OnBeginDrag(PointerEventData data)
        {
            GameObject handler =
                ExecuteEvents.ExecuteHierarchy(
                    m_DragRecipient,
                    data,
                    ExecuteEvents.beginDragHandler
                );

            if (handler)
            {
                data.Use();
            }
        }
        #endregion  // IBeginDragHandler

        // ====================================================================
        // IDragHandler
        // ====================================================================
        #region IDragHandler
        /// <summary>
        /// Forward drag events to drag recipient
        /// </summary>
        public void OnDrag(PointerEventData data)
        {
            GameObject handler =
                ExecuteEvents.ExecuteHierarchy(
                    m_DragRecipient,
                    data,
                    ExecuteEvents.dragHandler
                );

            if (handler)
            {
                data.Use();
            }
        }
        #endregion  // IDragHandler

        // ====================================================================
        // IEndDragHandler
        // ====================================================================
        #region IEndDragHandler
        /// <summary>
        /// Forward drag events to drag recipient
        /// </summary>
        public void OnEndDrag(PointerEventData data)
        {
            GameObject handler =
                ExecuteEvents.ExecuteHierarchy(
                    m_DragRecipient,
                    data,
                    ExecuteEvents.endDragHandler
                );

            if (handler)
            {
                data.Use();
            }
        }
        #endregion  // IEndDragHandler

        // ====================================================================
        // Methods
        // ====================================================================

        private void OnEnable()
        {
            if (!m_DragRecipient)
            {
                Transform parent = transform.parent;
                if (!parent)
                {
                    MelcherLog.LogError($"Requires a parent or explicit {nameof(m_DragRecipient)}", this);
                    enabled = false;
                }
                else
                {
                    m_DragRecipient = parent.gameObject;
                }
            }
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Tooltip("Optional target to send drag events to. If left blank, " + 
            "this object's parent will be treated as the recipient.")]
        [SerializeField]
        protected GameObject m_DragRecipient = null;

    }   // class DragPassthrough

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
