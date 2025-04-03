using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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

public readonly partial struct AspectProjectileImpulse : IAspect
{
    private readonly RefRW<TagProjectile> projectile;
    private readonly RefRO<PhysImpulse> impulse;

    public void ApplyImpulse(ref PhysicsVelocity velocity)
    {
        velocity.Linear *= impulse.ValueRO.value;
    }

    public void SetDecayTimer(float time)
    {
        projectile.ValueRW.despawnTimer = .1f;
    }
}
