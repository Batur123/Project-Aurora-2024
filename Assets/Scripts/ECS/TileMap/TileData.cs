using UnityEngine;
using UnityEngine.Tilemaps;

namespace ECS.TileMap {
    [CreateAssetMenu(menuName = "Tiles/CustomTile")]
    public class CustomTile : Tile
    {
        public bool isWall;
        public bool isTrigger;
        public string tileName = "Default";
        // Or an enum: public TileType tileType;
    }

}