using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ECS.Systems {
    [BurstCompile]
    public partial struct SpawnerSystem : ISystem {
        private const float SPAWN_DISTANCE = 5f;
        private const float OFFSET_RANGE = 2f;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<WaveManager>();
            state.RequireForUpdate<PlayerSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
            var waveManager = SystemAPI.GetSingleton<WaveManager>();
            var playerSingl = SystemAPI.GetSingleton<PlayerSingleton>();
            var playerTrans = SystemAPI.GetComponentRO<LocalTransform>(playerSingl.PlayerEntity);

            float2 playerPos2D = playerTrans.ValueRO.Position.xy;

            for (int i = 0; i < 15; i++) {
                int directionIndex = Random.Range(0, 4);
                float2 baseOffset = directionIndex switch {
                    0 => new float2(0, SPAWN_DISTANCE),
                    1 => new float2(0, -SPAWN_DISTANCE),
                    2 => new float2(SPAWN_DISTANCE, 0),
                    _ => new float2(-SPAWN_DISTANCE, 0),
                };

                float2 randomOffset = new float2(
                    Random.Range(-OFFSET_RANGE, OFFSET_RANGE),
                    Random.Range(-OFFSET_RANGE, OFFSET_RANGE)
                );

                float2 finalSpawnPos = playerPos2D + baseOffset + randomOffset;

                EnemyType randomEnemyType = Random.Range(1, 1001) switch {
                    <= 700 => EnemyType.BASIC_ZOMBIE,
                    <= 950 => EnemyType.RUNNER_ZOMBIE,
                    _ => EnemyType.TANK_ZOMBIE
                };

                new ProcessEnemySpawnerJob {
                    deltaTime = SystemAPI.Time.DeltaTime,
                    ecb = ecb,
                    waveManager = waveManager,
                    randomEnemyType = randomEnemyType,
                    spawnPosition2D = finalSpawnPos
                }.ScheduleParallel();
            }
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

        public float2 spawnPosition2D;

        private void Execute([ChunkIndexInQuery] int chunkIndex,
            ref EntityData spawner,
            ref SpawnerTime spawnerTime) {
            spawnerTime.nextSpawnTime -= deltaTime;
            if (spawnerTime.nextSpawnTime > 0 || !waveManager.isActive)
                return;

            Entity newEntity = ecb.Instantiate(chunkIndex, spawner.prefab);

            ecb.AddComponent<EnemyTag>(chunkIndex, newEntity);
            ecb.AddComponent<IsSpawned>(chunkIndex, newEntity);

            float3 spawnPos3D = new float3(spawnPosition2D.x, spawnPosition2D.y, 0);
            ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPositionRotationScale(
                spawnPos3D,
                quaternion.identity,
                2.5f
            ));

            ecb.AddComponent(chunkIndex, newEntity, new EnemyData {
                enemyType = randomEnemyType,
                health = 10,
                maxHealth = 10,
                damage = 2f,
                meleeAttackRange = 0.2f,
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
            
           //Entity healthBarEntity = ecb.Instantiate(chunkIndex, spawner.healthBarPrefab);
           //ecb.AddComponent(chunkIndex, newEntity, new HealthBarEntity {
           //    healthBarEntity = healthBarEntity
           //});
           //ecb.SetComponent(chunkIndex, healthBarEntity, LocalTransform.FromPositionRotationScale(
           //    spawnPos3D,
           //    quaternion.identity,
           //    2.5f
           //));
            
            spawnerTime.nextSpawnTime = 0.05f;
        }
    }
}
