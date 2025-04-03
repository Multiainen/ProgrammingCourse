using System;
using System.Drawing;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
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
        TowerProjectilesToSpawn projectileSpawner;
        if (SystemAPI.TryGetSingleton<TowerProjectilesToSpawn>(out projectileSpawner))
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            float time = SystemAPI.Time.DeltaTime;
            MapComponent map = SystemAPI.GetSingleton<MapComponent>();
            MapRefComponent mapRef = SystemAPI.GetSingleton<MapRefComponent>();
            TowerDataRef towerRef = SystemAPI.GetSingleton<TowerDataRef>();
            ExplosionsToSpawn explosions = SystemAPI.GetSingleton<ExplosionsToSpawn>();
            var hasVelocity = SystemAPI.GetComponentLookup<PhysicsVelocity>(true);
            var targetable = SystemAPI.GetComponentLookup<PhysTarget>(true);

            // for assigning individual indices to collision events to process them
            NativeReference<int> collisionIndex = new NativeReference<int>(0, Allocator.TempJob);

            // trigger collision event called by Unity.Physics
            // these will be fired by collisions between physical projectiles (physics bodies) and enemies (trigger colliders)
            state.Dependency = new HandleTriggerEvents
            {
                Tf = SystemAPI.GetComponentLookup<LocalTransform>(true),
                Targetable = targetable,
                HasVelocity = hasVelocity,
                TowerTargets = SystemAPI.GetComponentLookup<TowerTargetsComponent>(true),
                TowerTargetTag = SystemAPI.GetComponentLookup<TowerTargetingTag>(true),
                Projectile = SystemAPI.GetComponentLookup<TagProjectile>(true),
                collisionWorld = physicsWorld.CollisionWorld,
                tRef = towerRef,
                explosions = explosions,
                ecb = ecb,
                collisionIndex = collisionIndex
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency = new HandleCollisionEvents
            {
                Targetable = targetable,
                ecb = ecb, 
                collisionIndex = collisionIndex,
                explosions = explosions,
                physicsWorld = physicsWorld.PhysicsWorld
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            var ecbRegular = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            JobHandle jobHandle = new ResolveHits
            {
                map = map,
                ecb = ecbRegular
            }.ScheduleParallel(state.Dependency);
            jobHandle.Complete();
            if (mapRef.contents.IsCreated)
            {
                jobHandle = new ResolveManualMotion
                {
                    map = map,
                    mRef = mapRef,
                    ecb = ecbRegular,
                    time = time
                }.ScheduleParallel(jobHandle);
                jobHandle.Complete();
            }
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
            jobHandle = new TowerShootJob
            {
                ecb = ecb,
                spawner = projectileSpawner,
                DwarfTargets = SystemAPI.GetComponentLookup<DwarfTarget>(true),
                map = map,
                mRef = mapRef,
                tRef = towerRef
            }.ScheduleParallel(jobHandle);
            jobHandle.Complete();
            jobHandle = new TowerCooldown
            {
                time = time,
                ecb = ecb
            }.ScheduleParallel(jobHandle);
            jobHandle.Complete();
            collisionIndex.Dispose();

        }
    }

    // explosive collision handling
    [BurstCompile]
    public partial struct HandleCollisionEvents : ICollisionEventsJob
    {
        [ReadOnly] public ComponentLookup<PhysTarget> Targetable;
        public EntityCommandBuffer.ParallelWriter ecb;
        public NativeReference<int> collisionIndex;
        public PhysicsWorld physicsWorld;
        public ExplosionsToSpawn explosions;
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;
            var details = collisionEvent.CalculateDetails(ref physicsWorld);
            float3 point = details.AverageContactPointPosition - new float3(0, .3f, 0);
            float3 velocity;
            ecb.DestroyEntity(collisionIndex.Value * 2, entityA);
            explosions.spawns.Enqueue(new ExplosionData { pos = point, force = 100, directDamage = 20 });
            NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
            physicsWorld.CollisionWorld.OverlapSphere(point, 6, ref hits, new CollisionFilter { BelongsTo = 16, CollidesWith = 1, GroupIndex = 5 });
            for (int i = 0; i < hits.Length; i++)
            {
                if (!Targetable.HasComponent(hits[i].Entity)) continue;
                collisionIndex.Value++;
                if (hits[i].Distance > .5f)
                    velocity = (hits[i].Position - point) * 20 / hits[i].Distance;
                else
                    velocity = (hits[i].Position - point) * 40;
                ecb.AddComponent(collisionIndex.Value * 2, hits[i].Entity, new PhysHit(hits[i].Entity, velocity + new float3(0, math.length(velocity) * .5f - velocity.y * .5f, 0), 20));
            }
        }
    }


    // handle collision trigger events
    [BurstCompile]
    public partial struct HandleTriggerEvents : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<LocalTransform> Tf;
        [ReadOnly] public ComponentLookup<PhysTarget> Targetable;
        [ReadOnly] public ComponentLookup<PhysicsVelocity> HasVelocity;
        [ReadOnly] public ComponentLookup<TowerTargetsComponent> TowerTargets;
        [ReadOnly] public ComponentLookup<TowerTargetingTag> TowerTargetTag;
        [ReadOnly] public ComponentLookup<TagProjectile> Projectile;
        [ReadOnly] public TowerDataRef tRef;
        public CollisionWorld collisionWorld;
        public ExplosionsToSpawn explosions;
        public EntityCommandBuffer.ParallelWriter ecb;
        public NativeReference<int> collisionIndex;

        // functions to check if a target is a physics body with motion and tagged as targetable by projectiles, respectively
        private bool IsDynamic(Entity entity) => HasVelocity.HasComponent(entity);
        private bool IsTargetable(Entity entity) => Targetable.HasComponent(entity);

        public void Execute(Unity.Physics.TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            // ensure the collider calling the trigger event is tagged targetable for physics-based collisions
            if (IsTargetable(entityB) && IsDynamic(entityA)) 
            {
                if (!TowerTargets.HasComponent(entityA)) // is projectile
                {
                    ref TowerDataContents towerData = ref tRef.contents.Value;
                    // get velocity component and vector value of moving party, turn that into actual velocity
                    float3 velocity = HasVelocity[entityA].Linear * .8f;

                    // adjust collision index for the buffer
                    collisionIndex.Value++;

                    // request buffer to add PhysHit component to target (enemy) entity to process the collision
                    if (math.lengthsq(velocity) > .5f)
                    {
                        ecb.AddComponent(collisionIndex.Value * 2, entityB, new PhysHit(entityA, velocity * towerData.projectileMass[Projectile[entityA].type], math.length(velocity) * towerData.projectileMass[Projectile[entityA].type] * towerData.projectileSharpness[Projectile[entityA].type]));
                        if (towerData.projectileMass[Projectile[entityA].type] > .1f)
                            ecb.AddComponent(collisionIndex.Value * 2 + 1, entityA, new PhysImpulse(1 - (.08f / towerData.projectileMass[Projectile[entityA].type])));
                        else
                            ecb.AddComponent(collisionIndex.Value * 2 + 1, entityA, new PhysImpulse(-towerData.projectileMass[Projectile[entityA].type]));
                    }
                }
                else // is tower scanning for targets
                {
                    collisionIndex.Value++;
                    TowerTargets[entityA].targets.Enqueue(entityB);
                    if (!TowerTargetTag.HasComponent(entityA))
                        ecb.AddComponent<TowerTargetingTag>(collisionIndex.Value * 2, entityA);
                }
            }
        }
    }

    [BurstCompile]
    private partial struct TowerShootJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public TowerProjectilesToSpawn spawner;
        public MapComponent map;
        [ReadOnly] public TowerDataRef tRef;
        [ReadOnly] public ComponentLookup<DwarfTarget> DwarfTargets;
        [ReadOnly] public MapRefComponent mRef;
        public void Execute(ref LocalTransform transform, AspectTowerShoot tower, ref PhysicsCollider collider, ref TowerComponent towerInfo, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            ref TowerDataContents tData = ref tRef.contents.Value;
            float3 pos = transform.Position + new float3(0, towerInfo.shotHeight, 0);
            float3 velocity = tower.LaunchData(DwarfTargets, ref mRef.contents.Value, pos, tData.projectileRadius[towerInfo.towerType]) * .6f;

            if (math.lengthsq(velocity) > 1)
            {
                spawner.targets.Enqueue(new LaunchData { pos = pos, force = velocity, type = tData.projectile[towerInfo.towerType] });
                towerInfo.cooldown = tData.cooldown[towerInfo.towerType];
                ecb.AddComponent(entityInQueryIndex, entity, new StoredCollider { Collider = collider.Value });
                collider.Value = default;
                if (towerInfo.towerType == 2)
                    map.soundQueue.Enqueue(new SoundOrder(transform.Position, 1));
                else if (towerInfo.towerType == 1)
                    map.soundQueue.Enqueue(new SoundOrder(transform.Position, 5));
                else
                    map.soundQueue.Enqueue(new SoundOrder(transform.Position, 4));
            }
            ecb.RemoveComponent<TowerTargetingTag>(entityInQueryIndex, entity);
        }
    }

    [BurstCompile]
    private partial struct TowerCooldown : IJobEntity
    {
        [ReadOnly] public float time;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(ref TowerComponent towerInfo, StoredCollider storedCollider, ref PhysicsCollider collider, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            if (towerInfo.cooldown < time)
            {
                collider.Value = storedCollider.Collider;

                // Remove the stored collider component since it's no longer needed
                ecb.RemoveComponent<StoredCollider>(entityInQueryIndex, entity);
            }
            else towerInfo.cooldown -= time;
        }
    }

    // handle collisions determined to have been between projectiles and enemies on the projectile side
    [BurstCompile]
    private partial struct ResolveProjectileImpulse : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(AspectProjectileImpulse impulse, ref PhysicsVelocity vel, ref PhysicsCollider collider, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            impulse.ApplyImpulse(ref vel);
            if (math.lengthsq(vel.Linear) < .1f) // Destroy projectile if it's stopped
            {
                collider.Value = default;
                impulse.SetDecayTimer(.1f);
            } 
            else
            // PhysHit has been handled, so remove it from this entity
                ecb.RemoveComponent<PhysImpulse>(entityInQueryIndex, entity);
        }
    }

    // handle collisions determined to have been between projectiles and enemies on the enemy side
    [BurstCompile]
    private partial struct ResolveHits : IJobEntity
    {
        public MapComponent map;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(ref PhysHit hit, LocalTransform transform, AspectDwarfMotion dwarf, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            float magnitude = math.length(hit.dir);
            dwarf.AdjustHP(-magnitude - math.lengthsq(hit.dir) * .1f - hit.damage);
            if (dwarf.GetForceEnabled())
                dwarf.AdjustMotion(hit.dir * .5f);
            else
            {
                if (entityInQueryIndex % 5 == 0)
                    map.soundQueue.Enqueue(new SoundOrder(transform.Position, 2));
                dwarf.SetForceEnabled(true);
                dwarf.SetOffPath(true);
                dwarf.SetMotion(hit.dir + new float3(0, magnitude * .2f, 0));
            }

            // PhysHit has been handled, so remove it from this entity
            ecb.RemoveComponent<PhysHit>(entityInQueryIndex, entity);
        }
    }

    // simulate motion for enemies, as they don't have physics bodies of their own
    // reasoning: tested with both enemies and projectiles having physics bodies before, performance degraded quickly after around 100 active enemies
    [BurstCompile]
    private partial struct ResolveManualMotion : IJobEntity
    {
        [ReadOnly] public MapComponent map;
        [ReadOnly] public MapRefComponent mRef;
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public float time;
        public void Execute(ref LocalTransform t, AspectDwarfMotion dwarf, PhysicsCollider collider, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            if (!dwarf.GetForceEnabled())
            {
                if (!dwarf.GetOffPath())
                {
                    if (!dwarf.GetAtGoal() && dwarf.Move(time * 1.2f, map, ref mRef.contents.Value, ref t, entity))
                    {
                        dwarf.SetAtGoal(true);
                        ecb.RemoveComponent<ManualMotion>(entityInQueryIndex, entity);
                        ecb.AddComponent(entityInQueryIndex, entity, new TagAtGoal { });
                    }
                }
                else if (dwarf.MoveToPath(time * 1.2f, ref mRef.contents.Value, ref t))
                    dwarf.SetOffPath(false);
            }
            else
            {
                if (t.Position.y <= 2.5001f && dwarf.GetMotion().y <= 0)
                {
                    ref MapRefComponentContents mapRef = ref mRef.contents.Value;
                    int tile = (int)t.Position.x * (ResMgr.mapHeight + 1) + (int)t.Position.z;
                    if (tile + ResMgr.mapHeight + 2 >= 1002001 || tile < 0)
                    {
                        //collider.Value.Value.SetCollisionResponse(CollisionResponsePolicy.None);
                        ecb.AddComponent(entityInQueryIndex, entity, new TagKillEnemy { timer = 1 });
                        ecb.RemoveComponent<PhysTarget>(entityInQueryIndex, entity);
                        return;
                    }
                    float groundHeight = Curves.HeightInTile(map.mapData[tile], map.mapData[tile + ResMgr.mapHeight + 1], map.mapData[tile + 1], map.mapData[tile + ResMgr.mapHeight + 2], new float2(t.Position.x - (int)t.Position.x, t.Position.z - (int)t.Position.z));
                    if (t.Position.y > groundHeight && mapRef.pathMap[tile] > 0) // if above ground and near path, check if hitting path instead
                    {
                        float2 pos = new float2(t.Position.x, t.Position.z);
                        int path = mapRef.pathMap[tile] - 1;
                        // find closest waypoint to position
                        int closest = mapRef.pathIndices[path];
                        for (int i = mapRef.pathIndices[path] + 1; i < mapRef.pathIndices[path + 1] - 1; i++)
                            if (math.lengthsq(mapRef.paths[i] - pos) < math.lengthsq(mapRef.paths[closest] - pos))
                                closest = i;
                        if (closest % 2 == 0)
                        {
                            closest--;
                        }
                        else if (math.lengthsq(mapRef.paths[closest - 1] - pos) < math.lengthsq(mapRef.paths[closest - 1] - mapRef.paths[closest]))
                            closest -= 2;
                        // get closest estimated point on waypoint curve. More iterations = more accurate; should cap at 18
                        float point = .5f, offset = .25f;
                        for (int i = 0; i < 18; i++)
                        {
                            if (math.lengthsq(Curves.QuadCurve(mapRef.paths[closest], mapRef.paths[closest + 1], mapRef.paths[closest + 2], point - offset) - pos)
                                < math.lengthsq(Curves.QuadCurve(mapRef.paths[closest], mapRef.paths[closest + 1], mapRef.paths[closest + 2], point + offset) - pos))
                                point -= offset;
                            else point += offset;
                            offset /= 2;
                        }
                        float2 comparePos = Curves.QuadCurve(mapRef.paths[closest], mapRef.paths[closest + 1], mapRef.paths[closest + 2], point);
                        if (math.lengthsq(comparePos - pos) < 1)
                        {
                            if (!dwarf.GetGrounded())
                            {
                                dwarf.SetGrounded(true);
                                dwarf.AdjustHP(dwarf.GetMotion().y);
                            }
                            dwarf.MultiplyMotion(1 - time * 5);
                            t.Position = new float3(t.Position.x, 2.5f, t.Position.z);
                            dwarf.SetMotion(new float3(dwarf.GetMotion().x, 0, dwarf.GetMotion().z));
                        }
                        else
                        {
                            float dist = math.length(comparePos - pos) - 1;
                            if (t.Position.y <= 2.5f - (dist / .6f))
                            {
                                if (!dwarf.GetGrounded())
                                {
                                    dwarf.SetGrounded(true);
                                    if (t.Position.y > 2)
                                        dwarf.AdjustHP(dwarf.GetMotion().y);
                                    else
                                        dwarf.AdjustHP(-99999);
                                }
                                dwarf.MultiplyMotion(1 - time * 5);
                                t.Position = new float3(t.Position.x, 2.5f - (dist / .6f), t.Position.z);
                                dwarf.SetMotion(new float3(dwarf.GetMotion().x, 0, dwarf.GetMotion().z));
                            }
                            else
                            {
                                dwarf.SetGrounded(false);
                                if (t.Position.y < 2)
                                {
                                    dwarf.MultiplyMotion(1 - 5 * time);
                                    dwarf.AdjustMotion(new float3(0, -5f * time, 0));
                                }
                                else
                                {
                                    // apply gravity and drag to motion
                                    dwarf.MultiplyMotion(1 - 1 * time);
                                    dwarf.AdjustMotion(new float3(0, -9.81f * time, 0));
                                }
                            }
                        }
                        if ((dwarf.GetMotion().x * dwarf.GetMotion().x) + (dwarf.GetMotion().z * dwarf.GetMotion().z) < .0001f)
                        {
                            // check if enemy is dead when stopping
                            if (dwarf.GetHP() <= 0)
                            {
                                //collider.Value.Value.SetCollisionResponse(CollisionResponsePolicy.None);
                                ecb.AddComponent(entityInQueryIndex, entity, new TagKillEnemy { timer = 10 });
                                ecb.RemoveComponent<PhysTarget>(entityInQueryIndex, entity);
                            }
                            else
                            {
                                dwarf.SetForceEnabled(false);
                                dwarf.SetMotion(0);
                                dwarf.SetGrounded(false);
                            }
                        }
                    }
                    else if (t.Position.y <= groundHeight)
                    {
                        if (!dwarf.GetGrounded())
                        {
                            dwarf.SetGrounded(true);
                            // if enemy just hit the ground underwater, kill them; above water, damage based on impact velocity
                            if (t.Position.y <= 2f)
                            {
                                dwarf.AdjustHP(-99999);
                                dwarf.MultiplyMotion(0);
                            }
                            else
                            {
                                dwarf.AdjustHP(dwarf.GetMotion().y);
                            }
                        }
                        dwarf.MultiplyMotion(1 - time * 10);
                        // keep enemy at ground level
                        t.Position = new float3(t.Position.x, groundHeight, t.Position.z);
                        // if velocity runs out, stop motion
                        if ((dwarf.GetMotion().x * dwarf.GetMotion().x) + (dwarf.GetMotion().z * dwarf.GetMotion().z) < .0001f)
                        {
                            // check if enemy is dead when stopping
                            if (dwarf.GetHP() <= 0)
                            {
                                //collider.Value.Value.SetCollisionResponse(CollisionResponsePolicy.None);
                                ecb.AddComponent(entityInQueryIndex, entity, new TagKillEnemy { timer = 10 });
                                ecb.RemoveComponent<PhysTarget>(entityInQueryIndex, entity);
                            }
                            else
                            {
                                dwarf.SetForceEnabled(false);
                                dwarf.SetMotion(0);
                                dwarf.SetGrounded(false);
                            }
                        }
                    }
                    else
                    {
                        dwarf.SetGrounded(false);
                        if (t.Position.y < 2)
                        {
                            if (t.Position.y - dwarf.GetMotion().y >= 2)
                                map.soundQueue.Enqueue(new SoundOrder(t.Position, 3));
                            dwarf.MultiplyMotion(1 - 5 * time);
                            dwarf.AdjustMotion(new float3(0, -5f * time, 0));
                        }
                        else
                        {
                            // apply gravity and drag to motion
                            dwarf.MultiplyMotion(1 - 1 * time);
                            dwarf.AdjustMotion(new float3(0, -9.81f * time, 0));
                        }
                    }
                }
                else
                {
                    dwarf.SetGrounded(false);
                    // apply gravity and drag to motion
                    dwarf.MultiplyMotion(1 - 1 * time);
                    dwarf.AdjustMotion(new float3(0, -9.81f * time, 0));
                }
                t.Position += dwarf.GetMotion() * time;
            }
        }
    }

    // set initial physics force and position of entity with physics velocity
    [BurstCompile]
    private partial struct PhysSpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(ref LocalTransform t, ref PhysicsVelocity p, ref PhysSpawn f, ref TagProjectile projectile, Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            p.Linear = f.force;
            t.Position = f.pos;
            t.Rotation = quaternion.LookRotation(new float3(f.force.x, f.force.y * .2f, f.force.z), new float3(0, 1, 0));

            ecb.RemoveComponent<PhysSpawn>(entityInQueryIndex, entity);
        }
    }
}
