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
        [HideInInspector] [Enum(Opaque,0,Cutout,1,Transparent,2,Custom,3)] _RenderMode("Render Mode", Int) = 2
        [Toggle(USE_GAMMA_COLORSPACE)] _UseGammaSpace("Use Gamma Space Blending", Float) = 0
        
        // Makes the billboard appear grounded, and prevents it from tilting side to side when in VR.
        [KeywordEnum(None, Auto, View, Vertical)] _Billboard_Mode("Billboard Mode", Float) = 1
        _RotationRoll("Rotation", Float) = 0
        _Scale("Scale (XY)", Vector) = (1, 1, 0, 0)
        [Toggle(USE_NON_UNIFORM_SCALE)] _UseNonUniformScale("Use Non-Uniform Object Scale", Float) = 1
        [Toggle(KEEP_CONSTANT_SCALING)] _KeepConstantScaling("Keep Constant Scaling", Int) = 0
        _ConstantScale("Constant Scale", Float) = 1
        
        [Toggle(USE_FLIPBOOK)] _UseFlipbook("Enable Flipbook", Float) = 0
        _FlipbookTexArray("Texture Array", 2DArray) = "" {}
        _FlipbookTint("Tint", Color) = (1,1,1,1)
        _FlipbookScrollVelocity("Scroll Velocity (XY)", Vector) = (0, 0, 0, 0)
        [Enum(Replace,0,Transparent,1,Add,2,Multiply,3)] _FlipbookBlendMode("Blend Mode", Float) = 0
        _FlipbookFramesPerSecond("Frames Per Second", Float) = 30
        [Toggle(USE_FLIPBOOK_SMOOTHING)] _UseFlipbookSmoothing("Smoothing", Float) = 0
        [Toggle] _FlipbookUseManualFrame("Control Frame Manually", Float) = 0
        _FlipbookManualFrame("Manual Frame", Float) = 0
        
        [Toggle(USE_DISTANCE_FADE)] _UseDistanceFade("Enable Distance Fade", Float) = 0
        _DistanceFadeMinAlpha("Min Alpha", Range(0, 1)) = 1
        _DistanceFadeMaxAlpha("Max Alpha", Range(0, 1)) = 0
        _DistanceFadeMin("Min Distance", Float) = 0
        _DistanceFadeMax("Max Distance", Float) = 10
        
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
            #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_AUTO _BILLBOARD_MODE_VIEW _BILLBOARD_MODE_VERTICAL
            #pragma shader_feature_local USE_NON_UNIFORM_SCALE
            #pragma shader_feature_local KEEP_CONSTANT_SCALING
            #pragma shader_feature_local USE_DISTANCE_FADE

            #include "UnityCG.cginc"

            #define DEGREES_TO_RADIANS 0.0174532925
            #define VERTICAL_BILLBOARD _BILLBOARD_MODE_AUTO && defined(USING_STEREO_MATRICES) || _BILLBOARD_MODE_VERTICAL

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

            // Fog..........................................................................

            #define USING_FOG (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))

            // This transfers fog using radial distance so that it works in mirrors.
            fixed TransferFog(float4 clipPosition)
            {
                float3 viewPosition = UnityObjectToViewPos(clipPosition);
                float fogCoord = length(viewPosition.xyz);
                UNITY_CALC_FOG_FACTOR_RAW(fogCoord);
                return saturate(unityFogFactor);
            }

            #if USING_FOG
            #define OSP_APPLY_FOG(col, input) col.rgb = lerp(unity_FogColor.rgb, col.rgb, input.fog);
            #define OSP_FOG_COORDS(texcoordNumber) fixed fog : TEXCOORD##texcoordNumber;
            #define OSP_TRANSFER_FOG(clipPosition, output) output.fog = TransferFog(clipPosition);
            #else
            #define OSP_APPLY_FOG(col, input)
            #define OSP_FOG_COORDS(texcoordNumber)
            #define OSP_TRANSFER_FOG(clipPosition, output)
            #endif // USING_FOG

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
                UNITY_FOG_COORDS(3)
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            UNITY_DECLARE_TEX2D(_MainTex);
            float4 _MainTex_ST;
            float4 _Color;

            // Transformation
            float _RotationRoll;
            float2 _Scale;
            float _ConstantScale;

            // Flipbook
            UNITY_DECLARE_TEX2DARRAY(_FlipbookTexArray);
            float4 _FlipbookTexArray_ST;
            float4 _FlipbookTint;
            float2 _FlipbookScrollVelocity;
            float _FlipbookBlendMode;
            float _FlipbookFramesPerSecond;
            float _FlipbookUseManualFrame;
            float _FlipbookManualFrame;

            // Distance Fade
            float _DistanceFadeMinAlpha;
            float _DistanceFadeMaxAlpha;
            float _DistanceFadeMin;
            float _DistanceFadeMax;

            // Alpha Test
            float _AlphaCutoff;

            // Transformation...............................................................

            bool IsInMirror()
            {
                return unity_CameraProjection[2][0] != 0.0f || unity_CameraProjection[2][1] != 0.0f;
            }

            float3 GetCenterCameraPosition()
            {
                #if defined(USING_STEREO_MATRICES)
                    float3 worldPosition = (unity_StereoWorldSpaceCameraPos[0] + unity_StereoWorldSpaceCameraPos[1]) / 2.0;
                #else
                    float3 worldPosition = _WorldSpaceCameraPos.xyz;
                #endif
                return worldPosition;
            }

            float4x4 LookAtMatrix(float3 forward, float3 up)
            {
                float3 xAxis = normalize(cross(forward, up));
                float3 yAxis = up;
                float3 zAxis = forward;
                return float4x4(
                    xAxis.x, yAxis.x, zAxis.x, 0,
                    xAxis.y, yAxis.y, zAxis.y, 0,
                    xAxis.z, yAxis.z, zAxis.z, 0,
                    0, 0, 0, 1);
            }

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

            // float GetRoll(float4x4 m, float3 scale)
            // {
            //     return atan2(-m[2][1] / scale.z, m[2][2] / scale.z);
            // }

            float2 Rotate2D(float2 v, float angle)
            {
                float s, c;
                sincos(angle, s, c);
                return float2(c * v.x - s * v.y, s * v.x + c * v.y);
            }
            
            // Shader Functions....................................................................

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionOs = float3(_Scale, 1) * v.vertex.xyz;

                #if KEEP_CONSTANT_SCALING || VERTICAL_BILLBOARD || USE_DISTANCE_FADE
                    float3 cameraPositionWs = GetCenterCameraPosition();
                    float3 objectCenterWs;
                    objectCenterWs.x = unity_ObjectToWorld[0][3];
                    objectCenterWs.y = unity_ObjectToWorld[1][3];
                    objectCenterWs.z = unity_ObjectToWorld[2][3];
                    float3 viewDirectionWs = objectCenterWs - cameraPositionWs;
                    float distanceWs = length(viewDirectionWs);
                #endif

                #if KEEP_CONSTANT_SCALING
                    positionOs *= _ConstantScale * distanceWs;
                #endif
                
                #ifdef USE_NON_UNIFORM_SCALE
                    positionOs *= GetScale(unity_ObjectToWorld);
                #else
                    positionOs *= length(mul(unity_ObjectToWorld, float4(0, 0, 1, 0)).xyz);
                #endif
                
                positionOs.xy = Rotate2D(positionOs.xy, DEGREES_TO_RADIANS * _RotationRoll);
                // TODO: Getting rotation from the model matrix breaks when parent objects are rotated/scaled.
                // positionOs.xy = Rotate2D(positionOs.xy, GetRoll(unity_ObjectToWorld, objectScale));

                #if _BILLBOARD_MODE_NONE
                    o.pos = UnityObjectToClipPos(v.vertex);
                #elif VERTICAL_BILLBOARD
                    if (IsInMirror())
                    {
                        viewDirectionWs = mul((float3x3) unity_WorldToObject, unity_CameraWorldClipPlanes[5].xyz);
                    }
                    float3 positionWs = mul(LookAtMatrix(-viewDirectionWs, float4(0, 1, 0, 0)), positionOs) + objectCenterWs.xyz;
                    o.pos =  mul(UNITY_MATRIX_VP, float4(positionWs, 1.0));
                #else
                    float4 positionVs = mul(UNITY_MATRIX_MV, float4(0, 0, 0, 1)) + float4(positionOs.xy, 0, 0);
                    o.pos = mul(UNITY_MATRIX_P, positionVs);
                #endif
                
                o.uv0 = TRANSFORM_TEX(v.uv, _MainTex);

                #ifdef USE_FLIPBOOK
                    float2 transformedTexcoord = TRANSFORM_TEX(v.uv, _FlipbookTexArray);
                    float2 scrolledTexcoord = transformedTexcoord + _Time.y * _FlipbookScrollVelocity;
                    o.uv1 = GetFlipbookTexcoord(_FlipbookTexArray, scrolledTexcoord, _FlipbookFramesPerSecond, _FlipbookUseManualFrame, _FlipbookManualFrame);
                #endif

                #ifdef USE_DISTANCE_FADE
                o.distanceFade = lerp(_DistanceFadeMinAlpha, _DistanceFadeMaxAlpha, smoothstep(_DistanceFadeMin, _DistanceFadeMax, distanceWs));
                #endif

                UNITY_TRANSFER_FOG(o,o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
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

                #ifdef USE_DISTANCE_FADE
                    col.a *= i.distanceFade;
                #endif

                #ifdef USE_ALPHA_TEST
                    clip(col.a - _AlphaCutoff);
                #endif

                col.rgb = FROM_SHADING_COLOR_SPACE(col.rgb);

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    CustomEditor "OrchidSeal.Billboard.Editor.BillboardEditor"
}
