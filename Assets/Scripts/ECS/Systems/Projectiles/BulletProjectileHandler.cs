using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using UnityEngine;

namespace ECS.Systems.Projectiles {
    
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct RegularBulletTriggerSystem : ISystem {
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
            }.ScheduleParallel(state.Dependency);
           // state.Dependency.Complete();
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
            [ReadOnly] public ComponentLookup<ProjectileComponent> projectileComponentLookup;
            [ReadOnly] public ComponentLookup<PlayerData> playerDataLookup;
            [ReadOnly] public ComponentLookup<ProjectileDataComponent> projectileDataLookup;
            [ReadOnly] public ComponentLookup<EnemyData> enemyDataLookup;
            
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
                    
                    //Debug.Log($"Frame {frameCount} Entity {projectileEntity} => State:[{triggerEvent.State}] " +
                    //          $"vs {triggerEvent.GetOtherEntity(projectileEntity)},  ProjectFilter: {projectileColliderFilter}" +
                    //          $"HitFilter: {targetColliderFilter}");
                    
                    if (targetColliderFilter == CollisionBelongsToLayer.Wall) {
                        ecb.DestroyEntity(chunkIndex, projectileEntity);
                        continue;
                    }

                    if (triggerEvent.State != StatefulEventState.Enter) {
                        continue;
                    }
                    
                    if (enemyDataLookup.HasComponent(targetEntity) && projectileComponentLookup.HasComponent(projectileEntity)) {
                        EnemyData enemyData = enemyDataLookup[targetEntity];
                        ProjectileComponent projectileComponent = projectileComponentLookup[projectileEntity];
                        ProjectileDataComponent projectileDataComponent = projectileDataLookup[projectileEntity];
                                
                       
                        enemyData.health -= projectileComponent.BaseDamage;
                        if (enemyData.health <= 0f) {
                            ecb.AddComponent<DisabledEnemyTag>(chunkIndex, targetEntity);
                        }
                        else {
                            ecb.SetComponent(chunkIndex, targetEntity, enemyData);
                        }
                                
                        projectileDataComponent.piercingEnemyNumber -= 1;
                        if (projectileDataComponent.piercingEnemyNumber <= 0) {
                            ecb.DestroyEntity(chunkIndex, projectileEntity);
                            continue;
                        }
                        else {
                            ecb.SetComponent(chunkIndex, projectileEntity, projectileDataComponent);
                        }
                    }
                }
            }
        }
    }
}