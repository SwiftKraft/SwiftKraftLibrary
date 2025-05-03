using UnityEngine;

namespace SwiftKraft.Utils
{
    public static class VectorExtensions
    {
        public static void GetPitchYaw(this Vector3 direction, out float pitch, out float yaw)
        {
            direction.Normalize();

            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float flat = new Vector2(direction.x, direction.z).magnitude;
            pitch = Mathf.Atan2(direction.y, flat) * Mathf.Rad2Deg;
        }
    }
}
