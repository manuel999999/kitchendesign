namespace KitchenDesign.KitchenModel
{
    using KitchenDesign.Cabinets;
    using KitchenDesign.Cameras;
    using System.Collections.Generic;
    using Unity.Cinemachine;
    using UnityEngine;

    internal sealed class KitchenCameraControl
    {
        private readonly OrbitCameraControl _orbitCamera;
        private readonly CinemachineCamera _photoCoverView;
        private readonly Transform _emptyTargetGroup;
        private readonly CinemachineTargetGroup _targetGroup;
        private readonly Transform _ground;

        internal KitchenCameraControl(
            OrbitCameraControl orbitCamera,
            CinemachineCamera photoCoverView,
            Transform emptyTargetGroup,
            CinemachineTargetGroup targetGroup,
            Transform ground)
        {
            this._orbitCamera = orbitCamera;
            this._photoCoverView = photoCoverView;
            this._emptyTargetGroup = emptyTargetGroup;
            this._targetGroup = targetGroup;
            this._ground = ground;
        }

        internal Camera GetCamera()
        {
            return Camera.main;
        }

        internal void RotateToFrontOfCabinet(ICabinet cabinet)
        {
            Vector3 cabinetFrontToBack =
                cabinet.GetTransform().forward;

            Debug.DrawRay(cabinet.GetTransform().position, cabinet.GetTransform().forward, Color.red, 5.0f);

            Vector3 orbitForward =
                this._orbitCamera.orbitRoot.forward;

            Debug.DrawRay(this._orbitCamera.orbitRoot.position, this._orbitCamera.orbitRoot.forward, Color.green, 5.0f);

            float dot = Vector3.Dot(cabinetFrontToBack, orbitForward);

            if (dot < -0.05f)
            {
                float angleToRotateCamera =
                    Vector3.SignedAngle(cabinetFrontToBack, orbitForward, Vector3.up);

                if (angleToRotateCamera > 0.0f)
                {
                    angleToRotateCamera -=
                        this._orbitCamera.rotationIntervalInDegrees;
                }
                else
                {
                    angleToRotateCamera +=
                        this._orbitCamera.rotationIntervalInDegrees;
                }

                this._orbitCamera.AdjustRotation(-angleToRotateCamera);
            }
        }

        internal void FrameLayout(CabinetLayout layout)
        {
            this._orbitCamera.virtualCamera.LookAt = this._emptyTargetGroup.transform;
            this._photoCoverView.LookAt = this._emptyTargetGroup.transform;

            if (layout && layout.GetTransform().childCount > 0)
            {
                Transform layoutTransform = layout.GetTransform();

                int childCount = layoutTransform.childCount;
                List<Transform> transformsToFrame = new List<Transform>(capacity: childCount);

                for (int i = 0; i < childCount; ++i)
                {
                    Transform child = layoutTransform.GetChild(i);

                    Cabinet cabinet = child.GetComponent<Cabinet>();

                    if (cabinet.isCornerOrEnd)
                    {
                        transformsToFrame.Add(child);
                    }
                }

                this._targetGroup.Targets.Clear();

                for (int i = 0; i < transformsToFrame.Count; ++i)
                {
                    this._targetGroup.Targets.Add(
                        new CinemachineTargetGroup.Target
                        {
                            Object = transformsToFrame[i],
                            Weight = 1.0f,
                            Radius = 2.0f
                        }
                    );
                }

                this._orbitCamera.virtualCamera.LookAt = this._targetGroup.transform;
                this._photoCoverView.LookAt = this._targetGroup.transform;
            }
            else
            {
                this._targetGroup.Targets.Clear();
            }
        }

        internal void MoveOrbitRootToCenter()
        {
            if (this.TryGetCenterOfTargets(out Vector3 targetWorldCenter))
            {
                this._orbitCamera.orbitRoot.position = targetWorldCenter;
            }
        }

        internal void CenterTheGround()
        {
            if (this.TryGetCenterOfTargets(out Vector3 targetWorldCenter))
            {
                this._ground.position = targetWorldCenter;
            }
        }

        private bool TryGetCenterOfTargets(out Vector3 targetWorldCenter)
        {
            targetWorldCenter = Vector3.zero;

            bool foundPoint = false;
            Bounds bounds = new Bounds();
            foreach (CinemachineTargetGroup.Target target in this._targetGroup.Targets)
            {
                if (target == null || !target.Object)
                {
                    continue;
                }

                if (!foundPoint)
                {
                    foundPoint = true;
                    bounds = new Bounds(target.Object.position, size: Vector3.zero);
                }
                else
                {
                    bounds.Encapsulate(target.Object.position);
                }
            }

            if (foundPoint)
            {
                targetWorldCenter = bounds.center;
                targetWorldCenter.y = 0.0f;
            }

            return foundPoint;
        }
    }
}
