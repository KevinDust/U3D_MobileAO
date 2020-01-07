Shader "ChillyRoom/Noise/PerlinNoise2D"
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
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _PERM[256];
			float _G[16];
			float _Scale;

			uint Clamp8(float v) {
				return ((uint)v % 8);// (int)clamp(v, 0.0, 255.0);
			}

			uint Clamp256(float v) {
				return ((uint)v % 256);// (int)clamp(v, 0.0, 255.0);
			}
//#define G_ARRAY
#ifdef G_ARRAY
			float2 grad(float index) {
				float gx = _G[Clamp8(index)*2];  
				float gy = _G[Clamp8(index) * 2 + 1];
				return float2(gx, gy);
			}
#else
			float2 grad(float index) {
				float gx = cos(0.785398163f * (index));  
				float gy = sin(0.785398163f * (index));
				return normalize(float2(gx, gy));
			}
#endif
			float fade(float t) {
				return t * t*t*(t*(t*6.0 - 15.0) + 10.0);
			}

			float4 perlin2D(float2 uv) {
				uint xi = floor(uv.x);
				uint yi = floor(uv.y);
				//小数部分
				float2 uvf = uv -float2(xi, yi);
				//return float4(fade(uvf.x), fade(uvf.y),0,1);

				//计算附近4个点的梯度向量索引
				float aa = _PERM[Clamp256(_PERM[xi] + yi)];	//x,y + 0,0
				float ab = _PERM[Clamp256(_PERM[xi] + yi+1)];	//x,y + 0,1
				float ba = _PERM[Clamp256(_PERM[xi+1] + yi)];	//x,y + 1,0
				float bb = _PERM[Clamp256(_PERM[xi+1] + yi+1)];	//x,y + 1,1
				float u0 = lerp(dot(grad(aa), uvf/*-float2(0,0)*/), dot(grad(ba), uvf - float2(1, 0)), fade(uvf.x));
				float u1 = lerp(dot(grad(ab), uvf - float2(0, 1) ), dot(grad(bb), uvf - float2(1, 1)), fade(uvf.x));
				return lerp(u0, u1, fade(uvf.y))*0.5 + 0.5;
			}


#ifdef _USEFBM
			float noise(float2 uv) {
				return perlin2D(uv*_Scale).r;
			}
#endif
#include "NoiseLib.cginc"

			fixed4 frag (v2f_customrendertexture i) : SV_Target
			{
#ifdef _USEFBM

				fixed4 col = useFbm2D(i.globalTexcoord.xy);
#else
				fixed4 col = perlin2D(i.globalTexcoord.xy*_Scale);
#endif
				return col;
			}
			ENDCG
		}
	}
}
