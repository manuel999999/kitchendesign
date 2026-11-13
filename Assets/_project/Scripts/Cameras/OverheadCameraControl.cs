namespace KitchenDesign.Cameras
{
    using KitchenDesign.Utility;
    using Melcher;
    using Unity.Cinemachine;
    using MEC;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    ///     Controls the camera that users can switch to when they click the
    ///     "Top" button on the navigation UI. This camera view is also
    ///     referred to as the plan view in some places.
    /// </summary>
    public class OverheadCameraControl : MelcherMonoBehaviour
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public void SetPriority()
        {
            m_BlendCamera.Priority = 30;
            m_OrthoCamera.Priority = 20;

            Timing.KillCoroutines(_cameraOrthoTransition);

            _cameraOrthoTransition =
                Timing.RunCoroutine(
                    CutToOrthoAfterBlend(m_BlendCamera)
                );

            static IEnumerator<float> CutToOrthoAfterBlend(CinemachineCamera blendCamera)
            {
                CinemachineBrain brain =
                    CinemachineCore.FindPotentialTargetBrain(blendCamera);

                // Always wait one frame to allow the brain to realize it
                // is updating.
                yield return Timing.WaitForOneFrame;

                // Wait for the blending to complete before revoking the blend
                // camera's priority. This will mask the transition from the
                // blend camera to the intended camera.
                while (brain.IsBlending)
                {
                    yield return Timing.WaitForOneFrame;
                }

                // Revoke the blend camera's priority.
                if (CinemachineCore.IsLive(blendCamera))
                {
                    blendCamera.Priority = 0;
                }
            }
        }

        public void ClearPriority()
        {
            // Prioritize the blend camera. It will provide a smoother
            // transition to the side camera.
            m_BlendCamera.Priority = 30;
            m_OrthoCamera.Priority = 0;

            Timing.KillCoroutines(_cameraOrthoTransition);

            _cameraOrthoTransition =
                CoroutineUtility.CallDelayedByFrames(
                    actionArgument: m_BlendCamera,
                    action: blendCamera => blendCamera.Priority = 0
                );
        }

        public void AdjustFieldOfView(float percentChange)
        {
            const float MinimumOrthographicSize = 1.25f;
            if (percentChange < 1.0f && _orthoCameraDesiredOrthographicSize <= MinimumOrthographicSize)
            {
                return;
            }

            const float MaximumOrthographicSize = 10.0f;
            if (percentChange > 1.0f && _orthoCameraDesiredOrthographicSize >= MaximumOrthographicSize)
            {
                return;
            }

            _orthoCameraDesiredOrthographicSize *= percentChange;
            _blendCameraDesiredFieldOfView *= percentChange;
        }

        protected void Awake()
        {
            _orthoCameraDesiredOrthographicSize =
                m_OrthoCamera.Lens.OrthographicSize;

            _blendCameraDesiredFieldOfView =
                m_BlendCamera.Lens.FieldOfView;
        }

        protected void Update()
        {
            const float SmoothTimeInSeconds = 0.5f;

            m_OrthoCamera.Lens.OrthographicSize =
                Mathf.SmoothDamp(
                    m_OrthoCamera.Lens.OrthographicSize,
                    _orthoCameraDesiredOrthographicSize,
                    ref _orthoCameraOrthographicSizeVelocity,
                    SmoothTimeInSeconds
                );

            m_BlendCamera.Lens.FieldOfView =
                Mathf.SmoothDamp(
                    m_BlendCamera.Lens.FieldOfView,
                    _blendCameraDesiredFieldOfView,
                    ref _blendCameraFieldOfViewVelocity,
                    SmoothTimeInSeconds
                );
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Tooltip("A camera whose field-of-view is nearly orthogonal. Allows " +
            "us to create the appearance of blending from a perspective " +
            "camera to an orthographic one, something which is otherwise " +
            "not possible by default in Cinemachine.")]
        [SerializeField]
        protected CinemachineCamera m_BlendCamera = null;

        [SerializeField]
        protected CinemachineCamera m_OrthoCamera = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected CinemachineFollowZoom _blendCameraFollowZoom;

        protected CoroutineHandle _cameraOrthoTransition;

        protected float _orthoCameraDesiredOrthographicSize;
        protected float _orthoCameraOrthographicSizeVelocity;

        protected float _blendCameraDesiredFieldOfView;
        protected float _blendCameraFieldOfViewVelocity;

    }   // class OverheadCameraControl

}   // namespace KitchenDesign.Cameras

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-10-03
/// ===========================================================================
