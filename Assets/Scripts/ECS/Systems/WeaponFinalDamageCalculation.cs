using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

namespace ECS.Systems {
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WeaponStatCalculationSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            foreach (var (weaponData, baseWeaponData, weaponEntity) in SystemAPI.Query<RefRW<WeaponData>, RefRW<BaseWeaponData>>().WithEntityAccess()) {
                DynamicBuffer<Child> attachments = state.EntityManager.GetBuffer<Child>(weaponEntity);
                
                float damage = baseWeaponData.ValueRO.damage;
                float accuracy = baseWeaponData.ValueRO.accuracy;
                float attackRate = baseWeaponData.ValueRO.attackRate;
                float recoilAmount = baseWeaponData.ValueRO.recoilAmount;
                float spreadAmount = baseWeaponData.ValueRO.spreadAmount;
                int bulletsPerShot = baseWeaponData.ValueRO.bulletsPerShot;

                // Iterate over all attachments and accumulate modifiers
                foreach (var child in attachments) {
                    Entity attachmentEntity = child.Value;

                    if (state.EntityManager.HasComponent<AttachmentComponent>(attachmentEntity)) {
                        var attachment = state.EntityManager.GetComponentData<AttachmentComponent>(attachmentEntity);

                        damage += attachment.damage;
                        accuracy += attachment.accuracy;
                        attackRate += attachment.attackRate;
                        recoilAmount += attachment.recoilAmount;
                        spreadAmount += attachment.spreadAmount;
                        bulletsPerShot += attachment.bulletsPerShot;
                    }
                }

                // Update the weapon's final stats
                weaponData.ValueRW.damage = damage;
                weaponData.ValueRW.accuracy = accuracy;
                weaponData.ValueRW.attackRate = attackRate;
                weaponData.ValueRW.recoilAmount = recoilAmount;
                weaponData.ValueRW.spreadAmount = spreadAmount;
                weaponData.ValueRW.bulletsPerShot = bulletsPerShot;
            }
        }
    }
}