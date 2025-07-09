// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using UnityEngine;
    using CoreQuaternion = System.Numerics.Quaternion;
    using CoreVec2 = System.Numerics.Vector2;
    using CoreVec3 = System.Numerics.Vector3;
    using CoreVec4 = System.Numerics.Vector4;
    using CoreColor = System.Numerics.Vector4;
    using System;
    using Common;

    public static class CoherenceToUnityConverters
    {
        public static Quaternion ToUnityQuaternion(this CoreQuaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static CoreQuaternion ToCoreQuaternion(this Quaternion q)
        {
            return new CoreQuaternion(q.x, q.y, q.z, q.w);
        }

        public static Vector2 ToUnityVector2(this CoreVec2 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static CoreVec2 ToCoreVector2(this Vector2 v)
        {
            return new CoreVec2(v.x, v.y);
        }

        public static Vector3 ToUnityVector3(this CoreVec3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static CoreVec3 ToCoreVector3(this Vector3 v)
        {
            return new CoreVec3(v.x, v.y, v.z);
        }

        public static Vector3 ToUnityVector3(this Vector3d v)
        {
            if (v.x < float.MinValue || v.x > float.MaxValue)
            {
                throw new OverflowException($"Cannot convert vector3d to vector3, component X is overflowing 32-bits ({v.x})");
            }

            if (v.y < float.MinValue || v.y > float.MaxValue)
            {
                throw new OverflowException($"Cannot convert vector3d to vector3, component Y is overflowing 32-bits ({v.y})");
            }

            if (v.z < float.MinValue || v.z > float.MaxValue)
            {
                throw new OverflowException($"Cannot convert vector3d to vector3, component Z is overflowing 32-bits ({v.z})");
            }

            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        public static Vector3d ToVector3d(this Vector3 v)
        {
            return new Vector3d(v.x, v.y, v.z);
        }

        public static Vector4 ToUnityVector4(this CoreVec4 v)
        {
            return new Vector4(v.X, v.Y, v.Z, v.W);
        }

        public static CoreVec4 ToCoreVector4(this Vector4 v)
        {
            return new CoreVec4(v.x, v.y, v.z, v.w);
        }

        public static Color ToUnityColor(this CoreColor c)
        {
            return new Color(c.X, c.Y, c.Z, c.W);
        }

        public static CoreColor ToCoreColor(this Color c)
        {
            return new CoreColor(c.r, c.g, c.b, c.a);
        }
    }
}
