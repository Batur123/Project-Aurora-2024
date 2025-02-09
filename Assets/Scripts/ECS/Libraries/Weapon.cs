using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace ECS.Libraries {
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GunLibrarySystem : ISystem {
        private NativeHashMap<int, Entity> _weaponVariants;

        public void OnCreate(ref SystemState state) {
            _weaponVariants = new NativeHashMap<int, Entity>(10, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state) {
            _weaponVariants.Dispose();
        }

        public int ComputeKey(GunType weaponType, int variantId) {
            return (0x20000000) | (int)weaponType * 1000 + variantId;
        }
        
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (builtPrefab, weaponType, entity) in
                     SystemAPI.Query<RefRO<BuiltPrefab>, RefRO<GunTypeComponent>>()
                         .WithEntityAccess()) {

                int key = ComputeKey(weaponType.ValueRO.gunType, weaponType.ValueRO.variantId);

                if (!_weaponVariants.ContainsKey(key)) {
                    _weaponVariants[key] = entity;
                }
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        public NativeHashMap<int, Entity> GetAllDescriptors() {
            return _weaponVariants;
        }
        
        public Entity GetDescriptor(GunType weaponType, int variantId) {
            return _weaponVariants.TryGetValue(ComputeKey(weaponType, variantId), out var descriptor) ? descriptor : Entity.Null;
        }
    }
}