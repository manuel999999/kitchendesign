namespace Melcher
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;

    /// <summary>
    /// An <see cref="Accordion"/> is a series of nested layout groups that
    /// can be animated to collapse/hide its content and vice versa.
    /// </summary>
    public class Accordion : MelcherMonoBehaviour
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public bool isExpanded => m_IsExpanded;

        public LayoutGroup contentLayout => m_ContentLayout;

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Immediately expand or collapse the accordion.
        /// </summary>
        public void Snap(bool expand)
        {
            m_IsExpanded = expand;
            m_Sequence.KillIfActive();

            // Interactable when expanded, otherwise not
            m_CanvasGroup.interactable = expand;

            Vector4 maskPadding = -GetIdealPadding(expand, m_ShrinkFromEdge);
            AssignRectOffset(m_MaskLayout.padding, maskPadding);

            Vector4 rootPadding = m_BasePadding;
            if (m_PreserveRootSize)
            {
                rootPadding += GetIdealPadding(expand, m_ExpandFromEdge);
            }
            AssignRectOffset(m_RootLayout.padding, rootPadding);

            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)m_RootLayout.transform);
        }

        /// <summary>
        /// Use a tween to expand or collapse the accordion in <paramref
        /// name="duration"/> seconds.
        /// </summary>
        public void TweenUsingDuration(bool expand, float duration, Ease ease = Ease.OutExpo)
        {
            m_IsExpanded = expand;

            Vector4 maskPadding = Vector4.zero;
            Vector4 rootPadding = m_BasePadding;

            if (!expand)
            {
                maskPadding += -GetIdealPadding(expand, m_ShrinkFromEdge);
                rootPadding += m_PreserveRootSize ? GetIdealPadding(expand, m_ExpandFromEdge) : Vector4.zero;
            }

            m_CanvasGroup.interactable = expand;

            m_Sequence.Reset(
                DOTween.Sequence()
                    .Join(
                        TweenLayoutPadding(m_MaskLayout, maskPadding, duration)
                            .SetEase(ease)
                    )
                    .Join(
                        TweenLayoutPadding(m_RootLayout, rootPadding, duration)
                            .SetEase(ease)
                    )
            );
        }

        /// <summary>
        /// Use a tween to expand or collapse the accordion. The duration of
        /// the tween is calculated using the distance the accordion needs to
        /// move and the provided <paramref name="speed"/>.
        /// </summary>
        public void TweenUsingSpeed(bool expand, float speed, Ease ease = Ease.OutExpo)
        {
            Vector4 maskPadding = -GetIdealPadding(expand, m_ShrinkFromEdge) - m_BasePadding;
            float moveDistance = (m_MaskLayout.padding.ToVector4() - maskPadding).magnitude;
            float duration = moveDistance / speed;

            TweenUsingDuration(expand, duration, ease);
        }

        private IEnumerator Start()
        {
            yield return null;

            // Delay until the first layout happens
            Snap(expand: m_ExpandOnAwake);
        }

        private Tween TweenLayoutPadding(LayoutGroup layout, Vector4 endPadding, float duration)
        {
            return DOTween.To(
                getter: () => layout.padding.ToVector4(),
                setter: (vec4) =>
                {
                    layout.padding.FromVector4(vec4);
                    LayoutRebuilder.MarkLayoutForRebuild((RectTransform)layout.transform);
                },
                endValue: endPadding,
                duration: duration
            );
        }

        private Vector4 GetIdealPadding(bool isExpanded, Edge edge)
        {
            Vector4 padding = Vector4.zero;

            if (!isExpanded)
            {
                Rect contentRect = ((RectTransform)m_ContentLayout.transform).rect;
                float contentSize = IsVerticalEdge(edge) ? contentRect.height : contentRect.width;
                padding[(int)edge] = contentSize;
            }

            return padding;
        }

        public static bool IsVerticalEdge(Edge edge)
        {
            return edge == Edge.Top || edge == Edge.Bottom;
        }

        private static void AssignRectOffset(RectOffset offset, Vector4 vector)
        {
            offset.top      = Mathf.RoundToInt(vector[(int)Edge.Top]);
            offset.bottom   = Mathf.RoundToInt(vector[(int)Edge.Bottom]);
            offset.left     = Mathf.RoundToInt(vector[(int)Edge.Left]);
            offset.right    = Mathf.RoundToInt(vector[(int)Edge.Right]);
        }


        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [Tooltip("The accordion's root. If Preserve Root Size is enabled, " +
            "it's padding is counter-set to that of the mask layout's. This " +
            "ensures the space occupied by the accordion in it's parent " +
            "layout doesn't change whether it is expanded or collapsed. " +
            "This padding is adjusted from the Expand From Edge edge.")]
        [SerializeField] protected LayoutGroup m_RootLayout = null;

        [Tooltip("The accordion mask. Its padding is adjusted to shrink the " + 
            "accordion when it is collapsed. This padding is adjusted from " +
            "the Shrink From Edge edge.")]
        [SerializeField] protected LayoutGroup m_MaskLayout = null;

        [SerializeField] protected Vector4 m_BasePadding = Vector4.zero;

        [Tooltip("The final, most nested layout group of the accordion. It " +
            "is left untouched and is used only to calculate the content's " +
            "real full size when adjusting the Sizer or Offset layout padding.")]
        [SerializeField] protected LayoutGroup m_ContentLayout = null;

        [SerializeField] protected bool m_ExpandOnAwake = true;

        [Tooltip("The padding edge to modify on the mask as the accordion " +
            "is collapsed. This affects the visual animation of the collapse " +
            "and expand.")]
        [SerializeField] protected Edge m_ShrinkFromEdge = Edge.Bottom;

        [Tooltip("When enabled, the accordion's root layout will resize its " +
            "padding to ensure the accordion as whole occupies the same " +
            "space whether it is expanded or collapsed.")]
        [SerializeField] protected bool m_PreserveRootSize = false;

        [Tooltip("This determines which edge's padding is counter-offset " +
            "to preserve the accordion's root size. Only used when Preserve " +
            "Root Size is enabled.")]
        [SerializeField] protected Edge m_ExpandFromEdge = Edge.Top;


        // ====================================================================
        // OnValidate-serialized members
        // ====================================================================

        [Space]

        /// <summary>
        /// The canvas group is made non-interactable when the accordion is
        /// collapsed or collapsing.
        /// </summary>
        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected CanvasGroup m_CanvasGroup = null;

        protected RecyclableTween<Sequence> m_Sequence = new RecyclableTween<Sequence>();

        protected bool m_IsExpanded;

        // ====================================================================
        // Nested types
        // ====================================================================

        /// <remarks>
        /// This order is important as it lines up with how Unity internally
        /// indexes a RectOffset using a Vector4:
        /// left=x, right=y, top=z, bottom=w
        /// </remarks>
        public enum Edge
        {
            Left,
            Right,
            Top,
            Bottom
        }   // enum Edge

        // ====================================================================
        // Editor
        // ====================================================================

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_RootLayout != null)
            {
                Vector4 currentRootPadding = m_RootLayout.padding.ToVector4();
                if (currentRootPadding != m_BasePadding)
                {
                    m_RootLayout.padding.FromVector4(m_BasePadding);
                    m_ContentLayout.padding.FromVector4(-m_BasePadding);
                    LayoutRebuilder.MarkLayoutForRebuild(m_RootLayout.transform as RectTransform);
                }
            }
        }

        [UnityEditor.MenuItem("GameObject/UI/Accordion (Vertical)")]
        private static void CreateVerticalAccordion()
        {
            CreateAccordion(
                out GameObject accordionRoot,
                out GameObject accordionMask,
                out GameObject accordionContent
            );

            accordionRoot.name += "Vertical";

            VerticalLayoutGroup AddVerticalLayout(GameObject go)
            {
                //VerticalLayoutGroup layout = UnityEditor.Undo.AddComponent<VerticalLayoutGroup>(go);
                VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childScaleWidth = false;
                layout.childScaleHeight = false;
                return layout;
            }

            UnityEditor.Undo.AddComponent<CanvasGroup>(accordionRoot);

            //Accordion accordion = UnityEditor.Undo.AddComponent<Accordion>(accordionSizer);
            Accordion accordion = accordionRoot.AddComponent<Accordion>();
            accordion.m_RootLayout = AddVerticalLayout(accordionRoot);
            accordion.m_MaskLayout = AddVerticalLayout(accordionMask);
            accordion.m_ContentLayout = AddVerticalLayout(accordionContent);
            accordion.m_ShrinkFromEdge = Edge.Bottom;
            accordion.m_ExpandFromEdge = Edge.Top;
        }

        [UnityEditor.MenuItem("GameObject/UI/Accordion (Horizontal)")]
        private static void CreateHorizontalAccordion()
        {
            CreateAccordion(
                out GameObject accordionRoot,
                out GameObject accordionMask,
                out GameObject accordionContent
            );

            accordionRoot.name += "Horizontal";

            HorizontalLayoutGroup AddVerticalLayout(GameObject go)
            {
                HorizontalLayoutGroup layout = UnityEditor.Undo.AddComponent<HorizontalLayoutGroup>(go);
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childScaleWidth = false;
                layout.childScaleHeight = false;
                return layout;
            }

            UnityEditor.Undo.AddComponent<CanvasGroup>(accordionRoot);

            Accordion accordion = UnityEditor.Undo.AddComponent<Accordion>(accordionRoot);
            accordion.m_RootLayout = AddVerticalLayout(accordionRoot);
            accordion.m_MaskLayout = AddVerticalLayout(accordionMask);
            accordion.m_ContentLayout = AddVerticalLayout(accordionContent);
            accordion.m_ShrinkFromEdge = Edge.Left;
            accordion.m_ExpandFromEdge = Edge.Right;
        }

        private static void CreateAccordion(
            out GameObject accordionRoot,
            out GameObject accordionMask,
            out GameObject accordionContent)
        {
            const string UndoName = "Create accordion";
            int uiLayer = LayerMask.NameToLayer("UI");

            accordionRoot = new GameObject("Accordion");
            UnityEditor.Undo.RegisterCreatedObjectUndo(accordionRoot, UndoName);
            accordionRoot.layer = uiLayer;

            accordionMask = new GameObject("AccordionMask");
            //UnityEditor.Undo.RegisterCreatedObjectUndo(accordionPosition, UndoName);
            accordionMask.layer = uiLayer;
            accordionMask.transform.SetParent(accordionRoot.transform, worldPositionStays: false);

            accordionMask.AddComponent<CanvasRenderer>();
            accordionMask.AddComponent<Image>().raycastTarget = false;
            accordionMask.AddComponent<Mask>().showMaskGraphic = false;

            accordionContent = new GameObject("AccordionContent");
            //UnityEditor.Undo.RegisterCreatedObjectUndo(accordionContent, UndoName);
            accordionContent.layer = uiLayer;
            accordionContent.transform.SetParent(accordionMask.transform, worldPositionStays: false);

            GameObject activeGameObject = UnityEditor.Selection.activeGameObject;
            Canvas canvas = null;
            Transform parentToTransform = null;
            if (activeGameObject != null)
            {
                canvas = activeGameObject.GetComponentInParent<Canvas>();
                parentToTransform = activeGameObject.transform;
            }

            if (!canvas)
            {
                canvas = GameObject.FindObjectOfType<Canvas>();
                if (!canvas)
                {
                    UnityEditor.EditorApplication.ExecuteMenuItem("GameObject/UI/Canvas");
                    canvas = GameObject.FindObjectOfType<Canvas>();
                }
                parentToTransform = canvas?.transform;
            }

            accordionRoot.transform.SetParent(parentToTransform, worldPositionStays: false);
            UnityEditor.Selection.activeGameObject = accordionRoot;
        }

#endif  // if UNITY_EDITOR

    }   // class Accordion


}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-04
/// ===========================================================================
