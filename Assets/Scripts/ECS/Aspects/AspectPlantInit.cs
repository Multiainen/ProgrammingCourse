using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public readonly partial struct AspectPlantInit : IAspect
{
    private readonly RefRO<TagFoliage> foliage;
    private readonly RefRO<TagInitialize> init;

    public void SetPos(ref LocalTransform t, float3 pos, float rot, float scale)
    {
        t.Position = pos;
        t.Rotation = quaternion.Euler(0, rot, 0);
        t.Scale = scale;
    }
}
