using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace ECS {
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CheckProjectileCollisionsSystem : ISystem {
        private ComponentLookup<PhysicsCollider> colliderLookup;
        private ComponentLookup<ProjectileComponent> projectileComponentLookup;
        private ComponentLookup<EnemyData> enemyDataLookup;
        private ComponentLookup<PlayerData> playerDataLookup;
        private ComponentLookup<ProjectileDataComponent> projectileDataLookup;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PlayerSingleton>();
            colliderLookup = state.GetComponentLookup<PhysicsCollider>(isReadOnly: true);
            projectileComponentLookup = state.GetComponentLookup<ProjectileComponent>(isReadOnly: false);
            enemyDataLookup = state.GetComponentLookup<EnemyData>(isReadOnly: false);
            playerDataLookup = state.GetComponentLookup<PlayerData>(isReadOnly: false);
            projectileDataLookup = state.GetComponentLookup<ProjectileDataComponent>(isReadOnly: false);
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            colliderLookup.Update(ref state);
            projectileComponentLookup.Update(ref state);
            enemyDataLookup.Update(ref state);
            playerDataLookup.Update(ref state);
            projectileDataLookup.Update(ref state);

            state.Dependency = new CheckTriggerEvents {
                colliderLookup = colliderLookup,
                projectileComponentLookup = projectileComponentLookup,
                enemyDataLookup = enemyDataLookup,
                playerDataLookup = playerDataLookup,
                projectileDataLookup = projectileDataLookup,
                entityManager = state.EntityManager,
                playerEntity = SystemAPI.GetSingleton<PlayerSingleton>().PlayerEntity,
                ecb = ecb.AsParallelWriter(),
                deltaTime = SystemAPI.Time.DeltaTime
                //itemPrefab = itemPrefab,
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        static CollisionBelongsToLayer CheckCollisionFilter(PhysicsCollider collider) {
            var collisionFilter = collider.Value.Value.GetCollisionFilter();

            foreach (CollisionBelongsToLayer layer in Enum.GetValues(typeof(CollisionBelongsToLayer))) {
                if ((collisionFilter.BelongsTo & (uint)layer) != 0) {
                    return layer;
                }
            }

            return CollisionBelongsToLayer.None;
        }


        struct CheckTriggerEvents : ITriggerEventsJob {
            [ReadOnly] public ComponentLookup<PhysicsCollider> colliderLookup;
            public ComponentLookup<ProjectileComponent> projectileComponentLookup;
            public ComponentLookup<EnemyData> enemyDataLookup;
            public ComponentLookup<PlayerData> playerDataLookup;
            public ComponentLookup<ProjectileDataComponent> projectileDataLookup;
            public Entity playerEntity;
            public EntityManager entityManager;
            public EntityCommandBuffer.ParallelWriter ecb;
            public float deltaTime;

            public void Execute(TriggerEvent collisionEvent) {
                var (projectile, otherEntity) = GetEntityWithComponent<ProjectileTag>(collisionEvent.EntityA, collisionEvent.EntityB);
                if (projectile == Entity.Null || !entityManager.HasComponent<PhysicsCollider>(otherEntity)) {
                    return;
                }
                
                if (CheckCollisionFilter(colliderLookup[otherEntity]) == CollisionBelongsToLayer.Wall) {
                    ecb.DestroyEntity(0, projectile);
                    return;
                }
                    
                var projectileData = projectileDataLookup[projectile];

                switch (projectileData.projectileType) {
                    case ProjectileType.BULLET: {
                        HandleBulletProjectile(projectile, otherEntity);
                        return;
                    }
                }
            }

            public void HandleBulletProjectile(Entity projectileEntity, Entity collidedEntity) {
                if (enemyDataLookup.HasComponent(collidedEntity) && projectileComponentLookup.HasComponent(projectileEntity)) {
                    var enemyData = enemyDataLookup[collidedEntity];
                    var projectileComponent = projectileComponentLookup[projectileEntity];
                    enemyData.health -= projectileComponent.BaseDamage;
                    enemyDataLookup[collidedEntity] = enemyData;

                    var projectileDataCheck = projectileDataLookup[projectileEntity];
                    projectileDataCheck.piercingEnemyNumber -= 1;
                    projectileDataLookup[projectileEntity] = projectileDataCheck;

                    if (enemyData.health <= 0f) {
                        ecb.DestroyEntity(0, collidedEntity);
                        var playerData = playerDataLookup[playerEntity];
                        playerData.experience += 1;
                        playerDataLookup[playerEntity] = playerData;
                    }

                    if (projectileDataCheck.piercingEnemyNumber <= 0) {
                        ecb.DestroyEntity(0, projectileEntity);
                    }
                }
                else {
                    ecb.DestroyEntity(0, collidedEntity);
                    ecb.DestroyEntity(0, projectileEntity);
                }
            }

            (Entity, Entity) GetEntityWithComponent<T>(Entity entityA, Entity entityB) where T : struct, IComponentData {
                if (entityManager.HasComponent<T>(entityA)) {
                    return (entityA, entityB);
                }

                if (entityManager.HasComponent<T>(entityB)) {
                    return (entityB, entityA);
                }

                return (Entity.Null, Entity.Null);
            }

            void HandleProjectileCollision(Entity projectileEntity, Entity collidedEntity) {
                if (enemyDataLookup.HasComponent(collidedEntity) && projectileComponentLookup.HasComponent(projectileEntity)) {
                    var enemyData = enemyDataLookup[collidedEntity];
                    var projectileData = projectileComponentLookup[projectileEntity];
                    enemyData.health -= projectileData.BaseDamage;
                    enemyDataLookup[collidedEntity] = enemyData;

                    if (enemyData.health <= 0f) {
                        ecb.DestroyEntity(0, collidedEntity);
                        var playerData = playerDataLookup[playerEntity];
                        playerData.experience += 1;
                        playerDataLookup[playerEntity] = playerData;
                    }

                    ecb.DestroyEntity(0, projectileEntity);
                    return;
                }

                ecb.DestroyEntity(0, collidedEntity);
                ecb.DestroyEntity(0, projectileEntity);
            }
        }
    }
}