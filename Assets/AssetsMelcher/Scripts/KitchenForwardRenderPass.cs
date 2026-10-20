namespace Melcher
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// Uses the stencil mask and ordered cabinet rendering to make convincing
    /// cutouts.
    /// </summary>
    class KitchenForwardRenderPass : ScriptableRenderPass
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="renderingLayerIndexStart"></param>
        /// <param name="stencilMaterial">
        ///     The material used during stencil-write operations. It's shader
        ///     should not render color or depth information but only modify
        ///     the stencil buffer. It should also render using front-face
        ///     culling to invert the mesh.
        /// </param>
        /// <param name="stencilMask">
        ///     The mask used by non-nested cut-out windows.
        /// </param>
        /// <param name="nestedStencilMask">
        ///     The mask used by nested cut-out windows. These windows will
        ///     only render within the window of another non-nested cut-out
        ///     window. A full cut-out object may have some faces that are
        ///     nested and others that are not.
        /// </param>
        /// <param name="interiorMask">
        ///     The mask used by interior cut-out faces (hollows) that
        ///     are not themselves nested within another cut-out window.
        /// </param>
        /// <param name="nestedInteriorMask">
        ///     The mask used by interior cut-out faces (hollows) that are
        ///     nested within another cut-out.
        /// </param>
        /// <param name="cabinetMask"></param>
        public KitchenForwardRenderPass(
            int renderingLayerIndexStart,
            Material stencilMaterial,
            LayerMask stencilMask,
            LayerMask nestedStencilMask,
            LayerMask interiorMask,
            LayerMask nestedInteriorMask,
            LayerMask cabinetMask,
            LayerMask nestedCabinetMask)
        {
            m_RenderingLayerIndexStart = renderingLayerIndexStart;

            m_StencilMaterial = stencilMaterial;

            m_CutOutStencilMask         = stencilMask;
            m_NestedCutOutStencilMask   = nestedStencilMask;

            m_CutOutInteriorMask        = interiorMask;
            m_NestedCutOutInteriorMask  = nestedInteriorMask;

            m_CabinetMask = cabinetMask;
            m_NestedCabinetMask = nestedCabinetMask;

            profilingSampler = new ProfilingSampler(nameof(KitchenForwardRenderPass));
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

            m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));

            // Create profilers so we more easily debug the render pass in the
            // Frame Debugger
            m_ProfilerForCabinets       = new ProfilingSampler(nameof(KitchenForwardRenderPass) + ".DrawCabinets");
            m_ProfilerForNestedCabinets = new ProfilingSampler(nameof(KitchenForwardRenderPass) + ".DrawCabinetsNested");
            m_ProfilerForInteriors      = new ProfilingSampler(nameof(KitchenForwardRenderPass) + ".DrawInteriors");
            m_ProfilerForNestedInteriors = new ProfilingSampler(nameof(KitchenForwardRenderPass) + ".DrawInteriorsNested");
            m_ProfilerForStencils       = new ProfilingSampler(nameof(KitchenForwardRenderPass) + ".DrawStencils");
            m_ProfilerForNestedStencils = new ProfilingSampler(nameof(KitchenForwardRenderPass) + ".DrawStencilsNested");

        }

