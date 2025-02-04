using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace ECS.Systems.Projectiles {
    
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(RegularBulletTriggerSystem))]
    [BurstCompile]
    public partial struct PlayerBulletProjectileLifeTimeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (projectileComponent, entity) in
                     SystemAPI.Query<RefRW<ProjectileComponent>>().WithNone<GrenadeComponent>()
                         .WithEntityAccess())
            {
                if (entity == Entity.Null || !state.EntityManager.HasComponent<ProjectileComponent>(entity)) {
                    continue;
                }
                
                projectileComponent.ValueRW.Lifetime -= SystemAPI.Time.DeltaTime;
                if (projectileComponent.ValueRW.Lifetime <= 0)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}