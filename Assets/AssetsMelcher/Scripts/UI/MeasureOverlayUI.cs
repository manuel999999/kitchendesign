namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using MEC;
    using KitchenDesign.Measures;

    /// <summary>
    /// This class is used to display <see cref="Measure"/>s measurement label
    /// as a canvas-space object. This ensures the text remains legible
    /// regardless of camera zoom or position (e.g., in the designer scene).
    /// </summary>
    public class MeasureOverlayUI : MelcherUI
    {
        /// <summary>
        /// Register a <see cref="Measure"/>; i.e., make its distance text
        /// render using a canvas label rather than its own 3D-space label.
        /// A measure can only be registered once; subsequent registers using
        /// an already-registered <see cref="Measure"/> are ignored.
        /// </summary>
        public void AddOrUpdateMeasurement(Measure measure, Vector2 canvasLabelPivot)
        {
            int existingIndex = m_ActiveMeasurements.FindIndex(x => x.measure == measure);
            if (existingIndex >= 0)
            {
                ActiveMeasure activeMeasure = m_ActiveMeasurements[existingIndex];
                activeMeasure.canvasLabelPivot = canvasLabelPivot;
                m_ActiveMeasurements[existingIndex] = activeMeasure;
            }
            else
            {
                Debug.Assert(m_ActiveMeasurements.Count < m_NumberOfLabelsToPool);
                m_ActiveMeasurements.Add(new ActiveMeasure(measure, canvasLabelPivot));

                // Hide the measure's 3D-label. We'll be drawing it using a
                // screen-space label
                measure.SetWorldSpaceLabelVisible(false);

                if (m_ActiveMeasurements.Count == 1)
                {
                    Show(transition: null, onHidden: null);
                }
            }

            measure.Refresh();
        }

        /// <summary>
        /// Remove a registered <see cref="Measure"/>. This will make it render
        /// its text using its 3D-space label.
        /// </summary>
        public void RemoveMeasurement(Measure measure)
        {
            int existingIndex = m_ActiveMeasurements.FindIndex(x => x.measure == measure);
            if (existingIndex >= 0)
            {
                m_ActiveMeasurements.RemoveAt(existingIndex);

                // Re-enable the measure's own label
                measure.SetWorldSpaceLabelVisible(true);

                if (m_ActiveMeasurements.Count <= 0)
                {
                    Hide(transition: null, onHidden: null);
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // Create a pool of canvas labels for the overlay
            for (int i = 0; i < m_NumberOfLabelsToPool; ++i)
            {
                m_PooledCanvasLabels.Add(
                    Instantiate(m_CanvasLabelPrefab, m_RectTransform)
                );
            }

            // Hide all of the canvas labels by default
            HideCanvasLabels();
        }

        protected override void DoShow()
        {
            base.DoShow();

            foreach (ActiveMeasure activeMeasure in m_ActiveMeasurements)
            {
                // Disable visibility of already registered 3D-labels
                activeMeasure.measure.SetWorldSpaceLabelVisible(false);
            }

            if (!Timing.IsRunning(m_UpdateCoroutine))
            {
                m_UpdateCoroutine = Timing.RunCoroutine(
                    UpdateMeasurements().CancelWith(gameObject)
                );
            }
        }

        protected override void DoHide()
        {
            Timing.KillCoroutines(m_UpdateCoroutine);

            HideCanvasLabels();

            foreach (ActiveMeasure activeMeasure in m_ActiveMeasurements)
            {
                // Restore visibility of still-registered 3D-labels
                activeMeasure.measure.SetWorldSpaceLabelVisible(true);
            }

            base.DoHide();
        }

        /// <summary>
        /// This coroutine runs as long as the overlay is visible, which is
        /// always. It ensures the canvas-space labels are properly aligned
        /// with the measures' world space positions.
        /// </summary>
        protected IEnumerator<float> UpdateMeasurements()
        {
            Camera camera = Camera.main;

            RectTransform canvasRectTransform = m_RectTransform;

            while (true)
            {
                for (int i = 0; i < m_PooledCanvasLabels.Count; ++i)
                {
                    bool isVisible = i < m_ActiveMeasurements.Count;

                    MelcherLocalizedText label = m_PooledCanvasLabels[i];

                    if (isVisible)
                    {
                        ActiveMeasure activeMeasure = m_ActiveMeasurements[i];

                        // Update the displayed text
                        label.phrase = activeMeasure.measure.GetMeasureText();

                        RectTransform labelRectTransform = label.GetComponent<RectTransform>();

                        // Get the original label's 3D world position
                        Vector3 labelPositionInWorldSpace = activeMeasure.measure.GetLabelPositionInWorldSpace();

                        // Match the canvas label's screen position to the original world position
                        Vector3 screenPoint = camera.WorldToScreenPoint(labelPositionInWorldSpace);
                        screenPoint.z = 0.2f;

                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPoint, cam: null, out Vector2 anchoredPosition))
                        {
                            labelRectTransform.anchoredPosition = anchoredPosition;
                            labelRectTransform.pivot = activeMeasure.canvasLabelPivot;
                        }
                    }

                    label.gameObject.SetActive(isVisible);
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        protected void HideCanvasLabels()
        {
            foreach (MelcherLocalizedText canvasLabel in m_PooledCanvasLabels)
            {
                canvasLabel.gameObject.SetActive(false);
            }
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [MelcherHeader]
        [SerializeField] protected int m_NumberOfLabelsToPool = 8;
        [SerializeField] protected MelcherLocalizedText m_CanvasLabelPrefab = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        /// The active <see cref="Measure" />s. <see cref="Measure" />s are
        /// added to this list when they are meant to be rendered via a canvas-
        /// space label. It's the job of this class to display a label at the
        /// measure's midpoint. E.g., the measurements that appear on the left
        /// and right side of a selected product are added to this list.
        /// </summary>
        protected List<ActiveMeasure> m_ActiveMeasurements = new List<ActiveMeasure>();

        protected List<MelcherLocalizedText> m_PooledCanvasLabels = new List<MelcherLocalizedText>();

        protected CoroutineHandle m_UpdateCoroutine;

        // ====================================================================
        // Nested types
        // ====================================================================

        protected struct ActiveMeasure
        {
            public ActiveMeasure(Measure measure, Vector2 canvasLabelPivot)
            {
                this.measure = measure;
                this.canvasLabelPivot = canvasLabelPivot;
            }

            public Measure measure;

            /// <summary>
            /// The pivot that will be assigned to the canvas label. The
            /// default value is (0.5, 0.5). This is set when we'd like to
            /// shift the label to avoid it overlapping with other labels in
            /// a way that is independent of the screen's resolution.
            /// </summary>
            public Vector2 canvasLabelPivot;

        }   // struct ActiveMeasure

    }   // class MeasureOverlayUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-15
/// ===========================================================================
