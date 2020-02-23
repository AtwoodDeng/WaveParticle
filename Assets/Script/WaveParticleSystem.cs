using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class WaveParticleSystem : MonoBehaviour
{
    // public int particleCount = 100;
    public int texResolution = 512;

    public float range=5;
    //public float noisySpeed=1;
    public float dirSpeed=1;
    //public float radius=1;
    //public float height=1;
    //public float DxDz=1;

    public ParticlePacket[] particlePackets;

    public ComputeShader ParticleToTexShader;
    public Material[] waveMaterials;

    public RenderTexture rt;
    private int kernelHandle;
    private ComputeBuffer particleBuffer;

    [System.Serializable]
    public class ParticlePacket
    {
        public int particleCount=10;
        public float noiseSpeed = 1f;
        public float radius = 1f;
        public float height = 1f;
        public float dxdz = 1f;
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

    private int ResultTexID = Shader.PropertyToID("Result");
    private int ResolutionID = Shader.PropertyToID("_Resolution");
    private int ParticleCountID = Shader.PropertyToID("_ParticleCount");
    private int RangeID = Shader.PropertyToID("_Range");
    //private int SpeedID = Shader.PropertyToID("_Speed");
    //private int RadiusID = Shader.PropertyToID("_Radius");
    //private int HeightID = Shader.PropertyToID("_Height");
    //private int DxDzID = Shader.PropertyToID("_DxDz");
    private int HeightMapID = Shader.PropertyToID("_HeightMap");
    private int ParticleInfosBufferID = Shader.PropertyToID("ParticleInfos");
    public void SetupTexture()
    {
        // rt = RenderTexture.GetTemporary(texResolution, texResolution,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt = new RenderTexture(texResolution, texResolution, 0, RenderTextureFormat.ARGBFloat);
        rt.enableRandomWrite = true;
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.Create();
    }

    public void SetupShader()
    {
        kernelHandle = ParticleToTexShader.FindKernel("CSMain");
    }

    public int GetParticleCount()
    {
        int count = 0;

        foreach (var packet in particlePackets)
        {
            count += packet.particleCount;
        }

        return count;
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

        // update noise speed
        for( int k = 0 ; k < particlePackets.Length; ++ k )
        {
            var packet = particlePackets[k];
            for (int i = 0; i < packet.particleCount; ++i)
            {
                particleInfos[index].radius = packet.radius;
                particleInfos[index].height = packet.height;
                particleInfos[index].dxdz = packet.dxdz;
                particleNoiseSpeed[index] = packet.noiseSpeed;
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

    public void UpdateShader()
    {
        int particleCount = GetParticleCount();

        ParticleToTexShader.SetTexture(kernelHandle, ResultTexID, rt);
        ParticleToTexShader.SetInt(ResolutionID, texResolution);
        ParticleToTexShader.SetInt(ParticleCountID, particleCount); 
        ParticleToTexShader.SetVector(RangeID, new Vector4(range,range,0,0));
        //ParticleToTexShader.SetFloat(SpeedID,noisySpeed);
        //ParticleToTexShader.SetFloat(RadiusID, radius);
        //ParticleToTexShader.SetFloat(HeightID, height);
        //ParticleToTexShader.SetFloat(DxDzID, DxDz);


        particleBuffer.SetData(particleInfos);

        ParticleToTexShader.SetBuffer(kernelHandle, ParticleInfosBufferID, particleBuffer);

        ParticleToTexShader.Dispatch(kernelHandle, texResolution / 8, texResolution / 8,1);

        foreach (var mat in waveMaterials)
        {
            mat.SetTexture(HeightMapID,rt);   
        }
    }

    public void ReleaseBuffer()
    {
        if ( particleBuffer != null )
            particleBuffer.Release();
    }

 
    // Start is called before the first frame update
    void Start()
    {
        SetupTexture();
        SetupShader();
        SetupParticles();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateParticleInfos();
        UpdateShader();
    }

    void OnLateUpdate()
    {
        RenderTexture.ReleaseTemporary(rt);
    }

    void OnDisable()
    {
        ReleaseBuffer();
    }

    public void OnDrawGizmos()
    {
        if (particlePositions != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var pos in particlePositions)
            {
                Gizmos.DrawWireSphere(pos,0.3f);
            }
        }
    }
}
