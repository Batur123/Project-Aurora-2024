using ECS.Components;
using Unity.Entities;

namespace ECS.Bakers {
    public class ProjectileSpawnBaker : Baker<ProjectileSpawnerAuthoring> {
        public override void Bake(ProjectileSpawnerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new EntityData {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                grenadePrefab = GetEntity(authoring.grenadePrefab, TransformUsageFlags.Dynamic),
                grenadeExplosionPrefab = GetEntity(authoring.grenadeExplosionPrefab, TransformUsageFlags.Dynamic),
            });

            AddComponent(entity, new ProjectileSpawner { });
        }
    }
}