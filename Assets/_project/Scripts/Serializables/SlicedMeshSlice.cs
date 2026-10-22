using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using Melcher;
using System.Collections.Generic;
using UnityEngine;

namespace KitchenDesign.Serializables
{
    [System.Serializable]
    public struct SlicedMeshSlice
    {
        public string name => m_Name;

        [SerializeField] private string m_Name;

        public Vector3 normalizedAnchor;

        [Tooltip("The length of mesh. Used to properly scale meshes " +
            "that are not a uniform 1 meter box. E.g., some cabinet " +
            "countertop meshes have a specific size. When any one of " +
            "these axises is 0, it is interpreted as 1 meter long.")]
        public Vector3 sizeInInches;

        public bool scaleAlongX;
        public bool scaleAlongY;
        public bool scaleAlongZ;

        public SlicedMeshAxisScaling scalingX;
        public SlicedMeshAxisScaling scalingY;
        public SlicedMeshAxisScaling scalingZ;

        public List<MeshRenderer> meshRenderers;
    }
}
