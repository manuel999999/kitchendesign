namespace Melcher
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.Experimental.Rendering;

    /// <summary>
    /// Renders the kitchen's cutouts, hollows and cabinets to the shadowmap
    /// to properly cast stencil masked shadows.
    /// </summary>
    class KitchenShadowRenderPass : ScriptableRenderPass
    {
        public KitchenShadowRenderPass(Material shadowMaterial)
        {
            m_ShadowMaterial = shadowMaterial;

            const int MaxCascades = 4;
            m_CascadeSlices = new ShadowSliceData[MaxCascades];
            m_CascadeSplitDistances = new Vector4[MaxCascades];

            profilingSampler = new ProfilingSampler(nameof(KitchenShadowRenderPass));
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

            casterIdStencilRef = Shader.PropertyToID("_KitchenShadowStencilRef");
            casterIdStencilCompare = Shader.PropertyToID("_KitchenShadowStencilCompare");
            casterIdStencilPassOp = Shader.PropertyToID("_KitchenShadowStencilPassOp");
            casterIdZwrite = Shader.PropertyToID("_KitchenShadowZWrite");
            casterIdCulling = Shader.PropertyToID("_KitchenShadowCulling");
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
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

            m_MainLightIndex = renderingData.lightData.mainLightIndex;
            if (m_MainLightIndex < 0)
            {
                Debug.Log("No main light index");
                return;
            }

            VisibleLight mainVisibleLight = renderingData.lightData.visibleLights[m_MainLightIndex];
            if (mainVisibleLight == null || mainVisibleLight.light == null)
            {
                Debug.Log("No main visible light");
                return;
            }

            Light mainLight = mainVisibleLight.light;
            if (mainLight.type != LightType.Directional)
            {
                Debug.Log("Main light isn't directional");
                return;
            }
            m_MainLightShadowNearPlane = mainLight.shadowNearPlane;

            m_CascadeCount = renderingData.shadowData.mainLightShadowCascadesCount;

            m_ShadowResolution = ShadowUtils.GetMaxTileResolutionInAtlas(
                renderingData.shadowData.mainLightShadowmapWidth,
                renderingData.shadowData.mainLightShadowmapHeight,
                m_CascadeCount
            );

            m_RenderTargetWidth = renderingData.shadowData.mainLightShadowmapWidth;
            m_RenderTargetHeight = (m_CascadeCount == 2) ?
                renderingData.shadowData.mainLightShadowmapHeight >> 1 :
                renderingData.shadowData.mainLightShadowmapHeight;

            // Call our custom shadow map getter to retrieve the main light map
            RTHandle shadowMap = ShadowUtils.GetShadowMapByName("_MainLightShadowmapTexture");
            if (shadowMap == null)
            {
                return;
            }

            ConfigureTarget(colorAttachment: shadowMap, depthAttachment: shadowMap);

            for (int cascadeIndex = 0; cascadeIndex < m_CascadeCount; ++cascadeIndex)
            {
                bool success = ShadowUtils.ExtractDirectionalLightMatrix(
                    ref renderingData.cullResults,
                    ref renderingData.shadowData,
                    m_MainLightIndex,
                    cascadeIndex,
                    m_RenderTargetWidth,
                    m_RenderTargetHeight,
                    m_ShadowResolution,
                    mainLight.shadowNearPlane,
                    out m_CascadeSplitDistances[cascadeIndex],
                    out m_CascadeSlices[cascadeIndex]
                );

                if (!success)
                {
                    return;
                }
            }

            m_WillExecute = true;
        }

        // TODO: Confirm this is no longer needed
        ///// <summary>
        ///// Mostly copied from com.unity.render-pipelines.universal@12.1.6/Runtime/ShadowUtils.cs.
        ///// Modified to support stencil buffer.
        ///// </summary>
        //private RenderTexture GetTemporaryShadowTexture()
        //{
        //    bool forceShadowPointSampling = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal &&
        //        GraphicsSettings.HasShaderDefine(Graphics.activeTier, BuiltinShaderDefine.UNITY_METAL_SHADOWS_USE_POINT_FILTERING);

        //    const int ShadowmapBufferBits = 16;
        //    const int ShadowmapStencilBits = 8;
        //    GraphicsFormat format = GraphicsFormatUtility.GetDepthStencilFormat(
        //        ShadowmapBufferBits,
        //        ShadowmapStencilBits
        //    );

        //    RenderTextureDescriptor rtd = new RenderTextureDescriptor(
        //        m_RenderTargetWidth,
        //        m_RenderTargetHeight,
        //        GraphicsFormat.None,
        //        format
        //    );

        //    rtd.shadowSamplingMode = (RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
        //        && (SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2)) ?
        //        ShadowSamplingMode.CompareDepths : ShadowSamplingMode.None;

        //    RenderTexture shadowTexture = RenderTexture.GetTemporary(rtd);
        //    shadowTexture.filterMode = forceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear;
        //    shadowTexture.wrapMode = TextureWrapMode.Clamp;

        //    // pray that its the same buffer that was used in URP shadow pass...
        //    return shadowTexture;
        //}

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!m_WillExecute)
            {
                return;
            }

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get(); // Avoiding assigning a name here. Apparently this causes issues in some versions of URP (name: nameof(KitchenRenderPass));
            using (new ProfilingScope(cmd, profilingSampler))
            {
                Matrix4x4 originalViewMatrix = renderingData.cameraData.GetViewMatrix();
                Matrix4x4 originalProjectionMatrix = renderingData.cameraData.GetProjectionMatrix();

                var shadowDrawingSettings =
                    new ShadowDrawingSettings(
                        renderingData.cullResults,
                        m_MainLightIndex,
                        BatchCullingProjectionType.Orthographic
                    );

                shadowDrawingSettings.useRenderingLayerMaskTest = UniversalRenderPipeline.asset.supportsLightLayers;

                for (int cascadeIndex = 0; cascadeIndex < m_CascadeCount; ++cascadeIndex)
                {
                    #region First half of ShadowUtils.RenderShadowSlice()
                    {
                        // these values match HDRP defaults (see https://github.com/Unity-Technologies/Graphics/blob/9544b8ed2f98c62803d285096c91b44e9d8cbc47/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowAtlas.cs#L197 )
                        cmd.SetGlobalDepthBias(1.0f, 2.5f);

                        cmd.SetViewport(
                            new Rect(
                                m_CascadeSlices[cascadeIndex].offsetX,
                                m_CascadeSlices[cascadeIndex].offsetY,
                                m_CascadeSlices[cascadeIndex].resolution,
                                m_CascadeSlices[cascadeIndex].resolution
                            )
                        );

                        cmd.SetViewProjectionMatrices(
                            m_CascadeSlices[cascadeIndex].viewMatrix,
                            m_CascadeSlices[cascadeIndex].projectionMatrix
                        );

                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                    }
                    #endregion  // end of ShadowUtils.RenderShadowSlice() first half

                    // Calculate the view's world position to sort the
                    // kitchen cabinets nearest to furthest
                    Vector3 viewPosition = m_CascadeSlices[cascadeIndex].viewMatrix * Vector3.zero;
                    kitchen.FindAndSortCabinetsForViewPosition(viewPosition);

                    int cabinetCount = kitchen.nearToFarCabinets.Count;
                    for (int cabinetIndex = 0; cabinetIndex < cabinetCount; ++cabinetIndex)
                    {
                        int stencilReference = cabinetIndex + 1;
                        cmd.SetGlobalFloat(casterIdStencilRef, stencilReference);

                        Cabinet cabinet = kitchen.nearToFarCabinets[cabinetIndex];

                        // Stencils
                        cmd.SetGlobalFloat(casterIdStencilCompare, (float)CompareFunction.Always);
                        cmd.SetGlobalFloat(casterIdStencilPassOp, (float)StencilOp.Replace);
                        cmd.SetGlobalFloat(casterIdCulling, (float)CullMode.Front);
                        cmd.SetGlobalFloat(casterIdZwrite, 0.0f);

                        foreach (Occupant product in cabinet.occupants)
                        {
                            RenderCutOutStencil(cmd, product.productVisualContentInstance.countertopCutOut);
                            RenderCutOutStencil(cmd, product.productVisualContentInstance.cabinetCutOut);
                        }

                        // Interiors
                        cmd.SetGlobalFloat(casterIdStencilCompare, (float)CompareFunction.Equal);
                        cmd.SetGlobalFloat(casterIdStencilPassOp, (float)StencilOp.Keep);
                        cmd.SetGlobalFloat(casterIdCulling, (float)CullMode.Back);
                        cmd.SetGlobalFloat(casterIdZwrite, 1.0f);

                        foreach (Occupant product in cabinet.occupants)
                        {
                            RenderCutOutInterior(cmd, product.productVisualContentInstance.countertopCutOut);
                            RenderCutOutInterior(cmd, product.productVisualContentInstance.cabinetCutOut);
                        }

                        // Cabinets
                        cmd.SetGlobalFloat(casterIdStencilCompare, (float)CompareFunction.NotEqual);
                        cmd.SetGlobalFloat(casterIdStencilPassOp, (float)StencilOp.Keep);
                        cmd.SetGlobalFloat(casterIdCulling, (float)CullMode.Back);
                        cmd.SetGlobalFloat(casterIdZwrite, 1.0f);

                        foreach (MeshRenderer cab in cabinet.meshRenderers)
                        {
                            if (!cab.enabled || !cab.gameObject.activeInHierarchy)
                            {
                                continue;
                            }

                            Mesh cabMesh = cab.GetComponent<MeshFilter>().sharedMesh;
                            cmd.DrawMesh(
                                cabMesh,
                                cab.transform.localToWorldMatrix,
                                material: m_ShadowMaterial,
                                submeshIndex: 0,
                                shaderPass: 0
                            );
                        }

                    }   // for cabinetIndex

                    #region Last half of ShadowUtils.RenderShadowSlice()
                    {
                        cmd.DisableScissorRect();
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();

                        cmd.SetGlobalDepthBias(0.0f, 0.0f); // Restore previous depth bias values
                    }
                    #endregion  // end of ShadowUtils.RenderShadowSlice() last half

                }   // for cascadeIndex

                // Restore the orginal camera view and projection matrices
                cmd.SetViewProjectionMatrices(originalViewMatrix, originalProjectionMatrix);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void RenderCutOutStencil(CommandBuffer cmd, CabinetCutOut cutOut)
        {
            if (cutOut)
            {
                var stencils = cutOut.stencils;
                while (stencils.MoveNext())
                {
                    // Skip disabled objects
                    if (!stencils.Current.gameObject.activeSelf)
                    {
                        continue;
                    }

                    Mesh mesh = stencils.Current.GetComponent<MeshFilter>().sharedMesh;
                    for (int i = 0; i < mesh.subMeshCount; ++i)
                    {
                        cmd.DrawMesh(
                            mesh,
                            stencils.Current.transform.localToWorldMatrix,
                            material: m_ShadowMaterial,
                            submeshIndex: i,
                            shaderPass: 0
                        );
                    }
                }
            }
        }

        private void RenderCutOutInterior(CommandBuffer cmd, CabinetCutOut cutOut)
        {
            var interiors = cutOut.interiors;
            while (interiors.MoveNext())
            {
                // Skip disabled objects
                if (!interiors.Current.gameObject.activeSelf)
                {
                    continue;
                }

                Mesh mesh = interiors.Current.GetComponent<MeshFilter>().sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; ++i)
                {
                    cmd.DrawMesh(
                        mesh,
                        interiors.Current.transform.localToWorldMatrix,
                        material: m_ShadowMaterial,
                        submeshIndex: i,
                        shaderPass: 0
                    );
                }
            }
        }
#endif

        /// Cleanup any allocated resources that were created during the execution of this render pass.
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

        private Material m_ShadowMaterial;

        private Vector4[] m_CascadeSplitDistances;
        private ShadowSliceData[] m_CascadeSlices;
        private int m_MainLightIndex;
        private float m_MainLightShadowNearPlane;
        private int m_CascadeCount;
        private int m_ShadowResolution;
        private int m_RenderTargetWidth;
        private int m_RenderTargetHeight;

        private bool m_WillExecute;

        private int casterIdStencilRef;
        private int casterIdStencilCompare;
        private int casterIdStencilPassOp;
        private int casterIdZwrite;
        private int casterIdCulling;

    }   // class KitchenShadowRenderPass

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-07
/// ===========================================================================
