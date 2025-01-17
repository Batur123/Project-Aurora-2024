using ScriptableObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ECS {
    public struct AttachmentTag : IComponentData {}

    public struct StockAttachmentComponent : IComponentData {
        public float recoilModifier;
        public float accuracyModifier;
    }

    public struct BarrelAttachmentComponent : IComponentData {
        public float damageModifier;
        public float accuracyModifier;
    }

    public struct MagazineAttachmentComponent : IComponentData {
        public int capacityModifier;
        public float reloadSpeedModifier;
    }

    public struct ScopeAttachmentComponent : IComponentData {
        public float accuracyModifier;
    }

    public struct AmmunitionAttachmentComponent : IComponentData {
        public float damageModifier;
    }
    
    public struct AttachmentTypeComponent : IComponentData {
        public AttachmentType attachmentType;
    }

    public struct GunAttachment : IBufferElementData
    {
        public Entity AttachmentEntity;
    }

    public struct AttachmentPrefab : IComponentData {
        public Entity prefab;
    }
    
    public class AttachmentManager : MonoBehaviour {
        public AttachmentTemplate[] attachmentTemplates; // Assign this in the inspector

        private EntityManager entityManager;

        void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Debug.Assert(entityManager != null, "EntityManager is null! Ensure the default world is initialized.");
        }

        public Entity CreateAttachmentEntity(LocalTransform gunTransform, AttachmentTemplate attachmentTemplate) {
            var prefabEntity = GetPrefabEntityForAttachment(attachmentTemplate);
            if (prefabEntity == Entity.Null) {
                Debug.LogError("Prefab entity for attachment type not found!" + attachmentTemplate);
                return Entity.Null;
            }
            
            var attachmentTypeComponent = entityManager.GetComponentData<AttachmentTypeComponent>(prefabEntity);
            var builtPrefab = entityManager.GetComponentData<AttachmentPrefab>(prefabEntity);
            Entity attachmentEntity = entityManager.Instantiate(builtPrefab.prefab);
            entityManager.SetName(attachmentEntity, attachmentTemplate.name);
            
            var attachmentObject = new GameObject(attachmentTemplate.attachmentName);
            var spriteRenderer = attachmentObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = attachmentTemplate.attachmentSprite;

            switch (attachmentTemplate.attachmentType) {
                case AttachmentType.Stock:
                    entityManager.AddComponentData(attachmentEntity, new StockAttachmentComponent {
                        recoilModifier = attachmentTemplate.recoilModifier,
                        accuracyModifier = attachmentTemplate.accuracyModifier
                    });
                    //attachmentObject.transform.localPosition = new Vector3(-0.5f, 0, 0);
                    break;
                case AttachmentType.Barrel:
                    entityManager.AddComponentData(attachmentEntity, new BarrelAttachmentComponent {
                        damageModifier = attachmentTemplate.damageModifier,
                        accuracyModifier = attachmentTemplate.accuracyModifier
                    });
                    //attachmentObject.transform.localPosition = new Vector3(0.5f, 0, 0);

                    break;
                case AttachmentType.Magazine:
                    entityManager.AddComponentData(attachmentEntity, new MagazineAttachmentComponent {
                        capacityModifier = attachmentTemplate.capacityModifier,
                        reloadSpeedModifier = attachmentTemplate.reloadSpeedModifier
                    });
                    //attachmentObject.transform.localPosition = new Vector3(0, -0.3f, 0);
                    break;
                case AttachmentType.Scope:
                    entityManager.AddComponentData(attachmentEntity, new ScopeAttachmentComponent {
                        accuracyModifier = attachmentTemplate.accuracyModifier
                    });
                    //.transform.localPosition = new Vector3(0, 0.3f, 0);
                    break;
                case AttachmentType.Ammunition:
                    entityManager.AddComponentData(attachmentEntity, new AmmunitionAttachmentComponent {
                        damageModifier = attachmentTemplate.damageModifier
                    });
                    break;
            }

            entityManager.AddComponentData(attachmentEntity, new AttachmentTypeComponent {
                attachmentType = attachmentTypeComponent.attachmentType
            });

            //entityManager.SetComponentData(attachmentEntity, LocalTransform.FromPositionRotationScale(
            //    gunTransform.Position + math.mul(gunTransform.Rotation, new float3(0f, 0.1f, 0f)),
            //    quaternion.identity,
            //    2.0f
            //));
            
            Debug.Log($"Created attachment: {attachmentTemplate.attachmentName}");
            return attachmentEntity;
        }
        
        public void AttachAttachmentToGun(Entity gunEntity, Entity attachmentEntity)
        {
            var buffer = entityManager.GetBuffer<GunAttachment>(gunEntity);
            buffer.Add(new GunAttachment { AttachmentEntity = attachmentEntity });

            if (entityManager.HasComponent<ScopeAttachmentComponent>(attachmentEntity))
            {
                
            }
            
            if (entityManager.HasComponent<BarrelAttachmentComponent>(attachmentEntity))
            {
                var barrel = entityManager.GetComponentData<BarrelAttachmentComponent>(attachmentEntity);
                var damageComponent = entityManager.GetComponentData<DamageComponent>(gunEntity);
                damageComponent.value += barrel.damageModifier;
                entityManager.SetComponentData(gunEntity, damageComponent);
            }
        }
        
        private Entity GetPrefabEntityForAttachment(AttachmentTemplate attachmentTemplate) {
            Entity prefabEntity = Entity.Null;
            var entityQuery = entityManager.CreateEntityQuery(typeof(AttachmentPrefab), typeof(AttachmentTag));
            using (var entities = entityQuery.ToEntityArray(Allocator.TempJob)) {
                foreach (var entity in entities) {
                    var attachmentTypeComponent = entityManager.GetComponentData<AttachmentTypeComponent>(entity);
                    if (attachmentTypeComponent.attachmentType == attachmentTemplate.attachmentType) {
                        prefabEntity = entity;
                        break;
                    }
                }
            }
            return prefabEntity;
        }
    }
}