Shader "Custom/RawImageFront" {
    Properties{
        _MainTex("Texture", 2D) = "white" {}
    }

        SubShader{
            Tags { "Queue" = "Overlay" "RenderType" = "Opaque" }
            Pass {
                Lighting Off
                Cull Off
                ZWrite Off
                ColorMask RGB
                Blend One OneMinusSrcAlpha

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    fixed4 color : COLOR;
                };

                struct v2f {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                };

                sampler2D _MainTex;

                v2f vert(appdata v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.color = v.color;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target {
                    fixed4 col = tex2D(_MainTex, i.uv);
                    col.a *= i.color.a;
                    return col * i.color;
                }
                ENDCG
            }
    }
}