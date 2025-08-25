// The wire drawing method is based on "Phone-wire AA" by Emil Persson aka Humus.
// https://www.humus.name/index.php?page=3D&ID=89
//
// To prevent thin wires from becoming disconnected pixels at a distance, clamp the size to be at
// least a pixel wide. And fade its transparency as it gets further away, instead.
Shader "Hidden/Orchid Seal/Billboard Example/Wire"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _Radius ("Radius", float) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                centroid float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
	            centroid float fade : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Radius;

            v2f vert (appdata v)
            {
                float w = dot(UNITY_MATRIX_MVP[3], float4(v.vertex.xyz, 1.0f));
                float pixelRadius = w / (unity_CameraProjection._m11 * _ScreenParams.y);
                float radius = max(_Radius, pixelRadius);
	            float fade = _Radius / radius;
                float3 position = v.vertex.xyz + radius * v.normal;

                float2 uv = v.uv;
                uv.x *= 0.02 / radius;
                
                v2f o;
                o.vertex = UnityObjectToClipPos(float4(position, 1));
                o.uv = TRANSFORM_TEX(uv, _MainTex);
                o.fade = fade;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color * tex2D(_MainTex, i.uv);
                col.a *= i.fade;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
