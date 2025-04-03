using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// collection of physics-related components

// physics impulse to be added to current entity velocity
public struct PhysImpulse : IComponentData
{
    public readonly float3 value;

    public PhysImpulse(float3 value) {  this.value = value; }
}

// initial physics velocity and position to assign to a spawning physics object
public struct PhysSpawn : IComponentData
{
    public float3 force;
    public float3 pos;
    public int type;
}

// tag signifying entity can be targeted by physics influences
public struct PhysTarget : IComponentData
{
    public int type;
}

// data from physics collision event to be processed
public readonly struct PhysHit : IComponentData
{
    public readonly float3 dir;
    public readonly Entity hitter;
    public readonly float damage;

    public PhysHit(Entity hitter, float3 dir, float damage)
    {
        this.hitter = hitter;
        this.dir = dir;
        this.damage = damage;
    }
}

// manual simulated physics motion on entities without physics bodies
public struct ManualMotion : IComponentData
{
    public float3 value;
    public bool grounded;
    public bool force;
    public bool offPath;
}

public struct StoredCollider : IComponentData
{
    public BlobAssetReference<Unity.Physics.Collider> Collider;
}
