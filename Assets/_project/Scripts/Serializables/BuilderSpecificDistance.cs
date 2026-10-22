using Cysharp.Threading.Tasks;
using KitchenDesign.Server;
using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using Melcher;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KitchenDesign.Serializables
{


    [System.Serializable]
    public struct BuilderSpecificDistance
    {
        public float proFit;

        public float stoll;
    }
}
