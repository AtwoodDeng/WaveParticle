using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class WaveParticleSystem : MonoBehaviour
{

    public WavePacketData _data;

    public WavePacketData data
    {
        get
        {
            if (_data == null)
                _data = Resources.Load<WavePacketData>("Data/BlueOcean");
            return _data;
        }
        set
        {
            if (value != null)
                _data = value;
        }
    }

    private WavePacketData dataLastFrame;

    // public int particleCount = 100;
    public int texResolution { get { return data.texResolution; } }


    public float range { get { return data.range; } }
    ////public float noisySpeed=1;
    public float dirSpeed { get { return data.dirSpeed; } }
    ////public float radius=1;
    ////public float height=1;
    ////public float DxDz=1;

    public float infectRadiusFalloff { get { return data.infectRadiusFalloff; } }
    public float infectDistanceFalloff { get { return data.infectDistanceFalloff; } }

    public Cubemap skybox { get { return data.skybox; } }

    public List<ParticlePacket> particlePackets
    {
        get { return data.packets;  }
    }

    

    public ComputeShader ParticleToTexShader;

    public MeshRenderer waveMesh;
    public MeshRenderer edgeMesh;
    //public Material[] waveMaterials;


    [Header("ReadOnly")]
    public RenderTexture[] heightMap;
    public RenderTexture[] normalMap;
    public RenderTexture toolMap;
    private int calculateHeightHandle;
    private int calculateNormalHandle;
    private int calculateToolHandle;
    private int calculateDetailHeightHandle;
    private ComputeBuffer particleBuffer;

    private const int READ = 0;
    private const int WRITE = 1;

    public void SwitchMap(RenderTexture[] textures)
    {
        var tem = textures[0];
        textures[0] = textures[1];
        textures[1] = tem;
    }

    public struct ParticleInfo
    {
        public Vector3 pos;
        public float radius;
        public float height;
        public float dxdz;
    }

    private Vector3[] particlePositions;
    private Vector3[] particleVelocities;
    private float[] particleNoiseSpeed;
    private ParticleInfo[] particleInfos;

    private int HeightMapCSID = Shader.PropertyToID("HeightMap");
    private int NormalMapCSID = Shader.PropertyToID("NormalMap");
    private int ToolMapCSID = Shader.PropertyToID("ToolMap");
    private int OutputMapID = Shader.PropertyToID("OutputMap");
    private int OutputMap2ID = Shader.PropertyToID("OutputMap2");
    private int ResolutionID = Shader.PropertyToID("_Resolution");
    private int ParticleCountID = Shader.PropertyToID("_ParticleCount");
    private int RangeID = Shader.PropertyToID("_Range");
    private int ResRangeID = Shader.PropertyToID("_ResRange");
    private int InfectRadiusFallOffID = Shader.PropertyToID("_InfectRadiusFallOff");
    private int InfectDistanceFallOffID = Shader.PropertyToID("_InfectDistanceFallOff");
    private int DetailFlowmapTilingID = Shader.PropertyToID("_DetailFlowmapTiling");
    private int DetailFlowmapSpeedID = Shader.PropertyToID("_DetailFlowmapSpeed");
    private int DetailFlowmapJumpID = Shader.PropertyToID("_DetailFlowmapJump");
    private int DetailFlowmapOffsetID = Shader.PropertyToID("_DetailFlowmapOffset");
    private int DetailNormalID = Shader.PropertyToID("DetailNormalMap");
    private int FlowMapID = Shader.PropertyToID("FlowMap");
    private int FlowMapSizeID = Shader.PropertyToID("_FlowMapSize");
    private int TimeID = Shader.PropertyToID("_Time");


    //private int SpeedID = Shader.PropertyToID("_Speed");
    //private int RadiusID = Shader.PropertyToID("_Radius");
    //private int HeightID = Shader.PropertyToID("_Height");
    //private int DxDzID = Shader.PropertyToID("_DxDz");
    private int HeightMapID = Shader.PropertyToID("_HeightMap");
    private int NormalMapID = Shader.PropertyToID("_NormalMap");
    private int SkyCubeID = Shader.PropertyToID("_SkyCube");
    private int ToolMapID = Shader.PropertyToID("_ToolMap");
    private int ParticleInfosBufferID = Shader.PropertyToID("ParticleInfos");

    public void SetupMaterial()
    {
        waveMesh.sharedMaterial = data.waveMaterial;
        edgeMesh.sharedMaterial = data.edgeMaterial;
    }
    public void SetupRT()
    {
        heightMap = new RenderTexture[2];

        for (int i = 0; i < 2; ++i)
        {
            // heightMap = RenderTexture.GetTemporary(texResolution, texResolution,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var rt = new RenderTexture(texResolution, texResolution, 0, RenderTextureFormat.ARGBFloat);
            rt.enableRandomWrite = true;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.Create();
            heightMap[i] = rt;
        }

        normalMap = new RenderTexture[2];
        for (int i = 0; i < 2; ++i)
        {
            var rt = new RenderTexture(texResolution, texResolution, 0, RenderTextureFormat.ARGBFloat);
            rt.enableRandomWrite = true;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.Create();

            normalMap[i] = rt;
        }

        toolMap = new RenderTexture(texResolution, texResolution, 0, RenderTextureFormat.ARGBFloat);
        toolMap.enableRandomWrite = true;
        toolMap.filterMode = FilterMode.Bilinear;
        toolMap.wrapMode = TextureWrapMode.Repeat;
        toolMap.Create();
    }


    public void SetupKernelHandles()
    {
        calculateHeightHandle = ParticleToTexShader.FindKernel("CalculateHeightMap");
        calculateNormalHandle = ParticleToTexShader.FindKernel("CalculateNormalMap");
        calculateToolHandle = ParticleToTexShader.FindKernel("CalculateTool");
        calculateDetailHeightHandle = ParticleToTexShader.FindKernel("CalculateDetailHeight");
    }

    public int GetParticleCount()
    {
        return data.GetParticleCount();
    }

    public void SetupParticles()
    {
        int particleCount = GetParticleCount();

        particlePositions = new Vector3[particleCount];
        particleVelocities = new Vector3[particleCount];
        particleInfos = new ParticleInfo[particleCount];
        particleNoiseSpeed = new float[particleCount];

        for (int i = 0; i < particleCount; ++i)
        {
            particlePositions[i] = new Vector3( Random.Range(-range,range) , 0 , Random.Range(-range, range));
            particleVelocities[i] = new Vector3(Random.Range(-1f,1f),0,Random.Range(-1f,1f)).normalized ;
        }

        particleBuffer = new ComputeBuffer(particleCount, 24);
    }

    public void UpdateParticleInfos()
    {
        int particleCount = GetParticleCount();

        if (particleInfos == null || particleInfos.Length != particleCount)
        {
            SetupParticles();
        }

        int index = 0;

        Random.InitState(0);
        // update noise speed
        for (int k = 0; k < particlePackets.Count; ++k)
        {
            var packet = particlePackets[k];
            for (int i = 0; i < packet.particleCount; ++i)
            {
                particleInfos[index].radius = packet.getRandRadius();
                particleInfos[index].height = packet.getRandHeight();
                particleInfos[index].dxdz = packet.getRandDxDz();
                particleNoiseSpeed[index] = packet.getRandSpeed();
                index++;
            }
        }

        // update position and  velocity
        for (int i = 0; i < particleCount; i++)
        {
            var vel = particleVelocities[i] * particleNoiseSpeed[i] + Vector3.right * dirSpeed;

            particlePositions[i] += vel * Time.deltaTime;
            particlePositions[i].x = Mathf.Repeat(particlePositions[i].x + range, range * 2f) - range;
            particlePositions[i].z = Mathf.Repeat(particlePositions[i].z + range, range * 2f) - range;

            particleInfos[i].pos = particlePositions[i];
        }

    }

    public void UpdateComputeShader()
    {
        int particleCount = GetParticleCount();


        //************ Height Map *************************
        ParticleToTexShader.SetInt(ResolutionID, texResolution);
        ParticleToTexShader.SetInt(ParticleCountID, particleCount); 
        ParticleToTexShader.SetVector(RangeID, new Vector4(range,range,1f / range ,1f / range));
        ParticleToTexShader.SetVector(ResRangeID, new Vector4(1.0f / texResolution * range , 1.0f / texResolution * range , 1.0f / texResolution, 1.0f/texResolution));
        particleBuffer.SetData(particleInfos);
        ParticleToTexShader.SetBuffer(calculateHeightHandle, ParticleInfosBufferID, particleBuffer);
        ParticleToTexShader.SetTexture(calculateHeightHandle, HeightMapCSID, heightMap[WRITE]);
        ParticleToTexShader.Dispatch(calculateHeightHandle, texResolution / 8, texResolution / 8,1);
        SwitchMap(heightMap);

        //************ Normal Map *************************
        ParticleToTexShader.SetTexture( calculateNormalHandle , HeightMapCSID , heightMap[READ]);
        ParticleToTexShader.SetTexture(calculateNormalHandle, NormalMapCSID, normalMap[WRITE]);
        ParticleToTexShader.Dispatch(calculateNormalHandle, texResolution / 8, texResolution / 8, 1);
        SwitchMap(normalMap);

        //************ Detail Height Map *************************
        if (data.UseDetail)
        {
            ParticleToTexShader.SetFloat(DetailFlowmapTilingID, data.detailFlowmapTiling);
            ParticleToTexShader.SetFloat(DetailFlowmapSpeedID, data.detailFlowmapSpeed);
            ParticleToTexShader.SetVector(DetailFlowmapJumpID, data.detailFlowmapJump);
            ParticleToTexShader.SetFloat(DetailFlowmapOffsetID, data.detailFlowmapOffset);
            ParticleToTexShader.SetVector(FlowMapSizeID,
                new Vector4(data.flowMap.width, data.flowMap.height, 1f / data.flowMap.width,
                    1f / data.flowMap.height));
            ParticleToTexShader.SetVector(TimeID, new Vector4(Time.time * 20f , Time.time,0,0));


            ParticleToTexShader.SetTexture(calculateDetailHeightHandle, HeightMapCSID, heightMap[READ]);
            ParticleToTexShader.SetTexture(calculateDetailHeightHandle, NormalMapCSID, normalMap[READ]);
            ParticleToTexShader.SetTexture(calculateDetailHeightHandle, DetailNormalID, data.detailNormalMap);
            ParticleToTexShader.SetTexture(calculateDetailHeightHandle, FlowMapID, data.flowMap);

            ParticleToTexShader.SetTexture(calculateDetailHeightHandle, OutputMapID, heightMap[WRITE]);
            ParticleToTexShader.SetTexture(calculateDetailHeightHandle, OutputMap2ID, normalMap[WRITE]);

            ParticleToTexShader.Dispatch(calculateDetailHeightHandle, texResolution / 8, texResolution / 8, 1);

            SwitchMap(heightMap);
            SwitchMap(normalMap);
        }


        //************ Tool Map *************************
        ParticleToTexShader.SetFloat(InfectDistanceFallOffID, infectDistanceFalloff );
        ParticleToTexShader.SetFloat(InfectRadiusFallOffID, infectRadiusFalloff);
        ParticleToTexShader.SetBuffer(calculateToolHandle, ParticleInfosBufferID, particleBuffer);
        ParticleToTexShader.SetTexture(calculateToolHandle, HeightMapCSID, heightMap[READ]);
        ParticleToTexShader.SetTexture(calculateToolHandle, NormalMapCSID, normalMap[READ]);
        ParticleToTexShader.SetTexture(calculateToolHandle, ToolMapCSID, toolMap);
        ParticleToTexShader.Dispatch(calculateToolHandle, texResolution / 8, texResolution / 8, 1);
         
    }

    public void UpdateMaterial()
    {
        Material[] mats = {data.waveMaterial, data.edgeMaterial};

        foreach( var mat in mats )
        {
            mat.SetTexture(HeightMapID, heightMap[READ]);
            mat.SetTexture(NormalMapID, normalMap[READ]);
            mat.SetTexture(ToolMapID, toolMap);
            mat.SetVector(RangeID, new Vector4(range, range, 1f / (range), 1f / (range)));
            mat.SetTexture(SkyCubeID, data.skybox);
        }
    }

    public void ReleaseBuffer()
    {
        if ( particleBuffer != null )
            particleBuffer.Release();

        if (heightMap != null)
        {
            foreach (var rt in heightMap)
            {
                rt.Release();
            }

            heightMap = null;
        }

        if (normalMap != null)
        {
            foreach (var rt in normalMap)
            {
                rt.Release();
            }

            normalMap = null;
        }

        if (toolMap != null)
            toolMap.Release();
    }

    public void SetupWave()
    {
        SetupMaterial();
        SetupRT();
        SetupKernelHandles();
        SetupParticles();
    }

    public void UpdateWave()
    {
        UpdateParticleInfos();
        UpdateComputeShader();
        UpdateMaterial();
    }
 
    // Start is called before the first frame update
    void Start()
    {
        dataLastFrame = data;
        SetupWave();
    }

    // Update is called once per frame
    void Update()
    {
        if (dataLastFrame != data)
        {
            ReleaseBuffer();
            SetupWave();
        }

        dataLastFrame = data;

        UpdateWave();
    }

    
    void OnDisable()
    {
        ReleaseBuffer();
    }

    public void OnDrawGizmosSelected()
    {
        if (particleInfos != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var info in particleInfos)
            {
                Gizmos.DrawWireSphere(transform.TransformPoint(info.pos),info.radius * 0.33f);
            }
        }
    }
}
