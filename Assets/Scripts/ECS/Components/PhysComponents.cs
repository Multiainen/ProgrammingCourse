using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// collection of physics-related components

// physics impulse to be added to current entity velocity
public struct PhysImpulse : IComponentData
{
    public float3 value;
}

// initial physics velocity and position to assign to a spawning physics object
public struct PhysSpawn : IComponentData
{
    public float3 force;
    public float3 pos;
}

// tag signifying entity can be targeted by physics influences
public struct PhysTarget : IComponentData
{

}

// data from physics collision event to be processed
public readonly struct PhysHit : IComponentData
{
    public readonly Entity hitter;
    public readonly float3 dir;
    public readonly float velocity;

    public PhysHit(Entity hitter, float3 dir, float velocity)
    {
        this.hitter = hitter;
        this.dir = dir;
        this.velocity = velocity;
    }
}

// manual simulated physics motion on entities without physics bodies
public struct ManualMotion : IComponentData
{
    public float3 value;
    public bool grounded;
}
