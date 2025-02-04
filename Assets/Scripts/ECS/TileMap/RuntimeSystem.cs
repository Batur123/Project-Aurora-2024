using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;
using Unity.Rendering;

namespace ECS.TileMap {

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TilemapRenderSystem : ISystem {
        private EntityQuery m_TilemapQuery;
        private bool m_Initialized;

        public void OnCreate(ref SystemState state) {
            return;
            // We'll look for any entity that has these three components:
            m_TilemapQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<RenderTilemapTag>()
            );
            m_Initialized = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            return;
            if (m_Initialized)
                return; // only run once

            var entityManager = state.EntityManager;

            // Gather up all tilemap-entities. (In many games, you might just have one.)
            using var entities = m_TilemapQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return;

            // For simplicity, let's handle the first tilemap found:
            var tilemapEntity = entities[0];

            var renderData = entityManager.GetComponentData<TilemapRenderData>(tilemapEntity);
            var tilemap = renderData.tileMap;
            var material = renderData.tileMaterial;
            Debug.Log("Tile map result" + tilemap);
            Debug.Log("Tile map result" + material);

            // 1) Create a "prototype" entity that has the required ECS rendering components
            //    We do this once, then we instantiate it for each tile.
            var prototype = entityManager.CreateEntity();

            // Describe how we want to render (shadows off, etc.)
            var desc = new RenderMeshDescription(
                shadowCastingMode: ShadowCastingMode.Off,
                receiveShadows: false
            );

            // Create a small array with our single mesh & single material.
            // We'll create a simple 1x1 quad in code. (See the helper method below.)
            var mesh = GenerateQuadMesh();
            var renderMeshArray = new RenderMeshArray(
                new Material[] { material },
                new Mesh[] { mesh }
            );
            Debug.Log("Tile map result" + mesh);

            // Add the ECS rendering components. This attaches things like:
            // - MaterialMeshInfo
            // - RenderFilterSettings
            // - RenderBounds
            // - and so on
            RenderMeshUtility.AddComponents(
                prototype,
                entityManager,
                desc,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            );

            // We'll also need a LocalToWorld to set the transform of each entity
            entityManager.AddComponentData(prototype, new LocalToWorld());

            // 2) Loop over the tilemap’s occupied cells, spawn an entity for each tile
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var bounds = tilemap.cellBounds;
            for (int z = bounds.zMin; z < bounds.zMax; z++) {
                for (int y = bounds.yMin; y < bounds.yMax; y++) {
                    for (int x = bounds.xMin; x < bounds.xMax; x++) {
                        var cellPos = new Vector3Int(x, y, z);
                        var tile = tilemap.GetTile(cellPos);
                        if (tile == null)
                            continue; // skip empty

                        // Instantiate our prototype
                        var e = ecb.Instantiate(prototype);
                        Debug.Log("RUN INSTANT");

                        // Position in the world
                        float3 worldPos = tilemap.GetCellCenterWorld(cellPos);

                        // Apply optional scaling from TilemapRenderData
                        float3 scale = new float3(renderData.tileScale);

                        // Construct a TRS matrix for LocalToWorld
                        float4x4 transform = float4x4.TRS(
                            worldPos,
                            quaternion.identity,
                            scale
                        );
                        ecb.SetComponent(e, new LocalToWorld { Value = transform });
                    }
                }
            }

            // 3) Playback the command buffer to actually create those entities
            ecb.Playback(entityManager);
            ecb.Dispose();

            // 4) (Optional) Destroy the prototype; no longer needed
            entityManager.DestroyEntity(prototype);

            // 5) (Optional) Destroy the original tilemap entity if you don’t need it anymore
            // entityManager.DestroyEntity(tilemapEntity);

            // Mark system as done
            m_Initialized = true;
        }

        // Simple 1x1 quad mesh with UV 0..1
        private static Mesh GenerateQuadMesh() {
            var mesh = new Mesh();
            mesh.name = "QuadMesh(Entities)";

            // A 1x1 quad, centered at origin
            var vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(0.5f, -0.5f, 0)
            };
            var uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0)
            };
            var triangles = new int[] { 0, 1, 2, 2, 3, 0 };

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}