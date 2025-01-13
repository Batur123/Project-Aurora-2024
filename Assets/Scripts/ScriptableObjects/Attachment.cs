using System;
using UnityEngine;

namespace ScriptableObjects {
    [CreateAssetMenu(fileName = "NewAttachment", menuName = "Gun/Attachment", order = 0)]
    public class AttachmentTemplate : ScriptableObject
    {
        public AttachmentType attachmentType;
        public Sprite attachmentSprite;
        public string attachmentName;
        public float damageModifier;   // Example for barrel or ammunition
        public float reloadSpeedModifier; // Example for magazinew
        public float accuracyModifier; // Example for stock or barrel
        public float recoilModifier;  // Example for stock
        public int capacityModifier;  // Example for magazine
    }
    
    public enum AttachmentType
    {
        Stock,
        Barrel,
        Magazine,
        Scope,
        Ammunition
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