using ECS.Components;
using ECS.Libraries;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS {
    [BurstCompile]
    [UpdateBefore(typeof(GunSpawnSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup))] // Choose the appropriate system group
    public partial struct AttachmentRemovalSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }
        
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, requestEntity) in SystemAPI.Query<RefRO<RemoveAttachmentRequest>>().WithEntityAccess()) {
                Entity attachmentEntity = request.ValueRO.attachmentEntity;

                if (state.EntityManager.HasComponent<Parent>(attachmentEntity)) {
                    ecb.RemoveComponent<Parent>(attachmentEntity);
                }

                Item itemData = state.EntityManager.GetComponentData<Item>(attachmentEntity);
                itemData.slot = -1;
                itemData.isEquipped = false;
                itemData.onGround = true;
                ecb.SetComponent(attachmentEntity, itemData);
                var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
                ecb.AddComponent<UpdateUserInterfaceTag>(playerSingleton.PlayerEntity);
                
                ecb.DestroyEntity(requestEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(PassiveItemsLibrarySystem))]
    public partial struct PassiveItemSpawnSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SpawnPassiveItemRequest>();
        }
        
        public void OnUpdate(ref SystemState state) {
            SystemHandle librarySystemHandle = state.World.GetExistingSystem<PassiveItemsLibrarySystem>();
            PassiveItemsLibrarySystem librarySystemRef = state.World.Unmanaged.GetUnsafeSystemRef<PassiveItemsLibrarySystem>(librarySystemHandle);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, requestEntity) in SystemAPI.Query<RefRO<SpawnPassiveItemRequest>>().WithEntityAccess()) {
                Entity descriptorEntity = librarySystemRef.GetDescriptor(request.ValueRO.passiveItemType);
                if (descriptorEntity == Entity.Null) {
                    Debug.LogWarning($"No descriptor found for {request.ValueRO.passiveItemType}");
                    ecb.DestroyEntity(requestEntity);
                    continue;
                }

                var prefab = state.EntityManager.GetComponentData<BuiltPrefab>(descriptorEntity);
                var blobReference = state.EntityManager.GetComponentData<PassiveItemBlobReference>(descriptorEntity);
                PassiveItemTemplateBlob blob = blobReference.templateBlob.Value;
                Entity passiveItemEntity = state.EntityManager.Instantiate(prefab.prefab);
                
                ecb.SetName(passiveItemEntity, request.ValueRO.passiveItemType.ToString());

                ecb.AddComponent<DroppedItemTag>(passiveItemEntity);
                ecb.AddComponent<Item>(passiveItemEntity);
                ecb.AddComponent<PassiveItemTag>(passiveItemEntity);
                ecb.SetComponent(passiveItemEntity, new Item {
                    slot = -1,
                    itemType = ItemType.PASSIVE_ITEM,
                    onGround = true,
                    isEquipped = false,
                    isStackable = false,
                    quantity = 1
                });
                ecb.AddComponent<PassiveItem>(passiveItemEntity);
                ecb.SetComponent(passiveItemEntity, new PassiveItem {
                    itemName = request.ValueRO.passiveItemType.ToString(),
                    passiveItemType = request.ValueRO.passiveItemType,
                    amount = 1,
                });

                var position = request.ValueRO.position;
                var rotation = quaternion.identity;
                var scale = request.ValueRO.scale;

                LocalTransform localTransform = LocalTransform.FromPositionRotationScale(position, rotation, scale);

                ecb.SetComponent(passiveItemEntity, localTransform);
                ecb.DestroyEntity(requestEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(AttachmentLibrarySystem))]
    [UpdateAfter(typeof(GunLibrarySystem))]
    public partial struct GunSpawnSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SpawnGunRequest>();
        }
        
        public void OnUpdate(ref SystemState state) {
            SystemHandle gunLibrarySystemHandle = state.World.GetExistingSystem<GunLibrarySystem>();
            GunLibrarySystem gunLibrarySystemRef = state.World.Unmanaged.GetUnsafeSystemRef<GunLibrarySystem>(gunLibrarySystemHandle);
            SystemHandle attachmentLibrarySystemHandle = state.World.GetExistingSystem<AttachmentLibrarySystem>();
            AttachmentLibrarySystem attachmentLibrarySystemRef = state.World.Unmanaged.GetUnsafeSystemRef<AttachmentLibrarySystem>(attachmentLibrarySystemHandle);
            //LootHelper.GetRandomLoot(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, requestEntity) in SystemAPI.Query<RefRO<SpawnGunRequest>>().WithEntityAccess()) {
                Entity descriptorEntity = gunLibrarySystemRef.GetDescriptor(request.ValueRO.gunType, request.ValueRO.variantId);
                if (descriptorEntity == Entity.Null) {
                    Debug.LogWarning($"No descriptor found for {request.ValueRO.gunType}");
                    ecb.DestroyEntity(requestEntity);
                    continue;
                }
                

                // 3) Retrieve the actual ECS prefab from the descriptor
                var builtPrefab = state.EntityManager.GetComponentData<BuiltPrefab>(descriptorEntity);
                var gunTypeComponent = state.EntityManager.GetComponentData<GunTypeComponent>(descriptorEntity);
                var muzzlePointTransform = state.EntityManager.GetComponentData<MuzzlePointTransform>(descriptorEntity);
                var scopePointTransform = state.EntityManager.GetComponentData<ScopePointTransform>(descriptorEntity);
                var projectileTypeComponent = state.EntityManager.GetComponentData<WeaponProjectileTypeComponent>(descriptorEntity);
                var gunBlobRef = state.EntityManager.GetComponentData<GunBlobReference>(descriptorEntity);
                GunTemplateBlob gunBlob = gunBlobRef.templateBlob.Value;

                Entity gunEntity = state.EntityManager.Instantiate(builtPrefab.prefab);

                BaseWeaponData randomizedWeaponData = LootHelper.GetRandomStats(gunBlob);
                Debug.Log(randomizedWeaponData);
                
                ecb.SetName(gunEntity, request.ValueRO.gunType.ToString());

                ecb.AddBuffer<Child>(gunEntity);
                ecb.AddComponent<DroppedItemTag>(gunEntity);
                ecb.AddComponent<GunTag>(gunEntity);
                ecb.AddComponent<GunTypeComponent>(gunEntity);
                ecb.AddComponent<AmmoComponent>(gunEntity);
                ecb.AddComponent<WeaponData>(gunEntity);
                ecb.AddComponent<BaseWeaponData>(gunEntity);
                ecb.AddComponent<MuzzlePointTransform>(gunEntity);
                ecb.AddComponent<ScopePointTransform>(gunEntity);
                ecb.AddComponent<Item>(gunEntity);
                ecb.AddComponent<WeaponProjectileTypeComponent>(gunEntity);
                ecb.SetComponent(gunEntity, new WeaponProjectileTypeComponent {
                    projectileType = projectileTypeComponent.projectileType
                });
                ecb.SetComponent(gunEntity, new GunTypeComponent {
                    gunType = gunTypeComponent.gunType,
                    variantId = 0
                });
                ecb.SetComponent(gunEntity, new AmmoComponent {
                    capacity = randomizedWeaponData.stats.ammoCapacity,
                    currentAmmo = randomizedWeaponData.stats.ammoCapacity,
                    isReloading = false,
                });
                ecb.SetComponent(gunEntity, randomizedWeaponData);
                ecb.SetComponent(gunEntity, new WeaponData {
                    weaponName = request.ValueRO.gunType.ToString(),
                    stats = {
                        damage = randomizedWeaponData.stats.damage,
                        attackSpeed = randomizedWeaponData.stats.attackSpeed,
                        recoilAmount = randomizedWeaponData.stats.recoilAmount,
                        spreadAmount = randomizedWeaponData.stats.spreadAmount,
                        bulletsPerShot = randomizedWeaponData.stats.bulletsPerShot,
                        ammoCapacity = randomizedWeaponData.stats.ammoCapacity,
                        reloadSpeed = randomizedWeaponData.stats.attackSpeed,
                        piercingBulletsPerShot = randomizedWeaponData.stats.piercingBulletsPerShot,
                    }
                });
                ecb.SetComponent(gunEntity, scopePointTransform);
                ecb.SetComponent(gunEntity, muzzlePointTransform);
                ecb.SetComponent(gunEntity, new Item {
                    slot = -1,
                    itemType = ItemType.WEAPON,
                    onGround = true,
                    isEquipped = false,
                    isStackable = false,
                    quantity = 1
                });

                var position = request.ValueRO.position;
                var rotation = quaternion.identity;
                var scale = request.ValueRO.scale;

                LocalTransform localTransform = LocalTransform.FromPositionRotationScale(position, rotation, scale);

                ecb.SetComponent(gunEntity, localTransform);
                
                Entity baseScopeEntity = attachmentLibrarySystemRef.GetDescriptor(AttachmentType.Scope, request.ValueRO.variantId);
                if (baseScopeEntity != Entity.Null) {
                    var attachmentBlobRef = state.EntityManager.GetComponentData<AttachmentBlobReference>(baseScopeEntity);
                    AttachmentTemplateBlob attachmentBlog = attachmentBlobRef.templateBlob.Value;
                    BaseAttachmentData randomizedAttachmentData = LootHelper.GetRandomStats(attachmentBlog);

                    AddAttachment(
                        ecb, ref state, gunEntity, AttachmentType.Scope, 0, attachmentLibrarySystemRef,
                        scopePointTransform.position, scopePointTransform.rotation,
                        new AttachmentData {
                            attachmentName = AttachmentType.Scope.ToString(),
                            stats = randomizedAttachmentData.stats,
                        },
                        randomizedAttachmentData,
                        request.ValueRO
                    );
                }
                
                Entity baseBarrelEntity = attachmentLibrarySystemRef.GetDescriptor(AttachmentType.Barrel, request.ValueRO.variantId);
                if (baseBarrelEntity != Entity.Null) {
                    var attachmentBlobRef = state.EntityManager.GetComponentData<AttachmentBlobReference>(baseBarrelEntity);
                    AttachmentTemplateBlob attachmentBlog = attachmentBlobRef.templateBlob.Value;
                    BaseAttachmentData randomizedAttachmentData = LootHelper.GetRandomStats(attachmentBlog);
                    
                    AddAttachment(
                        ecb, ref state, gunEntity, AttachmentType.Barrel, 0, attachmentLibrarySystemRef,
                        muzzlePointTransform.position, muzzlePointTransform.rotation,
                        new AttachmentData {
                            attachmentName = AttachmentType.Barrel.ToString(),
                            stats = randomizedAttachmentData.stats,
                        },
                        randomizedAttachmentData,
                        request.ValueRO
                    );
                }
                
                ecb.DestroyEntity(requestEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        public void AddAttachment(
            EntityCommandBuffer ecb, 
            ref SystemState state, 
            Entity gunEntity, 
            AttachmentType attachmentType, 
            int variantId, 
            AttachmentLibrarySystem attachmentLibrarySystemRef, 
            float3 position, 
            quaternion rotation, 
            AttachmentData attachmentData, 
            BaseAttachmentData baseAttachmentData,
            SpawnGunRequest request) 
        {
            Entity descriptorEntity = attachmentLibrarySystemRef.GetDescriptor(attachmentType, variantId);
            if (descriptorEntity == Entity.Null) {
                return;
            }

            var prefab = state.EntityManager.GetComponentData<BuiltPrefab>(descriptorEntity);
            Entity attachmentEntity = ecb.Instantiate(prefab.prefab);
            ecb.SetName(attachmentEntity, attachmentType.ToString());
            ecb.AddComponent(attachmentEntity, baseAttachmentData);
            ecb.AddComponent(attachmentEntity, attachmentData);
                    
            ecb.AddComponent(attachmentEntity, LocalTransform.FromPositionRotationScale(
                position,
                rotation,
                1
            ));

            ecb.AddComponent(attachmentEntity, new AttachmentTag());
            ecb.AddComponent(attachmentEntity, new AttachmentTypeComponent {
                attachmentType = attachmentType
            });
            ecb.AddComponent(attachmentEntity, new Parent { Value = gunEntity });
            ecb.AddComponent(attachmentEntity, new LocalToWorld  { Value = float4x4.identity });
            ecb.AddComponent<Item>(attachmentEntity);
            ecb.SetComponent(attachmentEntity, new Item {
                slot = -1,
                itemType = ItemType.ATTACHMENT,
                onGround = true,
                isEquipped = false,
                isStackable = false,
                quantity = 1
            });
                    
            if (!state.EntityManager.HasComponent<Child>(gunEntity))
            {
                ecb.AddBuffer<Child>(gunEntity);
            }
            ecb.AddComponent<DisableSpriteRendererRequest>(attachmentEntity);
        }
    }
}