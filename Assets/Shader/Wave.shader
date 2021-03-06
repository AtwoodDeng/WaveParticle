﻿Shader "Unlit/Wave"
{
    Properties
    {
        [Header(Mesh)]
        //_HeightMap ("Height Map", 2D) = "white" {}
        _TessFactor("Tess Factor", float) = 1
        //_Albedo("Albedo" , color ) = (1.0,1.0,1.0,1.0)

        [Header(Specular)]
        _DirSpecIntensity("Directional Specular Intensity" , float ) = 1.0
        _SkySpecularIntensity("Sky Specular Intensity" , float ) = 1.0
        _Roughness("Roughness" , range(0,1)) = 0.0
        _SkyCube("Sky Cube" , Cube ) = "white" {}
        _IOR("IOR " , float) = 1.5
        
        [Header(Scattering)]
        _AmbientSkyLight("Ambient Sky Light", color) = (1.0,1.0,1.0,1.0)
        _AmbientOceanLight("Ambient Ocean Light", color) = (1.0,1.0,1.0,1.0)
        _AmbientTop("Ambient Top", float) = 1
        _AmbientBottom("Ambient Bottom", float) = 1
        _AmbientExtinction("Ambient Extinction", float) = 1
        _AmbientIntensity("Ambient Intensity", float) = 1

        [Header(subsurfaceScattering)]
        _SubSurfaceSunDistort("SSS Distort", range(0,1.0)) = 1.0
        _SubSurfaceSunFallOff("SSS Fall off"  , range(1.0,16.0)) = 1.0
        _SSSIntensity("SSS Intensity", float) = 1
        _SSSColor("SSS Color", color) = (1.0,1.0,1.0,1.0)

        [Header(Detail)] 
        _DetailNormal("Detail Normal", 2D) = "white" {}
        _FlowMap("Flow Map", 2D) = "white" {}
        _DetailNormalIntensity("Detail Normal Intensity", range(0,2)) = 1.0
        _DetailFlowIntensity("Detail Flow Intensity", range(0,2)) = 1.0
        _DetailFlowmapTiling("Detail Normal Tiling", float) = 1.0
        _DetailFlowmapOffset("Detail Normal Offset", float) = 0.0
        _DetailFlowmapSpeed("Detail Normal Speed", float) = 1.0
        _DetailFlowmapJump("Detail Normal Jump", vector) = (0.25,0.25,0,0)

        [Header(Foam)]
        _FoamMap("Foam Map", 2D) = "white" {}
        _FoamFlowMap("Foam Flow Map", 2D) = "white" {}
        _FoamIntensity("Foam Intensity", float ) = 1.0
        _FoamFlowIntensity("Foam Flow Intensity", float ) = 1.0
        _FoamH0("Foam H0", float ) = 1.0
        _FoamHmax("Foam Hmax", float ) = 1.0
        _FoamThreshold_J("Foam J", float ) = 1.0
        _FoamFlowmapSpeed("Foam Flow Speed", float ) = 1.0
        _FoamFlowmapTiling("Foam Flow Tiling", float ) = 1.0
        _FoamAlphaCut("Foam Alpha Cut", float ) = 0.5

        [Toggle]_IsEdge("Is Edge" , float )=0

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex tessvert
            #pragma fragment frag
            #pragma hull hs
            #pragma domain ds
            #pragma target 4.6

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "WaveLib.cginc"

            float _IsEdge;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 texcoord:TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 tangentWS : TANGENT;
                float3 normalWS : NORMAL;
                float3 objPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 view : TEXCOORD3;
            };
            

            struct InternalTessInterp_appdata {
              float4 vertex : INTERNALTESSPOS;
              float4 tangent : TANGENT;
              float3 normal : NORMAL;
              float2 texcoord : TEXCOORD0;
              //float3 worldPos : TEXCOORD1;
              //float3 view : TEXCOORD2;
            };


            InternalTessInterp_appdata tessvert (appdata v) {
              InternalTessInterp_appdata o;
              o.vertex = v.vertex;
              o.tangent = v.tangent;
              o.normal = v.normal;
              o.texcoord = v.texcoord;
              return o;
            }


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.objPos = v.vertex.xyz;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _HeightMap);
                //o.normalWS = mul(unity_ObjectToWorld, v.normal);
                o.normalWS = v.normal;
                o.worldPos = mul(unity_ObjectToWorld , v.vertex);
                o.view = normalize(_WorldSpaceCameraPos - o.worldPos);
                o.tangentWS = mul(unity_ObjectToWorld,v.tangent);

                return o;
            }


            UnityTessellationFactors hsconst (InputPatch<InternalTessInterp_appdata,3> v) {
              UnityTessellationFactors o;
              float4 tf;
              tf = float4(_TessFactor,_TessFactor,_TessFactor,_TessFactor);
              o.edge[0] = tf.x; 
              o.edge[1] = tf.y; 
              o.edge[2] = tf.z; 
              o.inside = tf.w;
              return o;
            }

            [UNITY_domain("tri")]
            [UNITY_partitioning("fractional_odd")]
            [UNITY_outputtopology("triangle_cw")]
            [UNITY_patchconstantfunc("hsconst")]
            [UNITY_outputcontrolpoints(3)]
            InternalTessInterp_appdata hs (InputPatch<InternalTessInterp_appdata,3> v, uint id : SV_OutputControlPointID) {
              return v[id];
            }


            [UNITY_domain("tri")]
            v2f ds (UnityTessellationFactors tessFactors, 
            const OutputPatch<InternalTessInterp_appdata,3> vi, 
            float3 bary : SV_DomainLocation) {

              InternalTessInterp_appdata v;

              v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;

              float2 uv = objPosToMapUV(v.vertex);
              float4 offset = tex2Dlod(_HeightMap,float4(uv,0,0));
              float4 normal = tex2Dlod(_NormalMap,float4(uv,0,0));

              offset.xyz = (_IsEdge > 0 && v.vertex.y < 0 ) ? float3(-0.5,0,0) : offset.xyz;
              v.vertex.xz += offset.yz;
              v.vertex.y = offset.x;

              v.tangent = vi[0].tangent*bary.x + vi[1].tangent*bary.y + vi[2].tangent*bary.z;
              
              v.normal = normal;

              normal.xyz = float3(0,1,0);
              
              float3 detailNormal = DetailNormal(uv,normal,v.tangent) * _DetailNormalIntensity;
              detailNormal = (_IsEdge > 0 && v.vertex.y < 0 ) ? float3(0,0,0) : detailNormal;
              v.normal = normalize(v.normal + detailNormal );
              v.vertex.xyz += detailNormal * 0.02; 
              

              v.texcoord = vi[0].texcoord*bary.x + vi[1].texcoord*bary.y + vi[2].texcoord*bary.z;
              

              v2f o = vert (v);
              return o;
            }


            fixed4 frag (v2f i) : SV_Target
            {
                LightingData data;
                data.view = i.view;
                data.normal = i.normalWS;
                data.worldPos = i.worldPos;
                data.objPos = i.objPos;
                data.lightDir = _WorldSpaceLightPos0.xyz;


                data.F0 = float3(1,1,1) * IORtoF0(1.0,_IOR); 
                data.albedo = _Albedo;
                data.metalness = 0; 
                data.roughness = _Roughness;

	            // Half-vector between Li and Lo.
	            float3 H = normalize(data.lightDir + data.view);
	            float VdotH = max(0.0, dot(H, data.view));
                data.fresnel = fresnelSchlick(data.F0, VdotH );

                float3 specularSun = BRDFDirectionalSpecular( data ) * _DirSpecIntensity;

                float3 specularSky = SkyReflection(data) * _SkySpecularIntensity;

                float3 subsurfaceScattering = SubsurfaceScattering(data) * _SSSIntensity;

                float3 ambientColor = AmbientColor(data) * _AmbientIntensity;

                float3 waveCol = specularSun + specularSky + ambientColor + subsurfaceScattering;

                float4 foam = Foam(data);

                waveCol = lerp( waveCol , foam.xyz , foam.a);
                
                return fixed4(waveCol ,1.0);

                // return tex2Dlod(_HeightMap,float4(i.objPos.xz*0.1-0.5,0,0));
                //return fixed4(1,1,1,1) * (i.objPos.y - 1) * 0.4;
                // return fixed4(1.0f,1.0f,1.0f,1.0f);
            }
            ENDCG
        }
    }
}