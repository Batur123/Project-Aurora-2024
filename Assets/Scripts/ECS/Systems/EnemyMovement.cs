using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ECS.Systems {
    // Working but use new for test
    //[BurstCompile]
    //[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    //[UpdateAfter(typeof(PhysicsSimulationGroup))]
    //public partial struct EnemyMovementSystem : ISystem {
    //    public void OnCreate(ref SystemState state) {
    //        state.RequireForUpdate<PlayerSingleton>();
    //        state.RequireForUpdate<EnemyTag>();
    //        state.RequireForUpdate<IsSpawned>();
    //    }
//
    //    public void OnUpdate(ref SystemState state) {
    //        var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
    //        RefRO<LocalTransform> playerTransform = SystemAPI.GetComponentRO<LocalTransform>(playerSingleton.PlayerEntity);
//
    //        foreach (var (enemyPhysics, enemyTransform, entity) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<LocalTransform>>()
    //                     .WithAll<EnemyTag, IsSpawned>().WithNone<DisabledEnemyTag>().WithEntityAccess()) {
    //            float3 direction = math.normalize(playerTransform.ValueRO.Position - enemyTransform.ValueRO.Position);
    //            enemyPhysics.ValueRW.Linear += direction * UnityEngine.Random.Range(0.9f, 1.5f) * SystemAPI.Time.DeltaTime;
    //        }
    //    }
    //}

    // [BurstCompile]
    // [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    // [UpdateBefore(typeof(PhysicsSimulationGroup))]
    // public partial struct EnemyMovementSystem : ISystem
    // {
    //     public void OnCreate(ref SystemState state)
    //     {
    //         state.RequireForUpdate<PlayerSingleton>();
    //         state.RequireForUpdate<EnemyTag>();
    //         state.RequireForUpdate<IsSpawned>();
    //     }
    //
    //     public void OnUpdate(ref SystemState state) {
    //         var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
    //         var playerTransform = SystemAPI.GetComponentRO<LocalTransform>(playerSingleton.PlayerEntity);
    //         float3 ppos = playerTransform.ValueRO.Position;
    //     
    //         float speed = 1f;
    //
    //         foreach (var (enemyPhysics, enemyTransform) 
    //                  in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<LocalTransform>>()
    //                      .WithAll<EnemyTag, IsSpawned>()
    //                      .WithNone<DisabledEnemyTag>())
    //         {
    //             float3 epos = enemyTransform.ValueRO.Position;
    //             float3 dir  = ppos - epos;
    //             dir.z       = 0f;
    //
    //             if (math.lengthsq(dir) > 0.000001f) 
    //             {
    //                 dir = math.normalize(dir);
    //             }
    //             else
    //             {
    //                 dir = float3.zero;
    //             }
    //
    //             // Kill old velocity so there's no "surfing"
    //             enemyPhysics.ValueRW.Linear = float3.zero;
    //         
    //             // Set new velocity instantly towards player
    //             enemyPhysics.ValueRW.Linear = dir * speed;
    //         }
    //     }
    // }

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial struct EnemyMovementSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PlayerSingleton>();
            state.RequireForUpdate<EnemyTag>();
            state.RequireForUpdate<IsSpawned>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var playerTransform = SystemAPI.GetComponentRO<LocalTransform>(playerSingleton.PlayerEntity);
            float3 playerPos = playerTransform.ValueRO.Position;

            new MoveEnemyJob {
                PlayerPosition = playerPos,
                Speed = 1f
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(EnemyTag), typeof(IsSpawned))]
    [WithNone(typeof(DisabledEnemyTag), typeof(Disabled))]
    public partial struct MoveEnemyJob : IJobEntity {
        public float3 PlayerPosition;
        public float Speed;

        private void Execute(
            in EnemyData enemyData,
            in EnemyTag enemyTag,
            in IsSpawned isSpawnedTag,
            ref PhysicsVelocity velocity,
            in LocalTransform enemyTransform
        ) {
            float3 epos = enemyTransform.Position;
            float3 dir = PlayerPosition - epos;

            dir.z = 0f;

            if (math.lengthsq(dir) > 1e-6f)
                dir = math.normalize(dir);
            else
                dir = float3.zero;

            velocity.Linear = dir * Speed;
        }
    }

    public partial class EnemyMovementSpriteFlip : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<PlayerSingleton>();
            RequireForUpdate<EnemyTag>();
        }


        protected override void OnUpdate() {
            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            LocalTransform playerLocalTransform = SystemAPI.GetComponent<LocalTransform>(playerSingleton.PlayerEntity);

            Entities.WithAll<EnemyTag, IsSpawned>().WithNone<DisabledEnemyTag>()
                .ForEach((LocalTransform localTransform, SpriteRenderer spriteRenderer) => {
                    var directionToPlayer = playerLocalTransform.Position.x - localTransform.Position.x;
                    if (directionToPlayer > 0) {
                        spriteRenderer.flipX = false;
                    }
                    else if (directionToPlayer < 0) {
                        spriteRenderer.flipX = true;
                    }
                })
                .WithoutBurst().Run();
        }
    }
}