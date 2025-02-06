using Unity.Entities;
using UnityEngine;

// collection of enemy-specific data components

// individual enemy ID
public struct EnemyID : IComponentData
{
    public int value;
}

// current enemy stat values
public struct DwarfData : IComponentData
{
    public float health;
}
