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
    public UnsafeList<int> pathMap;
    public UnsafeList<int2> islands;
    public UnsafeList<int> pathOrigins;
    public UnsafeList<float2> paths;
    public UnsafeList<int> pathIndices;
    public int mapWidth, mapHeight;
}
