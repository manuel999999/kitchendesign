namespace KitchenDesign
{
    using Melcher;
    using DG.Tweening;

    /// <summary>
    /// A RecyclableTween is a wrapper for a DOTween Tweenable (Tween or Sequence)
    /// that automatically clears its reference when the tween is killed. This
    /// makes it suitable for handling tweens when tween recycling is enabled.
    /// </summary>
    /// <remarks>
    /// IMPORTANT:</br>
    /// Tweens that are wrapped in a RecyclableTween should only be killed
    /// (manually, automatic kill is fine) by calling <see cref="KillIfActive"/>.
    /// This is because <see cref="KillIfActive"/> correctly handles/kills
    /// tweens that are created inside a <see cref="TweenCallback"/>.
    ///
    /// These type of "nested" tweens have their <see cref="Tween.onComplete"/>
    /// and <see cref="Tween.onKill"/> callbacks delayed until the next DOTween
    /// update cycle. This delay causes an issue where trying to recycle a
    /// tween (e.g. kill and reassign) that has this delay will result in the
    /// callbacks being triggered unexpectedly when the next eventual DOTween
    /// update tick occurs.
    ///
    /// In short, use <see cref="KillIfActive"/>.
    /// </remarks>
    public class RecyclableTween<TweenType>
        where TweenType : Tween
    {
        // =====================================================================
        // Internal members
        // =====================================================================

        private TweenType m_Value;

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        /// Get the wrapped <see cref="Tween"/> or <see cref="Sequence"/>. Do
        /// not kill the tween using this value directly. Use
        /// <see cref="KillIfActive"/> instead.
        /// </summary>
        public TweenType value => m_Value;

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Return true if tween reference is set and the tween is active.
        /// Otherwise, return false.
        /// </summary>
        public bool IsActive()
        {
            return m_Value != null && m_Value.IsActive();
        }

        /// <summary>
        /// Kill the tween if it is active. Otherwise, do nothing.
        /// </summary>
        public void KillIfActive(bool complete = false)
        {
            if (IsActive())
            {
                // Disable the automatic clear callback
                var tween = m_Value;
                tween.onKill -= Clear;

                // Immediately clear the value
                m_Value = null;

                tween.Kill(complete);
            }
        }

        /// <summary>
        /// Assign a new tween to be managed. When the tween is killed, its
        /// reference within this object will automatically be cleared. This
        /// function will kill any tweenable that was previously assigned to
        /// this object.
        /// </summary>
        /// <param name="tween">New value for the reference.</param>
        /// <param name="completePrevious">Whether the pre-existing tween
        ///     should be completed when it is killed.
        /// </param>
        public void Reset(TweenType tween, bool completePrevious = false)
        {
            KillIfActive(completePrevious);

            m_Value = tween;
            m_Value.onKill += Clear;
        }

        /// <summary>
        /// Called when the tween is killed to clear the reference.
        /// </summary>
        private void Clear()
        {
            m_Value = null;
        }

    }   // class RecyclableTween

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2021-12-06
/// ===========================================================================
