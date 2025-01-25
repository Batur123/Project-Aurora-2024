using ECS.Animations;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteEnemyAnimationAuthoring : MonoBehaviour
{
    [Header("Walk Animation")]
    public Sprite[] walkSprites;
    public float walkFrameDuration = 0.1f;

    class Baker : Baker<SpriteEnemyAnimationAuthoring>
    {
        public override void Bake(SpriteEnemyAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponentObject(entity, new SpriteAnimationClips
            {
                WalkSprites = authoring.walkSprites,
                WalkFrameDuration = authoring.walkFrameDuration,
            });

            AddComponent(entity, new AnimatorState
            {
                CurrentAnimation = AnimationType.Walk,
                CurrentFrame = 0,
                Timer = 0f
            });
            AddComponent(entity, new EnemyAnimation{});

        }
    }
}