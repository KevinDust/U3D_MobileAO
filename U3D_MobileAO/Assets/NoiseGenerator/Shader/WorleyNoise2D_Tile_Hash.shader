Shader "ChillyRoom/Noise/WorleyNoise2D_Tile_Hash"
{
	Properties
	{
		_Scale("_Scale", Range(0,255)) = 1.0
		_Octaves("_Octaves", Int) = 1.0
		_Lacunarity("_Lacunarity", Float) = 1.0
		_Gain("_Gain", Float) = 1.0
		_Amplitude0("_Amplitude0", Float) = 1.0
		_Frequency0("_Frequency0", Float) = 1.0
		_XOffset("X", Float) = 1.0
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
			#pragma multi_compile _ _USEFBM 
			#include "UnityCG.cginc"
			#include "UnityCustomRenderTexture.cginc"
			float _Scale;

			float2 Hash(float2 P)
			{
				return frac(cos(mul(float2x2(-64.2, 71.3, 81.4, -29.8), P))*8321.3);
			}
			float worley2D(float2 P)
			{
				float Dist = 1.;
				float2 I = floor(P);
				float2 F = frac(P);

				for (int X = -1; X <= 1; X++)
					for (int Y = -1; Y <= 1; Y++)
					{
						float D = distance(Hash(I + float2(X, Y)) + float2(X, Y), F);
						Dist = min(Dist, D);
					}
				return 1-sqrt(Dist);

			}

			float _XOffset;
#ifdef _USEFBM
			float noise(float2 uv) {
				return worley2D(uv*_Scale+float2(_XOffset,0)).r;
			}
#endif
#include "NoiseLib.cginc"

			fixed4 frag (v2f_customrendertexture i) : SV_Target
			{
#ifdef _USEFBM

				fixed4 col = useFbm2D(i.globalTexcoord.xy);
#else
				fixed4 col = worley2D(i.globalTexcoord.xy*_Scale + float2(_XOffset, 0));
#endif
				return col;
			}
			ENDCG
		}
	}
}
