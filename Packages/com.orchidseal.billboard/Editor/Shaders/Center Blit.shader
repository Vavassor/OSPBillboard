Shader "Orchid Seal/OSP Billboard/Center Blit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _TargetTexelSize;
            UNITY_DECLARE_TEX2D(_MainTex);
            float4 _MainTex_TexelSize;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0) v.texcoord.y = 1.0 - v.texcoord.y;
                #endif

                float2 scale = _TargetTexelSize.zw * _MainTex_TexelSize.xy;
                float2 offset = floor(0.5 * (_TargetTexelSize.zw - _MainTex_TexelSize.zw)) * _TargetTexelSize.xy;
                o.uv = scale * (v.texcoord - offset);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (i.uv.x < 0.0 || i.uv.x > 1.0 || i.uv.y < 0.0 || i.uv.y > 1.0)
                {
                    return 0;
                }
                
                return UNITY_SAMPLE_TEX2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
