using ECS.Components;
using Unity.Entities;

namespace ECS.Bakers {
    public class ItemSpawnBaker : Baker<ItemSpawnerAuthoring> {
        public override void Bake(ItemSpawnerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new EntityData {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            });
            
            AddComponent(entity, new ItemSpawner {});
        }
    }
}