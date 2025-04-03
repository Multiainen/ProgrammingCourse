using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// aspect for a projectile, as determined by the Projectile component
public readonly partial struct AspectMap : IAspect
{
    private readonly RefRW<MapComponent> map;

    public int GetVertQueueLength()
    {
        if (!map.ValueRO.vertsToRaise.IsCreated || map.ValueRO.vertsToRaise.IsEmpty())
            return 0;
        return map.ValueRO.vertsToRaise.Count;
    }

    public float2 GetNextVert()
    {
        return map.ValueRO.vertsToRaise.Dequeue();
    }

    public float GetMapData(int index)
    {
        return map.ValueRO.mapData[index];
    }

    public void SetMapData(int index, float value)
    {
        map.ValueRW.mapData[index] = value;
    }
}