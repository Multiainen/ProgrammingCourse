using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// aspect for a projectile that's currently launching, as determined by the Initialize tag and Projectile component
public readonly partial struct AspectTowerInit : IAspect
{
    private readonly RefRW<TowerID> tower;
    private readonly RefRO<TagInitialize> init;
    private readonly RefRW<PositionComponent> position;

    // set initial velocity and position of projectile
    public void Init(float3 pos, int id)
    {
        tower.ValueRW.value = id;
        position.ValueRW.value = pos;
    }
}

public readonly partial struct AspectTowerPosition : IAspect
{
    private readonly RefRW<TowerID> tower;
    private readonly RefRO<TagSetPos> init;
    private readonly RefRW<PositionComponent> position;

    public void AssignPos(ref LocalTransform transform)
    {
        transform.Position = position.ValueRO.value;
    }
}

public readonly partial struct AspectTower : IAspect
{
    private readonly RefRO<TowerID> id;
    private readonly RefRW<TowerComponent> tower;
    private readonly RefRO<TagAtGoal> tag;

    // set initial velocity and position of projectile
    public bool Cooldown(float time)
    {
        tower.ValueRW.cooldown -= time;
        if (tower.ValueRW.cooldown < 0) return true;
        return false;
    }
}
