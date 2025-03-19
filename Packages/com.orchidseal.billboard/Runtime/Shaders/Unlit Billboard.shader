// Billboards are a flat image or sprite that always face the camera.
// 
// Put the material on Unity's Quad mesh. And enable GPU instancing on the material, because this shader
// disables batching! Billboarding uses the object pivot, but batching combines meshes so they all have one pivot.
// 
// This shader is for Unity's built in render pipeline.
Shader "Orchid Seal/OSP Billboard/Unlit Billboard"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        [KeywordEnum(Multiply, Overlay)] _ColorMode("Tint Color Mode", Float) = 0
        [HideInInspector] [Enum(Opaque,0,Cutout,1,Transparent,2,Premultiply,3,Additive,4,Custom,5)] _RenderMode("Render Mode", Int) = 2
        [Toggle(USE_GAMMA_COLORSPACE)] _UseGammaSpace("Use Gamma Space Blending", Float) = 0
        [Toggle(USE_PIXEL_SHARPEN)] _UsePixelSharpen("Sharp Pixels", Float) = 0
        
        [KeywordEnum(None, Spherical, Cylindrical_World, Cylindrical_Local)] _Billboard_Mode("Billboard Mode", Float) = 1
        _Position("Position (XY)", Vector) = (0, 0, 0, 0)
        _RotationRoll("Rotation", Float) = 0
        _Scale("Scale (XY)", Vector) = (1, 1, 0, 0)
        [Toggle(USE_NON_UNIFORM_SCALE)] _UseNonUniformScale("Use Non-Uniform Object Scale", Float) = 1
        [Toggle(KEEP_CONSTANT_SCALING)] _KeepConstantScaling("Keep Constant Scaling", Int) = 0
        _ConstantScale("Constant Scale", Float) = 1
        [Toggle(FLIP_FACING_HORIZONTAL)] _FlipFacingHorizontal("Flip Facing Horizontal", Float) = 0
        
        _FlipbookTexArray("Texture Array", 2DArray) = "" {}
        _FlipbookTint("Tint", Color) = (1,1,1,1)
        _FlipbookScrollVelocity("Scroll Velocity (XY)", Vector) = (0, 0, 0, 0)
        [Enum(Replace,0,Transparent,1,Add,2,Multiply,3)] _FlipbookBlendMode("Blend Mode", Float) = 0
        _FlipbookFramesPerSecond("Frames Per Second", Float) = 30
        [Toggle(USE_FLIPBOOK_SMOOTHING)] _UseFlipbookSmoothing("Smoothing", Float) = 0
        [Toggle] _FlipbookUseManualFrame("Control Frame Manually", Float) = 0
        _FlipbookManualFrame("Manual Frame", Float) = 0
        
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width", Float) = 1
        
        _DistanceFadeMinAlpha("Min Alpha", Range(0, 1)) = 1
        _DistanceFadeMaxAlpha("Max Alpha", Range(0, 1)) = 0
        _DistanceFadeMin("Min Distance", Float) = 0
        _DistanceFadeMax("Max Distance", Float) = 10
        
        [Toggle] _RandomizeAnimWorldPosition("Randomize Animation by World Position", Float) = 0
        
        [Toggle] _FloatOn("Float Enabled", Float) = 0
        _FloatAmplitude("Float Amplitude", Float) = 0.15
        _FloatAxis("Float Axis", Vector) = (0, 1, 0, 0)
        _FloatFrequency("Float Frequency", Float) = 3
        _FloatPhase("Float Phase", Float) = 0
        
        // [Toggle(SPIN_ON)] _SpinOn("Spin Enabled", Float) = 0
        // _SpinAxis("Spin Axis", Vector) = (0, 1, 0, 0)
        // _SpinPhase("Spin Phase", Float) = 0
        // _SpinSpeed("Spin Speed", Float) = 1
        
        [Toggle] _ThrobOn("Throb Enabled", Float) = 0
        _ThrobAmplitude("Throb Amplitude", Float) = 0.15
        _ThrobFrequency("Throb Frequency", Float) = 5
        _ThrobPhase("Throb Phase", Float) = 0
        
        [Toggle(USE_ALPHA_TEST)] _UseAlphaTest("Enable Alpha Test", Float) = 0
        _AlphaCutoff("Alpha Cutoff", Float) = 0.5
        [Toggle] _AlphaToMask("Alpha To Mask", Float) = 0
        
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc("Source Blend", Float) = 5 //"SrcAlpha"
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst("Destination Blend", Float) = 10 //"OneMinusSrcAlpha"
        [Enum(Add,0,Sub,1,RevSub,2,Min,3,Max,4)] _BlendOp("Blend Operation", Float) = 0 // "Add"
        
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0.0 //"Off"
        
        _StencilRef("Reference", Int) = 0
        _StencilReadMask("Read Mask", Int) = 255
        _StencilWriteMask("Write Mask", Int) = 255
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Comparison", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass("Pass", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail("Fail", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail("ZFail", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
            "PreviewType" = "Plane"
        }

        Blend [_BlendSrc] [_BlendDst]
        BlendOp [_BlendOp]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        Cull Off
        AlphaToMask [_AlphaToMask]
        Stencil
        {
            Ref [_StencilRef]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask] 
            Comp [_StencilComp]
            Pass [_StencilPass]
            Fail [_StencilFail]
            ZFail [_StencilZFail]
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ _COLORMODE_MULTIPLY _COLORMODE_OVERLAY
            #pragma shader_feature_local USE_GAMMA_COLORSPACE
            #pragma shader_feature_local USE_FLIPBOOK
            #pragma shader_feature_local USE_FLIPBOOK_SMOOTHING
            #pragma shader_feature_local USE_ALPHA_TEST
            #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_SPHERICAL _BILLBOARD_MODE_CYLINDRICAL_WORLD _BILLBOARD_MODE_CYLINDRICAL_LOCAL
            #pragma shader_feature_local USE_NON_UNIFORM_SCALE
            #pragma shader_feature_local KEEP_CONSTANT_SCALING
            #pragma shader_feature_local USE_OUTLINE
            #pragma shader_feature_local USE_DISTANCE_FADE
            #pragma shader_feature_local USE_PIXEL_SHARPEN
            #pragma shader_feature_local FLIP_FACING_HORIZONTAL
            #pragma shader_feature_local USE_VERTEX_ANIMATION

            #include "UnityCG.cginc"
            #include "Billboard Common.cginc"
            #include "OSP Billboard.cginc"

            // Blending.....................................................................

            #define BLEND_MODE_LERP 0
            #define BLEND_MODE_TRANSPARENT 1
            #define BLEND_MODE_ADD 2
            #define BLEND_MODE_MULTIPLY 3

            float4 BlendColor(float4 s, float4 d, float blendMode)
            {
                switch (blendMode)
                {
                case BLEND_MODE_LERP: return s;
                case BLEND_MODE_TRANSPARENT:
                {
                    float resultAlpha = s.a + d.a * (1.0 - s.a);
                    float3 resultRgb = (s.a * s.rgb + (1.0 - s.a) * d.a * d.rgb) / resultAlpha;
                    return float4(resultRgb, resultAlpha);
                }
                case BLEND_MODE_ADD: return s + d;
                case BLEND_MODE_MULTIPLY: return s * d;
                default: return 0;
                }
            }

            float BlendAlpha(float s, float d, float blendMode)
            {
                switch (blendMode)
                {
                case BLEND_MODE_LERP: return s;
                case BLEND_MODE_TRANSPARENT: return s + d * (1.0 - s);
                case BLEND_MODE_ADD: return s + d;
                case BLEND_MODE_MULTIPLY: return s * d;
                default: return 0;
                }
            }

            #define BLEND_BY_STRENGTH 0
            #define BLEND_BY_VERTEX_COLOR_RED 1
            #define BLEND_BY_VERTEX_COLOR_ALPHA 2

            float GetBlendAmount(float strength, float4 vertexColor, float blendBy)
            {
                switch (blendBy)
                {
                case BLEND_BY_STRENGTH: return strength;
                case BLEND_BY_VERTEX_COLOR_RED: return vertexColor.r;
                case BLEND_BY_VERTEX_COLOR_ALPHA: return vertexColor.a;
                default: return 0;
                }
            }

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
            
            float SampleAlpha(float2 uv0, float4 uv1)
            {
                float alpha = _Color.a * UNITY_SAMPLE_TEX2D(_MainTex, uv0).a;

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

            float GetOutline(float2 uv0, float4 uv1, float width, float baseAlpha)
            {
                float2 t0 = _MainTex_TexelSize.xy * width;
                float2 t1 = _FlipbookTexArray_TexelSize.xy * width;
                float4 offsets0 = float4(t0.x, t0.y, -t0.x, -t0.y);
                float4 offsets1 = float4(t1.x, t1.y, -t1.x, -t1.y);
                float a0 = SampleAlpha(uv0 + offsets0.xy, float4(uv1.xy + offsets1.xy, uv1.zw));
                float a1 = SampleAlpha(uv0 + offsets0.xw, float4(uv1.xy + offsets1.xw, uv1.zw));
                float a2 = SampleAlpha(uv0 + offsets0.zw, float4(uv1.xy + offsets1.zw, uv1.zw));
                float a3 = SampleAlpha(uv0 + offsets0.zy, float4(uv1.xy + offsets1.zy, uv1.zw));
                return saturate(a0 + a1 + a2 + a3) - baseAlpha;
            }

            // Shader Functions....................................................................

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionOs = v.vertex.xyz;

                #if KEEP_CONSTANT_SCALING || USE_DISTANCE_FADE || FLIP_FACING_HORIZONTAL
                    float3 viewDirectionWs = unity_ObjectToWorld._m03_m13_m23 - GetCenterCameraPosition();
                    #if KEEP_CONSTANT_SCALING || USE_DISTANCE_FADE
                        float distanceWs = length(viewDirectionWs);
                    #endif
                #endif
                
                #if KEEP_CONSTANT_SCALING
                    positionOs *= _ConstantScale * distanceWs;
                #endif
                
                #ifdef USE_NON_UNIFORM_SCALE
                    positionOs *= GetScale(unity_ObjectToWorld);
                #else
                    positionOs *= length(mul(unity_ObjectToWorld, float4(0, 0, 1, 0)).xyz);
                #endif

                float2 scale = _Scale;

                #ifdef FLIP_FACING_HORIZONTAL
                if (dot(unity_WorldToObject._m20_m21_m22, viewDirectionWs) < 0)
                {
                    scale.x *= -1;
                }
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
                    col *= _Color;
                #elif defined(_COLORMODE_OVERLAY)
                    col.rgb = lerp(1 - 2 * (1 - col.rgb) * (1 - _Color.rgb), 2 * col.rgb * _Color.rgb, step(col.rgb, 0.5));
                    col.a *= _Color.a;
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
                col += _OutlineColor * GetOutline(i.uv0, i.uv1, _OutlineWidth, col.a);
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
            ENDCG
        }
    }
    CustomEditor "OrchidSeal.Billboard.Editor.BillboardEditor"
}
