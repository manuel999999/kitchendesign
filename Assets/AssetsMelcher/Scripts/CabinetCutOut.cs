namespace Melcher
{
    using System;
    using KitchenDesign.Shared.Enums;
    using UnityEngine;

    /// <summary>
    /// A cut out has a stencil which is used to mask the cabinet and an
    /// interior that is rendered within the stencil to give the illusion of
    /// a hollow cavity.
    /// </summary>
    public class CabinetCutOut : MelcherMonoBehaviour
    {
        // =====================================================================
        // Properties
        // =====================================================================
        public bool isCountertop => m_IsCountertop;

        public Transform stencilRoot => m_StencilRoot;
        public Transform interiorRoot => m_InteriorRoot;

        public TransformHelper transformHelper => m_TransformHelper;

        public ComponentsInChildrenEnumerator<MeshRenderer> stencils =>
            new ComponentsInChildrenEnumerator<MeshRenderer>(m_StencilRoot);

        public ComponentsInChildrenEnumerator<MeshRenderer> interiors =>
            new ComponentsInChildrenEnumerator<MeshRenderer>(m_InteriorRoot);

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     This function changes the render layers of the cut-out meshes.
        ///     It is intended to be called after a product is added to or
        ///     removed from a <see cref="CabinetShelf"/> as at that point its
        ///     drawing behaviour needs to change.
        ///     <br /><br />
        ///
        ///     By changing the render layer, we allow <see
        ///     cref="KitchenForwardRenderPass"/> to selectively draw the
        ///     non-nested cut-outs first and then only draw the nested
        ///     cut-outs within those.
        /// </summary>
        /// <param name="interiorLayer"></param>
        /// <param name="stencilLayer"></param>
        /// <param name="isCutOutOnOrAtFront"></param>
        public void SetRenderLayers(int interiorLayer, int stencilLayer, bool isCutOutOnOrAtFront)
        {
            m_InteriorRoot.SetLayerRecursively(interiorLayer, includeSelf: true);

            // Set the stencil cube's render layer but avoid changing the face
            // that is on the outside of the cabinet.
            foreach (CubeFace cubeFace in s_CubeFaces)
            {
                if ((cubeFace == CubeFace.ZMin && isCutOutOnOrAtFront) ||
                    (cubeFace == CubeFace.ZMax && !isCutOutOnOrAtFront))
                {
                    continue;
                }

                GetCubeFace(m_StencilRoot, cubeFace).layer = stencilLayer;
            }
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
                ProFitUtility.SetAllRendererMaterials(renderers.Current, material);
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

        public GameObject GetStencilFace(CubeFace face)
        {
            return GetCubeFace(m_StencilRoot, face);
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

        [Tooltip("Stencils are the 'windows' that look into the cabinet. " +
            "The cut-out's interiors are only visible through the windows.")]
        [SerializeField] protected Transform m_StencilRoot = null;

        [Tooltip("The root of the interior faces within the cut-out. " +
            "Interiors can catch shadows and are only rendered where " +
            "stencils are visible.")]
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

        [Tooltip("The draw order indicates the priority to use when " +
            "ordering which cut-outs are rendered first. " +
            "Groups are drawn far-to-near cut-out will " +
            "be drawn with. All cut-outs with the same draw order are drawn " +
            "first values are drawn first")]
        [SerializeField] protected int m_DrawOrder = default;

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

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-18
/// ===========================================================================
