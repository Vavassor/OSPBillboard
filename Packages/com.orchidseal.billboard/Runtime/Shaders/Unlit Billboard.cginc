#ifndef UNLIT_BILLBOARD_CGINC_
#define UNLIT_BILLBOARD_CGINC_

#include "UnityCG.cginc"
#include "Billboard Common.cginc"
#include "OSP Billboard.cginc"

// Color Spaces.................................................................

#if defined(UNITY_COLORSPACE_GAMMA) || !defined(USE_GAMMA_COLORSPACE)
#define TO_SHADING_COLOR_SPACE(col) col
#else
#define TO_SHADING_COLOR_SPACE(col) GammaToLinearSpace(col)
#endif

#if defined(UNITY_COLORSPACE_GAMMA) || !defined(USE_GAMMA_COLORSPACE)
#define FROM_SHADING_COLOR_SPACE(col) col
#else
#define FROM_SHADING_COLOR_SPACE(col) LinearToGammaSpace(col)
#endif

// Inputs.......................................................................

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float2 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    #ifdef USE_DISTANCE_FADE
        float distanceFade :TEXCOORD2;
    #endif
    OSP_FOG_COORDS(4)
    float4 pos : SV_POSITION;
    fixed4 color : COLOR;
    UNITY_VERTEX_OUTPUT_STEREO
};

UNITY_DECLARE_TEX2D(_MainTex);
float4 _MainTex_ST;
float4 _MainTex_TexelSize;
float4 _Color;

// Transformation
float2 _Position;
float _RotationRoll;
float2 _Scale;
float _ConstantScale;

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

// Outline
float4 _OutlineColor;
float _OutlineWidth;

// Distance Fade
float _DistanceFadeMinAlpha;
float _DistanceFadeMaxAlpha;
float _DistanceFadeMin;
float _DistanceFadeMax;

// Alpha Test
float _AlphaCutoff;

// Vertex Animation
half _RandomizeAnimWorldPosition;

// Float
half _FloatOn;
half _FloatAmplitude;
half3 _FloatAxis;
half _FloatFrequency;
half _FloatPhase;

// Spin
// TODO: Should it spin in 3D? Or spin in screen space?
// half3 _SpinAxis;
// half _SpinPhase;
// half _SpinSpeed;

// Throb
half _ThrobOn;
half _ThrobAmplitude;
half _ThrobFrequency;
half _ThrobPhase;

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

// Transformation...............................................................

// Decompose the scale from a transformation matrix.
float3 GetScale(float4x4 m)
{
    float3 scale;

    scale.x = length(float3(m[0][0], m[1][0], m[2][0]));
    scale.y = length(float3(m[0][1], m[1][1], m[2][1]));
    scale.z = length(float3(m[0][2], m[1][2], m[2][2]));

    // It's not possible to determine which scale components were negative when decomposing. But we can at
    // least support sprites flipping horizontally by assuming it was the X-axis.
    float det = determinant(m);
    if (det < 0)
    {
        scale.x = -scale.x;
    }

    return scale;
}

// Outline.............................................................................

float SampleAlpha(float2 uv0, float4 uv1, float tintAlpha)
{
    float alpha = tintAlpha * UNITY_SAMPLE_TEX2D(_MainTex, uv0).a;

    #if defined(USE_FLIPBOOK)
        float flipbookAlpha = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, float3(uv1.xy, floor(uv1.z))).a;

        #if defined(USE_FLIPBOOK_SMOOTHING)
            float flipbookAlpha2 = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, uv1.xyw).a;
            flipbookAlpha = lerp(flipbookAlpha, flipbookAlpha2, frac(uv1.z));
        #endif
    
        alpha = BlendAlpha(_FlipbookTint.a * flipbookAlpha, alpha, _FlipbookBlendMode);
    #endif

    return alpha;
}

float GetOutline(float2 uv0, float4 uv1, float width, float baseAlpha, float tintAlpha)
{
    float2 t0 = _MainTex_TexelSize.xy * width;
    float2 t1 = _FlipbookTexArray_TexelSize.xy * width;
    float4 offsets0 = float4(t0.x, t0.y, -t0.x, -t0.y);
    float4 offsets1 = float4(t1.x, t1.y, -t1.x, -t1.y);
    float a0 = SampleAlpha(uv0 + offsets0.xy, float4(uv1.xy + offsets1.xy, uv1.zw), tintAlpha);
    float a1 = SampleAlpha(uv0 + offsets0.xw, float4(uv1.xy + offsets1.xw, uv1.zw), tintAlpha);
    float a2 = SampleAlpha(uv0 + offsets0.zw, float4(uv1.xy + offsets1.zw, uv1.zw), tintAlpha);
    float a3 = SampleAlpha(uv0 + offsets0.zy, float4(uv1.xy + offsets1.zy, uv1.zw), tintAlpha);
    return saturate(a0 + a1 + a2 + a3) - baseAlpha;
}

// Shader Functions....................................................................

