// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ChillyRoom/Noise/Sample 3D Texture" {
	Properties{
		_Z("Z", Range(-2,2)) = 1.0
		_X("X", Range(-2,2)) = 1.0
		_Volume("Texture", 3D) = "" {}
	}
		SubShader{
		Cull Off
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers flash gles

#include "UnityCG.cginc"

		struct vs_input {
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		}; 

	struct ps_input {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};


	ps_input vert(vs_input v)
	{
		ps_input o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

	sampler3D _Volume;
	float _Z;
	float _X;
	float4 frag(ps_input i) : COLOR
	{
		return tex3D(_Volume, float3(i.uv.x+_X,i.uv.y,_Z)).b;
	}

		ENDCG

	}
	}

		Fallback "VertexLit"
}
