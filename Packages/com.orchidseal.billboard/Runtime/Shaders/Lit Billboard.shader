Shader "Orchid Seal/OSP Billboard/Lit Billboard"
{
    Properties
    {
        [HideInInspector] [Enum(Opaque,0,Cutout,1,Transparent,2,Premultiply,3,Additive,4,Custom,5)] _RenderMode("Render Mode", Int) = 2
        
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        [Gamma] _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Toggle(USE_ALPHA_TEST)] _UseAlphaTest("Enable Alpha Test", Float) = 0
        _AlphaCutoff("Alpha Cutoff", Float) = 0.5
        [Toggle] _AlphaToMask("Alpha To Mask", Float) = 0
        
        [KeywordEnum(None, Auto, Camera, World_Y, Local_Y)] _Billboard_Mode("Billboard Mode", Float) = 1
        
        _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        
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
            #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_AUTO _BILLBOARD_MODE_CAMERA _BILLBOARD_MODE_WORLD_Y _BILLBOARD_MODE_LOCAL_Y
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma vertex vertParticleShadowCaster
            #pragma fragment fragParticleShadowCaster

            #include "UnityCG.cginc"
            #include "OSP Billboard.cginc"

            #if defined(CAST_TRANSPARENT_SHADOWS) && defined(UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS)
            #define UNITY_STANDARD_USE_DITHER_MASK 1
            #endif

            UNITY_DECLARE_TEX2D(_MainTex);
            float4 _MainTex_ST;
            float4 _Color;

            float _AlphaCutoff;

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

            void vertParticleShadowCaster(VertexInputShadowCaster v, out VertexOutputShadowCaster o, out float4 opos: SV_POSITION)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v.vertex = mul(unity_WorldToObject, BillboardWs(v.vertex));
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.alpha = v.color.a;
                TRANSFER_SHADOW_CASTER_NOPOS(o,opos);
            }

            half4 fragParticleShadowCaster(
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
            #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_AUTO _BILLBOARD_MODE_CAMERA _BILLBOARD_MODE_WORLD_Y _BILLBOARD_MODE_LOCAL_Y
            
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
            #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_AUTO _BILLBOARD_MODE_CAMERA _BILLBOARD_MODE_WORLD_Y _BILLBOARD_MODE_LOCAL_Y
            
            #pragma vertex VertexForwardAdd
            #pragma fragment FragmentForwardAdd
            #include "Lit Billboard Forward.cginc"
            ENDCG
        }
    }
    CustomEditor "OrchidSeal.Billboard.Editor.LitBillboardEditor"
}
