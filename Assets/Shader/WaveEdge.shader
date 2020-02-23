Shader "Unlit/WaveEdge"
{
    Properties
    {
        _HeightMap ("Height Map", 2D) = "white" {}
        _TessFactor("Tess Factor", float) = 1 
        //_Range ("Range", vector ) = ( 5.0, 5.0, 0.01 , 0.01)
        //_Speed ("Speed", range(0,5.0) ) = 2.0
        //_Radius ("Radius", range(0,5.0) ) = 2.0
        //_Height("Height", range(0,2.0) ) = 1.0
        //_DxDz ("DxDz", range(0,2.0) ) = 1.0
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
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float3 objPos : TEXCOORD1;
            };

            struct InternalTessInterp_appdata {
              float4 vertex : INTERNALTESSPOS;
              float4 tangent : TANGENT;
              float3 normal : NORMAL;
              float2 texcoord : TEXCOORD0;
            };

            sampler2D _HeightMap;
            float4 _HeightMap_ST;
            float _TessFactor;

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
            v2f ds (UnityTessellationFactors tessFactors, const OutputPatch<InternalTessInterp_appdata,3> vi, float3 bary : SV_DomainLocation) {
              appdata v;

              v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;
              //v.vertex.y = ( v.vertex.y > 0 ) ? GetWaveHeight(v.vertex.xyz) : -0.5;
              // v.vertex.y = ( v.vertex.y > 0 ) ? tex2Dlod(_HeightMap,float4(v.vertex.xz*0.1 - 0.5,0,0) ).r : -0.5;
              
              float4 offset = tex2Dlod(_HeightMap,float4(v.vertex.xz*0.1 - 0.5,0,0) );
               
              v.vertex.xz += ( v.vertex.y > 0 ) ? offset.yz : float2(0,0);
              v.vertex.y = ( v.vertex.y > 0 ) ? offset.x : -0.5;

              v.tangent = vi[0].tangent*bary.x + vi[1].tangent*bary.y + vi[2].tangent*bary.z;
              v.normal = vi[0].normal*bary.x + vi[1].normal*bary.y + vi[2].normal*bary.z;
              v.texcoord = vi[0].texcoord*bary.x + vi[1].texcoord*bary.y + vi[2].texcoord*bary.z;

              v2f o = vert (v);
              return o; 
            }


            fixed4 frag (v2f i) : SV_Target
            {
                return lerp( fixed4(0.8,0.5,0.5,1.0) , fixed4(0.6f,0.5f,0.8f,1.0f) , sin(_Time.y) );
                // return fixed4(1.0f,1.0f,1.0f,1.0f);
            }
            ENDCG
        }
    }
}