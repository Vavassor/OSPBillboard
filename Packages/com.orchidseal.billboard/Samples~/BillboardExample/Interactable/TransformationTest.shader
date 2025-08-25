Shader "Hidden/Orchid Seal/Billboard Example/TransformationTest"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Header(Float)]
        [Toggle(FLOAT_ON)] _FloatOn("Float Enabled", Float) = 0
        _FloatAmplitude("Float Amplitude", Float) = 0.15
        _FloatAxis("Float Axis", Vector) = (0, 1, 0, 0)
        _FloatFrequency("Float Frequency", Float) = 3
        _FloatPhase("Float Phase", Float) = 0
        
        [Header(Spin)]
        [Toggle(SPIN_ON)] _SpinOn("Spin Enabled", Float) = 0
        _SpinAxis("Spin Axis", Vector) = (0, 1, 0, 0)
        _SpinPhase("Spin Phase", Float) = 0
        _SpinSpeed("Spin Speed", Float) = 1
        
        [Header(Throb)]
        [Toggle(THROB_ON)] _ThrobOn("Throb Enabled", Float) = 0
        _ThrobAmplitude("Throb Amplitude", Float) = 0.15
        _ThrobFrequency("Throb Frequency", Float) = 5
        _ThrobPhase("Throb Phase", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "DisableBatching" = "True"
            "RenderType" = "Opaque"
        }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard nolightmap nometa fullforwardshadows addshadow vertex:vert
        #pragma target 3.0

        #pragma shader_feature_local FLOAT_ON
        #pragma shader_feature_local SPIN_ON
        #pragma shader_feature_local THROB_ON

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        half _FloatAmplitude;
        half3 _FloatAxis;
        half _FloatFrequency;
        half _FloatPhase;

        half3 _SpinAxis;
        half _SpinPhase;
        half _SpinSpeed;

        half _ThrobAmplitude;
        half _ThrobFrequency;
        half _ThrobPhase;

        float3x3 AngleAxisMatrix(float angle, float3 axis)
        {
            float c, s;
            sincos(angle, s, c);

            float t = 1 - c;
            float x = axis.x;
            float y = axis.y;
            float z = axis.z;

            return float3x3(
                t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
                t * x * y + s * z,  t * y * y + c,      t * y * z - s * x,
                t * x * z - s * y,  t * y * z + s * x,  t * z * z + c
            );
        }

        void vert (inout appdata_full v)
        {
            #if THROB_ON
            v.vertex.xyz += _ThrobAmplitude * sin(_ThrobFrequency * _Time.y + _ThrobPhase) * v.vertex.xyz;
            #endif // THROB_ON

            #if SPIN_ON
            float3x3 rotation = AngleAxisMatrix(_SpinSpeed * _Time.y + _SpinPhase, _SpinAxis);
            v.vertex.xyz = mul(rotation, v.vertex.xyz);
            v.normal.xyz = mul(rotation, v.normal.xyz);
            #endif // SPIN_ON

            #if FLOAT_ON
            v.vertex.xyz += _FloatAmplitude * sin(_FloatFrequency * _Time.y + _FloatPhase) * _FloatAxis;
            #endif // FLOAT_ON
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
