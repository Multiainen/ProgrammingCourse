using GPUECSAnimationBaker.Engine.AnimatorSystem;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;

// system to manage standard enemy and projectile operations
[BurstCompile]
public partial struct EnemyProjectileSystem : ISystem
{
    private EntityCommandBuffer.ParallelWriter ecb;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        MapComponent map;
        if (SystemAPI.TryGetSingleton<MapComponent>(out map))
        {
            MapRefComponent mapRef = SystemAPI.GetSingleton<MapRefComponent>();
            ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            float time = SystemAPI.Time.DeltaTime;
            JobHandle jobHandle = new SetJob
            {
                ecb = ecb,
                map = map
            }.ScheduleParallel(state.Dependency);
            jobHandle.Complete();
            jobHandle = new PositionTowerJob
            {
                ecb = ecb
            }.ScheduleParallel(state.Dependency);
            jobHandle.Complete();
            jobHandle = new ProjectileDespawnJob
            {
                time = time,
                ecb = ecb
            }.ScheduleParallel(state.Dependency);
            jobHandle.Complete();
            jobHandle = new EnemyDeathJob
            {
                map = map,
                time = time,
                ecb = ecb
            }.ScheduleParallel(state.Dependency);
            jobHandle.Complete();
            jobHandle = new FoliageInitializeJob
            {
                map = map,
                mapRef = mapRef,    
                ecb = ecb,
                seed = time - (int)time
            }.ScheduleParallel(state.Dependency);
            jobHandle.Complete();
        }
    }

    // teleport designated enemies to their designated positions, then tag as needing initial target assignment
    [BurstCompile]
    private partial struct SetJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public MapComponent map;
        public void Execute(ref LocalTransform t, AspectDwarfSet h, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            if (!map.pathStartStep.IsCreated || map.pathStartStep.IsEmpty) return;
            h.SetPos(ref t, map);

            ecb.RemoveComponent<TagSetPos>(entityInQueryIndex, entity);
            ecb.AddComponent(entityInQueryIndex, entity, new ManualMotion { });
        }
    }



    [BurstCompile]
    private partial struct PositionTowerJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(ref LocalTransform t, AspectTowerPosition h, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            h.AssignPos(ref t);

            ecb.RemoveComponent<TagSetPos>(entityInQueryIndex, entity);
            ecb.RemoveComponent<PositionComponent>(entityInQueryIndex, entity);
            ecb.AddComponent<TagAtGoal>(entityInQueryIndex, entity);
        }
    }

    // move despawn timer of projectiles along
    [BurstCompile]
    private partial struct ProjectileDespawnJob : IJobEntity
    {
        [ReadOnly] public float time;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(ref TagProjectile projectile, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            projectile.despawnTimer -= time;
            // destroy projectile if despawn timer runs out
            if (projectile.despawnTimer < 0)
                ecb.DestroyEntity(entityInQueryIndex, entity);
        }
    }

    // move despawn timer of dying enemies along
    [BurstCompile]
    private partial struct EnemyDeathJob : IJobEntity
    {
        public MapComponent map;
        [ReadOnly] public float time;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(ref LocalTransform t, ref TagKillEnemy timer, GpuEcsAnimatorAspect animator, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            timer.timer -= time;
            t.Position -= new float3(0, time * .08f, 0);
            // destroy enemy if despawn timer runs out
            if (timer.timer < 0)
            {
                ecb.DestroyEntity(entityInQueryIndex, entity);
                // decompose and raise ground if below walkable level
                map.vertsToRaise.Enqueue(new float2(t.Position.x, t.Position.z));
            }
            // trigger death animation if not yet triggered
            else if (!timer.animTriggered)
            {
                timer.animTriggered = true;
                animator.RunAnimation(1);
            }
        }
    }

    [BurstCompile]
    private partial struct FoliageInitializeJob : IJobEntity
    {
        public MapComponent map;
        [ReadOnly] public MapRefComponent mapRef;
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public float seed;
        public void Execute(ref LocalTransform t, AspectPlantInit foliage, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            ref MapRefComponentContents contents = ref mapRef.contents.Value;
            float2 pos = new float2((noise.cnoise(new float2(seed + .174f * entityInQueryIndex)) + .5f) * ResMgr.mapHeight, (noise.cnoise(new float2(seed + .8316f * entityInQueryIndex)) + .5f) * ResMgr.mapHeight);
            while (math.lengthsq(pos - new float2(500, 500)) < 150 || pos.y <= 0 || pos.y >= 1000 || pos.x <= 0 || pos.x >= 1000 || contents.pathMap[(int)pos.x * (ResMgr.mapHeight + 1) + (int)pos.y] > 0)
            {
                seed += .178176f;
                pos = new float2((noise.cnoise(new float2(seed + .174f * entityInQueryIndex)) + .5f) * ResMgr.mapHeight, (noise.cnoise(new float2(seed + .8316f * entityInQueryIndex)) + .5f) * ResMgr.mapHeight);
            }
            float scale = (noise.cnoise(new float2(seed + .5167884f * entityInQueryIndex)) + 1) * .9f;
            if (scale < 1) scale += (1 - scale) * .5f;
            foliage.SetPos(ref t, new float3(pos.x, map.mapData[(int)pos.x * (ResMgr.mapHeight + 1) + (int)pos.y], pos.y), noise.cnoise(new float2(seed + .3516f * entityInQueryIndex)) * 360, scale);
            
            if (pos.x > ResMgr.mapWidth / 2)
            {
                if (pos.x > ResMgr.mapWidth / 4 * 3)
                {
                    if (pos.x > ResMgr.mapWidth / 8 * 7)
                    {
                        if (pos.y > ResMgr.mapWidth / 2)
                        {
                            if (pos.y > ResMgr.mapWidth / 4 * 3)
                            {
                                if (pos.y > ResMgr.mapWidth / 8 * 7)
                                    ecb.AddComponent<Chunk63>(entityInQueryIndex, entity);
                                else
                                    ecb.AddComponent<Chunk62>(entityInQueryIndex, entity);
                            }
                            else
                            {
                                if (pos.y > ResMgr.mapWidth / 8 * 5)
                                    ecb.AddComponent<Chunk61>(entityInQueryIndex, entity);
                                else
                                    ecb.AddComponent<Chunk60>(entityInQueryIndex, entity);
                            }
                        }
                        else
                        {
                            if (pos.y > ResMgr.mapWidth / 4)
                            {
                                if (pos.y > ResMgr.mapWidth / 8 * 3)
                                    ecb.AddComponent<Chunk59>(entityInQueryIndex, entity);
                                else
                                    ecb.AddComponent<Chunk58>(entityInQueryIndex, entity);
                            }
                            else
                            {
                                if (pos.y > ResMgr.mapWidth / 8)
                                    ecb.AddComponent<Chunk57>(entityInQueryIndex, entity);
                                else
                                    ecb.AddComponent<Chunk56>(entityInQueryIndex, entity);
                            }
                        }
                    }
                    else if (pos.y > ResMgr.mapWidth / 2)
                    {
                        if (pos.y > ResMgr.mapWidth / 4 * 3)
                        {
                            if (pos.y > ResMgr.mapWidth / 8 * 7)
                                ecb.AddComponent<Chunk55>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk54>(entityInQueryIndex, entity);
                        }
                        else
                        {
                            if (pos.y > ResMgr.mapWidth / 8 * 5)
                                ecb.AddComponent<Chunk53>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk52>(entityInQueryIndex, entity);
                        }
                    }
                    else
                    {
                        if (pos.y > ResMgr.mapWidth / 4)
                        {
                            if (pos.y > ResMgr.mapWidth / 8 * 3)
                                ecb.AddComponent<Chunk51>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk50>(entityInQueryIndex, entity);
                        }
                        else
                        {
                            if (pos.y > ResMgr.mapWidth / 8)
                                ecb.AddComponent<Chunk49>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk48>(entityInQueryIndex, entity);
                        }
                    }
                }
                else if (pos.x > ResMgr.mapWidth / 8 * 5)
                {
                    if (pos.y > ResMgr.mapWidth / 2)
                    {
                        if (pos.y > ResMgr.mapWidth / 4 * 3)
                        {
                            if (pos.y > ResMgr.mapWidth / 8 * 7)
                                ecb.AddComponent<Chunk47>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk46>(entityInQueryIndex, entity);
                        }
                        else
                        {
                            if (pos.y > ResMgr.mapWidth / 8 * 5)
                                ecb.AddComponent<Chunk45>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk44>(entityInQueryIndex, entity);
                        }
                    }
                    else
                    {
                        if (pos.y > ResMgr.mapWidth / 4)
                        {
                            if (pos.y > ResMgr.mapWidth / 8 * 3)
                                ecb.AddComponent<Chunk43>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk42>(entityInQueryIndex, entity);
                        }
                        else
                        {
                            if (pos.y > ResMgr.mapWidth / 8)
                                ecb.AddComponent<Chunk41>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk40>(entityInQueryIndex, entity);
                        }
                    }
                }
                else if (pos.y > ResMgr.mapWidth / 2)
                {
                    if (pos.y > ResMgr.mapWidth / 4 * 3)
                    {
                        if (pos.y > ResMgr.mapWidth / 8 * 7)
                            ecb.AddComponent<Chunk39>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk38>(entityInQueryIndex, entity);
                    }
                    else
                    {
                        if (pos.y > ResMgr.mapWidth / 8 * 5)
                            ecb.AddComponent<Chunk37>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk36>(entityInQueryIndex, entity);
                    }
                }
                else
                {
                    if (pos.y > ResMgr.mapWidth / 4)
                    {
                        if (pos.y > ResMgr.mapWidth / 8 * 3)
                            ecb.AddComponent<Chunk35>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk34>(entityInQueryIndex, entity);
                    }
                    else
                    {
                        if (pos.y > ResMgr.mapWidth / 8)
                            ecb.AddComponent<Chunk33>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk32>(entityInQueryIndex, entity);
                    }
                }
            }
            else if (pos.x > ResMgr.mapWidth / 4 * 3)
            {
                if (pos.x > ResMgr.mapWidth / 8 * 7)
                {
                    if (pos.y > ResMgr.mapWidth / 2)
                    {
                        if (pos.y > ResMgr.mapWidth / 4 * 3)
                        {
                            if (pos.y > ResMgr.mapWidth / 8 * 7)
                                ecb.AddComponent<Chunk31>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk30>(entityInQueryIndex, entity);
                        }
                        else
                        {
                            if (pos.y > ResMgr.mapWidth / 8 * 5)
                                ecb.AddComponent<Chunk29>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk28>(entityInQueryIndex, entity);
                        }
                    }
                    else
                    {
                        if (pos.y > ResMgr.mapWidth / 4)
                        {
                            if (pos.y > ResMgr.mapWidth / 8 * 3)
                                ecb.AddComponent<Chunk27>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk26>(entityInQueryIndex, entity);
                        }
                        else
                        {
                            if (pos.y > ResMgr.mapWidth / 8)
                                ecb.AddComponent<Chunk25>(entityInQueryIndex, entity);
                            else
                                ecb.AddComponent<Chunk24>(entityInQueryIndex, entity);
                        }
                    }
                }
                else if (pos.y > ResMgr.mapWidth / 2)
                {
                    if (pos.y > ResMgr.mapWidth / 4 * 3)
                    {
                        if (pos.y > ResMgr.mapWidth / 8 * 7)
                            ecb.AddComponent<Chunk23>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk22>(entityInQueryIndex, entity);
                    }
                    else
                    {
                        if (pos.y > ResMgr.mapWidth / 8 * 5)
                            ecb.AddComponent<Chunk21>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk20>(entityInQueryIndex, entity);
                    }
                }
                else
                {
                    if (pos.y > ResMgr.mapWidth / 4)
                    {
                        if (pos.y > ResMgr.mapWidth / 8 * 3)
                            ecb.AddComponent<Chunk19>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk18>(entityInQueryIndex, entity);
                    }
                    else
                    {
                        if (pos.y > ResMgr.mapWidth / 8)
                            ecb.AddComponent<Chunk17>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk16>(entityInQueryIndex, entity);
                    }
                }
            }
            else if (pos.x > ResMgr.mapWidth / 8 * 5)
            {
                if (pos.y > ResMgr.mapWidth / 2)
                {
                    if (pos.y > ResMgr.mapWidth / 4 * 3)
                    {
                        if (pos.y > ResMgr.mapWidth / 8 * 7)
                            ecb.AddComponent<Chunk15>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk14>(entityInQueryIndex, entity);
                    }
                    else
                    {
                        if (pos.y > ResMgr.mapWidth / 8 * 5)
                            ecb.AddComponent<Chunk13>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk12>(entityInQueryIndex, entity);
                    }
                }
                else
                {
                    if (pos.y > ResMgr.mapWidth / 4)
                    {
                        if (pos.y > ResMgr.mapWidth / 8 * 3)
                            ecb.AddComponent<Chunk11>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk10>(entityInQueryIndex, entity);
                    }
                    else
                    {
                        if (pos.y > ResMgr.mapWidth / 8)
                            ecb.AddComponent<Chunk9>(entityInQueryIndex, entity);
                        else
                            ecb.AddComponent<Chunk8>(entityInQueryIndex, entity);
                    }
                }
            }
            else if (pos.y > ResMgr.mapWidth / 2)
            {
                if (pos.y > ResMgr.mapWidth / 4 * 3)
                {
                    if (pos.y > ResMgr.mapWidth / 8 * 7)
                        ecb.AddComponent<Chunk7>(entityInQueryIndex, entity);
                    else
                        ecb.AddComponent<Chunk6>(entityInQueryIndex, entity);
                }
                else
                {
                    if (pos.y > ResMgr.mapWidth / 8 * 5)
                        ecb.AddComponent<Chunk5>(entityInQueryIndex, entity);
                    else
                        ecb.AddComponent<Chunk4>(entityInQueryIndex, entity);
                }
            }
            else
            {
                if (pos.y > ResMgr.mapWidth / 4)
                {
                    if (pos.y > ResMgr.mapWidth / 8 * 3)
                        ecb.AddComponent<Chunk3>(entityInQueryIndex, entity);
                    else
                        ecb.AddComponent<Chunk2>(entityInQueryIndex, entity);
                }
                else
                {
                    if (pos.y > ResMgr.mapWidth / 8)
                        ecb.AddComponent<Chunk1>(entityInQueryIndex, entity);
                    else
                        ecb.AddComponent<Chunk0>(entityInQueryIndex, entity);
                }
            }
            ecb.RemoveComponent<TagInitialize>(entityInQueryIndex, entity);

        }
    }

}


public static class BlobAssetReferenceExtensions
{
    public static unsafe bool IsValid<T>(ref BlobArray<T> blob) where T : unmanaged
    {
        return blob.GetUnsafePtr() != null;
    }
}


