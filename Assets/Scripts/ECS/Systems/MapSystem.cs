using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public partial class MapSystem : SystemBase
{
    ECSBridge bridge;
    EntitiesGraphicsSystem hybridRenderer;
    float2 seed;
    public GameObject[] terrain;
    public GameObject[] pathObjects;

    protected override void OnUpdate()
    {
        // if the ECS bridge reference isn't assigned yet, get it 
        if (!bridge)
        {
            bridge = GameObject.Find("Root").GetComponent<ECSBridge>();
            hybridRenderer = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            seed = new float2(UnityEngine.Random.Range(0f, 8000f), UnityEngine.Random.Range(0f, 8000f));

            int islandCount = ResMgr.mapHeight / 100;
            NativeArray<float3> verts = new NativeArray<float3>(bridge.mapChunksX * bridge.mapChunksY * 529, Allocator.TempJob);
            NativeList<float3> pathVerts = new NativeList<float3>(0, Allocator.TempJob);
            NativeList<int> pathIndices = new NativeList<int>(0, Allocator.TempJob);
            NativeArray<float> mapData = new NativeArray<float>((ResMgr.mapWidth + 1) * (ResMgr.mapHeight + 1), Allocator.TempJob);
            NativeArray<int> pathMap = new NativeArray<int>((ResMgr.mapWidth + 1) * (ResMgr.mapHeight + 1), Allocator.TempJob);
            NativeArray<int2> islands = new NativeArray<int2>(islandCount + 1, Allocator.TempJob);
            NativeList<int> pathOrigins = new NativeList<int>(0, Allocator.TempJob);
            NativeList<int> pathWaypointIndices = new NativeList<int>(0, Allocator.TempJob);
            NativeList<float2> paths = new NativeList<float2>(0, Allocator.TempJob);

            GenMapJob mapJob = new GenMapJob()
            {
                seed = seed,
                islandCount = islandCount,
                verts = verts,
                pathVerts = pathVerts,
                pathIndices = pathIndices,
                mapData = mapData,
                islands = islands,
                pathOrigins = pathOrigins,
                pathWaypointIndices = pathWaypointIndices,
                paths = paths,
                pathMap = pathMap,
                mapWidth = ResMgr.mapWidth,
                mapHeight = ResMgr.mapHeight
            };
            JobHandle fvJobHandle = mapJob.Schedule(new JobHandle());

            fvJobHandle.Complete();

            Mesh[] meshes = new Mesh[bridge.mapChunksX * bridge.mapChunksY + 3 + islandCount];
            BatchMeshID[] meshID = new BatchMeshID[meshes.Length];
            Vector3[] conVerts;
            int[] conTris;
            terrain = new GameObject[bridge.mapChunksX * bridge.mapChunksY];
            Vector2[] uv = new Vector2[529];
            for (int x = -1; x <= 21; x++)
                for (int y = -1; y <= 21; y++)
                {
                    uv[(x + 1) * 23 + y + 1] = new Vector2(x * .05f, y * .05f);
                }
            for (int i = 0; i < bridge.mapChunksX * bridge.mapChunksY; i++)
            {
                meshes[i] = new Mesh();
                conVerts = new Vector3[529];
                for (int j = 0; j < 529; j++)
                    conVerts[j] = verts[529 * i + j];
                meshes[i].vertices = conVerts;
                meshes[i].uv = uv;
                conTris = new int[2904];
                for (int k = 0, l = 0; k < conTris.Length; k += 6, l++)
                {
                    if (l % 23 == 22)
                    {
                        k -= 6;
                        continue;
                    }
                    if (l < 23 || l > 482 || l % 23 == 0 || l % 23 == 21)
                    {
                        conTris[k] = l;
                        conTris[k + 1] = l + 23;
                        conTris[k + 2] = l + 24;
                        conTris[k + 3] = l;
                        conTris[k + 4] = l + 24;
                        conTris[k + 5] = l + 1;
                    }
                    else
                    {
                        conTris[k] = l;
                        conTris[k + 1] = l + 24;
                        conTris[k + 2] = l + 23;
                        conTris[k + 3] = l;
                        conTris[k + 4] = l + 1;
                        conTris[k + 5] = l + 24;
                    }
                }
                meshes[i].triangles = conTris;
                meshID[i] = hybridRenderer.RegisterMesh(meshes[i]);
                terrain[i] = new GameObject();
                //terrain[i].AddComponent<MeshRenderer>().material = bridge.terrainMat;
                //terrain[i].AddComponent<MeshFilter>().mesh = meshes[i];
                terrain[i].transform.position = new Vector3(10 + i / bridge.mapChunksX * 20, 0, 10 + i % bridge.mapChunksY * 20);
            }

            pathObjects = new GameObject[3 + islandCount];
            for (int i = 0; i < pathIndices.Length - 1; i++)
            {
                meshes[bridge.mapChunksX * bridge.mapChunksY + i] = new Mesh();
                conVerts = new Vector3[pathIndices[i + 1] - pathIndices[i]];
                for (int j = pathIndices[i]; j < pathIndices[i + 1]; j++)
                    conVerts[j - pathIndices[i]] = pathVerts[j];
                meshes[bridge.mapChunksX * bridge.mapChunksY + i].vertices = conVerts;
                uv = new Vector2[meshes[bridge.mapChunksX * bridge.mapChunksY + i].vertexCount];
                for (int j = 0; j < uv.Length; j++)
                    uv[j] = new Vector2(j % 4 * .25f, j / 4);
                meshes[bridge.mapChunksX * bridge.mapChunksY + i].uv = uv;
                conTris = new int[(conVerts.Length / 4 - 1) * 18];
                for (int l = 0, m = 0; l < conTris.Length; l += 18, m += 4)
                {
                    conTris[l] = m;
                    conTris[l + 1] = m + 4;
                    conTris[l + 2] = m + 5;
                    conTris[l + 3] = m;
                    conTris[l + 4] = m + 5;
                    conTris[l + 5] = m + 1;
                    conTris[l + 6] = m + 1;
                    conTris[l + 7] = m + 5;
                    conTris[l + 8] = m + 6;
                    conTris[l + 9] = m + 1;
                    conTris[l + 10] = m + 6;
                    conTris[l + 11] = m + 2;
                    conTris[l + 12] = m + 2;
                    conTris[l + 13] = m + 6;
                    conTris[l + 14] = m + 7;
                    conTris[l + 15] = m + 2;
                    conTris[l + 16] = m + 7;
                    conTris[l + 17] = m + 3;
                }
                meshes[bridge.mapChunksX * bridge.mapChunksY + i].triangles = conTris;
                pathObjects[i] = new GameObject();
                //pathObjects[i].AddComponent<MeshRenderer>().material = bridge.pathMat;
                //pathObjects[i].AddComponent<MeshFilter>().mesh = meshes[i];
                meshID[bridge.mapChunksX * bridge.mapChunksY + i] = hybridRenderer.RegisterMesh(meshes[bridge.mapChunksX * bridge.mapChunksY + i]);
            }
            BatchMaterialID matID = hybridRenderer.RegisterMaterial(bridge.terrainMat);
            RenderMeshArray rm = new RenderMeshArray(new UnityEngine.Material[] { bridge.terrainMat, bridge.pathMat }, meshes);
            Entity[] terrainEntities = new Entity[terrain.Length];
            Entity[] pathEntities = new Entity[pathObjects.Length];
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            BlobAssetReference<Unity.Physics.Collider> collider;
            NativeArray<float> colliderHeights = new NativeArray<float>(mapData.Length, Allocator.Temp);
                for (int i = 0; i < mapData.Length; i++)
                    colliderHeights[i / (ResMgr.mapWidth + 1) + i % (ResMgr.mapHeight + 1) * (ResMgr.mapWidth + 1)] = mapData[i];
            GameObject terrainCol = new GameObject();
            Entitize.InitColliderOnly(terrainCol.transform, Unity.Physics.TerrainCollider.Create(colliderHeights, new int2(1000, 1000), new float3(1, 1, 1), Unity.Physics.TerrainCollider.CollisionMethod.Triangles));
            GameObject.Destroy(terrainCol);
            for (int i = 0; i < terrain.Length; i++)
            {
                terrainEntities[i] = Entitize.Init(rm, i, 0, terrain[i].transform);
                GameObject.Destroy(terrain[i]);
            }
            NativeArray<float3> colliderVerts = new NativeArray<float3>();
            NativeArray<int3> colliderTris = new NativeArray<int3>();
            for (int i = 0; i < pathObjects.Length; i++)
            {
                colliderVerts = new NativeArray<float3>(4 + meshes[bridge.mapChunksX * bridge.mapChunksY + i].vertexCount / 16 * 4, Allocator.Temp);
                colliderTris = new NativeArray<int3>((colliderVerts.Length - 4) / 4 * 6, Allocator.Temp);
                for (int j = pathIndices[i], k = 0; j < pathIndices[i + 1] - 4; j += 16, k += 4)
                {
                    colliderVerts[k] = pathVerts[j] - new float3(0, .1f, 0);
                    colliderVerts[k + 1] = pathVerts[j + 1] - new float3(0, .1f, 0);
                    colliderVerts[k + 2] = pathVerts[j + 2] - new float3(0, .1f, 0);
                    colliderVerts[k + 3] = pathVerts[j + 3] - new float3(0, .1f, 0);
                }
                colliderVerts[colliderVerts.Length - 4] = pathVerts[pathIndices[i + 1] - 4] - new float3(0, .1f, 0);
                colliderVerts[colliderVerts.Length - 3] = pathVerts[pathIndices[i + 1] - 3] - new float3(0, .1f, 0);
                colliderVerts[colliderVerts.Length - 2] = pathVerts[pathIndices[i + 1] - 2] - new float3(0, .1f, 0);
                colliderVerts[colliderVerts.Length - 1] = pathVerts[pathIndices[i + 1] - 1] - new float3(0, .1f, 0);
                for (int l = 0, m = 0; l < colliderTris.Length; l += 6, m += 4)
                {
                    colliderTris[l] = new int3(m, m + 4, m + 5);
                    colliderTris[l + 1] = new int3(m, m + 5, m + 1);
                    colliderTris[l + 2] = new int3(m + 1, m + 5, m + 6);
                    colliderTris[l + 3] = new int3(m + 1, m + 6, m + 2);
                    colliderTris[l + 4] = new int3(m + 2, m + 6, m + 7);
                    colliderTris[l + 5] = new int3(m + 2, m + 7, m + 3);
                }
                collider = Unity.Physics.MeshCollider.Create(colliderVerts, colliderTris);
                terrainEntities[i] = Entitize.InitCollider(rm, i + terrain.Length, 1, pathObjects[i].transform, collider);
                GameObject.Destroy(pathObjects[i]);
            }
            colliderHeights.Dispose(); colliderVerts.Dispose(); colliderTris.Dispose(); 

            AssignMapJob assignMapJob = new AssignMapJob()
            {
                mapData = mapData,
                islands = islands,
                paths = paths,
                pathIndices = pathWaypointIndices,
                pathOrigins = pathOrigins,
                pathMap = pathMap,
                mapHeight = ResMgr.mapHeight,
                mapWidth = ResMgr.mapWidth
            };

            JobHandle mapJobHandle = assignMapJob.Schedule(fvJobHandle);

            mapJobHandle.Complete();

            verts.Dispose(); paths.Dispose(); mapData.Dispose(); islands.Dispose(); pathVerts.Dispose(); pathOrigins.Dispose(); pathIndices.Dispose();
        }
    }

    [BurstCompile]
    private partial struct AssignMapJob : IJobEntity
    {
        public NativeArray<float> mapData;
        public NativeArray<int2> islands;
        public NativeList<int> pathOrigins;
        public NativeList<float2> paths;
        public NativeList<int> pathIndices;
        public NativeArray<int> pathMap;
        public int mapWidth, mapHeight;
        public void Execute(ref MapComponent m, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            m.paths = new UnsafeList<float2>(paths.Length, Allocator.Persistent);
            m.mapData = new UnsafeList<float>(mapData.Length, Allocator.Persistent);
            m.islands = new UnsafeList<int2>(islands.Length, Allocator.Persistent);
            m.pathOrigins = new UnsafeList<int>(pathOrigins.Length, Allocator.Persistent);
            m.pathIndices = new UnsafeList<int>(pathIndices.Length, Allocator.Persistent);
            m.pathMap = new UnsafeList<int>(pathMap.Length, Allocator.Persistent);
            m.mapWidth = mapWidth; m.mapHeight = mapHeight;
            for (int i = 0; i < paths.Length; i++)
                m.paths.Add(paths[i]);
            for (int i = 0; i < islands.Length; i++)
                m.islands.Add(islands[i]);
            for (int i = 0; i < pathIndices.Length; i++)
                m.pathIndices.Add(pathIndices[i]);
            for (int i = 0; i < pathOrigins.Length; i++)
                m.pathOrigins.Add(pathOrigins[i]);
            for (int i = 0; i <  mapData.Length; i++)
                m.mapData.Add(mapData[i]);
            for (int i = 0; i < pathMap.Length; i++)
                m.pathMap.Add(pathMap[i]);
        }
    }

[BurstCompile]
    private partial struct GenMapJob : IJob
    {
        public float2 seed;
        public int islandCount;
        public NativeArray<float3> verts;
        public NativeList<float3> pathVerts;
        public NativeList<int> pathIndices;
        public NativeArray<float> mapData;
        public NativeArray<int2> islands;
        public NativeList<int> pathOrigins;
        public NativeList<float2> paths;
        public NativeList<int> pathWaypointIndices;
        public NativeArray<int> pathMap;
        public int mapWidth;
        public int mapHeight;
        public void Execute()
        {
                for (int x = 0; x < mapWidth; x++)
                    for (int y = 0; y < mapHeight; y++)
                        mapData[x * mapHeight + y] = noise.cnoise(new float2(seed.x + x * .11f, seed.y + y * .11f));
            NativeList<int2> islandLocs = new NativeList<int2>(0, Allocator.Temp);
            NativeArray<int> restrictions = new NativeArray<int>(mapWidth * mapHeight, Allocator.Temp);
            pathWaypointIndices.Add(0);
            int2 curIslandLoc = new int2(mapWidth / 2, mapHeight / 2);
            bool reposition;
            int distLimit = mapWidth * mapHeight / 100;
            float2 dynamicSeed = seed;
            int2 islandSize;
            float centralElevation, maxDist, curDist;
            float2 distCurve1, distCurve2;

            float2 curWaypoint, curWaypointDir;
            float waypointRotation = 0, curWaypointRotation;
            int curIndex;

            // generate main routes to starting island
            for (int i = 0; i < 3; i++)
            {
                dynamicSeed += new float2(.18269f, .96438f);
                if (i == 0)
                {
                    paths.Add(new float2(mapWidth + 10, mapHeight / 2 + (noise.cnoise(dynamicSeed) + 1) * mapHeight / 4));
                    paths.Add(new float2(mapWidth, mapHeight / 2 + (noise.cnoise(dynamicSeed) + 1) * mapHeight / 4));
                }
                else if (i == 1)
                {
                    paths.Add(new float2(-10, mapHeight / 2 + (noise.cnoise(dynamicSeed) + 1) * mapHeight / 4));
                    paths.Add(new float2(0, mapHeight / 2 + (noise.cnoise(dynamicSeed) + 1) * mapHeight / 4));
                }
                else
                {
                    paths.Add(new float2(mapHeight * .3f + (noise.cnoise(dynamicSeed) + 1) * mapHeight * .2f, -10));
                    paths.Add(new float2(mapHeight * .3f + (noise.cnoise(dynamicSeed) + 1) * mapHeight * .2f, 0));
                }
                
                curIndex = pathWaypointIndices[pathWaypointIndices.Length - 1] + 1;
                curWaypoint = paths[curIndex];
                while (BaseOps.MagSqr(curWaypoint, curIslandLoc) > 400)
                {
                    curWaypointDir = math.normalize(curIslandLoc - paths[curIndex]);
                    dynamicSeed += new float2(.18269f, .76438f);
                    curWaypoint = paths[curIndex] + curWaypointDir * (25 + (noise.cnoise(dynamicSeed + new float2(.6316f, .82589f)) * 15));
                    curWaypointRotation = noise.cnoise(dynamicSeed + new float2(.2316f, .32589f)) * 15;
                    while ((waypointRotation + curWaypointRotation) * (waypointRotation + curWaypointRotation) > 2250)
                    {
                        dynamicSeed += new float2(.48269f, .26438f);
                        curWaypointRotation = (noise.cnoise(dynamicSeed + new float2(.2316f, .32589f))) * 15;
                    }
                    waypointRotation += curWaypointRotation;
                    curWaypoint = curIslandLoc + Curves.Rotate(curWaypoint - curIslandLoc, curWaypointRotation);
                    paths.Add(paths[curIndex] + math.normalize(paths[curIndex] - paths[curIndex - 1]) * (20 + (noise.cnoise(dynamicSeed + new float2(.3316f, .52589f)) * 15)));
                    paths.Add(curWaypoint);
                    curIndex += 2;
                }
                if (BaseOps.MagSqr(curWaypoint, curIslandLoc) < 300)
                    paths[paths.Length - 1] = curIslandLoc + (paths[paths.Length - 1] - curIslandLoc) * 1.25f;
                paths.Add(paths[curIndex] + math.normalize(paths[curIndex] - paths[curIndex - 1]) * 5);
                paths.Add(curIslandLoc + math.normalize(curWaypoint - curIslandLoc) * 12);

                for (int j = pathWaypointIndices[i]; j < paths.Length; j += 2)
                {
                    for (int x = (int)paths[j + 1].x - 10; x <= paths[j + 1].x + 10; x++)
                        for (int y = (int)paths[j + 1].y - 10; y <= paths[j + 1].y + 10; y++)
                            if (x >= 0 && y >= 0 && x <= mapWidth && y <= mapHeight)
                                restrictions[x * (mapHeight + 1) + y] = i + 1;
                    for (int x = (int)paths[j + 1].x - 2; x <= paths[j + 1].x + 2; x++)
                        for (int y = (int)paths[j + 1].y - 2; y <= paths[j + 1].y + 2; y++)
                            if (x >= 0 && y >= 0 && x <= mapWidth && y <= mapHeight)
                                pathMap[x * (mapHeight + 1) + y] = i + 1;
                }
                pathOrigins.Add(-1);
                pathWaypointIndices.Add(paths.Length);
            }
            // generate starting island
            islandLocs.Add(curIslandLoc);
            islandSize = new int2((int)((noise.cnoise(dynamicSeed) + 1) * 8) + 16, (int)((noise.cnoise(dynamicSeed + new float2(.642f, .74147f)) + 1) * 8) + 16);
            centralElevation = 3 - mapData[curIslandLoc.x * mapHeight + curIslandLoc.y] * .5f;
            maxDist = islandSize.x * islandSize.x + islandSize.y * islandSize.y;
            distCurve1 = new float2(maxDist * .75f, 1); distCurve2 = new float2(maxDist * .75f, 0);
            for (int x = curIslandLoc.x - islandSize.x; x <= curIslandLoc.x + islandSize.x; x++)
            {
                for (int y = curIslandLoc.y - islandSize.y; y <= curIslandLoc.y + islandSize.y; y++)
                {
                    curDist = ((x - curIslandLoc.x) * (x - curIslandLoc.x) + (y - curIslandLoc.y) * (y - curIslandLoc.y)) / maxDist;
                    mapData[x * mapHeight + y] += (centralElevation * Curves.CubicCurve(new float2(0, 1), distCurve1, distCurve2, new float2(1, 0), curDist).y);
                    if (mapData[x * mapHeight + y] > 1.2f)
                        mapData[x * mapHeight + y] = 1.2f + (mapData[x * mapHeight + y] - 1.2f) * .5f;
                }
            }

            // generate minor islands
            for (int i = 0; i < mapHeight / 100; i++)
            {
                reposition = true;
                while (reposition)
                {
                    dynamicSeed += new float2(.7528824f, .247853f);
                    curIslandLoc = new int2((int)(mapWidth * .1f) + (int)((noise.cnoise(new float2(dynamicSeed.x * .147f + i * .7428f, dynamicSeed.y * .1237f + i * .6948f)) + 1) * mapWidth * .4f), (int)(mapHeight * .1f) + (int)((noise.cnoise(new float2(dynamicSeed.x * .08376f + i * .57838f, dynamicSeed.y * .17048f + i * .50878f)) + 1) * mapHeight * .4f));
                    reposition = false;
                    if (restrictions[curIslandLoc.x * mapHeight + curIslandLoc.y] > 0)
                        reposition = true;
                    else
                        foreach (int2 loc in islandLocs)
                            if ((loc.x - curIslandLoc.x) * (loc.x - curIslandLoc.x) + (loc.y - curIslandLoc.y) * (loc.y - curIslandLoc.y) < distLimit)
                            {
                                reposition = true;
                                break;
                            }
                }
                islandLocs.Add(curIslandLoc);
                centralElevation = 3 - mapData[curIslandLoc.x * mapHeight + curIslandLoc.y] * .5f;
                dynamicSeed += new float2(.78477f, .3288f);
                islandSize = new int2((int)((noise.cnoise(dynamicSeed) + 1) * 4) + 10, (int)((noise.cnoise(dynamicSeed + new float2(.642f, .74147f)) + 1) * 4) + 10);
                if (islandSize.x < 4) islandSize.x = 4;
                if (islandSize.y < 4) islandSize.y = 4;
                maxDist = islandSize.x * islandSize.x + islandSize.y * islandSize.y;
                distCurve1 = new float2(maxDist * .75f, 1); distCurve2 = new float2(maxDist * .75f, 0);
                for (int x = curIslandLoc.x - islandSize.x; x <= curIslandLoc.x + islandSize.x; x++)
                {
                    for (int y = curIslandLoc.y - islandSize.y; y <= curIslandLoc.y + islandSize.y; y++)
                    {
                        curDist = ((x - curIslandLoc.x) * (x - curIslandLoc.x) + (y - curIslandLoc.y) * (y - curIslandLoc.y)) / maxDist;
                        mapData[x * mapHeight + y] += (centralElevation * Curves.CubicCurve(new float2(0, 1), distCurve1, distCurve2, new float2(1, 0), curDist).y);
                        if (mapData[x * mapHeight + y] > 1.2f)
                            mapData[x * mapHeight + y] = 1.2f + (mapData[x * mapHeight + y] - 1.2f) * .5f;
                    }
                }
            }
            // generate minor island routes
            int pathCrossed, closest, retraceCount;
            bool nearIsland;
            float2 curPoint;
            for (int i = 1; i < islandLocs.Length; i++)
            {
                dynamicSeed += new float2(.38269f, .26438f);

                nearIsland = false;
                while (true)
                {
                    retraceCount = 2;
                    paths.Add(islandLocs[i] + new float2((noise.cnoise(dynamicSeed)) * 20, (noise.cnoise(dynamicSeed + new float2(.1631f, .7428f))) * 20));
                    paths.Add(islandLocs[i] + math.normalize(islandLocs[i] - paths[paths.Length - 1]) * 2);
                    curIndex = pathWaypointIndices[pathWaypointIndices.Length - 1] + 1;
                    while (!nearIsland && paths[paths.Length - 1].x > 0 && paths[paths.Length - 1].x < mapWidth && paths[paths.Length - 1].y > 0 && paths[paths.Length - 1].y < mapWidth
                        && restrictions[(int)paths[paths.Length - 1].x * mapHeight + (int)paths[paths.Length - 1].y] == 0)
                    {
                        curWaypointDir = math.normalize(paths[curIndex] - islandLocs[i]);
                        dynamicSeed += new float2(.18269f, .76438f);
                        curWaypoint = paths[curIndex] + curWaypointDir * (25 + (noise.cnoise(dynamicSeed + new float2(.6316f, .82589f)) * 15));
                        curWaypointRotation = (noise.cnoise(dynamicSeed + new float2(.2316f, .32589f))) * 15;
                        curWaypoint = islandLocs[i] + Curves.Rotate(curWaypoint - islandLocs[i], curWaypointRotation);
                        paths.Add(paths[curIndex] + math.normalize(paths[curIndex] - paths[curIndex - 1]) * (20 + (noise.cnoise(dynamicSeed + new float2(.3316f, .52589f)) * 15)));
                        paths.Add(curWaypoint);
                        retraceCount += 2;
                        curIndex += 2;
                        foreach (int2 islandCheck in islandLocs)
                        {
                            if (islandCheck.x == islandLocs[i].x && islandCheck.y == islandLocs[i].y) continue;
                            if ((islandCheck.x - curWaypoint.x) * (islandCheck.x - curWaypoint.x) + (islandCheck.y - curWaypoint.y) * (islandCheck.y - curWaypoint.y) < 300
                                || (islandCheck.x - curWaypoint.x) * (islandCheck.x - curWaypoint.x) + (islandCheck.y - curWaypoint.y) * (islandCheck.y - curWaypoint.y) < 300)
                            {
                                nearIsland = true;
                                break;
                            }
                        }
                        for (int k = 0; k < 20; k++)
                        {
                            curPoint = Curves.QuadCurve(paths[paths.Length - 3], paths[paths.Length - 2], paths[paths.Length - 1], .05f * k);
                            if (curPoint.x <= 0 || curPoint.y <= 0 || curPoint.x >= mapWidth || curPoint.y >= mapHeight || restrictions[(int)curPoint.x * mapHeight + (int)curPoint.y] > 0)
                            {
                                paths[paths.Length - 1] = curPoint;
                                paths[paths.Length - 2] = paths[paths.Length - 3] + curWaypointDir * k;
                                break;
                            }
                        }
                    }
                    if (nearIsland || math.lengthsq(paths[paths.Length - 1] - islandLocs[0]) < math.lengthsq(paths[paths.Length - 1] - islandLocs[i]))
                    {
                        nearIsland = false;
                        for (int k = 0; k < retraceCount; k++)
                            paths.RemoveAt(paths.Length - 1);
                    }
                    else if (paths[paths.Length - 1].x <= 0 || paths[paths.Length - 1].x >= mapWidth || paths[paths.Length - 1].y <= 0 || paths[paths.Length - 1].y >= mapWidth)
                    {
                        pathCrossed = -1;
                        break;
                    }
                    else if (restrictions[(int)paths[paths.Length - 1].x * mapHeight + (int)paths[paths.Length - 1].y] > 0)
                    {
                        pathCrossed = restrictions[(int)paths[paths.Length - 1].x * mapHeight + (int)paths[paths.Length - 1].y] - 1;
                        break;
                    }
                }
                pathOrigins.Add(pathCrossed);
                if (pathCrossed >= 0)
                {
                    closest = pathWaypointIndices[pathCrossed] + 1;
                    for (int j = pathWaypointIndices[pathCrossed] + 3; j < pathWaypointIndices[pathCrossed + 1]; j += 2)
                    {
                        if ((paths[paths.Length - 1].x - paths[j].x) * (paths[paths.Length - 1].x - paths[j].x)
                            + (paths[paths.Length - 1].y - paths[j].y) * (paths[paths.Length - 1].y - paths[j].y) <
                                (paths[paths.Length - 1].x - paths[closest].x) * (paths[paths.Length - 1].x - paths[closest].x)
                            + (paths[paths.Length - 1].y - paths[closest].y) * (paths[paths.Length - 1].y - paths[closest].y))
                            closest = j;
                    }
                    paths.Add(paths[paths.Length - 1] + math.normalize(paths[paths.Length - 1] - paths[paths.Length - 2]) * (6 + (noise.cnoise(dynamicSeed + new float2(.3316f, .52589f)) * 5)));
                    paths.Add(paths[closest]);
                }
                for (int j = pathWaypointIndices[pathWaypointIndices.Length - 1] + 1; j < paths.Length; j += 2)
                {
                    for (int x = (int)paths[j].x - 10; x < paths[j].x + 10; x++)
                        for (int y = (int)paths[j].y - 10; y < paths[j].y + 10; y++)
                            if (x >= 0 && y >= 0 && x <= mapWidth && y <= mapHeight && restrictions[x * (mapHeight + 1) + y] == 0)
                                restrictions[x * (mapHeight + 1) + y] = i + 3;
                    for (int x = (int)paths[j].x - 2; x < paths[j].x + 2; x++)
                        for (int y = (int)paths[j].y - 2; y < paths[j].y + 2; y++)
                            if (x >= 0 && y >= 0 && x <= mapWidth && y <= mapHeight)
                                if (pathMap[x * (mapHeight + 1) + y] == 0 || (x * x) + (y * y) < 2)
                                pathMap[x * (mapHeight + 1) + y] = i + 3;
                }
                pathWaypointIndices.Add(paths.Length);
            }

            int xMulti = mapWidth / 20, yMulti = mapHeight / 20;
            for (int x = 0; x < xMulti; x++)
                for (int y = 0; y < yMulti; y++)
                {
                    for (int vX = 0; vX <= 22; vX++)
                        for (int vY = 0; vY <= 22; vY++)
                            if (x + vX == 0 || x == xMulti - 1 && vX == 22 || y + vY == 0 || y == yMulti - 1 && vY == 22)
                                verts[(x * yMulti + y) * 529 + vX * 23 + vY] = new float3(vX - 11, .5f, vY - 11);
                            else
                                verts[(x * yMulti + y) * 529 + vX * 23 + vY] = new float3(vX - 11, mapData[(x * 20 + vX - 1) * (mapHeight + 1) + y * 20 + vY - 1], vY - 11);
                }
            int pathVertLength;
            NativeArray<float> pathLengths;
            float2 curDir;
            pathIndices.Add(0);
            for (int i = 0; i < pathWaypointIndices.Length - 1; i++)
            {
                pathVertLength = 4;
                pathLengths = new NativeArray<float>((pathWaypointIndices[i + 1] - pathWaypointIndices[i]) / 2 - 1, Allocator.Temp);
                for (int j = pathWaypointIndices[i] + 1; j < pathWaypointIndices[i + 1] - 2; j += 2)
                {
                    for (int t = 1; t <= 100; t++)
                        pathLengths[(j - pathWaypointIndices[i]) / 2] += math.length(Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], .01f * t) - Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], .01f * (t - 1)));
                    pathVertLength += (int)pathLengths[(j - pathWaypointIndices[i]) / 2] * 4;
                }
                pathIndices.Add(pathIndices[pathIndices.Length - 1] + pathVertLength);
                for (int j = pathWaypointIndices[i] + 1; j < pathWaypointIndices[i + 1] - 2; j += 2)
                {
                    for (int t = 0; t < (int)pathLengths[(j - pathWaypointIndices[i]) / 2]; t++)
                    {
                        dynamicSeed += new float2(.011f, 0);
                        curPoint = Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], 1f / (pathLengths[(j - pathWaypointIndices[i]) / 2]) * t);
                        curDir = Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], 1f / (pathLengths[(j - pathWaypointIndices[i]) / 2]) * (t + .1f)) -
                            Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], 1f / (pathLengths[(j - pathWaypointIndices[i]) / 2]) * (t - .1f));
                        curDir = math.normalize(new float2(curDir.y, -curDir.x));
                        pathVerts.Add(new float3((curPoint - curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).x, -.1f, (curPoint - curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).y));
                        pathVerts.Add(new float3((curPoint - curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).x, 2.05f + noise.cnoise(dynamicSeed) * .05f, (curPoint - curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).y));
                        pathVerts.Add(new float3((curPoint + curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).x, 2.05f + noise.cnoise(dynamicSeed) * .05f, (curPoint + curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).y));
                        pathVerts.Add(new float3((curPoint + curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).x, -.1f, (curPoint + curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).y));
                    }
                    if (j >= pathWaypointIndices[i + 1] - 4)
                    {
                        dynamicSeed += new float2(.011f, 0);
                        curPoint = Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], 1f);
                        curDir = Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], 1.01f) -
                            Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], .99f);
                        curDir = math.normalize(new float2(curDir.y, -curDir.x));
                        pathVerts.Add(new float3((curPoint - curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).x, -.1f, (curPoint - curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).y));
                        pathVerts.Add(new float3((curPoint - curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).x, 2.05f + noise.cnoise(dynamicSeed) * .05f, (curPoint - curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).y));
                        pathVerts.Add(new float3((curPoint + curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).x, 2.05f + noise.cnoise(dynamicSeed) * .05f, (curPoint + curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).y));
                        pathVerts.Add(new float3((curPoint + curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).x, -.1f, (curPoint + curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).y));
                    }
                }
            }
        }
    }
}
