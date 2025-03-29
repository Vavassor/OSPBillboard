Shader "Custom/Iridescent Test"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [NormalMap] _BumpMap ("Normal", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 1)) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Gradient ("Gradient", 2D) = "white" {}
        _GradientPower ("Gradient Power", float) = 1
        [Enum(Replace,0,Transparent,1,Add,2,Multiply,3)] _GradientBlendMode ("Gradient Blend Mode", float) = 1
        _GradientBlend ("Gradient Blend", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        // Color Blending..........................................................................

        #define BLEND_MODE_REPLACE 0
        #define BLEND_MODE_TRANSPARENT 1
        #define BLEND_MODE_ADD 2
        #define BLEND_MODE_MULTIPLY 3

        float4 BlendColor(float4 s, float4 d, float blendMode)
        {
            switch (blendMode)
            {
            case BLEND_MODE_REPLACE: return s;
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

        sampler2D _MainTex;
        sampler2D _BumpMap;
        half _NormalScale;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        sampler2D _Gradient;
        half _GradientPower;
        half _GradientBlendMode;
        half _GradientBlend;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
        
        half3 Plasma(half2 U)
        {
            #define H(s,v)   sin( s + (v).zxy - cos(s.zxy + (v).yzx) + cos(s.yzx + v) )
            float time = _Time.y;
            U = U/8. + sin(time);
            
            float3 d = float3(11.1, 14.3, 8.2);
            float3 t = time/d;
            float3 v = (U.x * H( float3(2,6,4), t - d.yzx)
                        + U.y * H( float3(3,7,5), t + d    )
                        ) /4.;
            float3 K = float3(3,5,6);
            
            return H(3.+K,((v+H(K,v))/2. +H(1.62+K, (v+H(K,v))/2. ) *3.)) * .5 + .5;
            #undef H
        }

        half3 Plasma2(half2 p)
        {
            float t = 2.0 * _Time.y;
            float w = sin(p.x + sin(2.0 * p.y + 0.2 * t)) + sin(length(p) + t) + 0.5 * sin(2.5 * p.x + t);
            return 0.6 + 0.4 * cos(w + float3(0,3.1,4.2));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Normal = UnpackScaleNormal (tex2D (_BumpMap, IN.uv_BumpMap), _NormalScale);
            // float3 normalWs = WorldNormalVector(IN, o.Normal);
            half rim = pow(1 - abs(dot(o.Normal, IN.viewDir)), _GradientPower);
            fixed4 gradient = tex2D(_Gradient, float2(saturate(rim), 0));
            // half4 gradient = half4(Plasma(256 * IN.uv_MainTex), 1);
            c.rgb = Plasma2(5.0 * IN.uv_MainTex);
            c = lerp(c, BlendColor(gradient, c, _GradientBlendMode), _GradientBlend);
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
