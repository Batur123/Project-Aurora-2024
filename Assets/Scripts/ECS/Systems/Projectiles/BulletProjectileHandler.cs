using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems.Projectiles {

    [BurstCompile]
    public partial struct ClearDisabledProjectiles : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            state.Dependency = new RemoveDisabledProjectiles {
                ECB = ecbParallel,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public partial struct RemoveDisabledProjectiles : IJobEntity {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(Entity entity, [EntityIndexInQuery] int index, in DisabledProjectileTag tag, in Disabled isDisabled) {
                ECB.DestroyEntity(index, entity);
            }
        }
    }
    
    [BurstCompile]
    public partial struct DisableProjectilesSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            state.Dependency = new DisableProjectilesJob {
                ECB = ecbParallel
            }.ScheduleParallel(state.Dependency);

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public partial struct DisableProjectilesJob : IJobEntity {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(Entity entity, [EntityIndexInQuery] int index, in DisabledProjectileTag tag) {
                ECB.AddComponent<Disabled>(index, entity);
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct RegularBulletTriggerSystem : ISystem {
        private ComponentHandles m_ComponentHandle;
            
        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
        
        public struct ComponentHandles
        {
            [ReadOnly] public ComponentLookup<PhysicsCollider> colliderLookup;
            [ReadOnly] public ComponentLookup<ProjectileComponent> projectileComponentLookup;
            [ReadOnly] public ComponentLookup<EnemyData> enemyDataLookup;
            [ReadOnly] public ComponentLookup<ProjectileDataComponent> projectileDataLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> projectileTransformLookup;
            [ReadOnly] public ComponentLookup<DisabledProjectileTag> disabledProjectileTagLookup;
            [ReadOnly] public ComponentLookup<DisabledEnemyTag> disabledEnemyTagLookup;

            public ComponentHandles(ref SystemState state)
            {
                colliderLookup = state.GetComponentLookup<PhysicsCollider>(true);
                projectileComponentLookup = state.GetComponentLookup<ProjectileComponent>(true);
                enemyDataLookup = state.GetComponentLookup<EnemyData>(true);
                projectileDataLookup = state.GetComponentLookup<ProjectileDataComponent>(true);
                projectileTransformLookup = state.GetComponentLookup<LocalTransform>(true);
                disabledProjectileTagLookup = state.GetComponentLookup<DisabledProjectileTag>(true);
                disabledEnemyTagLookup = state.GetComponentLookup<DisabledEnemyTag>(true);
            }

            public void Update(ref SystemState state)
            {
                colliderLookup.Update(ref state);
                projectileComponentLookup.Update(ref state);
                enemyDataLookup.Update(ref state);
                projectileDataLookup.Update(ref state);
                projectileTransformLookup.Update(ref state);
                disabledProjectileTagLookup.Update(ref state);
                disabledEnemyTagLookup.Update(ref state);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
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
                componentHandles = m_ComponentHandle,
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

        [BurstCompile]
        public partial struct TriggerEventsJob : IJobEntity {
            public EntityCommandBuffer.ParallelWriter ecb;
            [ReadOnly] public ComponentHandles componentHandles;
            
            void Execute([ChunkIndexInQuery] int chunkIndex, Entity projectileEntity, ref DynamicBuffer<StatefulTriggerEvent> buffer) {
                for (int i = 0; i < buffer.Length; i++) {
                    var triggerEvent = buffer[i];
                    if (triggerEvent.State != StatefulEventState.Enter) {
                        continue;
                    }
                    
                    var targetEntity = triggerEvent.GetOtherEntity(projectileEntity);

                    if (projectileEntity == Entity.Null 
                        || projectileEntity == null || 
                        targetEntity == Entity.Null
                        || targetEntity == null ) {
                        continue;
                    }
                    
                    if (componentHandles.disabledProjectileTagLookup.HasComponent(projectileEntity) || componentHandles.disabledEnemyTagLookup.HasComponent(targetEntity)) {
                        continue;
                    }

                    if (!componentHandles.colliderLookup.HasComponent(targetEntity) || !componentHandles.colliderLookup.HasComponent(projectileEntity) ||
                        !componentHandles.projectileComponentLookup.HasComponent(projectileEntity)) {
                        continue;
                    }

                    var targetCollider = componentHandles.colliderLookup[targetEntity];
                    var targetColliderFilter = GetCollisionFilter(targetCollider);
                    var projectileCollider = componentHandles.colliderLookup[projectileEntity];
                    var projectileColliderFilter = GetCollisionFilter(projectileCollider);

                    if (projectileColliderFilter != CollisionBelongsToLayer.Projectile) {
                        continue;
                    }

                    //Debug.Log($"Frame {frameCount} Entity {projectileEntity} => State:[{triggerEvent.State}] " +
                    //          $"vs {triggerEvent.GetOtherEntity(projectileEntity)},  ProjectFilter: {projectileColliderFilter}" +
                    //          $"HitFilter: {targetColliderFilter}");

                    if (targetColliderFilter == CollisionBelongsToLayer.Wall) {
                        ecb.AddComponent<DisabledProjectileTag>(chunkIndex, projectileEntity); 
                        continue;
                    }

                    if (
                        componentHandles.enemyDataLookup.HasComponent(targetEntity)
                        && componentHandles.projectileComponentLookup.HasComponent(projectileEntity)
                        && componentHandles.projectileTransformLookup.HasComponent(projectileEntity)
                    ) {
                        EnemyData enemyData = componentHandles.enemyDataLookup[targetEntity];
                        ProjectileComponent projectileComponent = componentHandles.projectileComponentLookup[projectileEntity];
                        ProjectileDataComponent projectileDataComponent = componentHandles.projectileDataLookup[projectileEntity];

                        LocalTransform localTransform = componentHandles.projectileTransformLookup[projectileEntity];
                        ecb.AddComponent(chunkIndex, projectileEntity, new ParticleSpawnerRequestTag {
                            particleLifeTime = 0.5f,
                            particleType = ParticleType.Bullet_Hit,
                            spawnPosition = localTransform.Position
                        });

                        enemyData.health -= projectileComponent.BaseDamage;
                        if (enemyData.health <= 0f) {
                            ecb.AddComponent<DisabledEnemyTag>(chunkIndex, targetEntity);
                        }
                        else {
                            ecb.SetComponent(chunkIndex, targetEntity, enemyData);
                        }
                        
                        projectileDataComponent.piercingEnemyNumber -= 1;
                        if (projectileDataComponent.piercingEnemyNumber <= 0) {
                            ecb.AddComponent<DisabledProjectileTag>(chunkIndex, projectileEntity);
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