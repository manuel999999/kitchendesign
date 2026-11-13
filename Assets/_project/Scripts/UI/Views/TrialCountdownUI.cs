namespace KitchenDesign.UI.Views
{
    using KitchenDesign.Utility;
    using KitchenDesign.Session;
    using KitchenDesign.Scriptables;
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;
    using MEC;

    /// <summary>
    /// An on-screen countdown of the user's remaining trial time. This does
    /// not handle the actual kicking of the user. For that, see <see
    /// cref="LogInUI"/>.
    /// </summary>
    public class TrialCountdownUI : MelcherUI
    {
        private SessionStatus _session;
        private AppOptionsSO _options;

        public void Initialize(SessionStatus session, AppOptionsSO options)
        {
            this._session = session;
            this._options = options;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        protected override void Awake()
        {
            base.Awake();

            m_ButtonLabel = MelcherSearch.FirstFromTransformDownward<MelcherLocalizedText>(m_Button.transform);
            _countdownLabelHourFormat = m_ButtonLabel.phrase;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Button.onClick += OnClickButton;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_Button.onClick -= OnClickButton;
        }

        /// <summary>
        /// Start a coroutine to periodically update the countdown label's
        /// text.
        /// </summary>
        protected override void DoShow()
        {
            base.DoShow();

            _periodicCountdownRefreshRoutine = Timing.RunCoroutine(PeriodicCountdownRefresh());
        }

        /// <summary>
        /// Stop the coroutine handling the periodic label refresh.
        /// </summary>
        protected override void DoHide()
        {
            base.DoHide();

            Timing.KillCoroutines(_periodicCountdownRefreshRoutine);
            _periodicCountdownRefreshRoutine = default;
        }

        protected void OnClickButton(MelcherButton caller)
        {
            AppLinks.OpenSubscriptionLandingPage(this._options, this._session.UserProfile.userType);
        }

        protected IEnumerator<float> PeriodicCountdownRefresh()
        {
            do
            {
                System.DateTime trialExpireDate = this._session.UserProfile.trialExpireUtc.GetValueOrDefault();
                System.DateTime now = System.DateTime.UtcNow;

                System.TimeSpan timeDifference = trialExpireDate - now;

                int totalHours = Mathf.FloorToInt((float)timeDifference.TotalHours);

                if (totalHours > 1)
                {
                    m_ButtonLabel.phrase = _countdownLabelHourFormat.Replace("%HOURS%", totalHours.ToString());
                }
                else
                {
                    m_ButtonLabel.phrase = $"Trial ends soon";
                }

                yield return Timing.WaitForSeconds(60.0f);

            } while (true);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [MelcherHeader]

        [SerializeField] protected MelcherButton m_Button = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected MelcherLocalizedText m_ButtonLabel;

        protected CoroutineHandle _periodicCountdownRefreshRoutine;

        protected string _countdownLabelHourFormat;

    }   // class TrialCountdownUI

}   // namespace KitchenDesign.UI.Views

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
