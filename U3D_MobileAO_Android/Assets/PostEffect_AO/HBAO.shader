Shader "ChillyRoom/PoseEffect/HBAO"
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
			#pragma shader_feature _FindHorizonal
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
            
			sampler2D _NoiseTex;
			float _NoiseTexSize;

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _CameraDepthTexture;
            sampler2D _CameraDepthNormalsTexture;
            sampler2D _RandomVectorTex;
            float4x4 Matrix_I_P;
            float4x4 Matrix_P;
            int _ArrayNum;
            float4 _SampleDirArray[50];
			float3 _ViewLightDir;

            int _StepNum;
			int _SampleDirNum;
			float _StepRadius;
			float _Radius;
            float _NoiseScale;
			float _Bias2;
            float _Bias;    //减少self shadowing
            float _MinStepPixelNum;
            float _Attenuation; //衰减系数
            float _Intensity;   //强度系数
            float2 _DeltaUV;
			float4 _AOColor;

            float2 snapUVToCenter(float2 uv){
				return uv;
				float2 screenUV = floor(uv *float2(_ScreenParams.x, _ScreenParams.y)) +float2(0.5, 0.5);
                return screenUV * float2(1/_ScreenParams.x, 1/_ScreenParams.y);
            }
            
            

            float weight(float diff){
                return saturate(1.0-pow(diff, _Attenuation));	//diff 过大 会直接变0
            }

            float3 depthToView(float viewD, float2 uv) {
                float4 projPos = mul(Matrix_P, float4(1, 1, -viewD, 1));    //unity的viewspace z是反的
                float4 temp = float4(uv.x * 2 - 1, uv.y * 2 - 1, projPos.z, 1) * projPos.w;
                float4 viewPos = mul(Matrix_I_P, temp);
                return float3(viewPos.xyz);

            }
            
            
            float3 depthBufferToView(float d, float2 uv) {
                float4 temp = float4(uv.x * 2 - 1, uv.y * 2 - 1, d, 1);
                float4 viewPos = mul(Matrix_I_P, temp);
                return viewPos.xyz/viewPos.w;

            }
            
            float3 GetViewByUV2(float2 uv) {
                float depthBuffer = tex2D(_CameraDepthTexture,uv).r;
                float3 viewPos = depthBufferToView(depthBuffer,uv);
                return viewPos;
            }
            float GetDepth(float2 uv){
                float depthBuffer = tex2D(_CameraDepthTexture,uv).r;
                float dEye = LinearEyeDepth(depthBuffer);
                return dEye;
                    
                float4 depthnormal = tex2D(_CameraDepthNormalsTexture, uv);
                float3 viewNorm;
                float viewDepth;
                DecodeDepthNormal(depthnormal, viewDepth, viewNorm);
                return viewDepth * _ProjectionParams.z;       //camera的farplane
            }
            
            float3 GetViewByUV(float2 uv) {
                float4 depthnormal = tex2D(_CameraDepthNormalsTexture, uv);
                float3 viewNorm;
                float viewDepth;
                DecodeDepthNormal(depthnormal, viewDepth, viewNorm);
                viewDepth *= _ProjectionParams.z;       
                float3 viewPos = depthToView(viewDepth, uv);
                return viewPos;
            }

            float3 MinDiff(float3 P, float3 Pr, float3 Pl)
            {
                float3 V1 = Pr - P;
                float3 V2 = P - Pl;
                return length(V1) < length(V2) ? V1 : V2;	//偏向短的一边，这样可以消除深度大造成的描边
                //return (V1+V2)/2;
            }
            
           
            
            
            //自己构造ddx,ddy
            float3 self_DDX(float3 pos,float2 uv){
                float2 screenDelta = float2(1.0f/_ScreenParams.x, 1.0f / _ScreenParams.y);
                float3 pos_r = GetViewByUV2(uv + float2(screenDelta.x, 0));
                return pos_r - pos;
            }
            
            float3 self_DDY(float3 pos,float2 uv){
                float2 screenDelta = float2(1.0f/_ScreenParams.x, 1.0f / _ScreenParams.y);
                float3 pos_b = GetViewByUV2(uv + float2(0,-screenDelta.y));
                return (pos - pos_b)* (_ScreenParams.y * screenDelta.x);
            }


            
			float hash(float2 p)
			{

				return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
				// 下面这个更好，但可能在mac机子上会有些问题。
				//return fract(sin(dot(p, vec2(1.0,113.0)))*43758.5453123);
			}

			//积分
            float raymarchAO_Integral(float angle, float2 uv, float3 random, float3 dPdu,float3 dpdv,float viewDepth,float3 viewPos){
                float ao = 0;
				//view space
				float cosA, sinA;
				sincos(angle, sinA, cosA);
				//samplerDir = (cos(A+random),sin(A+random))
				float2 sampleDir = float2(cosA * random.x - sinA * random.y, cosA * random.y + sinA * random.x);
				float2 sampleDir_V = normalize(sampleDir* _ViewLightDir.xy);

                //(float4(sampleDir_V0.xy,viewDepth,1)-float4(sampleDir_V1.xy,viewDepth,1)) * 2Near/(r-l) = (float4(uvoffset0*viewDepth,?,viewDepth) - float4(uvoffset1*viewDepth,?,viewDepth));
                //sampleDir_V * Radius * 2Near/(r-l)【常量】 = uvOffset * viewDepth; 
                //sampleDir_V * _StepRadius = uvOffset * viewDepth;
                
                //根据radius 计算 step size；
				float2 stepSizeView = sampleDir_V * _StepRadius / _StepNum;
				
				//viewSpace
				float4 stepSizeH = mul(Matrix_P, float4(stepSizeView.x, stepSizeView.y, -viewDepth, 1));	//viewspace的depth是反的
				float2 stepSize = stepSizeH.xy / stepSizeH.w * 1;	//-1,1的方向 正好是我们需要的
				float2 minStepSize = float2(1.0f / _ScreenParams.x, 1.0f / _ScreenParams.y)*_MinStepPixelNum;
				stepSize = normalize(stepSize) *max(minStepSize, abs(stepSize));	//最少跨距一个像素
						
				float2 stepSizeNV = float2(stepSize.x*Matrix_I_P[0][0], stepSize.y*Matrix_I_P[1][1]);
				float3 T = normalize(stepSizeNV.x * dPdu + stepSizeNV.y * dpdv);// +_Bias * vnormal;


				////imageSpace
				//float2 stepSizeImage = sampleDir_V;
				//stepSize = stepSizeImage * _Radius / _StepNum;
				//stepSize = normalize(stepSize) *max(minStepSize, abs(stepSize));	//最少跨距一个像素

				//float2 stepSizeV = float2(stepSize.x*Matrix_I_P[0][0], stepSize.y*Matrix_I_P[1][1]);
				//
				//T = normalize(stepSizeV.x * dPdu + stepSizeV.y * dpdv);

                float sinT = T.z + _Bias * viewDepth;
				float lastSinH = sinT;
                //raymarch
                for(int s =1;s<=_StepNum;s++){
                    //snapUV+ jitter                   
					float jitter = 0;//hash(uv*s);
					float2 uv_s = snapUVToCenter(uv+ stepSize*(s+ jitter));
                    float depth_Si = GetDepth(uv_s);
					float3 viewPos_Si = depthToView(depth_Si, uv_s);
                    //Phi = H - T
                    //cos(Phi)dPhi  = sinPHI0-sinPHI1 = sin(H0-T) - sin(H1-T) =sin(H0)-sin(T) - (sin(H1)-sin(T)) = sin(H0)-sin(H1)    //cos～=1
					//sin(Phi)dPhi  = -cosPHI0-(-cosPHI1) = cos(H1-T) - cos(H0-T) =[cos(H1)cos(T)+sin(H1)sin(T)] - [cos(H0)cos(T)+sin(H0)sin(T)]) = sin(H1)sin(T)-sin(H0)sin(T) 
					float3 H = (viewPos_Si - viewPos);
					float sinH = H.z / length(H);
					if (sinH > lastSinH) {
						//sin(H1)sin(T)-sin(H0)sin(T) 
						//ao += (lastSinH*sinT - sinH*sinT) *weight(length(H) / _StepRadius)*_Intensity;
						ao += (sinH - lastSinH) *weight(length(H) / _StepRadius)*_Intensity;
						lastSinH = sinH;
					}				
					//return ao;
                }   
                return ao;
            }
			//寻找最大的horizon
			float raymarchAO_FindHorizonal(float angle, float2 uv, float3 random, float3 dPdu, float3 dpdv, float viewDepth, float3 viewPos) {
				float ao = 0;
				//view space
				float cosA, sinA;
				sincos(angle, sinA, cosA);
				//samplerDir = (cos(A+random),sin(A+random))
				float2 sampleDir = float2(cosA * random.x - sinA * random.y, cosA * random.y + sinA * random.x);
				float2 sampleDir_V = normalize(sampleDir* _ViewLightDir.xy);

				//(float4(sampleDir_V0.xy,viewDepth,1)-float4(sampleDir_V1.xy,viewDepth,1)) * 2Near/(r-l) = (float4(uvoffset0*viewDepth,?,viewDepth) - float4(uvoffset1*viewDepth,?,viewDepth));
				//sampleDir_V * Radius * 2Near/(r-l)【常量】 = uvOffset * viewDepth; 
				//sampleDir_V * _StepRadius = uvOffset * viewDepth;

				//根据radius 计算 step size；
				float2 stepSizeView = sampleDir_V * _StepRadius / _StepNum;

				//viewSpace
				float4 stepSizeH = mul(Matrix_P, float4(stepSizeView.x, stepSizeView.y, -viewDepth, 1));	//viewspace的depth是反的
				float2 stepSize = stepSizeH.xy / stepSizeH.w * 1;	//-1,1的方向 正好是我们需要的
				float2 minStepSize = float2(1.0f / _ScreenParams.x, 1.0f / _ScreenParams.y)*_MinStepPixelNum;
				stepSize = normalize(stepSize) *max(minStepSize, abs(stepSize));	//最少跨距一个像素

				float2 stepSizeNV = float2(stepSize.x*Matrix_I_P[0][0], stepSize.y*Matrix_I_P[1][1]);
				float3 T = normalize(stepSizeNV.x * dPdu + stepSizeNV.y * dpdv);// +_Bias * vnormal;


				////imageSpace
				//float2 stepSizeImage = sampleDir_V;
				//stepSize = stepSizeImage * _Radius / _StepNum;
				//stepSize = normalize(stepSize) *max(minStepSize, abs(stepSize));	//最少跨距一个像素

				//float2 stepSizeV = float2(stepSize.x*Matrix_I_P[0][0], stepSize.y*Matrix_I_P[1][1]);
				//
				//T = normalize(stepSizeV.x * dPdu + stepSizeV.y * dpdv);

				float sinT = T.z + _Bias * viewDepth;
				float2 maxSinH_LH = float2(sinT,0);
				//raymarch
				for (int s = 1; s <= _StepNum; s++) {
					//snapUV+ jitter                   
					float jitter = 0;// hash(uv*s);
					float2 uv_s = snapUVToCenter(uv + stepSize * (s + jitter));
					float depth_Si = GetDepth(uv_s);
					float3 viewPos_Si = depthToView(depth_Si, uv_s);
					//Phi = H - T
					//cos(Phi)dPhi  = sinPHI0-sinPHI1 = sin(H0-T) - sin(H1-T) =sin(H0)-sin(T) - (sin(H1)-sin(T)) = sin(H0)-sin(H1)    //cos～=1
					float3 H = (viewPos_Si - viewPos);
					float sinH = H.z / length(H);
					maxSinH_LH = sinH>maxSinH_LH.x ? float2(sinH, length(H)) : maxSinH_LH;
					//return ao;
				}
				float atten = 1.0 / (1 + maxSinH_LH.y);
				ao = saturate((maxSinH_LH.x - sinT) *atten)*_Intensity;// *(lastSinH < (_MinDepth + _Bias) ? 0 : 1);
				return ao;
			}
           
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                //Ndc
                //float4 depthnormal = tex2D(_CameraDepthNormalsTexture, i.uv);
                //float3 viewNorm;
                //float viewDepth;
                //DecodeDepthNormal(depthnormal, viewDepth, viewNorm);
                ////viewSpace 的Depth
                //viewDepth *= _ProjectionParams.z;       //camera的farplane

				float viewDepth = GetDepth(i.uv);


                float3 viewPos = depthToView(viewDepth, i.uv);
				//return float4(viewPos, 1);
                //test                   
                viewPos = GetViewByUV2(i.uv);
                
                float2 screenDelta = float2(1.0f/_ScreenParams.x, 1.0f / _ScreenParams.y)*1;
                
                //自己构造ddx,ddy
                
                float3 viewPos_r = GetViewByUV2(i.uv + float2(screenDelta.x, 0));
                float3 viewPos_l = GetViewByUV2(i.uv + float2(-screenDelta.x, 0));
                float3 viewPos_t = GetViewByUV2(i.uv + float2(0,screenDelta.y));
                float3 viewPos_b = GetViewByUV2(i.uv + float2(0,-screenDelta.y));
                
                float3 dPdu = normalize(MinDiff(viewPos, viewPos_r, viewPos_l));
                //dPdu = self_DDX(viewPos,i.uv);
                //dPdu = (ddx(viewPos));

                
                float3 dPdv = normalize(MinDiff(viewPos, viewPos_t, viewPos_b) * (_ScreenParams.y * screenDelta.x));
                //dPdv = self_DDY(viewPos,i.uv);
                //dPdv = (ddy(viewPos));

				//圆采样的 方向 根据stepNum反算
				float angle = 2 * UNITY_PI / _SampleDirNum;

				//xy: 随机旋转,z: 随机step大小
				float3 rand = tex2D(_NoiseTex, viewPos.xy * _NoiseTexSize).rgb;

                float ao = 0;
                for (int n = 0; n < _SampleDirNum; n++) {
#if _FindHorizonal
                    ao+= raymarchAO_FindHorizonal(angle*n, i.uv, rand, dPdu, dPdv, viewDepth, viewPos);
#else
					ao += raymarchAO_Integral(angle*n, i.uv, rand, dPdu, dPdv, viewDepth, viewPos);
#endif
                }
				float luminance = Luminance(col.rgb)*_AOColor.a;
				//return luminance;
				//ao = min(luminance, ao);
				float3 aoC = lerp(_AOColor.rgb, 1.0, luminance);
				ao = (1 - pow(ao / _SampleDirNum, 1));
				col.rgb = lerp(aoC,1,ao);// lerp(col.rgb, ao*col.rgb, luminance);
                return col;
            }
            ENDCG
        }
    }
}
