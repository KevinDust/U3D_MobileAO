Shader "ChillyRoom/Noise/Blend3D/PerlinWorley"
{
	Properties
	{
		_MainTex0("MainTex",3D) = "white" {}
		_MainTex1("MainTex",3D) = "white" {}
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
				sampler3D _MainTex0;
				sampler3D _MainTex1;

				float _OldMax;
				float _NewMax;
				float _NewMin;
				float _Scale;

				fixed4 frag(v2f_customrendertexture i) : SV_Target
				{

					float _OldMin = tex3D(_MainTex1, i.globalTexcoord.xyz*_Scale);
					float _Perlin = tex3D(_MainTex0, i.globalTexcoord.xyz*_Scale);
					float PerlinWorley = _NewMin + (((_Perlin - _OldMin) / (_OldMax - _OldMin))*(_NewMax - _NewMin));
					return PerlinWorley;
				}
				ENDCG
			}
		}
}
