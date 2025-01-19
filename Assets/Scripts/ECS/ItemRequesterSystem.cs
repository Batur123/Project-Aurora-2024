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
    
    [BurstCompile]
    [UpdateBefore(typeof(GunSpawnSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AttachmentLibrarySystem : ISystem {
        private NativeHashMap<int, Entity> _attachmentTypeToDescriptor;

        public void OnCreate(ref SystemState state) {
            _attachmentTypeToDescriptor = new NativeHashMap<int, Entity>(10, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state) {
            _attachmentTypeToDescriptor.Dispose();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (builtPrefab, attachmentType, entity) in
                     SystemAPI.Query<RefRO<BuiltPrefab>, RefRO<AttachmentTypeComponent>>()
                         .WithEntityAccess()) {

                var attachmentTypeEnum = attachmentType.ValueRO.attachmentType;
                int attachmentTypeKey = (int)attachmentTypeEnum;
                if (!_attachmentTypeToDescriptor.ContainsKey(attachmentTypeKey)) {
                    _attachmentTypeToDescriptor[attachmentTypeKey] = entity;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public Entity GetDescriptor(AttachmentType attachmentType) {
            int key = (int)attachmentType;
            if (_attachmentTypeToDescriptor.TryGetValue(key, out var descriptor)) {
                return descriptor;
            }

            return Entity.Null;
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GunLibrarySystem : ISystem {
        private NativeHashMap<int, Entity> _gunTypeToDescriptor;

        public void OnCreate(ref SystemState state) {
            _gunTypeToDescriptor = new NativeHashMap<int, Entity>(10, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state) {
            _gunTypeToDescriptor.Dispose();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (builtPrefab, gunType, entity) in
                     SystemAPI.Query<RefRO<BuiltPrefab>, RefRO<GunTypeComponent>>()
                         .WithEntityAccess()) {
                var gunTypeEnum = gunType.ValueRO.type;
                int gunTypeKey = (int)gunTypeEnum;

                if (!_gunTypeToDescriptor.ContainsKey(gunTypeKey)) {
                    _gunTypeToDescriptor[gunTypeKey] = entity;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public Entity GetDescriptor(GunType gunType) {
            int key = (int)gunType;
            if (_gunTypeToDescriptor.TryGetValue(key, out var descriptor)) {
                return descriptor;
            }

            return Entity.Null;
        }
    }

    // public static class GunSpawnHelper {
    //     public static void SpawnGun(GunType gunType, float3 position) {
    //         var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    //         var requestEntity = em.CreateEntity();
    //
    //         // Add the request
    //         em.AddComponentData(requestEntity, new SpawnGunRequest {
    //             gunType = gunType,
    //             position = position
    //         });
    //     }
    // }

    public struct SpawnGunRequest : IComponentData {
        public GunType gunType;
        public float3 position;
    }
    
    public struct SpawnAttachmentRequest : IComponentData {
        public GunType gunType;
        public float3 position;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GunSpawnSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            SystemHandle gunLibrarySystemHandle = state.World.GetExistingSystem<GunLibrarySystem>();
            GunLibrarySystem gunLibrarySystemRef = state.World.Unmanaged.GetUnsafeSystemRef<GunLibrarySystem>(gunLibrarySystemHandle);
            SystemHandle attachmentLibrarySystemHandle = state.World.GetExistingSystem<AttachmentLibrarySystem>();
            AttachmentLibrarySystem attachmentLibrarySystemRef = state.World.Unmanaged.GetUnsafeSystemRef<AttachmentLibrarySystem>(attachmentLibrarySystemHandle);
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, requestEntity) in SystemAPI.Query<RefRO<SpawnGunRequest>>().WithEntityAccess()) {
                Entity descriptorEntity = gunLibrarySystemRef.GetDescriptor(request.ValueRO.gunType);
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
                var gunBlobRef = state.EntityManager.GetComponentData<GunBlobReference>(descriptorEntity);
                GunTemplateBlob gunBlob = gunBlobRef.templateBlob.Value;

                Entity gunEntity = state.EntityManager.Instantiate(builtPrefab.prefab);
                
                ecb.SetName(gunEntity, request.ValueRO.gunType.ToString());

                ecb.AddBuffer<Child>(gunEntity);
                ecb.AddComponent<DroppedItemTag>(gunEntity);
                ecb.AddComponent<GunTag>(gunEntity);
                ecb.AddComponent<GunTypeComponent>(gunEntity);
                ecb.AddComponent<AmmoComponent>(gunEntity);
                ecb.AddComponent<DurabilityComponent>(gunEntity);
                ecb.AddComponent<DamageComponent>(gunEntity);
                ecb.AddComponent<ReloadTimeComponent>(gunEntity);
                ecb.AddComponent<WeaponData>(gunEntity);
                ecb.AddComponent<MuzzlePointTransform>(gunEntity);
                ecb.AddComponent<ScopePointTransform>(gunEntity);
                ecb.AddComponent<Item>(gunEntity);
                ecb.SetComponent(gunEntity, new GunTypeComponent { type = gunTypeComponent.type });
                ecb.SetComponent(gunEntity, new AmmoComponent {
                    capacity = gunBlob.ammoCapacity,
                    currentAmmo = gunBlob.ammoCapacity,
                    isReloading = false,
                });
                ecb.SetComponent(gunEntity, new DurabilityComponent { value = gunBlob.durability });
                ecb.SetComponent(gunEntity, new DamageComponent { value = gunBlob.damage });
                ecb.SetComponent(gunEntity, new ReloadTimeComponent { time = gunBlob.reloadTime });
                ecb.SetComponent(gunEntity, new WeaponData {
                    attackRate = gunBlob.attackRate,
                    recoilAmount = gunBlob.recoilAmount,
                    spreadAmount = gunBlob.spreadAmount,
                    lastAttackTime = gunBlob.lastAttackTime,
                    bulletsPerShot = gunBlob.bulletsPerShot
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
                var scale = 1f;

                LocalTransform localTransform = LocalTransform.FromPositionRotationScale(position, rotation, scale);

                ecb.SetComponent(gunEntity, localTransform);
                
                // Spawn Base Scope that has +0 values
                Entity baseScopeEntity = attachmentLibrarySystemRef.GetDescriptor(AttachmentType.Scope);
                if (baseScopeEntity != Entity.Null) {
                    var scopePrefab = state.EntityManager.GetComponentData<BuiltPrefab>(baseScopeEntity);
                    Entity attachmentEntity = ecb.Instantiate(scopePrefab.prefab);
                    ecb.SetName(attachmentEntity, AttachmentType.Scope.ToString());
                    ecb.AddComponent(attachmentEntity, new ScopeAttachmentComponent() {
                        accuracyModifier = 5,
                    });
                    
                    ecb.AddComponent(attachmentEntity, LocalTransform.FromPositionRotationScale(
                        scopePointTransform.position,
                        scopePointTransform.rotation,
                        1.0f
                    ));

                    ecb.AddComponent(attachmentEntity, new AttachmentTag());
                    ecb.AddComponent(attachmentEntity, new AttachmentTypeComponent {
                        attachmentType = AttachmentType.Scope
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
                    //spriteRenderer.sprite = attachmentTemplate.attachmentSprite;
                    
                    if (!state.EntityManager.HasComponent<Child>(gunEntity))
                    {
                        ecb.AddBuffer<Child>(gunEntity);
                    }
                }
                
                Entity baseBarrelEntity = attachmentLibrarySystemRef.GetDescriptor(AttachmentType.Barrel);
                if (baseBarrelEntity != Entity.Null) {
                    var barrelPrefab = state.EntityManager.GetComponentData<BuiltPrefab>(baseBarrelEntity);
                    Entity attachmentEntity = ecb.Instantiate(barrelPrefab.prefab);
                    ecb.SetName(attachmentEntity, AttachmentType.Barrel.ToString());
                    ecb.AddComponent(attachmentEntity, new BarrelAttachmentComponent() {
                        accuracyModifier = 5,
                    });
                    
                    ecb.AddComponent(attachmentEntity, LocalTransform.FromPositionRotationScale(
                        muzzlePointTransform.position,
                        muzzlePointTransform.rotation,
                        1.0f
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
    }
}