using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
}

public struct PositionComponent: IComponentData
{
    public float3 value;
}
