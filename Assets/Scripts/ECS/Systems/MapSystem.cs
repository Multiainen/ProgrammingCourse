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
using static UnityEngine.GraphicsBuffer;

public partial class MapSystem : SystemBase
{
    ECSBridge bridge;
    EntitiesGraphicsSystem hybridRenderer;
    DwarfManager dwarfManager;
    float2 seed;
    public GameObject[] terrain;
    public GameObject[] pathObjects;
    NativeArray<float> mapData = new NativeArray<float>((ResMgr.mapWidth + 1) * (ResMgr.mapHeight + 1), Allocator.Persistent);
    NativeArray<int> pathMap = new NativeArray<int>((ResMgr.mapWidth + 1) * (ResMgr.mapHeight + 1), Allocator.Persistent);

    NativeArray<int2> islands = new NativeArray<int2>(ResMgr.mapHeight / 100 + 1, Allocator.Persistent);
    NativeList<int> pathOrigins = new NativeList<int>(0, Allocator.Persistent);
    NativeList<int> pathWaypointIndices = new NativeList<int>(0, Allocator.Persistent);
    NativeList<float2> paths = new NativeList<float2>(0, Allocator.Persistent);

    protected override void OnUpdate()
    {
        if (!bridge)
        {
            bridge = GameObject.Find("Root").GetComponent<ECSBridge>();
            hybridRenderer = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            dwarfManager = World.GetOrCreateSystemManaged<DwarfManager>();
            seed = new float2(UnityEngine.Random.Range(0f, 8000f), UnityEngine.Random.Range(0f, 8000f));


            NativeArray<float3> verts = new NativeArray<float3>(bridge.mapChunksX * bridge.mapChunksY * 529, Allocator.TempJob);
            NativeList<float3> pathVerts = new NativeList<float3>(0, Allocator.TempJob);
            NativeList<int> pathIndices = new NativeList<int>(0, Allocator.TempJob);



            GenMapJob mapJob = new GenMapJob()
            {
                seed = seed,
                islandCount = ResMgr.mapHeight / 100,
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

            Mesh[] meshes = new Mesh[bridge.mapChunksX * bridge.mapChunksY + 3 + ResMgr.mapHeight / 100];
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
                    conTris[k] = l;
                    conTris[k + 1] = l + 24;
                    conTris[k + 2] = l + 23;
                    conTris[k + 3] = l;
                    conTris[k + 4] = l + 1;
                    conTris[k + 5] = l + 24;
                }
                meshes[i].triangles = conTris;
                meshes[i].RecalculateNormals();
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
                meshes[i].triangles = conTris;
                meshID[i] = hybridRenderer.RegisterMesh(meshes[i]);
                terrain[i] = new GameObject();
                terrain[i].transform.position = new Vector3(10 + i / bridge.mapChunksX * 20, 0, 10 + i % bridge.mapChunksY * 20);
            }

            pathObjects = new GameObject[3 + ResMgr.mapHeight / 100];
            Vector3[] manualNormals;
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
                meshes[bridge.mapChunksX * bridge.mapChunksY + i].RecalculateNormals();
                manualNormals = meshes[bridge.mapChunksX * bridge.mapChunksY + i].normals;
                for (int j = 0; j < manualNormals.Length; j += 4)
                {
                    manualNormals[j + 1] = Vector3.up; manualNormals[j + 2] = Vector3.up;
                }
                meshes[bridge.mapChunksX * bridge.mapChunksY + i].normals = manualNormals;
                pathObjects[i] = new GameObject();
                meshID[bridge.mapChunksX * bridge.mapChunksY + i] = hybridRenderer.RegisterMesh(meshes[bridge.mapChunksX * bridge.mapChunksY + i]);
            }
            BatchMaterialID matID = hybridRenderer.RegisterMaterial(bridge.terrainMat);
            bridge.renderMeshArray = new RenderMeshArray(new UnityEngine.Material[] { bridge.terrainMat, bridge.pathMat }, meshes);
            bridge.terrainMeshes = meshes;
            Entity[] terrainEntities = new Entity[terrain.Length];
            Entity[] pathEntities = new Entity[pathObjects.Length];
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            BlobAssetReference<Unity.Physics.Collider> collider;
            NativeArray<float> colliderHeights = new NativeArray<float>(mapData.Length, Allocator.Temp);
            for (int i = 0; i < mapData.Length; i++)
                colliderHeights[i / (ResMgr.mapWidth + 1) + i % (ResMgr.mapHeight + 1) * (ResMgr.mapWidth + 1)] = mapData[i];
            GameObject terrainCol = new GameObject();
            collider = Unity.Physics.TerrainCollider.Create(colliderHeights, new int2(1000, 1000), new float3(1, 1, 1), Unity.Physics.TerrainCollider.CollisionMethod.Triangles);
            collider.Value.SetCollisionFilter(new CollisionFilter
            {
                BelongsTo = (uint)bridge.collisionFilters[2].x,
                CollidesWith = (uint)bridge.collisionFilters[2].y,
                GroupIndex = bridge.collisionFilters[2].z
            });
            Entitize.InitColliderOnly(terrainCol.transform, collider);
            GameObject.Destroy(terrainCol);
            for (int i = 0; i < terrain.Length; i++)
            {
                terrainEntities[i] = Entitize.Init(bridge.renderMeshArray, i, 0, terrain[i].transform);
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
                collider.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = (uint)bridge.collisionFilters[2].x,
                    CollidesWith = (uint)bridge.collisionFilters[2].y,
                    GroupIndex = bridge.collisionFilters[2].z
                });
                terrainEntities[i] = Entitize.InitCollider(bridge.renderMeshArray, i + terrain.Length, 1, pathObjects[i].transform, collider);
                GameObject.Destroy(pathObjects[i]);
            }
            colliderHeights.Dispose(); colliderVerts.Dispose(); colliderTris.Dispose();


            //AssignMapJob assignMapJob = new AssignMapJob()
            //{
            //    mapData = mapData,
            //    islands = islands,
            //    paths = paths,
            //    pathIndices = pathWaypointIndices,
            //    pathOrigins = pathOrigins,
            //    pathMap = pathMap,
            //    mapHeight = ResMgr.mapHeight,
            //    mapWidth = ResMgr.mapWidth
            //};

            //JobHandle mapJobHandle = assignMapJob.Schedule(fvJobHandle);

            //mapJobHandle.Complete();

            foreach ((AspectAssignMap m, Entity entity) in SystemAPI.Query<AspectAssignMap>().WithEntityAccess())
            {
                m.Init(mapData, islands, pathOrigins, paths, pathMap, pathWaypointIndices, ResMgr.mapWidth, ResMgr.mapHeight);
                pathOrigins.Dispose(); paths.Dispose(); mapData.Dispose(); islands.Dispose(); pathWaypointIndices.Dispose(); pathMap.Dispose();
                break;
            }


            verts.Dispose(); pathVerts.Dispose(); pathIndices.Dispose();
        }
        else if (!SystemAPI.TryGetSingleton<MapComponent>(out MapComponent map) || !map.mapData.IsCreated || map.mapData.IsEmpty)
        {
            foreach ((AspectAssignMap m, Entity entity) in SystemAPI.Query<AspectAssignMap>().WithEntityAccess())
            {
                m.Init(mapData, islands, pathOrigins, paths, pathMap, pathWaypointIndices, ResMgr.mapWidth, ResMgr.mapHeight);


                pathOrigins.Dispose(); paths.Dispose(); mapData.Dispose(); islands.Dispose(); pathWaypointIndices.Dispose(); pathMap.Dispose();
                bridge.StopTime();
                break;
            }
        }
    }

    //[BurstCompile]
    private partial struct AssignMapJob : IJobEntity
    {
        public NativeArray<float> mapData;
        public NativeArray<int2> islands;
        public NativeList<int> pathOrigins;
        public NativeList<float2> paths;
        public NativeList<int> pathIndices;
        public NativeArray<int> pathMap;
        public int mapWidth, mapHeight;
        public void Execute(ref MapComponent m, ref MapRefComponent mRef, ref TowerDataRef towerRef, ref ExplosionsToSpawn explosions, ref TowerProjectilesToSpawn projectiles, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            m.mapData = new UnsafeList<float>(mapData.Length, Allocator.Persistent);
            m.vertsToRaise = new UnsafeQueue<float2>(Allocator.Persistent);
            
            m.pathStartStep = new UnsafeList<int>(pathIndices.Length, Allocator.Persistent);
            m.mapWidth = mapWidth; m.mapHeight = mapHeight;
            for (int i = 0; i <  mapData.Length; i++)
                m.mapData.Add(mapData[i]);

            BlobBuilder blobBuilder = new BlobBuilder(Allocator.Persistent);
            ref MapRefComponentContents pathMapBlob = ref blobBuilder.ConstructRoot<MapRefComponentContents>();
            var pathMapBuilder = blobBuilder.Allocate(ref pathMapBlob.pathMap, pathMap.Length);
            for (int i = 0; i < pathMap.Length; i++)
                pathMapBuilder[i] = pathMap[i];
            var pathIndicesBuilder = blobBuilder.Allocate(ref pathMapBlob.pathIndices, pathIndices.Length);
            for (int i = 0; i < pathIndices.Length; i++)
                pathIndicesBuilder[i] = pathIndices[i];
            var pathOriginsBuilder = blobBuilder.Allocate(ref pathMapBlob.pathOrigins, pathOrigins.Length);
            for (int i = 0; i < pathOrigins.Length; i++)
                pathOriginsBuilder[i] = pathOrigins[i];
            var pathsBuilder = blobBuilder.Allocate(ref pathMapBlob.paths, paths.Length);
            for (int i = 0; i < paths.Length; i++)
                pathsBuilder[i] = paths[i];
            var islandsBuilder = blobBuilder.Allocate(ref pathMapBlob.islands, islands.Length);
            for (int i = 0; i < islands.Length; i++)
                islandsBuilder[i] = islands[i];
            var pathStepsBuilder = blobBuilder.Allocate(ref pathMapBlob.pathSteps, (paths.Length) * 100);
            var pathStepLengthBuilder = blobBuilder.Allocate(ref pathMapBlob.pathStepLength, (paths.Length) * 100);
            float2 curPoint;
            for (int k = 0, l = 0; k < pathIndices.Length - 1; k++)
            {
                for (int i = pathIndices[k] + 1; i < pathIndices[k + 1] - 1; i += 2)
                {
                    for (int j = 0; j < 200; j++)
                    {
                        curPoint = Curves.QuadCurve(paths[i], paths[i + 1], paths[i + 2], j * .005f);
                        if (m.pathStartStep.Length <= k && math.lengthsq(curPoint - islands[l]) < 20000)
                            m.pathStartStep.Add(i * 100 + j);
                        pathStepsBuilder[i * 100 + j] = curPoint;
                        if (j > 0 || i > pathIndices[k] + 1)
                            pathStepLengthBuilder[i * 100 + j - 1] = math.length(pathStepsBuilder[i * 100 + j - 1] - pathStepsBuilder[i * 100 + j]);
                    }
                }
                if (k > 1) l++;
            }
            mRef.contents = blobBuilder.CreateBlobAssetReference<MapRefComponentContents>(Allocator.Persistent);
            blobBuilder.Dispose();
            blobBuilder = new BlobBuilder(Allocator.Persistent);
            int arrayLength = TowerData.projectile.Length;
            ref TowerDataContents towerBlob = ref blobBuilder.ConstructRoot<TowerDataContents>();
            var tpb = blobBuilder.Allocate(ref towerBlob.projectile, arrayLength);
            for (int i = 0; i < arrayLength; i++)
                tpb[i] = TowerData.projectile[i];
            var tcb = blobBuilder.Allocate(ref towerBlob.cooldown, arrayLength);
            for (int i = 0; i < arrayLength; i++)
                tcb[i] = TowerData.cooldown[i];
            var tpmb = blobBuilder.Allocate(ref towerBlob.projectileMass, arrayLength);
            for (int i = 0; i < arrayLength; i++)
                tpmb[i] = TowerData.projectileMass[i];
            var tpsb = blobBuilder.Allocate(ref towerBlob.projectileSharpness, arrayLength);
            for (int i = 0; i < arrayLength; i++)
                tpsb[i] = TowerData.projectileSharpness[i];
            var tprb = blobBuilder.Allocate(ref towerBlob.projectileRadius, arrayLength);
            for (int i = 0; i < arrayLength; i++)
                tprb[i] = TowerData.projectileRadius[i];
            var tpbb = blobBuilder.Allocate(ref towerBlob.projectileBehaviour, arrayLength);
            for (int i = 0; i < arrayLength; i++)
                tpbb[i] = TowerData.projectileBehaviour[i];
            towerRef.contents = blobBuilder.CreateBlobAssetReference<TowerDataContents>(Allocator.Persistent);
            blobBuilder.Dispose();

            explosions.spawns = new UnsafeQueue<ExplosionData>(Allocator.Persistent);
            projectiles.targets = new UnsafeQueue<LaunchData>(Allocator.Persistent);

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
                for (int x = 0; x <= mapWidth; x++)
                    for (int y = 0; y <= mapHeight; y++)
                        mapData[x * (mapHeight + 1) + y] = noise.cnoise(new float2(seed.x + x * .056f, seed.y + y * .056f));
            NativeArray<int> restrictions = new NativeArray<int>(pathMap.Length, Allocator.Temp);
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
                    paths.Add(new float2(mapWidth + 10, mapHeight / 2 + (noise.cnoise(dynamicSeed) + 1) * (mapHeight + 1) / 4));
                    paths.Add(new float2(mapWidth, mapHeight / 2 + (noise.cnoise(dynamicSeed) + 1) * (mapHeight + 1) / 4));
                }
                else if (i == 1)
                {
                    paths.Add(new float2(-10, mapHeight / 2 + (noise.cnoise(dynamicSeed) + 1) * (mapHeight + 1) / 4));
                    paths.Add(new float2(0, mapHeight / 2 + (noise.cnoise(dynamicSeed) + 1) * (mapHeight + 1) / 4));
                }
                else
                {
                    paths.Add(new float2(mapHeight * .3f + (noise.cnoise(dynamicSeed) + 1) * (mapHeight + 1) * .2f, -10));
                    paths.Add(new float2(mapHeight * .3f + (noise.cnoise(dynamicSeed) + 1) * (mapHeight + 1) * .2f, 0));
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

                float2 processPoint;
                for (int j = pathWaypointIndices[i] + 1; j < paths.Length - 2; j += 2)
                    for (int k = 0; k < 100;  k++)
                {
                    processPoint = Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], k * .01f);
                    for (int x = (int)processPoint.x - 10; x <= processPoint.x + 10; x++)
                        for (int y = (int)processPoint.y - 10; y <= processPoint.y + 10; y++)
                            if (x >= 0 && y >= 0 && x <= mapWidth && y <= mapHeight)
                                restrictions[x * (mapHeight + 1) + y] = i + 1;
                    for (int x = (int)processPoint.x - 2; x <= processPoint.x + 2; x++)
                        for (int y = (int)processPoint.y - 2; y <= processPoint.y + 2; y++)
                            if (x >= 0 && y >= 0 && x <= mapWidth && y <= mapHeight)
                                pathMap[x * (mapHeight + 1) + y] = i + 1;
                }
                pathOrigins.Add(-1);
                pathWaypointIndices.Add(paths.Length);
            }
            // generate starting island
            islands[0] = curIslandLoc;
            islandSize = new int2((int)((noise.cnoise(dynamicSeed) + 1) * 8) + 16, (int)((noise.cnoise(dynamicSeed + new float2(.642f, .74147f)) + 1) * 8) + 16);
            centralElevation = 4.5f - mapData[curIslandLoc.x * (mapHeight + 1) + curIslandLoc.y] * .5f;
            maxDist = islandSize.x * islandSize.x + islandSize.y * islandSize.y;
            distCurve1 = new float2(maxDist * .5f, 1); distCurve2 = new float2(maxDist * .5f, 0);
            for (int x = curIslandLoc.x - islandSize.x * 2; x <= curIslandLoc.x + islandSize.x * 2; x++)
            {
                for (int y = curIslandLoc.y - islandSize.y * 2; y <= curIslandLoc.y + islandSize.y * 2; y++)
                {
                    curDist = ((x - curIslandLoc.x) * (x - curIslandLoc.x) + (y - curIslandLoc.y) * (y - curIslandLoc.y)) / maxDist;
                    if (curDist > 1) continue;
                    mapData[x * (mapHeight + 1) + y] += (centralElevation * Curves.CubicCurve(new float2(0, 1), distCurve1, distCurve2, new float2(1, 0), curDist).y);
                    if (mapData[x * (mapHeight + 1) + y] > 1.2f)
                        mapData[x * (mapHeight + 1) + y] = 1.2f + (mapData[x * (mapHeight + 1) + y] - 1.2f) * .4f;
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
                    if (restrictions[curIslandLoc.x * (mapHeight + 1) + curIslandLoc.y] > 0)
                        reposition = true;
                    else
                        foreach (int2 loc in islands)
                            if ((loc.x - curIslandLoc.x) * (loc.x - curIslandLoc.x) + (loc.y - curIslandLoc.y) * (loc.y - curIslandLoc.y) < distLimit)
                            {
                                reposition = true;
                                break;
                            }
                }
                islands[i + 1] = curIslandLoc;
                centralElevation = 3.5f - mapData[curIslandLoc.x * (mapHeight + 1) + curIslandLoc.y] * .5f;
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
                        mapData[x * (mapHeight + 1) + y] += (centralElevation * Curves.CubicCurve(new float2(0, 1), distCurve1, distCurve2, new float2(1, 0), curDist).y);
                        if (mapData[x * (mapHeight + 1) + y] > 1.2f)
                            mapData[x * (mapHeight + 1) + y] = 1.2f + (mapData[x * (mapHeight + 1) + y] - 1.2f) * .4f;
                    }
                }
            }
            // generate minor island routes
            int pathCrossed, closest, retraceCount;
            bool nearIsland;
            float2 curPoint;
            for (int i = 1; i < islands.Length; i++)
            {
                dynamicSeed += new float2(.38269f, .26438f);

                nearIsland = false;
                while (true)
                {
                    retraceCount = 2;
                    paths.Add(islands[i] + new float2((noise.cnoise(dynamicSeed)) * 20, (noise.cnoise(dynamicSeed + new float2(.1631f, .7428f))) * 20));
                    paths.Add(islands[i] + math.normalize(islands[i] - paths[paths.Length - 1]) * 2);
                    curIndex = pathWaypointIndices[pathWaypointIndices.Length - 1] + 1;
                    while (!nearIsland && paths[paths.Length - 1].x > 0 && paths[paths.Length - 1].x < mapWidth && paths[paths.Length - 1].y > 0 && paths[paths.Length - 1].y < mapWidth
                        && restrictions[(int)paths[paths.Length - 1].x * (mapHeight + 1) + (int)paths[paths.Length - 1].y] == 0)
                    {
                        curWaypointDir = math.normalize(paths[curIndex] - islands[i]);
                        dynamicSeed += new float2(.18269f, .76438f);
                        curWaypoint = paths[curIndex] + curWaypointDir * (25 + (noise.cnoise(dynamicSeed + new float2(.6316f, .82589f)) * 15));
                        curWaypointRotation = (noise.cnoise(dynamicSeed + new float2(.2316f, .32589f))) * 15;
                        curWaypoint = islands[i] + Curves.Rotate(curWaypoint - islands[i], curWaypointRotation);
                        paths.Add(paths[curIndex] + math.normalize(paths[curIndex] - paths[curIndex - 1]) * (20 + (noise.cnoise(dynamicSeed + new float2(.3316f, .52589f)) * 15)));
                        paths.Add(curWaypoint);
                        retraceCount += 2;
                        curIndex += 2;
                        foreach (int2 islandCheck in islands)
                        {
                            if (islandCheck.x == islands[i].x && islandCheck.y == islands[i].y) continue;
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
                            if (curPoint.x <= 0 || curPoint.y <= 0 || curPoint.x >= mapWidth || curPoint.y >= mapHeight || restrictions[(int)curPoint.x * (mapHeight + 1) + (int)curPoint.y] > 0)
                            {
                                paths[paths.Length - 1] = curPoint;
                                paths[paths.Length - 2] = paths[paths.Length - 3] + curWaypointDir * k;
                                break;
                            }
                        }
                    }
                    if (nearIsland || math.lengthsq(paths[paths.Length - 1] - islands[0]) < math.lengthsq(paths[paths.Length - 1] - islands[i]))
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
                    else if (restrictions[(int)paths[paths.Length - 1].x * (mapHeight + 1) + (int)paths[paths.Length - 1].y] > 0)
                    {
                        pathCrossed = restrictions[(int)paths[paths.Length - 1].x * (mapHeight + 1) + (int)paths[paths.Length - 1].y] - 1;
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
                float2 processPoint;
                for (int j = pathWaypointIndices[pathWaypointIndices.Length - 1] + 1; j < paths.Length - 2; j += 2)
                    for (int k = 0; k < 100; k++)
                {
                        processPoint = Curves.QuadCurve(paths[j], paths[j + 1], paths[j + 2], k * .01f);
                    for (int x = (int)processPoint.x - 10; x < processPoint.x + 10; x++)
                        for (int y = (int)processPoint.y - 10; y < processPoint.y + 10; y++)
                            if (x >= 0 && y >= 0 && x <= mapWidth && y <= mapHeight && restrictions[x * (mapHeight + 1) + y] == 0)
                                restrictions[x * (mapHeight + 1) + y] = i + 3;
                    for (int x = (int)processPoint.x - 2; x < processPoint.x + 2; x++)
                        for (int y = (int)processPoint.y - 2; y < processPoint.y + 2; y++)
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
                        pathVerts.Add(new float3((curPoint - curDir * (1f + noise.cnoise(dynamicSeed) / 4)).x, 2.55f + noise.cnoise(dynamicSeed) * .05f, (curPoint - curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).y));
                        pathVerts.Add(new float3((curPoint + curDir * (1f + noise.cnoise(dynamicSeed) / 4)).x, 2.55f + noise.cnoise(dynamicSeed) * .05f, (curPoint + curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).y));
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
                        pathVerts.Add(new float3((curPoint - curDir * (1f + noise.cnoise(dynamicSeed) / 4)).x, 2.55f + noise.cnoise(dynamicSeed) * .05f, (curPoint - curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).y));
                        pathVerts.Add(new float3((curPoint + curDir * (1f + noise.cnoise(dynamicSeed) / 4)).x, 2.55f + noise.cnoise(dynamicSeed) * .05f, (curPoint + curDir * (1.1f + noise.cnoise(dynamicSeed) / 2)).y));
                        pathVerts.Add(new float3((curPoint + curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).x, -.1f, (curPoint + curDir * (2.6f + noise.cnoise(dynamicSeed) / 2)).y));
                    }
                }
            }
        }
    }

    private partial struct UpdatePlantRenders : IJobParallelFor
    {
        public void Execute(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}
