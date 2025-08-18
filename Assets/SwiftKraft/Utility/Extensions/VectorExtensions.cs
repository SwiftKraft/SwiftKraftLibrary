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

        public static Vector2 CircleToSquare(this Vector2 p)
        {
            float r2 = p.magnitude;
            if (r2 <= 0f) return Vector2.zero;

            float rInf = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
            return (rInf > 0f) ? p * (r2 / rInf) : Vector2.zero;
        }

        public static Vector2 SquareToCircle(this Vector2 q)
        {
            float rInf = Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.y));
            if (rInf <= 0f) return Vector2.zero;

            float r2 = q.magnitude;
            return (r2 > 0f) ? q * (rInf / r2) : Vector2.zero;
        }

        public static Vector3 SphereToCube(this Vector3 p)
        {
            float r2 = p.magnitude;
            if (r2 <= 0f) return Vector3.zero;

            float rInf = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
            return (rInf > 0f) ? p * (r2 / rInf) : Vector3.zero;
        }

        public static Vector3 CubeToSphere(this Vector3 q)
        {
            float rInf = Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.y), Mathf.Abs(q.z));
            if (rInf <= 0f) return Vector3.zero;

            float r2 = q.magnitude;
            return (r2 > 0f) ? q * (rInf / r2) : Vector3.zero;
        }
    }
}
