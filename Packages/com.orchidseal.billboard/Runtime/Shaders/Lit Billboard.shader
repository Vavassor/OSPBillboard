Shader "Orchid Seal/OSP Billboard/Lit Billboard"
{
    Properties
    {
        [HideInInspector] [Enum(Opaque,0,Cutout,1,Transparent,2,Premultiply,3,Additive,4,Custom,5)] _RenderMode("Render Mode", Int) = 2
        [Toggle(USE_PIXEL_SHARPEN)] _UsePixelSharpen("Sharp Pixels", Float) = 0
        
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        [Gamma] _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Toggle(USE_ALPHA_TEST)] _UseAlphaTest("Enable Alpha Test", Float) = 0
        _AlphaCutoff("Alpha Cutoff", Float) = 0.5
        [Toggle] _AlphaToMask("Alpha To Mask", Float) = 0
        
        [KeywordEnum(None, Spherical, Cylindrical_World, Cylindrical_Local)] _Billboard_Mode("Billboard Mode", Float) = 1
        _Position("Position (XY)", Vector) = (0, 0, 0, 0)
        _RotationRoll("Rotation", Float) = 0
        _Scale("Scale (XY)", Vector) = (1, 1, 0, 0)
        
        _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        
        [HDR] _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionMap("Emission Map", 2D) = "black" {}
        
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
            Name "Shadow Caster"
            Tags { "LightMode" = "ShadowCaster" }

            BlendOp Add
            Blend One Zero
            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma target 3.0

            #pragma shader_feature_local USE_ALPHA_TEST
            #pragma shader_feature_local CAST_TRANSPARENT_SHADOWS
            #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_SPHERICAL _BILLBOARD_MODE_CYLINDRICAL_WORLD _BILLBOARD_MODE_CYLINDRICAL_LOCAL
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

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
                fixed alpha : TEXCOORD2;
            };

            void vertShadowCaster(VertexInputShadowCaster v, out VertexOutputShadowCaster o, out float4 opos: SV_POSITION)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float4 positionOs = v.vertex;
                positionOs.xy = Transform2d(positionOs.xy, _Position, _RotationRoll, _Scale);
                v.vertex = mul(unity_WorldToObject, float4(BillboardWs(positionOs), 1));
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.alpha = v.color.a;
                TRANSFER_SHADOW_CASTER_NOPOS(o,opos);
            }

            half4 fragShadowCaster(
                VertexOutputShadowCaster i
                #if defined(UNITY_STANDARD_USE_DITHER_MASK)
                    , UNITY_VPOS_TYPE vpos : VPOS
                #endif
                ): SV_Target
            {
                half alpha = UNITY_SAMPLE_TEX2D(_MainTex, i.texcoord).a * _Color.a;
                alpha *= i.alpha;
                
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
            ENDCG
        }

        Pass
        {
            Name "Forward Base"
            Tags { "LightMode" = "ForwardBase" }
            
            CGPROGRAM
            #pragma target 3.0
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap 
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            #pragma shader_feature_local USE_ALPHA_TEST
            #pragma shader_feature_local USE_NORMAL_MAP
            #pragma shader_feature_local USE_EMISSION_MAP
            #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_SPHERICAL _BILLBOARD_MODE_CYLINDRICAL_WORLD _BILLBOARD_MODE_CYLINDRICAL_LOCAL
            #pragma shader_feature_local USE_PIXEL_SHARPEN
            
            #pragma vertex VertexForwardBase
            #pragma fragment FragmentForwardBase
            #include "Lit Billboard Forward.cginc"
            ENDCG
        }

        Pass
        {
            Name "Forward Add"
            Tags { "LightMode" = "ForwardAdd" }
            
            Blend [_BlendSrc] One
            Fog { Color (0,0,0,0) }
            ZWrite Off
            ZTest LEqual
            
            CGPROGRAM
            #pragma target 3.0
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog

            #pragma shader_feature_local USE_ALPHA_TEST
            #pragma shader_feature_local USE_NORMAL_MAP
            #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_SPHERICAL _BILLBOARD_MODE_CYLINDRICAL_WORLD _BILLBOARD_MODE_CYLINDRICAL_LOCAL
            #pragma shader_feature_local USE_PIXEL_SHARPEN
            
            #pragma vertex VertexForwardAdd
            #pragma fragment FragmentForwardAdd
            #include "Lit Billboard Forward.cginc"
            ENDCG
        }
    }
    CustomEditor "OrchidSeal.Billboard.Editor.LitBillboardEditor"
}
