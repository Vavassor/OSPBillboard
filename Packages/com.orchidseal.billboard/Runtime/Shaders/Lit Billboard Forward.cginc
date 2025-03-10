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

struct VertexOutputBase
{
    float4 positionCs : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    float3 positionWs : TEXCOORD1;
    float3 viewDirectionWs : TEXCOORD2;
    float4 tangentToWorldAndPackedData[3] : TEXCOORD3; // [3x3:tangentToWorld]
    half4 ambientColor : TEXCOORD6;
    OSP_SHADOW_COORDS(7)
    DECLARE_LIGHT_COORDS(8)
    OSP_FOG_COORDS(9)
    UNITY_VERTEX_OUTPUT_STEREO
};

struct VertexOutputAdd
{
    float4 positionCs : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    float3 positionWs : TEXCOORD1;
    float3 viewDirectionWs : TEXCOORD2;
    float4 tangentToWorldAndLightDir[3] : TEXCOORD3; // [3x3:tangentToWorld | 1x3:lightDir]
    OSP_SHADOW_COORDS(6)
    UNITY_VERTEX_OUTPUT_STEREO
};

UNITY_DECLARE_TEX2D(_MainTex);
float4 _MainTex_ST;
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

VertexOutputBase VertexForwardBase(VertexInput v)
{
    VertexOutputBase o;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(VertexOutputBase, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float3 normalWs;
    float3 positionWs;
    #ifdef USE_NORMAL_MAP
    float3 tangentWs;
    BillboardWithNormalTangentWs(v.vertex, v.normal, v.tangent, positionWs, normalWs, tangentWs);
    #else
    BillboardWithNormalWs(v.vertex, v.normal, positionWs, normalWs);
    #endif // USE_NORMAL_MAP

    o.positionCs = UnityWorldToClipPos(float4(positionWs, 1));
    o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
    o.positionWs = positionWs;
    o.viewDirectionWs.xyz = positionWs.xyz - _WorldSpaceCameraPos;

    #ifdef USE_NORMAL_MAP
    float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWs.xyz, tangentWs, v.tangent.w);
    o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
    o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
    o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
    #else
    o.tangentToWorldAndPackedData[0].xyz = 0;
    o.tangentToWorldAndPackedData[1].xyz = 0;
    o.tangentToWorldAndPackedData[2].xyz = normalWs;
    #endif

    half3 ambientColor = 0;
    #ifdef VERTEXLIGHT_ON
    // Approximated illumination from non-important point lights
    ambientColor.rgb = Shade4PointLights(
        unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
        unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
        unity_4LightAtten0, positionWs, normalWs);
    #endif
    o.ambientColor.rgb = ShadeSHPerVertex(normalWs, ambientColor);

    COMPUTE_LIGHT_COORDS(o)
    OSP_TRANSFER_SHADOW(o, o.positionCs, o.positionWs)
    OSP_TRANSFER_FOG(o,o.pos);

    return o;
}

fixed4 FragmentForwardBase(VertexOutputBase i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float3 positionWs = i.positionWs.xyz;
    float3 viewDirectionWs = -normalize(i.viewDirectionWs.xyz);

    #if USE_NORMAL_MAP
    half3 tangent = i.tangentToWorldAndPackedData[0].xyz;
    half3 binormal = i.tangentToWorldAndPackedData[1].xyz;
    half3 normal = i.tangentToWorldAndPackedData[2].xyz;
    half3 normalTs = UnpackScaleNormal(UNITY_SAMPLE_TEX2D(_BumpMap, i.uv0), _BumpScale);
    float3 normalWs = normalize(tangent * normalTs.x + binormal * normalTs.y + normal * normalTs.z);
    #else
    float3 normalWs = normalize(i.tangentToWorldAndPackedData[2].xyz);
    #endif // USE_NORMAL_MAP

    fixed4 baseColor = UNITY_SAMPLE_TEX2D(_MainTex, i.uv0) * _Color;
    float3 albedo = baseColor.rgb;
    float smoothness = _Glossiness * baseColor.a;

    half oneMinusReflectivity;
    half3 specularColor;
    half3 diffuseColor = DiffuseAndSpecularFromMetallic(albedo, _Metallic, specularColor, oneMinusReflectivity);
    
    UnityLight mainLight;
    mainLight.color = _LightColor0.rgb;
    mainLight.dir = _WorldSpaceLightPos0.xyz;
    UNITY_LIGHT_ATTENUATION(atten, i, positionWs);
    mainLight.color *= atten;
    
    UnityIndirect indirectLight;
    indirectLight.diffuse = ShadeSHPerPixel(normalWs, i.ambientColor, positionWs);
    
    // Only uses the nearest reflection probe.
    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(smoothness, viewDirectionWs, normalWs, specularColor);
    indirectLight.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g);
    
    fixed4 color = UNITY_BRDF_PBS(diffuseColor, specularColor, oneMinusReflectivity, smoothness, normalWs, viewDirectionWs, mainLight, indirectLight);
    color.a = baseColor.a;

    #ifdef USE_EMISSION_MAP
    color.rgb += UNITY_SAMPLE_TEX2D(_EmissionMap, i.uv0).rgb * _EmissionColor.rgb;
    #endif // USE_EMISSION_MAP

    #ifdef USE_ALPHA_TEST
    clip(color.a - _AlphaCutoff);
    #endif

    OSP_APPLY_FOG(i.fogCoord, color);

    return color;
}

VertexOutputAdd VertexForwardAdd(VertexInput v)
{
    VertexOutputAdd o;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(VertexOutputAdd, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float3 normalWs;
    float3 positionWs;
    #ifdef USE_NORMAL_MAP
    float3 tangentWs;
    BillboardWithNormalTangentWs(v.vertex, v.normal, v.tangent, positionWs, normalWs, tangentWs);
    #else
    BillboardWithNormalWs(v.vertex, v.normal, positionWs, normalWs);
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

    OSP_TRANSFER_SHADOW(o, o.positionCs, o.positionWs)

    return o;
}

fixed4 FragmentForwardAdd(VertexOutputAdd i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

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

    fixed4 baseColor = UNITY_SAMPLE_TEX2D(_MainTex, i.uv0) * _Color;
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

    fixed4 color = UNITY_BRDF_PBS(diffuseColor, specularColor, oneMinusReflectivity, smoothness, normalWs, viewDirectionWs, mainLight, indirectLight);
    color.a = baseColor.a;

    #ifdef USE_ALPHA_TEST
    clip(color.a - _AlphaCutoff);
    #endif

    return color;
}

#endif // LIT_BILLBOARD_CGINC_
