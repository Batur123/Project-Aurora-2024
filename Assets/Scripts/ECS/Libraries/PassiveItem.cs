using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace ECS.Libraries {
    [BurstCompile]
    [UpdateBefore(typeof(GunSpawnSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PassiveItemsLibrarySystem : ISystem {
        private NativeHashMap<int, Entity> _passiveItemVariants;

        public void OnCreate(ref SystemState state) {
            _passiveItemVariants = new NativeHashMap<int, Entity>(10, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state) {
            _passiveItemVariants.Dispose();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (builtPrefab, attachmentType, entity) in
                     SystemAPI.Query<RefRO<BuiltPrefab>, RefRO<PassiveItemTypeComponent>>()
                         .WithEntityAccess()) {

                int key = ComputeKey(attachmentType.ValueRO.passiveItemType);

                if (!_passiveItemVariants.ContainsKey(key)) {
                    _passiveItemVariants[key] = entity;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public NativeHashMap<int, Entity> GetAllDescriptors() {
            return _passiveItemVariants;
        }

        public int ComputeKey(PassiveItemType passiveItemType) {
            return (0x30000000) | (int)passiveItemType * 1000 + 0;
        }

        public Entity GetDescriptor(PassiveItemType passiveItemType) {
            return _passiveItemVariants.TryGetValue(ComputeKey(passiveItemType), out var descriptor) ? descriptor : Entity.Null;
        }
    }
}