using Unity.Entities;
using UnityEngine;

// aspect for an enemy hit by a physical projectile, as determined by the PhysHit component
public readonly partial struct AspectDwarfHit : IAspect
{
    private readonly RefRW<PhysHit> hit;
    private readonly RefRW<DwarfData> data;

    public float ApplyPhysHitDamage(float time)
    {
        data.ValueRW.health -= (hit.ValueRO.velocity / time);
        return data.ValueRO.health;
    }
    public float GetHP()
    {
        return data.ValueRO.health;
    }
}
