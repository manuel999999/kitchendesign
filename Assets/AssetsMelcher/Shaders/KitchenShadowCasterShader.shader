Shader "Scopic/KitchenShadowCaster"
{
    // This shadow caster material needs to be flexible to handle drawing
    // different stencil values and using different comparison modes, zwrite
    // settings, etc.
    //
    // It is used during the Kitchen Shadow Render pass which ensures the
    // shadows cast by the kitchen respect the cutout and hollow geometry.

    Properties
    {
        // [IntRange] _KitchenShadowStencilRef("Stencil Reference Value", Range(0,255)) = 0
        // [Enum(UnityEngine.Rendering.CompareFunction)] _KitchenShadowStencilCompare("Stencil Compare", Float) = 4 // "LessEqual"
        // [Enum(UnityEngine.Rendering.StencilOp)] _KitchenShadowStencilPassOp("Pass Op", Float) = 0 // "Keep"

        // [Enum(Off,0,On,1)] _KitchenShadowZWrite("ZWrite", Float) = 1.0
        // [Enum(UnityEngine.Rendering.CompareFunction)] _KitchenShadowZTest("ZTest", Float) = 4 // "LessEqual"
        // [Enum(UnityEngine.Rendering.CullMode)] _KitchenShadowCulling ("Culling", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            Stencil
            {
                Ref [_KitchenShadowStencilRef]
                Comp [_KitchenShadowStencilCompare]
                Pass [_KitchenShadowStencilPassOp]
            }

            ZTest LEqual //[_KitchenSHadowZTest]
            ZWrite [_KitchenShadowZWrite]
            Cull [_KitchenShadowCulling]
        }
    }
}
