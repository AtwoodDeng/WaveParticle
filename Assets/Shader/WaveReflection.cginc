
samplerCUBE _SkyCube;

float3 SkyReflection( LightingData data )
{
	float3 reflectDir = reflect(-data.view , data.normal );

	float4 refSkyCol =  texCUBE(_SkyCube , reflectDir);
	
	float3 F = data.fresnel;

	return F * refSkyCol.rgb ;
}