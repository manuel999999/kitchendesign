namespace KitchenDesign
{
    using Melcher;
    using System;
    using KitchenDesign.Shared.Enums;
    using UnityEngine;

    public class CabinetCutOut : MelcherMonoBehaviour
    {
        // =====================================================================
        // Properties
        // =====================================================================
        public bool isCountertop => m_IsCountertop;

        public Transform stencilRoot => m_StencilRoot;
        public Transform interiorRoot => m_InteriorRoot;

        public TransformHelper transformHelper => m_TransformHelper;

        public ComponentsInChildrenEnumerator<MeshRenderer> interiors =>
            new ComponentsInChildrenEnumerator<MeshRenderer>(m_InteriorRoot);

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetInteriorLayer(int interiorLayer)
        {
            m_InteriorRoot.SetLayerRecursively(interiorLayer, includeSelf: true);
        }

        /// <summary>
        /// Set the material of the interior cut-out geometry. The cut-out may
        /// be either a cabinet or countertop cut-out.
        /// </summary>
        /// <param name="material"></param>
        public void SetInteriorMaterial(Material material)
        {
            var renderers = interiors;
            while (renderers.MoveNext())
            {
                Utility.RendererUtility.SetAllRendererMaterials(renderers.Current, material);
            }
        }

        /// <summary>
        /// Set which cut-out faces are active. Each value that is passed as
        /// <see langword="true"/> means that face is cut-out and is active.
        ///
        /// This function needs to be called anytime a product's inset or
        /// position changes because the required stencils may change.
        /// </summary>
        public void SetActiveStencils(bool cutOutFront, bool cutOutTop, bool cutOutBack)
        {
            // Assume all stencils off by default
            foreach (CubeFace face in EnumInfo<CubeFace>.values)
            {
                SetStencilFaceActive(face, active: false);
            }

            if (cutOutFront)
            {
                SetStencilFaceActive(CubeFace.ZMin, active: true);
            }

            if (cutOutTop)
            {
                SetStencilFaceActive(CubeFace.YMax, active: true);
            }

            if (cutOutBack)
            {
                SetStencilFaceActive(CubeFace.ZMax, active: true);
            }

            if (m_IsCountertop)
            {
                // Always hide the bottom interior face of the countertop
                // cut-out. This lets us look into the cabinet cut-out below it
                GetCubeFace(interiorRoot, CubeFace.YMin).gameObject.SetActive(false);
            }
            else
            {
                // This is a cabinet cut-out because it's not a countertop

                // If the cut-out is active, we want the top interior face
                // hidden so we can look up into the countertop cut-out. If
                // there's no top cut-out, we can show the interior face.
                bool showCabinetTopInteriorFace = !cutOutTop;
                GetCubeFace(interiorRoot, CubeFace.YMax).gameObject.SetActive(showCabinetTopInteriorFace);
            }

            if (m_IsCabinetShelf)
            {
                // Hide the bottom-most interior face on cabinet shelves. This
                // ensures it doesn't z-fight with the separate countertop
                // section that will render beneath it.
                GetCubeFace(interiorRoot, CubeFace.YMin).gameObject.SetActive(false);
            }
        }

        public bool TryGetClipBox(out Bounds localBounds, out CutoutFaces openFaces)
        {
            localBounds = default;
            openFaces = CutoutFaces.None;

            if (!gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!m_HasClipBounds)
            {
                m_HasClipBounds = TryMeasureStencilBounds(out m_ClipBounds);
            }

            if (!m_HasClipBounds)
            {
                return false;
            }

            localBounds = m_ClipBounds;

            foreach (CubeFace face in s_CubeFaces)
            {
                bool isWindow = GetCubeFace(m_StencilRoot, face).activeSelf;
                bool isShared = m_IsCountertop
                    ? face == CubeFace.YMin
                    : face == CubeFace.YMax && !GetCubeFace(m_InteriorRoot, face).activeSelf;

                if (isWindow || isShared)
                {
                    openFaces |= (CutoutFaces)(1 << (int)face);
                }
            }

            return true;
        }

        private bool TryMeasureStencilBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (MeshFilter meshFilter in m_StencilRoot.GetComponentsInChildren<MeshFilter>(includeInactive: true))
            {
                if (!meshFilter.sharedMesh)
                {
                    continue;
                }

                Matrix4x4 toRoot = m_StencilRoot.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                Bounds meshBounds = meshFilter.sharedMesh.bounds;

                for (int corner = 0; corner < 8; ++corner)
                {
                    Vector3 point = toRoot.MultiplyPoint3x4(new Vector3(
                        (corner & 1) == 0 ? meshBounds.min.x : meshBounds.max.x,
                        (corner & 2) == 0 ? meshBounds.min.y : meshBounds.max.y,
                        (corner & 4) == 0 ? meshBounds.min.z : meshBounds.max.z));

                    if (!hasBounds)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            return hasBounds;
        }

        public GameObject GetInteriorFace(CubeFace face)
        {
            return GetCubeFace(m_InteriorRoot, face);
        }

        private void Awake()
        {
            // Hide bottom of countertop cut-out so we can view into
            // cabinet cut-out down it
            interiorRoot.GetChild((int)CubeFace.YMin).gameObject.SetActive(!m_IsCountertop);
        }

        protected GameObject GetCubeFace(Transform cube, CubeFace face)
        {
            return cube.GetChild((int)face).gameObject;
        }

        private void SetStencilFaceActive(CubeFace face, bool active)
        {
            GetCubeFace(m_StencilRoot, face).gameObject.SetActive(active);
            GetCubeFace(m_InteriorRoot, face).gameObject.SetActive(!active);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Tooltip("The window cube: its mesh bounds are the clip box, and " +
            "its active faces are the box's open faces. It isn't drawn.")]
        [SerializeField] protected Transform m_StencilRoot = null;

        [Tooltip("The root of the interior faces within the cut-out. " +
            "Interiors can catch shadows and show where the cabinet is " +
            "clipped away.")]
        [SerializeField] protected Transform m_InteriorRoot = null;

        [Tooltip("Set this to true if the cut out is for the countertop " +
            "and the product is mounted to the front or back edge. This " +
            "will be used to extend the cut out to match the countertop's " +
            "overhang/lip.")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_ApplyCountertopOverhang")]
        [SerializeField] protected bool m_IsCountertop = false;

        [SerializeField]
        [Tooltip("This should be set to true when it is a cabinet cut-out " +
            "that belongs to a cabinet shelf. This ensures the y-min " +
            "interior face is hidden so the shelf's nested countertop mesh " +
            "doesn't z-fight with the interior face.")]
        protected bool m_IsCabinetShelf = default;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        private TransformHelper m_TransformHelper = null;

        // ====================================================================
        // Internal static fields
        // ====================================================================

        static protected CubeFace[] s_CubeFaces =
            System.Linq.Enumerable.ToArray(MelcherEnumInfo<CubeFace>.values);

        private bool m_HasClipBounds;
        private Bounds m_ClipBounds;

        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        /// Symbolic names for the faces of a cube. These values correspond to
        /// to the order of the submeshes within the meter cube/cut-out cube
        /// model.
        /// </summary>
        public enum CubeFace
        {
            XMax,
            XMin,
            YMax,
            YMin,
            ZMax,
            ZMin
        }   // enum CubeFace

    }   // class CabinetCutOut

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-18
/// ===========================================================================
