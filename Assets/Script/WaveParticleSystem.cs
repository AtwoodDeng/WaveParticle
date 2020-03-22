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
    public RenderTexture heightMap;
    public RenderTexture normalMap;
    public RenderTexture toolMap;
    private int calculateHeightHandle;
    private int calculateNormalHandle;
    private int calculateToolHandle;
    private ComputeBuffer particleBuffer;

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

    private int TexWriteID = Shader.PropertyToID("TexWrite");
    private int TexReadID = Shader.PropertyToID("TexRead");
    private int TexReadSecID = Shader.PropertyToID("TexReadSec");
    private int ResolutionID = Shader.PropertyToID("_Resolution");
    private int ParticleCountID = Shader.PropertyToID("_ParticleCount");
    private int RangeID = Shader.PropertyToID("_Range");
    private int ResRangeID = Shader.PropertyToID("_ResRange");
    private int InfectRadiusFallOffID = Shader.PropertyToID("_InfectRadiusFallOff");
    private int InfectDistanceFallOffID = Shader.PropertyToID("_InfectDistanceFallOff");
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
        // heightMap = RenderTexture.GetTemporary(texResolution, texResolution,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        heightMap = new RenderTexture(texResolution, texResolution, 0, RenderTextureFormat.ARGBFloat);
        heightMap.enableRandomWrite = true;
        heightMap.filterMode = FilterMode.Bilinear;
        heightMap.wrapMode = TextureWrapMode.Repeat;
        heightMap.Create();

        normalMap = new RenderTexture(texResolution, texResolution, 0, RenderTextureFormat.ARGBFloat);
        normalMap.enableRandomWrite = true;
        normalMap.filterMode = FilterMode.Bilinear;
        normalMap.wrapMode = TextureWrapMode.Repeat;
        normalMap.Create();

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

        ParticleToTexShader.SetTexture(calculateHeightHandle, TexWriteID, heightMap);
        ParticleToTexShader.SetInt(ResolutionID, texResolution);
        ParticleToTexShader.SetInt(ParticleCountID, particleCount); 
        ParticleToTexShader.SetVector(RangeID, new Vector4(range,range,0,0));
        ParticleToTexShader.SetVector(ResRangeID, new Vector4(1.0f / texResolution * range , 1.0f / texResolution * range));

        particleBuffer.SetData(particleInfos);

        ParticleToTexShader.SetBuffer(calculateHeightHandle, ParticleInfosBufferID, particleBuffer);

        ParticleToTexShader.Dispatch(calculateHeightHandle, texResolution / 8, texResolution / 8,1);

        ParticleToTexShader.SetTexture( calculateNormalHandle , TexReadID , heightMap);
        ParticleToTexShader.SetTexture(calculateNormalHandle, TexWriteID, normalMap);
        ParticleToTexShader.Dispatch(calculateNormalHandle, texResolution / 8, texResolution / 8, 1);
        
        ParticleToTexShader.SetFloat(InfectDistanceFallOffID, infectDistanceFalloff );
        ParticleToTexShader.SetFloat(InfectRadiusFallOffID, infectRadiusFalloff);
        ParticleToTexShader.SetBuffer(calculateToolHandle, ParticleInfosBufferID, particleBuffer);
        ParticleToTexShader.SetTexture(calculateToolHandle, TexReadID, heightMap);
        ParticleToTexShader.SetTexture(calculateToolHandle, TexReadSecID, normalMap);
        ParticleToTexShader.SetTexture(calculateToolHandle, TexWriteID, toolMap);
        ParticleToTexShader.Dispatch(calculateToolHandle, texResolution / 8, texResolution / 8, 1);
         
    }

    public void UpdateMaterial()
    {
        Material[] mats = {data.waveMaterial, data.edgeMaterial};

        foreach( var mat in mats )
        {
            mat.SetTexture(HeightMapID, heightMap);
            mat.SetTexture(NormalMapID, normalMap);
            mat.SetTexture(ToolMapID, toolMap);
            mat.SetVector(RangeID, new Vector4(range, range, 1f / (range), 1f / (range)));
            mat.SetTexture(SkyCubeID, data.skybox);
        }
    }

    public void ReleaseBuffer()
    {
        if ( particleBuffer != null )
            particleBuffer.Release();

        if ( heightMap != null )
            heightMap.Release();

        if (normalMap!= null)
            normalMap.Release();

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
