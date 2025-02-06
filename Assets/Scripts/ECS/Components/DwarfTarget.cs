using Unity.Entities;
using UnityEngine;

// component containing current, previous and next upcoming waypoint target of an enemy
// previous waypoint info needed to calculate current lane segment dimensions
// next waypoint info needed for projectile target leading (if enemy is set to reach current waypoint before projectile impact)
public struct DwarfTarget : IComponentData
{
    public Unity.Mathematics.float3 value;
    public Unity.Mathematics.int2 key;
    public Unity.Mathematics.float3 nextValue;
    public Unity.Mathematics.int2 nextKey;
    public Unity.Mathematics.float3 prevValue;
    public Unity.Mathematics.int2 prevKey;
}
