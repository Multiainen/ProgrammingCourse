using Unity.Entities;
using UnityEngine;

// collection of tags and minor components specific to or primarily for enemies

// tag to designate enemy as needing position assignment (teleport)
public struct TagSetPos : IComponentData
{

}

// tag to designate enemy as having reached their final goal
public struct TagAtGoal : IComponentData
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
