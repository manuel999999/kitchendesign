namespace KitchenDesign.ToolsEditor
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
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_project/Editor/Tools/ScreencastKeysOverlay.uxml");
            _panelContent = m_DocumentAsset.Instantiate();

            _panelContent.RegisterCallback<AttachToPanelEvent>(
                ev =>
                {
                    _absoluteRootElement = _panelContent;
                    while (_absoluteRootElement.parent != null)
                    {
                        _absoluteRootElement = _absoluteRootElement.parent;
                    }

                    _absoluteRootElement.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                }
            );

            _panelContent.RegisterCallback<DetachFromPanelEvent>(
                ev =>
                {
                    if (_absoluteRootElement != null)
                    {
                        _absoluteRootElement.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                    }
                }
            );

            _panelContent.style.minWidth = 900;
            _panelContent.style.minHeight = 60;
            _panelContent.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            // Find all the labels that display the users input
            _screencastKeyLabels = _panelContent.Query<Label>(name: "InputEcho").ToList();

            return _panelContent;
        }


        public override void OnCreated()
        {
            base.OnCreated();

            MelcherEditorUtility.globalEventHandler -= OnEvent;
            MelcherEditorUtility.globalEventHandler += OnEvent;

            _isCreated = true;
            MelcherEditorCoroutine.StartEditorCoroutine(UpdateLabelColor());
        }

        public override void OnWillBeDestroyed()
        {
            _isCreated = false;

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
            PickingMode pickingMode = _isPickable ? PickingMode.Position : PickingMode.Ignore;

            // Find the overlay's root and restyle it to be transparent
            VisualElement root = _panelContent;
            while (root != null && !root.ClassListContains("unity-overlay") && root.parent != null)
            {
                root.pickingMode = pickingMode;
                root.delegatesFocus = _isPickable;
                root = root.parent;
            }

            if (root != null)
            {
                root.pickingMode = pickingMode;

                VisualElement overlayHeader = root.Q(className: "overlay-header");
                overlayHeader.visible = _isPickable;

                Label headerLabel = root.Q<Label>(name: "overlay-header__title");
                headerLabel.text = _isPickable ? displayName : "";

                VisualElement background = root.Q(className: "overlay-box-background");
                Color backgroundColor = background.style.backgroundColor.value;
                backgroundColor.a = _isPickable ? 0.6f : 0.0f;
                background.style.backgroundColor = backgroundColor;
            }
        }

        protected void OnMouseDown(MouseDownEvent ev)
        {
            _mouseEvent.type       = EventType.MouseDown;
            _mouseEvent.button     = ev.button;
            _mouseEvent.control    = ev.ctrlKey;
            _mouseEvent.shift      = ev.shiftKey;
            _mouseEvent.alt        = ev.altKey;
            _mouseEvent.command    = ev.commandKey;
            _mouseEvent.keyCode    = KeyCode.Mouse0 + ev.button;
            OnKeyDown(_mouseEvent);
        }

        protected void OnKeyDown(Event ev)
        {
            if (_panelContent == null)
            {
                return;
            }

            // Special condition: Clear the input history and early exit
            if (ev.keyCode == KeyCode.PageUp)
            {
                foreach (Label label in _screencastKeyLabels)
                {
                    label.text = "";
                }
                return;
            }

            // Special condition: Update the visibility and pickability of the panel
            if (ev.keyCode == KeyCode.PageDown)
            {
                _isPickable = !_isPickable;
                UpdateVisuals();
                return;
            }

            if (_keyStates[(int)ev.keyCode])
            {
                // Filter out repeated key events. These are sent when a key is
                // held down
                return;
            }

            if (ev.isKey)
            {
                // Mark keyboard keys as pressed so we can detect and filter
                // out duplicate key down events (i.e., when the key is held)
                _keyStates[(int)ev.keyCode] = true;
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
                _labelParts.Clear();

                if (ev.control)
                {
                    _labelParts.Add("Ctrl");
                }

                if (ev.shift)
                {
                    _labelParts.Add("Shift");
                }

                if (ev.alt)
                {
                    _labelParts.Add("Alt");
                }

                _labelParts.Add(ev.keyCode.ToString());
            }

            int labelCount = _screencastKeyLabels.Count;

            string latestInput = string.Join(" + ", _labelParts);
            if (ev.keyCode == m_LastKeyCode)
            {
                // This key was pressed more than once. Show a little x2 or
                // similar depending on how many times it was pressed
                ++_repeatedKeyCount;
                latestInput += $" x{_repeatedKeyCount}";
            }
            else
            {
                _repeatedKeyCount = 1;
                m_LastKeyCode = ev.keyCode;

                // Shift the previous input labels up. This keeps the previous
                // inputs visible on screen but less prominent
                for (int i = 1; i < labelCount; ++i)
                {
                    _screencastKeyLabels[i - 1].text = _screencastKeyLabels[i].text;
                }
            }

            // Show the newest input. Highlight it in bright white. This will
            // decay back to its default color in the UpdateLabelColor coroutine
            _screencastKeyLabels[labelCount - 1].text = latestInput;
            _screencastKeyLabels[labelCount - 1].style.color = Color.white;
        }

        /// <summary>
        /// Called when a keyboard key is released. This marks the key as not
        /// pressed which means the next time it is seen in a KeyDown event,
        /// we know it is a physical key press and not an automatic event
        /// sent when the key is held down.
        /// </summary>
        protected void OnKeyUp(Event ev)
        {
            _keyStates[(int)ev.keyCode] = false;
        }

        protected IEnumerator UpdateLabelColor()
        {
            while (_isCreated)
            {
                if (_screencastKeyLabels != null && _screencastKeyLabels.Count > 0)
                {
                    Label previousInputLabel = _screencastKeyLabels[0];

                    int labelCount = _screencastKeyLabels.Count;
                    Label currentInputLabel = _screencastKeyLabels[labelCount - 1];

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
        protected readonly Event _mouseEvent = new Event();

        protected VisualElement _panelContent;

        /// <summary>
        /// The absolute root of the scene view. This allows us to detect
        /// mouse clicks outside of the overlay.
        /// </summary>
        protected VisualElement _absoluteRootElement;

        /// <summary>
        /// The names of the key and modifiers that are pressed. This is used
        /// to construct a string with these parts joined by pluses.
        /// </summary>
        protected List<string> _labelParts = new List<string>();

        /// <summary>
        /// The labels where the user's inputs will be displayed.
        /// </summary>
        protected List<Label> _screencastKeyLabels;    // intentionally uninitialized

        [SerializeField, MelcherAssetField]
        protected VisualTreeAsset m_DocumentAsset = default;

        /// <summary>
        /// A bitset for all keyboard, mouse and joystick keys. This is indexed
        /// using the values of <see cref="KeyCode"/> and represents if the key
        /// is held down by the user.
        /// </summary>
        protected BitArray _keyStates = new BitArray(1024);

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
        protected int _repeatedKeyCount;

        /// <summary>
        /// Is the panel created. When the panel is destroyed, its color-tweening
        /// coroutine should end too.
        /// </summary>
        protected bool _isCreated;

        /// <summary>
        /// Can the user click the panel and drag it around.
        /// </summary>
        protected bool _isPickable;

    }   // class ScreencastKeysOverlay

}   // namespace KitchenDesign.ToolsEditor

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-01-13
/// ===========================================================================
