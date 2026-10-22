namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class DesignerNavigationUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void CloseHamburgerMenu()
        {
            m_MenuButton.ClosePanel();
        }

        protected override void Awake()
        {
            base.Awake();

            m_MeasureModeGroup.gameObject.SetActive(false);

            UpdateMeasureModeButtons();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_ToNearestOccupantMeasureModeButton.onClick += UseToNearestOccupantMeasureMode;
            m_ToVolumeEdgeMeasureModeButton.onClick += UseToVolumeEdgeMeasureMode;

            m_SideViewButton.onClick += UseSideView;
            m_TopViewButton.onClick += UseTopView;

            m_ZoomInButton.onClick += ZoomIn;
            m_ZoomOutButton.onClick += ZoomOut;
            m_ResetViewButton.onClick += ResetView;
            m_SearchButton.onClick += OpenProductSearch;

            m_RotateClockwiseButton.onClick += RotateClockwise;
            m_RotateCounterClockwiseButton.onClick += RotateCounterClockwise;

            EventBridge.Designer.restart += OnRestart;
            EventBridge.Designer.onHighlightChanged.OnInvoke += OnHighlightChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_ToNearestOccupantMeasureModeButton.onClick -= UseToNearestOccupantMeasureMode;
            m_ToVolumeEdgeMeasureModeButton.onClick -= UseToVolumeEdgeMeasureMode;

            m_SideViewButton.onClick -= UseSideView;
            m_TopViewButton.onClick -= UseTopView;

            m_ZoomInButton.onClick -= ZoomIn;
            m_ZoomOutButton.onClick -= ZoomOut;
            m_ResetViewButton.onClick -= ResetView;
            m_SearchButton.onClick -= OpenProductSearch;

            m_RotateClockwiseButton.onClick -= RotateClockwise;
            m_RotateCounterClockwiseButton.onClick -= RotateCounterClockwise;

            EventBridge.Designer.restart -= OnRestart;
            EventBridge.Designer.onHighlightChanged.OnInvoke -= OnHighlightChanged;
        }

        protected void Update()
        {
            // Allow mouse wheel to change zoom
            float scaledMouseWheelInput = Input.mouseScrollDelta.y * m_MouseWheelSensitivity;
            if (Application.isFocused &&
                !m_UsingTopView &&
                IsMouseWithinScreen() &&
                Mathf.Abs(scaledMouseWheelInput) > 0.01f &&
                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                m_OrbitCamera.AdjustZoom(scaledMouseWheelInput);
            }

            static bool IsMouseWithinScreen()
            {
                Vector2 mousePosition = Input.mousePosition;
                return
                    mousePosition.x >= 0.0f &&
                    mousePosition.y >= 0.0f &&
                    mousePosition.x < Screen.width &&
                    mousePosition.y < Screen.height;
            }
        }

        protected void UseToNearestOccupantMeasureMode(MelcherButton caller)
        {
            m_MeasurementOverlayManager.measureMode = MeasureMode.ToNearestOccupant;
            UpdateMeasureModeButtons();
        }

        protected void UseToVolumeEdgeMeasureMode(MelcherButton caller)
        {
            m_MeasurementOverlayManager.measureMode = MeasureMode.ToCabinetEdge;
            UpdateMeasureModeButtons();
        }

        protected void UseSideView(MelcherButton caller)
        {
            if (m_UsingTopView)
            {
                m_UsingTopView = false;

                m_OverheadCamera.ClearPriority();
            }
        }

        protected void UseTopView(MelcherButton caller)
        {
            if (!m_UsingTopView)
            {
                m_UsingTopView = true;

                m_OverheadCamera.SetPriority();
            }
        }

        protected void ZoomIn(MelcherButton caller)
        {
            if (!m_UsingTopView)
            {
                m_OrbitCamera.AdjustZoom(0.3f);
            }
            else
            {
                m_OverheadCamera.AdjustFieldOfView(percentChange: 0.8f);
            }
        }

        protected void ZoomOut(MelcherButton caller)
        {
            if (!m_UsingTopView)
            {
                m_OrbitCamera.AdjustZoom(-0.3f);
            }
            else
            {
                m_OverheadCamera.AdjustFieldOfView(percentChange: 1.2f);
            }
        }

        protected void OpenProductSearch(MelcherButton caller)
        {
            EventBridge.Designer.showProductSearchUi();
        }

        protected void ResetView(MelcherButton caller)
        {
            m_OrbitCamera.ResetView();
        }

        protected void OnRestart()
        {
            ResetView(null);

            m_MeasureModeGroup.gameObject.SetActive(false);
        }

        protected async UniTask OnHighlightChanged(GameObject highlightedObject)
        {
            bool showMeasureModeGroup =
                highlightedObject &&
                highlightedObject.TryGetComponent(out Occupant _);

            m_MeasureModeGroup.gameObject.SetActive(showMeasureModeGroup);

            UpdateMeasureModeButtons();

        }

        /// <summary>
        ///     To better communicate which mode is currently active, the
        ///     active mode's button is disabled.
        /// </summary>
        protected void UpdateMeasureModeButtons()
        {
            bool isUsingNearestMode =
                m_MeasurementOverlayManager.measureMode == MeasureMode.ToNearestOccupant;

            m_ToNearestOccupantMeasureModeIndicator.gameObject.SetActive(isUsingNearestMode);
            m_ToVolumeEdgeMeasureModeIndicator.gameObject.SetActive(!isUsingNearestMode);
        }


        protected void RotateClockwise(MelcherButton caller)
        {
            m_OrbitCamera.RotateClockwise();
        }

        protected void RotateCounterClockwise(MelcherButton caller)
        {
            m_OrbitCamera.RotateCounterClockwise();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DesignerNavigationUI))]

        [SerializeField] protected DesignerHamburgerMenu m_MenuButton = null;

        [Space]
        [SerializeField] protected RectTransform m_MeasureModeGroup = null;
        [SerializeField] protected MelcherButton m_ToNearestOccupantMeasureModeButton = null;
        [SerializeField] protected MelcherButton m_ToVolumeEdgeMeasureModeButton = null;
        [SerializeField] protected RectTransform m_ToNearestOccupantMeasureModeIndicator = null;
        [SerializeField] protected RectTransform m_ToVolumeEdgeMeasureModeIndicator = null;

        [Space]
        [SerializeField] protected MelcherButton m_SideViewButton = null;
        [SerializeField] protected MelcherButton m_TopViewButton = null;

        [Space]
        [SerializeField] protected float m_MouseWheelSensitivity = 0.3f;
        [SerializeField] protected MelcherButton m_ZoomInButton = null;
        [SerializeField] protected MelcherButton m_ZoomOutButton = null;
        [SerializeField] protected MelcherButton m_ResetViewButton = null;
        [SerializeField] protected MelcherButton m_SearchButton = null;

        [Space]
        [SerializeField] protected MelcherButton m_RotateClockwiseButton = null;
        [SerializeField] protected MelcherButton m_RotateCounterClockwiseButton = null;

        // ====================================================================
        // OnValidate-serialized field
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected DesignerOrbitCamera m_OrbitCamera = default;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected DesignerOverheadCamera m_OverheadCamera = default;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected MeasurementOverlayManager m_MeasurementOverlayManager = default;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected bool m_UsingTopView;

    }   // class DesignerNavigationUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
