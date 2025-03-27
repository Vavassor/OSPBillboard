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

// Pixel Sharpen...............................................................

// https://github.com/cnlohr/shadertrixx/blob/main/README.md#lyuma-beautiful-retro-pixels-technique
float2 SharpenPixelUv(float2 uv, float4 texelSize)
{
    float2 coord = uv.xy * texelSize.zw;
    float2 fr = frac(coord + 0.5);
    float2 fw = max(abs(ddx(coord)), abs(ddy(coord)));
    return uv.xy + (saturate((fr-(1-fw)*0.5)/fw) - fr) * texelSize.xy;
}

// Transformation..............................................................

float2 Rotate2D(float2 v, float angle)
{
    float s, c;
    sincos(angle, s, c);
    return float2(c * v.x - s * v.y, s * v.x + c * v.y);
}

float2 Transform2d(float2 position, float2 translation, float rotationDegrees, float2 scale)
{
    return scale * Rotate2D(position.xy, radians(rotationDegrees)) + translation;
}

#endif // BILLBOARD_COMMON_CGINC_
