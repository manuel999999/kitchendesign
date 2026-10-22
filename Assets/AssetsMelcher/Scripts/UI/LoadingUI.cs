namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using MEC;
    using DG.Tweening;

    /// <summary>
    /// A pop-up that covers the screen and shows a spinner.
    ///
    /// We use this to block the screen during web requests and to provide
    /// feedback to the player that something is loading.
    /// </summary>
    public class LoadingUI : MelcherUI
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public Sprite loadingSprite
        {
            get => m_LoadingSprite;
            set => m_LoadingSprite = value;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Set the message that is shown down the spinning widget.
        /// </summary>
        public void SetStatusMessage(string statusMessage)
        {
            m_StatusLabel.phrase = statusMessage;
        }

        /// <summary>
        /// Hide the status message label and background on Awake.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            SetStatusMessage("");
        }

        /// <summary>
        /// Show the UI and begin spinning the spinner.
        /// </summary>
        protected override void DoShow()
        {
            base.DoShow();

            if (m_TintBackground)
            {
                m_TintTween.Reset(
                    m_TintBackground.DOAlpha(endValue: 1.0f, m_BackgroundFadeInDuration)
                );
            }

            UpdateLoadingImage();

            StartOrStopSpinnerCoroutine(start: true);
        }

        public void UpdateLoadingImage()
        {
            bool hasLoadingImage = false;

            if (m_LoadingImage)
            {
                m_LoadingImage.sprite = m_LoadingSprite;
                m_LoadingImage.SetAllDirty();
                hasLoadingImage = m_LoadingImage.sprite;
                m_LoadingImage.gameObject.SetActive(hasLoadingImage);
            }

            m_Spinner.gameObject.SetActive(!hasLoadingImage);
        }

        /// <summary>
        /// Hide the UI (clearing the previous status message) and stop the
        /// spinner from spinning.
        /// </summary>
        protected override void DoHide()
        {
            if (m_TintBackground)
            {
                m_TintTween.Reset(
                    m_TintBackground.DOAlpha(endValue: 0.0f, m_BackgroundFadeOutDuration)
                        .OnComplete(FinishHide)
                );
            }
            else
            {
                FinishHide();
            }

            void FinishHide()
            {
                base.DoHide();

                // Hide the previous status message.
                SetStatusMessage("");

                StartOrStopSpinnerCoroutine(start: false);
            }
        }

        protected void StartOrStopSpinnerCoroutine(bool start)
        {
            if (start != m_RotateCoroutine.IsValid)
            {
                if (start)
                {
                    m_RotateCoroutine = Timing.RunCoroutine(SpinSpinner().CancelWith(gameObject));
                }
                else
                {
                    Timing.KillCoroutines(m_RotateCoroutine);
                    m_RotateCoroutine = default;
                }
            }
        }

        /// <summary>
        /// Rotate the spinner widget.
        /// </summary>
        protected IEnumerator<float> SpinSpinner()
        {
            while (true)
            {
                m_Spinner.localEulerAngles += Vector3.back * Time.deltaTime * m_SpinnerDegreesPerSecond;
                yield return Timing.WaitForOneFrame;
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [MelcherHeader]

        [SerializeField] protected CanvasRenderer m_TintBackground = default;

        [Space]
        [SerializeField] protected RectTransform m_Spinner;
        [SerializeField] protected Image m_LoadingImage;
        [SerializeField] protected float m_SpinnerDegreesPerSecond = 120.0f;
        [SerializeField] protected float m_BackgroundFadeInDuration = 0.2f;
        [SerializeField] protected float m_BackgroundFadeOutDuration = 0.2f;

        [Space]
        [Tooltip("A label that can be shown to indicate what part of the " +
            "load process is happening. E.g., \"Loading user profile\"")]
        [SerializeField] protected MelcherLocalizedText m_StatusLabel = default;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected CoroutineHandle m_RotateCoroutine;
        protected RecyclableTween<Tween> m_TintTween = new RecyclableTween<Tween>();

        /// <summary>
        ///     An optional sprite to show when this UI is visible. If this
        ///     is <see langword="null"/>, a spinner is shown instead.
        /// </summary>
        protected Sprite m_LoadingSprite;

    }   // class HomeUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
