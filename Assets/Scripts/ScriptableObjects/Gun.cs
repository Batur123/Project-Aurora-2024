using System;
using ECS;
using UnityEngine;

namespace ScriptableObjects {
    [CreateAssetMenu(fileName = "NewGunTemplate", menuName = "Gun/GunTemplate", order = 1)]
    public class GunTemplate : ScriptableObject
    {
        public GunType gunType;
        public string gunName;
        public ProjectileType projectileType;
        public int ammoCapacity = 10;
        public float damage = 5;
        public float accuracy = 1;
        public float attackSpeed = 1;
        public float recoilAmount = 1;
        public float spreadAmount = 1;
        public int bulletsPerShot = 1;
        public float reloadSpeed = 1;
        public int piercingBulletsPerShot = 1;
    }

    public enum GunType
    {
        Pistol,
        Shotgun,
        Rifle,
        GrenadeLauncher,
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