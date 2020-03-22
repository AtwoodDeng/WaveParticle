
float _SubSurfaceSunFallOff;
float _SubSurfaceSunDistort;
float3 _SSSColor;

float3 SubsurfaceScattering( LightingData data )
{
	float v = abs(data.view.y);
	// Approximate subsurface scattering - add light when surface faces viewer. Use geometry normal - don't need high freqs.
	half towardsSun = pow(max(0., dot(data.lightDir + data.normal * _SubSurfaceSunDistort , -data.view)), _SubSurfaceSunFallOff);
	half3 subsurface = towardsSun* _LightColor0 ;
	float2 uv = objPosToMapUV(data.objPos);
	float sss = tex2Dlod(_ToolMap,float4(uv,0,0)).g;

	subsurface *= (1.0 - v * v) * sss;

	float3 F = data.fresnel;
	subsurface *= (float3(1,1,1) - F) ;

	return subsurface * _SSSColor;
}