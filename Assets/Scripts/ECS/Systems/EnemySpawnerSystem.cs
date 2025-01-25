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
            spawnerTime.nextSpawnTime = 1f;
        }
    }
}