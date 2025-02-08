using ECS.Components;
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
                     SystemAPI.Query<RefRW<ProjectileComponent>>().WithNone<GrenadeComponent, DisabledProjectileTag>()
                         .WithEntityAccess())
            {
                if (entity == Entity.Null || !state.EntityManager.HasComponent<ProjectileComponent>(entity)) {
                    continue;
                }
                
                projectileComponent.ValueRW.Lifetime -= SystemAPI.Time.DeltaTime;
                if (projectileComponent.ValueRW.Lifetime <= 0)
                {
                    ecb.AddComponent<DisabledProjectileTag>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}