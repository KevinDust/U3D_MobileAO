Shader "ChillyRoom/Noise/Blend/2DDefault"
{
	Properties
	{
		_MainTex0("MainTex",2D) = "white" {}
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
			#include "UnityCG.cginc"
			#include "UnityCustomRenderTexture.cginc"
			sampler2D _MainTex0;
			float4 _MainTex_ST;
			

			fixed4 frag (v2f_customrendertexture i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex0,i.globalTexcoord.xy);
				return col;
			}
			ENDCG
		}
	}
}
