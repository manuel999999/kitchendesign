namespace KitchenDesign
{
    using Melcher;
    using Unity.Cinemachine;
    using DG.Tweening;
    using UnityEngine;
    using KitchenDesign.Cabinets;

    /// <summary>
    ///     The main camera used by the design scene. This camera can rotate
    ///     and zoom in or out. It is sometimes referred to as the side camera
    ///     to distinguish it from the top camera.
    /// </summary>
    public class DesignerOrbitCamera : MelcherMonoBehaviour
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public float rotationIntervalInDegrees => m_RotateDegrees;
        public Transform orbitRoot => m_OrbitRoot;
        public CinemachineCamera virtualCamera => m_OrbitCamera;
        public float initialCameraGroupFramingSize => m_InitialSideCameraGroupFramingSize;

        // ====================================================================
        // Methods
        // ====================================================================

        public void AdjustRotation(float deltaAngle)
        {
            const float TweenSeconds = 1.0f;

            m_TargetCameraYaw += deltaAngle;

            m_CameraRotateTween.Reset(
                m_OrbitRoot.DOLocalRotateQuaternion(Quaternion.Euler(Vector3.up * m_TargetCameraYaw), TweenSeconds)
                    .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                    .SetEase(Ease.OutQuad)
            );
        }

        public void AdjustZoom(float deltaZoom)
        {
            const float TweenSeconds = 0.3f;

            m_SideCameraGroupFramingSize = Mathf.Clamp(m_SideCameraGroupFramingSize + deltaZoom, 0.0f, 1.8f);

            m_CameraZoomSequence.Reset(
                DOTween.Sequence()
                    .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                    .Join(
                        TweenFramingSize(m_OrbitCamera, m_SideCameraGroupFramingSize, TweenSeconds)
                    ),
                completePrevious: true
            );

            static Tween TweenFramingSize(CinemachineCamera virtualCamera, float endValue, float duration)
            {
                CinemachineGroupFraming groupComposer = virtualCamera.GetComponent<CinemachineGroupFraming>();

                return DOTween.To(
                    getter: () => groupComposer.FramingSize,
                    setter: f => groupComposer.FramingSize = f,
                    endValue,
                    duration
                );
            }
        }

        public void RotateClockwise()
        {
            AdjustRotation(m_RotateDegrees);
        }

        public void RotateCounterClockwise()
        {
            AdjustRotation(-m_RotateDegrees);
        }

        public void ResetView()
        {
            m_OrbitRoot.eulerAngles = new Vector3(0.0f, m_InitialCameraYaw, 0.0f);
            m_TargetCameraYaw = m_InitialCameraYaw;

            CinemachineGroupFraming sideGroupComposer = m_OrbitCamera.GetComponent<CinemachineGroupFraming>();
            sideGroupComposer.FramingSize = m_InitialSideCameraGroupFramingSize;

            //CinemachineGroupComposer topGroupComposer = m_TopCamera.GetCinemachineComponent<CinemachineGroupComposer>();
            //topGroupComposer.m_GroupFramingSize = m_InitialTopCameraGroupFramingSize;
            //m_TopCameraGroupFramingSize = m_InitialTopCameraGroupFramingSize;
        }

        protected void Awake()
        {
            m_InitialCameraYaw = m_OrbitRoot.eulerAngles.y;
            m_TargetCameraYaw = m_InitialCameraYaw;

            CinemachineGroupFraming sideGroupComposer =
                m_OrbitCamera.GetComponent<CinemachineGroupFraming>();
            m_InitialSideCameraGroupFramingSize = sideGroupComposer.FramingSize;
            m_SideCameraGroupFramingSize = m_InitialSideCameraGroupFramingSize;
        }

        protected void OnEnable()
        {
            EventBridge.Designer.restart += ResetView;
            EventBridge.Designer.onKitchenLayoutChanged += OnKitchenCabinetLayoutChanged;
        }

        protected void OnDisable()
        {
            EventBridge.Designer.restart -= ResetView;
            EventBridge.Designer.onKitchenLayoutChanged -= OnKitchenCabinetLayoutChanged;
        }

        protected void OnKitchenCabinetLayoutChanged(CabinetLayout layoutPrefab, CabinetLayout layoutInstance)
        {
            if (layoutPrefab)
            {
                // Rotate the camera to match the initial camera yaw requested
                // by the kitchen layout.

                m_OrbitRoot.eulerAngles = Vector3.up * layoutPrefab.designerCameraStartYaw;
                m_TargetCameraYaw = layoutPrefab.designerCameraStartYaw;
            }
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Space]

        [SerializeField]
        protected float m_RotateDegrees = 45.0f;

        [SerializeField]
        protected Transform m_OrbitRoot = null;

        [SerializeField]
        protected CinemachineCamera m_OrbitCamera = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected RecyclableTween<Sequence> m_CameraZoomSequence = new ();

        protected RecyclableTween<Tween> m_CameraRotateTween = new ();

        protected float m_InitialSideCameraGroupFramingSize;
        protected float m_SideCameraGroupFramingSize;

        protected float m_InitialCameraYaw;
        protected float m_TargetCameraYaw;
    }   // class DesignerOrbitCamera

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-08-28
/// ===========================================================================
