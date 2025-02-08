using ECS.Components;
using ScriptableObjects;
using UnityEngine;

namespace ECS.Authorings.BaseAttachments {
    public class AttachmentAuthoring : MonoBehaviour {
        public AttachmentType attachmentType; // E.g., Barrel, Scope, etc.
        public int variantId;                 // E.g., 0 = Suppressor, 1 = Compensator, etc.
        public float lootWeight;
        public GameObject prefab;             // Prefab for this attachment
        public AttachmentTemplate attachmentTemplate; // Base stats for the attachment
    }
}