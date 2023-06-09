﻿Shader "Unlit/MyUnlitShader"
{
	Properties{
	   [NoScaleOffset] _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
	}

		SubShader{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			LOD 100
			Cull off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			BlendOp Add, Max

			Pass {
				CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma target 2.0
					#pragma multi_compile_fog

					#include "UnityCG.cginc"

					struct appdata_t {
						half4 vertex : POSITION;
						half2 texcoord : TEXCOORD0;
						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct v2f {
						half4 vertex : SV_POSITION;
						half2 texcoord : TEXCOORD0;
						UNITY_VERTEX_OUTPUT_STEREO
					};

					sampler2D _MainTex;
					half4 _MainTex_ST;

					v2f vert(appdata_t v)
					{
						v2f o;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
						o.vertex = UnityObjectToClipPos(v.vertex);
						o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
						return o;
					}

					fixed4 frag(v2f i) : SV_Target
					{
						fixed4 col = tex2D(_MainTex, i.texcoord);
						return col;
					}
				ENDCG
			}
	}

}