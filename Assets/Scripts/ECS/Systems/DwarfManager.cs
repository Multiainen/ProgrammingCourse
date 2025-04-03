using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Collections;
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
    Dictionary<Entity, float4> chunkCheck = new Dictionary<Entity, float4>(); // entities to receive or update their chunk assignment, and their previous/current positions
    public EndSimulationEntityCommandBufferSystem _endSimulationEcbSystem;
    public int mapXDivider = 15, mapYDivider = 15, mapCentreX = 50, mapCentreY = 50; // map centre location and quarter dividers for chunk assignment

    protected override void OnCreate()
    {
        _endSimulationEcbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        TowerProjectilesToSpawn projectileSpawn;
        // if the ECS bridge reference isn't assigned yet, get it and do initial operations
        if (!bridge)
        {
            bridge = GameObject.Find("Root").GetComponent<ECSBridge>();
            hybridRenderer = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        }
        // enemy spawning
        else if (SystemAPI.TryGetSingleton<TowerProjectilesToSpawn>(out projectileSpawn))
        {
            SpawnEnemies();
            SpawnProjectiles();
            SpawnProjectilesUnmanaged(projectileSpawn);
            PlaceTower();
            PlaceOtherBuilding();
            //UpdateChunk();
            UpdateVerts();
            SpawnExplosions();
            KillEnemies();
            PlaySounds();
            ProcessRaycast();
            if (ResMgr.spawnResources) SpawnResources();
        }
    }

    private void ProcessRaycast()
    {
        if (bridge.mouseRC.transform)
        {
            MapRefComponent mapRef;
            if (!SystemAPI.TryGetSingleton<MapRefComponent>(out mapRef))
            {
                return;
            }
            ref MapRefComponentContents contents = ref mapRef.contents.Value;
            if (contents.pathMap[(int)bridge.mouseRC.point.x * (ResMgr.mapHeight + 1) + (int)bridge.mouseRC.point.z] > 0)
                bridge.mouseOnpath = true;
            else
                bridge.mouseOnpath = false;
        }
    }

    private void SpawnResources()
    {
        Entity foliageSpawner = SystemAPI.GetSingletonEntity<FoliageSpawner>();
        var foliageBuffer = SystemAPI.GetBuffer<FoliageBufferElement>(foliageSpawner);
        Entity curEntity = new Entity();
        float seedOffset = 0;
        for (int i = 0; i < ResMgr.resDepots.Length; i++)
            for (int j = 0; j < ResMgr.resDepots[i].Length; j++)
            {
                seedOffset += 1.742728f;
                foliageBuffer = SystemAPI.GetBuffer<FoliageBufferElement>(foliageSpawner);
                EntityManager.Instantiate(foliageBuffer.ElementAt(i + 3).ItemEntity);
                foreach ((LocalTransform t, TagGeneric tag, Entity e) in SystemAPI.Query<LocalTransform, TagGeneric>().WithEntityAccess())
                {
                    curEntity = e;
                }
                EntityManager.RemoveComponent<TagGeneric>(curEntity); EntityManager.SetComponentData<LocalTransform>(curEntity, new LocalTransform { Position = new float3(ResMgr.resDepots[i][j].x, 0, ResMgr.resDepots[i][j].y), Rotation = quaternion.Euler(0, noise.snoise(new float2(ResMgr.generalSeed + seedOffset, ResMgr.generalSeed + seedOffset * 1.137f)) * 180, 0), Scale = 1 });
            }
        foliageBuffer = SystemAPI.GetBuffer<FoliageBufferElement>(foliageSpawner);
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 500; j++)
                EntityManager.Instantiate(foliageBuffer.ElementAt(i).ItemEntity);
        ResMgr.spawnResources = false;
    }
    private void PlaySounds()
    {
        MapComponent map;
        float time = SystemAPI.Time.DeltaTime;
        if (!SystemAPI.TryGetSingleton<MapComponent>(out map) || !map.soundQueue.IsCreated) return;
        for (int i = 0; i < ResMgr.soundsPlaying.Length; i++)
            for (int j = 0; j < ResMgr.soundsPlaying[i].Count; j++)
            {
                ResMgr.soundsPlaying[i][j] = new AudioStats(ResMgr.soundsPlaying[i][j].pos, ResMgr.soundsPlaying[i][j].time + time);
                if (ResMgr.soundsPlaying[i][j].time > .5f)
                { ResMgr.soundsPlaying[i].RemoveAt(j); j--; continue; }
            }
        while (map.soundQueue.Count > 0)
        {
            int index = map.soundQueue.Peek().index;
            int counter = 0;
            float3 pos = map.soundQueue.Peek().pos;
            for (int i = 0; i < ResMgr.soundsPlaying[index].Count; i++)
            {
                if (math.lengthsq(ResMgr.soundsPlaying[index][i].pos - pos) < 100)
                    counter++;
            }
            if (counter < 3)
            {
                FMODUnity.RuntimeManager.PlayOneShot("event:/" + ResMgr.soundBank[index], pos);
                ResMgr.soundsPlaying[index].Add(new AudioStats(pos, 0));
            }
            map.soundQueue.Dequeue();
        }
    }

    private void KillEnemies()
    {
        foreach ((TagAtGoal tag, DwarfData dwarf, Entity entity) in SystemAPI.Query<TagAtGoal, DwarfData>().WithEntityAccess())
        {
            retagEnemies.Push(entity);
        }
        while (retagEnemies.Count > 0)
        {
            EntityManager.DestroyEntity(retagEnemies.Pop());
            bridge.ui.UpdateDisplay(0, --ResMgr.resources[0]);
            if (ResMgr.resources[0] < 1)
                bridge.ui.GameOver();
        }
    }

    private void SpawnExplosions()
    {
        ExplosionsToSpawn spawn;
        if (!SystemAPI.TryGetSingleton<ExplosionsToSpawn>(out spawn) || !spawn.spawns.IsCreated) return;
        while (spawn.spawns.Count > 0)
        {
            FMODUnity.RuntimeManager.PlayOneShot("event:/Explosion", spawn.spawns.Peek().pos + (new float3(495, 9, 495) - spawn.spawns.Peek().pos) * .5f);
            bridge.spawnExplosionList.Add(spawn.spawns.Dequeue());
        }
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
                int path = bridge.spawnPaths[UnityEngine.Random.Range(0, bridge.spawnPaths.Length)];
                dwarf.SetTarget(path, path, new float2(UnityEngine.Random.Range(-.8f, .8f), UnityEngine.Random.Range(-.8f, .8f)));

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
            //AssignChunk();
            bridge.addEnemyList.Clear();
        }
    }

    private void UpdateVerts()
    {
        foreach (AspectMap map in SystemAPI.Query<AspectMap>())
        {
            int count = map.GetVertQueueLength();
            if (count < 1) return;
            count /= 100;
            int2 cur, curInChunk;
            float2 curPoint;
            int chunkIndex;
            Vector3[] verts;
            float[] curHeights;
            for (int i = 0; i <= count; i++)
            {
                bridge.ui.UpdateDisplay(3, ++ResMgr.resources[3]);
                curPoint = map.GetNextVert();
                cur = new int2((int)curPoint.x, (int)curPoint.y);
                if (cur.x <= 5 || cur.x >= ResMgr.mapWidth - 5 || cur.y <= 5 || cur.y >= ResMgr.mapHeight - 5)
                    continue;
                curHeights = DistributeHeight(curPoint, cur, map);
                if (curHeights == null)
                {
                    List<int2> options = new List<int2>();
                    int spreadCounter = 1;
                    while (options.Count < 1 && spreadCounter < 5)
                    {
                        for (int x = cur.x - spreadCounter; x <= cur.x + spreadCounter; x++)
                            for (int y = cur.y - spreadCounter; y <= cur.y + spreadCounter; y++)
                                if (map.GetMapData(cur.x * (ResMgr.mapHeight + 1) + cur.y) < 2.495f)
                                    options.Add(new int2(x, y));
                        spreadCounter++;
                    }
                    if (options.Count == 0)
                        continue;
                    cur = options[UnityEngine.Random.Range(0, options.Count)];
                }

                // TRANSITION INTO SPREADING HEIGHT ACROSS TILE

                curInChunk = new int2(cur.x % bridge.mapChunkSize + 1, cur.y % bridge.mapChunkSize + 1);
                chunkIndex = cur.x / bridge.mapChunkSize * bridge.mapChunksY + cur.y / bridge.mapChunkSize;
                verts = bridge.terrainMeshes[chunkIndex].vertices;

                //curHeight = verts[curInChunk.x * (bridge.mapChunkSize + 3) + curInChunk.y].y + .02f;
                //map.SetMapData(cur.x * (ResMgr.mapHeight + 1) + cur.y, curHeight);
                //verts[curInChunk.x * (bridge.mapChunkSize + 3) + curInChunk.y] += new Vector3(0, .02f);
                bridge.terrainMeshes[chunkIndex].vertices = verts;
                bridge.terrainMeshes[chunkIndex] = UpdateChunkMesh(bridge.terrainMeshes[chunkIndex]);
                bridge.renderMeshArray.MeshReferences[chunkIndex].Value = bridge.terrainMeshes[chunkIndex];

                if (curInChunk.x < 3 && cur.x > 2)
                {
                    verts = bridge.terrainMeshes[chunkIndex - bridge.mapChunksY].vertices;
                    if (curInChunk.x == 1)
                        verts[(bridge.mapChunkSize + 1) * (bridge.mapChunkSize + 3) + curInChunk.y] += new Vector3(0, .02f);
                    else
                        verts[(bridge.mapChunkSize + 2) * (bridge.mapChunkSize + 3) + curInChunk.y] += new Vector3(0, .02f);
                    bridge.terrainMeshes[chunkIndex - bridge.mapChunksY].vertices = verts;
                    bridge.terrainMeshes[chunkIndex - bridge.mapChunksY] = UpdateChunkMesh(bridge.terrainMeshes[chunkIndex - bridge.mapChunksY]);
                    bridge.renderMeshArray.MeshReferences[chunkIndex - bridge.mapChunksY].Value = bridge.terrainMeshes[chunkIndex - bridge.mapChunksY];
                }
                else if (curInChunk.x > bridge.mapChunkSize - 1 && cur.x < ResMgr.mapWidth - 3)
                {
                    verts = bridge.terrainMeshes[chunkIndex + bridge.mapChunksY].vertices;
                    if (curInChunk.x == bridge.mapChunkSize + 1)
                        verts[bridge.mapChunkSize + 3 + curInChunk.y] += new Vector3(0, .02f);
                    else
                        verts[curInChunk.y] += new Vector3(0, .02f);
                    bridge.terrainMeshes[chunkIndex + bridge.mapChunksY].vertices = verts;
                    bridge.terrainMeshes[chunkIndex + bridge.mapChunksY] = UpdateChunkMesh(bridge.terrainMeshes[chunkIndex + bridge.mapChunksY]);
                    bridge.renderMeshArray.MeshReferences[chunkIndex + bridge.mapChunksY].Value = bridge.terrainMeshes[chunkIndex + bridge.mapChunksY];
                }
                if (curInChunk.y < 3 && cur.y > 2)
                {
                    verts = bridge.terrainMeshes[chunkIndex - 1].vertices;
                    if (curInChunk.y == 1)
                        verts[curInChunk.x * (bridge.mapChunkSize + 3) + bridge.mapChunkSize + 1] += new Vector3(0, .02f);
                    else
                        verts[curInChunk.x * (bridge.mapChunkSize + 3) + bridge.mapChunkSize + 2] += new Vector3(0, .02f);
                    bridge.terrainMeshes[chunkIndex - 1].vertices = verts;
                    bridge.terrainMeshes[chunkIndex - 1] = UpdateChunkMesh(bridge.terrainMeshes[chunkIndex - 1]);
                    bridge.renderMeshArray.MeshReferences[chunkIndex - 1].Value = bridge.terrainMeshes[chunkIndex - 1];
                }
                else if (curInChunk.y > bridge.mapChunkSize - 1 && cur.y < ResMgr.mapHeight - 3)
                {
                    verts = bridge.terrainMeshes[chunkIndex + 1].vertices;
                    if (curInChunk.y == bridge.mapChunkSize + 1)
                        verts[curInChunk.x * (bridge.mapChunkSize + 3) + 1] += new Vector3(0, .02f);
                    else
                        verts[curInChunk.x * (bridge.mapChunkSize + 3)] += new Vector3(0, .02f);
                    bridge.terrainMeshes[chunkIndex + 1].vertices = verts;
                    bridge.terrainMeshes[chunkIndex + 1] = UpdateChunkMesh(bridge.terrainMeshes[chunkIndex + 1]);
                    bridge.renderMeshArray.MeshReferences[chunkIndex + 1].Value = bridge.terrainMeshes[chunkIndex + 1];
                }
            }
        }
    }

    private float[] DistributeHeight(float2 curPoint, int2 cur, AspectMap map)
    {
        float[] ret = new float[4];
        float totalDist;

        if (map.GetMapData(cur.x * (ResMgr.mapHeight + 1) + cur.y) >= 2.495f)
        {
            if (map.GetMapData(cur.x * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
            {
                if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y) >= 2.495f)
                {
                    if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
                    {
                        return null;
                    }
                    else
                    {
                        return new float[] { 0, 0, 0, .02f };
                    }
                }
                else if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
                {
                    return new float[] { 0, 0, .02f, 0 };
                }
                else
                {
                    ret[2] = math.length(new int2(cur.x + 1, cur.y) - curPoint); totalDist = ret[2];
                    ret[3] = math.length(new int2(cur.x + 1, cur.y + 1) - curPoint); totalDist += ret[3];
                    for (int i = 0; i < ret.Length; i++)
                        ret[i] = ret[i] / totalDist * .02f;
                }
            }
            else if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y) >= 2.495f)
            {
                if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
                {
                    return new float[] { 0, .02f, 0, 0 };
                }
                else
                {
                    ret[1] = math.length(new int2(cur.x, cur.y + 1) - curPoint); totalDist = ret[1];
                    ret[3] = math.length(new int2(cur.x + 1, cur.y + 1) - curPoint); totalDist += ret[3];
                    for (int i = 0; i < ret.Length; i++)
                        ret[i] = ret[i] / totalDist * .02f;
                }
            }
            else if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
            {
                ret[1] = math.length(new int2(cur.x, cur.y + 1) - curPoint); totalDist = ret[1];
                ret[2] = math.length(new int2(cur.x + 1, cur.y) - curPoint); totalDist += ret[2];
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = ret[i] / totalDist * .02f;
            }
            else
            {
                ret[1] = math.length(new int2(cur.x, cur.y + 1) - curPoint); totalDist = ret[1];
                ret[2] = math.length(new int2(cur.x + 1, cur.y) - curPoint); totalDist += ret[2];
                ret[3] = math.length(new int2(cur.x + 1, cur.y + 1) - curPoint); totalDist += ret[3];
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = ret[i] / totalDist * .02f;
            }
        }
        else if (map.GetMapData(cur.x * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
        {
            if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y) >= 2.495f)
            {
                if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
                {
                    return new float[] { .02f, 0, 0, 0 };
                }
                else
                {
                    ret[0] = math.length(cur - curPoint); totalDist = ret[0];
                    ret[3] = math.length(new int2(cur.x + 1, cur.y + 1) - curPoint); totalDist += ret[3];
                    for (int i = 0; i < ret.Length; i++)
                        ret[i] = ret[i] / totalDist * .02f;
                }
            }
            else if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
            {
                ret[0] = math.length(cur - curPoint); totalDist = ret[0];
                ret[2] = math.length(new int2(cur.x + 1, cur.y) - curPoint); totalDist += ret[2];
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = ret[i] / totalDist * .02f;
            }
            else
            {
                ret[0] = math.length(cur - curPoint); totalDist = ret[0];
                ret[2] = math.length(new int2(cur.x + 1, cur.y) - curPoint); totalDist += ret[2];
                ret[3] = math.length(new int2(cur.x + 1, cur.y + 1) - curPoint); totalDist += ret[3];
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = ret[i] / totalDist * .02f;
            }
        }
        else if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y) >= 2.495f)
        {
            if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
            {
                ret[0] = math.length(cur - curPoint); totalDist = ret[0];
                ret[1] = math.length(new int2(cur.x, cur.y + 1) - curPoint); totalDist += ret[1];
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = ret[i] / totalDist * .02f;
            }
            else
            {
                ret[0] = math.length(cur - curPoint); totalDist = ret[0];
                ret[1] = math.length(new int2(cur.x, cur.y + 1) - curPoint); totalDist += ret[1];
                ret[3] = math.length(new int2(cur.x + 1, cur.y + 1) - curPoint); totalDist += ret[3];
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = ret[i] / totalDist * .02f;
            }
        }
        else if (map.GetMapData((cur.x + 1) * (ResMgr.mapHeight + 1) + cur.y + 1) >= 2.495f)
        {
            ret[0] = math.length(cur - curPoint); totalDist = ret[0];
            ret[1] = math.length(new int2(cur.x, cur.y + 1) - curPoint); totalDist += ret[1];
            ret[2] = math.length(new int2(cur.x + 1, cur.y) - curPoint); totalDist += ret[2];
            for (int i = 0; i < ret.Length; i++)
                ret[i] = ret[i] / totalDist * .02f;
        }
        else
        {
            ret[0] = math.length(cur - curPoint); totalDist = ret[0];
            ret[1] = math.length(new int2(cur.x, cur.y + 1) - curPoint); totalDist += ret[1];
            ret[2] = math.length(new int2(cur.x + 1, cur.y) - curPoint); totalDist += ret[2];
            ret[3] = math.length(new int2(cur.x + 1, cur.y + 1) - curPoint); totalDist += ret[3];
            for (int i = 0; i < ret.Length; i++)
                ret[i] = ret[i] / totalDist * .02f;
        }
        return ret;
    }

    private Mesh UpdateChunkMesh(Mesh mesh)
    {
        int[] conTris = new int[2904];
        for (int k = 0, l = 0; k < conTris.Length; k += 6, l++)
        {
            if (l % 23 == 22)
            {
                k -= 6;
                continue;
            }
            conTris[k] = l;
            conTris[k + 1] = l + 24;
            conTris[k + 2] = l + 23;
            conTris[k + 3] = l;
            conTris[k + 4] = l + 1;
            conTris[k + 5] = l + 24;
        }
        mesh.triangles = conTris;
        mesh.RecalculateNormals();
        conTris = new int[2400];
        for (int k = 0, l = 24; k < conTris.Length; k += 6, l++)
        {
            if (l % 23 > 20 || l % 23 == 0)
            {
                k -= 6;
                continue;
            }
            conTris[k] = l;
            conTris[k + 1] = l + 24;
            conTris[k + 2] = l + 23;
            conTris[k + 3] = l;
            conTris[k + 4] = l + 1;
            conTris[k + 5] = l + 24;
        }
        mesh.triangles = conTris;
        return mesh;
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
            foreach ((AspectTowerInit tower, LocalTransform transform, PhysicsCollider collider, Entity entity) in SystemAPI.Query<AspectTowerInit, LocalTransform, PhysicsCollider>().WithEntityAccess())
            {
                tower.Init(bridge.TowerList[0].pos, ResMgr.GenID(), bridge.TowerList[0].type);
                collider.Value.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = (uint)bridge.collisionFilters[4].x,
                    CollidesWith = (uint)bridge.collisionFilters[4].y,
                    GroupIndex = bridge.collisionFilters[4].z
                });
            }
            EntityManager.RemoveComponent<TagInitialize>(e);
            EntityManager.AddComponent<TagSetPos>(e);
            bridge.TowerList.RemoveAt(0);
        }
    }

    public void PlaceOtherBuilding()
    {
        if (bridge.OtherBuildingsList.Count > 0)
        {
            var itemDatabase = SystemAPI.GetSingletonEntity<TowerSpawner>();
            var itemBuffer = SystemAPI.GetBuffer<TowerBufferElement>(itemDatabase);
            Entity e = EntityManager.Instantiate(itemBuffer.ElementAt(bridge.OtherBuildingsList[0].type).ItemEntity);
            EntityManager.RemoveComponent<TagGeneric>(e); EntityManager.SetComponentData<LocalTransform>(e, new LocalTransform { Position = bridge.OtherBuildingsList[0].pos, Rotation = quaternion.Euler(0, 0, 0), Scale = 1 });
            bridge.OtherBuildingsList.RemoveAt(0);
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
                if (bridge.addProjectileList[index].behaviourType == 1)
                    collider.Value.Value.SetCollisionFilter(new CollisionFilter
                    {
                        BelongsTo = (uint)bridge.collisionFilters[3].x,
                        CollidesWith = (uint)bridge.collisionFilters[3].y,
                        GroupIndex = bridge.collisionFilters[3].z
                    });
                else
                collider.Value.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = (uint)bridge.collisionFilters[1].x,
                    CollidesWith = (uint)bridge.collisionFilters[1].y,
                    GroupIndex = bridge.collisionFilters[1].z
                });

                // set initial force and location
                launch.SetPhysSpawn(bridge.addProjectileList[index].force, bridge.addProjectileList[index].loc, bridge.addProjectileList[index].type);
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

    private void SpawnProjectilesUnmanaged(TowerProjectilesToSpawn spawn)
    {
        if (!spawn.targets.IsCreated || spawn.targets.IsEmpty()) return;
        Entity itemDatabase;
        DynamicBuffer<TossBufferElement> tossBuffer;
        Entity[] projectiles = new Entity[spawn.targets.Count];
        LaunchData[] launchData = new LaunchData[spawn.targets.Count];
        // instantiate requested amount of projectiles
        for (int i = 0; i < spawn.targets.Count; i++)
        {
            itemDatabase = SystemAPI.GetSingletonEntity<TossSpawner>();
            tossBuffer = SystemAPI.GetBuffer<TossBufferElement>(itemDatabase);
            projectiles[i] = EntityManager.Instantiate(tossBuffer.ElementAt(spawn.targets.Peek().type).ItemEntity);
            launchData[i] = spawn.targets.Dequeue();
        }
        int index = 0;
        // projectile initialization operations
        foreach ((AspectProjectileLaunch launch, LocalTransform transform, PhysicsCollider collider, Entity entity) in SystemAPI.Query<AspectProjectileLaunch, LocalTransform, PhysicsCollider>().WithEntityAccess())
        {
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
            launch.SetPhysSpawn(launchData[index].force, launchData[index].pos, launchData[index].type);
            launch.SetType(launchData[index].type);

            // move list along & set entity to be retagged
            retagEnemies.Push(entity);
        }

        // remove initialization tag
        while (retagEnemies.Count > 0)
        { EntityManager.RemoveComponent<TagInitialize>(retagEnemies.Pop()); }
        spawn.targets.Clear();
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
