using ECS.Components;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

namespace ECS.Systems {
    [BurstCompile]
    public partial struct WeaponStatCalculationSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            foreach (var (weaponData, baseWeaponData, weaponEntity) in SystemAPI.Query<RefRW<WeaponData>, RefRW<BaseWeaponData>>().WithEntityAccess()) {
                DynamicBuffer<Child> attachments = state.EntityManager.GetBuffer<Child>(weaponEntity);
                
                float damage = baseWeaponData.ValueRO.damage;
                float attackSpeed = baseWeaponData.ValueRO.attackSpeed;
                float recoilAmount = baseWeaponData.ValueRO.recoilAmount;
                float spreadAmount = baseWeaponData.ValueRO.spreadAmount;
                int bulletsPerShot = baseWeaponData.ValueRO.bulletsPerShot;

                // Iterate over all attachments and accumulate modifiers
                foreach (var child in attachments) {
                    Entity attachmentEntity = child.Value;

                    if (state.EntityManager.HasComponent<AttachmentComponent>(attachmentEntity)) {
                        var attachment = state.EntityManager.GetComponentData<AttachmentComponent>(attachmentEntity);

                        damage += attachment.damage;
                        attackSpeed += attachment.attackSpeed;
                        recoilAmount += attachment.recoilAmount;
                        spreadAmount += attachment.spreadAmount;
                        bulletsPerShot += attachment.bulletsPerShot;
                    }
                }

                // Update the weapon's final stats
                weaponData.ValueRW.damage = damage;
                weaponData.ValueRW.attackSpeed = attackSpeed;
                weaponData.ValueRW.recoilAmount = recoilAmount;
                weaponData.ValueRW.spreadAmount = spreadAmount;
                weaponData.ValueRW.bulletsPerShot = bulletsPerShot;
            }
        }
    }
}