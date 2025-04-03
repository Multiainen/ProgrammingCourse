using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// component containing current, previous and next upcoming waypoint target of an enemy
// previous waypoint info needed to calculate current lane segment dimensions
// next waypoint info needed for projectile target leading (if enemy is set to reach current waypoint before projectile impact)
public struct DwarfTarget : IComponentData
{
    public float2 offset;
    public int targetPath;
    public int curPath;
    public int step;
    public float distFromLast;
    public bool atGoal;
}
