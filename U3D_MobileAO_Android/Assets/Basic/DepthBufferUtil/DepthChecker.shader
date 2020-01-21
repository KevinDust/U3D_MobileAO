﻿Shader "ChillyRoom/DepthChecker"
{
    Properties
    {

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
                float2 uv : TEXCOORD0;
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
			sampler2D _LastTex;
            sampler2D _MainTex;
			sampler2D_float _CameraDepthTexture;
            fixed4 frag (v2f i) : SV_Target
            {
				//return tex2D(_LastTex,i.uv);
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				depth = Linear01Depth(depth);
	#if defined(UNITY_REVERSED_Z)
				depth = 1 - depth;
	#endif
				return depth;
            }
            ENDCG
        }
    }
}
