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

// DOTS unmanaged system that handles physics interactions (collisions, impulses etc) 
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
[BurstCompile]
public partial struct PhysSystem : ISystem
{
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
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        float time = SystemAPI.Time.DeltaTime;
        MapComponent map = SystemAPI.GetSingleton<MapComponent>();

        // for assigning individual indices to collision events to process them
        NativeReference<int> collisionIndex = new NativeReference<int>(0, Allocator.TempJob);

        // trigger collision event called by Unity.Physics
        // these will be fired by collisions between physical projectiles (physics bodies) and enemies (trigger colliders)
        state.Dependency = new HandleCollisionEvents
        {
            Targetable = SystemAPI.GetComponentLookup<PhysTarget>(true),
            HasVelocity = SystemAPI.GetComponentLookup<PhysicsVelocity>(true),
            ecb = ecb,
            collisionIndex = collisionIndex
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        var ecbRegular = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        JobHandle jobHandle = new ResolveHits
        {
            Motion = SystemAPI.GetComponentLookup<ManualMotion>(true),
            time = time,
            ecb = ecbRegular
        }.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
        jobHandle = new ResolveManualMotion
        {
            map = map,
            ecb = ecb,
            time = time
        }.ScheduleParallel(jobHandle);
        jobHandle = new ResolveProjectileImpulse
        {
            ecb = ecb
        }.ScheduleParallel(jobHandle);
        jobHandle.Complete();
        jobHandle = new PhysSpawnJob
        {
            ecb = ecb
        }.ScheduleParallel(jobHandle);
        jobHandle.Complete();
        collisionIndex.Dispose();
    }


    // handle collision trigger events
    [BurstCompile]
    public partial struct HandleCollisionEvents : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<PhysTarget> Targetable;
        [ReadOnly] public ComponentLookup<PhysicsVelocity> HasVelocity;
        public EntityCommandBuffer.ParallelWriter ecb;
        public NativeReference<int> collisionIndex;

        // functions to check if a target is a physics body with motion and tagged as targetable by projectiles, respectively
        private bool IsDynamic(Entity entity) => HasVelocity.HasComponent(entity);
        private bool IsTargetable(Entity entity) => Targetable.HasComponent(entity);

        public void Execute(Unity.Physics.TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            // ensure the collider calling the trigger event is tagged targetable and the other party is a moving physics body
            if (IsTargetable(entityB) && IsDynamic(entityA))
            {
                // get velocity component and vector value of moving party, turn that into actual velocity
                float3 velocity = HasVelocity[entityA].Linear * .8f;
                float speed = math.sqrt(velocity.x * velocity.x + velocity.y * velocity.y + velocity.z * velocity.z);

                // adjust collision index for the buffer
                collisionIndex.Value++;

                // request buffer to add PhysHit component to target (enemy) entity to process the collision
                if (speed > 1)
                {
                    ecb.AddComponent(collisionIndex.Value * 2, entityB, new PhysHit(entityA, velocity, speed));
                    ecb.AddComponent(collisionIndex.Value * 2 + 1, entityA, new PhysImpulse(-velocity * .1f));
                }
            }
        }
    }

    // handle collisions determined to have been between projectiles and enemies on the projectile side
    [BurstCompile]
    private partial struct ResolveProjectileImpulse : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(AspectProjectileImpulse impulse, ref PhysicsVelocity vel,  Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            impulse.ApplyImpulse(ref vel);

            // PhysHit has been handled, so remove it from this entity
            ecb.RemoveComponent<PhysImpulse>(entityInQueryIndex, entity);
        }
    }

    // handle collisions determined to have been between projectiles and enemies on the enemy side
    [BurstCompile]
    private partial struct ResolveHits : IJobEntity
    {
        [ReadOnly] public ComponentLookup<ManualMotion> Motion;
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public float time;
        public void Execute(ref LocalTransform t, ref PhysHit hit, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            // check if enemy is already in simulated motion; if yes, adjust the motion rather than adding a new component
            if (!Motion.HasComponent(entity))
            {
                ecb.AddComponent(entityInQueryIndex, entity, new ManualMotion { value = hit.dir });
                // remove moving tag so enemy in physical motion doesn't also move towards next waypoint before recovering
                ecb.RemoveComponent<TagMoving>(entityInQueryIndex, entity);
            }
            else
                ecb.SetComponent(entityInQueryIndex, entity, new ManualMotion { value = hit.dir + Motion[entity].value });

            // PhysHit has been handled, so remove it from this entity
            ecb.RemoveComponent<PhysHit>(entityInQueryIndex, entity);
        }
    }

    // IN PROGRESS //
    // simulate motion for enemies, as they don't have physics bodies of their own
    // reasoning: tested with both enemies and projectiles having physics bodies before, performance degraded quickly after around 100 active enemies
    [BurstCompile]
    private partial struct ResolveManualMotion : IJobEntity
    {
        [ReadOnly] public MapComponent map;
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public float time;
        public void Execute(ref LocalTransform t, AspectDwarfMotion dwarf, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            // speculative motion update
            t.Position += dwarf.GetMotion() * time;

            if (t.Position.y <= 2.5f)
            {
                int tile = (int)t.Position.x * (ResMgr.mapHeight + 1) + (int)t.Position.z;
                float groundHeight = Curves.HeightInTile(map.mapData[tile], map.mapData[tile + ResMgr.mapHeight + 1], map.mapData[tile + 1], map.mapData[tile + ResMgr.mapHeight + 2], new float2(t.Position.x - (int)t.Position.x, t.Position.z - (int)t.Position.z));
                if (t.Position.y > groundHeight && map.pathMap[tile] > 0) // if above ground and near path, check if hitting path instead
                {
                    float2 pos = new float2(t.Position.x, t.Position.z);
                    int path = map.pathMap[tile] - 1;
                    // find closest waypoint to position
                    int closest = map.pathIndices[path];
                    for (int i = map.pathIndices[path] + 1; i < map.pathIndices[path + 1]; i++)
                        if (math.lengthsq(map.paths[i] - pos) < math.lengthsq(map.paths[closest] - pos))
                            closest = i;
                    if (closest % 2 == 0) closest--;
                    // get closest estimated point on waypoint curve. More iterations = more accurate
                    for (int i = 0; i < 10; i++)
                    {

                    }
                }

                // if enemy just hit the ground, mark them as grounded and damage them based on the impact speed
                if (!dwarf.GetGrounded())
                {
                    dwarf.SetGrounded(true);
                    dwarf.SetHP(-dwarf.GetMotion().y);
                }
                // keep enemy at ground level
                t.Position = new float3(t.Position.x, 2, t.Position.z);
                dwarf.MultiplyMotion(1 - time * 3);
                // if velocity runs out, stop motion
                if ((dwarf.GetMotion().x * dwarf.GetMotion().x) + (dwarf.GetMotion().z * dwarf.GetMotion().z) < .0001f)
                { 
                    // check if enemy is dead when stopping
                    if (dwarf.GetHP() < 0)
                    {

                    }
                    else
                    {

                    }
                }
            }
            else 
            {
                // apply gravity and drag to motion
                dwarf.MultiplyMotion(1 - 1 * time);
                dwarf.AdjustMotion(new float3(0, -9.81f * time, 0)); 
            }

        }
    }

    // set initial physics force and position of entity with physics velocity
    [BurstCompile]
    private partial struct PhysSpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(ref LocalTransform t, ref PhysicsVelocity p, ref PhysSpawn f, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            p.Linear = f.force;
            t.Position = f.pos;
            ecb.RemoveComponent<PhysSpawn>(entityInQueryIndex, entity);
        }
    }
}
