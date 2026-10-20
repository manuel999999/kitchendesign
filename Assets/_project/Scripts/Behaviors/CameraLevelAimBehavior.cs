using Unity.Cinemachine;
using UnityEngine;

namespace KitchenDesign
{
    [DisallowMultipleComponent]
    public class CameraLevelAimBehavior : CinemachineExtension
    {
        [Tooltip("World-space offset added to the framed aim point, measured at the Look At target's distance.")]
        [SerializeField] private Vector3 worldAimOffset = Vector3.up;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize)
            {
                return;
            }

            if (vcam.LookAt == null)
            {
                return;
            }

            Vector3 position = state.GetCorrectedPosition();
            Quaternion orientation = state.GetCorrectedOrientation();
            float distance = Vector3.Distance(position, vcam.LookAt.position);
            Vector3 forward = orientation * Vector3.forward * distance + this.worldAimOffset;

            if (Vector3.Cross(forward, state.ReferenceUp).sqrMagnitude < 0.000001f)
            {
                return;
            }

            Quaternion level = Quaternion.LookRotation(forward, state.ReferenceUp);
            state.OrientationCorrection = Quaternion.Inverse(state.RawOrientation) * level;
        }
    }
}
