using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems.Projectiles {

    // Fuse Countdown for Grenade Projectile-Prefab. After fuse expires its explodes.
    [BurstCompile]
    public partial struct GrenadeFuseExplosionSystem : ISystem {
        private EntityCommandBuffer.ParallelWriter GetEntityCommandBufferParallel(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var parallelEcb = GetEntityCommandBufferParallel(ref state);

            foreach (var (grenade, transform, projectileComponent, entity) in
                     SystemAPI.Query<RefRW<GrenadeComponent>, RefRW<LocalTransform>, RefRW<ProjectileComponent>>().WithEntityAccess().WithAll<StartFuseCountdown>()) {
                grenade.ValueRW.FuseDuration -= SystemAPI.Time.DeltaTime;

                if (grenade.ValueRW.FuseDuration <= 0f) {
                    ecb.DestroyEntity(entity);
                    new GrenadeExplosionSpawnerJob {
                        ecb = parallelEcb,
                        position = transform.ValueRO.Position,
                        projectileComponent = projectileComponent.ValueRO,
                    }.ScheduleParallel();
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    // Spawns Explosion Particle that has Collider-Trigger detection.
    [BurstCompile]
    public partial struct GrenadeExplosionSpawnerJob : IJobEntity {
        public EntityCommandBuffer.ParallelWriter ecb;
        public Vector3 position;
        public ProjectileComponent projectileComponent;

        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int chunkIndex, ref EntityData spawner, ProjectileSpawner projectileSpawner) {
            Entity projectileEntity = ecb.Instantiate(chunkIndex, spawner.grenadeExplosionPrefab);
            ecb.AddComponent(chunkIndex, projectileEntity, new LocalTransform {
                Position = position,
                Rotation = Quaternion.identity,
                Scale = 0.5f,
            });
            ecb.AddComponent(chunkIndex, projectileEntity, new ExplosionTag { lifeTime = 3.0f });
            ecb.AddComponent(chunkIndex, projectileEntity, new ProjectileComponent {
                BaseDamage = projectileComponent.BaseDamage,
                Lifetime = 10f,
                Velocity = 0f,
                Speed = 0f
            });
        }
    }

    // Clears Explosion Particle after some amount
    [BurstCompile]
    public partial struct ClearExplosionParticleEntity : ISystem {
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, explosionTag, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<ExplosionTag>>().WithEntityAccess()) {
                explosionTag.ValueRW.lifeTime -= SystemAPI.Time.DeltaTime;
                explosionTag.ValueRW.elapsedExplosionTime += SystemAPI.Time.DeltaTime;

                if (explosionTag.ValueRW.elapsedExplosionTime >= 0.1f) {
                    if (state.EntityManager.HasComponent<PhysicsCollider>(entity)) {
                        ecb.RemoveComponent<PhysicsCollider>(entity);
                        continue;
                    }
                }

                if (explosionTag.ValueRW.lifeTime <= 0) {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    // Grenade Projectile Path when Throwing (it goes in parabolic)
    [BurstCompile]
    public partial struct GrenadeSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (grenade, transform, entity) in
                     SystemAPI.Query<RefRW<GrenadeComponent>, RefRW<LocalTransform>>().WithEntityAccess().WithNone<StartFuseCountdown>()) {
                grenade.ValueRW.ElapsedTime += SystemAPI.Time.DeltaTime;

                float t = math.saturate(grenade.ValueRW.ElapsedTime / grenade.ValueRW.ThrowTime);
                float3 start = grenade.ValueRW.StartPosition;
                float3 end = grenade.ValueRW.TargetPosition;

                float3 horizontalPosition = math.lerp(start, end + grenade.ValueRO.RandomizedTarget, t);
                float height = grenade.ValueRW.PeakHeight * (1 - 4 * (t - 0.5f) * (t - 0.5f));
                float3 newPosition = horizontalPosition + new float3(0, height, 0);
                transform.ValueRW.Position = newPosition;

                if (t >= 1.0f) {
                    transform.ValueRW.Position.z = 0f;
                    ecb.AddComponent<StartFuseCountdown>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    // Detection of collision from a spawned explosion particle
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct ExplosionTriggerSystem : ISystem {
        private ComponentHandles m_ComponentHandle;

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }

        public struct ComponentHandles {
            [ReadOnly] public ComponentLookup<PhysicsCollider> colliderLookup;
            [ReadOnly] public ComponentLookup<ProjectileComponent> projectileComponentLookup;
            [ReadOnly] public ComponentLookup<EnemyData> enemyDataLookup;
            [ReadOnly] public ComponentLookup<DisabledProjectileTag> disabledProjectileTagLookup;
            [ReadOnly] public ComponentLookup<DisabledEnemyTag> disabledEnemyTagLookup;

            public ComponentHandles(ref SystemState state) {
                colliderLookup = state.GetComponentLookup<PhysicsCollider>(true);
                projectileComponentLookup = state.GetComponentLookup<ProjectileComponent>(true);
                enemyDataLookup = state.GetComponentLookup<EnemyData>(true);
                disabledProjectileTagLookup = state.GetComponentLookup<DisabledProjectileTag>(true);
                disabledEnemyTagLookup = state.GetComponentLookup<DisabledEnemyTag>(true);
            }

            public void Update(ref SystemState state) {
                colliderLookup.Update(ref state);
                projectileComponentLookup.Update(ref state);
                enemyDataLookup.Update(ref state);
                disabledProjectileTagLookup.Update(ref state);
                disabledEnemyTagLookup.Update(ref state);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PlayerSingleton>();
            m_ComponentHandle = new ComponentHandles(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            m_ComponentHandle.Update(ref state);

            EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

            state.Dependency = new TriggerEventsJob {
                ecb = ecb,
                componentHandles = m_ComponentHandle
            }.ScheduleParallel(state.Dependency);
        }

        static CollisionBelongsToLayer GetCollisionFilter(PhysicsCollider collider) {
            var collisionFilter = collider.Value.Value.GetCollisionFilter();
            CollisionBelongsToLayer result = CollisionBelongsToLayer.None;

            for (int i = 0; i < 32; i++) {
                var layer = (CollisionBelongsToLayer)(1 << i);
                if ((collisionFilter.BelongsTo & (uint)layer) != 0) {
                    result = layer;
                    break;
                }
            }

            return result;
        }

        /*
         * TODO Add Cooldown component to decrease Cooldown timers maybe and schedule parallel.
         */
        [BurstCompile]
        public partial struct TriggerEventsJob : IJobEntity {
            public EntityCommandBuffer.ParallelWriter ecb;
            [ReadOnly] public ComponentHandles componentHandles;

            void Execute([ChunkIndexInQuery] int chunkIndex, Entity explosionTriggerEntity, ref DynamicBuffer<StatefulTriggerEvent> buffer) {
                for (int i = 0; i < buffer.Length; i++) {
                    var triggerEvent = buffer[i];
                    if (triggerEvent.State != StatefulEventState.Enter) {
                        continue;
                    }

                    var targetEntity = triggerEvent.GetOtherEntity(explosionTriggerEntity);

                    if (explosionTriggerEntity == Entity.Null || targetEntity == Entity.Null 
                       || !componentHandles.colliderLookup.HasComponent(explosionTriggerEntity) || !componentHandles.colliderLookup.HasComponent(targetEntity)
                       || componentHandles.disabledEnemyTagLookup.HasComponent(targetEntity)) {
                        continue;
                    }

                    var filter = GetCollisionFilter(componentHandles.colliderLookup[explosionTriggerEntity]);
                    var hitFilter = GetCollisionFilter(componentHandles.colliderLookup[targetEntity]);

                    if (filter != CollisionBelongsToLayer.Explosion || hitFilter != CollisionBelongsToLayer.Enemy) {
                        continue;
                    }

                    if (componentHandles.enemyDataLookup.HasComponent(targetEntity) && componentHandles.projectileComponentLookup.HasComponent(explosionTriggerEntity)) {
                        EnemyData enemyData = componentHandles.enemyDataLookup[targetEntity];
                        ProjectileComponent projectileComponent = componentHandles.projectileComponentLookup[explosionTriggerEntity];

                        enemyData.health -= projectileComponent.BaseDamage;
                        if (enemyData.health <= 0f) {
                            ecb.AddComponent<DisabledEnemyTag>(chunkIndex, targetEntity);
                        }
                        else {
                            ecb.SetComponent(chunkIndex, targetEntity, enemyData);
                        }
                    }
                }
            }
        }
    }
}