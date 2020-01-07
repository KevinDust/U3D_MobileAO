Shader "ChillyRoom/Noise/WorleyNoise3D"
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


			float3 hash(float3 x)
			{
				x = float3(dot(x, float3(127.1, 311.7, 74.7)),
					dot(x, float3(269.5, 183.3, 246.1)),
					dot(x, float3(113.5, 271.9, 124.6)));

				return frac(sin(x)*43758.5453123);
			}
			#define HASHSCALE3 float3(.1031, .1030, .0973)
			float3 hash33(float3 p3)
			{
				p3 = frac(p3 * HASHSCALE3);
				p3 += dot(p3, p3.yxz + 19.19);
				return frac((p3.xxy + p3.yxx)*p3.zyx);

			}

			float3 worley3D1(float3 x)
			{
				float3 p = floor(x);
				float3 f = frac(x);

				float id = 0.0;
				float2 res = float2(100.0,100.0);
				for (int k = -1; k <2; k++)
					for (int j = -1; j <2; j++)
						for (int i = -1; i <2; i++)
						{
							float3 b = float3(float(i), float(j), float(k));
							float3 r = float3(b) - f + hash(p + b);
							float d = dot(r, r);

							if (d < res.x)
							{
								id = dot(p + b, float3(1.0, 57.0, 113.0));
								res = float2(d, res.x);
							}
							else if (d < res.y)
							{
								res.y = d;
							}
						}

				return float3(sqrt(res), abs(id));
			}

			float4 worley3D(float3 uv) {
	
				float3 i_uv = floor(uv);
				float3 f_uv = frac(uv);
				float m = 1000;
				for (int _z = -1; _z <= 1; _z++) {
					for (int _y = -1; _y <= 1; _y++) {
						for (int _x = -1; _x <= 1; _x++)
						{
							float3 neighbor = float3(float(_x), float(_y), float(_z));
							float3 p = neighbor + hash33(neighbor + i_uv);	//0~1
							float distance = dot(p - f_uv, p - f_uv);
							m = min(distance, m);// ? distance : m;
						}
					}
				}
				return  sqrt(m.x);
			}

#ifdef _USEFBM
			float noise(float3 uvw) {
				return worley3D(uvw*_Scale).r;
			}
#endif
			fixed4 frag(v2f_customrendertexture i) : SV_Target
			{
#ifdef _USEFBM

				fixed4 col = useFbm3D(i.globalTexcoord.xyz);
#else
				fixed4 col = worley3D(i.globalTexcoord.xyz*_Scale).r;
#endif
			return col;
			}
			ENDCG
		}
	}
}
