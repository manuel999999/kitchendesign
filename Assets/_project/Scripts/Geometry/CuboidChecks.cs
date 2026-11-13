namespace KitchenDesign.Geometry
{
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using Melcher;
    using UnityEngine;

    public static class CuboidChecks
    {
        public static bool IsDistanceAlmostZero(float meters)
        {
            return Mathf.Abs(meters) < (Units.Inches2Meters * 0.0625f);
        }

        public static bool IsCuboidContainedWithin(Cuboid toBeContained, Cuboid toDoContaining)
        {
            return CuboidOverlapCheck(toBeContained, toDoContaining) == OccupantCuboidStatus.Fits;
        }

        public static OccupantCuboidStatus CuboidOverlapCheck(Cuboid toBeContained, Cuboid toDoContaining)
        {
            OccupantCuboidStatus cuboidStatus;

            const float Threshold = 1e-8f;

            toBeContained.xMin += Threshold;
            toBeContained.xMax -= Threshold;
            toBeContained.yMin += Threshold;
            toBeContained.yMax -= Threshold;
            toBeContained.zMin += Threshold;
            toBeContained.zMax -= Threshold;

            if (SetStatusThenReturnResult(OccupantCuboidStatus.TooFarLeft,      toBeContained.xMin < toDoContaining.xMin) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarRight,     toBeContained.xMax > toDoContaining.xMax) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarDown,      toBeContained.yMin < toDoContaining.yMin) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarUp,        toBeContained.yMax > toDoContaining.yMax) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarForward,   toBeContained.zMin < toDoContaining.zMin) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarBack,      toBeContained.zMax > toDoContaining.zMax))
            {
                MelcherLog.Log("Failing on condition " + cuboidStatus, severity: MelcherLogSeverity.Verbose);
                MelcherLog.Log($"To be contained '{toBeContained}' vs container '{toDoContaining}", severity: MelcherLogSeverity.Verbose);
            }
            else
            {
                cuboidStatus = OccupantCuboidStatus.Fits;
            }

            return cuboidStatus;

            bool SetStatusThenReturnResult(OccupantCuboidStatus status, bool testResult)
            {
                cuboidStatus = status;

                return testResult;
            }
        }
    }
}
