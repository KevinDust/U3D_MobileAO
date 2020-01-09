// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#ifndef AMPLIFY_AO_APPLY_POSTEFFECT
#define AMPLIFY_AO_APPLY_POSTEFFECT

struct PostEffectOutputTemporal
{
	half4 occlusionColor : SV_Target0;
	half4 temporalAcc : SV_Target1;
};


half4 ApplyDebug( const v2f_in IN )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const half2 screenPos = IN.uv.xy;

	const half2 occlusionDepth = FetchOcclusionDepth( screenPos );

	const half4 occlusionRGBA = CalcOcclusion( occlusionDepth.x, occlusionDepth.y );

	return half4( occlusionRGBA.rgb, 1 );
}


PostEffectOutputTemporal ApplyDebugTemporal( const v2f_in IN, const bool aUseMotionVectors )
{
	UNITY_SETUP_INSTANCE_ID( IN );
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

	const float2 screenPos = IN.uv.xy;

	const half2 occlusionDepth = FetchOcclusionDepth( screenPos );

	PostEffectOutputTemporal OUT;

	if( occlusionDepth.y < HALF_MAX )
	{
		half occlusion;
		const half4 temporalAcc = TemporalFilter( screenPos, occlusionDepth, aUseMotionVectors, occlusion );

		const half4 occlusionRGBA = CalcOcclusion( occlusion, occlusionDepth.y );

		OUT.occlusionColor = occlusionRGBA;
		OUT.temporalAcc = temporalAcc;
	}
	else
	{
		OUT.occlusionColor = half4( (1).xxxx );
		OUT.temporalAcc = half4( (1).xxxx );
	}

	return OUT;
}


#endif
