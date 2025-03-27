// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// Modified by VRChat Inc. to be more VR-friendly and support supersampling.

Shader "Orchid Seal/OSP Billboard/Supersampled UI Billboard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        
        [KeywordEnum(None, Spherical, Cylindrical_World, Cylindrical_Local)] _Billboard_Mode("Billboard Mode", Float) = 1
        
        [Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorMask ("Color Mask", Float) = 15
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0 //"Off"
        
        [Header(Depth)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 0.0 //"Off"
        
        [Header(Stencil)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_Cull]
        Lighting Off
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "OSP Billboard.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_SPHERICAL _BILLBOARD_MODE_CYLINDRICAL_WORLD _BILLBOARD_MODE_CYLINDRICAL_LOCAL

            struct appdata_t
            {
                float4 vertex   : POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 mask     : TEXCOORD1;
                centroid float2 texcoordCentroid : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
        	    float scaleOs = length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z));
        	    // scaleOs *= (_KeepConstantScaling) ? _ConstantScale * length(unity_ObjectToWorld._m03_m13_m23 - GetCenterCameraPosition()); : 1;

                float4 positionOs = v.vertex;

                #if (_BILLBOARD_MODE_CYLINDRICAL_LOCAL || _BILLBOARD_MODE_CYLINDRICAL_WORLD || _BILLBOARD_MODE_SPHERICAL)
                positionOs.xyz *= scaleOs;
                #endif
                float4 vPosition = BillboardCs(positionOs);
                
                OUT.vertex = vPosition;
                OUT.texcoord = OUT.texcoordCentroid = TRANSFORM_TEX(v.texcoord, _MainTex);

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                // per pixel partial derivatives
                float2 dx = ddx(IN.texcoord.xy);
                float2 dy = ddy(IN.texcoord.xy);// rotated grid uv offsets
                float2 uvOffsets = float2(0.125, 0.375);
                float2 offsetUV = float2(0.0, 0.0);// supersampled using 2x2 rotated grid
                half4 color = 0;

                // calculate gradient manually, since we don't want them to come from centroid texcoords
                float4 grad = float4(ddx(IN.texcoord.xy), ddy(IN.texcoord.xy));
                const float bias_pow = 0.5f; // pow(2, -1.0f)
                grad *= bias_pow;

                // supersampling
                offsetUV.xy = IN.texcoordCentroid.xy + uvOffsets.x * dx + uvOffsets.y * dy;
                color += tex2Dgrad(_MainTex, offsetUV, grad.xy, grad.zw);
                offsetUV.xy = IN.texcoordCentroid.xy - uvOffsets.x * dx - uvOffsets.y * dy;
                color += tex2Dgrad(_MainTex, offsetUV, grad.xy, grad.zw);
                offsetUV.xy = IN.texcoordCentroid.xy + uvOffsets.y * dx - uvOffsets.x * dy;
                color += tex2Dgrad(_MainTex, offsetUV, grad.xy, grad.zw);
                offsetUV.xy = IN.texcoordCentroid.xy - uvOffsets.y * dx + uvOffsets.x * dy;
                color += tex2Dgrad(_MainTex, offsetUV, grad.xy, grad.zw);

                color *= 0.25;
                color = (color + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
