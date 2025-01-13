using System;
using UnityEngine;

namespace ScriptableObjects {
    [CreateAssetMenu(fileName = "NewGunTemplate", menuName = "Gun/GunTemplate", order = 1)]
    public class GunTemplate : ScriptableObject
    {
        public string gunName;
        public int ammoCapacity = 10;
        public int durability = 100;
        public float damage = 1f;
        public float reloadTime = 1f;
        public GunType gunType;
        public float attackRate = 1f; // Attacks per second
        public float recoilAmount = 1f; // Recoil intensity - Pushes player back!
        public float spreadAmount = 2f; // Spread of bullets - 2 DEGREE
        public float lastAttackTime = 0f; // Time of last attack
        public int bulletsPerShot = 1;
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