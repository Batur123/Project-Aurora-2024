using System;
using ScriptableObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.InputSystem;

namespace ECS {
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CheckWeaponOnGroundTriggerSystem : ISystem {
        private ComponentLookup<PhysicsCollider> colliderLookup;
        private ComponentLookup<PlayerData> playerDataLookup;
        private ComponentLookup<Item> itemLookup;
        private ComponentLookup<AttachmentTypeComponent> attachmentTypeLookup;
        private BufferLookup<Child> childLookup;
        private ComponentLookup<AttachmentTag> attachmentTagLookup;
        
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PlayerSingleton>();
            colliderLookup = state.GetComponentLookup<PhysicsCollider>(isReadOnly: true);
            playerDataLookup = state.GetComponentLookup<PlayerData>(isReadOnly: false);
            itemLookup = state.GetComponentLookup<Item>(isReadOnly: false);
            attachmentTypeLookup = state.GetComponentLookup<AttachmentTypeComponent>(isReadOnly: true);
            childLookup = state.GetBufferLookup<Child>(isReadOnly: true);
            attachmentTagLookup = state.GetComponentLookup<AttachmentTag>(isReadOnly: true);
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
           
            colliderLookup.Update(ref state);
            playerDataLookup.Update(ref state);
            itemLookup.Update(ref state);
            attachmentTypeLookup.Update(ref state);
            childLookup.Update(ref state);
            attachmentTagLookup.Update(ref state);

            state.Dependency = new CheckTriggerEvents {
                colliderLookup = colliderLookup,
                playerDataLookup = playerDataLookup,
                itemLookup = itemLookup,
                attachmentTagLookup = attachmentTagLookup,
                attachmentTypeLookup = attachmentTypeLookup,
                childLookup = childLookup,
                entityManager = state.EntityManager,
                playerEntity = SystemAPI.GetSingleton<PlayerSingleton>().PlayerEntity,
                ecb = ecb.AsParallelWriter(),
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

        public static int SelectSlotIndexOfAttachment(AttachmentType attachmentType) {
            switch (attachmentType) {
                case AttachmentType.Barrel: return 1;
                case AttachmentType.Ammunition: return 4;
                case AttachmentType.Scope: return 3;
                case AttachmentType.Magazine: return 2;
                default: return -1;
            }
        }
        
        struct CheckTriggerEvents : ITriggerEventsJob {
            [ReadOnly] public ComponentLookup<PhysicsCollider> colliderLookup;
            [ReadOnly] public ComponentLookup<PlayerData> playerDataLookup;
            [ReadOnly] public ComponentLookup<Item> itemLookup;
            [ReadOnly] public ComponentLookup<AttachmentTypeComponent> attachmentTypeLookup;
            [ReadOnly] public BufferLookup<Child> childLookup;
            [ReadOnly] public ComponentLookup<AttachmentTag> attachmentTagLookup;
            
            public Entity playerEntity;
            public EntityManager entityManager;
            public EntityCommandBuffer.ParallelWriter ecb;

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

                        ecb.AppendToBuffer(0, playerEntity, new EquippedGun { GunEntity = itemEntity });

                        ecb.SetComponent(0, itemEntity, new Item {
                            isEquipped = true,
                            slot = 0,
                            onGround = false,
                            quantity = 1,
                            isStackable = false
                        });

                        ecb.AppendToBuffer(0, otherEntity, new Inventory { itemEntity = itemEntity });

                        if (childLookup.HasBuffer(itemEntity)) {
                            var attachments = childLookup[itemEntity];
                            for (int i = 0; i < attachments.Length; i++) {
                                var attachmentEntity = attachments[i].Value;

                                if (!itemLookup.HasComponent(attachmentEntity) || !attachmentTagLookup.HasComponent(attachmentEntity)) {
                                    continue;
                                }

                                var attachmentType = attachmentTypeLookup[attachmentEntity].attachmentType;
                                var slotIndex = SelectSlotIndexOfAttachment(attachmentType);

                                ecb.SetComponent(0, attachmentEntity, new Item {
                                    isEquipped = true,
                                    slot = slotIndex,
                                    onGround = false,
                                    quantity = 1,
                                    isStackable = false
                                });
                            }
                        }

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