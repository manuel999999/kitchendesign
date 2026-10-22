namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;
    using KitchenDesign.Geometry;

    /// <summary>
    ///     A base class for a world space UI object that anchor to the
    ///     kitchen and are intended to be clicked or dragged by the user. For
    ///     example, <see cref="ResizerWidget"/> and <see cref="ButtonWidget"/>.
    /// </summary>
    public abstract class KitchenWidget : MelcherMonoBehaviour
    {
        // ====================================================================
        // Events
        // ====================================================================

        // ====================================================================
        // Properties
        // ====================================================================

        public OccupiableVolume target
        {
            get => m_TargetVolume;
            set => m_TargetVolume = value;
        }

        public Vector3 normalizedTargetAnchor
        {
            get => m_NormalizedTargetAnchor;
            set => m_NormalizedTargetAnchor = value;
        }

        public Camera worldCamera
        {
            get => m_WorldSpaceCanvas.worldCamera;
            set => m_WorldSpaceCanvas.worldCamera = value;
        }

        public bool isVisible => m_IsVisible;

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetVisible(bool isVisible)
        {
            m_IsVisible = isVisible;

            // Match interactable with visibility
            m_Raycaster.enabled = isVisible;

            // Animate the widget appearing or hiding
            m_HideOrShowTween.Reset(
                DOTween.Sequence()
                    .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                    .Join(
                        // Scale widget root
                        m_Transform.DOScale(
                            endValue: isVisible ? Vector3.one : Vector3.zero,
                            duration: isVisible ? 0.25f : 0.15f
                        ).SetEase(isVisible ? Ease.OutBack : Ease.InBack)
                    )
                    .Join(
                        // Scale widget canvas
                        m_CanvasRoot.DOScale(
                            endValue: isVisible ? Vector3.one * m_CanvasLocalScaleWhenVisible : Vector3.zero,
                            duration: isVisible ? 0.25f : 0.15f
                        ).SetEase(isVisible ? Ease.OutBack : Ease.InBack)
                    )
                    .SetDelay(isVisible ? m_PopInDelay : 0.0f)
            );
        }

        protected virtual void Awake()
        {
            m_TouchHitbox.gameObject.SetActive(MelcherWebGL.IsMobilePlatform());

            m_CanvasLocalScaleWhenVisible = m_CanvasRoot.localScale.x;

            AppSkin currentSkin = ProFitGlobals.appOptions.appSkin;

            m_Glyph.textMesh.color = currentSkin.resizerWidgetForegroundColor;
            m_Border.color = currentSkin.resizerWidgetForegroundColor;
            m_Background.color = currentSkin.resizerWidgetBackgroundColor;

            // Do appearance animation when first created
            m_Transform.localScale = Vector3.zero;
            SetVisible(true);
        }

        protected virtual void LateUpdate()
        {
            AnchorToTargetVolume();

            BillboardCanvasToCamera();
        }

        /// <summary>
        ///     Snap the widget to it's anchored position.
        /// </summary>
        protected void AnchorToTargetVolume()
        {
            // Snap to the anchored position
            if (m_TargetVolume != null)
            {
                Cuboid targetCuboid = m_TargetVolume.resizerCuboid;
                Vector3 localPosInCabinet = Cuboid.NormalizedToPoint(targetCuboid, m_NormalizedTargetAnchor);
                Vector3 worldPos = m_TargetVolume.GetTransform().TransformPoint(localPosInCabinet);
                m_Transform.position = worldPos;
            }
        }

        /// <summary>
        ///     Billboard the widget to the camera.
        /// </summary>
        protected void BillboardCanvasToCamera()
        {
            Camera camera = worldCamera;

            Vector3 cameraForward = camera.transform.forward;
            Vector3 lookPosition = m_CanvasRoot.position + cameraForward;
            m_CanvasRoot.LookAt(lookPosition, Vector3.up);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [MelcherHeader]

        [SerializeField]
        protected Graphic m_Background = default;

        [SerializeField]
        [Tooltip("The text mesh that displays the widget iconography: e.g., " +
            "an arrow when its resizable or a circle with a line through it " +
            "otherwise.")]
        protected MelcherLocalizedText m_Glyph = default;

        [Tooltip("The ring around the background. Matches the colour of the " +
            "glyph.")]
        [SerializeField]
        protected Graphic m_Border = default;

        [SerializeField]
        [Tooltip("This raycast target is only enabled when playing on " +
            "mobile. It has a larger hit box to make touches easier to detect.")]
        protected Graphic m_TouchHitbox = default;

        [SerializeField]
        [Tooltip("The number of seconds this widget will delay when it " +
            "animates its pop-in (being made visible).")]
        protected float m_PopInDelay = 0.0f;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Transform m_Transform = default;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down)]
        protected RectTransform m_CanvasRoot = default;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down)]
        protected Canvas m_WorldSpaceCanvas = default;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down)]
        protected GraphicRaycaster m_Raycaster = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        ///     The cuboid that this widget is capable of resizing.
        /// </summary>
        protected OccupiableVolume m_TargetVolume;

        /// <summary>
        ///     The normalized coordinates (0-1) of the widget relative to the
        ///     local space of its resizable cuboid. E.g., (0, 1, 1) could
        ///     correspond to the left-top-back corner of a cabinet cuboid.
        /// </summary>
        protected Vector3 m_NormalizedTargetAnchor;

        /// <summary>
        ///     The position of the pointer in the event camera's local space
        ///     at the time the last successful cabinet drag event occurred.
        ///     This is a reference position for the next drag/resize and is
        ///     only updated when a resize succeeds. This ensures the pointer
        ///     always matches the screen position of its widget.
        /// </summary>
        protected Vector3 m_LastCameraLocalDragPosition;

        protected RecyclableTween<Sequence> m_HideOrShowTween = new RecyclableTween<Sequence>();

        protected float m_CanvasLocalScaleWhenVisible;

        protected bool m_IsVisible;

    }   // class KitchenWidget

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-04-16
/// ===========================================================================
