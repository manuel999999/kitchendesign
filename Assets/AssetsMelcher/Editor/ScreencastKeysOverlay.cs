namespace KitchenDesign
{
    using Melcher;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor.Overlays;
    using UnityEditor;

    /// <summary>
    /// Blender-like overlay to display what keys are pressed within the
    /// scene view. This is useful to record tutorial videos when describing a
    /// workflow.
    /// </summary>
    [Overlay(typeof(SceneView), displayName: "Screencast Keys", defaultDisplay: true)]
    public class ScreencastKeysOverlay : Overlay
    {
        public override VisualElement CreatePanelContent()
        {
            m_DocumentAsset =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AssetsMelcher/Editor/ScreencastKeysOverlay.uxml");
            m_PanelContent = m_DocumentAsset.Instantiate();

            m_PanelContent.RegisterCallback<AttachToPanelEvent>(
                ev =>
                {
                    m_AbsoluteRootElement = m_PanelContent;
                    while (m_AbsoluteRootElement.parent != null)
                    {
                        m_AbsoluteRootElement = m_AbsoluteRootElement.parent;
                    }

                    m_AbsoluteRootElement.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                }
            );

            m_PanelContent.RegisterCallback<DetachFromPanelEvent>(
                ev =>
                {
                    if (m_AbsoluteRootElement != null)
                    {
                        m_AbsoluteRootElement.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                    }
                }
            );

            m_PanelContent.style.minWidth = 900;
            m_PanelContent.style.minHeight = 60;
            m_PanelContent.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            // Find all the labels that display the users input
            m_ScreencastKeyLabels = m_PanelContent.Query<Label>(name: "InputEcho").ToList();

            return m_PanelContent;
        }


        public override void OnCreated()
        {
            base.OnCreated();

            MelcherEditorUtility.globalEventHandler -= OnEvent;
            MelcherEditorUtility.globalEventHandler += OnEvent;

            m_IsCreated = true;
            MelcherEditorCoroutine.StartEditorCoroutine(UpdateLabelColor());
        }

        public override void OnWillBeDestroyed()
        {
            m_IsCreated = false;

            MelcherEditorUtility.globalEventHandler -= OnEvent;

            base.OnWillBeDestroyed();
        }

        protected void OnEvent()
        {
            if (Event.current == null)
            {
                return;
            }

            switch (Event.current.type)
            {
                case EventType.KeyDown:
                    OnKeyDown(Event.current);
                    break;

                case EventType.KeyUp:
                    OnKeyUp(Event.current);
                    break;
            }
        }

        protected void OnGeometryChanged(GeometryChangedEvent ev)
        {
            UpdateVisuals();
        }

        protected void UpdateVisuals()
        {
            PickingMode pickingMode = m_IsPickable ? PickingMode.Position : PickingMode.Ignore;

            // Find the overlay's root and restyle it to be transparent
            VisualElement root = m_PanelContent;
            while (root != null && !root.ClassListContains("unity-overlay") && root.parent != null)
            {
                root.pickingMode = pickingMode;
                root.delegatesFocus = m_IsPickable;
                root = root.parent;
            }

            if (root != null)
            {
                root.pickingMode = pickingMode;

                VisualElement overlayHeader = root.Q(className: "overlay-header");
                overlayHeader.visible = m_IsPickable;

                Label headerLabel = root.Q<Label>(name: "overlay-header__title");
                headerLabel.text = m_IsPickable ? displayName : "";

                VisualElement background = root.Q(className: "overlay-box-background");
                Color backgroundColor = background.style.backgroundColor.value;
                backgroundColor.a = m_IsPickable ? 0.6f : 0.0f;
                background.style.backgroundColor = backgroundColor;
            }
        }

        protected void OnMouseDown(MouseDownEvent ev)
        {
            m_MouseEvent.type       = EventType.MouseDown;
            m_MouseEvent.button     = ev.button;
            m_MouseEvent.control    = ev.ctrlKey;
            m_MouseEvent.shift      = ev.shiftKey;
            m_MouseEvent.alt        = ev.altKey;
            m_MouseEvent.command    = ev.commandKey;
            m_MouseEvent.keyCode    = KeyCode.Mouse0 + ev.button;
            OnKeyDown(m_MouseEvent);
        }

        protected void OnKeyDown(Event ev)
        {
            if (m_PanelContent == null)
            {
                return;
            }

            // Special condition: Clear the input history and early exit
            if (ev.keyCode == KeyCode.PageUp)
            {
                foreach (Label label in m_ScreencastKeyLabels)
                {
                    label.text = "";
                }
                return;
            }

            // Special condition: Update the visibility and pickability of the panel
            if (ev.keyCode == KeyCode.PageDown)
            {
                m_IsPickable = !m_IsPickable;
                UpdateVisuals();
                return;
            }

            if (m_KeyStates[(int)ev.keyCode])
            {
                // Filter out repeated key events. These are sent when a key is
                // held down
                return;
            }

            if (ev.isKey)
            {
                // Mark keyboard keys as pressed so we can detect and filter
                // out duplicate key down events (i.e., when the key is held)
                m_KeyStates[(int)ev.keyCode] = true;
            }

            if (ev.keyCode == KeyCode.None ||
                ev.keyCode == KeyCode.LeftShift ||
                ev.keyCode == KeyCode.RightShift ||
                ev.keyCode == KeyCode.LeftControl ||
                ev.keyCode == KeyCode.RightControl ||
                ev.keyCode == KeyCode.LeftCommand ||
                ev.keyCode == KeyCode.RightCommand ||
                ev.keyCode == KeyCode.LeftAlt ||
                ev.keyCode == KeyCode.RightAlt)
            {
                return;
            }

            {
                // Build the string that shows which key and modifiers were
                // pressed
                m_LabelParts.Clear();

                if (ev.control)
                {
                    m_LabelParts.Add("Ctrl");
                }

                if (ev.shift)
                {
                    m_LabelParts.Add("Shift");
                }

                if (ev.alt)
                {
                    m_LabelParts.Add("Alt");
                }

                m_LabelParts.Add(ev.keyCode.ToString());
            }

            int labelCount = m_ScreencastKeyLabels.Count;

            string latestInput = string.Join(" + ", m_LabelParts);
            if (ev.keyCode == m_LastKeyCode)
            {
                // This key was pressed more than once. Show a little x2 or
                // similar depending on how many times it was pressed
                ++m_RepeatedKeyCount;
                latestInput += $" x{m_RepeatedKeyCount}";
            }
            else
            {
                m_RepeatedKeyCount = 1;
                m_LastKeyCode = ev.keyCode;

                // Shift the previous input labels up. This keeps the previous
                // inputs visible on screen but less prominent
                for (int i = 1; i < labelCount; ++i)
                {
                    m_ScreencastKeyLabels[i - 1].text = m_ScreencastKeyLabels[i].text;
                }
            }

            // Show the newest input. Highlight it in bright white. This will
            // decay back to its default color in the UpdateLabelColor coroutine
            m_ScreencastKeyLabels[labelCount - 1].text = latestInput;
            m_ScreencastKeyLabels[labelCount - 1].style.color = Color.white;
        }

        /// <summary>
        /// Called when a keyboard key is released. This marks the key as not
        /// pressed which means the next time it is seen in a KeyDown event,
        /// we know it is a physical key press and not an automatic event
        /// sent when the key is held down.
        /// </summary>
        protected void OnKeyUp(Event ev)
        {
            m_KeyStates[(int)ev.keyCode] = false;
        }

        protected IEnumerator UpdateLabelColor()
        {
            while (m_IsCreated)
            {
                if (m_ScreencastKeyLabels != null && m_ScreencastKeyLabels.Count > 0)
                {
                    Label previousInputLabel = m_ScreencastKeyLabels[0];

                    int labelCount = m_ScreencastKeyLabels.Count;
                    Label currentInputLabel = m_ScreencastKeyLabels[labelCount - 1];

                    currentInputLabel.style.color =
                        Color.Lerp(currentInputLabel.style.color.value, previousInputLabel.resolvedStyle.color, 0.1f);
                }

                yield return null;
            }
        }

        /// <summary>
        /// A reusable event to avoid unnecessary GC. It is setup in <see
        /// cref="OnMouseDown(MouseDownEvent)"/> and passed to <see
        /// cref="OnKeyDown(Event)"/>.
        /// </summary>
        protected readonly Event m_MouseEvent = new Event();

        protected VisualElement m_PanelContent;

        /// <summary>
        /// The absolute root of the scene view. This allows us to detect
        /// mouse clicks outside of the overlay.
        /// </summary>
        protected VisualElement m_AbsoluteRootElement;

        /// <summary>
        /// The names of the key and modifiers that are pressed. This is used
        /// to construct a string with these parts joined by pluses.
        /// </summary>
        protected List<string> m_LabelParts = new List<string>();

        /// <summary>
        /// The labels where the user's inputs will be displayed.
        /// </summary>
        protected List<Label> m_ScreencastKeyLabels;    // intentionally uninitialized

        [SerializeField, MelcherAssetField]
        protected VisualTreeAsset m_DocumentAsset = default;

        /// <summary>
        /// A bitset for all keyboard, mouse and joystick keys. This is indexed
        /// using the values of <see cref="KeyCode"/> and represents if the key
        /// is held down by the user.
        /// </summary>
        protected BitArray m_KeyStates = new BitArray(1024);

        /// <summary>
        /// The last keyboard, mouse or joystick key that was pressed. This is
        /// used to detect when the same key is pressed multiple times in a row
        /// so it can be collapsed into the current label (x2, x3, etc.)
        /// </summary>
        protected KeyCode m_LastKeyCode;

        /// <summary>
        /// Number of times <see cref="m_LastKeyCode"/> has been pressed in a
        /// row.
        /// </summary>
        protected int m_RepeatedKeyCount;

        /// <summary>
        /// Is the panel created. When the panel is destroyed, its color-tweening
        /// coroutine should end too.
        /// </summary>
        protected bool m_IsCreated;

        /// <summary>
        /// Can the user click the panel and drag it around.
        /// </summary>
        protected bool m_IsPickable;

    }   // class ScreencastKeysOverlay

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-01-13
/// ===========================================================================
