using ScriptableObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Components {
    public struct DroppedItemTag : IComponentData {}
    public struct GunTag : IComponentData {}

    public struct AmmoComponent : IComponentData {
        public int currentAmmo;
        public int capacity;
        public bool isReloading;
    }

    public enum AttachmentType
    {
        Stock,
        Barrel,
        Magazine,
        Scope,
        Ammunition
    }
    
    public enum Rarity {
        Common, 
        Uncommon, 
        Rare, 
        Epic, 
        Legendary,
        Mythic
    }
    
    public enum GunType
    {
        Pistol,
        Shotgun,
        Rifle,
        GrenadeLauncher,
    }
    
    // Base Stats
    public struct BaseWeaponData : IComponentData {
        public int minAmmoCapacity;
        public int maxAmmoCapacity;
        public int ammoCapacity;
        
        public float damage;
        public float minDamage;
        public float maxDamage;
        
        public float attackSpeed;
        public float minAttackSpeed;
        public float maxAttackSpeed;
        
        public float recoilAmount;
        public float minRecoilAmount;
        public float maxRecoilAmount;
        
        public float spreadAmount;
        public float minSpreadAmount;
        public float maxSpreadAmount;
        
        public int bulletsPerShot;
        public int minBulletsPerShot;
        public int maxBulletsPerShot;
        
        public float reloadSpeed;
        public float minReloadSpeed;
        public float maxReloadSpeed;
        
        public int piercingBulletsPerShot;
        public int minPiercingBulletsPerShot;
        public int maxPiercingBulletsPerShot;
    }
    
    // Calculated Stats after base + upgrades
    public struct WeaponData : IComponentData {
        public FixedString64Bytes weaponName;
        public int ammoCapacity;
        public float damage;
        public float attackSpeed;
        public float recoilAmount;
        public float spreadAmount;
        public int bulletsPerShot;
        public float reloadSpeed;
        public int piercingBulletsPerShot;
    }

    // Attachment Stats that calculates
    public struct AttachmentComponent : IComponentData {
        public FixedString64Bytes attachmentName;
        public int ammoCapacity;
        public float damage;
        public float attackSpeed;
        public float recoilAmount;
        public float spreadAmount;
        public int bulletsPerShot;
        public float reloadSpeed;
        public int piercingBulletsPerShot;
    }
    
    // Blob data from initial scriptable object to put into BaseStats
    public struct GunTemplateBlob
    {
        public int minAmmoCapacity;
        public int maxAmmoCapacity;
        public float minDamage;
        public float maxDamage;
        public float minAttackSpeed;
        public float maxAttackSpeed;
        public float minRecoilAmount;
        public float maxRecoilAmount;
        public float minSpreadAmount;
        public float maxSpreadAmount;
        public int minBulletsPerShot;
        public int maxBulletsPerShot;
        public float minReloadSpeed;
        public float maxReloadSpeed;
        public int minPiercingBulletsPerShot;
        public int maxPiercingBulletsPerShot;
    }
    
    public struct AttachmentTemplateBlob
    {
        public float damageModifier;
        public float reloadSpeedModifier;
        public float accuracyModifier;
        public float recoilModifier;
        public int capacityModifier;
    }
    
    public struct MuzzlePointTransform : IComponentData
    {
        public float3 position;   // Position of the muzzle point
        public quaternion rotation; // Rotation of the muzzle point
        public float3 scale;      // Scale of the muzzle point
        public float3 offset;
        public float3 boundOffset;
    }

    public struct ScopePointTransform : IComponentData
    {
        public float3 position;   // Position of the scope point
        public quaternion rotation; // Rotation of the scope point
        public float3 scale;      // Scale of the scope point
        public float3 offset;
        public float3 boundOffset;
    }

    public struct GunBlobReference : IComponentData
    {
        public BlobAssetReference<GunTemplateBlob> templateBlob;
    }

    public struct AttachmentBlobReference : IComponentData
    {
        public BlobAssetReference<AttachmentTemplateBlob> templateBlob;
    }

    public struct AttachmentTag : IComponentData {}

    public struct AttachmentTypeComponent : IComponentData {
        public AttachmentType attachmentType;
        public int variantId;
        public float lootWeight;
    }

    public struct GunTypeComponent : IComponentData
    {
        public GunType gunType;
        public int variantId;
        public float lootWeight;
    }
    
    public struct WeaponProjectileTypeComponent : IComponentData {
        public ProjectileType projectileType;
    }
    
    public struct GrenadeComponent : IComponentData
    {
        public float3 StartPosition; // Where the grenade starts
        public float3 TargetPosition; // Where the grenade should land
        public float PeakHeight;
        public float ThrowTime; // Total time for the throw
        public float ElapsedTime; // Time elapsed since the throw began
        public float FuseDuration;
        public float ExplosionRadius;
        public float3 RandomizedTarget;
    }

    public struct ExplosionTag : IComponentData {
        public float lifeTime;
        public float elapsedExplosionTime;
    }

    public struct StartFuseCountdown : IComponentData {}

    public struct MuzzlePoint : IComponentData {
        public float3 position;  // Position of the muzzle point relative to the weapon
    }
    
    public struct BuiltPrefab : IComponentData {
        public Entity prefab;
    }
    
    public struct GunAttachment : IBufferElementData
    {
        public Entity AttachmentEntity;
    }

    public struct AttachmentPrefab : IComponentData {
        public Entity prefab;
    }
    
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
        public int totalEnemy;
    }
    
    public struct PlayerTag : IComponentData {
    }

    public struct EnemyTag : IComponentData, IEnableableComponent  {
    }
    public struct DisabledEnemyTag : IComponentData {}
    public struct DisabledProjectileTag : IComponentData {}

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
        public Entity healthBarPrefab;
    }

    public struct ParticleData : IComponentData {
        public Entity rainPrefab;
        public Entity bulletHitExplosionPrefab;
    }

    public class EnemyMaterial : IComponentData {
        public Material material;
        public Mesh mesh;
    }

    public struct EnemyData : IComponentData {
        public EnemyType enemyType;
        public float health;
        public float maxHealth;
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
        public int killCount;
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
        EXPLOSIVE_GRENADE,
        EXPLOSIVE_BULLET,
        POISON_BULLET,
        ELECTRIC_BULLET,
    }
    
    public struct ProjectileDataComponent : IComponentData {
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

    /*
     * TODO Add Only UI Update Tag reduce overhead and size of components for archetypes
     *  public struct UpdateUserInterfaceTag : IComponentData {}
     */
    public struct UIUpdateFlag : IComponentData {
        public bool needsUpdate;
    }
    
    public struct InventoryOpen : IComponentData {}

    public enum ItemType {
        NONE,
        MATERIAL,
        WEAPON,
        ATTACHMENT
    }
    
    public struct Item : IComponentData {
        public int slot;
        public ItemType itemType;
        public bool isEquipped;
        public int quantity;
        public bool isStackable;
        public bool onGround;
    }
    
    public struct Inventory : IBufferElementData {
        public Entity itemEntity;
    }
    
    public struct DisableSpriteRendererRequest : IComponentData { }
    public struct EnableSpriteRendererRequest : IComponentData { }
    public struct PickupRequest : IComponentData { }
}