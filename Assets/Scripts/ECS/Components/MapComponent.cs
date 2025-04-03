using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[ChunkSerializable]
public struct MapComponent : IComponentData
{
    public UnsafeHashSet<int2> revealedTiles;
    public UnsafeList<int2> addNewTiles;
    public UnsafeList<float> mapData;
    public UnsafeQueue<float2> vertsToRaise;
    public UnsafeQueue<SoundOrder> soundQueue;
    public UnsafeList<int> pathStartStep;
    public int mapWidth, mapHeight;
}

public struct SoundOrder
{
    public float3 pos;
    public int index;

    public SoundOrder(float3 pos, int index)
    {
        this.pos = pos;
        this.index = index;
    }
}

public struct MapRefComponent : IComponentData
{
    public BlobAssetReference<MapRefComponentContents> contents;
}

public struct MapRefComponentContents : IComponentData
{
    public BlobArray<int> pathMap;
    public BlobArray<int2> islands;
    public BlobArray<int> pathOrigins;
    public BlobArray<int> pathIndices;
    public BlobArray<float2> paths;
    public BlobArray<float2> pathSteps;
    public BlobArray<float> pathStepLength;
}
