using UnityEngine;

namespace SwiftKraft.Utils
{
    public struct TransformData
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public TransformData(Transform transform, bool local = false)
        {
            Position = local ? transform.localPosition : transform.position;
            Rotation = local ? transform.localRotation : transform.rotation;
        }

        public TransformData(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public static implicit operator TransformData(Vector3 vec) => new(vec, Quaternion.identity);
        public static implicit operator TransformData(Quaternion quat) => new(default, quat);
        public static implicit operator TransformDataScale(TransformData s) => new(s.Position, s.Rotation, Vector3.one);
    }

    public struct TransformDataScale
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public TransformDataScale(Transform transform, bool local = false)
        {
            Position = local ? transform.localPosition : transform.position;
            Rotation = local ? transform.localRotation : transform.rotation;
            Scale = local ? transform.localScale : transform.lossyScale;
        }

        public TransformDataScale(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public static implicit operator TransformData(TransformDataScale s) => new(s.Position, s.Rotation);
    }
}
