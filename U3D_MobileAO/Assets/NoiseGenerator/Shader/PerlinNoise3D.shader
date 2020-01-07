Shader "ChillyRoom/Noise/PerlinNoise3D"
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
			float _G3D[48];
			float _Scale;

			uint Clamp16(float v) {
				return ((uint)v % 16);// (int)clamp(v, 0.0, 255.0);
			}

			uint Clamp256(float v) {
				return ((uint)v % 256);// (int)clamp(v, 0.0, 255.0);
			}
			
			float3 grad(float index) {
				float gx = _G3D[Clamp16(index) * 3];  
				float gy = _G3D[Clamp16(index) * 3 + 1];
				float gz = _G3D[Clamp16(index) * 3 + 2];

				return float3(gx, gy,gz);
			}

			float fade(float t) {
				return t * t*t*(t*(t*6.0 - 15.0) + 10.0);
			}

			uint P(uint x) {
				return Clamp256(_PERM[x]);
			}


			float interpolateX(float3 uvf, uint xi, uint yi, uint zi,
				uint x0, uint y0, uint z0) {
				float index0 = P(P(P(xi + x0) + yi+y0) + zi+z0);
				float index1 = P(P(P(xi + x0 + 1) + yi + y0) + zi + z0);


				return lerp(dot(grad(index0), uvf - float3(x0, y0, z0)),
							dot(grad(index1), uvf - float3(x0+1, y0, z0)),
					fade(uvf.x));
			}

			float interpolateY(float3 uvf, uint xi, uint yi, uint zi,
				uint x0, uint y0, uint z0) {

				return lerp(
					interpolateX(uvf, xi, yi, zi, x0, y0, z0),
					interpolateX(uvf, xi, yi, zi, x0, y0+1, z0)
					, fade(uvf.y));
			}
			float interpolateZ(float3 uvf, uint xi, uint yi, uint zi,
				uint x0, uint y0, uint z0) {

				return lerp(
					interpolateY(uvf, xi, yi, zi, x0, y0, z0),
					interpolateY(uvf, xi, yi, zi, x0, y0, z0+1)
					, fade(uvf.z));
			}

			float4 perlin3D(float3 uv) {
				uint xi = floor(uv.x);
				uint yi = floor(uv.y);
				uint zi = floor(uv.z);

				//小数部分
				float3 uvf = uv -float3(xi, yi, zi);
				float r = interpolateZ(uvf, xi, yi, zi,0, 0, 0);

				return r * 0.5 + 0.5;
				
				//计算附近8个点的梯度向量索引
				float aaa = _PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi)]+zi)];		//x,y,z + 0,0,0
				float baa = _PERM[Clamp256(_PERM[Clamp256(_PERM[xi+1] + yi)]+zi)];		//x,y,z + 1,0,0

				float aba = _PERM[Clamp256(_PERM[Clamp256(_PERM[xi+0] + yi+1)]+zi)];	//x,y,z + 0,1,0
				float bba = _PERM[Clamp256(_PERM[Clamp256(_PERM[xi+1] + yi+1)]+zi)];	//x,y,z + 1,1,0

				float aab = _PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi)]+zi+1)];		//x,y,z + 0,0,1
				float bab = _PERM[Clamp256(_PERM[Clamp256(_PERM[xi+1] + yi)]+zi+1)];	//x,y,z + 1,0,1

				float abb = _PERM[Clamp256(_PERM[Clamp256(_PERM[xi] + yi+1)]+zi+1)];	//x,y,z + 0,1,1
				float bbb = _PERM[Clamp256(_PERM[Clamp256(_PERM[xi+1] + yi+1)]+zi+1)];	//x,y,z + 1,1,1

				float u0 = lerp(dot(grad(aaa), uvf/*-float3(0,0,0)*/), dot(grad(baa), uvf - float3(1, 0, 0)), fade(uvf.x));
				float u1 = lerp(dot(grad(aba), uvf - float3(0, 1, 0)), dot(grad(bba), uvf - float3(1, 1, 0)), fade(uvf.x));
				float v0 = lerp(u0, u1, fade(uvf.y));

				float u2 = lerp(dot(grad(aab), uvf - float3(0, 0, 1)), dot(grad(bab), uvf - float3(1, 0, 1)), fade(uvf.x));
				float u3 = lerp(dot(grad(abb), uvf - float3(0, 1, 1)), dot(grad(bbb), uvf - float3(1, 1, 1)), fade(uvf.x));
				float v1 = lerp(u2, u3, fade(uvf.y));
				return lerp(v0, v1, fade(uvf.z))*0.5 + 0.5;
			}

#ifdef _USEFBM
			float noise(float3 uvw) {
				return perlin3D(uvw*_Scale).r;
			}
#endif
#include "NoiseLib.cginc"
			fixed4 frag(v2f_customrendertexture i) : SV_Target
			{
#ifdef _USEFBM

				fixed4 col = useFbm3D(i.globalTexcoord.xyz);
#else
				fixed4 col = perlin3D(i.globalTexcoord.xyz*_Scale);
#endif
			return col;
			}
			ENDCG
		}
	}
}
