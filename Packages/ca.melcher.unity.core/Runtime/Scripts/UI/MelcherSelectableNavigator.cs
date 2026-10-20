namespace Melcher
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

    /// <summary>
    ///     Handles navigation between <see cref="Selectable"/>
    /// </summary>
    public class MelcherSelectableNavigator : MelcherMonoBehaviour
    {
        // ====================================================================
        // Static properties
        // ====================================================================

        /// <summary>
        ///     When this is <see langword="true"/>, the app will quit when the
        ///     device's back button is pressed (Android & Windows phone only).
        ///     <br /><br />
        ///
        ///     This is a custom version of <see cref="Input.backButtonLeavesApp"/>
        ///     that supports being unset. Unity's built-in version treats its
        ///     value as always being true once it has been set.
        /// </summary>
        public static bool backButtonLeavesApp;

        // ====================================================================
        // Methods
        // ====================================================================

        protected void Update()
        {
            EventSystem eventSystem = EventSystem.current;

#if UNITY_ANDROID || UNITY_WSA
            // Recreate Unity's <see cref="Input.backButtonLeavesApp"/>. I.e.,
            // the Back button (KeyCode.Escape) on Android & Windows Phone quits
            // the app.
            if (backButtonLeavesApp && Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
#endif  // if UNITY_ANDROID || UNITY_WSA

            if (eventSystem.currentSelectedGameObject &&
                eventSystem.currentSelectedGameObject.TryGetComponent(out Selectable currentSelectable))
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    // Tab navigation
                    HandleNavigationEvent(currentSelectable);
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    // Enter key to trigger another selectable
                    if (currentSelectable.TryGetComponent(out MelcherOnSubmitSelectable proxy))
                    {
                        proxy.Trigger(eventSystem);
                    }
                }
            }
        }

        private static void HandleNavigationEvent(Selectable currentSelectable)
        {
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            Selectable nextSelectable;

            if (reverse)
            {
                nextSelectable = currentSelectable.FindSelectableOnLeft();

                if (!nextSelectable)
                {
                    nextSelectable = currentSelectable.FindSelectableOnUp();
                }

                // Wrap around to bottom
                if (nextSelectable == null)
                {
                    Selectable nextNextSelectable = currentSelectable;
                    do
                    {
                        nextSelectable = nextNextSelectable;

                        nextNextSelectable =
                            nextSelectable.FindSelectableOnDown();

                        if (!nextNextSelectable)
                        {
                            nextNextSelectable = nextSelectable.FindSelectableOnRight();
                        }

                        if (nextNextSelectable == currentSelectable)
                        {
                            // Cyclical loop
                            break;
                        }
                    }
                    while (nextNextSelectable);
                }
            }
            else
            {
                nextSelectable = currentSelectable.FindSelectableOnRight();

                if (!nextSelectable)
                {
                    nextSelectable = currentSelectable.FindSelectableOnDown();
                }

                // Wrap around to top
                if (nextSelectable == null)
                {
                    Selectable nextNextSelectable = currentSelectable;
                    do
                    {
                        nextSelectable = nextNextSelectable;

                        nextNextSelectable =
                            nextSelectable.FindSelectableOnUp();

                        if (!nextNextSelectable)
                        {
                            nextNextSelectable = nextSelectable.FindSelectableOnLeft();
                        }

                        if (nextNextSelectable == currentSelectable)
                        {
                            // Cyclical loop
                            break;
                        }
                    }
                    while (nextNextSelectable);
                }
            }

            if (nextSelectable)
            {
                nextSelectable.Select();

                RectTransform nextSelectableRectTransform = (RectTransform)nextSelectable.transform;

                // Look for a scroll rect in the selectable's hierarchy
                ScrollRect scrollRect = null;
                for (Transform parent = nextSelectableRectTransform.parent;
                    parent && !scrollRect;
                    parent = parent.parent)
                {
                    if (parent.TryGetComponent(out Canvas _))
                    {
                        // Abort if a canvas is found
                        break;
                    }

                    parent.TryGetComponent(out scrollRect);
                }

                // If we have a scroll rect, bring the object into view
                if (scrollRect)
                {
                    Rect contentRectWorldRect = scrollRect.content.GetRectInCanvasSpace();
                    Rect nextSelectableWorldRect = nextSelectableRectTransform.GetRectInCanvasSpace();

                    // Offset the focus point by this much. This helps
                    // to keep any heading associated with the control
                    // in frame
                    const float FocusOffset = 256.0f;

                    if (scrollRect.vertical)
                    {
                        float y = nextSelectableWorldRect.yMax + FocusOffset - contentRectWorldRect.yMin;
                        y = Mathf.Clamp(y, 0.0f, contentRectWorldRect.height);
                        scrollRect.verticalNormalizedPosition = y / contentRectWorldRect.height;
                    }
                    else
                    {
                        float x = nextSelectableWorldRect.xMin - FocusOffset - contentRectWorldRect.xMin;
                        x = Mathf.Clamp(x, 0.0f, contentRectWorldRect.width);
                        scrollRect.horizontalNormalizedPosition = x / contentRectWorldRect.width;
                    }
                }
            }
        }

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        [MelcherResetStaticFieldsMethod]
        protected static int OnResetStaticFields()
        {
            int fieldsReset = 0;

            ++fieldsReset;  backButtonLeavesApp = false;

            return fieldsReset;
        }

#endif  // if UNITY_EDITOR

    }   // class MelcherSelectableManager

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-24
/// ===========================================================================
