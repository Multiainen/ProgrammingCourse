using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

// collection of enemy-specific data components

// individual enemy ID
public struct TowerID : IComponentData
{
    public int value;
}

// current enemy stat values
public struct TowerComponent : IComponentData
{
    public float health;
    public float cooldown;
    public float shotHeight;
    public int projectileType;
    public int towerType;
}

public struct PositionComponent: IComponentData
{
    public float3 value;
}

[ChunkSerializable]
public struct TowerTargetsComponent : IComponentData
{
    public UnsafeQueue<Entity> targets;
}

[ChunkSerializable]
public struct TowerProjectilesToSpawn : IComponentData
{
    public UnsafeQueue<LaunchData> targets;
}

[ChunkSerializable]
public struct ExplosionsToSpawn : IComponentData
{
    public UnsafeQueue<ExplosionData> spawns;
}

public struct ExplosionData
{
    public float3 pos;
    public float force;
    public float directDamage;
}

public struct ExplosionComponent : IComponentData
{

}

public struct TowerTargetingTag : IComponentData
{

}

public struct LaunchData
{
    public float3 pos;
    public float3 force;
    public int type;
}

public struct TowerDataRef : IComponentData
{
    public BlobAssetReference<TowerDataContents> contents;
}

public struct TowerDataContents : IComponentData
{
    public BlobArray<int> projectile;
    public BlobArray<float> cooldown;

    public BlobArray<float> projectileMass;
    public BlobArray<float> projectileSharpness;
    public BlobArray<float> projectileRadius;
    public BlobArray<int> projectileBehaviour;
}

public struct GoldEarned : IComponentData
{
    public int value;
}


