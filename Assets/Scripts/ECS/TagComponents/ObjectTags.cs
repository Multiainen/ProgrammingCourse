using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// collection of tag components for non-enemy entities

// tag signifying entity is a terrain object
public struct TagTerrain : IComponentData
{

}

// tag signifying entity is the floor of the level
public struct TagFloor : IComponentData
{

}

// tag signifying entity is a projectile
public struct TagProjectile : IComponentData
{
    public float despawnTimer;
    public int type;
}

// tag signifying entity is destined for removal
public struct TagRemoveObject : IComponentData
{

}

// tag signifying entity is part of foliage
public struct TagFoliage : IComponentData
{

}

public struct TagSetGeneric : IComponentData
{
    public float3 pos;
    public float rot;
    public float scale;
}

public struct TagGeneric : IComponentData
{

}

