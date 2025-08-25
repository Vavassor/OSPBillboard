Shader "Orchid Seal/OSP Billboard/Unlit Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture", 2D) = "white" {}
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
            #define SPRITE_RENDERER_ON
            #include "Unlit Billboard.cginc"
            ENDCG
        }
    }
    CustomEditor "OrchidSeal.Billboard.Editor.BillboardEditor"
}
