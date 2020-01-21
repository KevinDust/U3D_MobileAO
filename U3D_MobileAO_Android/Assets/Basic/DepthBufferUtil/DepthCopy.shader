Shader "ChillyRoom/PostEffect/DepthCopy"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite On ZTest Always
		ColorMask 0
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
                float4 vertex : POSITION;
            };



            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;


			struct fragOut
			{
				float4 color : SV_Target;
				float depth : SV_Depth;		
			};

			sampler2D_float _MainDepthTexture;
			fragOut frag (v2f i)
            {
				fragOut o;
				o.depth = tex2D(_MainDepthTexture, i.uv).r;
				o.color = 0;
				return o;
				//o.color = (tex2D(_MainDepthTexture, i.uv).r);
				//o.color = EncodeFloatRGBA(tex2D(_MainDepthTexture, i.uv).r);
				
				//float4 c = tex2D(_MainDepthTexture, i.uv).r;
				
				//float2 c = EncodeFloatRG(tex2D(_MainDepthTexture, i.uv).r);
				//return c;

            }
            ENDCG
        }
    }
}
