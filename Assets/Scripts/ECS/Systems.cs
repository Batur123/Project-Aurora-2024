using System.Threading;
using ECS.Components;
using ECS.Systems;
using ECS.Systems.Projectiles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace ECS {
    public static class LevelUtils {
        public static int CalculateRequiredExperience(int level) {
            if (level <= 2) {
                return 5;
            }

            int totalRequiredExperience = 5;
            for (int i = 3; i <= level; i++) {
                totalRequiredExperience += 5 + (i - 2) * 5;
            }

            return totalRequiredExperience;
        }

        public static int CalculateLevelByExperience(int experience) {
            int level = 1;
            while (CalculateRequiredExperience(level + 1) <= experience) {
                level++;
            }

            return level;
        }
    }

    [BurstCompile]
    [UpdateAfter(typeof(PlayerSpawnerSystem))]
    public partial struct PlayerStatsSystem : ISystem {
        private Entity _cachedPlayerEntity;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            if (!state.EntityManager.Exists(_cachedPlayerEntity)) {
                var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
                _cachedPlayerEntity = playerSingleton.PlayerEntity;
            }

            PlayerData playerData = SystemAPI.GetComponent<PlayerData>(_cachedPlayerEntity);
            CharacterStats stats = SystemAPI.GetComponent<CharacterStats>(_cachedPlayerEntity);
            DynamicBuffer<EquippedGun> equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(_cachedPlayerEntity);

            UIController.Instance.UpdateHealthBar(stats.characterStats.health, stats.characterStats.maxHealth);
            UIController.Instance.SetTextValue(UIController.TextType.SCOREBOARD_TEXT,
                $"Level: {LevelUtils.CalculateLevelByExperience(playerData.experience)} Exp: {playerData.experience} Kill: {playerData.killCount}");

            if (equippedGunBuffer.IsEmpty) {
                UIController.Instance.SetTextValue(UIController.TextType.AMMO_TEXT, $"");
                return;
            }

            AmmoComponent ammoComponent = SystemAPI.GetComponent<AmmoComponent>(equippedGunBuffer[0].GunEntity);
            GunTypeComponent gunTypeComponent = SystemAPI.GetComponent<GunTypeComponent>(equippedGunBuffer[0].GunEntity);

            var text = SystemAPI.HasComponent<ReloadingTag>(_cachedPlayerEntity)
                ? $"{gunTypeComponent.gunType.ToString()} (Reloading)"
                : $"{gunTypeComponent.gunType.ToString()} {ammoComponent.currentAmmo}/{ammoComponent.capacity}";

            UIController.Instance.SetTextValue(UIController.TextType.AMMO_TEXT, text);
        }
    }

    [BurstCompile]
    public partial struct PlayerInputSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
            state.RequireForUpdate<WaveManager>();
        }

        public void OnUpdate(ref SystemState state) {
            var input = Keyboard.current;

            if (input.eKey.isPressed) {
                WaveManager waveManager = SystemAPI.GetSingleton<WaveManager>();
                if (waveManager.isActive) {
                    return;
                }

                waveManager.waveTimer = 100f;
                waveManager.currentWave++;
                waveManager.isActive = true;
                SystemAPI.SetSingleton(waveManager);
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct PlayerMovementSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
            state.RequireForUpdate<WaveManager>();
        }

        public void OnUpdate(ref SystemState state) {
            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            if (state.EntityManager.HasComponent<InventoryOpen>(playerSingleton.PlayerEntity)) {
                return;
            }

            var input = Keyboard.current;
            float2 moveDirection = float2.zero;

            if (input.wKey.isPressed) moveDirection.y += 1f;
            if (input.sKey.isPressed) moveDirection.y -= 1f;
            if (input.aKey.isPressed) moveDirection.x -= 1f;
            if (input.dKey.isPressed) moveDirection.x += 1f;

            if (math.lengthsq(moveDirection) > 0) {
                moveDirection = math.normalize(moveDirection);
            }

            if (SystemAPI.TryGetSingletonRW(out RefRW<PlayerSingleton> singletonRW)) {
                Entity playerEntity = singletonRW.ValueRW.PlayerEntity;
                RefRW<LocalTransform> transformRW = SystemAPI.GetComponentRW<LocalTransform>(playerEntity);
                transformRW.ValueRW.Position += new float3(moveDirection * 1.3f * SystemAPI.Time.DeltaTime, 0f);

                if (state.EntityManager.HasComponent<SpriteRenderer>(playerEntity)) {
                    SpriteRenderer spriteRenderer = state.EntityManager.GetComponentObject<SpriteRenderer>(playerEntity);
                    spriteRenderer.flipX = moveDirection.x < 0;
                }
            }
        }
    }
    
    [BurstCompile]
    public partial struct ReloadWeapon : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<ReloadingTag>();
            state.RequireForUpdate<PlayerSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            DynamicBuffer<EquippedGun> equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            if (equippedGunBuffer.IsEmpty) {
                return;
            }

            Entity gunEntity = equippedGunBuffer[0].GunEntity;
            AmmoComponent ammoComponent = SystemAPI.GetComponent<AmmoComponent>(gunEntity);
            ReloadTimer reloadTimer = SystemAPI.GetComponent<ReloadTimer>(playerSingleton.PlayerEntity);

            if (!ammoComponent.isReloading) {
                return;
            }

            reloadTimer.timeRemaining -= SystemAPI.Time.DeltaTime;
            SystemAPI.SetComponent(playerSingleton.PlayerEntity, reloadTimer);

            if (reloadTimer.timeRemaining <= 0) {
                ammoComponent.currentAmmo = ammoComponent.capacity;
                ammoComponent.isReloading = false;
                SystemAPI.SetComponent(gunEntity, ammoComponent);
                state.EntityManager.RemoveComponent<ReloadingTag>(playerSingleton.PlayerEntity);
            }


        }
    }

    [UpdateAfter(typeof(PlayerSpawnerSystem))]
    [BurstCompile]
    public partial struct PlayerShootingSystem : ISystem {

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<MousePosition>();
            state.RequireForUpdate<ProjectileSpawner>();
            state.RequireForUpdate<EntityData>();
            state.RequireForUpdate<WaveManager>();
        }

        [BurstCompile]
        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (SystemAPI.TryGetSingletonRW(out RefRW<PlayerSingleton> singletonRW)) {
                Entity playerEntity = singletonRW.ValueRW.PlayerEntity;
                var equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerEntity);
                RefRW<ProjectileShootingData> projectileShootingData = SystemAPI.GetComponentRW<ProjectileShootingData>(playerEntity);
                RefRO<LocalTransform> transform = SystemAPI.GetComponentRO<LocalTransform>(playerEntity);

                projectileShootingData.ValueRW.nextShootingTime -= SystemAPI.Time.DeltaTime;
                if (projectileShootingData.ValueRO.nextShootingTime < 0) {
                    projectileShootingData.ValueRW.nextShootingTime = 0;
                }

                if (equippedGunBuffer.IsEmpty) {
                    return;
                }

                if (state.EntityManager.HasComponent<InventoryOpen>(playerEntity)) {
                    return;
                }

                AmmoComponent ammoComponent = SystemAPI.GetComponent<AmmoComponent>(equippedGunBuffer[0].GunEntity);
                bool isReloading = ammoComponent.isReloading;

                if (Input.GetKeyDown(KeyCode.R) && !isReloading && ammoComponent.currentAmmo < ammoComponent.capacity) {
                    StartReload(ref state, playerEntity, equippedGunBuffer[0].GunEntity);
                    return;
                }

                if (isReloading || ammoComponent.currentAmmo <= 0) {
                    return;
                }

                if (!Input.GetMouseButton(0)) {
                    return;
                }

                var mousePositionEntity = SystemAPI.GetSingleton<MousePosition>();
                Vector2 shootDirection =
                    (mousePositionEntity.Value - new Vector3(transform.ValueRO.Position.x, transform.ValueRO.Position.y, transform.ValueRO.Position.z)).normalized;

                WeaponData weaponData = SystemAPI.GetComponent<WeaponData>(equippedGunBuffer[0].GunEntity);
                if (projectileShootingData.ValueRO.nextShootingTime > 0) {
                    return;
                }

                shootDirection = ApplyRecoil(shootDirection, weaponData.stats.recoilAmount);

                MuzzlePointTransform muzzlePointTransform = state.EntityManager.GetComponentData<MuzzlePointTransform>(equippedGunBuffer[0].GunEntity);
                LocalTransform weaponLocalTransform = state.EntityManager.GetComponentData<LocalTransform>(equippedGunBuffer[0].GunEntity);
                GunTypeComponent gunTypeComponent = state.EntityManager.GetComponentData<GunTypeComponent>(equippedGunBuffer[0].GunEntity);
                WeaponProjectileTypeComponent weaponProjectileTypeComponent =
                    state.EntityManager.GetComponentData<WeaponProjectileTypeComponent>(equippedGunBuffer[0].GunEntity);

                float3 gunWorldPosition = weaponLocalTransform.Position;
                quaternion gunWorldRotation = weaponLocalTransform.Rotation;
                float3 muzzleLocalPosition = muzzlePointTransform.position;
                float3 transformedMuzzlePosition = math.mul(gunWorldRotation, muzzleLocalPosition);
                float3 muzzleWorldPosition = gunWorldPosition + transformedMuzzlePosition;

                EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

                Vector2 originalDirection = shootDirection; // store once outside the loop

                for (int i = 0; i < weaponData.stats.bulletsPerShot; i++) {
                    Vector2 randomDir = ApplySpread(originalDirection, weaponData.stats.spreadAmount);
                    float3 randomizedTarget = new float3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);

                    new SpawnProjectileJob {
                        ecb = ecb,
                        position = muzzleWorldPosition,
                        shootDirection = math.normalize(randomDir),
                        gunType = gunTypeComponent.gunType,
                        weaponProjectileType = weaponProjectileTypeComponent.projectileType,
                        weaponData = weaponData,
                        mousePosition = mousePositionEntity.Value,
                        randomPeak = Random.Range(0.3f, 1.5f),
                        randomizedTarget = randomizedTarget,
                    }.ScheduleParallel();
                }

                projectileShootingData.ValueRW.nextShootingTime = 1 / weaponData.stats.reloadSpeed;
                SystemAPI.SetComponent(equippedGunBuffer[0].GunEntity, new AmmoComponent {
                    currentAmmo = ammoComponent.currentAmmo - 1,
                    capacity = ammoComponent.capacity
                });
            }
        }

        private void StartReload(ref SystemState state, Entity playerEntity, Entity gunEntity) {
            AmmoComponent ammoComponent = SystemAPI.GetComponent<AmmoComponent>(gunEntity);

            SystemAPI.SetComponent(gunEntity, new AmmoComponent {
                currentAmmo = ammoComponent.currentAmmo,
                capacity = ammoComponent.capacity,
                isReloading = true
            });

            state.EntityManager.AddComponent<ReloadingTag>(playerEntity);
            var reloadTimer = SystemAPI.GetComponentRW<ReloadTimer>(playerEntity);
            reloadTimer.ValueRW.timeRemaining = 2.0f;
        }

        private Vector2 ApplyRecoil(Vector2 shootDirection, float recoilAmount) {
            var recoilAngle = Random.Range(-recoilAmount, recoilAmount);
            return RotateVector2(shootDirection, recoilAngle);
        }

        private Vector2 ApplySpread(Vector2 shootDirection, float spreadAmount) {
            var spreadAngle = Random.Range(-spreadAmount, spreadAmount);
            return RotateVector2(shootDirection, spreadAngle);
        }

        private Vector2 RotateVector2(Vector2 vector, float angleInDegrees) {
            var angleInRadians = math.radians(angleInDegrees);
            var cosAngle = math.cos(angleInRadians);
            var sinAngle = math.sin(angleInRadians);
            var x = vector.x * cosAngle - vector.y * sinAngle;
            var y = vector.x * sinAngle + vector.y * cosAngle;
            return new Vector2(x, y);
        }
    }

    [BurstCompile]
    public partial struct EnemyMeleeAttackSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var playerLocalTransform = SystemAPI.GetComponentRO<LocalTransform>(playerSingleton.PlayerEntity);
            var characterStats = SystemAPI.GetComponentRW<CharacterStats>(playerSingleton.PlayerEntity);

            foreach (var (enemyData, enemyTransform, attackTimer)
                     in SystemAPI.Query<RefRO<EnemyData>, RefRO<LocalTransform>, RefRW<AttackTimer>>()) {
                attackTimer.ValueRW.TimeElapsed += SystemAPI.Time.DeltaTime;

                var distance = Vector3.Distance(enemyTransform.ValueRO.Position, playerLocalTransform.ValueRO.Position);
                if (distance <= enemyData.ValueRO.meleeAttackRange && attackTimer.ValueRW.TimeElapsed >= enemyData.ValueRO.attackSpeed) {
                    characterStats.ValueRW.characterStats.health -= enemyData.ValueRO.damage;
                    attackTimer.ValueRW.TimeElapsed = 0f;
                }

            }
        }
    }

    [BurstCompile]
    public partial struct DisableEnemiesOnDeathSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            new DisableEnemiesJob {
                ECB = ecbParallel
            }.ScheduleParallel();

            state.Dependency.Complete(); // Ensure completion before playback
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public partial struct DisableEnemiesJob : IJobEntity {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(Entity entity, [EntityIndexInQuery] int index, in DisabledEnemyTag tag) {
                ECB.AddComponent<Disabled>(index, entity);
            }
        }
    }

    public struct GamePausedTag : IComponentData {}
    
    [BurstCompile]
    public partial struct UpdatePlayerStats : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var playerData = SystemAPI.GetComponentRW<PlayerData>(playerSingleton.PlayerEntity);

            var killCountRef = new NativeReference<int>(Allocator.TempJob);
            var experienceRef = new NativeReference<int>(Allocator.TempJob);

            new UpdatePlayerStatsJob {
                ECB = ecbParallel,
                KillCount = killCountRef,
                Experience = experienceRef
            }.Schedule();
            state.Dependency.Complete();

            Interlocked.Add(ref playerData.ValueRW.killCount, killCountRef.Value);
            Interlocked.Add(ref playerData.ValueRW.experience, experienceRef.Value);

            // 2) Accumulate kills/experience
            playerData.ValueRW.killCount += killCountRef.Value;
            playerData.ValueRW.experience += experienceRef.Value;

            // 3) Check if we leveled up (maybe multiple times)
            int currentLevel = playerData.ValueRO.level;
            int newLevel = LevelUtils.CalculateLevelByExperience(playerData.ValueRO.experience);

            if (newLevel > currentLevel)
            {
                // we gained (newLevel - currentLevel) levels at once
                int gainedLevels = newLevel - currentLevel;
                playerData.ValueRW.level = newLevel;
                playerData.ValueRW.pendingLevelUps += gainedLevels;

                Debug.Log($"🎉 Player gained {gainedLevels} levels! Now level {newLevel}");

                // 4) Pause the game if not already paused
                //    so we can show the level-up UI
                if (!state.EntityManager.HasComponent<GamePausedTag>(playerSingleton.PlayerEntity))
                {
                    ecb.AddComponent<GamePausedTag>(playerSingleton.PlayerEntity);
                }
            }
            
            killCountRef.Dispose();
            experienceRef.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public partial struct UpdatePlayerStatsJob : IJobEntity {
            public EntityCommandBuffer.ParallelWriter ECB;
            public NativeReference<int> KillCount;
            public NativeReference<int> Experience;

            public void Execute(Entity entity, [EntityIndexInQuery] int index, in DisabledEnemyTag tag, in Disabled isDisabled) {
                KillCount.Value += 1;
                Experience.Value += 1;
                ECB.DestroyEntity(index, entity);
            }
        }
    }
}