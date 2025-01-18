using System;
using ECS.Bakers;
using ScriptableObjects;
using Unity.Burst;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using Math = System.Math;
using Random = UnityEngine.Random;

namespace ECS {
    [BurstCompile]
    public partial class PlayerCameraSystem : SystemBase {
        private CinemachineCamera _cinemachineCamera;
        private GameObject _proxyGameObject;

        protected override void OnCreate() {
            RequireForUpdate<PlayerSingleton>();
        }


        protected override void OnUpdate() {
            if (_cinemachineCamera == null) {
                _cinemachineCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CinemachineCamera>();
            }
 
            if (_proxyGameObject == null) {
                _proxyGameObject = new GameObject("PlayerProxy");
            }

            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var localTransform = SystemAPI.GetComponent<LocalTransform>(playerSingleton.PlayerEntity);
            _proxyGameObject.transform.position = localTransform.Position.xyz;
            _proxyGameObject.transform.rotation = localTransform.Rotation;
            _cinemachineCamera.Follow = _proxyGameObject.transform;
        }
    }

    [BurstCompile]
    public partial struct PlayerLockRotation : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var localTransform = SystemAPI.GetComponent<LocalTransform>(playerSingleton.PlayerEntity);
            localTransform.Rotation = Quaternion.identity;
            SystemAPI.SetComponent(playerSingleton.PlayerEntity, localTransform);
        }
    }

    [BurstCompile]
    public partial struct EnemyLockRotation : ISystem {
        public void OnUpdate(ref SystemState state) {
            foreach (var enemyTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<EnemyTag, IsSpawned>()) {
                enemyTransform.ValueRW.Rotation = Quaternion.identity;
            }
        }
    }

    public partial struct PlayerSpawnerSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerTag>();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            if (!SystemAPI.HasSingleton<PlayerSingleton>()) {
                foreach (var (spawner, entity) in SystemAPI.Query<RefRO<EntityData>>()
                             .WithEntityAccess()
                             .WithAll<PlayerTag>()
                             .WithNone<IsSpawned>()) {
                    Debug.Log(spawner.ValueRO.prefab);
                    Entity playerEntity = ecb.Instantiate(spawner.ValueRO.prefab);

                    ecb.SetComponent(playerEntity, LocalTransform.FromPositionRotationScale(
                        //new float3(10, 0, 0),
                        new float3(0, 0, 0),
                        quaternion.identity,
                        0.2f
                    ));
                    ecb.AddComponent<PlayerTag>(playerEntity);
                    ecb.AddComponent<IsSpawned>(entity);
                    ecb.AddComponent(entity, new PhysicsVelocity {
                        Linear = float3.zero,
                        Angular = float3.zero
                    });
                    ecb.AddComponent(playerEntity, new ProjectileShootingData { nextShootingTime = 2f });
                    ecb.AddComponent(playerEntity, new PlayerData { experience = 0, level = 1 });
                    ecb.AddComponent(playerEntity, new CharacterStats {
                        health = 10f,
                        maxHealth = 10f,
                        stamina = 100f,
                        maxStamina = 100f,
                        armor = 6f,
                        criticalHitChance = 0f,
                        criticalDamage = 1.0f,
                        luck = 1f,
                        sanity = 10f,
                        lifeSteal = 1f,
                        dodge = 4f,
                        healthRegeneration = 2f,
                        armorRegeneration = 3f,
                    });

                    ecb.AddComponent(playerEntity, new AnimationParameters());
                    ecb.AddBuffer<EquippedGun>(playerEntity);

                    ecb.AddComponent<UIUpdateFlag>(playerEntity);

                    ecb.AddComponent<ReloadTimer>(playerEntity);
                    
                    ecb.AddBuffer<Inventory>(playerEntity);

                    Entity singletonEntity = ecb.CreateEntity();
                    ecb.AddComponent(singletonEntity, new PlayerSingleton { PlayerEntity = playerEntity });
                    ecb.SetName(singletonEntity, "Player Singleton Entity");
                    
                    var assaultRifleRequest = ecb.CreateEntity();
                    ecb.AddComponent(assaultRifleRequest, new SpawnGunRequest
                    {
                        gunType = GunType.Rifle,
                        position = new float3(1,1,0)
                    });
                    
                    var shotgunRequest = ecb.CreateEntity();
                    ecb.AddComponent(shotgunRequest, new SpawnGunRequest
                    {
                        gunType = GunType.Shotgun,
                        position = new float3(-1,-1,0)
                    });
                    break;
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
        }
    }

    [BurstCompile]
    public partial struct SpawnerSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<WaveManager>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
            WaveManager waveManager = SystemAPI.GetSingleton<WaveManager>();

            new ProcessEnemySpawnerJob {
                deltaTime = SystemAPI.Time.DeltaTime,
                ecb = ecb,
                randomPosition = (0.03f < Random.Range(0f, 1f)) switch {
                    true => new Vector2(Random.Range(-7f, 7f), Random.Range(-4f, 4f)),
                    false => new Vector2(Random.Range(-5f, 5f), Random.Range(-3f, 3f))
                },
                randomEnemyType = Random.Range(1, 1001) switch {
                    <= 700 => EnemyType.BASIC_ZOMBIE,
                    <= 950 => EnemyType.RUNNER_ZOMBIE,
                    _ => EnemyType.TANK_ZOMBIE
                },
                waveManager = waveManager,
            }.ScheduleParallel();
        }

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
    }

    [BurstCompile]
    public partial struct ProcessEnemySpawnerJob : IJobEntity {
        public WaveManager waveManager;
        public EntityCommandBuffer.ParallelWriter ecb;
        public float deltaTime;
        public EnemyType randomEnemyType;
        public Vector2 randomPosition;


        private void Execute([ChunkIndexInQuery] int chunkIndex, ref EntityData spawner, ref SpawnerTime spawnerTime) {
            spawnerTime.nextSpawnTime -= deltaTime;
            if (spawnerTime.nextSpawnTime > 0 || !waveManager.isActive) {
                return;
            }

            Entity newEntity = ecb.Instantiate(chunkIndex, spawner.prefab);

            // Attach Tags
            ecb.AddComponent<EnemyTag>(chunkIndex, newEntity);
            ecb.AddComponent<IsSpawned>(chunkIndex, newEntity);

            ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPositionRotationScale(
                new float3(randomPosition.x, randomPosition.y, 0),
                quaternion.identity,
                2.5f
            ));

            ecb.AddComponent(chunkIndex, newEntity, new EnemyData {
                enemyType = randomEnemyType,
                health = 10,
                damage = 2f,
                meleeAttackRange = 1f,
                attackSpeed = 1f
            });

            ecb.AddComponent(chunkIndex, newEntity, new AttackTimer {
                TimeElapsed = 0f
            });
            ecb.AddComponent(chunkIndex, newEntity, new PhysicsVelocity {
                Linear = float3.zero,
                Angular = float3.zero
            });
            ecb.AddComponent(chunkIndex, newEntity, new PhysicsDamping {
                Linear = 0.9f,
                Angular = 0.9f
            });
            spawnerTime.nextSpawnTime = 2f;
        }
    }

    [BurstCompile]
    public partial class WaveManagerSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<PlayerSingleton>();
            Entity waveManagerEntity = EntityManager.CreateEntity(ComponentType.ReadWrite<WaveManager>());
            SystemAPI.SetSingleton(new WaveManager { currentWave = 1, isActive = false, waveTimer = 20f });
            EntityManager.SetName(waveManagerEntity, "WaveManagerEntity");

        }

        protected override void OnUpdate() {
            WaveManager waveManager = SystemAPI.GetSingleton<WaveManager>();
            var currentText = waveManager.isActive ? $"- Time Left: {Math.Round(waveManager.waveTimer)}" : "";
            UIController.Instance.SetTextValue(UIController.TextType.COUNTDOWN_TEXT, $"Wave: {waveManager.currentWave} {currentText}");
            UIController.Instance.SetTextValue(UIController.TextType.ARMOR_TEXT, !waveManager.isActive ? "Press E to start wave" : "");

            // test
            //UIController.Instance.SetTextValue(UIController.TextType.ITEM_DROP_TEXT, "ITS THE ITEM");
            //UIController.Instance.UpdateTextPosition(UIController.TextType.ITEM_DROP_TEXT, new Vector2(5f, 5f));

            if (!waveManager.isActive) {
                return;
            }

            waveManager.waveTimer -= SystemAPI.Time.DeltaTime;
            if (waveManager.waveTimer <= 0) {
                waveManager.isActive = false;
            }

            SystemAPI.SetSingleton(waveManager);
        }
    }

    [BurstCompile]
    public partial struct EnemyMovementSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            if (SystemAPI.TryGetSingletonRW(out RefRW<PlayerSingleton> singletonRW)) {
                Entity playerEntity = singletonRW.ValueRW.PlayerEntity;
                RefRO<LocalTransform> playerTransform = SystemAPI.GetComponentRO<LocalTransform>(playerEntity);
                foreach (var enemyTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<EnemyTag, IsSpawned>()) {
                    enemyTransform.ValueRW.Position += math.normalize(playerTransform.ValueRO.Position - enemyTransform.ValueRO.Position) *
                                                       1 * SystemAPI.Time.DeltaTime;
                }
            }
        }
    }

    public partial struct PlayerStatsSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

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

        public void OnUpdate(ref SystemState state) {
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            PlayerData playerData = SystemAPI.GetComponent<PlayerData>(playerSingleton.PlayerEntity);
            CharacterStats characterStats = SystemAPI.GetComponent<CharacterStats>(playerSingleton.PlayerEntity);
            DynamicBuffer<EquippedGun> equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            //UIController.Instance.SetTextValue(UIController.TextType.HEALTH_TEXT, $"Health: {characterStats.health.ToString()}");
            UIController.Instance.UpdateHealthBar(characterStats.health, characterStats.maxHealth);

            UIController.Instance.SetTextValue(UIController.TextType.SCOREBOARD_TEXT,
                $"Level: {CalculateLevelByExperience(playerData.experience)} Exp: {playerData.experience}");


            if (!equippedGunBuffer.IsEmpty) {
                AmmoComponent ammoComponent = SystemAPI.GetComponent<AmmoComponent>(equippedGunBuffer[0].GunEntity);
                GunTypeComponent gunTypeComponent = SystemAPI.GetComponent<GunTypeComponent>(equippedGunBuffer[0].GunEntity);

                if (SystemAPI.HasComponent<ReloadingTag>(playerSingleton.PlayerEntity)) {
                    UIController.Instance.SetTextValue(UIController.TextType.AMMO_TEXT,
                        $"{gunTypeComponent.type.ToString()} (Reloading)");
                }
                else {
                    UIController.Instance.SetTextValue(UIController.TextType.AMMO_TEXT,
                        $"{gunTypeComponent.type.ToString()} {ammoComponent.currentAmmo}/{ammoComponent.capacity}");
                }
            }
            else {
                UIController.Instance.SetTextValue(UIController.TextType.AMMO_TEXT, $"");
            }
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

                waveManager.waveTimer = 5f;
                waveManager.currentWave++;
                waveManager.isActive = true;
                Debug.Log(waveManager.waveTimer + " - " + waveManager.currentWave + " - " + waveManager.isActive);
                SystemAPI.SetSingleton(waveManager);
            }
        }
    }

    // [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    // public partial struct PlayerAnimationSystem : ISystem
    // {
    //     public void OnUpdate(ref SystemState state)
    //     {
    //         foreach (var (animationParams, localTransform) in SystemAPI.Query<RefRW<AnimationParameters>, RefRO<LocalTransform>>())
    //         {
    //             float speed = math.length(localTransform.ValueRO.Position.xy);
    //             animationParams.ValueRW.Speed = speed;
    //             animationParams.ValueRW.Side = speed > 0 ? 1 : -1;
    //             animationParams.ValueRW.HoldItem = false; // Example logic
    //         }
    //     }
    // }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem {
        [BurstCompile]
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
                transformRW.ValueRW.Position += new float3(moveDirection * 2 * SystemAPI.Time.DeltaTime, 0f);
            }
        }
    }

    [BurstCompile]
    public partial struct PlayerProjectileLifeTimeSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (projectileComponent, transform, projectileEntity) in
                     SystemAPI.Query<RefRW<ProjectileComponent>, RefRW<LocalTransform>>().WithEntityAccess().WithAll<ProjectileComponent>()) {

                projectileComponent.ValueRW.Lifetime -= SystemAPI.Time.DeltaTime;
                transform.ValueRW.Position += projectileComponent.ValueRW.Velocity * SystemAPI.Time.DeltaTime;

                if (projectileComponent.ValueRW.Lifetime <= 0) {
                    ecb.DestroyEntity(projectileEntity);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    public partial struct ProcessProjectileSpawnerJob : IJobEntity {
        public EntityCommandBuffer.ParallelWriter ecb;
        public Vector3 position;
        public Vector2 shootDirection;


        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int chunkIndex, ref EntityData spawner, ProjectileSpawner projectileSpawner) {
            Entity projectileEntity = ecb.Instantiate(chunkIndex, spawner.prefab);
            ecb.AddComponent(chunkIndex, projectileEntity, new LocalTransform {
                Position = position,
                Rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg),
                Scale = 0.3f,
            });
            ecb.AddComponent(chunkIndex, projectileEntity, new ProjectileComponent {
                Speed = 5f,
                Lifetime = 4f,
                Velocity = new float3(shootDirection.x, shootDirection.y, 0f) * 10f,
                BaseDamage = 2f,
            });
            ecb.AddComponent(chunkIndex, projectileEntity, new ProjectileTag { });
        }
    }

    public partial struct ReloadWeapon : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<ReloadingTag>();
            state.RequireForUpdate<PlayerSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            DynamicBuffer<EquippedGun> equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            Debug.Log("RELOADING STARTED");
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
                Debug.Log("RELOADING DONE");
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

                shootDirection = ApplyRecoil(shootDirection, weaponData.recoilAmount);

                var muzzlePointTransform = state.EntityManager.GetComponentData<MuzzlePointTransform>(equippedGunBuffer[0].GunEntity);
                var weaponLocalTransform = state.EntityManager.GetComponentData<LocalTransform>(equippedGunBuffer[0].GunEntity);

                float3 gunWorldPosition = weaponLocalTransform.Position;
                quaternion gunWorldRotation = weaponLocalTransform.Rotation;
                float3 muzzleLocalPosition = muzzlePointTransform.position;
                float3 transformedMuzzlePosition = math.mul(gunWorldRotation, muzzleLocalPosition);
                float3 muzzleWorldPosition = gunWorldPosition + transformedMuzzlePosition;
                
                EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
                for (int i = 0; i < weaponData.bulletsPerShot; i++) {
                    shootDirection = ApplySpread(shootDirection, weaponData.spreadAmount);
                    new ProcessProjectileSpawnerJob {
                        ecb = ecb,
                        position = muzzleWorldPosition,
                        shootDirection = math.normalize(shootDirection),
                    }.ScheduleParallel();
                }

                projectileShootingData.ValueRW.nextShootingTime = 1 / weaponData.attackRate;
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

    public partial struct EnemyMeleeAttackSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            foreach (var (enemyData, enemyTransform, attackTimer, enemyEntity) in SystemAPI.Query<RefRW<EnemyData>, RefRW<LocalTransform>, RefRW<AttackTimer>>()
                         .WithEntityAccess()) {
                attackTimer.ValueRW.TimeElapsed += SystemAPI.Time.DeltaTime;

                foreach (var (playerData, playerTransform, characterStats, playerEntity) in SystemAPI
                             .Query<RefRW<PlayerData>, RefRO<LocalTransform>, RefRW<CharacterStats>>()
                             .WithEntityAccess()) {
                    var distance = Vector3.Distance(enemyTransform.ValueRO.Position, playerTransform.ValueRO.Position);
                    if (distance <= enemyData.ValueRO.meleeAttackRange && attackTimer.ValueRW.TimeElapsed >= enemyData.ValueRO.attackSpeed) {
                        characterStats.ValueRW.health -= enemyData.ValueRO.damage;
                        Debug.Log($"Enemy {enemyEntity} attacks Player {playerEntity}, causing {enemyData.ValueRO.damage} damage.");
                        attackTimer.ValueRW.TimeElapsed = 0f;
                    }
                }
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CheckCollisionsSystem : ISystem {
        private ComponentLookup<PhysicsCollider> colliderLookup;
        private ComponentLookup<ProjectileComponent> projectileComponentLookup;
        private ComponentLookup<EnemyData> enemyDataLookup;
        private ComponentLookup<PlayerData> playerDataLookup;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PlayerSingleton>();
            colliderLookup = state.GetComponentLookup<PhysicsCollider>(isReadOnly: true);
            projectileComponentLookup = state.GetComponentLookup<ProjectileComponent>(isReadOnly: false);
            enemyDataLookup = state.GetComponentLookup<EnemyData>(isReadOnly: false);
            playerDataLookup = state.GetComponentLookup<PlayerData>(isReadOnly: false);
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            colliderLookup.Update(ref state);
            projectileComponentLookup.Update(ref state);
            enemyDataLookup.Update(ref state);
            playerDataLookup.Update(ref state);

            state.Dependency = new CheckCollisionEvents {
                colliderLookup = colliderLookup,
                projectileComponentLookup = projectileComponentLookup,
                enemyDataLookup = enemyDataLookup,
                playerDataLookup = playerDataLookup,
                entityManager = state.EntityManager,
                playerEntity = SystemAPI.GetSingleton<PlayerSingleton>().PlayerEntity,
                ecb = ecb.AsParallelWriter(),
                deltaTime = SystemAPI.Time.DeltaTime
                //itemPrefab = itemPrefab,
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        static CollisionBelongsToLayer CheckCollisionFilter(PhysicsCollider collider) {
            var collisionFilter = collider.Value.Value.GetCollisionFilter();

            foreach (CollisionBelongsToLayer layer in Enum.GetValues(typeof(CollisionBelongsToLayer))) {
                if ((collisionFilter.BelongsTo & (uint)layer) != 0) {
                    return layer;
                }
            }

            return CollisionBelongsToLayer.None;
        }


        struct CheckCollisionEvents : ICollisionEventsJob {
            [ReadOnly] public ComponentLookup<PhysicsCollider> colliderLookup;
            public ComponentLookup<ProjectileComponent> projectileComponentLookup;
            public ComponentLookup<EnemyData> enemyDataLookup;
            public ComponentLookup<PlayerData> playerDataLookup;
            public Entity playerEntity;
            public EntityManager entityManager;
            public EntityCommandBuffer.ParallelWriter ecb;
            public float deltaTime;

            public void Execute(CollisionEvent collisionEvent) {
                var (projectile, otherEntity) = GetEntityWithComponent<ProjectileTag>(collisionEvent.EntityA, collisionEvent.EntityB);
                if (projectile != Entity.Null && entityManager.HasComponent<PhysicsCollider>(otherEntity)) {

                    var collider = colliderLookup[otherEntity];
                    var selectedFilter = CheckCollisionFilter(collider);

                    if (selectedFilter == CollisionBelongsToLayer.Wall) {
                        ecb.DestroyEntity(0, projectile);
                        return;
                    }

                    HandleProjectileCollision(projectile, otherEntity);
                    return;
                }

                var (enemy, player) = GetEntityWithComponent<EnemyTag>(collisionEvent.EntityA, collisionEvent.EntityB);
                if (player != Entity.Null && enemy != Entity.Null) {
                    return;
                }

            }

            //void SpawnItem() {
            //    Entity itemEntity = ecb.Instantiate(0, itemPrefab);
            //    ecb.AddComponent(0, itemEntity, new LocalTransform {
            //        Position = new Vector3(-5f, 1f, 0f),
            //        Rotation = Quaternion.identity,
            //        Scale = 0.6f,
            //    });
            //    ecb.AddComponent(0, itemEntity, new ItemTag { });
            //}

            (Entity, Entity) GetEntityWithComponent<T>(Entity entityA, Entity entityB) where T : struct, IComponentData {
                if (entityManager.HasComponent<T>(entityA)) {
                    return (entityA, entityB);
                }

                if (entityManager.HasComponent<T>(entityB)) {
                    return (entityB, entityA);
                }

                return (Entity.Null, Entity.Null);
            }

            void HandleProjectileCollision(Entity projectileEntity, Entity collidedEntity) {
                if (enemyDataLookup.HasComponent(collidedEntity) && projectileComponentLookup.HasComponent(projectileEntity)) {
                    var enemyData = enemyDataLookup[collidedEntity];
                    var projectileData = projectileComponentLookup[projectileEntity];
                    enemyData.health -= projectileData.BaseDamage;
                    enemyDataLookup[collidedEntity] = enemyData;

                    if (enemyData.health <= 0f) {
                        ecb.DestroyEntity(0, collidedEntity);
                        var playerData = playerDataLookup[playerEntity];
                        playerData.experience += 1;
                        playerDataLookup[playerEntity] = playerData;
                    }

                    ecb.DestroyEntity(0, projectileEntity);
                    return;
                }

                ecb.DestroyEntity(0, collidedEntity);
                ecb.DestroyEntity(0, projectileEntity);
            }
        }
    }
}