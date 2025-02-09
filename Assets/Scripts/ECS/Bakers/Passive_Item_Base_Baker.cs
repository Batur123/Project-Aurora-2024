using ECS.Authorings;
using ECS.Components;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace ECS.Bakers {
    public class PassiveItemBaker : Baker<PassiveItemAuthoring> {
        public override void Bake(PassiveItemAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var template = authoring.passiveItemTemplate;
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<PassiveItemTemplateBlob>();

            var statsData = template.statsData;
            var characterData = template.characterStatsData;
            
            root.statsData = statsData;
            root.characterData = characterData;
            
            var blobRef = builder.CreateBlobAssetReference<PassiveItemTemplateBlob>(Allocator.Persistent);
            builder.Dispose();

            AddBlobAsset(ref blobRef, out var hash);
            AddComponent(entity, new PassiveItemBlobReference { templateBlob = blobRef });
            AddComponent(entity, new PassiveItemTypeComponent {
                passiveItemType = authoring.passiveItemType,
                lootWeight = authoring.lootWeight,
                itemName = authoring.passiveItemType.ToString(),
            });
            AddComponent(entity, new BuiltPrefab { prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic) });
            AddComponent(entity, new PassiveItemTag {});
            Debug.Log($"[Baker][Passive Item]: {authoring.passiveItemType} - [Item Name]: {template.passiveItemName}");
        }
    }
}