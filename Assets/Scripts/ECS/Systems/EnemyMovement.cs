using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems {
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemyMovementSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            if (SystemAPI.TryGetSingletonRW(out RefRW<PlayerSingleton> singletonRW)) {
                Entity playerEntity = singletonRW.ValueRW.PlayerEntity;
                RefRO<LocalTransform> playerTransform = SystemAPI.GetComponentRO<LocalTransform>(playerEntity);

                foreach (var (enemyPhysics, enemyTransform) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<LocalTransform>>()
                             .WithAll<EnemyTag, IsSpawned>()) {
                    float3 direction = math.normalize(playerTransform.ValueRO.Position - enemyTransform.ValueRO.Position);
                    enemyPhysics.ValueRW.Linear = direction * 1f; // Adjust speed here
                }
            }
        }
    }

    public partial class EnemyMovementSpriteFlip : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<PlayerSingleton>();
            RequireForUpdate<EnemyTag>();
        }


        protected override void OnUpdate() {
            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            LocalTransform playerLocalTransform = SystemAPI.GetComponent<LocalTransform>(playerSingleton.PlayerEntity);
            
            Entities.WithAll<EnemyTag, IsSpawned>()
                .ForEach((LocalTransform localTransform, SpriteRenderer spriteRenderer) =>
                {
                    var directionToPlayer = playerLocalTransform.Position.x - localTransform.Position.x;
                    if (directionToPlayer > 0) {
                        spriteRenderer.flipX = false;
                    } else if (directionToPlayer < 0) {
                        spriteRenderer.flipX = true;
                    }
                })
                .WithoutBurst().Run();
        }
    }
}