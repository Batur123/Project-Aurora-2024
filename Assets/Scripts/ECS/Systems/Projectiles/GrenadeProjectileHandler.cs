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
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<ExplosionTag>>().WithEntityAccess()) {
                explosionTag.ValueRW.lifeTime -= SystemAPI.Time.DeltaTime;
                explosionTag.ValueRW.elapsedExplosionTime += SystemAPI.Time.DeltaTime;

                if (explosionTag.ValueRW.elapsedExplosionTime >= 1) {
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
        }
    }

    // Grenade Projectile Path when Throwing (it goes in parabolic)
    [BurstCompile]
    public partial struct GrenadeSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (grenade, transform, entity) in
                     SystemAPI.Query<RefRW<GrenadeComponent>, RefRW<LocalTransform>>().WithEntityAccess().WithNone<StartFuseCountdown>()) {
                grenade.ValueRW.ElapsedTime += deltaTime;

                float t = math.saturate(grenade.ValueRW.ElapsedTime / grenade.ValueRW.ThrowTime); 
                float3 start = grenade.ValueRW.StartPosition;
                float3 end   = grenade.ValueRW.TargetPosition;

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
        }
    }

    // Detection of collision from a spawned explosion particle
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct ExplosionTriggerSystem : ISystem {
        private ComponentLookup<PhysicsCollider> colliderLookup;
        private ComponentLookup<ProjectileComponent> projectileComponentLookup;
        private ComponentLookup<EnemyData> enemyDataLookup;
        private ComponentLookup<PlayerData> playerDataLookup;
        private ComponentLookup<ProjectileDataComponent> projectileDataLookup;

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PlayerSingleton>();

            colliderLookup = state.GetComponentLookup<PhysicsCollider>(isReadOnly: true);
            projectileComponentLookup = state.GetComponentLookup<ProjectileComponent>(isReadOnly: false);
            enemyDataLookup = state.GetComponentLookup<EnemyData>(isReadOnly: false);
            playerDataLookup = state.GetComponentLookup<PlayerData>(isReadOnly: false);
            projectileDataLookup = state.GetComponentLookup<ProjectileDataComponent>(isReadOnly: false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            colliderLookup.Update(ref state);
            projectileComponentLookup.Update(ref state);
            enemyDataLookup.Update(ref state);
            playerDataLookup.Update(ref state);
            projectileDataLookup.Update(ref state);

            EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

            state.Dependency = new TriggerEventsJob {
                ecb = ecb,
                colliderLookup = colliderLookup,
                projectileComponentLookup = projectileComponentLookup,
                enemyDataLookup = enemyDataLookup,
                playerDataLookup = playerDataLookup,
                projectileDataLookup = projectileDataLookup,
                frameCount = Time.frameCount,
                playerEntity = SystemAPI.GetSingleton<PlayerSingleton>().PlayerEntity,
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
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

        [BurstCompile]
        public partial struct TriggerEventsJob : IJobEntity {
            public EntityCommandBuffer.ParallelWriter ecb;

            [ReadOnly] public ComponentLookup<PhysicsCollider> colliderLookup;

            public ComponentLookup<ProjectileComponent> projectileComponentLookup;
            public ComponentLookup<PlayerData> playerDataLookup;
            public ComponentLookup<ProjectileDataComponent> projectileDataLookup;
            public ComponentLookup<EnemyData> enemyDataLookup;

            public Entity playerEntity;
            public int frameCount;

            void Execute([ChunkIndexInQuery] int chunkIndex, Entity explosionTriggerEntity, ref DynamicBuffer<StatefulTriggerEvent> buffer) {
                for (int i = 0; i < buffer.Length; i++) {
                    var triggerEvent = buffer[i];
                    var targetEntity = triggerEvent.GetOtherEntity(explosionTriggerEntity);

                    if (explosionTriggerEntity == Entity.Null || targetEntity == Entity.Null) {
                        continue;
                    }

                    if (!colliderLookup.HasComponent(explosionTriggerEntity) || !colliderLookup.HasComponent(targetEntity)) {
                        //Debug.Log("Stateful Trigger collider is removed. This should never happen.");
                        continue;
                    }

                    var filter = GetCollisionFilter(colliderLookup[explosionTriggerEntity]);
                    var hitFilter = GetCollisionFilter(colliderLookup[targetEntity]);

                    if (filter != CollisionBelongsToLayer.Explosion && hitFilter != CollisionBelongsToLayer.Enemy) {
                        continue;
                    }

#if UNITY_EDITOR
                    Debug.Log($"Frame {frameCount} Entity {explosionTriggerEntity} => State:[{triggerEvent.State}] " +
                              $"vs {triggerEvent.GetOtherEntity(explosionTriggerEntity)}, Filter: {filter} - TargetFilter: {hitFilter}");
#endif


                    switch (triggerEvent.State) {
                        case StatefulEventState.Enter: {
                            if (enemyDataLookup.HasComponent(targetEntity) && projectileComponentLookup.HasComponent(explosionTriggerEntity)) {
                                EnemyData enemyData = enemyDataLookup[targetEntity];
                                ProjectileComponent projectileComponent = projectileComponentLookup[explosionTriggerEntity];

                                enemyData.health -= projectileComponent.BaseDamage;
                                if (enemyData.health <= 0f) {
                                    PlayerData playerData = playerDataLookup[playerEntity];
                                    playerData.experience += 1;
                                    ecb.SetComponent(chunkIndex, playerEntity, playerData);
                                    ecb.DestroyEntity(chunkIndex, targetEntity);
                                }
                                else {
                                    ecb.SetComponent(chunkIndex, targetEntity, enemyData);
                                }
                            }

                            break;
                        }
                    }
                }
            }
        }
    }
}