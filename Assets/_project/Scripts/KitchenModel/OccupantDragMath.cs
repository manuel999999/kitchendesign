namespace KitchenDesign.KitchenModel
{
    using KitchenDesign.Occupancy;
    using Melcher;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public static class OccupantDragMath
    {
        public static Plane CalculateCameraLocalDragPlane(OccupiableVolume dragIsRelativeTo, Camera camera)
        {
            Transform resizableCuboidTransform = dragIsRelativeTo.GetTransform();

            Vector3 worldPlanePosition =
                resizableCuboidTransform.position + Vector3.up * dragIsRelativeTo.occupiableCuboid.size.y;

            Vector3 worldPlaneNormalCabinetTop = resizableCuboidTransform.up;
            Vector3 worldPlaneNormalCabinetFront = -resizableCuboidTransform.forward;

            Transform cameraTransform = camera.transform;
            Vector3 cameraDirection = cameraTransform.forward;
            float lookDotFront = Vector3.Dot(cameraDirection, worldPlaneNormalCabinetFront);
            float angleDegreesFromCabinetFront = Mathf.Acos(-lookDotFront) * Mathf.Rad2Deg;

            const float MaxDegreeAngleForHorizontalPlane = 65.0f;
            bool useTopPlane = angleDegreesFromCabinetFront > MaxDegreeAngleForHorizontalPlane;
            Vector3 cameraLocalPlaneNormal = cameraTransform.InverseTransformDirection(
                useTopPlane ? worldPlaneNormalCabinetTop : worldPlaneNormalCabinetFront
            );
            Vector3 cameraLocalPlanePosition = cameraTransform.InverseTransformPoint(worldPlanePosition);

            return new Plane(cameraLocalPlaneNormal, cameraLocalPlanePosition);
        }

        public static bool CalculateCameraLocalDragPosition(
            OccupiableVolume    dragIsRelativeTo,
            Camera              camera,
            PointerEventData    eventData,
            out Vector3         cameraLocalPosition,
            out Plane           cameraLocalDragPlane)
        {
            cameraLocalPosition = Vector3.zero;
            cameraLocalDragPlane = CalculateCameraLocalDragPlane(dragIsRelativeTo, camera);

            Ray worldRay = camera.ScreenPointToRay(eventData.position);

            Transform cameraTransform = camera.transform;

            Ray cameraLocalRay =
                new Ray(
                    cameraTransform.InverseTransformPoint(worldRay.origin),
                    cameraTransform.InverseTransformDirection(worldRay.direction)
                );

            if (!cameraLocalDragPlane.Raycast(cameraLocalRay, out float cameraLocalDistance))
            {
                MelcherLog.LogEditorOnly("Raycast didn't hit. Was this the backside?");
                return false;
            }

            cameraLocalPosition = cameraLocalRay.GetPoint(cameraLocalDistance);

            return true;
        }
    }
}
