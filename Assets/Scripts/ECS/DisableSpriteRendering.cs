using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ECS {
    [UpdateInGroup(typeof(InitializationSystemGroup))] // Runs on the main thread
    public partial struct DisableSpriteRendererSystem : ISystem {
        public void OnUpdate(ref SystemState state) {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (request, entity) in SystemAPI
                         .Query<RefRO<DisableSpriteRendererRequest>>()
                         .WithAll<SpriteRenderer>()
                         .WithEntityAccess()) {
                SpriteRenderer spriteRenderer = state.EntityManager.GetComponentObject<SpriteRenderer>(entity);
                spriteRenderer.enabled = false;
                ecb.RemoveComponent<DisableSpriteRendererRequest>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}