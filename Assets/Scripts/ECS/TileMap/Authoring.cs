using UnityEngine;
using UnityEngine.Tilemaps;

namespace ECS.TileMap {
    public class TilemapHybridAuthoring : MonoBehaviour
    {
        [Tooltip("Reference to the Unity Tilemap component.")]
        public Tilemap tilemap;

        [Tooltip("Material to use for all ECS tile quads.")]
        public Material tileMaterial;

        [Tooltip("Scale of each tile quad in world units.")]
        public float tileScale = 1.0f;
    }
}