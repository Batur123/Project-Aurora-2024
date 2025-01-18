using ScriptableObjects;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace ECS {
    public struct AttachmentDropProcessTag : IComponentData {}
    
    [BurstCompile]
    public partial struct DropEquippedWeaponAttachment : ISystem {

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            if (equippedGunBuffer.IsEmpty 
                || Input.GetMouseButton(0) 
                || !Input.GetKey(KeyCode.B) 
                || state.EntityManager.HasComponent<AttachmentDropProcessTag>(playerSingleton.PlayerEntity)) {
                return;
            }

            var equippedGunEntity = equippedGunBuffer[0].GunEntity;
            DynamicBuffer<Child> children = state.EntityManager.GetBuffer<Child>(equippedGunEntity);
            NativeList<Entity> attachmentsToRemove = new NativeList<Entity>(Allocator.Temp);
            foreach (Child child in children) {
                if (state.EntityManager.HasComponent<AttachmentTag>(child.Value)) {
                    attachmentsToRemove.Add(child.Value);
                }
            }
            foreach (Entity attachment in attachmentsToRemove) {
                GunAttachmentHelper.RequestRemoveAttachment(equippedGunEntity, attachment);
            }

            state.EntityManager.AddComponent<AttachmentDropProcessTag>(playerSingleton.PlayerEntity);
            attachmentsToRemove.Dispose();
        }
    }
    
    [BurstCompile]
    public partial struct DropEquippedWeapon : ISystem {
        private EntityQuery playerQuery;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
            playerQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerSingleton>());
        }

        public void OnUpdate(ref SystemState state) {
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            if (equippedGunBuffer.IsEmpty || Input.GetMouseButton(0) || !Input.GetKey(KeyCode.X)) {
                return;
            }

            var equippedGunEntity = equippedGunBuffer[0].GunEntity;
            state.EntityManager.AddComponent<DroppedItemTag>(equippedGunEntity);

            var bufferLookup = SystemAPI.GetBufferLookup<EquippedGun>(false);
            var ammoLookup = SystemAPI.GetComponentLookup<AmmoComponent>(false);
            var itemLookup = SystemAPI.GetComponentLookup<Item>(false);
            var inventoryLookup = SystemAPI.GetBufferLookup<Inventory>(false);
            var attachmentTypeLookup = SystemAPI.GetComponentLookup<AttachmentTypeComponent>(false);
            var attachmentTagLookup = SystemAPI.GetComponentLookup<AttachmentTag>(false);
            var childLookup = SystemAPI.GetBufferLookup<Child>(false);

            bufferLookup.Update(ref state);
            ammoLookup.Update(ref state);
            itemLookup.Update(ref state);
            inventoryLookup.Update(ref state);
            childLookup.Update(ref state);
            attachmentTypeLookup.Update(ref state);
            attachmentTagLookup.Update(ref state);

            state.Dependency = new ClearEquippedGunBufferJob {
                PlayerEntity = playerSingleton.PlayerEntity,
                BufferFromEntity = bufferLookup,
                InventoryBuffers = inventoryLookup,
                AmmoComponents = ammoLookup,
                childLookup = childLookup,
                ItemComponents = itemLookup,
                AttachmentTypeLookup = attachmentTypeLookup,
                AttachmentTagLookup = attachmentTagLookup,
                ecb = GetEntityCommandBuffer(ref state)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
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
        
        [BurstCompile]
        private struct ClearEquippedGunBufferJob : IJob {
            public Entity PlayerEntity;
            public EntityCommandBuffer.ParallelWriter ecb;
            
            [NativeDisableParallelForRestriction] 
            public BufferLookup<EquippedGun> BufferFromEntity;
            
            [ReadOnly] public ComponentLookup<AttachmentTypeComponent> AttachmentTypeLookup;
            [ReadOnly] public BufferLookup<Child> childLookup;
            [ReadOnly] public ComponentLookup<AttachmentTag> AttachmentTagLookup;
            
            [NativeDisableParallelForRestriction] 
            public BufferLookup<Inventory> InventoryBuffers;
            
            [NativeDisableParallelForRestriction] 
            public ComponentLookup<AmmoComponent> AmmoComponents;
            
            [NativeDisableParallelForRestriction]
            public ComponentLookup<Item> ItemComponents;
   
            
            public void Execute() {
                ecb.SetComponent(0, PlayerEntity, new UIUpdateFlag { needsUpdate = true });

                ecb.RemoveComponent<ReloadingTag>(0, PlayerEntity);
                ecb.SetComponent(0, PlayerEntity, new ReloadTimer { timeRemaining = 2f });

                var buffer = BufferFromEntity[PlayerEntity];

                var gunBuffer = buffer[0].GunEntity;
                
                AmmoComponent ammoComponent = AmmoComponents[gunBuffer];
                Item itemComponent = ItemComponents[gunBuffer];
                
                ecb.SetComponent(0, gunBuffer, new Item {
                    onGround = true,
                    slot = -1,
                    isEquipped = false,
                    isStackable = itemComponent.isStackable,
                    quantity = itemComponent.quantity,
                });
                
                // Now, remove the equipped gun from the Inventory buffer
                if (InventoryBuffers.HasBuffer(PlayerEntity)) {
                    var inventoryBuffer = InventoryBuffers[PlayerEntity];
                    int indexToRemove = -1;

                    // Find the index of the equipped gun in the Inventory buffer
                    for (int i = 0; i < inventoryBuffer.Length; i++) {
                        if (inventoryBuffer[i].itemEntity == gunBuffer) {
                            indexToRemove = i;
                            break;
                        }
                    }

                    if (indexToRemove != -1) {
                        inventoryBuffer.RemoveAt(indexToRemove);
                    } else {
                        Debug.LogWarning("Equipped gun not found in Inventory buffer.");
                    }
                }
                
                ecb.SetComponent(0, gunBuffer, new AmmoComponent {
                    currentAmmo = ammoComponent.currentAmmo,
                    capacity = ammoComponent.capacity,
                    isReloading = false,
                });
                
                // disable attachments
                if (childLookup.HasBuffer(gunBuffer)) {
                    var attachments = childLookup[gunBuffer];
                    for (int i = 0; i < attachments.Length; i++) {
                        var attachmentEntity = attachments[i].Value;

                        if (!ItemComponents.HasComponent(attachmentEntity) || !AttachmentTagLookup.HasComponent(attachmentEntity)) {
                            continue;
                        }

                        ecb.SetComponent(0, attachmentEntity, new Item {
                            isEquipped = false,
                            slot = -1,
                            onGround = false,
                            quantity = 1,
                            isStackable = false
                        });
                    }
                }

                buffer.Clear();
            }
        }
    }
}