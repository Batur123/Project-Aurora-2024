using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Tiles {
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GridGenerationSystem : ISystem {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<TilePrefabs>();
        }

        public void OnUpdate(ref SystemState state) {
            if (SystemAPI.HasSingleton<GridGenerationCompleted>()) {
                state.Enabled = false;
                return;
            }
            
            if (!SystemAPI.HasSingleton<TilePrefabs>()) {
                return;
            }

            var tilePrefabs = SystemAPI.GetSingleton<TilePrefabs>();

            const int gridStartX = -5;
            const int gridEndX = 5;
            const int gridStartY = -5;
            const int gridEndY = 5;

            // Define tile size and spacing in world units
            const float tileSize = 1.0f; // Default tile size (1x1 world units)
            const float tileSpacing = 1.0f; // Space between tiles

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Generate the grid
            for (int x = gridStartX; x < gridEndX; x++) {
                for (int y = gridStartY; y < gridEndY; y++) {
                    bool isBorder = x == gridStartX || y == gridStartY || x == gridEndX - 1 || y == gridEndY - 1;

                    var prefab = isBorder ? tilePrefabs.BorderPrefab : tilePrefabs.GrassPrefab;

                    var entity = ecb.Instantiate(prefab);

                    // Add Tile component
                    ecb.AddComponent<Tile>(entity);
                    ecb.SetComponent(entity, new Tile {
                        GridPosition = new int2(x, y),
                        Type = isBorder ? TileType.Border : TileType.Grass,
                        IsWalkable = !isBorder
                    });

                    // Calculate world position with spacing
                    float3 worldPosition = new float3(
                        x * (tileSize + tileSpacing),
                        y * (tileSize + tileSpacing),
                        0
                    );

                    // Add transform data
                    ecb.SetComponent(entity, new LocalTransform {
                        Position = worldPosition,
                        Rotation = quaternion.identity,
                        Scale = tileSize // Scale tile to fit grid cell
                    });
                }
            }

            // Mark the grid generation as completed
            var completedEntity = ecb.CreateEntity();
            ecb.AddComponent<GridGenerationCompleted>(completedEntity);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