#if !UNITY_6000_0_OR_NEWER
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_WillExecute = false;

            if (!kitchen)
            {
                //Debug.Log("No kitchen");
                return;
            }

            m_WillExecute = true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!m_WillExecute)
            {
                return;
            }

            // Calculate the view's world position to sort the
            // kitchen cabinets nearest to furthest
            Vector3 viewPosition = renderingData.cameraData.GetViewMatrix() * Vector3.zero;
            kitchen.FindAndSortCabinetsForViewPosition(viewPosition);
            kitchen.UpdateRenderingMasksForKitchenForwardRenderPass(m_RenderingLayerIndexStart);

            DrawingSettings drawingSettings =
                CreateDrawingSettings(
                    m_ShaderTagIdList,
                    ref renderingData,
                    sortingCriteria: SortingCriteria.CommonOpaque
                );

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get(); // Avoiding assigning a name here. Apparently this causes issues in some versions of URP (name: nameof(KitchenRenderPass));

            int cabinetCount = kitchen.nearToFarCabinets.Count;
            for (int cabinetIndex = 0; cabinetIndex < cabinetCount; ++cabinetIndex)
            {
                Cabinet cabinet = kitchen.nearToFarCabinets[cabinetIndex];

                uint cabinetRenderingMask = 1u << (m_RenderingLayerIndexStart + cabinetIndex);

                int stencilReference = cabinetIndex + 1;

                // cmd.SetGlobalFloat("_CutoutStencilRef", stencilReference);

                using (new ProfilingScope(cmd, m_ProfilerForStencils))
                {
                    // Stencils - These are the main cut-out windows.

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    drawingSettings.overrideMaterial = m_StencilMaterial;

                    FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                    filteringSettings.layerMask = m_CutOutStencilMask;
                    filteringSettings.renderingLayerMask = cabinetRenderingMask;

                    StencilState stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(CompareFunction.Always);
                    stencilState.SetPassOperation(StencilOp.Replace);

                    RenderStateBlock renderStateBlock = new RenderStateBlock();
                    renderStateBlock.stencilReference = stencilReference;
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilState = stencilState;

                    context.DrawRenderers(
                        renderingData.cullResults,
                        ref drawingSettings,
                        ref filteringSettings,
                        ref renderStateBlock
                    );
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                using (new ProfilingScope(cmd, m_ProfilerForNestedStencils))
                {
                    // Nested stencils - These are cut-out windows within other
                    // cut-out windows. A nested cut-out can be something like
                    // the top face of a burner's cut-out if the burner is
                    // placed in a cabinet shelf.

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    drawingSettings.overrideMaterial = m_StencilMaterial;

                    FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                    filteringSettings.layerMask = m_NestedCutOutStencilMask;
                    filteringSettings.renderingLayerMask = cabinetRenderingMask;

                    // Nested cut-outs only draw within existing stencils
                    // and they increment the contents of the stencil
                    // buffer to indicate where the outer cut-out should
                    // not render to avoid overlapping the nested cut-out.
                    StencilState stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(CompareFunction.Equal);
                    stencilState.SetPassOperation(StencilOp.IncrementWrap);

                    RenderStateBlock renderStateBlock = new RenderStateBlock();
                    renderStateBlock.stencilReference = stencilReference;
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilState = stencilState;

                    context.DrawRenderers(
                        renderingData.cullResults,
                        ref drawingSettings,
                        ref filteringSettings,
                        ref renderStateBlock
                    );
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                using (new ProfilingScope(cmd, m_ProfilerForNestedInteriors))
                {
                    // Nested interiors. These are the cut-out interiors that
                    // may be visible through a nested cut-out window. These
                    // render before the non-nested interiors to ensure the
                    // nested parts are not overdrawn by the shelf interior.

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    drawingSettings.overrideMaterial = null;

                    FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                    filteringSettings.layerMask = m_NestedCutOutInteriorMask;
                    filteringSettings.renderingLayerMask = cabinetRenderingMask;

                    StencilState stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(CompareFunction.GreaterEqual);  // was GreaterEqual

                    RenderStateBlock renderStateBlock = new RenderStateBlock();
                    renderStateBlock.stencilReference = stencilReference + 1;   // was just `stencilReference`
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilState = stencilState;

                    context.DrawRenderers(
                        renderingData.cullResults,
                        ref drawingSettings,
                        ref filteringSettings,
                        ref renderStateBlock
                    );
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Interiors
                using (new ProfilingScope(cmd, m_ProfilerForInteriors))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    drawingSettings.overrideMaterial = null;

                    FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                    filteringSettings.layerMask = m_CutOutInteriorMask;
                    filteringSettings.renderingLayerMask = cabinetRenderingMask;

                    StencilState stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(CompareFunction.Equal);

                    RenderStateBlock renderStateBlock = new RenderStateBlock();
                    renderStateBlock.stencilReference = stencilReference;
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilState = stencilState;

                    context.DrawRenderers(
                        renderingData.cullResults,
                        ref drawingSettings,
                        ref filteringSettings,
                        ref renderStateBlock
                    );
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Nested cabinets - Currently, these are just the
                // countertops within cabinet shelves. They must be drawn
                // with separate stencil test rules to avoid rendering
                // within nested stencils.
                using (new ProfilingScope(cmd, m_ProfilerForNestedCabinets))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // Copied from Cabinets.
                    drawingSettings.overrideMaterial = null;

                    FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                    filteringSettings.layerMask = m_NestedCabinetMask;
                    filteringSettings.renderingLayerMask = cabinetRenderingMask;

                    StencilState stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(CompareFunction.GreaterEqual);

                    DepthState depthState = DepthState.defaultValue;
                    depthState.writeEnabled = true;

                    RenderStateBlock renderStateBlock = new RenderStateBlock();
                    renderStateBlock.stencilReference = stencilReference;
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilState = stencilState;
                    renderStateBlock.depthState = depthState;
                    renderStateBlock.mask |= RenderStateMask.Depth;

                    context.DrawRenderers(
                        renderingData.cullResults,
                        ref drawingSettings,
                        ref filteringSettings,
                        ref renderStateBlock
                    );
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Cabinets - Draw the kitchen cabinet sections: straight,
                // corner and ends. These are rendered after everything
                // else so they can check the stencils and avoid drawing
                // over things that should count as 'windows' into the
                // cabinet.
                using (new ProfilingScope(cmd, m_ProfilerForCabinets))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    drawingSettings.overrideMaterial = null;

                    FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                    filteringSettings.layerMask = m_CabinetMask;
                    filteringSettings.renderingLayerMask = cabinetRenderingMask;

                    StencilState stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(CompareFunction.Greater); // Was CompareFunction.NotEqual

                    DepthState depthState = DepthState.defaultValue;
                    depthState.writeEnabled = true;

                    RenderStateBlock renderStateBlock = new RenderStateBlock();
                    renderStateBlock.stencilReference = stencilReference;
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilState = stencilState;
                    renderStateBlock.depthState = depthState;
                    renderStateBlock.mask |= RenderStateMask.Depth;

                    context.DrawRenderers(
                        renderingData.cullResults,
                        ref drawingSettings,
                        ref filteringSettings,
                        ref renderStateBlock
                    );
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            CommandBufferPool.Release(cmd);
        }
#endif

        public override void FrameCleanup(CommandBuffer cmd)
        {
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        // =====================================================================
        // Fields
        // =====================================================================

        public Kitchen kitchen;

        private int m_RenderingLayerIndexStart;
        private Material m_StencilMaterial;

        private LayerMask m_CutOutStencilMask;

        /// <summary>
        ///     This is the layer mask used by cut-out windows that themselves
        ///     are only visible through another cut-out window.
        /// </summary>
        private LayerMask m_NestedCutOutStencilMask;

        private LayerMask m_CutOutInteriorMask;

        /// <summary>
        ///     This is the layer mask used by cut-out interiors that may be
        ///     visible through a nested cut-out window.
        /// </summary>
        private LayerMask m_NestedCutOutInteriorMask;

        private LayerMask m_CabinetMask;

        /// <summary>
        ///     This is the layer mask used by cabinet meshes that exist within
        ///     products. Currently, this is only cabinet shelves whose
        ///     interior bottom is a countertop.
        /// </summary>
        private LayerMask m_NestedCabinetMask;

        private bool m_WillExecute;

        private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        private ProfilingSampler m_ProfilerForStencils;

        private ProfilingSampler m_ProfilerForNestedStencils;

        private ProfilingSampler m_ProfilerForNestedInteriors;

        private ProfilingSampler m_ProfilerForInteriors;

        private ProfilingSampler m_ProfilerForNestedCabinets;

        private ProfilingSampler m_ProfilerForCabinets;

    }   // class KitchenForwardRenderPass

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-07
/// ===========================================================================
