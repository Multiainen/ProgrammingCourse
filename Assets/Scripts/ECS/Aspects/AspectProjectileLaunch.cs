using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// aspect for a projectile that's currently launching, as determined by the Initialize tag and Projectile component
public readonly partial struct AspectProjectileLaunch : IAspect
{
    private readonly RefRW<TagProjectile> projectile;
    private readonly RefRO<TagInitialize> init;
    private readonly RefRW<PhysSpawn> PhysSpawn;
    private readonly RefRW<PhysTarget> physTarget;

    // set initial velocity and position of projectile
    public void SetPhysSpawn(float3 force, float3 pos, int type)
    {
        PhysSpawn.ValueRW.force = force;
        PhysSpawn.ValueRW.pos = pos;
        projectile.ValueRW.type = type;
    }

    public void SetType(int type)
    {
        physTarget.ValueRW.type = type;
    }
}
