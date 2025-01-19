using System.Numerics;
using ECS.Bakers;
using ScriptableObjects;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Vector3 = UnityEngine.Vector3;

namespace ECS {
    [BurstCompile]
    public partial struct EquippedWeaponPositionRotation : ISystem {
        private EntityQuery playerQuery;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<MousePosition>();
            state.RequireForUpdate<PlayerSingleton>();
            playerQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerSingleton>());
        }

        public void OnUpdate(ref SystemState state) {
            if (playerQuery.IsEmpty)
                return;

            // Get the player singleton and the equipped gun buffer
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            if (equippedGunBuffer.IsEmpty) {
                return;
            }

            if (state.EntityManager.HasComponent<InventoryOpen>(playerSingleton.PlayerEntity)) {
                return;
            }

            // Get the player and gun's local transforms
            var localTransform = SystemAPI.GetComponentRO<LocalTransform>(playerSingleton.PlayerEntity);
            var closestEnemy = SystemAPI.GetComponentRO<ClosestEnemyComponent>(playerSingleton.PlayerEntity);
            var gunEntity = equippedGunBuffer[0];
            var gunEntityLocalTransform = SystemAPI.GetComponent<LocalTransform>(gunEntity.GunEntity);

            float2 playerPosition = new float2(localTransform.ValueRO.Position.x, localTransform.ValueRO.Position.y);

            float2 targetPosition;
            if (closestEnemy.ValueRO.closestEnemy == Entity.Null) {
                targetPosition = playerPosition + new float2(1, 0);
            } else {
                if (closestEnemy.ValueRO.closestEnemy != Entity.Null && SystemAPI.HasComponent<LocalTransform>(closestEnemy.ValueRO.closestEnemy)) {
                    var enemyLocalTransform = SystemAPI.GetComponentRO<LocalTransform>(closestEnemy.ValueRO.closestEnemy);
                    targetPosition = new float2(
                        enemyLocalTransform.ValueRO.Position.x,
                        enemyLocalTransform.ValueRO.Position.y
                    );
                }
                else {
                    targetPosition = playerPosition + new float2(1, 0);
                }
            }

            float2 directionToTarget = targetPosition - playerPosition;
            float angle = math.atan2(directionToTarget.y, directionToTarget.x);

            gunEntityLocalTransform.Rotation = quaternion.Euler(0, 0, angle);
            gunEntityLocalTransform.Position = new float3(
                playerPosition.x + math.normalize(directionToTarget).x * 0.2f,
                playerPosition.y + math.normalize(directionToTarget).y * 0.2f,
                localTransform.ValueRO.Position.z
            );

            SystemAPI.SetComponent(gunEntity.GunEntity, gunEntityLocalTransform);
        }
    }
}