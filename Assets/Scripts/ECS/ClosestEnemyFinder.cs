using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct ClosestEnemyComponent : IComponentData
{
    public Entity closestEnemy;
    public float3 closestPosition;
}

namespace ECS {
    
    [BurstCompile]
    public partial struct ClosestEnemySystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }
        
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var playerPosition = SystemAPI.GetComponentRO<LocalTransform>(playerSingleton.PlayerEntity);
            
            Entity closestEnemy = Entity.Null;
            var closestDistance = float.MaxValue;

            foreach (var (enemyTransform, enemyEntity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag, IsSpawned>().WithEntityAccess()) {
                var distance = math.distance(playerPosition.ValueRO.Position, enemyTransform.ValueRO.Position);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEnemy = enemyEntity;
                }
            }

            if (closestEnemy != Entity.Null && SystemAPI.HasComponent<ClosestEnemyComponent>(playerSingleton.PlayerEntity)) {
                ecb.SetComponent(playerSingleton.PlayerEntity, new ClosestEnemyComponent { closestEnemy = closestEnemy, closestPosition = closestDistance });
            }


            ecb.Playback(state.EntityManager);
        }
    }
}