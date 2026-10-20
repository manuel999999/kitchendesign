namespace Melcher
{
    /// <summary>
    ///     This struct helps prevent runaway loops.
    /// </summary>
    /// <example>
    ///     Example `while` loop:
    ///
    ///     <code>
    ///         MelcherSafeLoop safeLoop = MelcherSafeLoop.Start();
    ///
    ///         while (safeLoop.While(IsProcessRunning()))
    ///         {
    ///
    ///         }
    ///     </code>
    ///
    ///     Example `for` loop:
    ///
    ///     <code>
    ///         MelcherSafeLoop safeLoop = MelcherSafeLoop.Start(10000);
    ///
    ///         for (LinkedListNode<object> node = head; safeLoop.While(node != null); node = node.Next)
    ///         {
    ///
    ///         }
    ///     </code>
    /// </example>
    public struct MelcherSafeLoop
    {
        // =====================================================================
        // Factory functions
        // =====================================================================

        /// <summary>
        ///     Create a <see cref="MelcherSafeLoop"/> object. Call this before
        ///     you intend to start a loop that may undesirably execute
        ///     forever.
        ///     <br /><br />
        ///
        ///     It starts with an internal counter set to 0 and a default max
        ///     counter of 1000.
        /// </summary>
        public static MelcherSafeLoop WithMaxIterations(int maxIterations = 1000)
        {
            return new MelcherSafeLoop(maxIterations);
        }

        // =====================================================================
        // Properties
        // =====================================================================

        public int iteration => m_IterationCurrent;
        public int iterationMax => m_IterationMax;

        public bool isRunaway => iteration >= iterationMax;

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Check if the loop has exceeded its runaway limit. Call this
        ///     in place of your loop's condition and pass it your loops
        ///     normal condition.
        ///     <br /><br />
        ///
        ///     If the iteration count exceeded the runaway limit, an error
        ///     message is logged to signal that a runaway loop was detected
        ///     and the function returns false to end the loop. Otherwise, the
        ///     function returns whatever value it received in its argument.
        ///     <br /><br />
        /// </summary>
        public bool While(bool condition)
        {
            if (m_IterationCurrent >= m_IterationMax)
            {
                MelcherLog.LogError("Runaway while loop detected.");
                return false;
            }

            ++m_IterationCurrent;

            return condition;
        }

        // =====================================================================
        // Private constructor
        // =====================================================================

        // This constructor is hidden simply to keep this type's API simple.
        // Users of this type should call Start() to create an instance.
        private MelcherSafeLoop(int maxIterations)
        {
            m_IterationMax = maxIterations;
            m_IterationCurrent = 0;
        }

        // =====================================================================
        // Member fields
        // =====================================================================

        private int m_IterationMax;
        private int m_IterationCurrent;

    }   // struct MelcherSafeLoop

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-14
/// ===========================================================================
