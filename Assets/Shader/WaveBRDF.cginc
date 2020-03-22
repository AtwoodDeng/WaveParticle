
// GGX/Towbridge-Reitz normal distribution function.
// Uses Disney's reparametrization of alpha = roughness^2.
float ndfGGX(float NdotH, float roughness)  
{
	float alpha   = roughness * roughness;
	float alphaSq = alpha * alpha;

	float denom = (NdotH * NdotH) * (alphaSq - 1.0) + 1.0;
	return alphaSq / (PI * denom * denom);
}

// Single term for separable Schlick-GGX below.
float gaSchlickG1(float cosTheta, float k)
{
	return cosTheta / max(Epslion,(cosTheta * (1.0 - k) + k));
}

// Schlick-GGX approximation of geometric attenuation function using Smith's method.
float gaSchlickGGX(float NdotV, float NdotL , float roughness)
{
	float r = roughness + 1.0;
	float k = (r * r) / 8.0; // Epic suggests using this roughness remapping for analytic lights.
	return gaSchlickG1(NdotV, k) * gaSchlickG1(NdotL, k);
}

// Shlick's approximation of the Fresnel factor.
float3 fresnelSchlick(float3 F0, float cosTheta)
{
	return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

half Lambda(half cosTheta, half sigma) 
{
	half v = cosTheta / sqrt((1.0 - cosTheta * cosTheta) * (2.0 * sigma));
	return (exp(-v * v)) / (2.0 * v * M_SQRT_PI);
}

half3 CetoDirectionalLightSpecular(half3 V, half3 N, half3 L , float roughness) 
{

	half3 Ty = half3(0.0, N.z, -N.y);
	half3 Tx = cross(Ty, N);
    
    half3 H = normalize(L + V);
	half dhn = dot(H, N);
	half idhn = 1.0 / dhn;
    half zetax = dot(H, Tx) * idhn;
    half zetay = dot(H, Ty) * idhn;

	half p = exp(-0.5 * (zetax * zetax / roughness + zetay * zetay / roughness)) / (2.0 * 3.14159 * roughness);

    half zL = dot(L, N); // cos of source zenith angle
    half zV = dot(V, N); // cos of receiver zenith angle
    half zH = dhn; // cos of facet normal zenith angle
    half zH2 = zH * zH;

    half tanV = atan2(dot(V, Ty), dot(V, Tx));
    half cosV2 = 1.0 / (1.0 + tanV * tanV);
    half sigmaV2 = roughness * cosV2 + roughness * (1.0 - cosV2);

    half tanL = atan2(dot(L, Ty), dot(L, Tx));
    half cosL2 = 1.0 / (1.0 + tanL * tanL);
    half sigmaL2 = roughness * cosL2 + roughness * (1.0 - cosL2);

    zL = max(zL, 0.01);
    zV = max(zV, 0.01);
    
    return (L.y < 0) ? 0.0 :  p / ((1.0 + Lambda(zL, sigmaL2) + Lambda(zV, sigmaV2)) * zV * zH2 * zH2 * 4.0);

}

float3 BRDFCetoDirectionalLightSpecular(LightingData data)
{
	return CetoDirectionalLightSpecular(data.view,data.normal,data.lightDir,data.roughness);
}


float3 BRDFDirectionalSpecular(LightingData data )
{
	// Half-vector between Li and Lo.
	float3 H = normalize(data.lightDir + data.view);

	// Calculate angles between surface normal and various light vectors.
	float NdotL = max(0.0, dot(data.normal, data.lightDir ));
	float NdotH = max(0.0, dot(data.normal, H));
	float NdotV = max(0.0, dot(data.normal, data.view));
	float VdotH = max(0.0, dot(H, data.view));

	// Calculate Fresnel term for direct lighting. 
	//float3 F  = fresnelSchlick(data.F0, VdotH );
	float3 F = data.fresnel;
	// Calculate normal distribution for specular BRDF.
	float D = ndfGGX(NdotH , data.roughness);
	// Calculate geometric attenuation for specular BRDF.
	float G = gaSchlickGGX(NdotV, NdotL, data.roughness);
	
	// Cook-Torrance specular microfacet BRDF.
	float3 specularBRDF = (F * D * G) / max(Epslion, 4.0 * NdotL * NdotH) * _LightColor0 ;

	// return ReflectedSunRadianceNice(data.view,data.normal,data.lightDir, F.x,data.roughness);
	return specularBRDF ;
}
 

float3 BRDFDirection(LightingData data )
{
	// Half-vector between Li and Lo.
	float3 H = normalize(data.lightDir + data.view);

	// Calculate angles between surface normal and various light vectors.
	float NdotL = max(0.0, dot(data.normal, data.lightDir ));
	float NdotH = max(0.0, dot(data.normal, H));
	float NdotV = max(0.0, dot(data.normal, data.view));
	float VdotH = max(0.0, dot(H, data.view));
	float VdotN = max(0.0, dot(data.normal, data.view));
	float VdotL = max(0.0, dot(data.normal, data.lightDir));


	// Calculate Fresnel term for direct lighting. 
	float3 F  = fresnelSchlick(data.F0, VdotH );
	// Calculate normal distribution for specular BRDF.
	float D = ndfGGX(NdotH , data.roughness);
	// Calculate geometric attenuation for specular BRDF.
	float G = gaSchlickGGX(NdotV, NdotL, data.roughness);

	// Diffuse scattering happens due to light being refracted multiple times by a dielectric medium.
	// Metals on the other hand either reflect or absorb energy, so diffuse contribution is always zero.
	// To be energy conserving we must scale diffuse BRDF contribution based on Fresnel factor & metalness.
	float3 kd = lerp(float3(1, 1, 1) - F, float3(0, 0, 0), data.metalness);

	// Lambert diffuse BRDF.
	// We don't scale by 1/PI for lighting & material units to be more convenient.
	// See: https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
	float3 diffuseBRDF = kd * data.albedo;
	 
	// Cook-Torrance specular microfacet BRDF.
	float3 specularBRDF = (F * D * G) / max(Epslion, 4.0 * NdotL * NdotH) ;


	// Total contribution for this light.
	return (diffuseBRDF + specularBRDF) * NdotL;

}