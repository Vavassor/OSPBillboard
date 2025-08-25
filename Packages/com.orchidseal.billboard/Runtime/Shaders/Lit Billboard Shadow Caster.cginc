#ifndef LIT_BILLBOARD_SHADOW_CASTER_CGINC_
#define LIT_BILLBOARD_SHADOW_CASTER_CGINC_

#include "UnityCG.cginc"
#include "OSP Billboard.cginc"
#include "Billboard Common.cginc"

#if defined(CAST_TRANSPARENT_SHADOWS) && defined(UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS)
#define UNITY_STANDARD_USE_DITHER_MASK 1
#endif

UNITY_DECLARE_TEX2D(_MainTex);
float4 _MainTex_ST;
float4 _Color;

float _AlphaCutoff;

// Transformation
float2 _Position;
float _RotationRoll;
float2 _Scale;

// Flipbook
UNITY_DECLARE_TEX2DARRAY(_FlipbookTexArray);
float4 _FlipbookTexArray_ST;
float4 _FlipbookTexArray_TexelSize;
float4 _FlipbookTint;
float2 _FlipbookScrollVelocity;
float _FlipbookBlendMode;
float _FlipbookFramesPerSecond;
float _FlipbookUseManualFrame;
float _FlipbookManualFrame;

#if defined(UNITY_STANDARD_USE_DITHER_MASK)
sampler3D _DitherMaskLOD;
#endif

struct VertexInputShadowCaster
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    fixed4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutputShadowCaster
{
    V2F_SHADOW_CASTER_NOPOS
    float2 texcoord : TEXCOORD1;
    float4 flipbookTexcoord : TEXCOORD2;
    fixed alpha : TEXCOORD3;
};

// Per renderer data ..................................................................

#ifdef SPRITE_RENDERER_ON
#ifdef UNITY_INSTANCING_ENABLED
UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
    UNITY_DEFINE_INSTANCED_PROP(fixed4, unity_SpriteRendererColorArray)
    UNITY_DEFINE_INSTANCED_PROP(fixed2, unity_SpriteFlipArray)
UNITY_INSTANCING_BUFFER_END(PerDrawSprite)

#define _RendererColor UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
#define _Flip UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)
#endif // instancing

CBUFFER_START(UnityPerDrawSprite)
#ifndef UNITY_INSTANCING_ENABLED
fixed4 _RendererColor;
fixed2 _Flip;
#endif
CBUFFER_END
#endif // sprite renderer

void vertShadowCaster(VertexInputShadowCaster v, out VertexOutputShadowCaster o, out float4 opos: SV_POSITION)
{
    UNITY_SETUP_INSTANCE_ID(v);

    float4 positionOs = v.vertex;
    float2 scale = _Scale;
    #if defined(SPRITE_RENDERER_ON)
    scale *= _Flip;
    #endif
    positionOs.xy = Transform2d(positionOs.xy, _Position, _RotationRoll, scale);
    v.vertex = mul(unity_WorldToObject, float4(BillboardWs(positionOs), 1));
    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
    
    float2 transformedTexcoord = TRANSFORM_TEX(v.texcoord, _FlipbookTexArray);
    float2 scrolledTexcoord = transformedTexcoord + _Time.y * _FlipbookScrollVelocity;
    o.flipbookTexcoord = GetFlipbookTexcoord(_FlipbookTexArray, scrolledTexcoord, _FlipbookFramesPerSecond, _FlipbookUseManualFrame, _FlipbookManualFrame);

    o.alpha = _Color.a * v.color.a;
    TRANSFER_SHADOW_CASTER_NOPOS(o,opos);
}

half4 fragShadowCaster(
    VertexOutputShadowCaster i
    #if defined(UNITY_STANDARD_USE_DITHER_MASK)
        , UNITY_VPOS_TYPE vpos : VPOS
    #endif
    ): SV_Target
{
    half alpha = UNITY_SAMPLE_TEX2D(_MainTex, i.texcoord).a;
    alpha *= i.alpha;

    #if defined(USE_FLIPBOOK)
    float flipbookAlpha = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, float3(i.flipbookTexcoord.xy, floor(i.flipbookTexcoord.z))).a;
    alpha = BlendAlpha(_FlipbookTint.a * flipbookAlpha, alpha, _FlipbookBlendMode);
    #endif
    
    #ifdef USE_ALPHA_TEST
        clip(alpha - _AlphaCutoff);
    #elif defined(CAST_TRANSPARENT_SHADOWS)   
        #ifdef UNITY_STANDARD_USE_DITHER_MASK
            half alphaRef = tex3D(_DitherMaskLOD, float3(vpos.xy*0.25,alpha*0.9375)).a;
            clip (alphaRef - 0.01);
        #else
            clip(alpha - _AlphaCutoff);
        #endif
    #endif
    
    SHADOW_CASTER_FRAGMENT(i)
}

#endif // LIT_BILLBOARD_SHADOW_CASTER_CGINC_
