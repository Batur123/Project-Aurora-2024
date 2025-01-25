using ECS;
using ECS.Animations;
using Unity.Entities;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SpriteAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerSingleton>();
        state.RequireForUpdate<PlayerAnimation>();
    }

    public void OnUpdate(ref SystemState state) {
        return;
        float dt = SystemAPI.Time.DeltaTime;

        // Check if we're pressing any WASD key *this frame* (held down)
        bool isWalking = Input.GetKey(KeyCode.W)
                      || Input.GetKey(KeyCode.A)
                      || Input.GetKey(KeyCode.S)
                      || Input.GetKey(KeyCode.D);

        // 1) Handle transitions (Idle <--> Walk)
        foreach (var animRef in SystemAPI.Query<RefRW<AnimatorState>>().WithAll<PlayerAnimation>())
        {
            var anim = animRef.ValueRO;

            if (isWalking)
            {
                // If the character is not already in Walk, switch to it
                if (anim.CurrentAnimation != AnimationType.Walk)
                {
                    anim.CurrentAnimation = AnimationType.Walk;
                    anim.CurrentFrame = 0;
                    anim.Timer = 0f;
                    animRef.ValueRW = anim;
                }
            }
            else
            {
                // If no WASD input, switch to Idle if not already Idle
                if (anim.CurrentAnimation != AnimationType.Idle)
                {
                    anim.CurrentAnimation = AnimationType.Idle;
                    anim.CurrentFrame = 0;
                    anim.Timer = 0f;
                    animRef.ValueRW = anim;
                }
            }
        }

        // 2) Animation update & sprite assignment
        foreach (var (animRef, clips, entity) in
                 SystemAPI.Query<RefRW<AnimatorState>, SpriteAnimationClips>().WithAll<PlayerAnimation>()
                          .WithEntityAccess())
        {
            var anim = animRef.ValueRO;

            // Select which sprite array & duration to use
            Sprite[] frames;
            float frameDuration;
            switch (anim.CurrentAnimation)
            {
                case AnimationType.Walk:
                    frames = clips.WalkSprites;
                    frameDuration = clips.WalkFrameDuration;
                    break;
                default: // Includes AnimationType.Idle
                    frames = clips.IdleSprites;
                    frameDuration = clips.IdleFrameDuration;
                    break;
            }

            // If no frames, skip
            if (frames == null || frames.Length == 0)
                continue;

            // Advance frame timer
            anim.Timer += dt;
            while (anim.Timer >= frameDuration)
            {
                anim.Timer -= frameDuration;
                anim.CurrentFrame++;

                // Loop the frames
                if (anim.CurrentFrame >= frames.Length)
                {
                    anim.CurrentFrame = 0;
                }
            }

            // Write back updated AnimatorState
            animRef.ValueRW = anim;
            int frameIndex = math.clamp(anim.CurrentFrame, 0, frames.Length - 1);

            // 3) Get the SpriteRenderer from PlayerSingleton
            var playerSingleton = SystemAPI.GetSingleton<PlayerSingleton>();
            var sr = SystemAPI.ManagedAPI.GetComponent<SpriteRenderer>(playerSingleton.PlayerEntity);

            if (sr != null)
            {
                sr.sprite = frames[frameIndex];
            }
        }
    }
}
