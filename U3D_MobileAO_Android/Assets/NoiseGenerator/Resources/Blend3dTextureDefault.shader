Shader "ChillyRoom/Noise/Blend/3DDefault"
{
	Properties
	{
		_MainTex0("MainTex",3D) = "white" {}
	}
	SubShader
	{
		LOD 100

		Pass
		{
			Name "Update"
			CGPROGRAM
			#pragma vertex CustomRenderTextureVertexShader2
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "UnityCustomRenderTexture.cginc"
			sampler3D _MainTex0;
			float4 _MainTex_ST;
			struct v2f_customrendertexture2
			{
				float4 vertex           : SV_POSITION;
				float3 localTexcoord    : TEXCOORD0;    // Texcoord local to the update zone (== globalTexcoord if no partial update zone is specified)
				float3 globalTexcoord   : TEXCOORD1;    // Texcoord relative to the complete custom texture
				float3 direction        : TEXCOORD3;    // For cube textures, direction of the pixel being rendered in the cubemap
			};
			v2f_customrendertexture2 CustomRenderTextureVertexShader2(appdata_customrendertexture IN)
			{
				v2f_customrendertexture2 OUT;

#if UNITY_UV_STARTS_AT_TOP
				const float2 vertexPositions[6] =
				{
					{ -1.0f,  1.0f },
				{ -1.0f, -1.0f },
				{ 1.0f, -1.0f },
				{ 1.0f,  1.0f },
				{ -1.0f,  1.0f },
				{ 1.0f, -1.0f }
				};

				const float2 texCoords[6] =
				{
					{ 0.0f, 0.0f },
				{ 0.0f, 1.0f },
				{ 1.0f, 1.0f },
				{ 1.0f, 0.0f },
				{ 0.0f, 0.0f },
				{ 1.0f, 1.0f }
				};
#else
				const float2 vertexPositions[6] =
				{
					{ 1.0f,  1.0f },
				{ -1.0f, -1.0f },
				{ -1.0f,  1.0f },
				{ -1.0f, -1.0f },
				{ 1.0f,  1.0f },
				{ 1.0f, -1.0f }
				};

				const float2 texCoords[6] =
				{
					{ 1.0f, 1.0f },
				{ 0.0f, 0.0f },
				{ 0.0f, 1.0f },
				{ 0.0f, 0.0f },
				{ 1.0f, 1.0f },
				{ 1.0f, 0.0f }
				};
#endif

				uint primitiveID = IN.vertexID / 6;
				uint vertexID = IN.vertexID % 6;
				float3 updateZoneCenter = CustomRenderTextureCenters[primitiveID].xyz;
				float3 updateZoneSize = CustomRenderTextureSizesAndRotations[primitiveID].xyz;
				float rotation = CustomRenderTextureSizesAndRotations[primitiveID].w * UNITY_PI / 180.0f;

#if !UNITY_UV_STARTS_AT_TOP
				rotation = -rotation;
#endif

				// Normalize rect if needed
				if (CustomRenderTextureUpdateSpace > 0.0) // Pixel space
				{
					// Normalize xy because we need it in clip space.
					updateZoneCenter.xy /= _CustomRenderTextureInfo.xy;
					updateZoneSize.xy /= _CustomRenderTextureInfo.xy;
				}
				else // normalized space
				{
					// Un-normalize depth because we need actual slice index for culling
					updateZoneCenter.z *= _CustomRenderTextureInfo.z;
					updateZoneSize.z *= _CustomRenderTextureInfo.z;
				}

				// Compute rotation

				// Compute quad vertex position
				float2 clipSpaceCenter = updateZoneCenter.xy * 2.0 - 1.0;
				float2 pos = vertexPositions[vertexID] * updateZoneSize.xy;
				pos = CustomRenderTextureRotate2D(pos, rotation);
				pos.x += clipSpaceCenter.x;
#if UNITY_UV_STARTS_AT_TOP
				pos.y += clipSpaceCenter.y;
#else
				pos.y -= clipSpaceCenter.y;
#endif

				// For 3D texture, cull quads outside of the update zone
				// This is neeeded in additional to the preliminary minSlice/maxSlice done on the CPU because update zones can be disjointed.
				// ie: slices [1..5] and [10..15] for two differents zones so we need to cull out slices 0 and [6..9]
				if (CustomRenderTextureIs3D > 0.0)
				{
					int minSlice = (int)(updateZoneCenter.z - updateZoneSize.z * 0.5);
					int maxSlice = minSlice + (int)updateZoneSize.z;
					if (_CustomRenderTexture3DSlice < minSlice || _CustomRenderTexture3DSlice >= maxSlice)
					{
						pos.xy = float2(1000.0, 1000.0); // Vertex outside of ncs
					}
				}

				OUT.vertex = float4(pos, 0.0, 1.0);
				OUT.localTexcoord = float3(texCoords[vertexID], CustomRenderTexture3DTexcoordW);
				OUT.globalTexcoord = float3(pos.xy * 0.5 + 0.5, CustomRenderTexture3DTexcoordW);
#if UNITY_UV_STARTS_AT_TOP
				OUT.globalTexcoord.y = 1.0 - OUT.globalTexcoord.y;
#endif
				OUT.direction = CustomRenderTextureComputeCubeDirection(OUT.globalTexcoord.xy);

				return OUT;
			}


			fixed4 frag(v2f_customrendertexture i) : SV_Target
			{
				fixed4 col = tex3D(_MainTex0,i.globalTexcoord.xyz);
				return col;
			}
			ENDCG
		}
	}
}
