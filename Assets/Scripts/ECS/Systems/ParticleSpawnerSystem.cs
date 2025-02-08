using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems {
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

    public partial struct ParticleSpawnerSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = GetEntityCommandBuffer(ref state);

            foreach (var (requestTag, entity)
                     in SystemAPI.Query<RefRO<ParticleSpawnerRequestTag>>()
                         .WithEntityAccess()) {
                state.Dependency = new ProcessParticleSpawner {
                    ecb = ecb,
                    requesterEntity = entity,
                    requestTag = requestTag.ValueRO,
                }.Schedule(state.Dependency);
                state.Dependency.Complete();
            }
        }

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
    }

    [BurstCompile]
    public partial struct ProcessParticleSpawner : IJobEntity {
        public EntityCommandBuffer.ParallelWriter ecb;
        public Entity requesterEntity;
        public ParticleSpawnerRequestTag requestTag;

        private void Execute(
            [ChunkIndexInQuery] int chunkIndex,
            in ParticleData particleData
        ) {
            Entity particlePrefab = Entity.Null;
        
            switch (requestTag.particleType) {
                case ParticleType.Rain:
                    particlePrefab = particleData.rainPrefab;
                    break;
                case ParticleType.Bullet_Hit:
                    particlePrefab = particleData.bulletHitExplosionPrefab;
                    break;
                default:
                    ecb.RemoveComponent<ParticleSpawnerRequestTag>(chunkIndex, requesterEntity);
                    return;
            }
            
            Entity particleEntity = ecb.Instantiate(chunkIndex, particlePrefab);

            switch (requestTag.particleType) {
                case ParticleType.Rain: {
                    ecb.AddComponent<RainFollowerParticle>(chunkIndex, particleEntity);
                    ecb.AddComponent(chunkIndex, particleEntity, new LocalTransform {
                        Position = new float3(0,10f,0),
                        Rotation = Quaternion.identity,
                        Scale = 5f,
                    });
                    break;
                }
                case ParticleType.Bullet_Hit: {
                    float scale = 0.1f;
                    
                    ecb.AddComponent(chunkIndex, particleEntity, new LocalTransform {
                        Position = requestTag.spawnPosition,
                        Rotation = Quaternion.identity,
                        Scale = scale,
                    });
                    ecb.AddComponent<BulletHitParticleData>(chunkIndex, particleEntity);
                    ecb.SetComponent(chunkIndex, particleEntity, new BulletHitParticleData {
                        lifeTime = requestTag.particleLifeTime,
                    });
                    ecb.AddComponent<ScaleChildParticlesTag>(chunkIndex, particleEntity);
                    ecb.SetComponent(chunkIndex, particleEntity, new ScaleChildParticlesTag {
                        scale = scale,
                    });
                    
                    break;
                }
            }

            ecb.RemoveComponent<ParticleSpawnerRequestTag>(chunkIndex, requesterEntity);
        }
    }

    [BurstCompile]
    public partial struct ParticleFollowerSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<RainFollowerParticle>();
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var playerTransform = SystemAPI.GetComponentRO<LocalTransform>(playerSingleton.PlayerEntity);
            foreach (var (followerParticle, localTransform)
                     in SystemAPI.Query<RefRO<RainFollowerParticle>, RefRW<LocalTransform>>()) {
                localTransform.ValueRW.Position.x = playerTransform.ValueRO.Position.x;
                localTransform.ValueRW.Position.y = playerTransform.ValueRO.Position.y + 7f;
            }
        }
    }

    [BurstCompile]
    public partial struct ParticleChildScaleSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<ScaleChildParticlesTag>();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            foreach (var (requestTag, rootEntity) in SystemAPI.Query<RefRO<ScaleChildParticlesTag>>().WithEntityAccess()) {
                if (state.EntityManager.HasComponent<LinkedEntityGroup>(rootEntity)) {
                    DynamicBuffer<LinkedEntityGroup> linkedGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(rootEntity);
                    for (int i = 0; i < linkedGroup.Length; i++) {
                        Entity child = linkedGroup[i].Value;
                        if (state.EntityManager.HasComponent<LocalTransform>(child)) {
                            LocalTransform transform = state.EntityManager.GetComponentData<LocalTransform>(child);
                            transform.Scale = requestTag.ValueRO.scale;
                            ecb.SetComponent(child, transform);
                        }
                    }
                }

                ecb.RemoveComponent<ScaleChildParticlesTag>(rootEntity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
    

    [BurstCompile]
    public partial struct BulletParticleLifetimeSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<BulletHitParticleData>();
            state.RequireForUpdate<PlayerSingleton>();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            foreach (var (particleData, entity)
                     in SystemAPI.Query<RefRW<BulletHitParticleData>>()
                         .WithEntityAccess()) {
                particleData.ValueRW.lifeTime -= SystemAPI.Time.DeltaTime;

                if (particleData.ValueRO.lifeTime <= 0f) {
                    ecb.DestroyEntity(entity);
                }
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}