using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// aspect for a projectile that's currently launching, as determined by the Initialize tag and Projectile component
public readonly partial struct AspectProjectileLaunch : IAspect
{
    private readonly RefRO<TagProjectile> projectile;
    private readonly RefRO<TagInitialize> init;
    private readonly RefRW<PhysSpawn> PhysSpawn;

    // set initial velocity and position of projectile
    public void SetPhysSpawn(float3 force, float3 pos)
    {
        PhysSpawn.ValueRW.force = force;
        PhysSpawn.ValueRW.pos = pos;
    }
}
