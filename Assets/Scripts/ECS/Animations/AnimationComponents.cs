using Unity.Entities;
using UnityEngine;

namespace ECS.Animations {
    public enum AnimationType : byte
    {
        Idle,
        Walk
    }

    public struct EnemyAnimation : IComponentData{}
    public struct PlayerAnimation : IComponentData{}

    public struct AnimatorState : IComponentData
    {
        public AnimationType CurrentAnimation;
        public int CurrentFrame;
        public float Timer;
    }

    public class SpriteAnimationClips : IComponentData
    {
        public Sprite[] IdleSprites;
        public float IdleFrameDuration;

        public Sprite[] WalkSprites;
        public float WalkFrameDuration;
    }
}