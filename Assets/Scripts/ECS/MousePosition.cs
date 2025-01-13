using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS
{
    public class CameraUtils {
        public static float2 ConvertWorldToScreenCoordinates(float3 point, float3 cameraPos, float4x4 camProjMatrix, float3 camUp, float3 camRight, float3 camForward, float pixelWidth, float pixelHeight, float scaleFactor)
        {
            float4 pointInCameraCoodinates = ConvertWorldToCameraCoordinates(point, cameraPos, camUp, camRight, camForward);
            float4 pointInClipCoordinates = math.mul(camProjMatrix, pointInCameraCoodinates);
            float4 pointInNdc = pointInClipCoordinates / pointInClipCoordinates.w;
            float2 pointInScreenCoordinates;
            pointInScreenCoordinates.x = pixelWidth / 2.0f * (pointInNdc.x + 1);
            pointInScreenCoordinates.y = pixelHeight / 2.0f * (pointInNdc.y + 1);
            return pointInScreenCoordinates / scaleFactor;
        }
  
        private static float4 ConvertWorldToCameraCoordinates(float3 point, float3 cameraPos, float3 camUp, float3 camRight, float3 camForward)
        {
            float4 translatedPoint = new float4(point - cameraPos, 1f);
            float4x4 transformationMatrix = float4x4.identity;
            transformationMatrix.c0 = new float4(camRight.x, camUp.x, -camForward.x, 0);
            transformationMatrix.c1 = new float4(camRight.y, camUp.y, -camForward.y, 0);
            transformationMatrix.c2 = new float4(camRight.z, camUp.z, -camForward.z, 0);

            float4 transformedPoint = math.mul(transformationMatrix, translatedPoint);

            return transformedPoint;
        }
    }
    
    public struct MousePosition : IComponentData
    {
        public Vector3 Value;
    }

    [BurstCompile]
    public partial class MousePositionSystem : SystemBase
    {
        private Entity mouseEntity;
        private EntityManager entityManager;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerSingleton>();
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            mouseEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(mouseEntity, new MousePosition { Value = new Vector3(0f,0f,0f) });
            entityManager.SetName(mouseEntity, "Mouse Position");
        }

        protected override void OnUpdate() {
            if (!Camera.main) {
                return;
            }
            
            SystemAPI.SetComponent(mouseEntity, new MousePosition
            {
                Value = Camera.main.ScreenToWorldPoint(Input.mousePosition)
            });
        }
    }
}