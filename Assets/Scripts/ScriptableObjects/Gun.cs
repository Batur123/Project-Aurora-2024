using System;
using ECS;
using ECS.Components;
using UnityEngine;

namespace ScriptableObjects {
    [CreateAssetMenu(fileName = "NewGunTemplate", menuName = "Gun/GunTemplate", order = 1)]
    public class GunTemplate : ScriptableObject
    {
        public GunType gunType;
        public string gunName;
        public ProjectileType projectileType;

        public int minAmmoCapacity = 10;
        public int maxAmmoCapacity = 10;
        
        public float minDamage = 5;
        public float maxDamage = 5;

        public float minAttackSpeed = 1;
        public float maxAttackSpeed = 1;

        public float minRecoilAmount = 1;
        public float maxRecoilAmount = 1;

        public float minSpreadAmount = 1;
        public float maxSpreadAmount = 1;

        public int minBulletsPerShot = 1;
        public int maxBulletsPerShot = 1;

        public float minReloadSpeed = 1;
        public float maxReloadSpeed = 1;
        
        public int minPiercingBulletsPerShot = 1;
        public int maxPiercingBulletsPerShot = 1;

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