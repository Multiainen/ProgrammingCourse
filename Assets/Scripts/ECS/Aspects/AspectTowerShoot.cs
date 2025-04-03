using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public readonly partial struct AspectTowerShoot : IAspect
{
    private readonly RefRO<TowerTargetingTag> tag;
    private readonly RefRO<TowerTargetsComponent> targets;

    public float3 LaunchData(ComponentLookup<DwarfTarget> dwarfTargets, ref MapRefComponentContents mapRef, float3 launchPoint, float colliderOffset)
    {
        if (targets.ValueRO.targets.Count < 1)
            return new float3(0);
        int furthest = dwarfTargets[targets.ValueRO.targets.Peek()].step;
        Entity bestTarget = targets.ValueRO.targets.Dequeue();
        while (targets.ValueRO.targets.Count > 0)
        {
            if (dwarfTargets[targets.ValueRO.targets.Peek()].step > furthest)
            {
                bestTarget = targets.ValueRO.targets.Peek();
                furthest = dwarfTargets[targets.ValueRO.targets.Dequeue()].step;
            }
            else
                targets.ValueRO.targets.Dequeue();

        }
        targets.ValueRO.targets.Clear();
        float startDistance = (math.length(new float3(mapRef.pathSteps[dwarfTargets[bestTarget].step].x, 2.5f, mapRef.pathSteps[dwarfTargets[bestTarget].step].y) - launchPoint) + 50) * .027f + dwarfTargets[bestTarget].distFromLast;
        int stepAdjustment = 1;
        while (startDistance > mapRef.pathStepLength[dwarfTargets[bestTarget].step + stepAdjustment])
        {
            startDistance -= mapRef.pathStepLength[dwarfTargets[bestTarget].step + stepAdjustment];
            stepAdjustment++;
        }
        float2 prelimTarget = Curves.Lerp(mapRef.pathSteps[dwarfTargets[bestTarget].step + stepAdjustment], mapRef.pathSteps[dwarfTargets[bestTarget].step + stepAdjustment + 1], startDistance / mapRef.pathStepLength[dwarfTargets[bestTarget].step + stepAdjustment]) + dwarfTargets[bestTarget].offset;
        float3 target = new float3(prelimTarget.x, 2.53f - colliderOffset * colliderOffset * .9f, prelimTarget.y);
        float distance = math.length(launchPoint - target);
        return Curves.CalculateLaunchDirection(launchPoint, target, 3.2f + colliderOffset + distance * 1.15f + (distance * distance * .003f));
    }
}
