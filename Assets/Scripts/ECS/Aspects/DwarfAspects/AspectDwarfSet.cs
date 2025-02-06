using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// aspect for an enemy that needs to be teleported to a certain position, as determined by the Set Pos tag
public readonly partial struct AspectDwarfSet : IAspect
{
    private readonly RefRW<DwarfTarget> target;
    private readonly RefRW<TargetRot> rot;
    private readonly RefRW<EnemyID> id;
    private readonly RefRO<TagSetPos> tagSet;

    public void SetPos(ref LocalTransform transformAspect)
    {
        transformAspect.Position = target.ValueRO.value;
    }

    public void SetRot(int rot)
    {
        this.rot.ValueRW.target = rot;
    }

    public void SetID(int id)
    {
        this.id.ValueRW.value = id;
    }

    public void SetTarget(float3 target, int2 key)
    {
        this.target.ValueRW.value = target;
        this.target.ValueRW.key = key;
    }
}
