using ScriptableObjects;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS {
    public struct SpawnedTag : IComponentData {
    };

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RifleSpawnSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            return;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (builtPrefab, gunType, entity) in
                     SystemAPI.Query<RefRO<BuiltPrefab>, RefRO<GunTypeComponent>>()
                         .WithEntityAccess().WithNone<SpawnedTag>()) {
                    var spawned = ecb.Instantiate(builtPrefab.ValueRO.prefab);
                    ecb.SetComponent(spawned, LocalTransform.FromPositionRotationScale(
                        new float3(0, 0, 0),
                        quaternion.identity,
                        1f
                    ));
                    ecb.AddComponent<SpawnedTag>(entity);
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}