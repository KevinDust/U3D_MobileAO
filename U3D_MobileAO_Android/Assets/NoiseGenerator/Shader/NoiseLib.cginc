
#ifdef _USEFBM
// Properties
int _Octaves = 1;
float _Lacunarity = 2.0;
float _Gain = 0.5;
//
// Initial values
float _Amplitude0 = 0.5;
float _Frequency0 = 1.;
float useFbm2D(float2 uv) {
	float value = 0;
	// sample the texture
	uv = uv *_Frequency0;
	for (int i = 0; i < _Octaves; i++) {
		value += _Amplitude0 * noise(uv);
		uv *= _Lacunarity;
		_Amplitude0 *= _Gain;
	}
	return value;
}

float useFbm3D(float3 uvw) {
	float value = 0;
	uvw = uvw * _Frequency0;
	for (int i = 0; i < _Octaves; i++) {
		value += _Amplitude0 * noise(uvw);
		uvw *= _Lacunarity;
		_Amplitude0 *= _Gain;
	}
	return value;
}

#endif