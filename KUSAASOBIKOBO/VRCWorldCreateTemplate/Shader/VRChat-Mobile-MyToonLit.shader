// VRChat Toon shader, based on Unity's Mobile/Diffuse. Copyright (c) 2019 VRChat.
//Partially derived from "XSToon" (MIT License) - Copyright (c) 2019 thexiexe@gmail.com
// Simplified Toon shader.
// -fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "VRChat/Mobile/MyToon Lit"
{
    Properties
    {    
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        Cull off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
            
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fwdbase 
            #pragma skip_variants SHADOWS_SHADOWMASK SHADOWS_SCREEN SHADOWS_DEPTH SHADOWS_CUBE

            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"

            struct VertexInput
            {
                fixed4 vertex : POSITION;
                fixed2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {    
                fixed4 pos : SV_POSITION;
                fixed2 uv : TEXCOORD0;
                fixed4 worldPos : TEXCOORD1;
                fixed4 color : TEXCOORD2;
                fixed4 indirect : TEXCOORD3;
                fixed4 direct : TEXCOORD4;
                SHADOW_COORDS(5)
            };

            UNITY_DECLARE_TEX2D(_MainTex); 
            fixed4 _MainTex_ST;

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);                
                fixed3 indirectDiffuse = ShadeSH9(float4(0, 0, 0, 1)); // We don't care about anything other than the color from GI, so only feed in 0,0,0, rather than the normal       
                o.color = v.color;
                o.direct = _LightColor0;
                o.indirect = indirectDiffuse.xyzz;
                //TRANSFER_SHADOW(o);
                return o;
            }
            
            float4 frag (VertexOutput i, float facing : VFACE) : SV_Target
            {
                UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos.xyz);
                fixed4 albedo = UNITY_SAMPLE_TEX2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                fixed4 final = (albedo * i.color) * (i.direct * attenuation + i.indirect);
                return final;
            }
            ENDCG
        }
    }
    Fallback "VRChat/Mobile/Diffuse"
}
