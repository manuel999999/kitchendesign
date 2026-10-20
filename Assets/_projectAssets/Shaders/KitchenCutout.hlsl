#ifndef KITCHEN_CUTOUT_INCLUDED
#define KITCHEN_CUTOUT_INCLUDED

#define KITCHEN_CUTOUT_MAX_SLOTS 32
#define KITCHEN_CUTOUT_MAX_BOXES 32

float4 _KitchenCutoutSlotRanges[KITCHEN_CUTOUT_MAX_SLOTS];
float4 _KitchenCutoutBoxRows[KITCHEN_CUTOUT_MAX_BOXES * 3];

float KitchenCutoutAlphaAt(float3 positionWS, uint slot)
{
    if (slot == 0u || slot >= (uint)KITCHEN_CUTOUT_MAX_SLOTS)
    {
        return 1.0;
    }

    float2 range = _KitchenCutoutSlotRanges[slot].xy;
    int start = (int)range.x;
    int count = (int)range.y;
    float4 p = float4(positionWS, 1.0);

    [loop]
    for (int i = 0; i < count; i++)
    {
        int row = (start + i) * 3;
        float3 q = float3(
            dot(_KitchenCutoutBoxRows[row], p),
            dot(_KitchenCutoutBoxRows[row + 1], p),
            dot(_KitchenCutoutBoxRows[row + 2], p));

        if (all(abs(q) <= 0.5))
        {
            return 0.0;
        }
    }

    return 1.0;
}

void KitchenCutoutAlpha_float(float3 PositionWS, out float Alpha)
{
#if defined(SHADERGRAPH_PREVIEW)
    Alpha = 1.0;
#else
    Alpha = KitchenCutoutAlphaAt(PositionWS, unity_RendererUserValue & 0xFFu);
#endif
}

void KitchenCutoutAlpha_half(half3 PositionWS, out half Alpha)
{
    float alpha;
    KitchenCutoutAlpha_float(PositionWS, alpha);
    Alpha = alpha;
}

#endif
