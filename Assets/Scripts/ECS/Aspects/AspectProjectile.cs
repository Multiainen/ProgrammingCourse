using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// aspect for a projectile, as determined by the Projectile component
public readonly partial struct AspectProjectile : IAspect
{
    private readonly RefRW<TagProjectile> projectile;

    public void AdvanceTimer(float time)
    {
        projectile.ValueRW.despawnTimer -= time;
    }

    public float GetTimer()
    {
        return projectile.ValueRO.despawnTimer;
    }
}
