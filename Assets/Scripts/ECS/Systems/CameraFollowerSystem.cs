using ECS.Components;
using Unity.Burst;
using Unity.Cinemachine;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems {
    [BurstCompile]
    public partial class PlayerCameraSystem : SystemBase {
        private CinemachineCamera _cinemachineCamera;
        private GameObject _proxyGameObject;

        protected override void OnCreate() {
            RequireForUpdate<PlayerSingleton>();
        }


        protected override void OnUpdate() {
            if (_cinemachineCamera == null) {
                _cinemachineCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CinemachineCamera>();
            }

            if (_proxyGameObject == null) {
                _proxyGameObject = new GameObject("PlayerProxy");
            }

            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var localTransform = SystemAPI.GetComponent<LocalTransform>(playerSingleton.PlayerEntity);
            _proxyGameObject.transform.position = localTransform.Position.xyz;
            _proxyGameObject.transform.rotation = localTransform.Rotation;
            _cinemachineCamera.Follow = _proxyGameObject.transform;
        }
    }
}