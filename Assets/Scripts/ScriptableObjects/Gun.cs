using System;
using ECS;
using ECS.Components;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

namespace ScriptableObjects {
    [CreateAssetMenu(fileName = "NewGunTemplate", menuName = "Item/GunTemplate", order = 1)]
    public class GunTemplate : ScriptableObject
    {
        public string gunName;
        public GunType gunType;
        public ProjectileType projectileType;
        public StatsRangeData statsRangeData;

    }
    
    // A wrapper struct around the enum:
    public struct GunTypeKey : IEquatable<GunTypeKey>
    {
        public GunType Value;

        // Compare equality by the enum
        public bool Equals(GunTypeKey other)
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
            return obj is GunTypeKey other && Equals(other);
        }
    }
}