using ECS.Authorings;
using ECS.Components;
using ECS.Systems;
using Unity.Entities;

namespace ECS.Bakers {
    public class ParticleSpawnBaker : Baker<ParticleSpawnerAuthoring> {
        public override void Bake(ParticleSpawnerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ParticleData {
                rainPrefab = GetEntity(authoring.rainPrefab, TransformUsageFlags.Dynamic),
                bulletHitExplosionPrefab = GetEntity(authoring.bulletHitExplosionPrefab, TransformUsageFlags.Dynamic),
            });
        }
    }
}