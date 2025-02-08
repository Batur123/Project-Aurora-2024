using ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace ECS.Bakers {
    public class EnemySpawnBaker : Baker<EnemySpawnerAuthoring> {
        public override void Bake(EnemySpawnerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new EntityData {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                healthBarPrefab = GetEntity(authoring.healthBarPrefab, TransformUsageFlags.Dynamic),
            });

            AddComponent(entity, new SpawnerTime {
                nextSpawnTime = 2.0f
            });
            
            var spriteRenderer = authoring.prefab.transform.GetComponent<SpriteRenderer>();

            var Mesh = SpriteMeshUtility.CreateMeshFromSprite(spriteRenderer.sprite);
            var Material = spriteRenderer.sharedMaterial;
            
            AddComponentObject(entity, new EnemyMaterial {
                material = Material,
                mesh = Mesh,
            });
        }
    }
}