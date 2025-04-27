using Newtonsoft.Json;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Saving.Data
{
    [JsonObject(MemberSerialization.Fields)]
    public struct SavableTransformData
    {
        public SavableVector3 Position;
        public SavableQuaternion Rotation;

        public SavableTransformData(SavableVector3 position, SavableQuaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public static implicit operator TransformData(SavableTransformData t) => new(t.Position, t.Rotation);
        public static implicit operator SavableTransformData(TransformData t) => new(t.Position, t.Rotation);
        public static implicit operator SavableTransformDataScale(SavableTransformData s) => new(s.Position, s.Rotation, Vector3.one);
    }

    [JsonObject(MemberSerialization.Fields)]
    public struct SavableTransformDataScale
    {
        public SavableVector3 Position;
        public SavableQuaternion Rotation;
        public SavableVector3 Scale;

        public SavableTransformDataScale(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public static implicit operator TransformDataScale(SavableTransformDataScale t) => new(t.Position, t.Rotation, t.Scale);
        public static implicit operator SavableTransformDataScale(TransformDataScale t) => new(t.Position, t.Rotation, t.Scale);
        public static implicit operator SavableTransformData(SavableTransformDataScale s) => new(s.Position, s.Rotation);
    }
}
