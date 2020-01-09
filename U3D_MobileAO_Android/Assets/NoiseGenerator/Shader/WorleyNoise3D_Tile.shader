Shader "ChillyRoom/Noise/WorleyNoise3D_Tile"
{
	Properties
	{
		_Scale("_Scale", Range(0,255)) = 1.0
		_Octaves("_Octaves", Int) = 1.0
		_Lacunarity("_Lacunarity", Float) = 1.0
		_Gain("_Gain", Float) = 1.0
		_Amplitude0("_Amplitude0", Float) = 1.0
		_Frequency0("_Frequency0", Float) = 1.0
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
			float _Scale;

			float3 hash0(float3 p)
			{
				p = float3(dot(p, float3(127.1, 311.7, 74.7)),
					dot(p, float3(269.5, 183.3, 246.1)),
					dot(p, float3(113.5, 271.9, 124.6)));

				return frac(sin(p)*43758.5453123);
			}

			float6 hash6(float6 p)
			{
				float6 result;
				float dotV = dot(float3(p.x, p.y, p.z), float3(127.1, 311.7, 74.7)) + dot(float3(p.u, p.v, p.w), float3(269.5, 183.3, 246.1));
				result.x = frac(sin(dotV)*43758.5453123);

				dotV = dot(float3(p.x, p.y, p.z), float3(113.4, 271.9, 124.6)) + dot(float3(p.u, p.v, p.w), float3(467.5, 647.3, 191.3));
				result.y = frac(sin(dotV)*43758.5453123);

				dotV = dot(float3(p.x, p.y, p.z), float3(229.1, 241.7, 251.7)) + dot(float3(p.u, p.v, p.w), float3(743.2, 680.9, 811.1));
				result.z = frac(sin(dotV)*43758.5453123);

				dotV = dot(float3(p.x, p.y, p.z), float3(953.1, 61.8, 73.2)) + dot(float3(p.u, p.v, p.w), float3(97.4, 157.3, 167.1));
				result.u = frac(sin(dotV)*43758.5453123);

				dotV = dot(float3(p.x, p.y, p.z), float3(401.2, 151.7, 74.7)) + dot(float3(p.u, p.v, p.w), float3(269.5, 838.3, 797.1));
				result.v = frac(sin(dotV)*43758.5453123);

				dotV = dot(float3(p.x, p.y, p.z), float3(127.1, 523.1, 74.7)) + dot(float3(p.u, p.v, p.w), float3(269.5, 373.3, 197.1));
				result.w = frac(sin(dotV)*43758.5453123);


				return result;
			}

			float dot6(float6 val, float6 val2) {
				return val.x*val2.x + val.y*val2.y + val.z*val2.z + val.u*val2.u + val.v*val2.v + val.w*val2.w;
			}

			float6 minus6(float6 val, float6 val2) {
				val.x -= val2.x;
				val.y -= val2.y;
				val.z -= val2.z;
				val.u -= val2.u;
				val.v -= val2.v;
				val.w -= val2.w;
				return val;
			}

			float6 Add6(float6 val, float6 val2) {
				val.x += val2.x;
				val.y += val2.y;
				val.z += val2.z;
				val.u += val2.u;
				val.v += val2.v;
				val.w += val2.w;
				return val;
			}

			float6 floor6(float6 val) {
				val.x = floor(val.x);
				val.y = floor(val.y);
				val.z = floor(val.z);
				val.u = floor(val.u);
				val.v = floor(val.v);
				val.w = floor(val.w);
				return val;
			}

			float6 frac6(float6 val) {
				val.x = frac(val.x);
				val.y = frac(val.y);
				val.z = frac(val.z);
				val.u = frac(val.u);
				val.v = frac(val.v);
				val.w = frac(val.w);
				return val;
			}

			float worley6D(float6 uv) {
	
				float6 i_uv = floor6(uv);
				float6 f_uv = frac6(uv);
				float m = 1000;
				for (int _w = -1; _w <= 1; _w++) {
					for (int _v = -1; _v <= 1; _v++) {
						for (int _u = -1; _u <= 1; _u++){
							for (int _z = -1; _z <= 1; _z++) {
								for (int _y = -1; _y <= 1; _y++) {
									for (int _x = -1; _x <= 1; _x++)
									{
										float6 neighbor;
										neighbor.x = float(_x);
										neighbor.y = float(_y); 
										neighbor.z = float(_z); 
										neighbor.u = float(_u);
										neighbor.v = float(_v); 
										neighbor.w = float(_w);
										float6 p = Add6(neighbor, hash6(Add6(neighbor, i_uv)));	//0~1
										float distance = dot6(minus6(p, f_uv), minus6(p, f_uv));
										m = distance < m ? distance : m;
									}
								}
							}
						}
					}
				}
				return m.x;
			}

			fixed4 worley3D_Tile(float3 uvw)
			{
				//圆心坐标(x1,y1),起始坐标(x2,y2) 
				float r = 2 * UNITY_PI;
				float x1 = 5, x2 = x1 + r;
				float y1 = 1, y2 = y1 + r;

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

				return worley6D(uv);
			}

#ifdef _USEFBM
			float noise(float3 uvw) {
				return worley3D_Tile(uvw).r;
			}
#endif
#include "NoiseLib.cginc"
			fixed4 frag(v2f_customrendertexture i) : SV_Target
			{
#ifdef _USEFBM

				fixed4 col = useFbm3D(i.globalTexcoord.xyz);
#else
				fixed4 col = worley3D_Tile(i.globalTexcoord.xyz);
#endif
			return col;
			}
			ENDCG
		}
	}
}
