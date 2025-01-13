using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace ECS {
    [BurstCompile]
    public partial struct DropEquippedWeapon : ISystem {
        private EntityQuery playerQuery;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
            playerQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerSingleton>());
        }

        public void OnUpdate(ref SystemState state) {
            PlayerSingleton playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var equippedGunBuffer = SystemAPI.GetBuffer<EquippedGun>(playerSingleton.PlayerEntity);

            if (equippedGunBuffer.IsEmpty || Input.GetMouseButton(0) || !Input.GetKey(KeyCode.X)) {
                return;
            }

            var equippedGunEntity = equippedGunBuffer[0].GunEntity;
            state.EntityManager.AddComponent<DroppedItemTag>(equippedGunEntity);

            var bufferLookup = state.GetBufferLookup<EquippedGun>(false);
            var ammoLookup = SystemAPI.GetComponentLookup<AmmoComponent>(false);

            bufferLookup.Update(ref state);
            ammoLookup.Update(ref state);

            state.Dependency = new ClearEquippedGunBufferJob {
                PlayerEntity = playerSingleton.PlayerEntity,
                BufferFromEntity = bufferLookup,
                AmmoComponents = ammoLookup,
                ecb = GetEntityCommandBuffer(ref state)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state) {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }

        [BurstCompile]
        private struct ClearEquippedGunBufferJob : IJob {
            public Entity PlayerEntity;
            public EntityCommandBuffer.ParallelWriter ecb;
            [NativeDisableParallelForRestriction] public BufferLookup<EquippedGun> BufferFromEntity;
            [NativeDisableParallelForRestriction] public ComponentLookup<AmmoComponent> AmmoComponents;

            public void Execute() {
                ecb.SetComponent(0, PlayerEntity, new UIUpdateFlag { needsUpdate = true });

                ecb.RemoveComponent<ReloadingTag>(0, PlayerEntity);
                ecb.SetComponent(0, PlayerEntity, new ReloadTimer { timeRemaining = 2f });

                var buffer = BufferFromEntity[PlayerEntity];

                AmmoComponent ammoComponent = AmmoComponents[buffer[0].GunEntity];

                ecb.SetComponent(0, buffer[0].GunEntity, new AmmoComponent {
                    currentAmmo = ammoComponent.currentAmmo,
                    capacity = ammoComponent.capacity,
                    isReloading = false,
                });
                
                buffer.Clear();
            }
        }
    }
}