using ScriptableObjects;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems.Projectiles {
    
    [BurstCompile]
    public partial struct SpawnProjectileJob : IJobEntity {
        public EntityCommandBuffer.ParallelWriter ecb;
        public Vector3 position;
        public Vector2 shootDirection;
        public Vector3 mousePosition;

        public GunType gunType;
        public ProjectileType weaponProjectileType;
        public WeaponData weaponData;

        public float randomPeak;
        public float3 randomizedTarget;

        public Entity InstantiatePrefab(int chunkIndex, ref EntityData spawner) {
            Entity prefab = Entity.Null;
            switch (weaponProjectileType) {
                case ProjectileType.BULLET: {
                    prefab = spawner.prefab;
                    break;
                }
                case ProjectileType.EXPLOSIVE_GRENADE: {
                    prefab = spawner.grenadePrefab;
                    break;
                }
                default: {
                    prefab = spawner.prefab;
                    break;
                }
            }

            Entity projectileEntity = ecb.Instantiate(chunkIndex, prefab);
            return projectileEntity;
        }

        public float SelectProjectileScale() {
            switch (weaponProjectileType) {
                case ProjectileType.EXPLOSIVE_GRENADE: {
                    return 1.5f;
                }
                default: {
                    return 0.3f;
                }
            }
        }

        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int chunkIndex, ref EntityData spawner, ProjectileSpawner projectileSpawner) {
            Entity projectileEntity = InstantiatePrefab(chunkIndex, ref spawner);

            ecb.AddComponent<PhysicsVelocity>(chunkIndex, projectileEntity);

            ecb.AddComponent(chunkIndex, projectileEntity, new LocalTransform {
                Position = position,
                Rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg),
                Scale = SelectProjectileScale(),
            });
            ecb.AddComponent(chunkIndex, projectileEntity, new ProjectileTag { });
            ecb.AddComponent(chunkIndex, projectileEntity, new ProjectileComponent {
                Speed = 5f,
                Lifetime = 4f,
                Velocity = new float3(shootDirection.x, shootDirection.y, 0f) * 1f,
                BaseDamage = 55f,
            });
            ecb.AddComponent(chunkIndex, projectileEntity, new ProjectileDataComponent {
                piercingEnemyNumber = weaponData.piercingBulletsPerShot,
            });

            // Add this to use projectile in correct system.
            ecb.AddComponent(chunkIndex, projectileEntity, new WeaponProjectileTypeComponent {
                projectileType = weaponProjectileType
            });

            if (weaponProjectileType != ProjectileType.EXPLOSIVE_GRENADE) {
                ecb.SetComponent(chunkIndex, projectileEntity, new PhysicsVelocity {
                    Linear = new float3(shootDirection.x, shootDirection.y, 0) * 10f, // bullet speed
                    Angular = float3.zero
                });
            }

            switch (gunType) {
                case GunType.GrenadeLauncher: {
                    ecb.AddComponent(chunkIndex, projectileEntity, new GrenadeComponent {
                        StartPosition = position,
                        TargetPosition = mousePosition,
                        ThrowTime = 1f,
                        ElapsedTime = 0f,
                        PeakHeight = randomPeak,
                        RandomizedTarget = randomizedTarget,
                        FuseDuration = 3f,
                    });

                    break;
                }
                default: {

                    break;
                }
            }
        }
    }
}