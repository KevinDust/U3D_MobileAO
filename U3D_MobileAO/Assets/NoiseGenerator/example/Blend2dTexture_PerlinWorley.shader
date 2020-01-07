Shader "ChillyRoom/Noise/Blend2D/PerlinWorley"
{
	Properties
	{
		_MainTex0("MainTex",2D) = "white" {}
		_MainTex1("MainTex",2D) = "white" {}
		_OldMax("_OldMax", Float) = 1.0
		_NewMax("_NewMax", Float) = 1.0
		_NewMin("_NewMin", Float) = 0.0
		_Scale("_Scale", Float) = 1.0

	}
		SubShader
		{
			LOD 100

			Pass
			{
				Name "Update"
				CGPROGRAM
				#pragma vertex CustomRenderTextureVertexShader
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "UnityCustomRenderTexture.cginc"
				sampler2D _MainTex0;
				sampler2D _MainTex1;

				float _OldMax;
				float _NewMax;
				float _NewMin;
				float _Scale;

				fixed4 frag(v2f_customrendertexture i) : SV_Target
				{

					float _OldMin = tex2D(_MainTex1, i.globalTexcoord.xy*_Scale);
					float _Perlin = tex2D(_MainTex0, i.globalTexcoord.xy*_Scale);
					float PerlinWorley = _NewMin + (((_Perlin - _OldMin) / (_OldMax - _OldMin))*(_NewMax - _NewMin));
					return PerlinWorley;
				}
				ENDCG
			}
		}
}
