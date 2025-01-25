using ECS.Animations;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimationAuthoring : MonoBehaviour
{
    [Header("Idle Animation")]
    public Sprite[] idleSprites;
    public float idleFrameDuration = 0.1f;

    [Header("Jump Animation")]
    public Sprite[] jumpSprites;
    public float jumpFrameDuration = 0.1f;

    class Baker : Baker<SpriteAnimationAuthoring>
    {
        public override void Bake(SpriteAnimationAuthoring authoring)
        {
            // Create an Entity from this GameObject
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // 2) Create and add a managed component with the sprite arrays
            AddComponentObject(entity, new SpriteAnimationClips
            {
                IdleSprites = authoring.idleSprites,
                IdleFrameDuration = authoring.idleFrameDuration,

                WalkSprites = authoring.jumpSprites,
                WalkFrameDuration = authoring.jumpFrameDuration
            });

            // 3) Add the unmanaged AnimatorState component to track animation
            AddComponent(entity, new AnimatorState
            {
                CurrentAnimation = AnimationType.Idle,
                CurrentFrame = 0,
                Timer = 0f
            });
            AddComponent(entity, new PlayerAnimation {});
        }
    }
}