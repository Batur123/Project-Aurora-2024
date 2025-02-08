/*using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems {
    public struct HealthBarUIData : IComponentData
    {
        public float3 ScreenPosition; // The screen position of the health bar
        public float HealthPercentage; // The fill percentage
    }
    
    public struct HealthBarData : IComponentData
    {
        public Entity EnemyEntity; // The enemy associated with this health bar
        public float MaxHealth;    // Maximum health of the enemy
        public float CurrentHealth; // Current health of the enemy
    }

    public struct HealthBarEntity : IComponentData {
        public Entity healthBarEntity;
    }
    
    [BurstCompile]
    public partial class HealthBarSpawnSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithAll<EnemyTag, IsSpawned>().WithNone<HealthBarData>() // Only process enemies without a health bar
                .ForEach((Entity enemy, ref EnemyData enemyData, ref LocalTransform transform) =>
                {
                    EntityManager.AddComponentData(enemy, new HealthBarData
                    {
                        EnemyEntity = enemy,
                        MaxHealth = enemyData.maxHealth,
                        CurrentHealth = enemyData.health
                    });

                    EntityManager.AddComponentData(enemy, new HealthBarUIData
                    {
                        ScreenPosition = float3.zero,
                        HealthPercentage = 1.0f
                    });
                }).WithStructuralChanges().Run();
        }
    }

    public partial class HealthBarUpdateSystem : SystemBase {
        
        protected override void OnCreate() {
            RequireForUpdate<PlayerSingleton>();
        }

        protected override void OnUpdate() {
            // Get the camera's view-projection matrix
            var camTransform = Camera.main.transform;
            float4x4 viewMatrix = float4x4.TRS(
                camTransform.position,
                camTransform.rotation,
                new float3(1f, 1f, 1f)
            );
            float4x4 projectionMatrix =  Camera.main.projectionMatrix;

            // Combine the view and projection matrices
            float4x4 viewProjectionMatrix = math.mul(projectionMatrix, math.inverse(viewMatrix));

            // Pass the matrix to the job
            var matrix = viewProjectionMatrix;
            float screenWidth =  Camera.main.pixelWidth;
            float screenHeight =  Camera.main.pixelHeight;

            Entities
                .WithBurst()
                .ForEach((ref HealthBarData healthBar, ref HealthBarUIData uiData, in LocalTransform enemyTransform) => {
                    // World position of the health bar
                    float4 worldPos = new float4(enemyTransform.Position.x, enemyTransform.Position.y + 0.5f, enemyTransform.Position.z, 1f);

                    // Transform to clip space
                    float4 clipPos = math.mul(matrix, worldPos);

                    // Perform perspective divide
                    float3 ndcPos = clipPos.xyz / clipPos.w;

                    // Convert NDC to screen space
                    uiData.ScreenPosition = new float3(
                        (ndcPos.x * 0.5f + 0.5f) * screenWidth,
                        (1f - (ndcPos.y * 0.5f + 0.5f)) * screenHeight,
                        ndcPos.z
                    );

                    // Update health percentage
                    uiData.HealthPercentage = math.clamp(healthBar.CurrentHealth / healthBar.MaxHealth, 0f, 1f);
                }).ScheduleParallel();
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial class HealthBarRenderSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .ForEach((ref LocalToWorld localToWorld, in HealthBarUIData uiData) =>
                {
                    // Scale the foreground bar based on health percentage
                    float3 scale = localToWorld.Value.c0.xyz;
                    scale.x = uiData.HealthPercentage;
                    localToWorld.Value.c0.xyz = scale;

                    // Update the position of the health bar
                    float3 newPosition = uiData.ScreenPosition;
                    localToWorld.Value.c3 = new float4(newPosition, 1f);

                }).ScheduleParallel();
        }
    }
}*/