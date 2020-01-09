#ifndef HBAO_INTEGRATED_INCLUDED
#define HBAO_INTEGRATED_INCLUDED

    half4 frag(v2f i) : SV_Target {
		half4 occ = FetchOcclusion(i.uv2);
		half3 ao = lerp(_BaseColor.rgb, half3(1.0, 1.0, 1.0), occ.a);
#if UNITY_SINGLE_PASS_STEREO
		half4 gbuffer3 = tex2D(_rt3Tex, UnityStereoTransformScreenSpaceTex(i.uv2));
#else
		half4 gbuffer3 = tex2D(_rt3Tex, i.uv2);
#endif
		gbuffer3.rgb = -log2(gbuffer3.rgb);
	    gbuffer3.rgb *= ao;
#if COLOR_BLEEDING_ON
	    gbuffer3.rgb += 1 - occ.rgb;
#endif
	    gbuffer3.rgb = exp2(-gbuffer3.rgb);

	    return gbuffer3;
    }

    half4 frag_blend(v2f i) : SV_Target {
		half4 occ = FetchOcclusion(i.uv2);
	    half3 ao = lerp(_BaseColor.rgb, half3(1.0, 1.0, 1.0), occ.a);
	    return half4(ao, 0);
    }

#endif // HBAO_INTEGRATED_INCLUDED
