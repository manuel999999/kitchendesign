namespace Melcher
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// This subsystem emits screen resize events. In order to work, it must be
    /// attached to a non-nested Canvas.
    /// </summary>
    [AddComponentMenu("")]
    [ExecuteInEditMode]
    [ExecuteAlways]
    public class MelcherScreenEvents : MelcherSubsystem<MelcherScreenEvents>
    {
        // ====================================================================
        // Events
        // ====================================================================

        /// <summary>
        /// Called when the game's resolution or screen orientation changes.
        /// </summary>
        public delegate void ScreenChangeHandler(ScreenOrientation old, ScreenOrientation now);
        public static event ScreenChangeHandler onChanged;

        // =====================================================================
        // Static functions
        // =====================================================================

        /// <summary>
        ///     Return true if any pointer is over a game object. The check is
        ///     performed using Unity's UGUI method <see
        ///     cref="EventSystem.IsPointerOverGameObject(int)"/> and also
        ///     considers any touch inputs.
        /// </summary>
        /// <param name="onlyCheckTouchPhase">
        ///     Optional filter for touch inputs. If this is not null, only
        ///     touches whose phase matches this value will be considered.
        /// </param>
        public static bool IsAnyPointerOverGameObject(TouchPhase? onlyCheckTouchPhase = TouchPhase.Began)
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem)
            {
                // Handle the mouse pointer
                if (eventSystem.IsPointerOverGameObject())
                {
                    return true;
                }

                // Handle touch inputs
                int touchCount = Input.touchCount;
                for (int i = 0; i < touchCount; ++i)
                {
                    Touch touch = Input.GetTouch(i);

                    if (onlyCheckTouchPhase.HasValue && touch.phase != onlyCheckTouchPhase.Value)
                    {
                        continue;
                    }

                    if (eventSystem.IsPointerOverGameObject(touch.fingerId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void ExecuteWhenReady(Action action)
        {
            if (s_Instance == null)
            {
                throw new InvalidOperationException("Not initialized.");
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (s_Instance.m_UpdateCount >= s_UpdateCountTillReady)
            {
                MelcherLog.Log("Executing now");
                action.Invoke();
            }
            else if (!s_Instance.m_IsExecutingQueuedActions)
            {
                MelcherLog.Log("Queueing");
                s_Instance.m_QueuedActions += action;
            }
            else
            {
                MelcherLog.LogError("Already executing queued actions.");
            }
        }

        // ====================================================================
        // Methods
        // ====================================================================

        public void ForceUpdate()
        {
            m_UpdateQueued = false;
            onChanged?.Invoke(m_LastOrientation, Screen.orientation);
            m_LastOrientation = Screen.orientation;

            if (m_UpdateCount < s_UpdateCountTillReady)
            {
                ++m_UpdateCount;
            }

            if (m_UpdateCount >= s_UpdateCountTillReady &&
                !m_IsExecutingQueuedActions &&
                m_QueuedActions != null)
            {
                m_IsExecutingQueuedActions = true;

                // Reset the "is executing" flag when this scope exits
                using var _ = MelcherDeferred.Defer(
                    () => m_IsExecutingQueuedActions = false
                );

                m_QueuedActions.Invoke();
                m_QueuedActions = null;
            }
        }

        private void Awake()
        {
            gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            m_UpdateQueued = false;
            m_UpdateCount = 0;
        }

        private IEnumerator Start()
        {
            yield return null;

            ForceUpdate();
        }

        /// <summary>
        /// Handle screen changes at the end of the frame.
        /// </summary>
        private void LateUpdate()
        {
            if (m_UpdateQueued || Screen.orientation != m_LastOrientation)
            {
                ForceUpdate();
            }
        }

        /// <summary>
        /// This function is automatically called by Unity whenever the
        /// RectTransform (or Canvas in this case) changes size. This is a
        /// convenient way to catch screen resize events.
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            // Notify listeners that the screen size changed.
            m_UpdateQueued = true;
        }

        // =====================================================================
        // Internal static fields
        // =====================================================================

        private const int s_UpdateCountTillReady = 1;

        // =====================================================================
        // Non-serialized members
        // =====================================================================

        private Action m_QueuedActions;

        private bool m_UpdateQueued;

        private int m_UpdateCount;

        private bool m_IsExecutingQueuedActions;

        private ScreenOrientation m_LastOrientation = default;

    }   // class MelcherScreenEvents

}   // namespace Melcher
