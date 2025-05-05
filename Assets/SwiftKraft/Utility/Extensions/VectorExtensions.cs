using UnityEngine;

namespace SwiftKraft.Utils
{
    public static class VectorExtensions
    {
        public static Vector2 GridSnap(this Vector2 vec, Vector2 grid, Vector2 offset = default) => new(vec.x.GridSnap(grid.x, offset.x), vec.y.GridSnap(grid.y, offset.y));
        public static Vector3 GridSnap(this Vector3 vec, Vector3 grid, Vector3 offset = default) => new(vec.x.GridSnap(grid.x, offset.x), vec.y.GridSnap(grid.y, offset.y), vec.z.GridSnap(grid.z, offset.z));
        public static Vector4 GridSnap(this Vector4 vec, Vector4 grid, Vector4 offset = default) => new(vec.x.GridSnap(grid.x, offset.x), vec.y.GridSnap(grid.y, offset.y), vec.z.GridSnap(grid.z, offset.z), vec.w.GridSnap(grid.w, offset.w));
    }
}
