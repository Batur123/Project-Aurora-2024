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

                    //Debug.Log($"Frame {frameCount} Entity {explosionTriggerEntity} => State:[{triggerEvent.State}] " +
                    //          $"vs {triggerEvent.GetOtherEntity(explosionTriggerEntity)}, Filter: {filter} - TargetFilter: {hitFilter}");
                    
                    switch (triggerEvent.State) {
                        case StatefulEventState.Enter: {
                            if (enemyDataLookup.HasComponent(targetEntity) && projectileComponentLookup.HasComponent(explosionTriggerEntity)) {
                                EnemyData enemyData = enemyDataLookup[targetEntity];
                                ProjectileComponent projectileComponent = projectileComponentLookup[explosionTriggerEntity];
                                ProjectileDataComponent projectileDataComponent = projectileDataLookup[explosionTriggerEntity];
                                
                                ecb.SetComponent(chunkIndex, explosionTriggerEntity, projectileDataComponent);

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