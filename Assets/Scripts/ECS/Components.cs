using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS {
    public enum EnemyType {
        BASIC_ZOMBIE,
        RUNNER_ZOMBIE,
        TANK_ZOMBIE,
    }

    public enum CollisionBelongsToLayer {
        None = 0,
        Player = 1 << 0,
        Enemy = 1 << 1,
        Projectile = 1 << 2,
        Wall = 1 << 3,
        GunEntity = 1 << 4,
        AttachmentEntity = 1 << 5,
        Explosion = 1 << 6,
    }

    public struct WaveManager : IComponentData {
        public int currentWave;
        public bool isActive;
        public float waveTimer;
    }
    
    public struct PlayerTag : IComponentData {
    }

    public struct EnemyTag : IComponentData {
    }

    public struct BossTag : IComponentData {
    }

    public struct NPCTag : IComponentData {
    }

    public struct ItemTag : IComponentData {
        
    }
    public struct ProjectileTag : IComponentData {
    }

    public struct IsSpawned : IComponentData {
    }

    public struct ItemSpawner : IComponentData {
        
    }
    public struct SpawnerTime : IComponentData {
        public float nextSpawnTime;
    }

    // Enemy-spawner specific data
    public struct EntityData : IComponentData {
        public Entity prefab;
        public Entity grenadePrefab;
        public Entity grenadeExplosionPrefab;
    }

    public struct EnemyData : IComponentData {
        public EnemyType enemyType;
        public float health;
        public float damage;
        public float attackSpeed;
        public float meleeAttackRange;
    }

    public struct AttackTimer : IComponentData {
        public float TimeElapsed;  // Tracks time since last attack
    }

    public struct CharacterStats : IComponentData {
        public float health;
        public float maxHealth; // starts 10
        public float stamina; 
        public float maxStamina; // starts 100
        public float armor; // starts 0
        public float maxArmor; // starts 0
        public float criticalHitChance; // every 1 point chance %5 ?
        public float criticalDamage; // every 1 point damage %25
        public float luck; // every 1 point luck %6.66
        public float sanity; // every 1 point sanity %3.14
        public float lifeSteal; // every 1 point life steal %1
        public float dodge; // every 1 point dodge %2
        public float healthRegeneration; // 1 point hp regen 0.5/s
        public float armorRegeneration; // 1 point armor regen 0.2/s
    }
    
    // Player-specific data
    public struct PlayerData : IComponentData {
        public int experience;
        public int level;
    }

    public struct EquippedGun : IBufferElementData
    {
        public Entity GunEntity;
    }

    public struct PlayerInputComponent : IComponentData {
        public float speed;
        public float2 direction;
    }

    public struct ProjectileShootingData : IComponentData {
        public float nextShootingTime;
    }

    public struct Projectile : IComponentData {
        public Entity projectilePrefab;
    }

    public struct ProjectileSpawner : IComponentData {
    }

    public struct ProjectileComponent : IComponentData {
        public float Lifetime;
        public float3 Velocity;
        public float BaseDamage;
        public float Speed;
    }

    public enum ProjectileType {
        NONE,
        BULLET,
        GRENADE,
        EXPLOSIVE_BULLET,
        POISON_BULLET,
        ELECTRIC_BULLET,
    }
    
    public struct ProjectileDataComponent : IComponentData {
        public ProjectileType projectileType;
        public int piercingEnemyNumber;
    }

    public struct PlayerSingleton : IComponentData {
        public Entity PlayerEntity;
    }

    public struct ReloadingTag : IComponentData { }

    public struct ReloadTimer : IComponentData {
        public float timeRemaining;
    }
    
    public struct AnimationParameters : IComponentData
    {
        public float Speed;
        public int Side;
        public bool HoldItem;
    }

    public struct UIUpdateFlag : IComponentData {
        public bool needsUpdate;
    }
    
    public struct InventoryOpen : IComponentData {}
    
    public struct CollisionTimer : IComponentData
    {
        public float TimeElapsed; // Tracks how long the collision has lasted
    }

    public struct Item : IComponentData {
        public int slot;
        public bool isEquipped;
        public int quantity;
        public bool isStackable;
        public bool onGround;
    }
    
    public struct Inventory : IBufferElementData {
        public Entity itemEntity;
    }
}