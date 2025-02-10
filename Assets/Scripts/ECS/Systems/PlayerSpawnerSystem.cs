using ECS.Components;
using ScriptableObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace ECS.Systems {
    public partial struct PlayerSpawnerSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerTag>();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            if (!SystemAPI.HasSingleton<PlayerSingleton>()) {
                foreach (var (spawner, entity) 
                         in SystemAPI.Query<RefRO<EntityData>>()
                             .WithEntityAccess()
                             .WithAll<PlayerTag>()
                             .WithNone<IsSpawned>()) {

                    
                    Entity playerEntity = ecb.Instantiate(spawner.ValueRO.prefab);
                    
                    ecb.SetComponent(playerEntity, LocalTransform.FromPositionRotationScale(
                        //new float3(10, 0, 0),
                        new float3(0, 0, 0),
                        quaternion.identity,
                        3f
                    ));
                    ecb.AddComponent<PlayerTag>(playerEntity);
                    ecb.AddComponent<IsSpawned>(entity);
                    ecb.AddComponent(entity, new PhysicsVelocity {
                        Linear = float3.zero,
                        Angular = float3.zero
                    });
                    ecb.AddComponent(playerEntity, new ProjectileShootingData { nextShootingTime = 2f });
                    ecb.AddComponent(playerEntity, new PlayerData { experience = 0, level = 1, killCount = 0 });
                    ecb.AddComponent(playerEntity, new CharacterStats {
                        characterStats = {
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
                        }
                    });

                    ecb.AddBuffer<EquippedGun>(playerEntity);

                    ecb.AddComponent<UpdateUserInterfaceTag>(playerEntity);

                    ecb.AddComponent<ReloadTimer>(playerEntity);
                    
                    ecb.AddBuffer<Inventory>(playerEntity);
                    ecb.AddBuffer<PassiveInventory>(playerEntity);

                    Entity singletonEntity = ecb.CreateEntity();
                    ecb.AddComponent(singletonEntity, new PlayerSingleton { PlayerEntity = playerEntity });
                    ecb.SetName(singletonEntity, "Player Singleton Entity");
                    
                    var assaultRifleRequest = ecb.CreateEntity();
                    ecb.AddComponent(assaultRifleRequest, new SpawnGunRequest
                    {
                        gunType = GunType.Rifle,
                        position = new float3(1,1,0),
                        variantId = 0,
                        scale = 1f,
                    });
                    var assaultRifleRequest2 = ecb.CreateEntity();
                    ecb.AddComponent(assaultRifleRequest2, new SpawnGunRequest
                    {
                        gunType = GunType.GrenadeLauncher,
                        position = new float3(1,2,0),
                        variantId = 0,
                        scale = 1f,
                    });
                    var shotgunRequest = ecb.CreateEntity();
                    ecb.AddComponent(shotgunRequest, new SpawnGunRequest
                    {
                        gunType = GunType.Shotgun,
                        position = new float3(-1,-1,0),
                        variantId = 0,
                        scale = 1f,
                    });
                    var minigunRequest = ecb.CreateEntity();
                    ecb.AddComponent(minigunRequest, new SpawnGunRequest
                    {
                        gunType = GunType.Rifle,
                        position = new float3(-2,-1,0),
                        variantId = 1,
                        scale = 1f,
                    });
                    var scopeReq = ecb.CreateEntity();
                    ecb.AddComponent(scopeReq, new SpawnAttachmentRequest {
                        attachmentType = AttachmentType.Scope,
                        position = new float3(-3,-1,0),
                        variantId = 0,
                    });

                    var bandageTest = ecb.CreateEntity();
                    ecb.AddComponent(bandageTest, new SpawnPassiveItemRequest {
                        passiveItemType = PassiveItemType.BANDAGE,
                        position = new float3(-2,-2,0),
                        variantId = 0,
                        scale = 1f,
                    });
                    
                    
                    ecb.AddComponent<ParticleSpawnerRequestTag>(playerEntity);
                    ecb.SetComponent(playerEntity, new ParticleSpawnerRequestTag {
                        particleType = ParticleType.Rain
                    });
                    
                    break;
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
        }
    }
}