using ECS.Authorings.BaseAttachments;
using ECS.Components;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace ECS.Bakers.AttachmentBakers {
    public class AttachmentBaker : Baker<AttachmentAuthoring> {
        public override void Bake(AttachmentAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var template = authoring.attachmentTemplate;

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AttachmentTemplateBlob>();

            var stats = template.statsRangeData;
            root.statsRangeData.minAttackSpeed = stats.minAttackSpeed;
            root.statsRangeData.maxAttackSpeed = stats.maxAttackSpeed;
            
            root.statsRangeData.minRecoilAmount = stats.minRecoilAmount;
            root.statsRangeData.maxRecoilAmount = stats.maxRecoilAmount;
            
            root.statsRangeData.minDamage = stats.minDamage;
            root.statsRangeData.maxDamage = stats.maxDamage;
            
            root.statsRangeData.minSpreadAmount = stats.minSpreadAmount;
            root.statsRangeData.maxSpreadAmount = stats.maxSpreadAmount;
            
            root.statsRangeData.minReloadSpeed = stats.minReloadSpeed;
            root.statsRangeData.maxReloadSpeed = stats.maxReloadSpeed;
            
            root.statsRangeData.minAmmoCapacity = stats.minAmmoCapacity;
            root.statsRangeData.maxAmmoCapacity = stats.maxAmmoCapacity;
            
            root.statsRangeData.minBulletsPerShot = stats.minBulletsPerShot;
            root.statsRangeData.maxBulletsPerShot = stats.maxBulletsPerShot;
            
            root.statsRangeData.minPiercingBulletsPerShot = stats.minPiercingBulletsPerShot;
            root.statsRangeData.maxPiercingBulletsPerShot = stats.maxPiercingBulletsPerShot;

            var blobRef = builder.CreateBlobAssetReference<AttachmentTemplateBlob>(Allocator.Persistent);
            builder.Dispose();

            AddBlobAsset(ref blobRef, out var hash);

            AddComponent(entity, new AttachmentBlobReference {
                templateBlob = blobRef
            });

            AddComponent(entity, new AttachmentTypeComponent {
                attachmentType = authoring.attachmentType,
                variantId = authoring.variantId,
                lootWeight = authoring.lootWeight
            });

            AddComponent(entity, new BuiltPrefab {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
            });
            AddComponent(entity, new AttachmentTag {});
            
            Debug.Log($"[Baker][Attachment]: {authoring.attachmentType} - [Item Name]: {template.attachmentName} - [Variant]: {authoring.variantId}");
        }
    }
}