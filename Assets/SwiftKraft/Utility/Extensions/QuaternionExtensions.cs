using UnityEngine;

namespace SwiftKraft.Utils
{
    public static class QuaternionExtensions
    {
        public static Quaternion SmoothDamp(this Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
        {
            if (Time.deltaTime == 0f)
                return current;

            Vector3 c = current.eulerAngles;
            Vector3 t = target.eulerAngles;
            return Quaternion.Euler(
              Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime),
              Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime),
              Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime)
            );
        }

        public static float NormalizeAngle(this float a) => a > 180f ? a - 360f : a;

        public static bool IsNaN(this Quaternion q) => float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w);
    }
}