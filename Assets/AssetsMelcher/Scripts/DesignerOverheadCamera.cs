namespace KitchenDesign
{
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
    public class DesignerOverheadCamera : MelcherMonoBehaviour
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public void SetPriority()
        {
            m_BlendCamera.Priority = 30;
            m_OrthoCamera.Priority = 20;

            Timing.KillCoroutines(m_CameraOrthoTransition);

            m_CameraOrthoTransition =
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

            Timing.KillCoroutines(m_CameraOrthoTransition);

            m_CameraOrthoTransition =
                ProFitCoroutineUtility.CallDelayedByFrames(
                    actionArgument: m_BlendCamera,
                    action: blendCamera => blendCamera.Priority = 0
                );
        }

        public void AdjustFieldOfView(float percentChange)
        {
            const float MinimumOrthographicSize = 1.25f;
            if (percentChange < 1.0f && m_OrthoCameraDesiredOrthographicSize <= MinimumOrthographicSize)
            {
                return;
            }

            const float MaximumOrthographicSize = 10.0f;
            if (percentChange > 1.0f && m_OrthoCameraDesiredOrthographicSize >= MaximumOrthographicSize)
            {
                return;
            }

            m_OrthoCameraDesiredOrthographicSize *= percentChange;
            m_BlendCameraDesiredFieldOfView *= percentChange;
        }

        protected void Awake()
        {
            m_OrthoCameraDesiredOrthographicSize =
                m_OrthoCamera.Lens.OrthographicSize;

            m_BlendCameraDesiredFieldOfView =
                m_BlendCamera.Lens.FieldOfView;
        }

        protected void Update()
        {
            const float SmoothTimeInSeconds = 0.5f;

            m_OrthoCamera.Lens.OrthographicSize =
                Mathf.SmoothDamp(
                    m_OrthoCamera.Lens.OrthographicSize,
                    m_OrthoCameraDesiredOrthographicSize,
                    ref m_OrthoCameraOrthographicSizeVelocity,
                    SmoothTimeInSeconds
                );

            m_BlendCamera.Lens.FieldOfView =
                Mathf.SmoothDamp(
                    m_BlendCamera.Lens.FieldOfView,
                    m_BlendCameraDesiredFieldOfView,
                    ref m_BlendCameraFieldOfViewVelocity,
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

        protected CinemachineFollowZoom m_BlendCameraFollowZoom;

        protected CoroutineHandle m_CameraOrthoTransition;

        protected float m_OrthoCameraDesiredOrthographicSize;
        protected float m_OrthoCameraOrthographicSizeVelocity;

        protected float m_BlendCameraDesiredFieldOfView;
        protected float m_BlendCameraFieldOfViewVelocity;

    }   // class DesignerOverheadCamera

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-10-03
/// ===========================================================================
