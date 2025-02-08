using System.Numerics;
using ECS.Bakers;
using ECS.Components;
using ScriptableObjects;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
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

            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            Entity playerEntity = playerSingleton.PlayerEntity;
            var equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerEntity);
            if (equippedGunBuffer.IsEmpty)
                return;

            if (state.EntityManager.HasComponent<InventoryOpen>(playerEntity))
                return;

            var playerLocalTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);

            var gunEntity = equippedGunBuffer[0].GunEntity;
            var gunLocalTransform = SystemAPI.GetComponent<LocalTransform>(gunEntity);

            var mousePositionSingleton = SystemAPI.GetSingleton<MousePosition>();
            float2 mousePos2D = new float2(mousePositionSingleton.Value.x, mousePositionSingleton.Value.y);

            float2 playerPos = new float2(playerLocalTransform.Position.x, playerLocalTransform.Position.y);
            float2 directionToMouse = mousePos2D - playerPos;
            float2 normalizedDir = math.normalize(directionToMouse);

            float angleToMouse = math.atan2(directionToMouse.y, directionToMouse.x);
            float playerAngle = math.atan2(playerLocalTransform.Rotation.value.y, playerLocalTransform.Rotation.value.x);
            float relativeAngle = angleToMouse - playerAngle;

            gunLocalTransform.Rotation = quaternion.Euler(0, 0, relativeAngle);
            gunLocalTransform.Position = new float3(
                playerPos.x + normalizedDir.x * 0.2f,
                playerPos.y + normalizedDir.y * 0.2f,
                playerLocalTransform.Position.z
            );

            SystemAPI.SetComponent(gunEntity, gunLocalTransform);
        }
    }
}