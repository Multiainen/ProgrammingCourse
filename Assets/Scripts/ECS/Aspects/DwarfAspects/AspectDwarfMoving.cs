using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Aspects;
using Unity.Transforms;
using UnityEngine;

// aspect of an enemy currently moving towards its next waypoint, as determined by the Moving tag
public readonly partial struct AspectDwarfMoving : IAspect
{
    private readonly RefRW<DwarfTarget> target;
    private readonly RefRW<TargetRot> rot;
    private readonly RefRO<EnemyID> id;
    private readonly RefRW<TagMoving> moving;

    // move enemy towards waypoint and rotate it gradually facing its movement direction
    public void Move(float deltaTime, ref LocalTransform transformAspect, Entity entity)
    {
        float3 dir = math.normalize(target.ValueRO.value - transformAspect.Position);
        transformAspect.Position += dir * deltaTime * 1;
        int newRot = Rotate();
        if (newRot != 0) transformAspect.Rotation = quaternion.Euler(new float3(0, math.radians(newRot), 0));
    }

    public int GetRot()
    {
        return rot.ValueRO.target;
    }

    public int GetSettingTarget()
    {
        return moving.ValueRO.settingTarget;
    }

    public void SetSettingTarget(int value)
    {
        moving.ValueRW.settingTarget = value;
    }

    // update stored rotation angle to move enemy gradually towards movement direction
    int Rotate()
    {
        int target = rot.ValueRO.target, cur = rot.ValueRO.cur;
        if (target == cur) return 0;
        if (target > cur)
        {
            if (target - cur < 180) rot.ValueRW.cur += 2;
            else if (cur == 0) rot.ValueRW.cur = 358;
            else rot.ValueRW.cur -= 2;
        }
        else
        {
            if (cur - target < 180) rot.ValueRW.cur -= 2;
            else if (cur == 358) rot.ValueRW.cur = 0;
            else rot.ValueRW.cur += 2;
        }
        return rot.ValueRO.cur;
    }

    public void SetRot(int rot)
    {
        this.rot.ValueRW.target = rot;
    }

    public float3 GetTarget()
    {
        return target.ValueRO.value;
    }

    public void SetTarget(float3 target, int2 key)
    {
        this.target.ValueRW.value = target;
        this.target.ValueRW.key = key;
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
