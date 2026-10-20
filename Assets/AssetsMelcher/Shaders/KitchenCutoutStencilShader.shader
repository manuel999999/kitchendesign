Shader "Scopic/KitchenCutoutStencil"
{
    Properties
    {
        [IntRange] _CutoutStencilRef ("Stencil Reference Value", Range(0,255)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            ZTest Less

            // Don't write to Z-Buffer. This allows the skybox to render after us
            ZWrite Off

            // Don't write any color information
            ColorMask 0

            // Invert the mesh. We are reusing the hollow's mesh but want it
            // inverted
            Cull Front

            Stencil
            {
                Ref [_CutoutStencilRef]
                Comp Always
                Pass Replace
            }
        }
    }
}
