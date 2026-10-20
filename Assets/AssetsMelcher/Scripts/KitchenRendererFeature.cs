namespace Melcher
{
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.SceneManagement;
 
    /// <summary>
    /// Draws cutouts, hollows and cabinets in the correct order to avoid
    /// issues.
    /// </summary>
    public class KitchenRendererFeature : ScriptableRendererFeature
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void LookForKitchen()
        {
            // Find the kitchen using `FindObjectOfType` rather than
            // `Kitchen.instance` to eliminate race conditions.
            Kitchen kitchen = GameObject.FindObjectOfType<Kitchen>();

            if (kitchen != null)
            {
                if (m_ShadowPass != null)
                {
                    m_ShadowPass.kitchen = kitchen;
                }

                if (m_ForwardPass != null)
                {
                    m_ForwardPass.kitchen = kitchen;
                }
            }
        }
    
        public override void Create()
        {
            m_ShadowPass = new KitchenShadowRenderPass(m_ShadowMaterial);

            m_ForwardPass = new KitchenForwardRenderPass(
                m_RenderingLayerIndexStart,
                m_CutOutStencilMaterial,
                m_CutOutStencilMask,
                m_NestedCutOutStencilMask,
                m_CutOutInteriorMask,
                m_NestedCutOutInteriorMask,
                m_CabinetMask,
                m_NestedCabinetMask
            );

            LookForKitchen();
        }
    
        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ShadowPass);
            renderer.EnqueuePass(m_ForwardPass);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= LookForKitchenInScene;
            SceneManager.sceneLoaded += LookForKitchenInScene;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= LookForKitchenInScene;
        }

        private void LookForKitchenInScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            LookForKitchen();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header("Shadow Pass")]
        [SerializeField] protected Material m_ShadowMaterial = null;

        [Header("Forward Pass")]

        // TODO: Could we get away with using Renderer.sortingOrder?
        [Tooltip("The index into Renderer.renderingLayerMask to start " +
            "when rendering cabinets. The cabinets are rendered in chunks, " +
            "nearest to farthest. Rendering cabinets is further broken down " +
            "into an ordered rending of cutouts, hollows and then the cabinet.")]
        [SerializeField] protected int m_RenderingLayerIndexStart = 8;

        [SerializeField] protected Material m_CutOutStencilMaterial = null;

        [Space]
        [SerializeField] protected LayerMask m_CutOutStencilMask = 0;
        [SerializeField] protected LayerMask m_NestedCutOutStencilMask = 0;
        [SerializeField] protected LayerMask m_CutOutInteriorMask = 0;
        [SerializeField] protected LayerMask m_NestedCutOutInteriorMask = 0;
        [SerializeField] protected LayerMask m_CabinetMask = 0;
        [SerializeField] protected LayerMask m_NestedCabinetMask = 0;

        // =====================================================================
        // Internal fields
        // =====================================================================

        private KitchenShadowRenderPass m_ShadowPass;
        private KitchenForwardRenderPass m_ForwardPass;
    
    }   // class KitchenRendererFeature

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-01
/// ===========================================================================
