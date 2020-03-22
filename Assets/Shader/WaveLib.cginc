#ifndef _MATH_
#define _MATH_
#define PI 3.1415926
#define Epslion 0.0001
#define M_SQRT_PI 1.7724538
#endif

sampler2D _HeightMap;
float4 _HeightMap_ST;
sampler2D _NormalMap;
//float4 _NormalMap_ST;
sampler2D _ToolMap;
//float4 _ToolMap_ST;
sampler2D _DetailNormal;
float4 _DetailNormal_ST;
sampler2D _FlowMap;
float4 _FlowMap_ST;

float _TessFactor;
float4 _Albedo;
float _Metalness;
float _Roughness;
float _IOR;

float _DirSpecIntensity;
float _SkySpecularIntensity;
float _AmbientIntensity;
float _SSSIntensity;
float _DetailNormalIntensity;


float4 _Range;

struct LightingData
{
	float3 view;
	float3 normal;
	float3 worldPos;
	float3 objPos;
	float3 lightDir;
	float3 F0;
	float3 albedo;
	float metalness;
	float roughness;
	float fresnel;
};

float IORtoF0(float IORin , float IORout )
{
	float r0 = ( IORin - IORout) / (IORin+IORout);

	return r0 * r0;
}

float2 objPosToMapUV(float3 objPos)
{
	return objPos.xz * _Range.zw * 0.5 + 0.5;
}

#include "UnityLightingCommon.cginc"
#include "WaveSurface.cginc"
#include "WaveBRDF.cginc"
#include "WaveReflection.cginc"
#include "WaveAmbient.cginc"
#include "WaveSSS.cginc"
#include "WaveDetail.cginc"