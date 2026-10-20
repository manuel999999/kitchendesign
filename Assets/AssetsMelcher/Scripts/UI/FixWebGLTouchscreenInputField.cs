namespace Melcher
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using TMPro;
    using MEC;

    /// <summary>
    /// There is an issue with WebGL builds running in a desktop browser with a
    /// touchscreen. For some reason, input fields cannot receive focus. They
    /// animate from the initial touch, but fail to actually activate and
    /// accept keyboard input. This component is a kludge to fix that and
    /// should be attached to all TMP_TextInputField components.
    /// </summary>
    public class FixWebGLTouchscreenInputField : MelcherMonoBehaviour
        , IPointerUpHandler
    {
        public void Awake()
        {
            // This is only needed in WebGL builds so disable it if it isn't
            bool isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;

            if (!isWebGL)
            {
                Destroy(this);
                return;
            }

            // Avoid a long-standing issue with TMP on web: Touch screen
            // selections in an email address field throw an exception that
            // kills our app.
            // https://discussions.unity.com/t/textmeshpro-input-field-webgl-error-on-touch/919155
            if (m_InputField.contentType == TMP_InputField.ContentType.EmailAddress)
            {
                m_InputField.contentType = TMP_InputField.ContentType.Standard;
            }

            // TODO: This may be unnecessary. It was added among other
            //       changes and worked so it is kept
            m_InputField.shouldHideMobileInput = false;
            m_InputField.shouldHideSoftKeyboard = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            bool isInputFromTouch = eventData.pointerId >= 0;

            // There is an issue with touch-based inputs on WebGL where input
            // boxes are not selected. This code forces the input box to be
            // selected by touch events
            if (isInputFromTouch)
            {
                Timing.CallDelayed(
                    delay: 0.05f,
                    action: () =>
                    {
                        m_InputField.ActivateInputField();
                    }
                );
            }
        }

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected TMP_InputField m_InputField = default;

    }   // class ForceSelect

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-01-11
/// ===========================================================================
