using System;
using ECS.Components;
using UnityEngine;

namespace ScriptableObjects {
    [CreateAssetMenu(fileName = "NewPassiveItemTemplate", menuName = "Item/PassiveItemTemplate", order = 2)]
    public class PassiveItemTemplate : ScriptableObject
    {
        public string passiveItemName;
        public PassiveItemType passiveItemType;
        public ProjectileType projectileType;
        public StatsData statsData;
        public CharacterStatsData characterStatsData;
    }
    
    // A wrapper struct around the enum:
    public struct PassiveItemTypeKey : IEquatable<PassiveItemTypeKey>
    {
        public PassiveItemType Value;

        // Compare equality by the enum
        public bool Equals(PassiveItemTypeKey other)
        {
            return Value == other.Value;
        }

        // Required override for proper hashing
        public override int GetHashCode()
        {
            return (int)Value; 
        }

        // Also override object.Equals if you like
        public override bool Equals(object obj)
        {
            return obj is PassiveItemTypeKey other && Equals(other);
        }
    }
}