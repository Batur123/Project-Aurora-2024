using System;
using ECS.Components;
using UnityEngine;

namespace ScriptableObjects {
    [CreateAssetMenu(fileName = "NewAttachmentTemplate", menuName = "Gun/AttachmentTemplate", order = 0)]
    public class AttachmentTemplate : ScriptableObject
    {
        public AttachmentType attachmentType;
        public string attachmentName;

        public float damageModifier;
        public float reloadSpeedModifier;
        public float accuracyModifier;
        public float recoilModifier;
        public int capacityModifier;
    }
    
    public struct AttachmentTypeKey : IEquatable<AttachmentTypeKey>
    {
        public AttachmentType Value;
        
        public bool Equals(AttachmentTypeKey other)
        {
            return Value == other.Value;
        }
        
        public override int GetHashCode()
        {
            return (int)Value; 
        }

        public override bool Equals(object obj)
        {
            return obj is AttachmentTypeKey other && Equals(other);
        }
    }
}