using System.Collections;
using System.Collections.Generic;
using System.Numerics;
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
    private readonly RefRO<EnemyID> id;
    private readonly RefRW<TagMoving> moving;

    // move enemy towards waypoint and rotate it facing its movement direction; return true if end goal was reached
    public bool Move(float deltaTime, MapComponent map, ref LocalTransform transformAspect, Entity entity)
    {
        float2 newPos = new float2(transformAspect.Position.x, transformAspect.Position.z) - target.ValueRO.offset, tempPos = newPos;
        float stepLength;
        int waypoint = target.ValueRO.waypoint;
        while (deltaTime > 0)
        {
            newPos = Curves.QuadCurve(map.paths[waypoint], map.paths[waypoint + 1], map.paths[waypoint + 2], (target.ValueRO.step + 1) * .005f);
            stepLength = math.length(newPos - tempPos);
            if (deltaTime < stepLength)
            {
                newPos = tempPos + (newPos - tempPos) * deltaTime / stepLength;
                break;
            }
            else 
            {
                deltaTime -= stepLength;
                tempPos = newPos;
                target.ValueRW.step++;
            }
        }
        transformAspect.Rotation = quaternion.Euler(new float3(0, math.atan2(newPos.x - (transformAspect.Position.x - target.ValueRO.offset.x), newPos.y - (transformAspect.Position.z - target.ValueRO.offset.y)) + math.radians(90), 0));
        transformAspect.Position = new float3(newPos.x + target.ValueRO.offset.x, 2.5f, newPos.y + target.ValueRO.offset.y);

        if (target.ValueRO.curPath != target.ValueRO.targetPath)
        {
            int newPath = map.pathOrigins[target.ValueRO.targetPath];
            while (map.pathOrigins[newPath] != target.ValueRO.curPath)
            {
                if (map.pathOrigins[newPath] < 0)
                    break;
                newPath = map.pathOrigins[newPath];
            }
            if (map.pathOrigins[newPath] >= 0 && math.lengthsq(newPos - target.ValueRO.offset - map.paths[map.pathIndices[newPath]]) < .01f)
            {
                target.ValueRW.waypoint = map.pathIndices[newPath] + 1;
                target.ValueRW.step = 0;
            }
        }

        if (target.ValueRO.step >= 200)
        {
            target.ValueRW.waypoint += 2;
            target.ValueRW.step = 0;
            if (map.pathIndices[target.ValueRO.curPath + 1] <= target.ValueRO.waypoint)
            {
                return true;
            }
        }
        return false;   
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

    public int2 GetTargetKey()
    {
        return target.ValueRO.key;
    }

    public int GetID()
    {
        return id.ValueRO.value;
    }
}
