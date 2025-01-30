using ECS.TileMap;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class TilemapHybridBaker : Baker<TilemapHybridAuthoring>
{
    public override void Bake(TilemapHybridAuthoring authoring)
    {
        // Create an entity to represent this tilemap setup in ECS
        var entity = GetEntity(TransformUsageFlags.None);

        if (authoring.tilemap == null)
            Debug.LogError($"Tilemap is null on GameObject '{authoring.name}'!");
        if (authoring.tileMaterial == null)
            Debug.LogError($"Material is null on GameObject '{authoring.name}'!");

        AddComponentObject(entity, new TilemapRenderData
        {
            tileScale = authoring.tileScale,
            tileMap = authoring.tilemap,
            tileMaterial = authoring.tileMaterial,
        });
        AddComponent(entity, new RenderTilemapTag { });
    }
}

public struct RenderTilemapTag : IComponentData {}

public class TilemapRenderData : IComponentData
{
    public float tileScale;
    public Tilemap tileMap;
    public Material tileMaterial;
}