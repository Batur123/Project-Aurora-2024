using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using UnityEngine;

namespace ECS {
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct RegularBulletTriggerSystem : ISystem {
        private ComponentLookup<PhysicsCollider> colliderLookup;
        private ComponentLookup<ProjectileComponent> projectileComponentLookup;
        private ComponentLookup<EnemyData> enemyDataLookup;
        private ComponentLookup<PlayerData> playerDataLookup;
        private ComponentLookup<ProjectileDataComponent> projectileDataLookup;
        public Entity playerEntity;

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
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
        }
        
        static CollisionBelongsToLayer GetCollisionFilter(PhysicsCollider collider) {
            var collisionFilter = collider.Value.Value.GetCollisionFilter();

            CollisionBelongsToLayer[] layers = {
                CollisionBelongsToLayer.None,
                CollisionBelongsToLayer.Player,
                CollisionBelongsToLayer.Enemy,
                CollisionBelongsToLayer.Projectile,
                CollisionBelongsToLayer.Wall,
                CollisionBelongsToLayer.GunEntity,
            };

            foreach (var layer in layers) {
                if ((collisionFilter.BelongsTo & (uint)layer) != 0) {
                    return layer;
                }
            }

            return CollisionBelongsToLayer.None;
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

            void Execute([ChunkIndexInQuery] int chunkIndex, Entity projectileEntity, ref DynamicBuffer<StatefulTriggerEvent> buffer) {

                for (int i = 0; i < buffer.Length; i++) {
                    var triggerEvent = buffer[i];
                    var targetEntity = triggerEvent.GetOtherEntity(projectileEntity);

                    if (projectileEntity == Entity.Null || targetEntity == Entity.Null) {
                        continue;
                    }

                    if (!colliderLookup.HasComponent(targetEntity) || !colliderLookup.HasComponent(projectileEntity) || !projectileComponentLookup.HasComponent(projectileEntity)) {
                        continue;
                    }
                    
                    var targetCollider = colliderLookup[targetEntity];
                    var targetColliderFilter = GetCollisionFilter(targetCollider);
                    var projectileCollider = colliderLookup[projectileEntity];
                    var projectileColliderFilter = GetCollisionFilter(projectileCollider);

                    if (projectileColliderFilter != CollisionBelongsToLayer.Projectile) {
                        continue;
                    }
                    
                    if (targetColliderFilter == CollisionBelongsToLayer.Wall) {
                        ecb.DestroyEntity(chunkIndex, projectileEntity);
                        continue;
                    }
                    
                    switch (triggerEvent.State) {
                        case StatefulEventState.Enter: {
                            
                            #if UNITY_EDITOR
                            Debug.Log($"Frame {frameCount} Entity {projectileEntity} => State:[{triggerEvent.State}] " +
                                      $"vs {triggerEvent.GetOtherEntity(projectileEntity)}, " +
                                      $"BodyIndices=({triggerEvent.BodyIndexA}, {triggerEvent.BodyIndexB}), " +
                                      $"ColliderKeys=({triggerEvent.ColliderKeyA}, {triggerEvent.ColliderKeyB}) EntityFilter: {projectileColliderFilter}," +
                                      $"HitFilter: {targetColliderFilter}");
                            #endif
                            
                            if (enemyDataLookup.HasComponent(targetEntity) && projectileComponentLookup.HasComponent(projectileEntity)) {
                                EnemyData enemyData = enemyDataLookup[targetEntity];
                                ProjectileComponent projectileComponent = projectileComponentLookup[projectileEntity];
                                ProjectileDataComponent projectileDataComponent = projectileDataLookup[projectileEntity];

                                // Decrease piercing
                                projectileDataComponent.piercingEnemyNumber -= 1;
                                ecb.SetComponent(chunkIndex, projectileEntity, projectileDataComponent);

                                // Decrease enemy health
                                enemyData.health -= projectileComponent.BaseDamage;
                                ecb.SetComponent(chunkIndex, targetEntity, enemyData);

                                if (enemyData.health <= 0f) {
                                    ecb.DestroyEntity(chunkIndex, targetEntity);
                                    
                                    // Increase Player XP
                                    PlayerData playerData = playerDataLookup[playerEntity];
                                    playerData.experience += 1;
                                    ecb.SetComponent(chunkIndex, playerEntity, playerData);
                                }

                                if (projectileDataComponent.piercingEnemyNumber <= 0) {
                                    ecb.DestroyEntity(chunkIndex, projectileEntity);
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