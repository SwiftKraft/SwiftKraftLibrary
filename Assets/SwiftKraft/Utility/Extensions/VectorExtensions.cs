using UnityEngine;

namespace SwiftKraft.Utils
{
    public static class VectorExtensions
    {
        public static Vector2 GridSnap(this Vector2 vec, Vector2 grid, Vector2 offset = default) => new(vec.x.GridSnap(grid.x, offset.x), vec.y.GridSnap(grid.y, offset.y));
        public static Vector3 GridSnap(this Vector3 vec, Vector3 grid, Vector3 offset = default) => new(vec.x.GridSnap(grid.x, offset.x), vec.y.GridSnap(grid.y, offset.y), vec.z.GridSnap(grid.z, offset.z));
        public static Vector4 GridSnap(this Vector4 vec, Vector4 grid, Vector4 offset = default) => new(vec.x.GridSnap(grid.x, offset.x), vec.y.GridSnap(grid.y, offset.y), vec.z.GridSnap(grid.z, offset.z), vec.w.GridSnap(grid.w, offset.w));

        public static bool Longer(this Vector2 pos, float target) => pos.sqrMagnitude > target * target;
        public static bool Longer(this Vector3 pos, float target) => pos.sqrMagnitude > target * target;
        public static bool Longer(this Vector4 pos, float target) => pos.sqrMagnitude > target * target;
        public static bool Longer(this Vector2 pos, Vector2 target) => pos.sqrMagnitude > target.sqrMagnitude;
        public static bool Longer(this Vector3 pos, Vector3 target) => pos.sqrMagnitude > target.sqrMagnitude;
        public static bool Longer(this Vector4 pos, Vector4 target) => pos.sqrMagnitude > target.sqrMagnitude;

        public static bool Shorter(this Vector2 pos, float target) => pos.sqrMagnitude < target * target;
        public static bool Shorter(this Vector3 pos, float target) => pos.sqrMagnitude < target * target;
        public static bool Shorter(this Vector4 pos, float target) => pos.sqrMagnitude < target * target;
        public static bool Shorter(this Vector2 pos, Vector2 target) => pos.sqrMagnitude < target.sqrMagnitude;
        public static bool Shorter(this Vector3 pos, Vector3 target) => pos.sqrMagnitude < target.sqrMagnitude;
        public static bool Shorter(this Vector4 pos, Vector4 target) => pos.sqrMagnitude < target.sqrMagnitude;

        public static bool Equal(this Vector2 pos, float target) => pos.sqrMagnitude == target * target;
        public static bool Equal(this Vector3 pos, float target) => pos.sqrMagnitude == target * target;
        public static bool Equal(this Vector4 pos, float target) => pos.sqrMagnitude == target * target;
        public static bool Equal(this Vector2 pos, Vector2 target) => pos.sqrMagnitude == target.sqrMagnitude;
        public static bool Equal(this Vector3 pos, Vector3 target) => pos.sqrMagnitude == target.sqrMagnitude;
        public static bool Equal(this Vector4 pos, Vector4 target) => pos.sqrMagnitude == target.sqrMagnitude;

        public static bool InRange(this Vector2 pos, Vector2 target, float range) => (pos - target).Shorter(range);
        public static bool InRange(this Vector3 pos, Vector3 target, float range) => (pos - target).Shorter(range);
        public static bool InRange(this Vector4 pos, Vector4 target, float range) => (pos - target).Shorter(range);
    }
}