v2f vert(appdata v)
{
    v2f o;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float3 positionOs = v.vertex.xyz;

    #if KEEP_CONSTANT_SCALING || USE_DISTANCE_FADE || FLIP_FACING_HORIZONTAL
        float3 viewDirectionWs = unity_ObjectToWorld._m03_m13_m23 - GetCenterCameraPosition();
        #if KEEP_CONSTANT_SCALING || USE_DISTANCE_FADE
            float distanceWs = length(viewDirectionWs);
        #endif
    #endif
    
    #if KEEP_CONSTANT_SCALING
        positionOs *= _ConstantScale / unity_CameraProjection._m11 * distanceWs;
        #if defined(USING_STEREO_MATRICES)
            positionOs *= 0.25; // Arbitrary scale factor. Just felt too large in VR? -Vavassor
        #endif
    #endif
    
    #ifdef USE_NON_UNIFORM_SCALE
        positionOs *= GetScale(unity_ObjectToWorld);
    #else
        positionOs *= length(mul(unity_ObjectToWorld, float4(0, 0, 1, 0)).xyz);
    #endif

    float2 scale = _Scale;

    #if defined(SPRITE_RENDERER_ON)
    scale *= _Flip;
    #endif

    #ifdef FLIP_FACING_HORIZONTAL
    scale = FlipFacingHorizontal(scale, viewDirectionWs);
    #endif

    #if USE_OUTLINE
        #if defined(USE_FLIPBOOK)
            float2 outlineTexelSize = min(_MainTex_TexelSize.xy, _FlipbookTexArray_TexelSize.xy);
        #else
            float2 outlineTexelSize = _MainTex_TexelSize.xy;
        #endif
        float2 paddingTexels = _OutlineWidth * outlineTexelSize;
        scale += 2.0 * paddingTexels;
        v.uv = (v.uv + sign(v.uv - float2(0.5, 0.5)) * paddingTexels);
    #endif

    positionOs.xy = Transform2d(positionOs.xy, _Position, _RotationRoll, scale);

    #ifdef USE_VERTEX_ANIMATION
    half randomizePhase = 0;
    
    if (_RandomizeAnimWorldPosition)
    {
        float3 objectCenterWs = unity_ObjectToWorld._m03_m13_m23;
        randomizePhase = UNITY_TWO_PI * frac(127 * (objectCenterWs.x + objectCenterWs.y + objectCenterWs.z));
    }
    
    if (_ThrobOn)
    {
        positionOs.xyz += _ThrobAmplitude * sin(_ThrobFrequency * _Time.y + _ThrobPhase + randomizePhase) * positionOs.xyz;
    }
    
    if (_FloatOn)
    {
        positionOs.xyz += _FloatAmplitude * sin(_FloatFrequency * _Time.y + _FloatPhase + randomizePhase) * _FloatAxis;
    }
    #endif

    o.pos = BillboardCs(float4(positionOs, 1));
    o.uv0 = TRANSFORM_TEX(v.uv, _MainTex);

    #ifdef USE_FLIPBOOK
        float2 transformedTexcoord = TRANSFORM_TEX(v.uv, _FlipbookTexArray);
        float2 scrolledTexcoord = transformedTexcoord + _Time.y * _FlipbookScrollVelocity;
        o.uv1 = GetFlipbookTexcoord(_FlipbookTexArray, scrolledTexcoord, _FlipbookFramesPerSecond, _FlipbookUseManualFrame, _FlipbookManualFrame);
    #endif

    #ifdef USE_DISTANCE_FADE
    o.distanceFade = lerp(_DistanceFadeMinAlpha, _DistanceFadeMaxAlpha, smoothstep(_DistanceFadeMin, _DistanceFadeMax, distanceWs));
    #endif

    fixed4 color = _Color * v.color;
    #if defined(SPRITE_RENDERER_ON)
    color *= fixed4(GammaToLinearSpace(_RendererColor.rgb), _RendererColor.a);
    #endif
    o.color = color;

    OSP_TRANSFER_FOG(o,o.pos);
    
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    #if defined(USE_PIXEL_SHARPEN)
        i.uv0.xy = SharpenPixelUv(i.uv0.xy, _MainTex_TexelSize);
        #ifdef USE_FLIPBOOK
            i.uv1.xy = SharpenPixelUv(i.uv1.xy, _FlipbookTexArray_TexelSize);
        #endif
    #endif
    
    fixed4 col = UNITY_SAMPLE_TEX2D(_MainTex, i.uv0);
    col.rgb = TO_SHADING_COLOR_SPACE(col.rgb);

    #if defined(_COLORMODE_MULTIPLY)
        col *= i.color;
    #elif defined(_COLORMODE_OVERLAY)
        col.rgb = lerp(1 - 2 * (1 - col.rgb) * (1 - i.color.rgb), 2 * col.rgb * i.color.rgb, step(col.rgb, 0.5));
        col.a *= i.color.a;
    #endif

    #if defined(USE_FLIPBOOK)
        float4 flipbookColor = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, float3(i.uv1.xy, floor(i.uv1.z)));

        #if defined(USE_FLIPBOOK_SMOOTHING)
            float4 flipbookColor2 = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, i.uv1.xyw);
            flipbookColor = lerp(flipbookColor, flipbookColor2, frac(i.uv1.z));
        #endif

        flipbookColor.rgb = TO_SHADING_COLOR_SPACE(flipbookColor.rgb);
        col = BlendColor(_FlipbookTint * flipbookColor, col, _FlipbookBlendMode);
    #endif

    #ifdef USE_OUTLINE
    col = lerp(col, _OutlineColor, GetOutline(i.uv0, i.uv1, _OutlineWidth, col.a, i.color.a));
    #endif

    #ifdef USE_DISTANCE_FADE
        col.a *= i.distanceFade;
    #endif

    #ifdef USE_ALPHA_TEST
        clip(col.a - _AlphaCutoff);
    #endif

    col.rgb = FROM_SHADING_COLOR_SPACE(col.rgb);

    OSP_APPLY_FOG(i.fogCoord, col);
    return col;
}

#endif // UNLIT_BILLBOARD_CGINC_
