Shader "ChillyRoom/Noise/WorleyNoise2D"
{
	Properties
	{
		_Scale("_Scale", Range(0,255)) = 1.0
		_Octaves("_Octaves", Int) = 1.0
		_Lacunarity("_Lacunarity", Float) = 1.0
		_Gain("_Gain", Float) = 1.0
		_Amplitude0("_Amplitude0", Float) = 1.0
		_Frequency0("_Frequency0", Float) = 1.0
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
			#include "NoiseLib.cginc"

			float _Scale;

			float2 random2(float2 p) {
				return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);
			}
			
			float2 Hash2(float2 p)
			{
				float r = 523.0*sin(dot(p, float2(53.3158, 43.6143)));
				return float2(frac(15.32354 * r), frac(17.25865 * r));
			}

			float worley2D0(float2 uv) {
				float d = 1.0e10;
				for (int xo = -1; xo <= 1; xo++)
				{
					for (int yo = -1; yo <= 1; yo++)
					{
						float2 tp = floor(uv) + float2(xo, yo);
						tp = uv - tp - Hash2(fmod(tp, _Scale / 2));
						d = min(d, dot(tp, tp));
					}
				}
				return sqrt(d);
			}

			//IQ algorithm
			float worley2D(float2 uv) {
				uint xi = floor(uv.x);
				uint yi = floor(uv.y);

				float2 f_xy = frac(uv);
				float3 m = 1000;
				for (int _y = -1; _y <= 1; _y++) {
					for (int _x = -1; _x <= 1; _x++)
					{
						float2 neighbor = float2(float(_x), float(_y));
						float2 p = neighbor + random2(neighbor + float2(xi, yi));	//0~1
						float distance = dot(p - f_xy, p - f_xy);
						m = distance < m.x ? float3(distance, p.x, p.y) : m;
					}
				}
				return sqrt(m.x);
			}


#ifdef _USEFBM
			float noise(float2 uv) {
				return worley2D(uv*_Scale).r;
			}
#endif

			fixed4 frag (v2f_customrendertexture i) : SV_Target
			{
#ifdef _USEFBM

				fixed4 col = useFbm2D(i.globalTexcoord.xy);
#else
				fixed4 col = worley2D(i.globalTexcoord.xy*_Scale);
#endif
				return col;
			}
			ENDCG
		}
	}
}
