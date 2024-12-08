using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    /// <summary>
    /// Subclass of LerpInterpolater, uses LerpAngle instead of Lerp.
    /// </summary>
    [Serializable]
    public class LerpAngleInterpolater : LerpInterpolater
    {
        protected override float Calculate(float deltaTime) => Mathf.LerpAngle(CurrentValue, MaxValue, Mathf.Pow(0.5f, LerpValue * deltaTime));
    }
}
