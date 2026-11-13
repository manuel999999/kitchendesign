namespace KitchenDesign.Photos
{
    using Cysharp.Threading.Tasks;
    using KitchenDesign.Cabinets;
    using KitchenDesign.Geometry;
    using KitchenDesign.KitchenModel;
    using KitchenDesign.Measures;
    using KitchenDesign.Occupancy;
    using KitchenDesign.Products;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using Melcher;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;

    public static class OccupantNeighbourFinder
    {
        public static List<OccupantNearestNeighbours> FindNeighboursOfAllOccupants(Kitchen kitchen)
        {
            List<OccupantNearestNeighbours> occupantToNeighbours = new();

            foreach (ElementIndex<Cabinet> i in kitchen.layout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                foreach (Occupant occupant in cabinet.occupants)
                {
                    FindNeighboursOfOccupant(occupant, occupantToNeighbours);
                }
            }

            return occupantToNeighbours;

            static void FindNeighboursOfOccupant(Occupant occupant, List<OccupantNearestNeighbours> occupantToNeighbours)
            {
                OccupantNearestNeighbours entry = new ();

                entry.occupant = occupant;

                occupant.TryGetComponent(out CabinetShelf nestedVolume);

                if (occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Up))
                {
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Top, CabinetLocalDirection.Left);
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Top, CabinetLocalDirection.Right);
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Top, CabinetLocalDirection.Front);
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Top, CabinetLocalDirection.Back);
                }

                if (occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Front))
                {
                    bool allowSearchUp = occupant.occupiedVolume.parentVolume == null;

                    bool allowSearchDown = !nestedVolume;

                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Front, CabinetLocalDirection.Left);
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Front, CabinetLocalDirection.Right);

                    if (allowSearchUp)
                    {
                        FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Front, CabinetLocalDirection.Up);
                    }

                    if (allowSearchDown)
                    {
                        FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Front, CabinetLocalDirection.Down);
                    }
                }

                occupantToNeighbours.Add(entry);

                if (nestedVolume)
                {
                    foreach (Occupant nestedOccupant in nestedVolume.occupants)
                    {
                        FindNeighboursOfOccupant(nestedOccupant, occupantToNeighbours);
                    }
                }
            }

            static void FindNeighbourOfOccupantInDirection(
                Occupant                    occupant,
                OccupantNearestNeighbours   entry,
                ScreenshotAngle             screenshotAngle,
                CabinetLocalDirection       directionOfNeighbour)
            {
                OccupantAndDistance nearestNeighbour =
                    entry.GetNearestInDirection(screenshotAngle, directionOfNeighbour);

                OccupiableVolume.FindNearestOccupantPredicate neighbourPredicate = null;
                switch (screenshotAngle)
                {
                    case ScreenshotAngle.Top:
                        neighbourPredicate = IsFlushWithTop;
                        break;

                    case ScreenshotAngle.Front:
                        neighbourPredicate = IsFlushWithFront;
                        break;

                    default:
                        throw new System.NotImplementedException($"Unhandled {nameof(ScreenshotAngle)}: {screenshotAngle}");
                }

                OccupiableVolume occupiedVolume = occupant.occupiedVolume;

                nearestNeighbour.occupant =
                    occupiedVolume.FindNearestOccupantInDirection(
                        occupant,
                        directionOfNeighbour,
                        out nearestNeighbour.distanceInMeters,
                        neighbourPredicate
                    );

                if (nearestNeighbour.occupant == null && occupant.occupiedVolume is Cabinet occupiedCabinet)
                {
                    nearestNeighbour.distanceInMeters =
                        occupiedCabinet.AdjustCabinetDistanceToEdgeOfVisualArea(
                            nearestNeighbour.distanceInMeters,
                            directionOfNeighbour
                        );
                }

                entry.SetNearestInDirection(screenshotAngle, directionOfNeighbour, nearestNeighbour);
            }

            static bool IsFlushWithTop(Occupant otherOccupant)
            {
                return otherOccupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Up);
            }

            static bool IsFlushWithFront(Occupant otherOccupant)
            {
                return otherOccupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Front);
            }
        }

        public static List<OccupantMeasurementPair> FindUniqueOccupantMeasurementPairs(
            Kitchen kitchen,
            List<OccupantNearestNeighbours> occupantToNearestNeighbours)
        {
            List<OccupantMeasurementPair> measurementPairs = new ();

            foreach (OccupantNearestNeighbours nearestNeighbours  in occupantToNearestNeighbours)
            {
                Occupant occupant = nearestNeighbours.occupant;

                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Front, CabinetLocalDirection.Left);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Front, CabinetLocalDirection.Right);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Front, CabinetLocalDirection.Down);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Front, CabinetLocalDirection.Up);

                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Top, CabinetLocalDirection.Left);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Top, CabinetLocalDirection.Right);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Top, CabinetLocalDirection.Front);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Top, CabinetLocalDirection.Back);
            }

            return measurementPairs;

            void RegisterOccupantMeasurementPairIfUnique(
                List<OccupantMeasurementPair>   measurementPairs,
                Occupant                        occupant,
                OccupantNearestNeighbours       nearestNeighbours,
                ScreenshotAngle                 screenshotAngle,
                CabinetLocalDirection           direction)
            {
                OccupantAndDistance nearestNeighbourInDirection =
                    nearestNeighbours.GetNearestInDirection(screenshotAngle, direction);

                if (nearestNeighbourInDirection.distanceInMeters <= 0.0f)
                {
                    return;
                }

                float extraDistanceInMetersForLabelOnly = 0.0f;

                if (nearestNeighbourInDirection.occupant)
                {
                    Occupant a = occupant;
                    Occupant b = nearestNeighbourInDirection.occupant;
                    if (Occupant.CompareByCabinetPosition(a, b) > 0)
                    {
                        MelcherCommon.Swap(ref a, ref b);
                        direction = direction.Reverse();
                    }

                    foreach (OccupantMeasurementPair pair in measurementPairs)
                    {
                        if (pair.occupantA == a && pair.occupantB == b && pair.screenshotAngle == screenshotAngle)
                        {
                            return;
                        }
                    }

                    measurementPairs.Add(
                        new OccupantMeasurementPair(
                            a,
                            b,
                            nearestNeighbourInDirection.distanceInMeters,
                            extraDistanceInMetersForLabelOnly,
                            direction,
                            screenshotAngle
                        )
                    );
                }
                else
                {
                    if (occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Up) &&
                        direction.ToAxis() == MeasurementAxis.Y)
                    {
                        extraDistanceInMetersForLabelOnly = kitchen.countertopThicknessInMeters;
                    }

                    measurementPairs.Add(
                        new OccupantMeasurementPair(
                            occupant,
                            null,
                            nearestNeighbourInDirection.distanceInMeters,
                            extraDistanceInMetersForLabelOnly,
                            direction,
                            screenshotAngle
                        )
                    );
                }
            }
        }
    }
}
