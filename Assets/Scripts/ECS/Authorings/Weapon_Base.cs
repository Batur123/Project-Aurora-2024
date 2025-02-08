using ECS.Components;
using ScriptableObjects;
using UnityEngine;

namespace ECS.Authorings.BaseGuns {
    public class WeaponAuthoring : MonoBehaviour {
        public GunType weaponType; // Base Gun type - Pistol Shotgun without variants
        public int variantId;      // E.g., 0 = Pump Shotgun, 1 = Automatic Shotgun
        public float lootWeight;
        public GameObject prefab;
        public GunTemplate gunTemplate;
    }
}