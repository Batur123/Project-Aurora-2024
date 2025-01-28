using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBaker : Baker<TilemapAuthoring>
{
    public override void Bake(TilemapAuthoring authoring)
    {
        var tilemap = authoring.targetTilemap;
        var bounds = tilemap.cellBounds;

        // Retrieve the material (either provided or use a default one)
        var material = authoring.spriteMaterial;

        // Register the material and create a reusable mesh
        var entitiesGraphicsSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        var materialID = entitiesGraphicsSystem.RegisterMaterial(material);
        var meshID = entitiesGraphicsSystem.RegisterMesh(CreateQuadMesh());

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var cellPos = new Vector3Int(x, y, 0);
                var tileBase = tilemap.GetTile(cellPos);

                if (tileBase == null || !(tileBase is Tile tile))
                    continue;

                var tileEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);

                // Extract sprite texture
                var texture = tile.sprite?.texture;
                if (texture == null)
                    continue;

                // Register the texture as a unique material
                var tileMaterial = new Material(material) { mainTexture = texture };
                var tileMaterialID = entitiesGraphicsSystem.RegisterMaterial(tileMaterial);

                // Add TileData (optional: use for game logic)
                AddComponent(tileEntity, new TileData
                {
                    GridPosition = new int2(x, y),
                    TileType = 0 // Example: Add custom type logic here
                });

                // Add MaterialMeshInfo for rendering
                AddComponent(tileEntity, new MaterialMeshInfo(tileMaterialID, meshID));

                // Add transform for positioning
                AddComponent(tileEntity, LocalTransform.FromPosition(new float3(x * 0.16f, y * 0.16f, 0)));
            }
        }
    }

    private Mesh CreateQuadMesh()
    {
        float width = 0.16f; // Match the Grid's Cell Size
        float height = 0.16f;

        var mesh = new Mesh
        {
            vertices = new[]
            {
                new Vector3(-width / 2, -height / 2, 0),  // Bottom-left
                new Vector3(width / 2, -height / 2, 0),   // Bottom-right
                new Vector3(-width / 2, height / 2, 0),   // Top-left
                new Vector3(width / 2, height / 2, 0)     // Top-right
            },
            uv = new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            },
            triangles = new[] { 0, 2, 1, 2, 3, 1 }
        };

        mesh.RecalculateBounds();
        return mesh;
    }
}

public struct TileData : IComponentData
{
    public int2 GridPosition;
    public int TileType;
}
