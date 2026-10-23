using UnityEngine;
using KitchenDesign.Occupancy;

namespace KitchenDesign.KitchenModel
{
    public sealed class KitchenInteractionState
    {
        public GameObject highlightObject;
        public OccupiableVolume volumeBeingResized;
        public Occupant productBeingDragged;
    }
}
