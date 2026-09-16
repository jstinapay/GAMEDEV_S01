using UnityEngine;

namespace GummiShip
{
    /// <summary>
    /// Procedural meshes for block shapes Unity has no primitive for.
    /// Currently: a square pyramid (4-sided cone) used as the classic
    /// Gummi nose. Base is a 1x1 square at z=-0.5, apex at z=+0.5,
    /// so it mounts flush on a hull face and points forward (+Z).
    /// </summary>
    public static class GummiMeshLibrary
    {
        static Mesh pyramid;

        public static Mesh Pyramid()
        {
            if (pyramid != null)
                return pyramid;

            pyramid = new Mesh();
            pyramid.name = "Gummi_Pyramid";
            var vertices = new Vector3[]
            {
                // Base corners (z = -0.5).
                new Vector3(-0.5f, -0.5f, -0.5f), // 0
                new Vector3(0.5f, -0.5f, -0.5f),  // 1
                new Vector3(0.5f, 0.5f, -0.5f),   // 2
                new Vector3(-0.5f, 0.5f, -0.5f),  // 3
                // Apex (z = +0.5).
                new Vector3(0f, 0f, 0.5f),        // 4
            };
            // Outward-facing winding (counter-clockwise from outside).
            var triangles = new int[]
            {
                0, 2, 1, 0, 3, 2, // base (faces -Z)
                0, 1, 4,          // bottom slope (faces -Y)
                1, 2, 4,          // right slope (faces +X)
                2, 3, 4,          // top slope (faces +Y)
                3, 0, 4,          // left slope (faces -X)
            };
            var uvs = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0.5f, 0.5f),
            };
            pyramid.vertices = vertices;
            pyramid.triangles = triangles;
            pyramid.uv = uvs;
            pyramid.RecalculateNormals();
            pyramid.RecalculateBounds();
            return pyramid;
        }
    }
}
