float _FoamIntensity;
float _FoamH0;
float _FoamHmax;
float _FoamThreshold_J;
float _FoamFlowmapSpeed;
float _FoamFlowmapTiling;
float _FoamFlowIntensity;
float _FoamAlphaCut;


float3 FoamFlow (float2 uv, float2 flowVector, float time, float2 jump, float tiling, float flowOffset, bool flowB) {
	float phaseOffset = flowB ? 0.5 : 0;
	float progress = frac(time + phaseOffset);
	float3 uvw;
	uvw.xy = uv - flowVector *(progress+flowOffset);
	uvw.xy *= tiling;
	uvw.xy += phaseOffset;
	uvw.xy += (time - progress) * jump;
	uvw.z = 1 - 0.5 * abs(1 - 2 * progress);

	return uvw;
}

float4 Foam( LightingData data )
{
	float2 uv = objPosToMapUV(data.objPos);

	//float J = tex2Dlod(_ToolMap,float4(uv,0,0) ).b;
	//float foam = saturate(_FoamThreshold_J - J) * _FoamIntensity;
	// return tex2Dlod(_HeightMap,float4(uv,0,0) ).yzyz;

	float h0 = _FoamH0; 
	float hmax = _FoamHmax;
	float foamAlpha = saturate( ( data.objPos.y - h0) / ( hmax - h0 ) ) ;

	
	float4 flow = tex2Dlod(_FoamFlowMap, float4( uv * _FoamFlowMap_ST.xy ,0,0) );
    float noise = flow.a;
    float2 flowVector = (flow.rg * 2 - 1) * _FoamFlowIntensity;
	float2 jump = float2(0,0);
	float speed = _FoamFlowmapSpeed * ( 1.0 + flow.z ) * 0.5; 
    float time = _Time.y * _FoamFlowmapSpeed + noise;

	float3 uvwA = FoamFlow(uv, flowVector, time, jump, _FoamFlowmapTiling,0.5, false);
    float3 uvwB = FoamFlow(uv, flowVector, time, jump, _FoamFlowmapTiling,0.5, true);
	
    float3 foam1 = tex2Dlod(_FoamMap, float4(uvwA.xy,0,0)).rgb * uvwA.z;
    float3 foam2 = tex2Dlod(_FoamMap, float4(uvwB.xy,0,0)).rgb * uvwB.z;

	float3 foam =  (foam1+foam2) / 2 ;
	//float3 foam = lerp( max(foam1,foam2), (foam1+foam2) , 0.5);
	 
	foamAlpha = saturate( foamAlpha + dot(float3(0.33,0.33,0.33),foam) - _FoamAlphaCut ); 


	return float4(foam * _FoamIntensity,foamAlpha);
}