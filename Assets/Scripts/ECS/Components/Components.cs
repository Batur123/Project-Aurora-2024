using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Components {
    /** Tags **/
    public struct AttachmentDropProcessTag : IComponentData {}
    public struct EnemyAnimation : IComponentData{}
    public struct PlayerAnimation : IComponentData{}
    public struct DroppedItemTag : IComponentData {}
    public struct GunTag : IComponentData {}
    public struct UpdateUserInterfaceTag : IComponentData {}
    public struct DisableSpriteRendererRequest : IComponentData {}
    public struct EnableSpriteRendererRequest : IComponentData {}
    public struct PickupRequest : IComponentData {}
    public struct InventoryOpen : IComponentData {}
    public struct AttachmentTag : IComponentData {}
    public struct StartFuseCountdown : IComponentData {}
    public struct PlayerTag : IComponentData {}
    public struct EnemyTag : IComponentData, IEnableableComponent {}
    public struct DisabledEnemyTag : IComponentData {}
    public struct DisabledProjectileTag : IComponentData {}
    public struct BossTag : IComponentData {}
    public struct NPCTag : IComponentData {}
    public struct ItemTag : IComponentData {}
    public struct ProjectileTag : IComponentData {}
    public struct IsSpawned : IComponentData {}
    public struct ItemSpawner : IComponentData {}
    public struct ProjectileSpawner : IComponentData {}
    public struct ReloadingTag : IComponentData {}
    public struct PassiveItemTag : IComponentData {}
    /** Tags **/


    /** Special Attributes **/
    public enum SpecialAttributes : byte {
        None,
        Overclock_Module,
        Cluster_Grenade_Module,
        Lifesteal_Module,
    }
    
    // Fire rate increases by 10% for each consecutive shot, resets on reload.
    public struct OverclockModuleAttribute : IComponentData {}

    /** Special Attributes **/

    /** [Passive Items] **/
    public struct PassiveItem : IComponentData {
        public FixedString64Bytes itemName;
        public int amount;
    }

    public enum PassiveItemType {
        NONE,
        BANDAGE,
        MEDKIT,
        NANO_BOT_RELOADER,
        DOUBLE_MAGAZINE,
        KEVLAR_VEST,
    }
    
    public struct PassiveItemTypeComponent : IComponentData {
        public PassiveItemType passiveItemType;
        public FixedString64Bytes itemName;
        public float lootWeight;
    }

    public struct PassiveItemTemplateBlob {
        public StatsData statsData;
        public CharacterStatsData characterData;
    }
    
    public struct PassiveItemBlobReference : IComponentData {
        public BlobAssetReference<PassiveItemTemplateBlob> templateBlob;
    }
    /** [Passive Items] **/
    
    public struct AmmoComponent : IComponentData {
        public int currentAmmo;
        public int capacity;
        public bool isReloading;
    }

    public enum AttachmentType {
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

    public enum GunType {
        Pistol,
        Shotgun,
        Rifle,
        GrenadeLauncher,
    }

    [Serializable]
    public struct StatsData
    {
        public int ammoCapacity;
        public float damage;
        public float attackSpeed;
        public float recoilAmount;
        public float spreadAmount;
        public int bulletsPerShot;
        public float reloadSpeed;
        public int piercingBulletsPerShot;
    }
    
    [Serializable]
    public struct StatsRangeData
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
    
    // Base Stats
    public struct BaseWeaponData : IComponentData {
        public StatsRangeData range;
        public StatsData stats;
    }
    
    // Calculated Stats after base + upgrades
    public struct WeaponData : IComponentData {
        public FixedString64Bytes weaponName;
        public StatsData stats;
    }

    // Base Attachment Stats
    public struct BaseAttachmentData : IComponentData {
        public StatsRangeData range;
        public StatsData stats;
    }
    
    // Attachment Stats that calculates
    public struct AttachmentData : IComponentData {
        public FixedString64Bytes attachmentName;
        public StatsData stats;
    }

    // Blob data from initial scriptable object to put into BaseStats
    public struct GunTemplateBlob {
        public StatsRangeData statsRangeData;
    }

    public struct AttachmentTemplateBlob {
        public StatsRangeData statsRangeData;
    }

    public struct MuzzlePointTransform : IComponentData {
        public float3 position; // Position of the muzzle point
        public quaternion rotation; // Rotation of the muzzle point
        public float3 scale; // Scale of the muzzle point
        public float3 offset;
        public float3 boundOffset;
    }

    public struct ScopePointTransform : IComponentData {
        public float3 position; // Position of the scope point
        public quaternion rotation; // Rotation of the scope point
        public float3 scale; // Scale of the scope point
        public float3 offset;
        public float3 boundOffset;
    }

    public struct GunBlobReference : IComponentData {
        public BlobAssetReference<GunTemplateBlob> templateBlob;
    }

    public struct AttachmentBlobReference : IComponentData {
        public BlobAssetReference<AttachmentTemplateBlob> templateBlob;
    }
    
    public struct AttachmentTypeComponent : IComponentData {
        public AttachmentType attachmentType;
        public int variantId;
        public float lootWeight;
    }

    public struct GunTypeComponent : IComponentData {
        public GunType gunType;
        public int variantId;
        public float lootWeight;
    }

    public struct WeaponProjectileTypeComponent : IComponentData {
        public ProjectileType projectileType;
    }

    public struct GrenadeComponent : IComponentData {
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

    public struct MuzzlePoint : IComponentData {
        public float3 position; // Position of the muzzle point relative to the weapon
    }

    public struct BuiltPrefab : IComponentData {
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
        public float TimeElapsed; // Tracks time since last attack
    }
    
    [Serializable]
    public struct CharacterStatsData
    {
        public float health;
        public float maxHealth;
        public float stamina;
        public float maxStamina;
        public float armor;
        public float maxArmor;
        public float criticalHitChance;
        public float criticalDamage;
        public float luck;
        public float sanity;
        public float lifeSteal;
        public float dodge;
        public float healthRegeneration;
        public float armorRegeneration;
    }


    public struct CharacterStats : IComponentData {
        public CharacterStatsData characterStats;
    }

    // Player-specific data
    public struct PlayerData : IComponentData {
        public int experience;
        public int level;
        public int killCount;
        public int pendingLevelUps;
    }

    public struct EquippedGun : IBufferElementData {
        public Entity GunEntity;
    }

    public struct ProjectileShootingData : IComponentData {
        public float nextShootingTime;
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

    public struct Projectile : IComponentData {
        public Entity projectilePrefab;
    }

    public struct ReloadTimer : IComponentData {
        public float timeRemaining;
    }

    public enum ItemType {
        NONE,
        MATERIAL,
        WEAPON,
        ATTACHMENT,
        PASSIVE_ITEM
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
    
    public enum ParticleType {
        None,
        Rain,
        Bullet_Hit,
    }

    public struct RainFollowerParticle : IComponentData {
    }

    public struct ParticleTypeComponent : IComponentData {
        public ParticleType particleType;
    }

    public struct ParticleSpawnerRequestTag : IComponentData {
        public ParticleType particleType;
        public Vector3 spawnPosition;
        public float particleLifeTime;
    }

    public struct BulletHitParticleData : IComponentData {
        public float lifeTime;
    }

    public struct ScaleChildParticlesTag : IComponentData {
        public float scale;
    }
    
    public enum AnimationType : byte
    {
        Idle,
        Walk
    }

    public struct AnimatorState : IComponentData
    {
        public AnimationType CurrentAnimation;
        public int CurrentFrame;
        public float Timer;
    }

    public class SpriteAnimationClips : IComponentData
    {
        public Sprite[] IdleSprites;
        public float IdleFrameDuration;

        public Sprite[] WalkSprites;
        public float WalkFrameDuration;
    }
    
    public struct MousePosition : IComponentData
    {
        public Vector3 Value;
    }
    
    public struct RemoveAttachmentRequest : IComponentData {
        public Entity gunEntity;
        public Entity attachmentEntity;
    }

    public struct SpawnGunRequest : IComponentData {
        public GunType gunType;
        public int variantId;
        public float3 position;
        public float scale;
    }

    public struct SpawnPassiveItemRequest : IComponentData {
        public PassiveItemType passiveItemType;
        public int variantId;
        public float3 position;
        public float scale;
    }
    
    public struct SpawnAttachmentRequest : IComponentData {
        public AttachmentType attachmentType;
        public int variantId;
        public float3 position;
    }
}