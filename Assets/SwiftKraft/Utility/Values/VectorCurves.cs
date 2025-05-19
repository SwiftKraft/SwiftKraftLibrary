using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    [Serializable]
    public class Vector2Curve
    {
        public AnimationCurve X = new();
        public AnimationCurve Y = new();

        public Vector2 Evaluate(float time) => new(X.EvaluateSafe(time), Y.EvaluateSafe(time));
    }

    [Serializable]
    public class Vector3Curve
    {
        public AnimationCurve X = new();
        public AnimationCurve Y = new();
        public AnimationCurve Z = new();

        public Vector3 Evaluate(float time) => new(X.EvaluateSafe(time), Y.EvaluateSafe(time), Z.EvaluateSafe(time));
    }

    [Serializable]
    public class Vector4Curve
    {
        public AnimationCurve X = new();
        public AnimationCurve Y = new();
        public AnimationCurve Z = new();
        public AnimationCurve W = new();

        public Vector4 Evaluate(float time) => new(X.EvaluateSafe(time), Y.EvaluateSafe(time), Z.EvaluateSafe(time), W.EvaluateSafe(time));
    }

    public static class CurveExtensions
    {
        public static float EvaluateSafe(this AnimationCurve curve, float time, float defVal = 0f) => curve.length > 0 ? curve.Evaluate(time) : defVal;
    }
}
