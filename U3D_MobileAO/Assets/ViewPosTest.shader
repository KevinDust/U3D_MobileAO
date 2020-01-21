Shader "Unlit/ViewPosTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float3 normal : NORMAL;

            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 viewPos : TEXCOORD0;
				float3 normal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.viewPos = mul(UNITY_MATRIX_MV, v.vertex).xyz;
				o.normal = v.normal;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
				float3 dPdu = ddx(i.viewPos);
				float3 dPdv = ddy(i.viewPos);
				float3 viewN = mul(UNITY_MATRIX_MV,float4(i.normal,1)).xyz;
				return float4(normalize(cross(dPdu, dPdv)), 1);
				return float4(viewN,1);
                //return float4(i.viewPos,1);
				//return float4(dPdu, 1)*100;
                //return float4(normalize(0.5*dPdu+ 0.5*dPdv),1);
            }
            ENDCG
        }
    }
    fallback "Diffuse"
}
