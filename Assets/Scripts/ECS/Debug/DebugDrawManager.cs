namespace ECS {
    using UnityEngine;
    using System.Collections.Generic;

    public class DebugDrawManager : MonoBehaviour {
        private static DebugDrawManager _instance;
        private List<(Vector3, Vector3, float)> capsules = new List<(Vector3, Vector3, float)>();

        public static DebugDrawManager Instance {
            get {
                if (_instance == null) {
                    var obj = new GameObject("DebugDrawManager");
                    _instance = obj.AddComponent<DebugDrawManager>();
                }

                return _instance;
            }
        }

        public void AddCapsule(Vector3 point1, Vector3 point2, float radius) {
            capsules.Add((point1, point2, radius));
        }

        private void OnDrawGizmos() {
            Gizmos.color = Color.red;
            foreach (var capsule in capsules) {
                DrawCapsule(capsule.Item1, capsule.Item2, capsule.Item3);
            }

            capsules.Clear(); // Clear each frame to avoid lingering drawings
        }

        private void DrawCapsule(Vector3 start, Vector3 end, float radius) {
            Gizmos.DrawWireSphere(start, radius);
            Gizmos.DrawWireSphere(end, radius);
            Vector3 direction = (end - start).normalized;
            float offsetDistance = Vector3.Distance(start, end);
            for (float i = 0f; i <= offsetDistance; i += 0.1f) {
                Vector3 position = start + direction * i;
                Gizmos.DrawWireSphere(position, radius);
            }
        }
    }
}