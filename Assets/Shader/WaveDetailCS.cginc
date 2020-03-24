float _DetailFlowmapTiling;
float _DetailFlowmapSpeed;
float4 _DetailFlowmapJump;
float _DetailFlowmapOffset;
float4 _FlowMapSize;

float3 FlowUVW (float2 uv, float2 flowVector, float time, float2 jump, float tiling, float flowOffset, bool flowB) {
	float phaseOffset = flowB ? 0.5 : 0;
	float progress = frac(time + phaseOffset);
	float3 uvw;
	uvw.xy = uv - flowVector * (progress+flowOffset);
	uvw.xy *= tiling;
	uvw.xy += phaseOffset;
	uvw.xy += (time - progress) * jump;
	uvw.z = 1 - 0.5 * abs(1 - 2 * progress);
	return uvw;
}

float4 ReadTex( Texture2D<float4> tex , float2 uv )
{
	uint2 id = floor( uv * _FlowMapSize.xy );

	return tex[id];
}

float3 DetailNormal( float2 uv , float3 normal , float3 tangent  )
{
	// uint2 detailUV = floor(uv * _FlowMapSize.xy);
    // float4 detailUV = float4(uv * _FlowMapSize.xy , 0 ,0 );

	float4 flow = ReadTex(FlowMap,uv);
    
	float noise = flow.a;
    float2 flowVector = flow.rg * 2 - 1;
	float2 jump = _DetailFlowmapJump.xy;
	float speed = _DetailFlowmapSpeed * ( 1.0 + flow.z ) * 0.5; 
	


    float time = _Time.y * _DetailFlowmapSpeed + noise;

    
    float3 uvwA = FlowUVW(uv, flowVector, time, jump, _DetailFlowmapTiling,_DetailFlowmapOffset, false);
    float3 uvwB = FlowUVW(uv, flowVector, time, jump, _DetailFlowmapTiling,_DetailFlowmapOffset, true);
	


    float3 normal1 = ReadTex(DetailNormalMap, frac(uvwA.xy)).rgb * uvwA.z;
    float3 normal2 = ReadTex(DetailNormalMap, frac(uvwB.xy)).rgb * uvwB.z;

	

    float3 binormal = normalize(cross(normal, tangent.xyz));

	normal1 = normal1 * 2 - 1.0;
	normal2 = normal2 * 2 - 1.0;
	
	float3 detailNormal = normalize(normal1+normal2);
	
	detailNormal = normalize(
		detailNormal.x * tangent +
		detailNormal.y * binormal +
		detailNormal.z * normal
	);

    
	return detailNormal;
}
