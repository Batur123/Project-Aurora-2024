using ECS.Bakers;
using ScriptableObjects;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS {
    public static class GunAttachmentHelper {
        public static void RequestRemoveAttachment(Entity gunEntity, Entity attachmentEntity) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, new RemoveAttachmentRequest {
                gunEntity = gunEntity,
                attachmentEntity = attachmentEntity
            });
            ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
            ecb.Dispose();
        }
    }

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
                ecb.SetComponent(playerSingleton.PlayerEntity, new UIUpdateFlag { needsUpdate = true });
                
                ecb.DestroyEntity(requestEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
    
    public struct RemoveAttachmentRequest : IComponentData {
        public Entity gunEntity;
        public Entity attachmentEntity;
    }
    
    public struct GunLibrarySingleton : IComponentData {
        public NativeHashMap<int, Entity> WeaponVariants;
    }
    
    
    
    [BurstCompile]
    [UpdateBefore(typeof(GunSpawnSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AttachmentLibrarySystem : ISystem {
        private NativeHashMap<int, Entity> _attachmentVariants;

        public void OnCreate(ref SystemState state) {
            _attachmentVariants = new NativeHashMap<int, Entity>(10, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state) {
            _attachmentVariants.Dispose();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (builtPrefab, attachmentType, entity) in
                     SystemAPI.Query<RefRO<BuiltPrefab>, RefRO<AttachmentTypeComponent>>()
                         .WithEntityAccess()) {

                // Combine attachmentType and variantId into a unique key
                int key = ComputeKey(attachmentType.ValueRO.attachmentType, attachmentType.ValueRO.variantId);

                if (!_attachmentVariants.ContainsKey(key)) {
                    _attachmentVariants[key] = entity;
                }
            }
            //Debug.Log("[System]: AttachmentLibrarySystem updated.");
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public NativeHashMap<int, Entity> GetAllDescriptors() {
            return _attachmentVariants;
        }
        
        public int ComputeKey(AttachmentType attachmentType, int variantId) {
            return (0x10000000) | (int)attachmentType * 1000 + variantId;
        }
        
        public Entity GetDescriptor(AttachmentType attachmentType, int variantId) {
            int key = ComputeKey(attachmentType, variantId);
            return _attachmentVariants.TryGetValue(key, out var descriptor) ? descriptor : Entity.Null;
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GunLibrarySystem : ISystem {
        private NativeHashMap<int, Entity> _weaponVariants;

        public void OnCreate(ref SystemState state) {
            _weaponVariants = new NativeHashMap<int, Entity>(10, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state) {
            _weaponVariants.Dispose();
        }

        public int ComputeKey(GunType weaponType, int variantId) {
            return (0x20000000) | (int)weaponType * 1000 + variantId;
        }
        
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (builtPrefab, weaponType, entity) in
                     SystemAPI.Query<RefRO<BuiltPrefab>, RefRO<GunTypeComponent>>()
                         .WithEntityAccess()) {

                int key = ComputeKey(weaponType.ValueRO.gunType, weaponType.ValueRO.variantId);

                if (!_weaponVariants.ContainsKey(key)) {
                    _weaponVariants[key] = entity;
                }
            }
            //Debug.Log("[System]: GunLibrarySystem updated.");
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        public NativeHashMap<int, Entity> GetAllDescriptors() {
            return _weaponVariants;
        }
        
        public Entity GetDescriptor(GunType weaponType, int variantId) {
            return _weaponVariants.TryGetValue(ComputeKey(weaponType, variantId), out var descriptor) ? descriptor : Entity.Null;
        }
    }

    public struct SpawnGunRequest : IComponentData {
        public GunType gunType;
        public int variantId;
        public float3 position;
        public float scale;
    }
    
    public struct SpawnAttachmentRequest : IComponentData {
        public AttachmentType attachmentType;
        public int variantId;
        public float3 position;
    }

    public static class LootHelper {
        public static void RequestRemoveAttachment(Entity gunEntity, Entity attachmentEntity) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, new RemoveAttachmentRequest {
                gunEntity = gunEntity,
                attachmentEntity = attachmentEntity
            });
            ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
            ecb.Dispose();
        }

        public static Entity GetRandomLoot(ref SystemState state) {
            // Guns
            SystemHandle gunLibrarySystemHandle = state.World.GetExistingSystem<GunLibrarySystem>();
            GunLibrarySystem gunLibrarySystemRef = state.World.Unmanaged.GetUnsafeSystemRef<GunLibrarySystem>(gunLibrarySystemHandle);

            // Attachments
            SystemHandle attachmentLibrarySystemHandle = state.World.GetExistingSystem<AttachmentLibrarySystem>();
            AttachmentLibrarySystem attachmentLibrarySystemRef = state.World.Unmanaged.GetUnsafeSystemRef<AttachmentLibrarySystem>(attachmentLibrarySystemHandle);

            NativeHashMap<int, Entity> allAttachments = attachmentLibrarySystemRef.GetAllDescriptors();
            NativeHashMap<int, Entity> allWeapons = gunLibrarySystemRef.GetAllDescriptors();
            
            NativeHashMap<int, float> lootWeights = new NativeHashMap<int, float>(allWeapons.Count + allAttachments.Count, Allocator.Temp);
            
            if (allWeapons.Count == 0 && allAttachments.Count == 0) {
                //Debug.LogWarning("No loot available to select.");
                lootWeights.Dispose();
                return Entity.Null;
            }
            
            float totalWeight = 0f;

            foreach (var weapon in allWeapons) {
                if (!state.EntityManager.HasComponent<GunTypeComponent>(weapon.Value)) {
                    //Debug.LogWarning($"[Loot]: Entity {weapon.Value} is missing GunTypeComponent.");
                    continue;
                }
                
                GunTypeComponent gunTypeComponent = state.EntityManager.GetComponentData<GunTypeComponent>(weapon.Value);
                float weight = gunTypeComponent.lootWeight;
                lootWeights[weapon.Key] = weight;
                totalWeight += weight;
                //Debug.Log($"[Loot]: Weapon Key: {weapon.Key}, Weight: {weight}, Total Weight: {totalWeight}");
            }

            foreach (var attachment in allAttachments) {
                if (!state.EntityManager.HasComponent<AttachmentTypeComponent>(attachment.Value)) {
                    //Debug.LogWarning($"[Loot]: Entity {attachment.Value} is missing AttachmentTypeComponent.");
                    continue;
                }
                
                AttachmentTypeComponent attachmentTypeComponent = state.EntityManager.GetComponentData<AttachmentTypeComponent>(attachment.Value);
                float weight = attachmentTypeComponent.lootWeight;
                lootWeights[attachment.Key] = weight;
                totalWeight += weight;
                //Debug.Log($"[Loot]: Attachment Key: {attachment.Key}, Weight: {weight}, Total Weight: {totalWeight}");
            }

            if (totalWeight <= 0f) {
                //Debug.LogWarning("[Loot]: Total weight is zero, no loot can be selected.");
                lootWeights.Dispose();
                return Entity.Null;
            }
            
            //Debug.Log("[Loot]: Total weight: " + totalWeight);
            
            float randomPoint = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;
            Entity selectedEntity = Entity.Null;

            //Debug.Log("[Loot]: Selected random point " + randomPoint);
            foreach (var loot in lootWeights) {
                //Debug.Log("Each iteration value = "+loot.Value);
                cumulativeWeight += loot.Value;
                if (randomPoint <= cumulativeWeight) {
                    //Debug.Log("[Loot]: Loot selected");
                    selectedEntity = allWeapons.ContainsKey(loot.Key) ? allWeapons[loot.Key] : allAttachments[loot.Key];
                    //Debug.Log("[Loot]: Entity = " + selectedEntity);
                    break;
                }
            }
            
            //Debug.Log("[Loot]: Cumulative Weight: "+cumulativeWeight);

            if (state.EntityManager.HasComponent<AttachmentTypeComponent>(selectedEntity)) {
                AttachmentTypeComponent attachmentTypeComponent = state.EntityManager.GetComponentData<AttachmentTypeComponent>(selectedEntity);
                Debug.Log("Spawn loot: " + attachmentTypeComponent.attachmentType + " " + attachmentTypeComponent.variantId + " " + attachmentTypeComponent.lootWeight);
                lootWeights.Dispose();
                return selectedEntity;
            }
            
            if (state.EntityManager.HasComponent<GunTypeComponent>(selectedEntity)) {
                GunTypeComponent gunTypeComponent = state.EntityManager.GetComponentData<GunTypeComponent>(selectedEntity);
                Debug.Log("Spawn loot: " + gunTypeComponent.gunType + " " + gunTypeComponent.variantId + " " + gunTypeComponent.lootWeight);
                lootWeights.Dispose();
                return selectedEntity;
            }
            
            Debug.LogWarning($"[Loot]: Random Loot is not spawned. Total Count {allWeapons.Count + allAttachments.Count}");
            lootWeights.Dispose();
            return Entity.Null;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(AttachmentLibrarySystem))]
    [UpdateAfter(typeof(GunLibrarySystem))]
    public partial struct GunSpawnSystem : ISystem {
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
                    capacity = gunBlob.ammoCapacity,
                    currentAmmo = gunBlob.ammoCapacity,
                    isReloading = false,
                });
                ecb.SetComponent(gunEntity, new BaseWeaponData {
                    damage = gunBlob.damage,
                    attackSpeed = gunBlob.attackSpeed,
                    recoilAmount = gunBlob.recoilAmount,
                    spreadAmount = gunBlob.spreadAmount,
                    bulletsPerShot = gunBlob.bulletsPerShot,
                    ammoCapacity = gunBlob.ammoCapacity,
                    reloadSpeed = gunBlob.attackSpeed,
                    piercingBulletsPerShot = gunBlob.piercingBulletsPerShot,
                });
                ecb.SetComponent(gunEntity, new WeaponData {
                    weaponName = request.ValueRO.gunType.ToString(),
                    damage = gunBlob.damage,
                    attackSpeed = gunBlob.attackSpeed,
                    recoilAmount = gunBlob.recoilAmount,
                    spreadAmount = gunBlob.spreadAmount,
                    bulletsPerShot = gunBlob.bulletsPerShot,
                    ammoCapacity = gunBlob.ammoCapacity,
                    reloadSpeed = gunBlob.attackSpeed,
                    piercingBulletsPerShot = gunBlob.piercingBulletsPerShot,
                });
                ecb.SetComponent(gunEntity, scopePointTransform);
                ecb.SetComponent(gunEntity, muzzlePointTransform);
                ecb.SetComponent(gunEntity, new Item {
                    slot = -1,
                    onGround = true,
                    isEquipped = false,
                    isStackable = false,
                    quantity = 1
                });

                ecb.AddBuffer<GunAttachment>(gunEntity);
                
                var position = request.ValueRO.position;
                var rotation = quaternion.identity;
                var scale = request.ValueRO.scale;

                LocalTransform localTransform = LocalTransform.FromPositionRotationScale(position, rotation, scale);

                ecb.SetComponent(gunEntity, localTransform);
                
                // Spawn Base Scope that has +0 values
                Entity baseScopeEntity = attachmentLibrarySystemRef.GetDescriptor(AttachmentType.Scope, request.ValueRO.variantId);
                if (baseScopeEntity != Entity.Null) {
                    //var scopePrefab = state.EntityManager.GetComponentData<BuiltPrefab>(baseScopeEntity);
                    //Entity attachmentEntity = ecb.Instantiate(scopePrefab.prefab);
                    
                    // Add Scope
                    AddAttachment(
                        ecb, ref state, gunEntity, AttachmentType.Scope, 0, attachmentLibrarySystemRef,
                        scopePointTransform.position, scopePointTransform.rotation,
                        new AttachmentComponent { spreadAmount = 0.5f, attachmentName = AttachmentType.Scope.ToString() },
                        request.ValueRO
                    );
                    
                   //ecb.SetName(attachmentEntity, AttachmentType.Scope.ToString());
                   //ecb.AddComponent(attachmentEntity, new AttachmentComponent {
                   //    accuracyModifier = 5,
                   //});
                   //
                   //ecb.AddComponent(attachmentEntity, LocalTransform.FromPositionRotationScale(
                   //    scopePointTransform.position,
                   //    scopePointTransform.rotation,
                   //    1.0f
                   //));

                   //ecb.AddComponent(attachmentEntity, new AttachmentTag());
                   //ecb.AddComponent(attachmentEntity, new AttachmentTypeComponent {
                   //    attachmentType = AttachmentType.Scope
                   //});
                   //ecb.AddComponent(attachmentEntity, new Parent { Value = gunEntity });
                   //ecb.AddComponent(attachmentEntity, new LocalToWorld  { Value = float4x4.identity });
                   //ecb.AddComponent<Item>(attachmentEntity);
                   //ecb.SetComponent(attachmentEntity, new Item {
                   //    slot = -1,
                   //    onGround = true,
                   //    isEquipped = false,
                   //    isStackable = false,
                   //    quantity = 1
                   //});
                   ////spriteRenderer.sprite = attachmentTemplate.attachmentSprite;
                   //
                   //if (!state.EntityManager.HasComponent<Child>(gunEntity))
                   //{
                   //    ecb.AddBuffer<Child>(gunEntity);
                   //}
                }
                
                Entity baseBarrelEntity = attachmentLibrarySystemRef.GetDescriptor(AttachmentType.Barrel, request.ValueRO.variantId);
                if (baseBarrelEntity != Entity.Null) {
                    var barrelPrefab = state.EntityManager.GetComponentData<BuiltPrefab>(baseBarrelEntity);
                    Entity attachmentEntity = ecb.Instantiate(barrelPrefab.prefab);
                    ecb.SetName(attachmentEntity, AttachmentType.Barrel.ToString());
                    ecb.AddComponent(attachmentEntity, new AttachmentComponent {
                        spreadAmount = 0.5f,
                        attachmentName = AttachmentType.Barrel.ToString()
                    });
                    
                    ecb.AddComponent(attachmentEntity, LocalTransform.FromPositionRotationScale(
                        muzzlePointTransform.position,
                        muzzlePointTransform.rotation,
                        1
                    ));

                    ecb.AddComponent(attachmentEntity, new AttachmentTag());
                    ecb.AddComponent(attachmentEntity, new AttachmentTypeComponent {
                        attachmentType = AttachmentType.Barrel
                    });
                    ecb.AddComponent(attachmentEntity, new Parent { Value = gunEntity });
                    ecb.AddComponent(attachmentEntity, new LocalToWorld  { Value = float4x4.identity });
                    ecb.AddComponent<Item>(attachmentEntity);
                    ecb.SetComponent(attachmentEntity, new Item {
                        slot = -1,
                        onGround = true,
                        isEquipped = false,
                        isStackable = false,
                        quantity = 1
                    });
                    
                    if (!state.EntityManager.HasComponent<Child>(gunEntity))
                    {
                        ecb.AddBuffer<Child>(gunEntity);
                    }
                }
                
                ecb.DestroyEntity(requestEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        public void AddAttachment(
            EntityCommandBuffer ecb, ref SystemState state, Entity gunEntity, 
            AttachmentType attachmentType, int variantId, AttachmentLibrarySystem attachmentLibrarySystemRef, 
            float3 position, quaternion rotation, AttachmentComponent attachmentComponent, SpawnGunRequest request) 
        {
            Entity descriptorEntity = attachmentLibrarySystemRef.GetDescriptor(attachmentType, variantId);
            if (descriptorEntity == Entity.Null) {
                return;
            }

            var prefab = state.EntityManager.GetComponentData<BuiltPrefab>(descriptorEntity);
            Entity attachmentEntity = ecb.Instantiate(prefab.prefab);
            ecb.SetName(attachmentEntity, attachmentType.ToString());
            ecb.AddComponent(attachmentEntity, attachmentComponent);
                    
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
                onGround = true,
                isEquipped = false,
                isStackable = false,
                quantity = 1
            });
                    
            if (!state.EntityManager.HasComponent<Child>(gunEntity))
            {
                ecb.AddBuffer<Child>(gunEntity);
            }
        }
    }
}