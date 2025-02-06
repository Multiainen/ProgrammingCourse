using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// aspect of an enemy that is currently initializing, as determined by the Initialize tag
public readonly partial struct AspectDwarfInitialize : IAspect
{
    private readonly RefRW<DwarfTarget> target;
    private readonly RefRW<TargetRot> rot;
    private readonly RefRW<EnemyID> id;
    private readonly RefRW<DwarfData> data;
    private readonly RefRO<TagInitialize> tagSet;

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

    // move waypoint target forward (previous current target is previous target, previous next target is current target, get new next target)
    public void SetTarget(float3 target, int2 key, float3 nextTarget, int2 nextKey)
    {
        this.target.ValueRW.value = target;
        this.target.ValueRW.key = key;
        this.target.ValueRW.prevValue = target;
        this.target.ValueRW.prevKey = key;
        this.target.ValueRW.nextValue = nextTarget;
        this.target.ValueRW.nextKey = nextKey;
    }

    public void SetHP(int hp)
    {
        data.ValueRW.health = hp;
    }
}
