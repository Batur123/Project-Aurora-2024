using System.Numerics;
using ECS.Bakers;
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
            var localTransform = SystemAPI.GetComponent<LocalTransform>(playerSingleton.PlayerEntity);
            var gunEntity = equippedGunBuffer[0];
            var gunEntityLocalTransform = SystemAPI.GetComponent<LocalTransform>(gunEntity.GunEntity);
            var mousePositionEntity = SystemAPI.GetSingleton<MousePosition>();

            Vector3 mouseWorldPosition = mousePositionEntity.Value;
            float2 playerPosition = new float2(localTransform.Position.x, localTransform.Position.y);
            float2 mousePosition = new float2(mouseWorldPosition.x, mouseWorldPosition.y);
            float2 directionToMouse = mousePosition - playerPosition;
            float angle = math.atan2(directionToMouse.y, directionToMouse.x);
            float playerAngle = math.atan2(localTransform.Rotation.value.y, localTransform.Rotation.value.x);
            angle -= playerAngle;
            gunEntityLocalTransform.Rotation = quaternion.Euler(0, 0, angle);
            gunEntityLocalTransform.Position = new float3(
                playerPosition.x + math.normalize(directionToMouse).x * 0.2f,
                playerPosition.y + math.normalize(directionToMouse).y * 0.2f,
                localTransform.Position.z
            );
            SystemAPI.SetComponent(gunEntity.GunEntity, gunEntityLocalTransform);
        }
    }
}