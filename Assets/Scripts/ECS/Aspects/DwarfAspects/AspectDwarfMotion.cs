using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

// aspect of an enemy currently in simulated motion, as determined by the Enemy Motion component
public readonly partial struct AspectDwarfMotion : IAspect
{
    private readonly RefRW<DwarfData> dwarfData;
    private readonly RefRW<DwarfTarget> target;
    private readonly RefRO<EnemyID> id;
    private readonly RefRO<PhysTarget> physTarget;
    private readonly RefRW<ManualMotion> motion;

    // move enemy towards waypoint and rotate it facing its movement direction; return true if end goal was reached
    public bool Move(float moveDistance, MapComponent map, ref MapRefComponentContents mapRef, ref LocalTransform transformAspect, Entity entity)
    {
        float2 offset = target.ValueRO.offset;
        float2 newPos = new float2(transformAspect.Position.x, transformAspect.Position.z) - offset;
        int step = target.ValueRO.step;
        moveDistance += target.ValueRO.distFromLast;
        while (moveDistance > mapRef.pathStepLength[step])
        {
            moveDistance -= mapRef.pathStepLength[step];
            step++;
            if (step / 100 >= mapRef.pathIndices[target.ValueRO.curPath + 1] - 1)
            {
                newPos = mapRef.pathSteps[step / 200 * 200];
                transformAspect.Rotation = quaternion.Euler(new float3(0, math.atan2(newPos.x - (transformAspect.Position.x - offset.x), newPos.y - (transformAspect.Position.z - offset.y)), 0));
                transformAspect.Position = new float3(newPos.x + offset.x, 2.5f, newPos.y + offset.y);
                return true;
            }
        }
        newPos = mapRef.pathSteps[step] + (mapRef.pathSteps[step + 1] - mapRef.pathSteps[step]) * moveDistance / mapRef.pathStepLength[step];
        transformAspect.Rotation = quaternion.Euler(new float3(0, math.atan2(newPos.x - (transformAspect.Position.x - offset.x), newPos.y - (transformAspect.Position.z - offset.y)), 0));
        transformAspect.Position = new float3(newPos.x + offset.x, 2.5f, newPos.y + offset.y);
        target.ValueRW.distFromLast = moveDistance;
        target.ValueRW.step = step; 

        if (target.ValueRO.curPath != target.ValueRO.targetPath)
        {
            int newPath = mapRef.pathOrigins[target.ValueRO.targetPath];
            while (mapRef.pathOrigins[newPath] != target.ValueRO.curPath)
            {
                if (mapRef.pathOrigins[newPath] < 0)
                    break;
                newPath = mapRef.pathOrigins[newPath];
            }
            if (mapRef.pathOrigins[newPath] >= 0 && math.lengthsq(newPos - offset - mapRef.paths[mapRef.pathIndices[newPath]]) < .01f)
            {
                //target.ValueRW.step = mapRef.pathIndices[newPath];
            }
        }
        return false;
    }

    public bool MoveToPath(float moveDistance, ref MapRefComponentContents mapRef, ref LocalTransform transformAspect)
    {
        int step = target.ValueRO.step;
        float2 offset = target.ValueRO.offset;
        float3 targetPos = new float3(mapRef.pathSteps[step].x + offset.x, 2.5f, mapRef.pathSteps[step].y + offset.y);

        if (math.length(targetPos - transformAspect.Position) < moveDistance)
        {
            transformAspect.Position = targetPos;
            return true;
        }
        transformAspect.Rotation = quaternion.Euler(new float3(0, math.atan2(targetPos.x - transformAspect.Position.x, targetPos.y - transformAspect.Position.z), 0));
        transformAspect.Position += math.normalize(targetPos - transformAspect.Position) * moveDistance;
        return false;
    }
    public float3 GetMotion(){ return motion.ValueRO.value; }
    public bool GetGrounded() { return motion.ValueRO.grounded;}
    public float GetHP() { return dwarfData.ValueRO.health; }

    public void AdjustMotion(float3 m) { motion.ValueRW.value += m; }
    public void SetMotion(float3 m) { motion.ValueRW.value = m; }
    public void MultiplyMotion(float m) { motion.ValueRW.value *= m; }
    public void SetGrounded(bool m) { motion.ValueRW.grounded = m;}
    public void AdjustHP(float m) { dwarfData.ValueRW.health += m; }
    public void SetHP(float m) { dwarfData.ValueRW.health = m; }

    public bool GetForceEnabled() { return motion.ValueRO.force; }

    public void SetForceEnabled(bool set) { motion.ValueRW.force = set; }

    public bool GetOffPath() { return motion.ValueRO.offPath; }

    public void SetOffPath(bool set) { motion.ValueRW.offPath = set; }

    public bool GetAtGoal() { return target.ValueRO.atGoal; }

    public void SetAtGoal(bool set) { target.ValueRW.atGoal = set; }

}
