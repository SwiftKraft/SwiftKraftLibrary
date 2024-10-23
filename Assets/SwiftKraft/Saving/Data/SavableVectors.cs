using UnityEngine;

namespace SwiftKraft.Saving.Data
{
    public struct SavableVector2
    {
        public float x, y;

        public SavableVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public SavableVector2(Vector2 vec)
        {
            x = vec.x;
            y = vec.y;
        }

        public static implicit operator Vector2(SavableVector2 v) => new(v.x, v.y);
        public static implicit operator SavableVector2(Vector2 v) => new(v);
    }

    public struct SavableVector3
    {
        public float x, y, z;

        public SavableVector3(float x, float y)
        {
            this.x = x;
            this.y = y;
            this.z = default;
        }

        public SavableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public SavableVector3(Vector3 vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
        }

        public static implicit operator Vector3(SavableVector3 v) => new(v.x, v.y, v.z);
        public static implicit operator SavableVector3(Vector3 v) => new(v);
    }

    public struct SavableVector4
    {
        public float x, y, z, w;

        public SavableVector4(float x, float y)
        {
            this.x = x;
            this.y = y;
            z = default;
            w = default;
        }

        public SavableVector4(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            w = default;
        }

        public SavableVector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public SavableVector4(Vector4 vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
            w = vec.w;
        }

        public static implicit operator Vector4(SavableVector4 v) => new(v.x, v.y, v.z, v.w);
        public static implicit operator SavableVector4(Vector4 v) => new(v);
    }

    public struct SavableQuaternion
    {
        public float x, y, z, w;

        public SavableQuaternion(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            w = 1f;
        }

        public SavableQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public SavableQuaternion(Quaternion quat)
        {
            x = quat.x;
            y = quat.y;
            z = quat.z;
            w = quat.w;
        }

        public static implicit operator Quaternion(SavableQuaternion v) => new(v.x, v.y, v.z, v.w);
        public static implicit operator SavableQuaternion(Quaternion v) => new(v);
    }
}
