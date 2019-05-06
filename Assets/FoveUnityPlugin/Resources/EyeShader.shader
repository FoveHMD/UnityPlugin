Shader "Fove/EyeShader"
{
	Properties {
		_Tex1 ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Tex2 ("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	SubShader {
		Pass {  
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				
				#include "UnityCG.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					half2 texcoord : TEXCOORD0;
				};

				sampler2D _Tex1;
				float4 _Tex1_ST;
				sampler2D _Tex2;
				float4 _Tex2_ST;

				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord = TRANSFORM_TEX(v.texcoord, _Tex1);
					return o;
				}
				
				fixed4 frag (v2f i) : SV_Target
				{
					float mask = round(i.texcoord.x);

					i.texcoord.x *= 2;
					float4 colorLeft = tex2D(_Tex2, i.texcoord);
					i.texcoord.x -= 1;
					float4 colorRight = tex2D(_Tex1, i.texcoord);

					return lerp(colorLeft, colorRight, mask);
				}
			ENDCG
			Cull Off
		}
	}
}
