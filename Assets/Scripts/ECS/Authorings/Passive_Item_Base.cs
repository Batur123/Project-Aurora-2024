using ECS.Components;
using ScriptableObjects;
using UnityEngine;

namespace ECS.Authorings {
    public class PassiveItemAuthoring : MonoBehaviour {
        public PassiveItemType passiveItemType;
        public int variantId;
        public float lootWeight;
        public GameObject prefab;
        public PassiveItemTemplate passiveItemTemplate;
    }
}