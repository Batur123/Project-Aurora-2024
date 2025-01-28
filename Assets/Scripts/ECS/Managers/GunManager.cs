using ECS.Bakers;
using ScriptableObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ECS {
    public struct DroppedItemTag : IComponentData {}
    public struct GunTag : IComponentData {}

    public struct AmmoComponent : IComponentData {
        public int currentAmmo;
        public int capacity;
        public bool isReloading;
    }

    // Base Stats
    public struct BaseWeaponData : IComponentData {
        public int ammoCapacity;
        public float damage;
        public float attackSpeed;
        public float recoilAmount;
        public float spreadAmount;
        public int bulletsPerShot;
        public float reloadSpeed;
        public int piercingBulletsPerShot;
    }
    
    // Calculated Stats after base + upgrades
    public struct WeaponData : IComponentData {
        public FixedString64Bytes weaponName;
        public int ammoCapacity;
        public float damage;
        public float attackSpeed;
        public float recoilAmount;
        public float spreadAmount;
        public int bulletsPerShot;
        public float reloadSpeed;
        public int piercingBulletsPerShot;
    }

    // Attachment Stats that calculates
    public struct AttachmentComponent : IComponentData {
        public FixedString64Bytes attachmentName;
        public int ammoCapacity;
        public float damage;
        public float attackSpeed;
        public float recoilAmount;
        public float spreadAmount;
        public int bulletsPerShot;
        public float reloadSpeed;
        public int piercingBulletsPerShot;
    }
    
    // Blob data from initial scriptable object to put into BaseStats
    public struct GunTemplateBlob
    {
        public int ammoCapacity;
        public float damage;
        public float attackSpeed;
        public float recoilAmount;
        public float spreadAmount;
        public int bulletsPerShot;
        public float reloadSpeed;
        public int piercingBulletsPerShot;
    }
    
    public struct AttachmentTemplateBlob
    {
        public float damageModifier;
        public float reloadSpeedModifier;
        public float accuracyModifier;
        public float recoilModifier;
        public int capacityModifier;
    }
    
    public struct MuzzlePointTransform : IComponentData
    {
        public float3 position;   // Position of the muzzle point
        public quaternion rotation; // Rotation of the muzzle point
        public float3 scale;      // Scale of the muzzle point
        public float3 offset;
        public float3 boundOffset;
    }

    public struct ScopePointTransform : IComponentData
    {
        public float3 position;   // Position of the scope point
        public quaternion rotation; // Rotation of the scope point
        public float3 scale;      // Scale of the scope point
        public float3 offset;
        public float3 boundOffset;
    }

    public struct GunBlobReference : IComponentData
    {
        public BlobAssetReference<GunTemplateBlob> templateBlob;
    }

    public struct AttachmentBlobReference : IComponentData
    {
        public BlobAssetReference<AttachmentTemplateBlob> templateBlob;
    }

    public struct AttachmentTag : IComponentData {}

    public struct AttachmentTypeComponent : IComponentData {
        public AttachmentType attachmentType;
        public int variantId;
        public float lootWeight;
    }

    public struct GunTypeComponent : IComponentData
    {
        public GunType gunType;
        public int variantId;
        public float lootWeight;
    }
    
    public struct WeaponProjectileTypeComponent : IComponentData {
        public ProjectileType projectileType;
    }
    
    public struct GrenadeComponent : IComponentData
    {
        public float3 StartPosition; // Where the grenade starts
        public float3 TargetPosition; // Where the grenade should land
        public float PeakHeight;
        public float ThrowTime; // Total time for the throw
        public float ElapsedTime; // Time elapsed since the throw began
        public float FuseDuration;
        public float ExplosionRadius;
        public float3 RandomizedTarget;
    }

    public struct ExplosionTag : IComponentData {
        public float lifeTime;
        public float elapsedExplosionTime;
    }

    public struct StartFuseCountdown : IComponentData {}

    public struct MuzzlePoint : IComponentData {
        public float3 position;  // Position of the muzzle point relative to the weapon
    }
    
    public struct BuiltPrefab : IComponentData {
        public Entity prefab;
    }
    
    public class GunManager : MonoBehaviour
    {
        public GunTemplate[] gunTemplates;
        private EntityManager entityManager;
        public AttachmentManager attachmentManager;
        
        void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Debug.Assert(entityManager != null, "EntityManager is null! Ensure the default world is initialized.");
        }
 
        public void CreateGunEntity(GunTemplate gunTemplate) {
            var prefabEntity = GetPrefabEntityForGun(gunTemplate);

            if (prefabEntity == Entity.Null) {
                Debug.Log("Prefab entity for gun type not found!");
                return;
            }
            
            var gunTypeComponent = entityManager.GetComponentData<GunTypeComponent>(prefabEntity);
            var builtPrefab = entityManager.GetComponentData<BuiltPrefab>(prefabEntity);
            var muzzlePointTransform = entityManager.GetComponentData<MuzzlePointTransform>(prefabEntity);
            var scopePointTransform = entityManager.GetComponentData<ScopePointTransform>(prefabEntity);

            Entity gunEntity = entityManager.Instantiate(builtPrefab.prefab);
            Debug.Log("INSTANTIATE");
            entityManager.AddBuffer<Child>(gunEntity);

            entityManager.AddComponent<DroppedItemTag>(gunEntity);
            entityManager.AddComponent<GunTag>(gunEntity);

            entityManager.AddComponent<GunTypeComponent>(gunEntity);
            entityManager.AddComponent<AmmoComponent>(gunEntity);
            entityManager.AddComponent<WeaponData>(gunEntity);
            entityManager.AddComponent<MuzzlePoint>(gunEntity);
            entityManager.AddComponent<ScopePointTransform>(gunEntity);

            entityManager.SetName(gunEntity, gunTemplate.gunName);

            entityManager.SetComponentData(gunEntity, new GunTypeComponent {
                gunType = gunTypeComponent.gunType,
                variantId = 0,
            });
            entityManager.SetComponentData(gunEntity, new AmmoComponent {
                capacity = gunTemplate.ammoCapacity, 
                currentAmmo = gunTemplate.ammoCapacity,
                isReloading = false,
            });
            entityManager.SetComponentData(gunEntity, new WeaponData {
                attackSpeed = gunTemplate.attackSpeed,
                recoilAmount = gunTemplate.recoilAmount,
                spreadAmount = gunTemplate.spreadAmount,
                bulletsPerShot = gunTemplate.bulletsPerShot
            });

            entityManager.AddBuffer<GunAttachment>(gunEntity);

            var position = new float3(Random.Range(1, 1), Random.Range(1,1), 0);
            var rotation = quaternion.identity;
            var scale = 1f;
            
            LocalTransform localTransform = LocalTransform.FromPositionRotationScale(position, rotation, scale);
            
            entityManager.SetComponentData(gunEntity, localTransform);

            foreach (var attachmentTemplate in attachmentManager.attachmentTemplates) {
                var attachmentEntity = attachmentManager.CreateAttachmentEntity(localTransform, attachmentTemplate);
                
                var gunWorldPosition = localTransform.Position;
                var scopePointWorldPosition = localTransform.Position + math.mul(localTransform.Rotation, scopePointTransform.position);
                scopePointTransform.offset = scopePointWorldPosition - gunWorldPosition;
                
                entityManager.SetComponentData(attachmentEntity, LocalTransform.FromPositionRotationScale(
                    localTransform.Position + math.mul(localTransform.Rotation, scopePointTransform.position),
                    localTransform.Rotation,
                    1.0f
                ));
                entityManager.SetComponentData(gunEntity, scopePointTransform);
                
                attachmentManager.AttachAttachmentToGun(gunEntity, attachmentEntity);
            }
        }

        private Entity GetPrefabEntityForGun(GunTemplate gunTemplate) {
            Entity prefabEntity = Entity.Null;
            var entityQuery = entityManager.CreateEntityQuery(typeof(BuiltPrefab), typeof(GunTypeComponent));
            using (var entities = entityQuery.ToEntityArray(Allocator.TempJob)) {
                foreach (var entity in entities) {
                    var gunTypeComponent = entityManager.GetComponentData<GunTypeComponent>(entity);
                    if (gunTypeComponent.gunType == gunTemplate.gunType) {
                        prefabEntity = entity;
                        break;
                    }
                }
            }
            return prefabEntity;
        }
    }

}