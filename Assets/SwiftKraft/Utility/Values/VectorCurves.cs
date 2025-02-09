using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    [Serializable]
    public class Vector2Curve
    {
        public AnimationCurve X;
        public AnimationCurve Y;

        public Vector2 Evaluate(float time) => new(X.Evaluate(time), Y.Evaluate(time));
    }

    [Serializable]
    public class Vector3Curve
    {
        public AnimationCurve X;
        public AnimationCurve Y;
        public AnimationCurve Z;

        public Vector3 Evaluate(float time) => new(X.Evaluate(time), Y.Evaluate(time), Z.Evaluate(time));
    }

    [Serializable]
    public class Vector4Curve
    {
        public AnimationCurve X;
        public AnimationCurve Y;
        public AnimationCurve Z;
        public AnimationCurve W;

        public Vector4 Evaluate(float time) => new(X.Evaluate(time), Y.Evaluate(time), Z.Evaluate(time), W.Evaluate(time));
    }
}
