using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Tiles {
    public class TilePrefabsAuthoring : MonoBehaviour {
        public GameObject GrassPrefab;
        public GameObject BorderPrefab;
    }
    public struct GridGenerationCompleted : IComponentData { }


    public class TilePrefabsBaker : Baker<TilePrefabsAuthoring> {
        public override void Bake(TilePrefabsAuthoring authoring) {
            var grassPrefabEntity = GetEntity(authoring.GrassPrefab, TransformUsageFlags.Dynamic);
            var borderPrefabEntity = GetEntity(authoring.BorderPrefab, TransformUsageFlags.Dynamic);

            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new TilePrefabs {
                GrassPrefab = grassPrefabEntity,
                BorderPrefab = borderPrefabEntity
            });
        }
    }

    public struct TilePrefabs : IComponentData {
        public Entity GrassPrefab;
        public Entity BorderPrefab;
    }
    
    public struct Tile : IComponentData {
        public int2 GridPosition;    // Position in the grid
        public bool IsWalkable;      // Whether the tile is walkable
        public TileType Type;        // Type of tile (e.g., Grass, Water, Border)
    }

    public enum TileType {
        Grass,
        Water,
        Border
    }  
}