using System;
using ECS.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ECS {
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CheckWeaponOnGroundTriggerSystem : ISystem {
        private ComponentHandles m_ComponentHandle;

        public struct ComponentHandles {
            [ReadOnly] public ComponentLookup<PhysicsCollider> colliderLookup;
            [ReadOnly] public ComponentLookup<AttachmentTag> attachmentTagLookup;
            [ReadOnly] public ComponentLookup<Item> itemLookup;
            [ReadOnly] public ComponentLookup<PassiveItem> passiveItemLookup;
            [ReadOnly] public BufferLookup<Child> childLookup;
            [ReadOnly] public BufferLookup<Inventory> inventoryLookup;
            [ReadOnly] public BufferLookup<PassiveInventory> passiveInventoryLookup;

            public ComponentHandles(ref SystemState state) {
                colliderLookup = state.GetComponentLookup<PhysicsCollider>(true);
                attachmentTagLookup = state.GetComponentLookup<AttachmentTag>(true);
                itemLookup = state.GetComponentLookup<Item>(true);
                passiveItemLookup = state.GetComponentLookup<PassiveItem>(true);
                childLookup = state.GetBufferLookup<Child>(true);
                inventoryLookup = state.GetBufferLookup<Inventory>(true);
                passiveInventoryLookup = state.GetBufferLookup<PassiveInventory>(true);
            }

            public void Update(ref SystemState state) {
                colliderLookup.Update(ref state);
                attachmentTagLookup.Update(ref state);
                itemLookup.Update(ref state);
                passiveItemLookup.Update(ref state);
                childLookup.Update(ref state);
                inventoryLookup.Update(ref state);
                passiveInventoryLookup.Update(ref state);
            }
        }


        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PlayerSingleton>();
            m_ComponentHandle = new ComponentHandles(ref state);
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var hasPickedUpItem = new NativeReference<bool>(Allocator.TempJob); // Initialize the flag
            m_ComponentHandle.Update(ref state);

            state.Dependency = new CheckTriggerEvents {
                componentHandles = m_ComponentHandle,
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

        struct CheckTriggerEvents : ITriggerEventsJob {
            [ReadOnly] public ComponentHandles componentHandles;

            public Entity playerEntity;
            public EntityManager entityManager;
            public EntityCommandBuffer.ParallelWriter ecb;

            [NativeDisableParallelForRestriction] public NativeReference<bool> hasPickedUpItem; // Native flag to track if an item has been picked up

            public void Execute(TriggerEvent triggerEvent) {
                if (hasPickedUpItem.Value) {
                    return;
                }

                var (itemEntity, otherEntity) = GetEntityWithComponent<DroppedItemTag>(triggerEvent.EntityA, triggerEvent.EntityB);

                if (itemEntity == Entity.Null || otherEntity == Entity.Null) {
                    return;
                }

                var collider = componentHandles.colliderLookup[otherEntity];
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
                else if (componentHandles.passiveItemLookup.HasComponent(itemEntity)) {
                    PickupToPassiveInventory(itemEntity, otherEntity);
                }
                else if (componentHandles.itemLookup.HasComponent(itemEntity)) {
                    PickupToInventory(itemEntity, otherEntity);
                }

                ecb.RemoveComponent<PickupRequest>(0, otherEntity);
            }

            public Entity IsPassiveItemExists(Entity itemEntity, Entity otherEntity) {
                DynamicBuffer<PassiveInventory> inventoryBuffer = componentHandles.passiveInventoryLookup[otherEntity];

                var foundEntity = Entity.Null;

                var searchItem = componentHandles.passiveItemLookup[itemEntity];
                
                foreach (PassiveInventory item in inventoryBuffer) {
                    var passiveItem = componentHandles.passiveItemLookup[item.itemEntity];

                    if (passiveItem.passiveItemType == searchItem.passiveItemType) {
                        foundEntity = item.itemEntity;
                        break;
                    }
                }

                return foundEntity;
            }
            
            public void PickupToPassiveInventory(Entity itemEntity, Entity otherEntity) {
                ecb.RemoveComponent<DroppedItemTag>(0, itemEntity);
                Entity foundEntity = IsPassiveItemExists(itemEntity, otherEntity);

                if (foundEntity == Entity.Null) {
                    ecb.AppendToBuffer(0, playerEntity, new PassiveInventory { itemEntity = itemEntity });
                    ecb.AddComponent<UpdateUserInterfaceTag>(0, otherEntity);
                    ecb.AddComponent<DisableSpriteRendererRequest>(0, itemEntity);
                    return;
                }
                
                var passiveItem = componentHandles.passiveItemLookup[foundEntity];
                passiveItem.amount++;
                ecb.SetComponent(0, foundEntity, passiveItem);
                ecb.AddComponent<UpdateUserInterfaceTag>(0, otherEntity);
                ecb.AddComponent<DisableSpriteRendererRequest>(0, itemEntity);
            }
            
            public void PickupToInventory(Entity itemEntity, Entity otherEntity) {
                int firstEmptySlotIndex = InventoryHelper.FindFirstEmptyInventorySlot(componentHandles.inventoryLookup[playerEntity], componentHandles.itemLookup);

                if (firstEmptySlotIndex == -1) {
                    //Debug.Log("Inventory was full");
                    return;
                }

                ecb.RemoveComponent<DroppedItemTag>(0, itemEntity);
                ecb.AppendToBuffer(0, playerEntity, new Inventory { itemEntity = itemEntity });

                Item item = componentHandles.itemLookup[itemEntity];
                item.slot = firstEmptySlotIndex;
                item.onGround = false;
                item.isEquipped = false;
                ecb.SetComponent(0, itemEntity, item);
                ecb.AddComponent<UpdateUserInterfaceTag>(0, otherEntity);
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

                if (componentHandles.childLookup.HasBuffer(itemEntity)) {
                    var attachments = componentHandles.childLookup[itemEntity];
                    int itemSlot = 16;
                    for (int i = 0; i < attachments.Length; i++) {
                        var attachmentEntity = attachments[i].Value;

                        if (!componentHandles.itemLookup.HasComponent(attachmentEntity) || !componentHandles.attachmentTagLookup.HasComponent(attachmentEntity)) {
                            continue;
                        }

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

                ecb.AddComponent<UpdateUserInterfaceTag>(0, otherEntity);
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