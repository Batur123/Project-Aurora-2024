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
            root.accuracyModifier = template.accuracyModifier;
            root.damageModifier = template.damageModifier;
            root.capacityModifier = template.capacityModifier;
            root.recoilModifier = template.recoilModifier;
            root.reloadSpeedModifier = template.reloadSpeedModifier;

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