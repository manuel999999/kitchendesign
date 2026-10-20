namespace Melcher
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Stores a queue of events that will clear once executed.
    /// </summary>
    public class MelcherEventQueue
    {
        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        /// Add an action to the end of the event queue so it is executed last.
        /// </summary>
        public void EnqueueLast(Action action)
        {
            if (action != null)
            {
                m_NextQueue.AddLast(action);
            }
        }

        /// <summary>
        /// Add an action to the start of the queue so it is executed first.
        /// </summary>
        public void EnqueueFirst(Action action)
        {
            if (action != null)
            {
                m_NextQueue.AddFirst(action);
            }
        }

        /// <summary>
        /// Execute all the events that were queued.
        /// </summary>
        public void Execute()
        {
            // The previous executing queue should be empty
            if (m_ExecutingQueue.Count > 0)
            {
                throw new Exception("Trying to execute an EventQueue that was already executing.");
            }

            // Swap lists: the Next queue will become the Executing queue.
            MelcherCommon.Swap(ref m_NextQueue, ref m_ExecutingQueue);

            while (m_ExecutingQueue.Count > 0)
            {
                Action action = m_ExecutingQueue.First.Value;
                m_ExecutingQueue.RemoveFirst();

                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        // ====================================================================
        // Internal members
        // ====================================================================

        /// <summary>
        /// The queue that is executing or has already executed.
        /// </summary>
        private LinkedList<Action> m_ExecutingQueue = new LinkedList<Action>();

        /// <summary>
        /// The queue that is being prepared for the next execute.
        /// </summary>
        private LinkedList<Action> m_NextQueue = new LinkedList<Action>();

    }   // class MelcherEventQueue

}   // namespace Melcher
