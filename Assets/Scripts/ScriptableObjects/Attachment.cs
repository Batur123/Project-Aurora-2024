using System;
using ECS.Components;
using UnityEngine;

namespace ScriptableObjects {
    [CreateAssetMenu(fileName = "NewAttachmentTemplate", menuName = "Item/AttachmentTemplate", order = 0)]
    public class AttachmentTemplate : ScriptableObject
    {
        public string attachmentName;
        public AttachmentType attachmentType;
        public StatsRangeData statsRangeData;
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