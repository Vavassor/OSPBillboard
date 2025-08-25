#ifndef LIT_BILLBOARD_FORWARD_CGINC_
#define LIT_BILLBOARD_FORWARD_CGINC_

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

#include "OSP Billboard.cginc"
#include "Billboard Common.cginc"

// Shadow Receiving................................................................................

// Macros from AutoLight.cginc assume variables have specific names which differ from ours.
// So redefine our own here.

#if defined(SHADOWS_SCREEN)
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
#define OSP_TRANSFER_SHADOW(a, positionCs, positionWs) a._ShadowCoord = mul(unity_WorldToShadow[0], positionWs);
#else // UNITY_NO_SCREENSPACE_SHADOWS
#define OSP_TRANSFER_SHADOW(a, positionCs, positionWs) a._ShadowCoord = ComputeScreenPos(positionCs);
#endif
#define OSP_SHADOW_COORDS(idx1) unityShadowCoord4 _ShadowCoord : TEXCOORD##idx1;
#endif // SHADOWS_SCREEN

#if defined (SHADOWS_DEPTH) && defined (SPOT)
#define OSP_SHADOW_COORDS(idx1) unityShadowCoord4 _ShadowCoord : TEXCOORD##idx1;
#define OSP_TRANSFER_SHADOW(a, positionCs, positionWs) a._ShadowCoord = mul(unity_WorldToShadow[0], positionWs);
#endif

#if defined (SHADOWS_CUBE)
#define OSP_SHADOW_COORDS(idx1) unityShadowCoord3 _ShadowCoord : TEXCOORD##idx1;
#define OSP_TRANSFER_SHADOW(a, positionCs, positionWs) a._ShadowCoord.xyz = positionWs.xyz - _LightPositionRange.xyz;
#endif

#if !defined (SHADOWS_SCREEN) && !defined (SHADOWS_DEPTH) && !defined (SHADOWS_CUBE)
#define OSP_SHADOW_COORDS(idx1)
#define OSP_TRANSFER_SHADOW(a, positionCs, positionWs)
#endif

// Inputs..........................................................................................

UNITY_DECLARE_TEX2D(_MainTex);
float4 _MainTex_ST;
float4 _MainTex_TexelSize;
fixed4 _Color;

half _Glossiness;
half _Metallic;

UNITY_DECLARE_TEX2D(_BumpMap);
float4 _BumpMap_ST;
half _BumpScale;

UNITY_DECLARE_TEX2D(_EmissionMap);
half3 _EmissionColor;

// Alpha Test
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

// Flipbook........................................................................................

float4 TransferFlipbook(float2 inputTexcoord)
{
    float2 transformedTexcoord = TRANSFORM_TEX(inputTexcoord, _FlipbookTexArray);
    float2 scrolledTexcoord = transformedTexcoord + _Time.y * _FlipbookScrollVelocity;
    return GetFlipbookTexcoord(_FlipbookTexArray, scrolledTexcoord, _FlipbookFramesPerSecond, _FlipbookUseManualFrame, _FlipbookManualFrame);    
}

float4 BlendFlipbook(float4 color, float4 uv1)
{
    float4 flipbookColor = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, float3(uv1.xy, floor(uv1.z)));

    #if defined(USE_FLIPBOOK_SMOOTHING)
    float4 flipbookColor2 = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, uv1.xyw);
    flipbookColor = lerp(flipbookColor, flipbookColor2, frac(uv1.z));
    #endif
    
    return BlendColor(_FlipbookTint * flipbookColor, color, _FlipbookBlendMode);
}

// Forward Base Pass...............................................................................

