using ECS.Authorings.BaseAttachments;
using ScriptableObjects;
using Unity.Collections;
using Unity.Entities;

namespace ECS.Bakers.AttachmentBakers {
    public class Supressor_Base : Baker<Supressor_BaseAuthoring> {
        public override void Bake(Supressor_BaseAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var template = authoring.attachmentTemplate;
        
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AttachmentTemplateBlob>();
            root.accuracyModifier = 0;
            root.damageModifier = 0;
            root.capacityModifier = 0;
            root.recoilModifier = 0;
            root.reloadSpeedModifier = 0;

            var blobRef = builder.CreateBlobAssetReference<AttachmentTemplateBlob>(Allocator.Persistent);
            builder.Dispose();

            AddBlobAsset(ref blobRef, out var hash);
            
            AddComponent(entity, new AttachmentBlobReference {
                templateBlob = blobRef
            });
            
            AddComponent(entity, new BuiltPrefab
            {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            });
            
            AddComponent(entity, new AttachmentTag{});
            AddComponent(entity, new AttachmentTypeComponent { attachmentType = AttachmentType.Barrel });

        }
    }
}