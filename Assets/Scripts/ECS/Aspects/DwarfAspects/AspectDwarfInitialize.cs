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
    private readonly RefRW<PhysTarget> physTarget;
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

    public void SetType(int type)
    {
        physTarget.ValueRW.type = type;
    }

    // move waypoint target forward (previous current target is previous target, previous next target is current target, get new next target)
    public void SetTarget(int targetPath, int curPath, float3 initialPos, float2 offset, int waypoint)
    {
        target.ValueRW.offset = offset;
        target.ValueRW.value = initialPos;
        target.ValueRW.targetPath = targetPath;
        target.ValueRW.curPath = curPath;
        target.ValueRW.waypoint = waypoint;
    }

    public void SetHP(int hp)
    {
        data.ValueRW.health = hp;
    }
}
