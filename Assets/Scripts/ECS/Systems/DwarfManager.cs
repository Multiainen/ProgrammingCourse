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

        SpawnEnemies();
        SpawnProjectiles();
        PlaceTower();
        UpdateChunk();
    }

    private void SpawnEnemies()
    {
        if (bridge.addEnemyList.Count > 0)
        {
            MapComponent mapComponent = SystemAPI.GetSingleton<MapComponent>();
            Entity itemDatabase;
            DynamicBuffer<HumonBufferElement> humonBuffer;
            Entity[] dworfs = new Entity[bridge.addEnemyList.Count];
            // instantiate requested amount of enemies
            for (int i = 0; i < bridge.addEnemyList.Count; i++)
            {
                itemDatabase = SystemAPI.GetSingletonEntity<HumonSpawner>();
                humonBuffer = SystemAPI.GetBuffer<HumonBufferElement>(itemDatabase);
                dworfs[i] = EntityManager.Instantiate(humonBuffer.ElementAt(bridge.addEnemyList[i].x).ItemEntity);
            }
            int index = 0;
            // enemy initialization operations
            foreach ((AspectDwarfInitialize dwarf, LocalTransform transform, PhysicsCollider collider, Entity entity) in SystemAPI.Query<AspectDwarfInitialize, LocalTransform, PhysicsCollider>().WithEntityAccess())
            {
                if (bridge.addEnemyList.Count == 0) { break; }
                for (int i = 0; i < dworfs.Length; i++)
                    if (entity.Equals(dworfs[i]))
                    { index = i; break; }

                // assign ID and initial stats for enemy
                curID = bridge.addEnemyList[index].y;
                dwarf.SetID(curID);
                dwarf.SetType(bridge.addEnemyList[index].x);
                dwarf.SetHP(bridge.enemyType[bridge.addEnemyList[index].x].hp);

                // determine spawn location and initial waypoint target, and assign them
                curLoc = bridge.startNodes[UnityEngine.Random.Range(0, bridge.startNodes.Length)];
                int2 nextLoc = bridge.openNodes[curLoc][UnityEngine.Random.Range(0, bridge.openNodes[curLoc].Count)];
                int path = UnityEngine.Random.Range(0, 3);
                dwarf.SetTarget(path, path, new float3(mapComponent.paths[mapComponent.pathIndices[path] + 1].x, 2.5f, mapComponent.paths[mapComponent.pathIndices[path] + 1].y), new float2(UnityEngine.Random.Range(-.8f, .8f), UnityEngine.Random.Range(-.8f, .8f)), mapComponent.pathIndices[path] + 1);

                // assign component to receive chunk, and set collision filters
                chunkCheck.Add(entity, new float4(curLoc.x, curLoc.y, 0, 0));
                collider.Value.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = (uint)bridge.collisionFilters[0].x,
                    CollidesWith = (uint)bridge.collisionFilters[0].y,
                    GroupIndex = bridge.collisionFilters[0].z
                });

                // move list along, finalize component assignment operations and set processed entity to change tags
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
    public void PlaceTower()
    {
        if (bridge.TowerList.Count > 0)
        {
            var itemDatabase = SystemAPI.GetSingletonEntity<TowerSpawner>();
            var itemBuffer = SystemAPI.GetBuffer<TowerBufferElement>(itemDatabase);
            Entity e = EntityManager.Instantiate(itemBuffer.ElementAt(bridge.TowerList[0].type).ItemEntity);
            foreach ((AspectTowerInit tower, LocalTransform transform, Entity entity) in SystemAPI.Query<AspectTowerInit, LocalTransform>().WithEntityAccess())
            {
                tower.Init(bridge.TowerList[0].pos, ResMgr.GenID());
            }
            EntityManager.RemoveComponent<TagInitialize>(e);
            EntityManager.AddComponent<TagSetPos>(e);
            bridge.TowerList.RemoveAt(0);
        }
    }

    private void SpawnProjectiles()
    {
        if (bridge.addProjectileList.Count > 0)
        {
            Entity itemDatabase;
            DynamicBuffer<TossBufferElement> tossBuffer;
            Entity[] projectiles = new Entity[bridge.addProjectileList.Count];
            // instantiate requested amount of projectiles
            for (int i = 0; i < bridge.addProjectileList.Count; i++)
            {
                itemDatabase = SystemAPI.GetSingletonEntity<TossSpawner>();
                tossBuffer = SystemAPI.GetBuffer<TossBufferElement>(itemDatabase);
                projectiles[i] = EntityManager.Instantiate(tossBuffer.ElementAt(bridge.addProjectileList[i].type).ItemEntity);
            }
            int index = 0;
            // projectile initialization operations
            foreach ((AspectProjectileLaunch launch, LocalTransform transform, PhysicsCollider collider, Entity entity) in SystemAPI.Query<AspectProjectileLaunch, LocalTransform, PhysicsCollider>().WithEntityAccess())
            {
                if (bridge.addProjectileList.Count == 0) { break; }
                for (int i = 0; i < projectiles.Length; i++)
                    if (entity.Equals(projectiles[i]))
                    { index = i; break; }
                // assign collision filters
                collider.Value.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = (uint)bridge.collisionFilters[1].x,
                    CollidesWith = (uint)bridge.collisionFilters[1].y,
                    GroupIndex = bridge.collisionFilters[1].z
                });

                // set initial force and location
                launch.SetPhysSpawn(bridge.addProjectileList[index].force, bridge.addProjectileList[index].loc);
                launch.SetType(bridge.addProjectileList[index].type);

                // move list along & set entity to be retagged
                retagEnemies.Push(entity);
            }

            // remove initialization tag
            while (retagEnemies.Count > 0)
            { EntityManager.RemoveComponent<TagInitialize>(retagEnemies.Pop()); }
            bridge.addProjectileList.Clear();
        }
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
