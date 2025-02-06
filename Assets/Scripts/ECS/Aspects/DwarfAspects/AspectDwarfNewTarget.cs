using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// aspect of an enemy waiting for a new waypoint target assignment, as determined by the New Target tag
// Moving tag ensures the enemy isn't currently being affected by physics motion. If it is, it should recover and reach its waypoint before getting a new one.
public readonly partial struct AspectDwarfNewTarget : IAspect
{
    private readonly RefRW<DwarfTarget> target;
    private readonly RefRW<EnemyID> id;
    private readonly RefRW<TargetRot> rot;
    private readonly RefRO<TagNewTarget> newTarget;
    private readonly RefRW<TagMoving> moving;

    public void SetRot(int rot)
    {
        this.rot.ValueRW.target = rot;
    }

    public int GetSettingTarget()
    {
        return moving.ValueRO.settingTarget;
    }

    public void SetSettingTarget(int value)
    {
        moving.ValueRW.settingTarget = value;
    }
    public float3 GetTarget()
    {
        return target.ValueRO.value;
    }

    public int2 GetNextKey()
    {
        return target.ValueRO.nextKey;
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

    public int GetID()
    {
        return id.ValueRO.value;
    }
}
