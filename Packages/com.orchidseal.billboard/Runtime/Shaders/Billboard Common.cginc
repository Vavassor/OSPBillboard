#ifndef BILLBOARD_COMMON_CGINC_
#define BILLBOARD_COMMON_CGINC_

// Flipbooks....................................................................

// The index in the z component of the result is non-integral so that the
// fractional part can be used for smoothing, if that's enabled.
float4 GetFlipbookTexcoord(Texture2DArray flipbook, float2 texcoord, float framesPerSecond, float useManualFrame, int manualFrame)
{
    float width, height;
    uint elementCount;
    flipbook.GetDimensions(width, height, elementCount);
    float frame = _Time.y * framesPerSecond / elementCount;
    uint index = frac(frame) * elementCount;
    index = lerp(index, manualFrame, useManualFrame);
    uint nextIndex = (index + 1) % elementCount;
    float4 flipbookTexcoord = float4(texcoord, index + frac(frame), nextIndex);
    return flipbookTexcoord;
}

// Fog..........................................................................

#if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
// This transfers fog using radial distance so that it works in mirrors.
fixed TransferFog(float4 clipPosition)
{
    float3 viewPosition = UnityObjectToViewPos(clipPosition);
    float fogCoord = length(viewPosition.xyz);
    UNITY_CALC_FOG_FACTOR_RAW(fogCoord);
    return saturate(unityFogFactor);
}
            
#define OSP_APPLY_FOG(col, input) col.rgb = lerp(unity_FogColor.rgb, col.rgb, input.fog);
#define OSP_FOG_COORDS(texcoordNumber) fixed fog : TEXCOORD##texcoordNumber;
#define OSP_TRANSFER_FOG(clipPosition, output) output.fog = TransferFog(clipPosition);
#else
#define OSP_APPLY_FOG(col, input)
#define OSP_FOG_COORDS(texcoordNumber)
#define OSP_TRANSFER_FOG(clipPosition, output)
#endif

#endif // BILLBOARD_COMMON_CGINC_
