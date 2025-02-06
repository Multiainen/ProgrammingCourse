using Unity.Entities;
using UnityEngine;

// collection of tags and minor components specific to or primarily for enemies

// tag to designate enemy as moving towards a new waypoint (not at goal and not being moved by physics)
public struct TagMoving : IComponentData
{
    public int settingTarget;
}

// tag to designate enemy as needing position assignment (teleport)
public struct TagSetPos : IComponentData
{

}

// tag to designate enemy as having reached their final goal
public struct TagAtGoal : IComponentData
{

}

// tag to designate eenmy as needing initial waypoint target assignment
public struct TagFirstTarget : IComponentData
{

}

// tag for testing purposes
public struct TagTest : IComponentData
{

}

// tag to designate enemy as needing new waypoint target assignment
public struct TagNewTarget : IComponentData
{

}

// tag to designate enemy (or other entity) as newly spawned
public struct TagInitialize : IComponentData
{

}

// tag to designate enemy as dead and to be removed
public struct TagKillEnemy : IComponentData
{
    public float timer;
    public bool animTriggered;
}
