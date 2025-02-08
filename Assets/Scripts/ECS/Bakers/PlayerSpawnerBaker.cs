using ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace ECS.Bakers {
    public class PlayerSpawnBaker : Baker<PlayerSpawnerAuthoring> {
        public override void Bake(PlayerSpawnerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new EntityData {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            });
            AddComponent(entity, new PlayerTag { });
        }
    }
}