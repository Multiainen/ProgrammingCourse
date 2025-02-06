using Unity.Entities;
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
}

// tag signifying entity is destined for removal
public struct TagRemoveObject : IComponentData
{

}

