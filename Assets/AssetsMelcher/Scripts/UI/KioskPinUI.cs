namespace Melcher
{
    using UnityEngine;
    using MEC;
    using UnityEngine.UI;
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     The prompt that shows up to gate access to sensitive app screens.
    /// </summary>
    public class KioskPinUI : MelcherUI
    {
        // ====================================================================
        // Constants
        // ====================================================================

        public const int MaxDigits = 4;

        // =====================================================================
        // Methods
        // =====================================================================

        protected override void Awake()
        {
            base.Awake();

            Clear();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_CancelButton.onClick += OnClickCancel;
            m_ClearButton.onClick += OnClickClear;

            ComponentsInChildrenEnumerator<MelcherButton> pinButtons =
                new(m_PinButtonLayout.transform);

            while (pinButtons.MoveNext())
            {
                MelcherButton pinButton = pinButtons.Current;

                pinButton.onClick += OnClickPinButton;
            }

            EventBridge.Menu.showKioskKeypad += ShowKioskKeypad;
            EventBridge.Menu.verifyKioskPermission += VerifyKioskPermission;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_CancelButton.onClick -= OnClickCancel;
            m_ClearButton.onClick -= OnClickClear;

            ComponentsInChildrenEnumerator<MelcherButton> pinButtons =
                new(m_PinButtonLayout.transform);

            while (pinButtons.MoveNext())
            {
                MelcherButton pinButton = pinButtons.Current;

                pinButton.onClick -= OnClickPinButton;
            }

            EventBridge.Menu.showKioskKeypad -= ShowKioskKeypad;
            EventBridge.Menu.verifyKioskPermission -= VerifyKioskPermission;
        }

        protected override void DoShow()
        {
            base.DoShow();
        }

        protected override void DoHide()
        {
            Clear();

            Timing.KillCoroutines(m_CountdownRoutine);

            base.DoHide();
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            Hide();
        }

        protected void OnClickPinButton(MelcherButton caller)
        {
            if (m_PinInput.Length >= MaxDigits)
            {
                // Auto-clear the input if the user enters a character beyond
                // the limit. This condition is only possible after the user
                // has entered the wrong PIN and was presented a message box
                // indicating such.

                m_PinInput = "";
                m_MessageBox.Clear();
            }

            char digit = caller.name[0];

            m_PinInput += digit;

            m_PinDigits.textMesh.maxVisibleCharacters = m_PinInput.Length;

            if (m_PinDigits.textMesh.maxVisibleCharacters >= MaxDigits)
            {
                TestPin();
            }
        }

        protected void TestPin()
        {
            Timing.RunCoroutine(TestPinCoroutine());

            IEnumerator<float> TestPinCoroutine()
            {
                if (m_PinInput == "3030")
                {
                    // Instantly accept a correct PIN when it is provided and
                    // reset the wrong pin counter so future PIN attempts are
                    // not penalized for this user's failed attempts.

                    m_NumberOfConsecutiveWrongPinAttempts = 0;

                    m_OnSuccessCallback?.Invoke();

                    Hide();

                    yield break;
                }

                ++m_NumberOfConsecutiveWrongPinAttempts;

                if (m_NumberOfConsecutiveWrongPinAttempts > 2)
                {
                    // By the third failed PIN, the user is forced to wait
                    // between PIN attempts. The waits gets progressively
                    // longer for each wrong PIN attempt.

                    using var restoreUi =
                        MelcherDeferred.Defer(
                            () =>
                            {
                                m_CanvasGroup.interactable = true;
                                EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                            }
                        );

                    m_CanvasGroup.interactable = false;
                    EventBridge.Menu.showFullScreenSpinner.Invoke(true);

                    yield return Timing.WaitForSeconds(m_NumberOfConsecutiveWrongPinAttempts);
                }

                m_MessageBox.Error("Incorrect PIN.");
            }
        }

        protected void OnClickClear(MelcherButton caller)
        {
            Clear();
        }

        protected void Clear()
        {
            m_PinInput = "";
            m_PinDigits.textMesh.maxVisibleCharacters = m_PinInput.Length;
            m_MessageBox.Clear();
        }

        /// <inheritdoc cref="EventBridge.Menu.showKioskKeypad"/>
        protected void ShowKioskKeypad(Action onSuccess)
        {
            m_OnSuccessCallback = onSuccess;

            Show();
        }

        /// <inheritdoc cref="EventBridge.Menu.verifyKioskPermission"/>
        protected void VerifyKioskPermission(Action onSuccess)
        {
            if (!ProFitGlobals.isKioskModeEnabled)
            {
                onSuccess?.Invoke();
                return;
            }

            ShowKioskKeypad(onSuccess);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(KioskPinUI))]

        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected MelcherLocalizedText m_PinDigits = null;
        [SerializeField] protected GridLayoutGroup m_PinButtonLayout = null;
        [SerializeField] protected MelcherButton m_CancelButton = null;
        [SerializeField] protected MelcherButton m_ClearButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected CoroutineHandle m_CountdownRoutine;
        protected int m_ClientId;
        protected int m_QuoteId;

        /// <summary>
        ///     The PIN being entered by the user using the keypad interface.
        /// </summary>
        protected string m_PinInput;

        /// <summary>
        ///     Counts the number of times an incorrect PIN has been entered.
        ///     This is used to ramp up the delay between PINs to discourage
        ///     brute force guessing.
        /// </summary>
        protected int m_NumberOfConsecutiveWrongPinAttempts;

        protected Action m_OnSuccessCallback;

    }   // class KioskPinUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-08-28
/// ===========================================================================

