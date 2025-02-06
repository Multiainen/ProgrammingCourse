using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

// managed system for handling DOTS code that can't intuitively be transferred into the unmanaged realm
// some of the functions in here may be placeholders that are moved to unmanaged systems later
public partial class DwarfManager : SystemBase
{
    ECSBridge bridge; // MonoBehaviour bridge class
    EntitiesGraphicsSystem hybridRenderer;
    int curID; // ID currently being processed
    int2 curLoc; // location currently being processed
    Stack<Entity> retagEnemies = new Stack<Entity>(); // stack for enemies to be retagged (can't be done within foreach query)
    Stack<Entity> retagEnemiesAlt = new Stack<Entity>(); // alternative retagging stack if foreach can have two retagging outcomes
    Dictionary<Entity, float4> chunkCheck = new Dictionary<Entity, float4>(); // entities to receive or update their chunk assignment, and their previous/current positions
    public EndSimulationEntityCommandBufferSystem _endSimulationEcbSystem;
    public int mapXDivider = 15, mapYDivider = 15, mapCentreX = 50, mapCentreY = 50; // map centre location and quarter dividers for chunk assignment

    protected override void OnCreate()
    {
        _endSimulationEcbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // if the ECS bridge reference isn't assigned yet, get it and do initial operations
        if (!bridge)
        {
            bridge = GameObject.Find("Root").GetComponent<ECSBridge>();
            hybridRenderer = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            // set collision filters for terrain objects and floor
            foreach ((TagTerrain terrain, PhysicsCollider collider, Entity entity) in SystemAPI.Query<TagTerrain, PhysicsCollider>().WithEntityAccess())
            {
                collider.Value.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = (uint)bridge.collisionFilters[2].x,
                    CollidesWith = (uint)bridge.collisionFilters[2].y,
                    GroupIndex = bridge.collisionFilters[2].z
                });
            }
            foreach ((TagFloor floor, PhysicsCollider collider, Entity entity) in SystemAPI.Query<TagFloor, PhysicsCollider>().WithEntityAccess())
            {
                collider.Value.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = (uint)bridge.collisionFilters[3].x,
                    CollidesWith = (uint)bridge.collisionFilters[3].y,
                    GroupIndex = bridge.collisionFilters[3].z
                });
            }
        }
        // enemy spawning
        if (bridge.addEnemyList.Count > 0)
        {
            // instantiate requested amount of enemies
            for (int i = 0; i < bridge.addEnemyList.Count; i++)
            {
                if (bridge.addEnemyList[i].x == 0) EntityManager.Instantiate(SystemAPI.GetSingleton<HumonSpawner>().prefab);
            }
            // enemy initialization operations
            foreach ((AspectDwarfInitialize dwarf, LocalTransform transform, PhysicsCollider collider, Entity entity) in SystemAPI.Query<AspectDwarfInitialize, LocalTransform, PhysicsCollider>().WithEntityAccess())
            {
                if (bridge.addEnemyList.Count == 0) { break; }

                // assign ID and initial stats for enemy
                curID = bridge.addEnemyList[0].y;
                dwarf.SetID(curID);
                dwarf.SetHP(bridge.enemyType[bridge.addEnemyList[0].x].hp);

                // determine spawn location and initial waypoint target, and assign them
                curLoc = bridge.startNodes[UnityEngine.Random.Range(0, bridge.startNodes.Length)];
                int2 nextLoc = bridge.openNodes[curLoc][UnityEngine.Random.Range(0, bridge.openNodes[curLoc].Count)];
                dwarf.SetTarget(new float3(curLoc.x + UnityEngine.Random.Range(-.8f, .8f), 0, curLoc.y + UnityEngine.Random.Range(-.8f, .8f)), curLoc, new float3(nextLoc.x + UnityEngine.Random.Range(-.8f, .8f), 0, nextLoc.y + UnityEngine.Random.Range(-.8f, .8f)), nextLoc);
                
                // assign component to receive chunk, and set collision filters
                chunkCheck.Add(entity, new float4(curLoc.x, curLoc.y, 0, 0));
                collider.Value.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = (uint)bridge.collisionFilters[0].x,
                    CollidesWith = (uint)bridge.collisionFilters[0].y,
                    GroupIndex = bridge.collisionFilters[0].z
                }) ;

                // move list along, finalize component assignment operations and set processed entity to change tags
                bridge.addEnemyList.RemoveAt(0);
                EntityManager.SetComponentData(entity, collider);
                retagEnemies.Push(entity);
            }
            // change Initialize tag to Set Pos tag, to teleport enemy to initial position
            while (retagEnemies.Count > 0)
            { EntityManager.RemoveComponent<TagInitialize>(retagEnemies.Peek()); EntityManager.AddComponent<TagSetPos>(retagEnemies.Pop()); }

            // assign chunks to newly spawned enemies
            AssignChunk();
            bridge.addEnemyList.Clear();
        }

        // projectile spawning
        if (bridge.addProjectileList.Count > 0)
        {
            // instantiate requested amount of projectiles
            for (int i = 0; i < bridge.addProjectileList.Count; i++)
            {
                if (bridge.addProjectileList[i].type == 0) EntityManager.Instantiate(SystemAPI.GetSingleton<TossSpawner>().prefab);
            }

            // projectile initialization operations
            foreach ((AspectProjectileLaunch launch, LocalTransform transform, PhysicsCollider collider, Entity entity) in SystemAPI.Query<AspectProjectileLaunch, LocalTransform, PhysicsCollider>().WithEntityAccess())
            {
                if (bridge.addProjectileList.Count == 0) { break; }

                // assign collision filters
                collider.Value.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = (uint)bridge.collisionFilters[1].x,
                    CollidesWith = (uint)bridge.collisionFilters[1].y,
                    GroupIndex = bridge.collisionFilters[1].z
                });

                // set initial force and location
                launch.SetPhysSpawn(bridge.addProjectileList[0].force, bridge.addProjectileList[0].loc);

                // move list along & set entity to be retagged
                bridge.addProjectileList.RemoveAt(0);
                retagEnemies.Push(entity);
            }

            // remove initialization tag
            while (retagEnemies.Count > 0)
            { EntityManager.RemoveComponent<TagInitialize>(retagEnemies.Pop()); }
            bridge.addProjectileList.Clear();
        }

        // assigning new target waypoints for enemies
        foreach ((AspectDwarfNewTarget dwarf, LocalTransform transform, Entity entity) in SystemAPI.Query<AspectDwarfNewTarget, LocalTransform>().WithEntityAccess())
        {
            // enemy already has current and next target waypoint, so get those
            curLoc = dwarf.GetTargetKey();
            int2 nextLoc = dwarf.GetNextKey();

            // if reached end goal, set enemy to be retagged accordingly
            if (bridge.endNodes.Contains(curLoc)) retagEnemies.Push(entity);
            else
            {
                // if enemy will reach end goal next, set an abstract post-finish waypoint (intended to be at the transform of the goal object)
                if (bridge.endNodes.Contains(nextLoc))
                {
                    dwarf.SetTarget(new float3(bridge.ultimateNode.x, 0, bridge.ultimateNode.y), bridge.ultimateNode);
                }
                // if there are no ways forward on this path, return to a previous node
                else if (bridge.openNodes[nextLoc].Count < 1) dwarf.SetTarget(new float3(bridge.fallbackNodes[nextLoc].x + UnityEngine.Random.Range(-.8f, .8f), 0, bridge.fallbackNodes[nextLoc].y + UnityEngine.Random.Range(-.8f, .8f)), bridge.fallbackNodes[nextLoc]);
                else
                {
                    int index = UnityEngine.Random.Range(0, bridge.openNodes[nextLoc].Count);
                    dwarf.SetTarget(new float3(bridge.openNodes[nextLoc][index].x + UnityEngine.Random.Range(-.8f, .8f), 0, bridge.openNodes[nextLoc][index].y + UnityEngine.Random.Range(-.8f, .8f)), bridge.openNodes[nextLoc][index]);
                }
                // set rotation target for enemy
                if (dwarf.GetTargetKey().x != curLoc.x)
                {
                    if (dwarf.GetTargetKey().x > curLoc.x)
                        dwarf.SetRot(180);
                    else
                        dwarf.SetRot(0);
                }
                else
                {
                    if (dwarf.GetTargetKey().y > curLoc.y)
                        dwarf.SetRot(90);
                    else
                        dwarf.SetRot(270);
                }
                // mark target setting operation as complete, set chunk to recalculate and assign retagging for enemy that hasn't reached the end goal
                dwarf.SetSettingTarget(0);
                chunkCheck.Add(entity, new float4(dwarf.GetTarget().x, dwarf.GetTarget().z, transform.Position.x, transform.Position.y));
                retagEnemiesAlt.Push(entity);
            }
        }
        // set enemies at the end of the path to at goal
        while (retagEnemies.Count > 0)
        { EntityManager.RemoveComponent<TagMoving>(retagEnemies.Peek()); EntityManager.RemoveComponent<TagNewTarget>(retagEnemies.Peek()); EntityManager.AddComponent<TagAtGoal>(retagEnemies.Pop()); }
        // remove New Target tag from enemies that now have a new target
        while (retagEnemiesAlt.Count > 0)
        { EntityManager.RemoveComponent<TagNewTarget>(retagEnemiesAlt.Pop()); }

        // initial target assignment for enemies
        // same logic as new target setting above, but no need to check if reached/reaching end yet
        foreach ((AspectDwarfInitialTarget dwarf, LocalTransform transform, Entity entity) in SystemAPI.Query<AspectDwarfInitialTarget, LocalTransform>().WithEntityAccess())
        {
            curLoc = dwarf.GetTargetKey();
            int index = UnityEngine.Random.Range(0, bridge.openNodes[curLoc].Count);
            dwarf.SetTarget(new float3(bridge.openNodes[curLoc][index].x + UnityEngine.Random.Range(-.8f, .8f), 0, bridge.openNodes[curLoc][index].y + UnityEngine.Random.Range(-.8f, .8f)), bridge.openNodes[curLoc][index]);
            if (dwarf.GetTargetKey().x != curLoc.x)
            {
                if (dwarf.GetTargetKey().x > curLoc.x)
                {
                    dwarf.SetRot(180);
                }
                else
                {
                    dwarf.SetRot(0);
                }
            }
            else
            {
                if (dwarf.GetTargetKey().y > curLoc.y)
                {
                    dwarf.SetRot(90);
                }
                else
                {
                    dwarf.SetRot(270);
                }
            }
            chunkCheck.Add(entity, new float4(dwarf.GetTarget().x, dwarf.GetTarget().z, transform.Position.x, transform.Position.y));
            retagEnemies.Push(entity);
        }
        while (retagEnemies.Count > 0)
        { EntityManager.RemoveComponent<TagFirstTarget>(retagEnemies.Peek()); EntityManager.AddComponent<TagMoving>(retagEnemies.Pop()); }
        UpdateChunk();
    }

    // update chunk assignment for all entities in chunkCheck
    private void UpdateChunk()
    {
        foreach (KeyValuePair<Entity, float4> e in chunkCheck)
        {
            if ((int)(e.Value.x - mapCentreX) / mapXDivider != (int)(e.Value.z - mapCentreX) / mapXDivider || (int)(e.Value.y - mapCentreY) / mapXDivider != (int)(e.Value.w - mapCentreY) / mapYDivider)
            {
                int2 newChunk = new int2((int)(e.Value.x - mapCentreX) / mapXDivider, (int)(e.Value.y - mapCentreY) / mapYDivider);
                int2 oldChunk = new int2((int)(e.Value.z - mapCentreX) / mapXDivider, (int)(e.Value.w - mapCentreY) / mapYDivider);

                // remove old chunk tag
                if (oldChunk.x < 0)
                {
                    if (oldChunk.x == -2)
                    {
                        if (oldChunk.y < 0)
                        {
                            if (oldChunk.y == -2)
                                EntityManager.RemoveComponent<Chunk0>(e.Key);
                            else
                                EntityManager.RemoveComponent<Chunk1>(e.Key);
                        }
                        else
                        {
                            if (oldChunk.y == 0)
                                EntityManager.RemoveComponent<Chunk2>(e.Key);
                            else
                                EntityManager.RemoveComponent<Chunk3>(e.Key);
                        }
                    }
                    else
                    {
                        if (oldChunk.y < 0)
                        {
                            if (oldChunk.y == -2)
                                EntityManager.RemoveComponent<Chunk4>(e.Key);
                            else
                                EntityManager.RemoveComponent<Chunk5>(e.Key);
                        }
                        else
                        {
                            if (oldChunk.y == 0)
                                EntityManager.RemoveComponent<Chunk6>(e.Key);
                            else
                                EntityManager.RemoveComponent<Chunk7>(e.Key);
                        }
                    }
                }
                else
                {
                    if (oldChunk.x == 0)
                    {
                        if (oldChunk.y < 0)
                        {
                            if (oldChunk.y == -2)
                                EntityManager.RemoveComponent<Chunk8>(e.Key);
                            else
                                EntityManager.RemoveComponent<Chunk9>(e.Key);
                        }
                        else
                        {
                            if (oldChunk.y == 0)
                                EntityManager.RemoveComponent<Chunk10>(e.Key);
                            else
                                EntityManager.RemoveComponent<Chunk11>(e.Key);
                        }
                    }
                    else
                    {
                        if (oldChunk.y < 0)
                        {
                            if (oldChunk.y == -2)
                                EntityManager.RemoveComponent<Chunk12>(e.Key);
                            else
                                EntityManager.RemoveComponent<Chunk13>(e.Key);
                        }
                        else
                        {
                            if (oldChunk.y == 0)
                                EntityManager.RemoveComponent<Chunk14>(e.Key);
                            else
                                EntityManager.RemoveComponent<Chunk15>(e.Key);
                        }
                    }
                }
                // assign new chunk tag
                if (newChunk.x < 0)
                {
                    if (newChunk.x == -2)
                    {
                        if (newChunk.y < 0)
                        {
                            if (newChunk.y == -2)
                                EntityManager.AddComponent<Chunk0>(e.Key);
                            else
                                EntityManager.AddComponent<Chunk1>(e.Key);
                        }
                        else
                        {
                            if (newChunk.y == 0)
                                EntityManager.AddComponent<Chunk2>(e.Key);
                            else
                                EntityManager.AddComponent<Chunk3>(e.Key);
                        }
                    }
                    else
                    {
                        if (newChunk.y < 0)
                        {
                            if (newChunk.y == -2)
                                EntityManager.AddComponent<Chunk4>(e.Key);
                            else
                                EntityManager.AddComponent<Chunk5>(e.Key);
                        }
                        else
                        {
                            if (newChunk.y == 0)
                                EntityManager.AddComponent<Chunk6>(e.Key);
                            else
                                EntityManager.AddComponent<Chunk7>(e.Key);
                        }
                    }
                }
                else
                {
                    if (newChunk.x == 0)
                    {
                        if (newChunk.y < 0)
                        {
                            if (newChunk.y == -2)
                                EntityManager.AddComponent<Chunk8>(e.Key);
                            else
                                EntityManager.AddComponent<Chunk9>(e.Key);
                        }
                        else
                        {
                            if (newChunk.y == 0)
                                EntityManager.AddComponent<Chunk10>(e.Key);
                            else
                                EntityManager.AddComponent<Chunk11>(e.Key);
                        }
                    }
                    else
                    {
                        if (newChunk.y < 0)
                        {
                            if (newChunk.y == -2)
                                EntityManager.AddComponent<Chunk12>(e.Key);
                            else
                                EntityManager.AddComponent<Chunk13>(e.Key);
                        }
                        else
                        {
                            if (newChunk.y == 0)
                                EntityManager.AddComponent<Chunk14>(e.Key);
                            else
                                EntityManager.AddComponent<Chunk15>(e.Key);
                        }
                    }
                }
            }
        }
        chunkCheck.Clear();
    }

    // assign chunk for entities in chunkCheck WITHOUT removing potential previous chunk tag
    private void AssignChunk()
    {
        foreach (KeyValuePair<Entity, float4> e in chunkCheck)
        {
            int2 newChunk = new int2((int)(e.Value.x - mapCentreX) / mapXDivider, (int)(e.Value.y - mapCentreY) / mapYDivider);
            // assign new chunk tag
            if (newChunk.x < 0)
            {
                if (newChunk.x == -2)
                {
                    if (newChunk.y < 0)
                    {
                        if (newChunk.y == -2)
                            EntityManager.AddComponent<Chunk0>(e.Key);
                        else
                            EntityManager.AddComponent<Chunk1>(e.Key);
                    }
                    else
                    {
                        if (newChunk.y == 0)
                            EntityManager.AddComponent<Chunk2>(e.Key);
                        else
                            EntityManager.AddComponent<Chunk3>(e.Key);
                    }
                }
                else
                {
                    if (newChunk.y < 0)
                    {
                        if (newChunk.y == -2)
                            EntityManager.AddComponent<Chunk4>(e.Key);
                        else
                            EntityManager.AddComponent<Chunk5>(e.Key);
                    }
                    else
                    {
                        if (newChunk.y == 0)
                            EntityManager.AddComponent<Chunk6>(e.Key);
                        else
                            EntityManager.AddComponent<Chunk7>(e.Key);
                    }
                }
            }
            else
            {
                if (newChunk.x == 0)
                {
                    if (newChunk.y < 0)
                    {
                        if (newChunk.y == -2)
                            EntityManager.AddComponent<Chunk8>(e.Key);
                        else
                            EntityManager.AddComponent<Chunk9>(e.Key);
                    }
                    else
                    {
                        if (newChunk.y == 0)
                            EntityManager.AddComponent<Chunk10>(e.Key);
                        else
                            EntityManager.AddComponent<Chunk11>(e.Key);
                    }
                }
                else
                {
                    if (newChunk.y < 0)
                    {
                        if (newChunk.y == -2)
                            EntityManager.AddComponent<Chunk12>(e.Key);
                        else
                            EntityManager.AddComponent<Chunk13>(e.Key);
                    }
                    else
                    {
                        if (newChunk.y == 0)
                            EntityManager.AddComponent<Chunk14>(e.Key);
                        else
                            EntityManager.AddComponent<Chunk15>(e.Key);
                    }
                }
            }
        }
        chunkCheck.Clear();
    }
}
