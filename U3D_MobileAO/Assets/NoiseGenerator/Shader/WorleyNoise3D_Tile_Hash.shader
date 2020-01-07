Shader "ChillyRoom/Noise/WorleyNoise3D_Tile_Hash"
{
	Properties
	{
		[IntRange]_Scale("_Scale", Range(0,255)) = 1.0
		_Octaves("_Octaves", Int) = 1.0
		_Lacunarity("_Lacunarity", Float) = 1.0
		_Gain("_Gain", Float) = 1.0
		_Amplitude0("_Amplitude0", Float) = 1.0
		_Frequency0("_Frequency0", Float) = 1.0
		_C0("_C0", Float) = 1.0
		[Toggle(_USEFBM)] _FBM_ON("FBM_ON", Float) = 0


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

			float hash(float n)
			{
				return frac(sin(n + 1.951f) * 43758.5453f);
			}

			// hash based 3d value noise
			float noise0(float3 x)
			{
				float3 p = floor(x);
				float3 f = frac(x);

				f = f * f*(float3(3.0f, 3.0f, 3.0f) - float3(2.0f, 2.0f, 2.0f) * f);
				float n = p.x + p.y*57.0f + 113.0f*p.z;
				return lerp(
					lerp(
						lerp(hash(n + 0.0f), hash(n + 1.0f), f.x),
						lerp(hash(n + 57.0f), hash(n + 58.0f), f.x),
						f.y),
					lerp(
						lerp(hash(n + 113.0f), hash(n + 114.0f), f.x),
						lerp(hash(n + 170.0f), hash(n + 171.0f), f.x),
						f.y),
					f.z);
			}
			float Cells(float3 p, float cellCount)
			{
				float3 pCell = p;
				float d = 1.0e10;
				for (int xo = -1; xo <= 1; xo++)
				{
					for (int yo = -1; yo <= 1; yo++)
					{
						for (int zo = -1; zo <= 1; zo++)
						{
							float3 tp = floor(pCell) + float3(xo, yo, zo);

							tp = pCell - tp - noise0(fmod(tp, cellCount / 1));

							d = min(d, dot(tp, tp));
						}
					}
				}
				d = min(d, 1.0f);
				d = max(d, 0.0f);
				return sqrt(d);
			}
#ifdef _USEFBM
			float noise(float3 uvw) {
				return Cells(uvw*_Scale, _Scale).r;
			}
#endif
#include "NoiseLib.cginc"
			float _C0;
			fixed4 frag(v2f_customrendertexture i) : SV_Target
			{
				i.globalTexcoord.xy += max(0.1,_C0);
#ifdef _USEFBM

				fixed4 col = useFbm3D(i.globalTexcoord.xyz);
#else

				i.globalTexcoord.xyz *= _Scale;
				fixed4 col = Cells((i.globalTexcoord.xyz), _Scale).r;
#endif
			return col;
			}
			ENDCG
		}
	}
}
