using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public readonly partial struct AspectAssignMap : IAspect
{
    private readonly RefRW<MapComponent> map;
    private readonly RefRW<MapRefComponent> mapRef;
    private readonly RefRW<TowerDataRef> towerData;
    private readonly RefRW<ExplosionsToSpawn> explosions;
    private readonly RefRW<TowerProjectilesToSpawn> projectiles;

    public void Init(NativeArray<float> mapData, NativeArray<int2> islands, NativeList<int> pathOrigins, NativeList<float2> paths, NativeArray<int> pathMap, NativeList<int> pathIndices, int mapWidth, int mapHeight)
    {
        map.ValueRW.mapData = new UnsafeList<float>(mapData.Length, Allocator.Persistent);
        map.ValueRW.vertsToRaise = new UnsafeQueue<float2>(Allocator.Persistent);
        map.ValueRW.soundQueue = new UnsafeQueue<SoundOrder>(Allocator.Persistent);

        map.ValueRW.pathStartStep = new UnsafeList<int>(pathIndices.Length, Allocator.Persistent);
        map.ValueRW.mapWidth = mapWidth; map.ValueRW.mapHeight = mapHeight;
        for (int i = 0; i < mapData.Length; i++)
            map.ValueRW.mapData.Add(mapData[i]);

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
                    if (map.ValueRW.pathStartStep.Length <= k && math.lengthsq(curPoint - islands[l]) < 20000)
                        map.ValueRW.pathStartStep.Add(i * 100 + j);
                    pathStepsBuilder[i * 100 + j] = curPoint;
                    if (j > 0 || i > pathIndices[k] + 1)
                        pathStepLengthBuilder[i * 100 + j - 1] = math.length(pathStepsBuilder[i * 100 + j - 1] - pathStepsBuilder[i * 100 + j]);
                }
            }
            if (k > 1) l++;
        }
        mapRef.ValueRW.contents = blobBuilder.CreateBlobAssetReference<MapRefComponentContents>(Allocator.Persistent);
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
        towerData.ValueRW.contents = blobBuilder.CreateBlobAssetReference<TowerDataContents>(Allocator.Persistent);
        blobBuilder.Dispose();

        explosions.ValueRW.spawns = new UnsafeQueue<ExplosionData>(Allocator.Persistent);
        projectiles.ValueRW.targets = new UnsafeQueue<LaunchData>(Allocator.Persistent);

        // generate resource deposits
        ResMgr.resDepots = new float2[2][];
        float2 curPos = float2.zero;
        float privateSeed = ResMgr.generalSeed;
        bool noGood;
        int depotCount = 2;
        for (int i = 0; i < ResMgr.resDepots.Length; i++)
        {
            ResMgr.resDepots[i] = new float2[depotCount];
            for (int j = 0; j < depotCount; j++)
            {
                noGood = true;
                while (noGood)
                {
                    privateSeed += .123456f;
                    curPos = new float2(500 + noise.snoise(new float2(privateSeed, privateSeed * 1.137f)) * 135, 500 + noise.snoise(new float2(privateSeed * 1.7428f, privateSeed * 1.431786f)) * 135);
                    if (math.lengthsq(curPos - new float2(500, 500)) > 18000 || math.lengthsq(curPos - new float2(500, 500)) < 400) continue;
                    noGood = false;
                    for (int k = 0; k <= i; k++)
                        for (int l = 0; l < ResMgr.resDepots[k].Length; l++)
                            if (math.lengthsq(curPos - ResMgr.resDepots[k][l]) < 400)
                            {
                                noGood = true;
                                break;
                            }
                    if (!noGood)
                    {
                        for (int x = (int)curPos.x - 3; x <= curPos.x + 3; x++)
                            for (int y = (int)curPos.y - 3; y <= curPos.y + 3; y++)
                                if (pathMap[x * (ResMgr.mapHeight + 1) + y] > 0)
                                {
                                    noGood = true;
                                    break;
                                }
                    }
                }
                ResMgr.resDepots[i][j] = curPos;
            }
        }
        ResMgr.spawnResources = true;
    }
}
