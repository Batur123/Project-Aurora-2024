using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine.InputSystem;

namespace ECS {
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CheckWeaponOnGroundTriggerSystem : ISystem {
        private ComponentLookup<PhysicsCollider> colliderLookup;
        private ComponentLookup<PlayerData> playerDataLookup;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PlayerSingleton>();
            colliderLookup = state.GetComponentLookup<PhysicsCollider>(isReadOnly: true);
            playerDataLookup = state.GetComponentLookup<PlayerData>(isReadOnly: false);
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            colliderLookup.Update(ref state);
            playerDataLookup.Update(ref state);

            state.Dependency = new CheckTriggerEvents {
                colliderLookup = colliderLookup,
                playerDataLookup = playerDataLookup,
                entityManager = state.EntityManager,
                playerEntity = SystemAPI.GetSingleton<PlayerSingleton>().PlayerEntity,
                ecb = ecb.AsParallelWriter(),
                deltaTime = SystemAPI.Time.DeltaTime
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
            public ComponentLookup<PlayerData> playerDataLookup;
            public Entity playerEntity;
            public EntityManager entityManager;
            public EntityCommandBuffer.ParallelWriter ecb;
            public float deltaTime;

            public void Execute(TriggerEvent triggerEvent) {
                var (itemEntity, otherEntity) = GetEntityWithComponent<DroppedItemTag>(triggerEvent.EntityA, triggerEvent.EntityB);

                if (itemEntity != Entity.Null 
                    && entityManager.HasComponent<PhysicsCollider>(itemEntity) 
                    && entityManager.HasComponent<PlayerData>(otherEntity)
                    && entityManager.HasComponent<GunTag>(itemEntity)                    
                    ) {
                    var collider = colliderLookup[otherEntity];
                    var selectedFilter = CheckCollisionFilter(collider);
                    if (selectedFilter != CollisionBelongsToLayer.Player) {
                        return;
                    }

                    var equippedGunBuffer = entityManager.GetBuffer<EquippedGun>(playerEntity);
                    if (!equippedGunBuffer.IsEmpty) {
                        return;
                    }
                    
                    var input = Keyboard.current;
                    if (input.fKey.isPressed) {
                        ecb.RemoveComponent<DroppedItemTag>(0, itemEntity);
                        var buffer = entityManager.GetBuffer<EquippedGun>(otherEntity);
                        buffer.Add(new EquippedGun { GunEntity = itemEntity });
                        var itemData = entityManager.GetComponentData<Item>(itemEntity);
                        itemData.isEquipped = true;
                        itemData.slot = 0;
                        itemData.onGround = false;
                        var inventory = entityManager.GetBuffer<Inventory>(otherEntity);
                        inventory.Add(new Inventory { itemEntity = itemEntity });
                        entityManager.SetComponentData(itemEntity, itemData);
                        entityManager.SetComponentData(otherEntity, new UIUpdateFlag { needsUpdate = true });
                    }
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
        }
    }
}