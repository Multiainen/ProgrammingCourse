using Unity.Collections;
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
    private readonly RefRW<TowerComponent> towerInfo;
    private readonly RefRW<TowerTargetsComponent> targets;

    // set initial values of tower
    public void Init(float3 pos, int id, int type)
    {
        tower.ValueRW.value = id;
        position.ValueRW.value = pos;
        targets.ValueRW.targets = new UnsafeQueue<Entity>(Allocator.Persistent);
        towerInfo.ValueRW.health = 100;
        towerInfo.ValueRW.shotHeight = 5;
        towerInfo.ValueRW.towerType = type;
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
