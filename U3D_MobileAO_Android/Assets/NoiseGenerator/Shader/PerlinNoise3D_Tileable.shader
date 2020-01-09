Shader "ChillyRoom/Noise/PerlinNoise3D_Tileable"
{
	Properties
	{
		_Scale("_Scale", Range(0,255)) = 1.0
		_Octaves("_Octaves", Int) = 1.0
		_Lacunarity("_Lacunarity", Float) = 1.0
		_Gain("_Gain", Float) = 1.0
		_Amplitude0("_Amplitude0", Float) = 1.0
		_Frequency0("_Frequency0", Float) = 1.0

		_C0("_C0", Float) = 1.0
		_C1("_C1", Float) = 1.0

		_R("R", Float) = 1.0
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
			

		struct float6 {
		float x;
		float y;
		float z;
		float u;
		float v;
		float w;
	};
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _PERM[256];
			float _G6D_0[768];
			float _G6D_1[768];
			float _Scale;

			uint Clamp128(float v) {
				return ((uint)v % 128);// (int)clamp(v, 0.0, 255.0);
			}

			uint Clamp256(float v) {
				return ((uint)v % 256);// (int)clamp(v, 0.0, 255.0);
			}

			float6 grad(float index) {
				float6 result;
				result.x = Clamp256(index)<128 ? _G6D_0[Clamp128(index) * 6 + 0] : _G6D_1[Clamp128(index) * 6 + 0];
				result.y = Clamp256(index)<128 ? _G6D_0[Clamp128(index) * 6 + 1] : _G6D_1[Clamp128(index) * 6 + 1];
				result.z = Clamp256(index)<128 ? _G6D_0[Clamp128(index) * 6 + 2] : _G6D_1[Clamp128(index) * 6 + 2];
				result.u = Clamp256(index)<128 ? _G6D_0[Clamp128(index) * 6 + 3] : _G6D_1[Clamp128(index) * 6 + 3];
				result.v = Clamp256(index)<128 ? _G6D_0[Clamp128(index) * 6 + 4] : _G6D_1[Clamp128(index) * 6 + 4];
				result.w = Clamp256(index)<128 ? _G6D_0[Clamp128(index) * 6 + 5] : _G6D_1[Clamp128(index) * 6 + 5];

				return result;
			}

			float fade(float t) {
				return t * t*t*(t*(t*6.0 - 15.0) + 10.0);
			}

			float6 minus6(float6 val, float x, float y, float z, float u, float v, float w) {
				val.x -= x;
				val.y -= y;
				val.z -= z;
				val.u -= u;
				val.v -= v;
				val.w -= w;
				return val;
			}

			float dot6(float6 val, float6 val2) {
				return val.x*val2.x + val.y*val2.y + val.z*val2.z + val.u*val2.u + val.v*val2.v + val.w*val2.w;
			}

			uint P(uint x) {
				return Clamp256(_PERM[x]);
			}

			#define HASHSCALE1 .1031
			#define HASHSCALE3 float3(.1031, .1030, .0973)
			#define HASHSCALE4 float4(.1031, .1030, .0973, .1099)
			float3 hash33(float3 p3)
			{
				p3 = frac(p3 * HASHSCALE3);
				p3 += dot(p3, p3.yxz + 19.19);
				return frac((p3.xxy + p3.yxx)*p3.zyx);

			}

			float hash61(float6 p6)  // replace this by something better
			{
				float6 p;
				p.x = frac(p6.x*0.3183099 + .1)*17.0;
				p.y = frac(p6.y*0.3183099 + .1)*17.0;
				p.z = frac(p6.z*0.3183099 + .1)*17.0;
				p.u = frac(p6.u*0.3183099 + .1)*17.0;
				p.v = frac(p6.v*0.3183099 + .1)*17.0;
				p.w = frac(p6.w*0.3183099 + .1)*17.0;

				return frac(p.x*p.y*p.z*p.u*p.v*p.w*(p.x + p.y + p.z + p.u + p.v + p.w));
			}

			float6 hash66(float6 p6) {
				float6 r;
				float3 xyz = hash33(float3(p6.x, p6.y, p6.z));
				float3 uvw = hash33(float3(p6.u, p6.v, p6.w));
				r.x = xyz.x;
				r.y = xyz.y;
				r.z = xyz.z;

				r.u = uvw.x;
				r.v = uvw.y;
				r.w = uvw.z;
				return r;
			}


			float interpolateX0(float6 uvf, uint xi, uint yi, uint zi, uint ui, uint vi, uint wi,
				uint x0, uint y0, uint z0, uint u0, uint v0, uint w0) {
				//float index0 = P(P(P(P(P(P(xi + x0) + yi + y0) + zi+z0) + ui+u0) + vi+v0) + wi+w0);
				//float index1 = P(P(P(P(P(P(xi + x0+1) + yi + y0) + zi + z0) + ui + u0) + vi + v0) + wi + w0);

				float6 index0;
				index0.x = xi + x0;
				index0.y = yi + y0;
				index0.z = zi + z0;
				index0.u = ui + u0;
				index0.v = vi + v0;
				index0.w = wi + w0;
				float6 index1;
				index1.x = xi + x0 + 1;
				index1.y = yi + y0;
				index1.z = zi + z0;
				index1.u = ui + u0;
				index1.v = vi + v0;
				index1.w = wi + w0;

				return lerp(hash61(index0),
					hash61(index1), fade(uvf.x));
			}

			float interpolateX(float6 uvf, uint xi, uint yi, uint zi, uint ui, uint vi, uint wi,
				uint x0, uint y0, uint z0, uint u0, uint v0, uint w0) {
				float index0 = P(P(P(P(P(P(xi + x0) + yi + y0) + zi+z0) + ui+u0) + vi+v0) + wi+w0);
				float index1 = P(P(P(P(P(P(xi + x0+1) + yi + y0) + zi + z0) + ui + u0) + vi + v0) + wi + w0);

				return lerp(dot6(grad(index0), minus6(uvf, x0  , y0, z0, u0, v0, w0)),
							dot6(grad(index1), minus6(uvf, x0+1, y0, z0, u0, v0, w0)), fade(uvf.x));
			}

			float interpolateY(float6 uvf, uint xi, uint yi, uint zi, uint ui, uint vi, uint wi,
				uint x0, uint y0, uint z0, uint u0, uint v0, uint w0) {

				return lerp(
					interpolateX(uvf, xi, yi, zi, ui, vi, wi, x0, y0  , z0, u0, v0, w0),
					interpolateX(uvf, xi, yi, zi, ui, vi, wi, x0, y0+1, z0, u0, v0, w0)
					, fade(uvf.y));
			}
			float interpolateZ(float6 uvf, uint xi, uint yi, uint zi, uint ui, uint vi, uint wi,
				uint x0, uint y0, uint z0, uint u0, uint v0, uint w0) {

				return lerp(
					interpolateY(uvf, xi, yi, zi, ui, vi, wi, x0, y0, z0  , u0, v0, w0),
					interpolateY(uvf, xi, yi, zi, ui, vi, wi, x0, y0, z0+1, u0, v0, w0)
					, fade(uvf.z));
			}

			float interpolateU(float6 uvf, uint xi, uint yi, uint zi, uint ui, uint vi, uint wi,
				uint x0, uint y0, uint z0, uint u0, uint v0, uint w0) {

				return lerp(
					interpolateZ(uvf, xi, yi, zi, ui, vi, wi, x0, y0, z0, u0  , v0, w0),
					interpolateZ(uvf, xi, yi, zi, ui, vi, wi, x0, y0, z0, u0+1, v0, w0)
					, fade(uvf.u));
			}

			float interpolateV(float6 uvf, uint xi, uint yi, uint zi, uint ui, uint vi, uint wi,
				uint x0, uint y0, uint z0, uint u0, uint v0, uint w0) {

				return lerp(
					interpolateU(uvf, xi, yi, zi, ui, vi, wi, x0, y0, z0, u0, v0    , w0),
					interpolateU(uvf, xi, yi, zi, ui, vi, wi, x0, y0, z0, u0, v0 + 1, w0)
					, fade(uvf.v));
			}


			float interpolateW(float6 uvf, uint xi, uint yi, uint zi, uint ui, uint vi, uint wi,
				uint x0, uint y0, uint z0, uint u0, uint v0, uint w0) {

				return lerp(
					interpolateV(uvf, xi, yi, zi, ui, vi, wi, x0, y0, z0, u0, v0, w0  ),
					interpolateV(uvf, xi, yi, zi, ui, vi, wi, x0, y0, z0, u0, v0, w0+1)
					, fade(uvf.w));
			}


			float4 perlin6D(float6 uv) {
				uint xi = floor(uv.x);
				uint yi = floor(uv.y);
				uint zi = floor(uv.z);
				uint ui = floor(uv.u);
				uint vi = floor(uv.v);
				uint wi = floor(uv.w);

				//小数部分
				float6 uvf;
				uvf.x = uv.x - xi;
				uvf.y = uv.y - yi;
				uvf.z = uv.z - zi;
				uvf.u = uv.u - ui;
				uvf.v = uv.v - vi;
				uvf.w = uv.w - wi;


				float r = interpolateW(uvf, xi, yi, zi, ui, vi, wi, 0, 0, 0, 0, 0, 0);

				return r *0.5+0.5;

			}

			float _C0;
			float _C1;

			float _R;


			fixed4 perlin3D_Tile(float3 uvw)
			{
				//圆心坐标(x1,y1),起始坐标(x2,y2) 
				float r = 2 * UNITY_PI;
				float x1 = 3, x2 = x1+r;
				float y1 = 3, y2 = y1+r;

				float x = uvw.x;
				float y = uvw.y;
				float z = uvw.z;
				float f = _Scale;
				//dx / (2 * Math.PI) 圆的半径
				float6 uv;
				uv.u = (float)(x1 + (sin(x * 2 * UNITY_PI))  * r / (2 * UNITY_PI))*f;
				uv.x = (float)(y1 + (cos(x * 2 * UNITY_PI))  * r / (2 * UNITY_PI))*f;
																					
				uv.v = (float)(x1 + (sin(y * 2 * UNITY_PI))  * r / (2 * UNITY_PI))*f;
				uv.y = (float)(y1 + (cos(y * 2 * UNITY_PI))  * r / (2 * UNITY_PI))*f;

				uv.w = (float)(x1 + (sin(z * 2 * UNITY_PI))  * r / (2 * UNITY_PI))*f;
				uv.z = (float)(y1 + (cos(z * 2 * UNITY_PI))  * r / (2 * UNITY_PI))*f;
				
				return perlin6D(uv);
			}

#ifdef _USEFBM
			float noise(float3 uvw) {
				return perlin3D_Tile(uvw).r;
			}
#endif
#include "NoiseLib.cginc"

			fixed4 frag(v2f_customrendertexture i) : SV_Target
			{
#ifdef _USEFBM

				fixed4 col = useFbm3D(i.globalTexcoord.xyz);
#else
				fixed4 col = perlin3D_Tile(i.globalTexcoord.xyz);
#endif
			return col;
			}
			ENDCG
		}
	}
}
