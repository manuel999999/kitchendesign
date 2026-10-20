namespace Melcher
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    ///     This component can be used to allow users to submit a form by
    ///     pressing the Enter key in a text field.
    ///     <br /><br />
    ///
    ///     <see cref="MelcherOnSubmitSelectable"/> is intended to be added to a
    ///     game object that has a submittable <see cref="Selectable"/>. When
    ///     the user submits the <see cref="Selectable"/> (e.g., presses the
    ///     Enter key while it has focus), this script will trigger an event
    ///     in a different <see cref="Selectable"/>.
    /// </summary>
    public class MelcherOnSubmitSelectable : MelcherMonoBehaviour
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public Selectable selectableToTarget => m_SelectableToTarget;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Invoke the event on the targeted selectable. E.g. call the
        ///     PointerClick handler to execute a UI button's onClick event.
        /// </summary>
        public bool Trigger(EventSystem events)
        {
            if (!DoesTargetExist(warn: true))
            {
                return false;
            }

            if (!(enabled || m_SelectableToTarget.gameObject.activeInHierarchy && m_SelectableToTarget.enabled))
            {
                // Do not execute if the targeted object is inactive or we are
                // inactive
                return false;
            }

            PointerEventData submitEventData = new PointerEventData(events);
            bool didTrigger = false;

            switch (m_HandlerType)
            {
                case HandlerType.PointerClick:
                    didTrigger = ExecuteEvents.Execute(selectableToTarget.gameObject, submitEventData, ExecuteEvents.pointerClickHandler);
                    break;

                case HandlerType.PointerDown:
                    didTrigger = ExecuteEvents.Execute(selectableToTarget.gameObject, submitEventData, ExecuteEvents.pointerDownHandler);
                    break;

                case HandlerType.Submit:
                    didTrigger = ExecuteEvents.Execute(selectableToTarget.gameObject, submitEventData, ExecuteEvents.submitHandler);
                    break;

                default:
                    MelcherLog.LogError($"Unhandled event type: {m_HandlerType}", this);
                    break;
            }

            return didTrigger;
        }

        protected void Awake()
        {
            DoesTargetExist(m_WarnIfTargetIsNullAtAwake);
        }

        protected bool DoesTargetExist(bool warn)
        {
            if (warn && !m_SelectableToTarget)
            {
                MelcherLog.LogWarning($"{this} does not have a target", this);
            }
            return m_SelectableToTarget;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [UnityEngine.Serialization.FormerlySerializedAs("m_SelectableToTrigger")]
        [SerializeField] protected Selectable m_SelectableToTarget = null;
        [SerializeField] protected HandlerType m_HandlerType = HandlerType.Submit;
        [Tooltip("When enabled, a warning will be displayed if the target is " +
            "null when")]
        [SerializeField] protected bool m_WarnIfTargetIsNullAtAwake = true;

        // ====================================================================
        // Nested types
        // ====================================================================

        protected enum HandlerType
        {
            [InspectorName(nameof(PointerClick) + " (UI Button)")]
            PointerClick,

            PointerDown,

            Submit,

        }   // enum HandlerType

    }   // class MelcherOnSubmitSelectable

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-24
/// ===========================================================================