struct VertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    fixed4 color : COLOR;
    float2 uv0 : TEXCOORD0;
    #if defined(USE_NORMAL_MAP)
    float4 tangent : TANGENT;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCs : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    float3 positionWs : TEXCOORD2;
    float3 viewDirectionWs : TEXCOORD3;
    float4 tangentToWorldAndLightDir[3] : TEXCOORD4; // [3x3:tangentToWorld | 1x3:lightDir]
    half4 ambientColor : TEXCOORD7;
    OSP_SHADOW_COORDS(8)
    DECLARE_LIGHT_COORDS(9)
    OSP_FOG_COORDS(10)
    fixed4 color : COLOR;
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput VertexForward(VertexInput v)
{
    VertexOutput o;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(VertexOutput, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float2 scale = _Scale;
    #ifdef FLIP_FACING_HORIZONTAL
    float3 viewDirectionWs = unity_ObjectToWorld._m03_m13_m23 - GetCenterCameraPosition();
    scale = FlipFacingHorizontal(scale, viewDirectionWs);
    #endif

    #if defined(SPRITE_RENDERER_ON)
    scale *= _Flip;
    #endif

    float4 positionOs = v.vertex;
    positionOs.xy = Transform2d(positionOs.xy, _Position, _RotationRoll, scale);

    float3 normalWs;
    float3 positionWs;
    #ifdef USE_NORMAL_MAP
    float3 tangentWs;
    BillboardWithNormalTangentWs(positionOs, v.normal, v.tangent, positionWs, normalWs, tangentWs);
    #else
    BillboardWithNormalWs(positionOs, v.normal, positionWs, normalWs);
    #endif // USE_NORMAL_MAP

    o.positionCs = UnityWorldToClipPos(positionWs);
    o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
    o.positionWs = positionWs;
    o.viewDirectionWs.xyz = positionWs.xyz - _WorldSpaceCameraPos;

    #ifdef USE_NORMAL_MAP
    float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
    float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWs, tangentWorld.xyz, tangentWorld.w);
    o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
    o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
    o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
    #else
    o.tangentToWorldAndLightDir[0].xyz = 0;
    o.tangentToWorldAndLightDir[1].xyz = 0;
    o.tangentToWorldAndLightDir[2].xyz = normalWs;
    #endif

    float3 lightDir = _WorldSpaceLightPos0.xyz - positionWs.xyz * _WorldSpaceLightPos0.w;
    #ifndef USING_DIRECTIONAL_LIGHT
    lightDir = normalize(lightDir);
    #endif
    o.tangentToWorldAndLightDir[0].w = lightDir.x;
    o.tangentToWorldAndLightDir[1].w = lightDir.y;
    o.tangentToWorldAndLightDir[2].w = lightDir.z;
    
    #ifdef UNITY_PASS_FORWARDBASE
    half3 ambientColor = 0;
        #ifdef VERTEXLIGHT_ON
        // Approximated illumination from non-important point lights
        ambientColor.rgb = Shade4PointLights(
            unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
            unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
            unity_4LightAtten0, positionWs, normalWs);
        #endif
    o.ambientColor.rgb = ShadeSHPerVertex(normalWs, ambientColor);
    #endif
    
    #if defined(USE_FLIPBOOK)
    o.uv1 = TransferFlipbook(v.uv0);
    #endif

    fixed4 color = _Color * v.color;
    #if defined(SPRITE_RENDERER_ON)
    color *= fixed4(GammaToLinearSpace(_RendererColor.rgb), _RendererColor.a);
    #endif
    o.color = color;

    #ifdef UNITY_PASS_FORWARDBASE
    COMPUTE_LIGHT_COORDS(o)
    OSP_TRANSFER_FOG(o,o.pos);
    #endif
    
    OSP_TRANSFER_SHADOW(o, o.positionCs, o.positionWs)
    
    return o;
}

fixed4 FragmentForward(VertexOutput i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    #if defined(USE_PIXEL_SHARPEN)
    i.uv0.xy = SharpenPixelUv(i.uv0.xy, _MainTex_TexelSize);
    #endif // USE_PIXEL_SHARPEN

    float3 positionWs = i.positionWs.xyz;
    float3 viewDirectionWs = -normalize(i.viewDirectionWs);

    #if USE_NORMAL_MAP
    half3 tangent = i.tangentToWorldAndLightDir[0].xyz;
    half3 binormal = i.tangentToWorldAndLightDir[1].xyz;
    half3 normal = i.tangentToWorldAndLightDir[2].xyz;
    half3 normalTs = UnpackScaleNormal(UNITY_SAMPLE_TEX2D(_BumpMap, i.uv0), _BumpScale);
    float3 normalWs = normalize(tangent * normalTs.x + binormal * normalTs.y + normal * normalTs.z);
    #else
    float3 normalWs = normalize(i.tangentToWorldAndLightDir[2].xyz);
    #endif // USE_NORMAL_MAP

    fixed4 baseColor = UNITY_SAMPLE_TEX2D(_MainTex, i.uv0) * i.color;
    #ifdef USE_FLIPBOOK
    baseColor = BlendFlipbook(baseColor, i.uv1);
    #endif
    
    float3 albedo = baseColor.rgb;
    float smoothness = _Glossiness * baseColor.a;

    half oneMinusReflectivity;
    half3 specularColor;
    half3 diffuseColor = DiffuseAndSpecularFromMetallic(albedo, _Metallic, specularColor, oneMinusReflectivity);
    
    UnityLight mainLight;
    mainLight.color = _LightColor0.rgb;
    mainLight.dir = half3(i.tangentToWorldAndLightDir[0].w, i.tangentToWorldAndLightDir[1].w, i.tangentToWorldAndLightDir[2].w);
    #ifndef USING_DIRECTIONAL_LIGHT
    mainLight.dir = normalize(mainLight.dir);
    #endif
    UNITY_LIGHT_ATTENUATION(atten, i, positionWs);
    mainLight.color *= atten;

    UnityIndirect indirectLight;
    indirectLight.diffuse = 0;
    indirectLight.specular = 0;

    #ifdef UNITY_PASS_FORWARDBASE
    indirectLight.diffuse = ShadeSHPerPixel(normalWs, i.ambientColor, positionWs);
    
    // Only uses the nearest reflection probe.
    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(smoothness, viewDirectionWs, normalWs, specularColor);
    indirectLight.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g);
    #endif

    fixed4 color = UNITY_BRDF_PBS(diffuseColor, specularColor, oneMinusReflectivity, smoothness, normalWs, viewDirectionWs, mainLight, indirectLight);
    color.a = baseColor.a;
    
    #if defined(UNITY_PASS_FORWARDBASE) && defined(USE_EMISSION_MAP)
    color.rgb += UNITY_SAMPLE_TEX2D(_EmissionMap, i.uv0).rgb * _EmissionColor.rgb;
    #endif // USE_EMISSION_MAP

    #ifdef USE_ALPHA_TEST
    clip(color.a - _AlphaCutoff);
    #endif

    #if defined(UNITY_PASS_FORWARDBASE)
    OSP_APPLY_FOG(i.fogCoord, color);
    #endif

    return color;
}

#endif // LIT_BILLBOARD_CGINC_
