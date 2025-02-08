using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ECS {
    public class LightFollower : MonoBehaviour {
        public Light2D light;
        public CinemachineCamera gameCamera;

        void Update() {
            light.transform.position = new Vector3(gameCamera.transform.position.x, gameCamera.transform.position.y, light.transform.position.z);
        }
    }
}