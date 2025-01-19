using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
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

                // Disable attachment rendering aswell
                if (state.EntityManager.HasComponent<GunTag>(entity)) {
                    DynamicBuffer<Child> children = state.EntityManager.GetBuffer<Child>(entity);
                    foreach (Child child in children) {
                        SpriteRenderer childSpriteRenderer = state.EntityManager.GetComponentObject<SpriteRenderer>(child.Value);
                        childSpriteRenderer.enabled = false;
                    }
                }
                
                ecb.RemoveComponent<DisableSpriteRendererRequest>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}