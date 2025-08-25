Shader "Orchid Seal/OSP Billboard/Lit Sprite"
{
    Properties
    {
        [HideInInspector] [Enum(Opaque,0,Cutout,1,Transparent,2,Premultiply,3,Additive,4,Custom,5)] _RenderMode("Render Mode", Int) = 2
        [Toggle(USE_PIXEL_SHARPEN)] _UsePixelSharpen("Sharp Pixels", Float) = 0
        
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        [Gamma] _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Toggle(USE_ALPHA_TEST)] _UseAlphaTest("Enable Alpha Test", Float) = 0
        _AlphaCutoff("Alpha Cutoff", Float) = 0.5
        [Toggle] _AlphaToMask("Alpha To Mask", Float) = 0
        
        [KeywordEnum(None, Spherical, Cylindrical_World, Cylindrical_Local)] _Billboard_Mode("Billboard Mode", Float) = 1
        _Position("Position (XY)", Vector) = (0, 0, 0, 0)
        _RotationRoll("Rotation", Float) = 0
        _Scale("Scale (XY)", Vector) = (1, 1, 0, 0)
        [Toggle(FLIP_FACING_HORIZONTAL)] _FlipFacingHorizontal("Flip Facing Horizontal", Float) = 0
        
        _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        
        [HDR] _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionMap("Emission Map", 2D) = "black" {}
        
        _FlipbookTexArray("Texture Array", 2DArray) = "" {}
        _FlipbookTint("Tint", Color) = (1,1,1,1)
        _FlipbookScrollVelocity("Scroll Velocity (XY)", Vector) = (0, 0, 0, 0)
        [Enum(Replace,0,Transparent,1,Add,2,Multiply,3)] _FlipbookBlendMode("Blend Mode", Float) = 0
        _FlipbookFramesPerSecond("Frames Per Second", Float) = 30
        [Toggle(USE_FLIPBOOK_SMOOTHING)] _UseFlipbookSmoothing("Smoothing", Float) = 0
        [Toggle] _FlipbookUseManualFrame("Control Frame Manually", Float) = 0
        _FlipbookManualFrame("Manual Frame", Float) = 0
        
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
        
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "CanUseSpriteAtlas" = "True"
            "DisableBatching" = "True"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        
        AlphaToMask [_AlphaToMask]
        Blend [_BlendSrc] [_BlendDst]
        BlendOp [_BlendOp]
        Cull Off
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
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        
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
            #pragma shader_feature_local USE_FLIPBOOK
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster
            #define SPRITE_RENDERER_ON
            #include "Lit Billboard Shadow Caster.cginc"
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
            #pragma shader_feature_local USE_FLIPBOOK
            #pragma shader_feature_local USE_FLIPBOOK_SMOOTHING
            #pragma shader_feature_local FLIP_FACING_HORIZONTAL
            
            #pragma vertex VertexForward
            #pragma fragment FragmentForward

            #define SPRITE_RENDERER_ON
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
            #pragma shader_feature_local USE_FLIPBOOK
            #pragma shader_feature_local USE_FLIPBOOK_SMOOTHING
            #pragma shader_feature_local FLIP_FACING_HORIZONTAL
            
            #pragma vertex VertexForward
            #pragma fragment FragmentForward

            #define SPRITE_RENDERER_ON
            #include "Lit Billboard Forward.cginc"
            ENDCG
        }
    }
    CustomEditor "OrchidSeal.Billboard.Editor.LitBillboardEditor"
}
