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
    
    public struct GunTypeComponent : IComponentData
    {
        public GunType type;
    }

    public struct AmmoComponent : IComponentData {
        public int currentAmmo;
        public int capacity;
        public bool isReloading;
    }

    public struct DurabilityComponent : IComponentData
    {
        public int value;
    }

    public struct DamageComponent : IComponentData
    {
        public float value;
    }

    public struct ReloadTimeComponent : IComponentData
    {
        public float time;
    }

    public struct WeaponData : IComponentData {
        public float attackRate; // Attacks per second
        public float recoilAmount; // Recoil intensity
        public float spreadAmount; // Spread of bullets
        public float lastAttackTime; // Time of last attack
        public int bulletsPerShot;
    }

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
        
        
        void Start()
        {
            //foreach (var gunTemplate in gunTemplates) {
            //    CreateGunEntity(gunTemplate);
            //}
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
            entityManager.AddComponent<DurabilityComponent>(gunEntity);
            entityManager.AddComponent<DamageComponent>(gunEntity);
            entityManager.AddComponent<ReloadTimeComponent>(gunEntity);
            entityManager.AddComponent<WeaponData>(gunEntity);
            entityManager.AddComponent<MuzzlePoint>(gunEntity);
            entityManager.AddComponent<ScopePointTransform>(gunEntity);

            entityManager.SetName(gunEntity, gunTemplate.gunName);

            entityManager.SetComponentData(gunEntity, new GunTypeComponent { type = gunTypeComponent.type });
            entityManager.SetComponentData(gunEntity, new AmmoComponent {
                capacity = gunTemplate.ammoCapacity, 
                currentAmmo = gunTemplate.ammoCapacity,
                isReloading = false,
            });
            entityManager.SetComponentData(gunEntity, new DurabilityComponent { value = gunTemplate.durability });
            entityManager.SetComponentData(gunEntity, new DamageComponent { value = gunTemplate.damage });
            entityManager.SetComponentData(gunEntity, new ReloadTimeComponent { time = gunTemplate.reloadTime });
            entityManager.SetComponentData(gunEntity, new WeaponData {
                attackRate = gunTemplate.attackRate,
                recoilAmount = gunTemplate.recoilAmount,
                spreadAmount = gunTemplate.spreadAmount,
                lastAttackTime = 0f,
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
                    if (gunTypeComponent.type == gunTemplate.gunType) {
                        prefabEntity = entity;
                        break;
                    }
                }
            }
            return prefabEntity;
        }
    }

}