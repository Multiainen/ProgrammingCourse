using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// aspect of an enemy currently receiving its initial waypoint target, as determined by the First Target tag
public readonly partial struct AspectDwarfInitialTarget : IAspect
{
    private readonly RefRW<DwarfTarget> target;
    private readonly RefRW<EnemyID> id;
    private readonly RefRW<TargetRot> rot;
    private readonly RefRO<TagFirstTarget> tagFirstTarget;

    public void SetRot(int rot)
    {
        this.rot.ValueRW.target = rot;
    }
    public float3 GetTarget()
    {
        return target.ValueRO.value;
    }

    // move waypoint target forward (previous current target is previous target, previous next target is current target, get new next target)
    public void SetTarget(float3 nextTarget, int2 nextKey)
    {
        target.ValueRW.prevValue = target.ValueRO.value;
        target.ValueRW.prevKey = target.ValueRO.key;
        target.ValueRW.value = target.ValueRO.nextValue;
        target.ValueRW.key = target.ValueRO.nextKey;
        target.ValueRW.nextValue = nextTarget;
        target.ValueRW.nextKey = nextKey;
    }

    public int2 GetTargetKey()
    {
        return target.ValueRO.key;
    }
    public int2 GetNextTargetKey()
    {
        return target.ValueRO.nextKey;
    }

    public int GetID()
    {
        return id.ValueRO.value;
    }
}
