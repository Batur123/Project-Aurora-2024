using System;
using ScriptableObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
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
        private BufferLookup<Inventory> inventoryLookup;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PlayerSingleton>();
            colliderLookup = state.GetComponentLookup<PhysicsCollider>(isReadOnly: true);
            playerDataLookup = state.GetComponentLookup<PlayerData>(isReadOnly: false);
            itemLookup = state.GetComponentLookup<Item>(isReadOnly: false);
            attachmentTypeLookup = state.GetComponentLookup<AttachmentTypeComponent>(isReadOnly: true);
            childLookup = state.GetBufferLookup<Child>(isReadOnly: true);
            attachmentTagLookup = state.GetComponentLookup<AttachmentTag>(isReadOnly: true);
            inventoryLookup = state.GetBufferLookup<Inventory>(isReadOnly: true);
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var hasPickedUpItem = new NativeReference<bool>(Allocator.TempJob); // Initialize the flag

            colliderLookup.Update(ref state);
            playerDataLookup.Update(ref state);
            itemLookup.Update(ref state);
            attachmentTypeLookup.Update(ref state);
            childLookup.Update(ref state);
            attachmentTagLookup.Update(ref state);
            inventoryLookup.Update(ref state);

            state.Dependency = new CheckTriggerEvents {
                colliderLookup = colliderLookup,
                playerDataLookup = playerDataLookup,
                itemLookup = itemLookup,
                attachmentTagLookup = attachmentTagLookup,
                attachmentTypeLookup = attachmentTypeLookup,
                childLookup = childLookup,
                inventoryLookup = inventoryLookup,
                entityManager = state.EntityManager,
                playerEntity = SystemAPI.GetSingleton<PlayerSingleton>().PlayerEntity,
                ecb = ecb.AsParallelWriter(),
                hasPickedUpItem = hasPickedUpItem,
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            hasPickedUpItem.Dispose();
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
            [ReadOnly] public ComponentLookup<AttachmentTag> attachmentTagLookup;

            [ReadOnly] public BufferLookup<Child> childLookup;
            [ReadOnly] public BufferLookup<Inventory> inventoryLookup;

            public Entity playerEntity;
            public EntityManager entityManager;
            public EntityCommandBuffer.ParallelWriter ecb;

            [NativeDisableParallelForRestriction] 
            public NativeReference<bool> hasPickedUpItem; // Native flag to track if an item has been picked up

            public void Execute(TriggerEvent triggerEvent) {
                if (hasPickedUpItem.Value) {
                    return;
                }
                
                var (itemEntity, otherEntity) = GetEntityWithComponent<DroppedItemTag>(triggerEvent.EntityA, triggerEvent.EntityB);

                if (itemEntity == Entity.Null || otherEntity == Entity.Null) {
                    return;
                }

                var collider = colliderLookup[otherEntity];
                var selectedFilter = CheckCollisionFilter(collider);
                if (selectedFilter != CollisionBelongsToLayer.Player) {
                    return;
                }

                var input = Keyboard.current;
                if (!input.fKey.isPressed) {
                    return;
                }
                
                if (entityManager.HasComponent<PickupRequest>(otherEntity)) {
                    //Debug.Log("Was picking up item already, skipped!!!!");
                    return;
                }
                ecb.AddComponent<PickupRequest>(0, otherEntity);
                
                hasPickedUpItem.Value = true;
                
                var equippedGunBuffer = entityManager.GetBuffer<EquippedGun>(playerEntity);

                
                if (entityManager.HasComponent<GunTag>(itemEntity) && equippedGunBuffer.IsEmpty) {
                    PickupWeapon(itemEntity, otherEntity);
                }
                else if (itemLookup.HasComponent(itemEntity)) {
                    PickupToInventory(itemEntity, otherEntity);
                }
                ecb.RemoveComponent<PickupRequest>(0, otherEntity);
            }

            public void PickupToInventory(Entity itemEntity, Entity otherEntity) {
                int firstEmptySlotIndex = InventoryHelper.FindFirstEmptyInventorySlot(inventoryLookup[playerEntity], itemLookup);

                if (firstEmptySlotIndex == -1) {
                    //Debug.Log("Inventory was full");
                    return;
                }

                ecb.RemoveComponent<DroppedItemTag>(0, itemEntity);
                ecb.AppendToBuffer(0, playerEntity, new Inventory { itemEntity = itemEntity });

                Item item = itemLookup[itemEntity];
                item.slot = firstEmptySlotIndex;
                item.onGround = false;
                item.isEquipped = false;
                ecb.SetComponent(0, itemEntity, item);
                entityManager.SetComponentData(otherEntity, new UIUpdateFlag { needsUpdate = true });
                ecb.AddComponent<DisableSpriteRendererRequest>(0, itemEntity);
            }

            public void PickupWeapon(Entity itemEntity, Entity otherEntity) {
                var equippedGunBuffer = entityManager.GetBuffer<EquippedGun>(playerEntity);
                if (!equippedGunBuffer.IsEmpty) {
                    return;
                }

                ecb.RemoveComponent<DroppedItemTag>(0, itemEntity);
                ecb.AppendToBuffer(0, playerEntity, new EquippedGun { GunEntity = itemEntity });

                ecb.SetComponent(0, itemEntity, new Item {
                    isEquipped = true,
                    itemType = ItemType.WEAPON,
                    slot = 20, // 20 always main weapon slot
                    onGround = false,
                    quantity = 1,
                    isStackable = false
                });

                ecb.AppendToBuffer(0, otherEntity, new Inventory { itemEntity = itemEntity });

                if (childLookup.HasBuffer(itemEntity)) {
                    var attachments = childLookup[itemEntity];
                    int itemSlot = 16;
                    for (int i = 0; i < attachments.Length; i++) {
                        var attachmentEntity = attachments[i].Value;

                        if (!itemLookup.HasComponent(attachmentEntity) || !attachmentTagLookup.HasComponent(attachmentEntity)) {
                            continue;
                        }

                        var attachmentType = attachmentTypeLookup[attachmentEntity].attachmentType;

                        ecb.SetComponent(0, attachmentEntity, new Item {
                            isEquipped = true,
                            itemType = ItemType.ATTACHMENT,
                            slot = itemSlot,
                            onGround = false,
                            quantity = 1,
                            isStackable = false
                        });
                        itemSlot++;
                    }
                }

                entityManager.SetComponentData(otherEntity, new UIUpdateFlag { needsUpdate = true });
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