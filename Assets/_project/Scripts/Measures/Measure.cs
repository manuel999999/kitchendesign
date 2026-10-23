namespace KitchenDesign.Measures
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Common;
    using System;
    using System.Text;
    using UnityEngine;
    using TMPro;

    /// <summary>
    ///     A <see cref="Measure"/> draws a line and distance between two
    ///     points. The distance text is drawn in 3D-space, which allows it to
    ///     have perspective but also makes it prone to being illegible during
    ///     wide camera zooms.
    ///
    ///     There is a companion class, <see cref="MeasureOverlayUI"/>, that
    ///     can override the 3D-space text and render the distance in
    ///     screen-space. This eliminates the issue of illegiblity with camera
    ///     zoom.
    ///
    ///     This class uses a <see cref="LineRenderer"/> and a <see
    ///     cref="TextMeshPro"/>. The text may or may not be visible, depending
    ///     on whether the measure has been added to the <see
    ///     cref="MeasureOverlayUI" />
    /// </summary>
    public class Measure : MelcherMonoBehaviour
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public string text => m_WorldSpaceLabel.text;

        /// <summary>
        /// The number of fraction divisions to use. E.g. 8 = eighths.
        /// </summary>
        public int minimumInchDivisions
        {
            get => m_MinimumInchDivisions;
            set => m_MinimumInchDivisions = value;
        }

        public float labelNormalizedPositionWithinLine
        {
            get => m_LabelNormalizedPositionWithinLine;
            set => m_LabelNormalizedPositionWithinLine = value;
        }

        public Color color
        {
            get => m_LineRenderer.startColor;
            set
            {
                m_WorldSpaceLabel.color = value;

                m_LineRenderer.startColor = value;
                m_LineRenderer.endColor = value;

                m_TickMark1.startColor = value;
                m_TickMark1.endColor = value;

                m_TickMark2.startColor = value;
                m_TickMark2.endColor = value;
            }
        }

        public MeasurementUnit units
        {
            get => m_DisplayUnits;
            set => m_DisplayUnits = value;
        }

        // ====================================================================
        // Methods
        // ====================================================================

        // Draws a line between the measure's start and end points. Used during
        // development.
        //public void Debug_DrawLine(Color color, float duration = 0.0f, bool depthTest = false)
        //{
        //    Debug.DrawLine(m_Transform.position, m_EndPoint.position, color, duration, depthTest);
        //}

        public string GetMeasureText()
        {
            string text = "";

            if (m_EndPoint)
            {
                Vector3 worldStartPoint = m_Transform.position;
                Vector3 worldEndPoint = m_EndPoint.position;

                // Format measurement text
                float distanceInMeters = Vector3.Distance(worldStartPoint, worldEndPoint);

                distanceInMeters += m_ExtraDistanceInMetersForLabelOnly;

                if (m_UseMonospace)
                {
                    int monoSize = IsWorldSpaceLabelVisible() ? m_WorldSpaceMonoSize : m_ScreenSpaceMonoSize;

                    if (m_DisplayUnits == MeasurementUnit.FeetAndInches)
                    {
                        text =
                            Utility.MeasurementFormat.FormatMetersToImperialMonospaced(
                                m_StringBuilder,
                                monoSize,
                                distanceInMeters,
                                m_ShowFeet,
                                minimumInchDivisions: m_MinimumInchDivisions,
                                showInchesWhenZero: false
                            );
                    }
                    else if (m_DisplayUnits == MeasurementUnit.Centimeters)
                    {
                        string distanceInCentimeters =
                            Utility.MeasurementFormat.FormatMetersToCentimeters(distanceInMeters);

                        text = $"<mspace={monoSize}>{distanceInCentimeters}</mspace>";
                    }
                    else
                    {
                        MelcherLog.LogError("Cannot format measurement. Unhandled units: " + units, this);
                    }
                }
                else
                {
                    if (m_DisplayUnits == MeasurementUnit.FeetAndInches)
                    {
                        text =
                            Utility.MeasurementFormat.FormatMetersToImperial(
                                distanceInMeters,
                                m_ShowFeet,
                                minimumInchDivisions: m_MinimumInchDivisions
                            );
                    }
                    else if (m_DisplayUnits == MeasurementUnit.Centimeters)
                    {
                        text =
                            Utility.MeasurementFormat.FormatMetersToCentimeters(
                                distanceInMeters,
                                leftDecimalPlaces: 3
                            );
                    }
                    else
                    {
                        MelcherLog.LogError("Cannot format measurement. Unhandled units: " + units, this);
                    }
                }
            }

            return text;
        }

        public void MeasureFromTo(Vector3 worldFrom, Vector3 worldTo)
        {
            m_Transform.position = worldFrom;
            m_EndPoint.position = worldTo;

            Refresh();
        }

        /// <summary>
        /// Set whether the <see cref="Measure" />'s own 3D-label should be
        /// rendered. Sometimes i
        /// </summary>
        /// <param name="isVisible"></param>
        public void SetWorldSpaceLabelVisible(bool isVisible)
        {
            m_WorldSpaceLabel.gameObject.SetActive(isVisible);
            m_WorldSpaceLabelOptionalBackground?.gameObject.SetActive(isVisible);
        }

        public void SetDisplayFlags(MeasureDisplayFlag mask)
        {
            m_DisplayFlags = mask;

            Refresh();
        }

        public bool IsWorldSpaceLabelVisible()
        {
            return m_WorldSpaceLabel.gameObject.activeSelf;
        }

        public Vector3 GetLabelPositionInWorldSpace()
        {
            Vector3 worldStartPoint = m_Transform.position;
            Vector3 worldEndPoint = m_EndPoint.position;
            return Vector3.Lerp(worldStartPoint, worldEndPoint, t: m_LabelNormalizedPositionWithinLine);
        }

        /// <summary>
        /// Synchronize the line renderer's positions, the world-space label
        /// and the world-space label's text to the currently measured
        /// distance.
        /// </summary>
        public void Refresh()
        {
            bool hasEndPoint = m_EndPoint;

            MeasureDisplayFlag effectiveDisplayFlags = m_DisplayFlags;
            if (!hasEndPoint)
            {
                effectiveDisplayFlags = MeasureDisplayFlag.None;
            }

            // Reveal/hide the visual components if the endpoint exists/doesn't exist
            bool useLabel = effectiveDisplayFlags.HasFlag(MeasureDisplayFlag.Label);
            m_WorldSpaceLabel.enabled = useLabel;
            if (m_WorldSpaceLabelOptionalBackground)
            {
                m_WorldSpaceLabelOptionalBackground.enabled = useLabel;
            }

            m_LineRenderer.enabled = effectiveDisplayFlags.HasFlag(MeasureDisplayFlag.Line);

            bool useTickMark = effectiveDisplayFlags.HasFlag(MeasureDisplayFlag.Tick);
            m_TickMark1.enabled = useTickMark;
            m_TickMark2.enabled = useTickMark;

            if (hasEndPoint)
            {
                // Move the label to measure's mid-point
                Vector3 worldEndPoint       = m_EndPoint.position;

                m_LabelRoot.position =
                    Vector3.Lerp(
                        m_Transform.position,
                        worldEndPoint,
                        m_LabelNormalizedPositionWithinLine
                    );

                // Point at camera
                Camera camera = null;
                if (m_LookAtCamera)
                {
                    if (m_AlignToEditorCamera && m_EditorViewCamera)
                    {
                        camera = m_EditorViewCamera;
                    }
                    else
                    {
                        camera = Camera.main;
                    }
                }

                if (camera != null)
                {
                    m_LabelRoot.forward = camera.transform.forward;
                    m_LabelRoot.position += -m_LabelRoot.forward * m_LabelDistance;
                }
                else
                {
                    // Move the label a bit in front of the line for better
                    // render ordering
                    m_LabelRoot.localPosition += Vector3.back * m_LabelDistance;
                }

                // Calculate the measured line's points. We want it to make an
                // I-beam shape (a straight line with a tick at both ends)
                Vector3 startInLineSpace = Vector3.zero;
                Vector3 endInLineSpace = m_Transform.InverseTransformPoint(worldEndPoint);

                m_LineRenderer.SetPosition(0, startInLineSpace);
                m_LineRenderer.SetPosition(1, endInLineSpace);

                Vector3 lineDirection = (endInLineSpace - startInLineSpace).normalized;
                Vector3 lineTickDirection = m_Transform.up; // default the tick to point in same direction as the top of the label

                // If we're using the world space label, we'll calculate a
                // proper tick mark direction from the mid line
                if (m_WorldSpaceLabel.gameObject.activeSelf)
                {
                    Vector3 RotateDirectionAroundAxis(Vector3 direction, float angleDegrees, Vector3 axis)
                    {
                        Quaternion q = Quaternion.AngleAxis(angleDegrees, axis);
                        return q * direction;
                    }

                    // Get the tick mark's direction by rotating the midline's
                    // 90 degrees around the label's forward direction
                    lineTickDirection = RotateDirectionAroundAxis(lineDirection, angleDegrees: 90.0f, Vector3.forward);
                }

                // The amount to shift the tick marks into the line. This is
                // to avoid the ticks overlapping when two measurements are
                // too close
                Vector3 tickInsetFromBeginning = Vector3.zero;//midLineDirection * (Units.Inches2Meters * 0.25f);

                Vector3 startTickTopInLineSpace     = startInLineSpace + tickInsetFromBeginning + (lineTickDirection * m_TickSizeInMeters);
                Vector3 startTickBottomInLineSpace  = startInLineSpace + tickInsetFromBeginning - (lineTickDirection * m_TickSizeInMeters);

                m_TickMark1.SetPosition(0, startTickTopInLineSpace);
                m_TickMark1.SetPosition(1, startTickBottomInLineSpace);

                Vector3 endTickTopInLineSpace       = endInLineSpace - tickInsetFromBeginning + (lineTickDirection * m_TickSizeInMeters);
                Vector3 endTickBottomInLineSpace    = endInLineSpace - tickInsetFromBeginning - (lineTickDirection * m_TickSizeInMeters);

                m_TickMark2.SetPosition(0, endTickTopInLineSpace);
                m_TickMark2.SetPosition(1, endTickBottomInLineSpace);

                // Update the label
                m_WorldSpaceLabel.text = GetMeasureText();

                if (m_WorldSpaceLabelOptionalBackground)
                {
                    m_WorldSpaceLabelOptionalBackground.text = m_WorldSpaceLabel.text;
                }
            }

        }

        protected void Awake()
        {
#if UNITY_EDITOR
            m_EditorViewCamera = UnityEditor.SceneView.GetWindow<UnityEditor.SceneView>().camera;
#endif  // if UNITY_EDITOR

            // Apply the app theme's colour to the lines
            color =
                ProFitGlobals.appOptions.appSkin.designerMeasureLineColor;

            //
            m_LineRendererWidthCurve = m_LineRenderer.widthCurve;
            while (m_LineRendererWidthCurve.length < 3)
            {
                m_LineRendererWidthCurve.AddKey(time: 0.1f * m_LineRendererWidthCurve.length, value: 0.0f);
            }
            m_LineRendererWidthCurveKeyframes = m_LineRendererWidthCurve.keys;
        }

        /// <summary>
        /// For development purposes, this function allows the measure to
        /// update every frame (to face the camera).
        /// </summary>
        protected void Update()
        {
            if (m_AlwaysRefresh)
            {
                Refresh();
            }
        }

        public void SetExtraDistanceForLabelOnly(float extraDistanceInMetersForLabelOnly)
        {
            m_ExtraDistanceInMetersForLabelOnly = extraDistanceInMetersForLabelOnly;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField] protected LineRenderer m_TickMark1 = null;
        [SerializeField] protected LineRenderer m_TickMark2 = null;

        [SerializeField] protected Transform m_EndPoint = null;

        [SerializeField]
        [Tooltip("The transform that is adjusted to move the world-space " +
            "label's position. This ensures the label can be composited " +
            "from multiple layers and they will all move as one.")]
        protected Transform m_LabelRoot = default;

        [SerializeField]
        [Tooltip("This is the world-space label used to draw the measured " +
            "distance. The world-space label is disabled and a canvas-space " +
            "label is used in its place when we want to ensure the text is " +
            "legible regardless of camera zoom, rotation, etc.")]
        protected TextMeshPro m_WorldSpaceLabel = default;

        [SerializeField]
        protected TextMeshPro m_WorldSpaceLabelOptionalBackground = default;

        [Tooltip("Whether the label should face the camera.")]
        [SerializeField] protected bool m_LookAtCamera = true;

        [Tooltip("For debug purposes. Makes the label look-at the editor camera.")]
        [SerializeField] protected bool m_AlignToEditorCamera = false;

        [SerializeField]
        [Tooltip("When true, the number of feet will be separated out. E.g. " +
            "2' 3\". When this is false, the feet group is omitted and " +
            "it's distance is included in the inches: 27\"")]
        protected bool m_ShowFeet = true;

        [Tooltip("Whether the text should be mono-spaced. Mono-spaced looks " +
            "better if the label will be updated in real time.")]
        [SerializeField] protected bool m_UseMonospace = true;

        [Tooltip("Refresh the text in Update. This is meant to be for " +
            "development purposes only and will be removed.")]
        [SerializeField] protected bool m_AlwaysRefresh = false;

        [Tooltip("The size of each character (monospace) when using " +
            "world-space text.")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_MonoSize")]
        [SerializeField] protected int m_WorldSpaceMonoSize = 138;

        [Tooltip("The size of each character (monospace) when using " +
            "screen-space text.")]
        [SerializeField] protected int m_ScreenSpaceMonoSize = 138;

        [Tooltip("How far to the label is offset towards the camera.")]
        [SerializeField] protected float m_LabelDistance = 0.2f;

        [Tooltip("The size of the tick marks at the end of the line in meters.")]
        [SerializeField] protected float m_TickSizeInMeters = 0.2f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("Controls where the label appears on the measurement line. " +
            "A value of 0.5 centers it. 0 puts it at the measurement's " +
            "starting point.")]
        protected float m_LabelNormalizedPositionWithinLine = 0.5f;

        [SerializeField]
        [Tooltip("The lowest division of inch fractions to show. E.g. 8 = eighths")]
        protected int m_MinimumInchDivisions = ImperialUnit.DefaultMaximumInchDivision;

        [SerializeField]
        [Tooltip("Which units the measure will display.")]
        protected MeasurementUnit m_DisplayUnits = MeasurementUnit.FeetAndInches;

        [SerializeField]
        protected MeasureDisplayFlag m_DisplayFlags = MeasureDisplayFlag.Label | MeasureDisplayFlag.Line | MeasureDisplayFlag.Tick;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Transform m_Transform = default;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected LineRenderer m_LineRenderer = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected StringBuilder m_StringBuilder = new StringBuilder();
        protected Camera m_EditorViewCamera;

        protected AnimationCurve m_LineRendererWidthCurve;
        protected Keyframe[] m_LineRendererWidthCurveKeyframes;

        /// <summary>
        ///     This distance will be added to the actual distance measured,
        ///     but will not affect the rendered line.
        /// </summary>
        protected float m_ExtraDistanceInMetersForLabelOnly;

        // ====================================================================
        // Nested types
        // ====================================================================


    }   // class Measure

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-07-26
/// ===========================================================================
