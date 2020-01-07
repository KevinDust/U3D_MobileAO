Shader "ChillyRoom/Noise/PerlinNoise3D_ZLoop"
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
			float _G4D[128];
			float _Scale;

			uint Clamp32(float v) {
				return ((uint)v % 32);// (int)clamp(v, 0.0, 255.0);
			}

			uint Clamp256(float v) {
				return ((uint)v % 256);// (int)clamp(v, 0.0, 255.0);
			}
			
			float4 grad(float index) {
				float gx = _G4D[Clamp32(index) * 4];
				float gy = _G4D[Clamp32(index) * 4 + 1];
				float gz = _G4D[Clamp32(index) * 4 + 2];
				float gw = _G4D[Clamp32(index) * 4 + 3];

				return float4(gx, gy, gz, gw);
			}

			float fade(float t) {
				return t * t*t*(t*(t*6.0 - 15.0) + 10.0);
			}

			float4 perlin4D(float4 uv) {
				uint xi = floor(uv.x);
				uint yi = floor(uv.y);
				uint zi = floor(uv.z);
				uint wi = floor(uv.w);

				//小数部分
				float4 uvf = uv -float4(xi, yi,zi,wi);
				//计算附近16个点的梯度向量索引
				float aaaa = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi)] + zi)] + wi)];
				float baaa = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi+1] + yi)] + zi)] + wi)];
				float abaa = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi+1)] + zi)] + wi)];
				float bbaa = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi+1] + yi+1)] + zi)] + wi)];
				
				float aaba = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi)] + zi+1)] + wi)];
				float baba = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi+1] + yi)] + zi+1)] + wi)];
				float abba = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi+1)] + zi+1)] + wi)];
				float bbba = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi+1] + yi+1)] + zi+1)] + wi)];

				float aaab = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi)] + zi)] + wi + 1)];
				float baab = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi + 1] + yi)] + zi)] + wi + 1)];
				float abab = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi + 1)] + zi)] + wi + 1)];
				float bbab = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi + 1] + yi + 1)] + zi)] + wi + 1)];

				float aabb = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi)] + zi + 1)] + wi + 1)];
				float babb = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi + 1] + yi)] + zi + 1)] + wi + 1)];
				float abbb = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi + 1)] + zi + 1)] + wi + 1)];
				float bbbb = _PERM[Clamp256(_PERM[Clamp256(_PERM[Clamp256(_PERM[xi + 1] + yi + 1)] + zi + 1)] + wi + 1)];



				float u0 = lerp(dot(grad(aaaa), uvf/*-float4(0,0,0,0)*/), dot(grad(baaa), uvf - float4(1, 0, 0, 0)), fade(uvf.x));
				float u1 = lerp(dot(grad(abaa), uvf - float4(0, 1, 0, 0)), dot(grad(bbaa), uvf - float4(1, 1, 0, 0)), fade(uvf.x));
				float v0 = lerp(u0, u1, fade(uvf.y));

				float u2 = lerp(dot(grad(aaba), uvf - float4(0, 0, 1, 0)), dot(grad(baba), uvf - float4(1, 0, 1, 0)), fade(uvf.x));
				float u3 = lerp(dot(grad(abba), uvf - float4(0, 1, 1, 0)), dot(grad(bbba), uvf - float4(1, 1, 1, 0)), fade(uvf.x));
				float v1 = lerp(u2, u3, fade(uvf.y));
				float w0 = lerp(v0, v1, fade(uvf.z));

				float u4 = lerp(dot(grad(aaab), uvf - float4(0, 0, 0, 1)), dot(grad(baab), uvf - float4(1, 0, 0, 1)), fade(uvf.x));
				float u5 = lerp(dot(grad(abab), uvf - float4(0, 1, 0, 1)), dot(grad(bbab), uvf - float4(1, 1, 0, 1)), fade(uvf.x));
				float v2 = lerp(u4, u5, fade(uvf.y));

				float u6 = lerp(dot(grad(aabb), uvf - float4(0, 0, 1, 1)), dot(grad(babb), uvf - float4(1, 0, 1, 1)), fade(uvf.x));
				float u7 = lerp(dot(grad(abbb), uvf - float4(0, 1, 1, 1)), dot(grad(bbbb), uvf - float4(1, 1, 1, 1)), fade(uvf.x));
				float v3 = lerp(u6, u7, fade(uvf.y));
				float w1 = lerp(v2, v3, fade(uvf.z));

				return lerp(w0, w1, fade(uvf.w))*0.5+0.5;

			}


			fixed4 perlin3D_ZLoop(float3 uvw)
			{
				//圆心坐标(x1,y1),起始坐标(x2,y2) 
				float x1 = 1.0f, x2 = 7.0f;
				float y1 = 1.0f, y2 = 7.0f;
				float dx = x2 - x1;
				float dy = y2 - y1;

				float x = uvw.x*_Scale;
				float y = uvw.y*_Scale;
				float z = uvw.z;

				//dx / (2 * Math.PI) 圆的半径
				float nz = (float)(x1 + cos(z * 2 * UNITY_PI)  * dx / (2 * UNITY_PI));
				float nw = (float)(y1 + sin(z * 2 * UNITY_PI)  * dy / (2 * UNITY_PI));

				return perlin4D(float4(x, y, nz, nw));
			}

#ifdef _USEFBM
			float noise(float3 uvw) {
				return perlin3D_ZLoop(uvw*_Scale).r;
			}
#endif
#include "NoiseLib.cginc"
			fixed4 frag(v2f_customrendertexture i) : SV_Target
			{
#ifdef _USEFBM

				fixed4 col = useFbm3D(i.globalTexcoord.xyz);
#else
				fixed4 col = perlin3D_ZLoop(i.globalTexcoord.xyz);
#endif
			return col;
			}
			ENDCG
		}
	}
}
