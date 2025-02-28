Shader "Orchid Seal/OSP Billboard/Editor/Contain Blit"
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

            float _Aspect;
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

                if (_Aspect > 1.0)
                {
                    o.uv = v.texcoord * float2(1.0, _Aspect) - float2(0.0, (_Aspect - 1.0) * 0.5);
                }
                else
                {
                    o.uv = v.texcoord * float2(1.0 / _Aspect, 1.0) - float2((1.0 / _Aspect - 1.0) * 0.5, 0.0);
                }

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
