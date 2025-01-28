using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class TilemapAuthoring : MonoBehaviour
{
    public Tilemap targetTilemap;  // The tilemap to convert to ECS
    public Material spriteMaterial; // The material for tiles
}