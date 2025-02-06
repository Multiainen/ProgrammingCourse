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
        ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        float time = SystemAPI.Time.DeltaTime;
        JobHandle jobHandle = new MoveJob
        {
            time = time,
            ecb = ecb
        }.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
        jobHandle = new SetJob
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
            time = time,
            ecb = ecb
        }.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
    }

    // move enemies currently set to move along towards their next waypoint
    [BurstCompile]
    private partial struct MoveJob : IJobEntity
    {
        public float time;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(ref LocalTransform t, AspectDwarfMoving h, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            h.Move(time, ref t, entity);

            // if reached target and not already setting new target, assign tag to set new target and mark as setting
            if (math.distancesq(h.GetTarget(), t.Position) < .01f && h.GetSettingTarget() != 1)
            {
                ecb.AddComponent(entityInQueryIndex, entity, new TagNewTarget { });
                h.SetSettingTarget(1);
            }
        }
    }

    // teleport designated enemies to their designated positions, then tag as needing initial target assignment
    [BurstCompile]
    private partial struct SetJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(ref LocalTransform t, AspectDwarfSet h, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            h.SetPos(ref t);

            ecb.RemoveComponent<TagSetPos>(entityInQueryIndex, entity);
            ecb.AddComponent(entityInQueryIndex, entity, new TagFirstTarget { });
        }
    }

    // move despawn timer of projectiles along
    [BurstCompile]
    private partial struct ProjectileDespawnJob : IJobEntity
    {
        public float time;
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
        public float time;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(ref TagKillEnemy timer, GpuEcsAnimatorAspect animator, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            timer.timer -= time;
            // destroy enemy if despawn timer runs out
            if (timer.timer < 0)
                ecb.DestroyEntity(entityInQueryIndex, entity);
            // trigger death animation if not yet triggered
            else if (!timer.animTriggered)
            {
                timer.animTriggered = true;
                animator.RunAnimation(2);
            }
        }
    }
}


