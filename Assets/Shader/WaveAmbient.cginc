
float3 _AmbientSkyLight;
float3 _AmbientOceanLight;
float _AmbientTop;
float _AmbientBottom;
float _AmbientExtinction;


// From patapom.com
float Ei(float z) {
  const float EulerMascheroniConstant = 0.577216f;
  const float z2 = z  * z;
  const float z3 = z2 * z;
  const float z4 = z3 * z;
  const float z5 = z4 * z;
  return EulerMascheroniConstant + log(z) + z + z2/4.f + z3/18.f + z4/96.f + z5/600.f;
}

float3 ComputeAmbientColor(float3 Position, float ExtinctionCoeff){
  float Hp = _AmbientTop - Position.y; // Height to the top of the volume 
  float a = -ExtinctionCoeff * Hp;
  float3 IsotropicScatteringTop = _AmbientSkyLight * max( 0.0, exp( a ) - a * Ei( a ));
  float Hb = Position.y - _AmbientBottom; // Height to the bottom of the volume
  a = -ExtinctionCoeff * Hb;
  float3 IsotropicScatteringBottom = _AmbientOceanLight * max( 0.0, exp( a ) - a * Ei( a ));
  return (IsotropicScatteringTop + IsotropicScatteringBottom) ;
}

float3 AmbientColor(LightingData data)
{
	float3 F = data.fresnel;
	float3 refractionFactor = (float3(1,1,1) - F) ;

    return ComputeAmbientColor(data.worldPos, _AmbientExtinction) * refractionFactor;
}