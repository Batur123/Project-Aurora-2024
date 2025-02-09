using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace ECS.Libraries {
    [BurstCompile]
    [UpdateBefore(typeof(GunSpawnSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AttachmentLibrarySystem : ISystem {
        private NativeHashMap<int, Entity> _attachmentVariants;

        public void OnCreate(ref SystemState state) {
            _attachmentVariants = new NativeHashMap<int, Entity>(10, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state) {
            _attachmentVariants.Dispose();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (builtPrefab, attachmentType, entity) in
                     SystemAPI.Query<RefRO<BuiltPrefab>, RefRO<AttachmentTypeComponent>>()
                         .WithEntityAccess()) {

                int key = ComputeKey(attachmentType.ValueRO.attachmentType, attachmentType.ValueRO.variantId);

                if (!_attachmentVariants.ContainsKey(key)) {
                    _attachmentVariants[key] = entity;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public NativeHashMap<int, Entity> GetAllDescriptors() {
            return _attachmentVariants;
        }

        public int ComputeKey(AttachmentType attachmentType, int variantId) {
            return (0x10000000) | (int)attachmentType * 1000 + variantId;
        }

        public Entity GetDescriptor(AttachmentType attachmentType, int variantId) {
            int key = ComputeKey(attachmentType, variantId);
            return _attachmentVariants.TryGetValue(key, out var descriptor) ? descriptor : Entity.Null;
        }
    }
}