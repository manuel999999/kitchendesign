namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using DG.Tweening;
    using Cysharp.Threading.Tasks;

    /// <summary>
    ///     A script for a world space UI object that can be dragged by the
    ///     user (mouse pointer/touch) to resize an object in the world: e.g.,
    ///     a cabinet or shelf.
    /// </summary>
    public class ResizerWidget : KitchenWidget
        , IBeginDragHandler
        , IDragHandler
        , IEndDragHandler
    {
        // ====================================================================
        // Events
        // ====================================================================

        public delegate void ResizeStateChangeHandler(ResizerWidget caller);

        public event ResizeStateChangeHandler onResizeBegin;
        public event ResizeStateChangeHandler onResizeEnd;

        public delegate UniTask ResizeValueChangeHandler(ResizerWidget caller, float lengthMetersDelta);
        public event ResizeValueChangeHandler onResized;

        // ====================================================================
        // Properties
        // ====================================================================

        public Extent affectExtent
        {
            get => m_AffectExtent;
            set => m_AffectExtent = value;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Update the direction the widget's arrows point to match the
        ///     camera's current perspective. This happens in <see
        ///     cref="LateUpdate"/> so it can react after any camera movement
        ///     occurs.
        /// </summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (m_TargetVolume == null)
            {
                // Return early because there is no target to use to update the
                // widget's position and arrow direction.
                return;
            }

            if (m_WasErrorState)
            {
                // No rotation on glyph when showing error symbol
                m_Glyph.transform.localEulerAngles = Vector3.zero;
            }
            else
            {
                Transform cuboidTransform = m_TargetVolume.GetTransform();

                // Align the arrows to the direction of the cabinet's movement
                Vector3 start = worldCamera.WorldToScreenPoint(cuboidTransform.position);
                Vector3 end = worldCamera.WorldToScreenPoint(cuboidTransform.position + cuboidTransform.right);
                Vector3 direction = end - start;

                float angle = Vector3.SignedAngle(Vector3.right, direction, Vector3.forward);
                m_Glyph.transform.localEulerAngles = new Vector3(0.0f, 0.0f, angle);
            }
        }

        /// <summary>
        ///     Change the color and icon of the widget to indicate that the
        ///     widget is or isn't in error state. Error state just means the
        ///     widget has reached its minimum or maximum drag distance.
        /// </summary>
        /// <param name="isError">
        ///     <see langword="true"/> to transition to the red (error) state.
        ///     <see langword="false"/> to transition the blue (normal) state.
        /// </param>
        private void SetErrorState(bool isError)
        {
            m_WasErrorState = isError;

            AppSkin currentSkin = ProFitGlobals.appOptions.appSkin;

            Color foregroundColor = currentSkin.resizerWidgetForegroundColor;
            Color backgroundColor = currentSkin.resizerWidgetBackgroundColor;

            if (isError)
            {
                m_Glyph.phrase = "$(ban)";
                backgroundColor = currentSkin.dangerColor;
            }
            else
            {
                m_Glyph.phrase = "$(left-right)";
            }

            const float TweenSeconds = 0.1f;

            m_ColorSequence.Reset(
                DOTween.Sequence()
            );

            m_ColorSequence.value.Join(
                 m_Glyph.textMesh.DOColor(foregroundColor, TweenSeconds)
                    .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                    .SetEase(Ease.Linear)
            );

            m_ColorSequence.value.Join(
                 m_Border.DOColor(foregroundColor, TweenSeconds)
                    .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                    .SetEase(Ease.Linear)
            );

            m_ColorSequence.value.Join(
                 m_Background.DOColor(backgroundColor, TweenSeconds)
                    .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                    .SetEase(Ease.Linear)
            );
        }

        // =====================================================================
        // IBeginDragHandler
        // =====================================================================
        #region IBeginDragHandler

        public void OnBeginDrag(PointerEventData eventData)
        {
            bool hitTest =
                ProFitUtility.CalculateCameraLocalDragPosition(
                    m_TargetVolume,
                    m_WorldSpaceCanvas.worldCamera,
                    eventData,
                    out m_LastCameraLocalDragPosition,
                    out Plane _
                );

            if (!hitTest)
            {
                // Ignore this drag
                eventData.pointerDrag = null;
                Debug.Log("Aborting drag");
                return;
            }

            onResizeBegin?.Invoke(this);

            // Animate big when dragged
            m_HideOrShowTween.Reset(
                DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                .Join(
                    m_CanvasRoot.DOScale(
                        endValue: m_CanvasLocalScaleWhenVisible * 2.0f,
                        duration: 0.2f
                    ).SetEase(Ease.OutBack)
                ),
                completePrevious: true
            );
        }

        #endregion  // IBeginDragHandler

        // =====================================================================
        // IDragHandler
        // =====================================================================
        #region IDragHandler

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 cameraLocalDragPosition;
            bool hitTest =
                ProFitUtility.CalculateCameraLocalDragPosition(
                    m_TargetVolume,
                    m_WorldSpaceCanvas.worldCamera,
                    eventData,
                    out cameraLocalDragPosition,
                    out Plane cameraLocalDragPlane
                );

            if (hitTest)
            {
                Vector3 cameraLocalDragDelta = cameraLocalDragPosition - m_LastCameraLocalDragPosition;
                Vector3 worldDragDelta = worldCamera.transform.TransformVector(cameraLocalDragDelta);

                Transform targetTransform = m_TargetVolume.GetTransform();
                Vector3 cuboidLocalDragDelta = targetTransform.InverseTransformVector(worldDragDelta);

                float lengthMetersDelta = 0.0f;

                MeasurementUnit units = ProFitGlobals.userProfile.activeUnits;

                if (units == MeasurementUnit.Centimeters)
                {
                    int lengthCentimetersDelta = Mathf.RoundToInt(cuboidLocalDragDelta.x * Const.Meters2Centimeters);
                    lengthMetersDelta = lengthCentimetersDelta * Const.Centimeters2Meters;
                }
                else if (units == MeasurementUnit.FeetAndInches)
                {
                    // Calculate the number of half-inches to resize the
                    // cabinet by. Stoll requested half-inch increments.

                    const float MinimumInchIncrements = 1.0f / 2.0f;

                    int lengthHalfInchesDelta =
                        Mathf.RoundToInt(cuboidLocalDragDelta.x * Const.Meters2Inches / MinimumInchIncrements);

                    lengthMetersDelta =
                        (lengthHalfInchesDelta * MinimumInchIncrements) * Const.Inches2Meters;
                }

                // When adjusting the minimum extent, flip the sign of the
                // delta. This makes the code easier to read because the value
                // will be positive when the cuboid is expanding and negative
                // when it is contracting.
                if (m_AffectExtent == Extent.Min)
                {
                    lengthMetersDelta = -lengthMetersDelta;
                }

                // Try to adjust the cuboid's length. Don't save the new
                // drag position if it fails as otherwise the mouse pointer will
                // deviate from the widget.
                bool isErrorState = lengthMetersDelta != 0;
                if (m_TargetVolume.TryToResizeLengthAsUser(ref lengthMetersDelta, m_AffectExtent))
                {
                    isErrorState = false;
                    m_LastCameraLocalDragPosition = cameraLocalDragPosition;

                    onResized?.Invoke(this, lengthMetersDelta);
                }

                if (isErrorState != m_WasErrorState)
                {
                    SetErrorState(isErrorState);
                }
            }
        }
        #endregion  // IDragHandler

        // =====================================================================
        // IEndDragHandler
        // =====================================================================
        #region IEndDragHandler

        public void OnEndDrag(PointerEventData eventData)
        {
            SetErrorState(false);
            // Animate the widget back to its normal size.
            SetVisible(true);
            onResizeEnd?.Invoke(this);
        }
        #endregion  // IEndDragHandler

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        ///     Indicates which side of the target cuboid this widget modifies.
        ///     For example, <see cref="Extent.Min"/> pushes and pulls the
        ///     left face of a cabinet and <see cref="Extent.Max"/> does the
        ///     right face.
        /// </summary>
        protected Extent m_AffectExtent;

        protected RecyclableTween<Sequence> m_ColorSequence = new RecyclableTween<Sequence>();

        /// <summary>
        ///     When the resize widget is dragged too far, it enters an Error
        ///     State which just means it turns red and shows a circle with
        ///     a line through it for the icon. This field indicates whether
        ///     the widget is in this error state.
        /// </summary>
        protected bool m_WasErrorState;

    }   // class ResizerWidget

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-05-16
/// ===========================================================================
