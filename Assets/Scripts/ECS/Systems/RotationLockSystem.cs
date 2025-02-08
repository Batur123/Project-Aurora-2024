using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems {
    [BurstCompile]
    public partial struct PlayerLockRotation : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var localTransform = SystemAPI.GetComponent<LocalTransform>(playerSingleton.PlayerEntity);
            localTransform.Rotation = Quaternion.identity;
            SystemAPI.SetComponent(playerSingleton.PlayerEntity, localTransform);
        }
    }

    [BurstCompile]
    public partial struct EnemyLockRotation : ISystem {
        public void OnUpdate(ref SystemState state) {
            foreach (var enemyTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<EnemyTag, IsSpawned>()) {
                enemyTransform.ValueRW.Rotation = Quaternion.identity;
            }
        }
    }
}