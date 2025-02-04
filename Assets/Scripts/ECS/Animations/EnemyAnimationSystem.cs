using ECS;
using ECS.Animations;
using Unity.Entities;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SpriteEnemyAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemyAnimation>();
    }

    public void OnUpdate(ref SystemState state) {
        return;
        // float dt = SystemAPI.Time.DeltaTime;
        // foreach (var animRef in SystemAPI.Query<RefRW<AnimatorState>>().WithAll<EnemyAnimation>())
        // {
        //     var anim = animRef.ValueRO;
        //     anim.CurrentAnimation = AnimationType.Walk;
        //     anim.CurrentFrame = 0;
        //     anim.Timer = 0f;
        //     animRef.ValueRW = anim;
        // }
        //
        // foreach (var (localTransform, entity) in
        //          SystemAPI.Query<RefRW<LocalTransform>>().WithAll<EnemyTag>()
        //              .WithEntityAccess()) {
        //     // 2) Animation update & sprite assignment
        //     foreach (var (animRef, clips, animationEntity) in
        //              SystemAPI.Query<RefRW<AnimatorState>, SpriteAnimationClips>().WithAll<EnemyAnimation>()
        //                  .WithEntityAccess())
        //     {
        //         var anim = animRef.ValueRO;
        //
        //         // Select which sprite array & duration to use
        //         Sprite[] frames;
        //         float frameDuration;
        //         frames = clips.WalkSprites;
        //         frameDuration = clips.WalkFrameDuration;
        //         if (frames == null || frames.Length == 0)
        //             continue;
        //
        //         anim.Timer += dt;
        //         while (anim.Timer >= frameDuration)
        //         {
        //             anim.Timer -= frameDuration;
        //             anim.CurrentFrame++;
        //
        //             if (anim.CurrentFrame >= frames.Length)
        //             {
        //                 anim.CurrentFrame = 0;
        //             }
        //         }
        //
        //         animRef.ValueRW = anim;
        //         int frameIndex = math.clamp(anim.CurrentFrame, 0, frames.Length - 1);
        //
        //         SpriteRenderer enemySpriteRenderer = state.EntityManager.GetComponentObject<SpriteRenderer>(entity);
        //
        //         if (enemySpriteRenderer != null)
        //         {
        //             enemySpriteRenderer.sprite = frames[frameIndex];
        //         }
        //     }
        // }
    }
}
