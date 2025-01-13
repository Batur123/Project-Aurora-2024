using Unity.Entities;

namespace ECS {
    public class EnemySpawnBaker : Baker<EnemySpawnerAuthoring> {
        public override void Bake(EnemySpawnerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new EntityData {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            });

            AddComponent(entity, new SpawnerTime {
                nextSpawnTime = 2.0f
            });
        }
    }

    public class ProjectileSpawnBaker : Baker<ProjectileSpawnerAuthoring> {
        public override void Bake(ProjectileSpawnerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new EntityData {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            });

            AddComponent(entity, new ProjectileSpawner { });
        }
    }
    
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