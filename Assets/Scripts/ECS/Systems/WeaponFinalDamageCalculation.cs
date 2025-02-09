using ECS.Components;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems {
    [BurstCompile]
    public partial struct WeaponStatCalculationSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
        }
        
        public void OnUpdate(ref SystemState state) {
            foreach (var (weaponData,baseWeaponData,weaponEntity)
                     in SystemAPI.Query<RefRW<WeaponData>, RefRO<BaseWeaponData>>().WithEntityAccess()) {
                DynamicBuffer<Child> attachments = state.EntityManager.GetBuffer<Child>(weaponEntity);

                float damage = baseWeaponData.ValueRO.stats.damage;
                float attackSpeed = baseWeaponData.ValueRO.stats.attackSpeed;
                float recoilAmount = baseWeaponData.ValueRO.stats.recoilAmount;
                float spreadAmount = baseWeaponData.ValueRO.stats.spreadAmount;
                int bulletsPerShot = baseWeaponData.ValueRO.stats.bulletsPerShot;

                int piercingBulletsPerShot = baseWeaponData.ValueRO.stats.piercingBulletsPerShot;
                float reloadSpeed = baseWeaponData.ValueRO.stats.reloadSpeed;
                int ammoCapacity = baseWeaponData.ValueRO.stats.ammoCapacity;

                foreach (var child in attachments) {
                    Entity attachmentEntity = child.Value;

                    if (state.EntityManager.HasComponent<AttachmentData>(attachmentEntity)) {
                        var attachment = state.EntityManager.GetComponentData<AttachmentData>(attachmentEntity);

                        damage += attachment.stats.damage;
                        attackSpeed += attachment.stats.attackSpeed;
                        recoilAmount += attachment.stats.recoilAmount;
                        spreadAmount += attachment.stats.spreadAmount;
                        bulletsPerShot += attachment.stats.bulletsPerShot;

                        piercingBulletsPerShot += attachment.stats.piercingBulletsPerShot;
                        reloadSpeed += attachment.stats.reloadSpeed;
                        ammoCapacity += attachment.stats.ammoCapacity;
                    }
                }

                weaponData.ValueRW.stats.damage = damage;
                weaponData.ValueRW.stats.attackSpeed = attackSpeed;
                weaponData.ValueRW.stats.recoilAmount = recoilAmount;
                weaponData.ValueRW.stats.spreadAmount = spreadAmount;
                weaponData.ValueRW.stats.bulletsPerShot = bulletsPerShot;

                weaponData.ValueRW.stats.piercingBulletsPerShot = piercingBulletsPerShot;
                weaponData.ValueRW.stats.reloadSpeed = reloadSpeed;
                weaponData.ValueRW.stats.ammoCapacity = ammoCapacity;
                
            }
        }
    }
}