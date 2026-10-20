namespace Melcher
{
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
        // =====================================================================
        // Methods
        // =====================================================================

        protected override void Awake()
        {
            base.Awake();

            m_ButtonLabel = MelcherSearch.FirstFromTransformDownward<MelcherLocalizedText>(m_Button.transform);
            m_CountdownLabelHourFormat = m_ButtonLabel.phrase;
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

            m_PeriodicCountdownRefreshRoutine = Timing.RunCoroutine(PeriodicCountdownRefresh());
        }

        /// <summary>
        /// Stop the coroutine handling the periodic label refresh.
        /// </summary>
        protected override void DoHide()
        {
            base.DoHide();

            Timing.KillCoroutines(m_PeriodicCountdownRefreshRoutine);
            m_PeriodicCountdownRefreshRoutine = default;
        }

        protected void OnClickButton(MelcherButton caller)
        {
            ProFitUtility.OpenSubscriptionLandingPage();
        }

        protected IEnumerator<float> PeriodicCountdownRefresh()
        {
            do
            {
                System.DateTime trialExpireDate = ProFitGlobals.userProfile.trialExpireUtc.GetValueOrDefault();
                System.DateTime now = System.DateTime.UtcNow;

                System.TimeSpan timeDifference = trialExpireDate - now;

                int totalHours = Mathf.FloorToInt((float)timeDifference.TotalHours);

                if (totalHours > 1)
                {
                    m_ButtonLabel.phrase = m_CountdownLabelHourFormat.Replace("%HOURS%", totalHours.ToString());
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

        protected CoroutineHandle m_PeriodicCountdownRefreshRoutine;

        protected string m_CountdownLabelHourFormat;

    }   // class TrialCountdownUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
