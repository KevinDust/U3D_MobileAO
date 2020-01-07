Shader "ChillyRoom/PoseEffect/SSAO"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
			
            sampler2D _MainTex;
			sampler2D _CameraDepthNormalsTexture;
			sampler2D _CameraDepthTexture;
			sampler2D _RandomVectorTex;
            
			int _SampleNum;
			float4 _VectorArray[100];
            
			float _Radius;	//
			float _NoiseScale;
			float _Bias;	//减少self shadowing
			float _MinDepth;
			float _Attenuation;	//衰减系数
			float _Intensity;	//衰减系数
            fixed4 frag (v2f i) : SV_Target
            {
               float4 col = tex2D(_MainTex, i.uv);
				//Ndc
				float4 depthnormal = tex2D(_CameraDepthNormalsTexture, i.uv);
				float3 viewNorm;
				float viewDepth;
				DecodeDepthNormal(depthnormal, viewDepth, viewNorm);
				//DepthNormal 保存的是 viewspace 的z，所以不用考虑reverseZ
				//#define COMPUTE_DEPTH_01 -(UnityObjectToViewPos( v.vertex ).z * _ProjectionParams.w)
				//_ProjectionParams.w = 1/Far
				viewDepth *= _ProjectionParams.z;		//camera的farplane

    
				half3 randN = tex2D(_RandomVectorTex, i.uv*_NoiseScale).xyz * 2.0 - 1.0;

				float ao = 0;
				for (int n = 0; n < _SampleNum; n++) {
					half3 randomDir = reflect(_VectorArray[n], randN);
					//保证半球上:解决self occlusion
					half flip = (dot(viewNorm, randomDir)<0) ? 0.0 : 1.0;
					randomDir += (viewNorm * _Bias);		//永远朝上
					//randomDir = normalize(randomDir);
                    //采样点的深度:viewSpace
					float sD = viewDepth - (randomDir.z * _Radius);

                    // 对应深度图的深度  
                  
                    //从世界坐标转到UV坐标
                    float2 offset = randomDir.xy * _Radius/ viewDepth; //depth就是 NDC的w
					float4 sampleND = tex2D(_CameraDepthNormalsTexture, i.uv + offset);
					float sampleD;
					float3 sampleN;
					DecodeDepthNormal(sampleND, sampleD, sampleN);
					sampleD *= _ProjectionParams.z;
					//找的是比深度图采样点更加远的点
					float diff = saturate(sD- sampleD);		    //viewSpace：越近越小
					if (diff > _MinDepth) {
						//衰减函数D：照搬unity算了
						ao += pow(1 - diff, _Attenuation)*flip; // sc2
						//ao += 1.0-saturate(pow(1.0 - diff, _Attenuation) + diff); // nullsq
						//ao += 1.0/(1.0+ diff * diff *_Attenuation); // iq
					}
				}
				ao /= _SampleNum;
				return (1-pow(ao, _Intensity));
            }
            ENDCG
        }
    }
}
