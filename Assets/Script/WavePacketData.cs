using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ParticlePacket
{
    public int particleCount = 10;
    public float noiseSpeed = 1f;
    [Range(0, 1f)] public float speedRandom = 0f;
    public float getRandSpeed() { return noiseSpeed * Random.Range(1f - speedRandom, 1f + speedRandom); }
    public float radius = 1f;
    [Range(0, 1f)] public float radiusRandom = 0f;
    public float getRandRadius() { return radius * Random.Range(1f - radiusRandom, 1f + radiusRandom); }
    public float height = 1f;
    [Range(0, 1f)] public float heightRandom = 0f;
    public float getRandHeight() { return height * Random.Range(1f - heightRandom, 1f + heightRandom); }
    public float dxdz = 1f;
    [Range(0, 1f)] public float dxdzRandom = 0f;
    public float getRandDxDz() { return dxdz * Random.Range(1f - dxdzRandom, 1f + dxdzRandom); }
}


[CreateAssetMenu(fileName = "Data", menuName = "Wave/WavePacketData", order = 1)]
public class WavePacketData : ScriptableObject
{
    [Header("Mesh")]
    public float range = 5;
    public float dirSpeed = 1f;
    public int texResolution = 512;
    public List<ParticlePacket> packets;

    [Header("Rendering")]
    public float infectRadiusFalloff = 2f;
    public float infectDistanceFalloff = 0.5f;
    public Cubemap skybox;
    public Material waveMaterial;
    public Material edgeMaterial;


    public int GetParticleCount()
    {
        int count = 0;

        foreach (var packet in packets)
        {
            count += packet.particleCount;
        }

        return count;
    }

}