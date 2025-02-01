using UnityEngine;
namespace ECS {
    public static class SpriteMeshUtility
    {
        /// <summary>
        /// Converts a Sprite’s geometry data into a Unity Mesh.
        /// </summary>
        /// <param name="sprite">The sprite to convert.</param>
        /// <returns>A new Mesh constructed from the sprite’s vertices, triangles, and UVs.</returns>
        public static Mesh CreateMeshFromSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                Debug.LogError("Sprite is null!");
                return null;
            }

            // Create a new mesh and assign its data based on the sprite.
            Mesh mesh = new Mesh();
            mesh.name = sprite.name + "_Mesh";

            // Get the sprite's vertices (stored as Vector2) and convert to Vector3.
            Vector2[] spriteVertices = sprite.vertices;
            Vector3[] vertices = new Vector3[spriteVertices.Length];
            for (int i = 0; i < spriteVertices.Length; i++)
            {
                // Convert each 2D vertex to 3D. (You might need to adjust the z-value if desired.)
                vertices[i] = spriteVertices[i];
            }
            mesh.vertices = vertices;

            // Get the triangles.
            // Note: Sprite triangles are stored as an array of ushort. Convert them to int.
            ushort[] spriteTriangles = sprite.triangles;
            int[] triangles = new int[spriteTriangles.Length];
            for (int i = 0; i < spriteTriangles.Length; i++)
            {
                triangles[i] = spriteTriangles[i];
            }
            mesh.triangles = triangles;

            // Get UV coordinates.
            mesh.uv = sprite.uv;

            // Optionally recalculate normals and bounds.
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }

}